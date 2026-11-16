using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 统一的对象池。敌人、子弹、特效都从这里生成/回收，
/// 避免每次 Instantiate / Destroy 造成 GC 卡顿（赛题「对象池」考点）。
///
/// 用法：
///   var go = PoolManager.Instance.Spawn(prefab, pos, Quaternion.identity);
///   PoolManager.Instance.Despawn(go);
///
/// 池子不跨场景（每个关卡场景放一个），换关时池子里的物体随场景自动销毁，不会留下脏引用。
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    // 每个预制体对应一条队列
    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    // 记录「这个实例是从哪个预制体来的」，回收时才知道该放回哪条队列
    private readonly Dictionary<GameObject, GameObject> origin = new Dictionary<GameObject, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>取出一个可用实例（不够就新建），并设置位置。</summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[PoolManager] prefab 为 null，检查预制体引用是否丢了。");
            return null;
        }

        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        GameObject go = null;
        // 队列里可能有被外部误删（已销毁）的物体，跳过并清掉引用，避免 origin 字典越积越大
        while (queue.Count > 0 && go == null)
        {
            var candidate = queue.Dequeue();
            if (ReferenceEquals(candidate, null)) continue;          // 纯 C# 空，Dictionary 不接受 null 键
            if (candidate == null) { origin.Remove(candidate); continue; }  // Unity 已销毁（假 null）
            go = candidate;
        }

        if (go == null)
        {
            go = Instantiate(prefab);
            origin[go] = prefab;
        }

        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    /// <summary>回收。不是从池子里来的物体直接销毁。重复调用是安全的（第二次直接忽略）。</summary>
    public void Despawn(GameObject go)
    {
        if (go == null) return;

        // 已经回收过了：防止同一个实例被两次入队，导致之后两次 Spawn 拿到同一个物体
        if (!go.activeSelf) return;

        if (!origin.TryGetValue(go, out var prefab) || prefab == null || !pools.ContainsKey(prefab))
        {
            Destroy(go);
            return;
        }

        go.SetActive(false);
        go.transform.SetParent(transform, false);
        pools[prefab].Enqueue(go);
    }

    /// <summary>开局预热，避免第一波敌人出现时卡一下。</summary>
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab, transform);
            origin[go] = prefab;
            go.SetActive(false);
            queue.Enqueue(go);
        }
    }
}
