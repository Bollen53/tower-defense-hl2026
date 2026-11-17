using UnityEngine;

public class MySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;   // Inspector 里拖预制体
    [SerializeField] private float interval = 2f;      // 每 2 秒生成一只

    private float timer;

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = interval;
        SpawnOne();
    }

    private void SpawnOne()
    {
        // ① 从池子取一个实例；位置和朝向这样传
        var go = MyPoolManager.Instance.Spawn(
            enemyPrefab,
            new Vector3(-9f, 4f, 0f),        // 起点
            Quaternion.identity);            // 不旋转

        if (go == null) return;

        // ② 拿到实例之后，可以对它做任何事
        var enemy = go.GetComponent<MyEnemy>();
        Debug.Log($"[MySpawner] 生成了 {go.name}，id={go.GetInstanceID()}");
    }
}