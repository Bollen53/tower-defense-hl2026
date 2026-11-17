using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【学习版】对象池：敌人、子弹这种「一秒生成十几个、用完就扔」的东西，
/// 不要 Instantiate/Destroy，而是回收再利用。
///
/// 为什么？每次 Destroy 都会产生垃圾，垃圾多了触发 GC，那一帧就卡一下。
/// 手机上一波 20 只怪 + 每秒十几发子弹，卡顿非常明显。
///
/// 它做的事就两件：
///   Spawn  —— 从队列里取一个；没有就 new 一个
///   Despawn—— 关掉、放进队列，不真正销毁
/// </summary>
public class TestPoolManager : MonoBehaviour
{
    public static TestPoolManager Instance { get; private set; }

    // 每个"预制体"对应一条回收队列
    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    // 记录"这个实例是从哪个预制体来的"——回收时才知道该放回哪条队列
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

    /// <summary>取出一个实例并摆到指定位置。</summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[TestPool] prefab 是空的");
            return null;
        }

        // 这个预制体第一次用，建一条空队列
        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        // 队列里有就复用，没有就新建
        GameObject go = queue.Count > 0 ? queue.Dequeue() : null;

        if (go == null)
        {
            go = Instantiate(prefab);
            origin[go] = prefab;
        }

        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);      // 关键：复用的对象要重新激活
        return go;
    }

    /// <summary>回收：不是销毁，是关掉后放回队列。</summary>
    public void Despawn(GameObject go)
    {
        if (go == null) return;

        // 已经回收过就别重复入队，否则两次 Spawn 会拿到同一个对象
        if (!go.activeSelf) return;

        if (!origin.TryGetValue(go, out var prefab))
        {
            // 不是从池子里来的，直接销毁
            Destroy(go);
            return;
        }

        go.SetActive(false);
        go.transform.SetParent(transform, false);   // 挂到池子下面，Hierarchy 干净
        pools[prefab].Enqueue(go);
    }
}
