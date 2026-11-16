using UnityEngine;

/// <summary>敌人的特殊机制类型。</summary>
public enum EnemySpecial
{
    None,   // 普通
    Dash,   // 周期性短暂加速（冲刺）
    Heal,   // 周期性治疗附近敌人
}

/// <summary>
/// 一种敌人的配置数据。这是一个 ScriptableObject 资产：
/// 右键 Create > TD > Enemy Data 就能建一个，改数值不需要动代码，也不用重新编译。
/// 三个关卡可以复用同一批数据资产，只改数值。
/// </summary>
[CreateAssetMenu(fileName = "Enemy_", menuName = "TD/Enemy Data", order = 1)]
public class EnemyData : ScriptableObject
{
    [Header("外观")]
    public string displayName = "普通敌人";
    public Sprite sprite;
    public Color tintColor = Color.white;
    public float scale = 1f;

    [Header("基础属性")]
    [Tooltip("最大生命值")]
    public float maxHealth = 100f;
    [Tooltip("每秒移动的世界单位")]
    public float moveSpeed = 1.5f;
    [Tooltip("护甲：物理伤害先减去这个值（最低造成 10% 伤害）")]
    public float armor = 0f;
    [Tooltip("击杀奖励金币")]
    public int reward = 10;
    [Tooltip("到达终点扣多少点生命")]
    public int damageToBase = 1;

    [Header("特殊机制")]
    public EnemySpecial special = EnemySpecial.None;
    [Tooltip("每隔多少秒触发一次")]
    public float specialInterval = 4f;
    [Tooltip("冲刺：速度倍率（1.8 = 快 80%）；治疗：按最大生命的比例治疗")]
    public float specialValue = 1.8f;
    [Tooltip("治疗的作用半径")]
    public float specialRadius = 2.5f;

    [Header("音效（可选）")]
    public AudioClip deathSfx;
}
