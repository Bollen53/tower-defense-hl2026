using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 子弹：追踪目标飞行，命中后结算单体或范围伤害，并施加减速。
/// 从对象池取出，用完回收。
///
/// 注意 targetSpawnId：对象池复用会让「已死的敌人」变成「下一波的新敌人」，
/// 只比较引用会导致子弹拐弯去追另一只怪。用 SpawnId 判断「还是不是同一只」。
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("表现")]
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private ParticleSystem hitVfx;

    private static readonly List<Enemy> splashBuffer = new List<Enemy>();

    private Enemy target;
    private int targetSpawnId = -1;
    private Vector3 lastTargetPos;

    private float speed = 9f;
    private float damage;
    private DamageType damageType;
    private float splashRadius;
    private float slowMultiplier = 1f;
    private float slowDuration;
    private float life;
    private bool launched;

    public void Launch(Enemy targetEnemy, TowerLevel stats, TowerData data)
    {
        target = targetEnemy;
        targetSpawnId = target != null ? target.SpawnId : -1;

        lastTargetPos = target != null ? target.transform.position : transform.position;
        speed = Mathf.Max(0.1f, data.projectileSpeed);
        damage = stats.damage;
        damageType = data.damageType;
        splashRadius = data.splashRadius;
        slowMultiplier = stats.slowMultiplier;
        slowDuration = stats.slowDuration;
        life = lifeTime;
        launched = true;

        AimTo(lastTargetPos);
    }

    private void Update()
    {
        if (!launched) return;

        life -= Time.deltaTime;
        if (life <= 0f)
        {
            Despawn();
            return;
        }

        // 目标还活着就继续追它；死了就飞向最后位置（范围伤害仍可能在原地炸开）
        if (IsTargetValid()) lastTargetPos = target.transform.position;

        Vector3 toTarget = lastTargetPos - transform.position;
        float distance = toTarget.magnitude;
        float step = speed * Time.deltaTime;

        if (distance <= step + 0.05f)
        {
            transform.position = lastTargetPos;
            Hit();
            return;
        }

        transform.position += toTarget / distance * step;
        AimTo(lastTargetPos);
    }

    private bool IsTargetValid()
    {
        return target != null && target.SpawnId == targetSpawnId && target.IsAlive;
    }

    private void AimTo(Vector3 worldPos)
    {
        Vector2 dir = worldPos - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f); // 假设子弹素材朝上
    }

    private void Hit()
    {
        if (splashRadius > 0.01f)
        {
            int count = EnemyRegistry.CollectInRange(transform.position, splashRadius, splashBuffer);
            for (int i = 0; i < count; i++) Apply(splashBuffer[i]);
        }
        else if (IsTargetValid())
        {
            Apply(target);
        }

        SpawnHitVfx();
        Despawn();
    }

    private void Apply(IDamageable victim)
    {
        if (victim == null || !victim.IsAlive) return;

        victim.TakeDamage(damage, damageType);

        if (slowMultiplier < 0.999f && victim is Enemy enemy)
        {
            enemy.ApplySlow(slowMultiplier, slowDuration);
        }
    }

    private void SpawnHitVfx()
    {
        if (hitVfx == null) return;

        var vfx = Instantiate(hitVfx, transform.position, Quaternion.identity);
        var main = vfx.main;
        float life2 = main.loop ? 2f : main.duration + main.startLifetime.constantMax;

        Destroy(vfx.gameObject, Mathf.Max(0.2f, life2));
    }

    private void Despawn()
    {
        if (!launched) return;

        launched = false;
        target = null;
        targetSpawnId = -1;

        if (PoolManager.Instance != null) PoolManager.Instance.Despawn(gameObject);
        else Destroy(gameObject);
    }
}
