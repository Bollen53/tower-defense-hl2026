using UnityEngine;

/// <summary>
/// 敌人行进路线。把子物体按顺序拖进 points 数组即可（建议用空物体做路径点）。
/// 场景里会画出连线，方便摆路线。
/// </summary>
public class WaypointPath : MonoBehaviour
{
    [SerializeField] private Transform[] points;

    public int Count => points == null ? 0 : points.Length;

    public Vector2[] GetPoints()
    {
        if (points == null || points.Length < 2) return new Vector2[0];

        // 漏拖一个点会静默变成「世界原点」，让敌人横穿地图、进度计算全乱，所以这里直接报错
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null)
            {
                Debug.LogError($"[WaypointPath] points[{i}] 是空的，检查是否漏拖路径点。", this);
                return new Vector2[0];
            }
        }

        var result = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++) result[i] = points[i].position;
        return result;
    }

    private void OnDrawGizmos()
    {
        if (points == null || points.Length < 2) return;

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        for (int i = 1; i < points.Length; i++)
        {
            if (points[i - 1] == null || points[i] == null) continue;
            Gizmos.DrawLine(points[i - 1].position, points[i].position);
        }

        Gizmos.color = Color.green;
        if (points[0] != null) Gizmos.DrawWireSphere(points[0].position, 0.3f);

        Gizmos.color = Color.red;
        if (points[points.Length - 1] != null)
            Gizmos.DrawWireSphere(points[points.Length - 1].position, 0.4f);
    }
}
