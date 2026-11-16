using System;
using UnityEngine;

/// <summary>
/// 一座已经建好的塔：索敌、开火、升级、出售。
/// 「建造」是 BuildManager 的事，塔自己不知道地图长什么样。
/// </summary>
public class Tower : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private SpriteRenderer body;
    [Tooltip("炮塔（会被旋转朝向目标）")]
    [SerializeField] private Transform turret;
    [Tooltip("子弹从这里出来，留空就用塔中心")]
    [SerializeField] private Transform firePoint;
    [Tooltip("素材默认朝向：-90 = 朝上，0 = 朝右")]
    [SerializeField] private float angleOffset = -90f;

    public TowerData Data { get; private set; }
    public int Level { get; private set; } = 1;
    public BuildTile Tile { get; private set; }

    public TowerLevel Stats => Data != null ? Data.GetLevel(Level) : TowerData.EmptyLevel;
    public bool IsMaxLevel => Data == null || Level >= Data.LevelCount;
    public int UpgradeCost => Data != null ? Data.GetUpgradeCost(Level) : 0;
    public int TotalInvested { get; private set; }
    public int SellValue => Mathf.FloorToInt(TotalInvested * 0.7f);

    /// <summary>等级/数值变化时通知 UI 刷新。</summary>
    public event Action<Tower> Changed;
    /// <summary>被卖掉时通知 BuildManager 清掉选中状态。</summary>
    public event Action<Tower> Sold;

    private float fireTimer;

    private void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
    }

    public void Setup(TowerData data, BuildTile tile)
    {
        if (data == null)
        {
            Debug.LogError("[Tower] Setup 收到 null 的 TowerData，这座塔不会工作。", this);
            enabled = false;
            return;
        }

        Data = data;
        Tile = tile;
        Level = 1;
        TotalInvested = data.BuildCost;
        fireTimer = 0f;
        ApplyVisual();
        Changed?.Invoke(this);
    }

    private void Update()
    {
        if (Data == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        fireTimer -= Time.deltaTime;

        var stats = Stats;
        var target = EnemyRegistry.FindBestTarget(transform.position, stats.range);
        if (target == null) return;

        Aim(target);

        if (fireTimer <= 0f)
        {
            Fire(target, stats);
            fireTimer = Mathf.Max(0.05f, stats.fireInterval);
        }
    }

    private void Aim(Enemy target)
    {
        if (turret == null) return;

        Vector2 dir = (Vector2)target.transform.position - (Vector2)turret.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        turret.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
    }

    private void Fire(Enemy target, TowerLevel stats)
    {
        if (target == null || !target.IsAlive) return;

        // 注意：projectilePrefab 是 Projectile 组件，PoolManager 需要的是 GameObject
        var prefabGo = Data.projectilePrefab != null ? Data.projectilePrefab.gameObject : null;

        if (prefabGo == null || PoolManager.Instance == null)
        {
            // 没有子弹预制体时退化为「瞬间命中」，方便先跑通
            target.DealDamage(stats.damage, Data.damageType);
            if (stats.slowMultiplier < 0.999f) target.ApplySlow(stats.slowMultiplier, stats.slowDuration);
            AudioManager.PlaySfx(Sfx.TowerShoot);
            return;
        }

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        var go = PoolManager.Instance.Spawn(prefabGo, spawnPos, Quaternion.identity);
        if (go == null) return;

        var projectile = go.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.LogError("[Tower] 子弹预制体上没有 Projectile 脚本：" + prefabGo.name);
            PoolManager.Instance.Despawn(go);   // 别把取出来的对象扔在原地
            return;
        }

        projectile.Launch(target, stats, Data);
        AudioManager.PlaySfx(Sfx.TowerShoot);
    }

    // ---------------- 升级 / 出售 ----------------

    public bool TryUpgrade()
    {
        if (Data == null) return false;

        if (IsMaxLevel)
        {
            ToastMessage.Show("已经是满级了");
            return false;
        }

        int cost = UpgradeCost;
        if (GameManager.Instance == null || !GameManager.Instance.TrySpendGold(cost))
        {
            ToastMessage.Show("金币不足，无法升级");
            return false;
        }

        TotalInvested += cost;
        Level++;
        ApplyVisual();
        AudioManager.PlaySfx(Sfx.Upgrade);
        Changed?.Invoke(this);
        return true;
    }

    public void Sell()
    {
        if (GameManager.Instance != null) GameManager.Instance.AddGold(SellValue);
        if (Tile != null) Tile.ClearTower();

        AudioManager.PlaySfx(Sfx.Sell);
        Sold?.Invoke(this);

        Destroy(gameObject);
    }

    private void ApplyVisual()
    {
        if (body != null && Data != null)
        {
            if (Data.levelSprites != null && Data.levelSprites.Length > 0)
            {
                int i = Mathf.Clamp(Level - 1, 0, Data.levelSprites.Length - 1);
                if (Data.levelSprites[i] != null) body.sprite = Data.levelSprites[i];
            }
            else if (Data.towerSprite != null)
            {
                body.sprite = Data.towerSprite;
            }
        }

        // 升级后明显变大一点，配合音效让玩家「感觉得到」（赛题要求）
        transform.localScale = Vector3.one * (1f + 0.12f * (Level - 1));
    }
}
