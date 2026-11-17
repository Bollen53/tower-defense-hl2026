using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyEnemy : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;   // 每秒走几个单位，可以在 Inspector 里改

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

    private void Move()
    {
        if (index >= path.Length) {return;}
        Vector2 target = path[index];
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if(Vector2.Distance(transform.position,target) <= 0.02f)
        {
            index++;
        }
    }

    void Start()
    {
        transform.position = path[0];
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }
}
