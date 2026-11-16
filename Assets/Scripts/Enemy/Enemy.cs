using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人总控：沿路径移动、特殊机制（冲刺/治疗）、减速状态、死亡与到达终点的处理。
/// 只关心「敌人自身」，金币和生命值都交给 GameManager。
///
/// 预制体只需要一个：Enemy + EnemyHealth + SpriteRenderer，
/// 不同敌人靠 EnemyData 资产换外观和数值，所以对象池只要池一种预制体。
/// </summary>
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("引用")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private ParticleSystem deathVfx;

    private static readonly List<Enemy> healBuffer = new List<Enemy>();

    public EnemyData Data { get; private set; }
    public EnemyHealth Health { get; private set; }

    /// <summary>每次从池子里取出来都会 +1。子弹用它判断「我追的那只怪还在不在」。</summary>
    public int SpawnId { get; private set; }

    /// <summary>0 = 起点，1 = 终点。塔用它来优先打「最靠近终点」的敌人。</summary>
    public float Progress { get; private set; }

    public bool IsAlive => Health != null && Health.IsAlive;
    public Transform Transform => transform;

    public float CurrentSpeed => Data == null ? 0f : Data.moveSpeed * slowMultiplier * dashMultiplier;

    private Vector2[] path;
    private int waypointIndex = 1;
    private float totalLength = 1f;
    private float travelled;

    private Color baseTint = Color.white;
    private float slowMultiplier = 1f;
    private float slowTimer;
    private float dashMultiplier = 1f;
    private float specialTimer;
    private bool finished;   // 已死亡 / 已到终点 / 初始化失败，防止重复结算

    private void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        if (Health == null) Health = GetComponent<EnemyHealth>();
    }

    /// <summary>从对象池取出后由 WaveManager 调用。</summary>
    public void Setup(EnemyData data, Vector2[] points, EnemyHealth health)
    {
        if (data == null)
        {
            Debug.LogError("[Enemy] EnemyData 为 null，检查 LevelWaves 里每一组的 enemy 是否漏拖。");
            finished = true;
            Despawn();
            return;
        }

        Data = data;
        SpawnId++;
        if (health != null) Health = health;

        if (points == null || points.Length < 2)
        {
            Debug.LogError("[Enemy] 路径点少于 2 个，检查 WaypointPath 的配置。");
            finished = true;   // 关键：别让它以半初始化状态永久留在场上
            Despawn();
            return;
        }

        if (Health == null)
        {
            Debug.LogError("[Enemy] 预制体上缺少 EnemyHealth 组件。", this);
            finished = true;
            Despawn();
            return;
        }

        path = points;
        waypointIndex = 1;
        travelled = 0f;
        Progress = 0f;
        slowMultiplier = 1f;
        slowTimer = 0f;
        dashMultiplier = 1f;
        finished = false;

        // 配成 0 会导致「每帧触发一次特殊机制」，这里钳一个下限
        specialTimer = data.special == EnemySpecial.None
            ? float.MaxValue
            : Mathf.Max(0.05f, data.specialInterval);

        totalLength = 0f;
        for (int i = 1; i < path.Length; i++) totalLength += Vector2.Distance(path[i - 1], path[i]);
        if (totalLength < 0.001f) totalLength = 0.001f;

        transform.position = path[0];
        transform.localScale = Vector3.one * Mathf.Max(0.05f, data.scale);

        baseTint = data.tintColor;
        if (body != null)
        {
            // 数据里没配精灵时，保留预制体/场景上原本的图，不要清空
            if (data.sprite != null) body.sprite = data.sprite;
            body.color = baseTint;
        }

        Health.Setup(data);
        Health.Died -= HandleDied;
        Health.Died += HandleDied;

        EnemyRegistry.Register(this);
    }

    private void Update()
    {
        if (Data == null || finished || !IsAlive) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        TickSlow();
        TickSpecial();
        Move();
    }

    private void Move()
    {
        if (waypointIndex >= path.Length)
        {
            ReachBase();
            return;
        }

        Vector2 pos = transform.position;
        Vector2 target = path[waypointIndex];
        Vector2 next = Vector2.MoveTowards(pos, target, CurrentSpeed * Time.deltaTime);

        travelled += Vector2.Distance(pos, next);
        Progress = Mathf.Clamp01(travelled / totalLength);
        transform.position = next;

        if (Vector2.Distance(next, target) <= 0.02f) waypointIndex++;
    }

    // ---------------- 受伤 ----------------

    public void TakeDamage(float amount, DamageType type)
    {
        if (Health != null) Health.TakeDamage(amount, type);
    }

    /// <summary>保留这个名字，塔在「没有子弹预制体」时会直接调它。</summary>
    public void DealDamage(float amount, DamageType type)
    {
        TakeDamage(amount, type);
    }

    public void Heal(float amount)
    {
        if (Health != null) Health.Heal(amount);
    }

    // ---------------- 减速 ----------------

    /// <summary>叠减速取「最强的那一次」，持续时间取最长，不会因为连打而无限叠加。</summary>
    public void ApplySlow(float multiplier, float duration)
    {
        if (!IsAlive || duration <= 0f) return;

        multiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
        slowMultiplier = Mathf.Min(slowMultiplier, multiplier);
        slowTimer = Mathf.Max(slowTimer, duration);

        if (body != null)
        {
            // 减速时变蓝白——让玩家「看得见」减速生效（赛题要求的反馈）
            body.color = Color.Lerp(baseTint, new Color(0.55f, 0.85f, 1f), 0.7f);
        }
    }

    private void TickSlow()
    {
        if (slowTimer <= 0f) return;

        slowTimer -= Time.deltaTime;
        if (slowTimer <= 0f)
        {
            slowTimer = 0f;
            slowMultiplier = 1f;
            if (body != null) body.color = baseTint;
        }
    }

    // ---------------- 特殊机制 ----------------

    private void TickSpecial()
    {
        if (Data.special == EnemySpecial.None) return;

        specialTimer -= Time.deltaTime;
        if (specialTimer > 0f) return;

        specialTimer = Mathf.Max(0.05f, Data.specialInterval);

        if (Data.special == EnemySpecial.Dash) StartCoroutine(DashRoutine());
        else if (Data.special == EnemySpecial.Heal) HealNearby();
    }

    private IEnumerator DashRoutine()
    {
        dashMultiplier = Mathf.Max(1f, Data.specialValue);
        yield return new WaitForSeconds(0.6f);
        dashMultiplier = 1f;
    }

    private void HealNearby()
    {
        float amount = Data.maxHealth * Mathf.Clamp(Data.specialValue, 0f, 1f);
        int count = EnemyRegistry.CollectInRange(transform.position, Data.specialRadius, healBuffer);

        for (int i = 0; i < count; i++)
        {
            if (healBuffer[i] != this) healBuffer[i].Heal(amount);
        }
    }

    // ---------------- 结算 ----------------

    private void HandleDied(EnemyHealth _)
    {
        if (finished) return;
        finished = true;

        if (Data != null && GameManager.Instance != null) GameManager.Instance.AddGold(Data.reward);

        if (Data != null && Data.deathSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayClip(Data.deathSfx);
        else
            AudioManager.PlaySfx(Sfx.EnemyDeath);

        SpawnDeathVfx();
        Despawn();
    }

    private void ReachBase()
    {
        if (finished) return;
        finished = true;

        // 换一个 SpawnId：还在飞的子弹会认为「目标已经不是同一只」，不再追这具尸体
        SpawnId++;

        if (Data != null && GameManager.Instance != null) GameManager.Instance.LoseLives(Data.damageToBase);
        AudioManager.PlaySfx(Sfx.BaseHit);
        Despawn();
    }

    private void SpawnDeathVfx()
    {
        if (deathVfx == null) return;

        var vfx = Instantiate(deathVfx, transform.position, Quaternion.identity);
        var main = vfx.main;

        float life = main.loop ? 2f : main.duration + main.startLifetime.constantMax;
        Destroy(vfx.gameObject, Mathf.Max(0.2f, life));
    }

    private void Despawn()
    {
        EnemyRegistry.Unregister(this);
        if (Health != null) Health.Died -= HandleDied;

        if (PoolManager.Instance != null) PoolManager.Instance.Despawn(gameObject);
        else Destroy(gameObject);
    }

    private void OnDisable()
    {
        // 换场景或池子回收时也要从登记表里清掉，否则静态列表会残留空引用
        EnemyRegistry.Unregister(this);
    }
}
