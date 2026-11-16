using System;
using UnityEngine;

/// <summary>某一等级塔的数值。第 1 项就是「刚建好」时的那一级。</summary>
[Serializable]
public class TowerLevel
{
    [Tooltip("第 1 级 = 建造费；后面几级 = 升级到这一级要花的钱")]
    public int cost = 50;
    public float damage = 10f;
    public float range = 2.5f;
    [Tooltip("两次开火的间隔（秒），越小越快")]
    public float fireInterval = 0.8f;
    [Tooltip("命中后给敌人施加的速度倍率：1 = 不减速，0.5 = 减速一半")]
    public float slowMultiplier = 1f;
    [Tooltip("减速持续时间（秒）")]
    public float slowDuration = 0f;
}

/// <summary>
/// 一种防御塔的配置。三种塔（单体/范围/减速）就是三个这样的资产，
/// 区别只在数值和子弹预制体，代码完全复用。
/// </summary>
[CreateAssetMenu(fileName = "Tower_", menuName = "TD/Tower Data", order = 0)]
public class TowerData : ScriptableObject
{
    /// <summary>数据没配好时用的兜底实例，避免每帧 new 造成 GC。</summary>
    public static readonly TowerLevel EmptyLevel = new TowerLevel();

    [Header("信息")]
    public string displayName = "箭塔";
    [TextArea] public string description;
    public Sprite icon;

    [Header("外观")]
    public Sprite towerSprite;
    [Tooltip("可选：按等级换图。留空就用同一张图 + 放大")]
    public Sprite[] levelSprites;

    [Header("战斗")]
    public DamageType damageType = DamageType.Physical;
    [Tooltip("子弹预制体（挂 Projectile 脚本）。为空则直接瞬间命中")]
    public Projectile projectilePrefab;
    public float projectileSpeed = 9f;
    [Tooltip("溅射半径：0 = 单体攻击，>0 = 范围攻击")]
    public float splashRadius = 0f;

    [Header("等级数值")]
    public TowerLevel[] levels =
    {
        new TowerLevel(),
        new TowerLevel(),
        new TowerLevel(),
    };

    public int LevelCount => levels == null ? 0 : levels.Length;
    public int BuildCost => LevelCount > 0 ? levels[0].cost : 0;
    public bool IsSplash => splashRadius > 0.01f;

    public TowerLevel GetLevel(int level1Based)
    {
        if (LevelCount == 0) return EmptyLevel;

        int index = Mathf.Clamp(level1Based - 1, 0, LevelCount - 1);
        return levels[index] != null ? levels[index] : EmptyLevel;
    }

    /// <summary>从 currentLevel 升到下一级的费用；已满级返回 0。</summary>
    public int GetUpgradeCost(int currentLevel)
    {
        if (LevelCount == 0 || currentLevel >= LevelCount) return 0;

        var next = levels[currentLevel];
        return next != null ? next.cost : 0;
    }

    private void OnValidate()
    {
        if (LevelCount == 0)
        {
            Debug.LogWarning($"[{name}] levels 是空的，这座塔建出来不会攻击。", this);
            return;
        }

        for (int i = 1; i < LevelCount; i++)
        {
            if (levels[i] == null || levels[i - 1] == null) continue;

            if (levels[i].cost > 0 && levels[i].cost < levels[i - 1].cost)
            {
                Debug.LogWarning($"[{name}] 第 {i + 1} 级的升级费比上一级还便宜，检查一下数值。", this);
            }
        }
    }
}
