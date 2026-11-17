using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyEnemy : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;   // 每秒走几个单位，可以在 Inspector 里改
    [SerializeField] private float maxHealth = 50f;   // 最大值，可在 Inspector 改
    private float health;                              // 当前值，不需要在 Inspector 显示

    private static readonly Vector2[] path =       // 路径点：8 个坐标
    {
    new Vector2(-9f,  4f),
    new Vector2(-6f,  4f),
    new Vector2(-6f, -3f),
    new Vector2(-1f, -3f),
    new Vector2(-1f,  4f),
    new Vector2( 4f,  4f),
    new Vector2( 4f, -3f),
    new Vector2( 9f, -3f),
};

    private int index = 1;   // 当前要去第几个点（从 1 开始，因为 0 是出生点）

    private void BackBase()
    {
        Debug.Log("[MyEnemy] 到达终点");
        MyPoolManager.Instance.Despawn(gameObject);   // 先只回收，扣命下一步加
    }
    private void Move()
    {
        if (index >= path.Length) 
        {
            BackBase();
            return;
        }
        Vector2 target = path[index];
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if(Vector2.Distance(transform.position,target) <= 0.02f)
        {
            index++;
        }
    }

    private void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log("扣10滴血, 还剩"+health+"血");
        if (health <= 0) Die();
    }

    private void Die()
    {
        Debug.Log("[MyEnemy] 死亡");
        MyPoolManager.Instance.Despawn(gameObject);
    }

    private void OnEnable()
    {
        transform.position = path[0];
        index = 1;
        health = maxHealth;
        Debug.Log($"[MyEnemy] 出现，血量 {health}");
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        if (Input.GetKeyDown(KeyCode.K)) TakeDamage(10f);
    }
}
