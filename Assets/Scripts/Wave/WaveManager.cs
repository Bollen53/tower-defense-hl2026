using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 波次调度：开局倒计时 → 一波波出怪 → 场上清空 → 布防倒计时 → 下一波 → 全清后判胜。
///
/// 支持多个出怪口：paths 里放几条路径，就有几个出生点。
/// 每个 SpawnGroup 用 pathIndex 指定从哪条路出（0 = paths[0]）。
///
/// 用协程写流程，比一堆状态机和计时器变量好读得多。
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private LevelWaves levelWaves;

    [Tooltip("出怪口路径：1 个就是单出口；2 个就是双出口。顺序对应 SpawnGroup.pathIndex")]
    [SerializeField] private WaypointPath[] paths;

    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("敌人父物体，留空则挂在场景根下")]
    [SerializeField] private Transform enemyParent;

    [Tooltip("开局预热多少个敌人到对象池")]
    [SerializeField] private int prewarmCount = 24;

    public int TotalWaves => levelWaves != null ? levelWaves.WaveCount : 0;
    public int PathCount => pathPoints == null ? 0 : pathPoints.Length;
    public int CurrentWave { get; private set; }
    public float Countdown { get; private set; }
    public bool Finished { get; private set; }

    /// <summary>(当前第几波, 总波数)</summary>
    public event Action<int, int> WaveChanged;
    /// <summary>布防倒计时剩余秒数（只在整秒变化时触发，避免每帧刷 UI）</summary>
    public event Action<float> CountdownChanged;
    public event Action LevelCleared;

    // 每条路径展开后的点数组
    private Vector2[][] pathPoints;

    private void Start()
    {
        EnemyRegistry.Clear();

        if (levelWaves == null)
        {
            Debug.LogError("[WaveManager] 没有配置 LevelWaves 资产", this);
            enabled = false;
            return;
        }

        if (levelWaves.WaveCount == 0)
        {
            Debug.LogError("[WaveManager] LevelWaves 里一波都没有配置，游戏会立刻判定胜利。", this);
            enabled = false;
            return;
        }

        if (paths == null || paths.Length == 0)
        {
            Debug.LogError("[WaveManager] paths 是空的，至少要有 1 条路径", this);
            enabled = false;
            return;
        }

        pathPoints = new Vector2[paths.Length][];
        for (int i = 0; i < paths.Length; i++)
        {
            if (paths[i] == null)
            {
                Debug.LogError($"[WaveManager] paths[{i}] 是空的", this);
                enabled = false;
                return;
            }

            var pts = paths[i].GetPoints();
            if (pts.Length < 2)
            {
                Debug.LogError($"[WaveManager] paths[{i}] 的有效路径点少于 2 个", this);
                enabled = false;
                return;
            }

            pathPoints[i] = pts;
        }

        if (PoolManager.Instance == null)
        {
            Debug.LogError("[WaveManager] 场景里缺 PoolManager", this);
            enabled = false;
            return;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("[WaveManager] enemyPrefab 未指定", this);
            enabled = false;
            return;
        }

        PoolManager.Instance.Prewarm(enemyPrefab, prewarmCount);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayBattleMusic();

        Debug.Log($"[WaveManager] 启动：{paths.Length} 个出怪口，{TotalWaves} 波");

        StartCoroutine(RunLevel());
    }

    private bool IsGameOver()
    {
        return GameManager.Instance != null && GameManager.Instance.State == GameState.Lost;
    }

    private IEnumerator RunLevel()
    {
        yield return Prep(levelWaves.firstPrepTime);

        for (int i = 0; i < TotalWaves; i++)
        {
            if (IsGameOver()) yield break;

            CurrentWave = i + 1;
            WaveChanged?.Invoke(CurrentWave, TotalWaves);
            AudioManager.PlaySfx(Sfx.WaveStart);

            yield return SpawnWave(levelWaves.waves[i]);

            while (EnemyRegistry.Count > 0)
            {
                if (IsGameOver()) yield break;
                yield return null;
            }

            if (i < TotalWaves - 1) yield return Prep(levelWaves.waves[i].prepTime);
        }

        if (IsGameOver()) yield break;

        Finished = true;
        LevelCleared?.Invoke();
        if (GameManager.Instance != null) GameManager.Instance.WinGame();
    }

    private IEnumerator Prep(float seconds)
    {
        float t = seconds;
        int lastShown = -1;

        while (t > 0f)
        {
            if (IsGameOver()) yield break;

            t -= Time.deltaTime;
            Countdown = Mathf.Max(0f, t);

            int shown = Mathf.CeilToInt(Countdown);
            if (shown != lastShown)
            {
                lastShown = shown;
                CountdownChanged?.Invoke(Countdown);
            }

            yield return null;
        }

        Countdown = 0f;
        CountdownChanged?.Invoke(0f);
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        if (wave == null || wave.groups == null) yield break;

        // ── 顺序模式：一组全部出完，才开始下一组 ──
        if (wave.sequentialGroups)
        {
            for (int i = 0; i < wave.groups.Count; i++)
            {
                if (IsGameOver()) yield break;

                var g = wave.groups[i];
                if (g == null || g.enemy == null || g.count <= 0) continue;

                yield return SpawnGroup(g, null);
            }

            yield break;
        }

        // ── 并发模式（默认）：所有组同时开始，用各自的 startDelay 错开 ──
        int running = 0;

        for (int i = 0; i < wave.groups.Count; i++)
        {
            var group = wave.groups[i];
            if (group == null || group.enemy == null || group.count <= 0) continue;

            running++;
            StartCoroutine(SpawnGroup(group, () => running--));
        }

        while (running > 0)
        {
            if (IsGameOver()) yield break;
            yield return null;
        }
    }

    private IEnumerator SpawnGroup(SpawnGroup group, Action onDone)
    {
        // try/finally：出了异常也一定把计数器减掉，否则整关会静默卡死
        try
        {
            if (group.startDelay > 0f) yield return new WaitForSeconds(group.startDelay);

            for (int i = 0; i < group.count; i++)
            {
                if (IsGameOver()) break;

                SpawnEnemy(group.enemy, group.pathIndex);

                if (group.interval > 0f) yield return new WaitForSeconds(group.interval);
            }
        }
        finally
        {
            onDone?.Invoke();
        }
    }

    private void SpawnEnemy(EnemyData data, int pathIndex)
    {
        if (PoolManager.Instance == null) return;

        int index = Mathf.Clamp(pathIndex, 0, pathPoints.Length - 1);
        if (index != pathIndex)
        {
            Debug.LogWarning($"[WaveManager] pathIndex={pathIndex} 超出范围（共 {pathPoints.Length} 条路），已用 {index}");
        }

        var pointList = pathPoints[index];

        var go = PoolManager.Instance.Spawn(enemyPrefab, pointList[0], Quaternion.identity);
        if (go == null) return;

        if (enemyParent != null) go.transform.SetParent(enemyParent, true);

        var enemy = go.GetComponent<Enemy>();
        var health = go.GetComponent<EnemyHealth>();

        if (enemy == null || health == null)
        {
            Debug.LogError("[WaveManager] 敌人预制体缺少 Enemy / EnemyHealth 脚本：" + enemyPrefab.name);
            PoolManager.Instance.Despawn(go);
            return;
        }

        enemy.Setup(data, pointList, health);
    }
}
