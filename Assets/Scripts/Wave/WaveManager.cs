using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 波次调度：开局倒计时 → 一波波出怪 → 场上清空 → 布防倒计时 → 下一波 → 全清后判胜。
///
/// 用协程写流程，比一堆状态机和计时器变量好读得多，也方便加「提前召唤下一波」这类功能。
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private LevelWaves levelWaves;
    [SerializeField] private WaypointPath path;
    [SerializeField] private GameObject enemyPrefab;
    [Tooltip("敌人父物体，留空则挂在场景根下")]
    [SerializeField] private Transform enemyParent;
    [Tooltip("开局预热多少个敌人到对象池")]
    [SerializeField] private int prewarmCount = 24;

    public int TotalWaves => levelWaves != null ? levelWaves.WaveCount : 0;
    public int CurrentWave { get; private set; }
    public float Countdown { get; private set; }
    public bool Finished { get; private set; }

    /// <summary>(当前第几波, 总波数)</summary>
    public event Action<int, int> WaveChanged;
    /// <summary>布防倒计时剩余秒数（只在整秒变化时触发，避免每帧刷 UI）</summary>
    public event Action<float> CountdownChanged;
    public event Action LevelCleared;

    private Vector2[] pathPoints;

    private void Start()
    {
        // 换场景时静态列表可能残留上一关的引用
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

        pathPoints = path != null ? path.GetPoints() : new Vector2[0];
        if (pathPoints.Length < 2)
        {
            Debug.LogError("[WaveManager] WaypointPath 至少要有 2 个有效路径点", this);
            enabled = false;
            return;
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

        // 进关卡切战斗 BGM（主菜单放的是另一首）
        if (AudioManager.Instance != null) AudioManager.Instance.PlayBattleMusic();

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

            // 等这一波全部被打死或跑到终点
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

            t -= Time.deltaTime;        // 暂停时 Time.deltaTime 为 0，倒计时自动冻结
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
        // 用 try/finally 保证计数器一定被减掉：
        // 一旦这里抛异常而没执行 onDone，SpawnWave 的 running 永不归零，整关会静默卡死。
        try
        {
            if (group.startDelay > 0f) yield return new WaitForSeconds(group.startDelay);

            for (int i = 0; i < group.count; i++)
            {
                if (IsGameOver()) break;

                SpawnEnemy(group.enemy);

                if (group.interval > 0f) yield return new WaitForSeconds(group.interval);
            }
        }
        finally
        {
            onDone?.Invoke();
        }
    }

    private void SpawnEnemy(EnemyData data)
    {
        if (PoolManager.Instance == null) return;

        var go = PoolManager.Instance.Spawn(enemyPrefab, pathPoints[0], Quaternion.identity);
        if (go == null) return;

        if (enemyParent != null) go.transform.SetParent(enemyParent, true);

        var enemy = go.GetComponent<Enemy>();
        var health = go.GetComponent<EnemyHealth>();

        if (enemy == null || health == null)
        {
            Debug.LogError("[WaveManager] 敌人预制体缺少 Enemy / EnemyHealth 脚本：" + enemyPrefab.name);
            PoolManager.Instance.Despawn(go);   // 别把一个空壳留在场上
            return;
        }

        enemy.Setup(data, pathPoints, health);
    }
}
