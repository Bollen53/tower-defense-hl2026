using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象池：生成与回收。
///
/// 核心思想：敌人死了不要 Destroy（会不断产生内存垃圾），
/// 而是 SetActive(false) 放回队列，下次要的时候再 SetActive(true) 拿出来用。
///
/// 数据结构就两本字典：
///   pools  = 预制体 → 一堆闲着的实例（队列，先进先出）
///   origin = 某个实例 → 它是从哪个预制体来的（回收时才知道该放回哪条队列）
///
/// Python 对照：
///   pools[prefab].Enqueue(x)  ≈  pools[prefab].append(x)
///   pools[prefab].Dequeue()   ≈  pools[prefab].popleft()
/// </summary>
public class MyPoolManager : MonoBehaviour
{
    public static MyPoolManager Instance { get; private set; }

    // 键：预制体；值：这个预制体的空闲实例队列
    private readonly Dictionary<GameObject, Queue<GameObject>> pools =
        new Dictionary<GameObject, Queue<GameObject>>();

    // 键：实例；值：它来自哪个预制体
    private readonly Dictionary<GameObject, GameObject> origin =
        new Dictionary<GameObject, GameObject>();

    private void Awake()
    {
        // 单例标准写法：已经有了就销毁自己，保证全局只有一个池子
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

    /// <summary>
    /// 取出一个实例放到指定位置。队列里有就复用，没有才新建。
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[MyPool] prefab 是空的", this);
            return null;
        }

        // 这个预制体第一次用 → 给它建一条空队列
        // TryGetValue 相当于 Python 的：if prefab in pools: queue = pools[prefab]
        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        // 队列非空就弹一个出来，否则 null
        GameObject go = queue.Count > 0 ? queue.Dequeue() : null;

        if (go == null)
        {
            // 池子里没有，只能真的创建一个，并记住它的来源
            go = Instantiate(prefab);
            origin[go] = prefab;
            Debug.Log($"[MyPool] 新建 id={go.GetInstanceID()}");
        }
        else
        {
            Debug.Log($"[MyPool] 复用 id={go.GetInstanceID()}");
        }

        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);              // 复用的对象是关着的，必须重新打开
        return go;
    }

    /// <summary>
    /// 回收：关掉 + 放回队列，不销毁。
    /// </summary>
    public void Despawn(GameObject go)
    {
        if (go == null) return;

        // 已经关着的说明回收过了，重复回收会让同一个实例在队列里出现两次，
        // 之后两次 Spawn 会拿到同一个对象 —— 非常难查的 bug。
        if (!go.activeSelf) return;

        // 不是从池子里来的（比如手动摆在场景里的），直接销毁
        if (!origin.TryGetValue(go, out var prefab) || prefab == null)
        {
            Destroy(go);
            return;
        }

        go.SetActive(false);

        // 挂到池子物体下面，让层级保持整洁（第二参数 false = 保持本地坐标）
        go.transform.SetParent(transform, false);

        pools[prefab].Enqueue(go);
        Debug.Log($"[MyPool] 回收 id={go.GetInstanceID()}，队列里现有 {pools[prefab].Count} 个");
    }
}
