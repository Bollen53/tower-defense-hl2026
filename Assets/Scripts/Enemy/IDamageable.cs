using UnityEngine;

/// <summary>伤害类型。物理伤害会被敌人的护甲抵消，魔法伤害无视护甲。</summary>
public enum DamageType
{
    Physical,
    Magic,
}

/// <summary>
/// 「能被打的东西」都实现这个接口。
/// 现在只有 EnemyHealth 实现（Enemy 也实现了它，内部转发给 EnemyHealth）。
/// 好处：以后加「可破坏的障碍物」「Boss 的护盾部件」时，
/// 子弹和塔不需要改代码，只要能拿到 IDamageable 就能打。
/// </summary>
public interface IDamageable
{
    bool IsAlive { get; }
    Transform Transform { get; }
    void TakeDamage(float amount, DamageType type);
}
