using System;
using UnityEngine;

/// <summary>
/// 敌人的生命值部分：负责掉血、护甲减免、死亡事件。
/// 和「移动」拆开，是为了别写成一个什么都管的巨型脚本（赛题明确反对）。
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    /// <summary>血量变化（血条订阅它）</summary>
    public event Action<EnemyHealth> HealthChanged;
    /// <summary>死亡（Enemy 订阅它，负责发金币、播特效、回收）</summary>
    public event Action<EnemyHealth> Died;

    private EnemyData data;
    private float current;

    public float Max => data != null ? data.maxHealth : 1f;
    public float Current => current;
    public float Normalized => Max <= 0f ? 0f : Mathf.Clamp01(current / Max);
    public bool IsAlive { get; private set; }
    public Transform Transform => transform;

    /// <summary>每次从对象池取出来时调用，重置为满血。</summary>
    public void Setup(EnemyData enemyData)
    {
        if (enemyData == null)
        {
            // 别让它变成「满血的无敌敌人」：打不死也永不结算，比报错更难查
            Debug.LogError("[EnemyHealth] Setup 收到 null 的 EnemyData，这个敌人将无法受伤。", this);
            data = null;
            current = 0f;
            IsAlive = false;
            return;
        }

        data = enemyData;
        current = Max;
        IsAlive = true;
        HealthChanged?.Invoke(this);   // 主动刷一次，避免池子复用时血条停在上一只怪的血量
    }

    public void TakeDamage(float amount, DamageType type)
    {
        if (!IsAlive || data == null || amount <= 0f) return;

        float final = amount;
        if (type == DamageType.Physical)
        {
            // 护甲是固定减伤，但至少打进 10%，否则重甲会让物理塔完全无效
            final = Mathf.Max(amount * 0.1f, amount - data.armor);
        }

        current -= final;
        HealthChanged?.Invoke(this);

        if (current <= 0f)
        {
            current = 0f;
            IsAlive = false;
            Died?.Invoke(this);
        }
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;

        current = Mathf.Min(Max, current + amount);
        HealthChanged?.Invoke(this);
    }
}
