using System.Collections;
using UnityEngine;

/// <summary>
/// 【临时测试脚本】验证「敌人能沿路径走」，不参与正式关卡。
/// 自带每秒位置日志，方便确认到底有没有在动。
/// </summary>
public class DevSpawner : MonoBehaviour
{
    [SerializeField] private Enemy enemy;

    [Tooltip("可选：填了就用这个路径；留空用内置路径")]
    [SerializeField] private WaypointPath path;

    [Tooltip("测试用移动速度（正式数值在 EnemyData 里）")]
    [SerializeField] private float testSpeed = 2.5f;

    /// <summary>和 Docs/关卡布局工作表.md 里 Level_01 的路径一致</summary>
    private static readonly Vector2[] BuiltInPath =
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

    private void Start()
    {
        if (enemy == null)
        {
            Debug.LogError("[DevSpawner] 请把场景里的 Enemy 拖进 enemy 槽", this);
            return;
        }

        var health = enemy.GetComponent<EnemyHealth>();
        if (health == null)
        {
            Debug.LogError("[DevSpawner] Enemy 物体上缺少 EnemyHealth 组件", this);
            return;
        }

        Vector2[] points = (path != null) ? path.GetPoints() : BuiltInPath;
        if (points.Length < 2)
        {
            Debug.LogError("[DevSpawner] 路径点不够（至少 2 个）", this);
            return;
        }

        var data = ScriptableObject.CreateInstance<EnemyData>();
        data.displayName = "测试步兵";
        data.maxHealth = 55f;
        data.moveSpeed = testSpeed;
        data.armor = 0f;
        data.reward = 9;
        data.damageToBase = 1;
        data.scale = 1f;
        data.special = EnemySpecial.None;

        enemy.Setup(data, points, health);

        Debug.Log($"[DevSpawner] 已生成敌人：{points.Length} 个路径点，起点 {points[0]}，" +
                  $"共 {TotalLength(points):0.0} 单位，速度 {testSpeed}，理论上 {TotalLength(points) / testSpeed:0.0} 秒走完");

        StartCoroutine(LogPositions());
    }

    /// <summary>每秒打一次位置，用来判断「到底动没动」</summary>
    private IEnumerator LogPositions()
    {
        for (int i = 1; i <= 8; i++)
        {
            yield return new WaitForSeconds(1f);

            if (enemy == null)
            {
                Debug.Log("[DevSpawner] Enemy 已销毁/回收，停止记录");
                yield break;
            }

            Debug.Log($"[DevSpawner] {i}s  位置={enemy.transform.position}  " +
                      $"进度={enemy.Progress:0.000}  存活={enemy.IsAlive}  " +
                      $"当前速度={enemy.CurrentSpeed:0.00}  时间缩放={Time.timeScale}");
        }
    }

    private static float TotalLength(Vector2[] pts)
    {
        float sum = 0f;
        for (int i = 1; i < pts.Length; i++) sum += Vector2.Distance(pts[i - 1], pts[i]);
        return sum;
    }

    private void OnDrawGizmos()
    {
        if (path != null) return;

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        for (int i = 1; i < BuiltInPath.Length; i++)
        {
            Gizmos.DrawLine(BuiltInPath[i - 1], BuiltInPath[i]);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(BuiltInPath[0], 0.3f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(BuiltInPath[BuiltInPath.Length - 1], 0.4f);
    }
}
