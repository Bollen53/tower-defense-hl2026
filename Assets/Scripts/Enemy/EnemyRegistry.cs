using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场上所有活着的敌人的登记表。
/// 塔要「找范围内敌人」时直接查这里，比每帧 Physics2D.OverlapCircle 便宜得多，
/// 也不用给敌人挂碰撞体。
/// </summary>
public static class EnemyRegistry
{
    private static readonly List<Enemy> all = new List<Enemy>();

    public static IReadOnlyList<Enemy> All => all;

    /// <summary>
    /// 注意这里会先清理空引用：静态列表如果残留了已销毁对象，
    /// 调用方用 Count > 0 判断「本波是否清空」会永远为真，整关卡死。
    /// </summary>
    public static int Count
    {
        get
        {
            Prune();
            return all.Count;
        }
    }

    /// <summary>关掉 Domain Reload 时静态字段会跨播放会话残留，这里兜底清空。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        all.Clear();
    }

    public static void Register(Enemy enemy)
    {
        if (enemy == null || all.Contains(enemy)) return;
        all.Add(enemy);
    }

    public static void Unregister(Enemy enemy)
    {
        all.Remove(enemy);
    }

    /// <summary>清空（换场景时调用）。</summary>
    public static void Clear()
    {
        all.Clear();
    }

    /// <summary>
    /// 找攻击范围内「最接近终点」的敌人——这是塔防里最常见的索敌策略，
    /// 比「最近的」更不容易漏怪。
    /// </summary>
    public static Enemy FindBestTarget(Vector2 from, float range)
    {
        Enemy best = null;
        float bestProgress = -1f;
        float sqrRange = range * range;

        for (int i = all.Count - 1; i >= 0; i--)
        {
            var e = all[i];
            if (e == null) { all.RemoveAt(i); continue; }
            if (!e.IsAlive || e.Data == null) continue;
            if (((Vector2)e.transform.position - from).sqrMagnitude > sqrRange) continue;

            if (e.Progress > bestProgress)
            {
                bestProgress = e.Progress;
                best = e;
            }
        }
        return best;
    }

    /// <summary>
    /// 把范围内敌人拷进 buffer（避免遍历时有人死亡被移除导致报错）。
    /// 返回实际数量。
    /// </summary>
    public static int CollectInRange(Vector2 center, float radius, List<Enemy> buffer)
    {
        buffer.Clear();
        float sqr = radius * radius;

        for (int i = all.Count - 1; i >= 0; i--)
        {
            var e = all[i];
            if (e == null) { all.RemoveAt(i); continue; }
            if (!e.IsAlive) continue;
            if (((Vector2)e.transform.position - center).sqrMagnitude <= sqr) buffer.Add(e);
        }
        return buffer.Count;
    }

    private static void Prune()
    {
        for (int i = all.Count - 1; i >= 0; i--)
        {
            if (all[i] == null) all.RemoveAt(i);
        }
    }
}
