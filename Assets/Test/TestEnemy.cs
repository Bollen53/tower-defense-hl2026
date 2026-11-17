using UnityEngine;

/// <summary>
/// 【学习版】一只敌人：沿路径走、会掉血、死了给钱、走到终点扣命。
/// 正式版 Enemy 还管减速、冲刺、治疗、对象池登记表——那些先不管，这里只留主干。
///
/// 测试操作：Play 后按 **K** 给自己造成 10 点伤害，用来观察「受伤 → 死亡 → 加钱」。
/// </summary>
public class TestEnemy : MonoBehaviour
{
    [Header("数值")]
    [SerializeField] private float maxHealth = 50f;
    [Tooltip("每秒移动多少个世界单位")]
    [SerializeField] private float speed = 1.5f;
    [Tooltip("被打死给玩家多少金币")]
    [SerializeField] private int reward = 5;
    [Tooltip("走到终点扣玩家多少生命")]
    [SerializeField] private int damageToBase = 1;

    [Header("路径")]
    [Tooltip("留空就用内置的 8 个点（和 Level_01 一致）")]
    [SerializeField] private Transform[] waypoints;

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

    private Vector2[] path;
    private int index;          // 当前要去第几个点
    private float health;
    private bool finished;      // 已死或已到终点，防止重复结算

    /// <summary>
    /// 用 OnEnable 而不是 Start 来初始化：
    /// 对象池复用时 Start 只执行一次，而 OnEnable 每次取出都会执行 —— 这才是重置状态的正确位置。
    /// </summary>
    private void OnEnable()
    {
        path = BuildPath();

        if (path.Length < 2)
        {
            Debug.LogError("[TestEnemy] 路径点不足 2 个", this);
            enabled = false;
            return;
        }

        index = 1;
        health = maxHealth;
        finished = false;
        transform.position = path[0];

        Debug.Log($"[TestEnemy] 出现：血量 {health}，速度 {speed}，路径 {path.Length} 点");
    }

    private void Update()
    {
        if (finished || path == null) return;

        // 测试用：按 K 自伤 10 点
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("[TestEnemy] 按下 K → 自伤 10 点");
            TakeDamage(10f);
            if (finished) return;
        }

        Move();
    }

    private void Move()
    {
        if (index >= path.Length)
        {
            ReachBase();
            return;
        }

        Vector2 target = path[index];
        Vector2 current = transform.position;

        // 每帧最多走 speed * deltaTime 这么远，所以速度与帧率无关
        Vector2 next = Vector2.MoveTowards(current, target, speed * Time.deltaTime);
        transform.position = next;

        // 够近了就切下一个点（0.02 是为了防止浮点误差导致永远“差一点点”）
        if (Vector2.Distance(next, target) <= 0.02f) index++;
    }

    public void TakeDamage(float amount)
    {
        if (finished) return;

        health -= amount;
        Debug.Log($"[TestEnemy] 受伤 {amount}，剩余 {health}");

        if (health <= 0f) Die();
    }

    private void Die()
    {
        finished = true;
        Debug.Log($"[TestEnemy] 死亡 → 玩家 +{reward} 金币");

        if (TestGameManager.Instance != null) TestGameManager.Instance.AddGold(reward);

        Recycle();
    }

    private void ReachBase()
    {
        finished = true;
        Debug.Log("[TestEnemy] 走到终点 → 玩家扣命");

        if (TestGameManager.Instance != null) TestGameManager.Instance.EnemyEscaped(damageToBase);

        Recycle();
    }

    /// <summary>回收：有池子就放回池子，没有就单纯关掉。</summary>
    private void Recycle()
    {
        if (TestPoolManager.Instance != null) TestPoolManager.Instance.Despawn(gameObject);
        else gameObject.SetActive(false);
    }

    private Vector2[] BuildPath()
    {
        // 场景里拖了点就用场景的，否则用内置坐标
        if (waypoints != null && waypoints.Length >= 2)
        {
            var result = new Vector2[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                result[i] = waypoints[i] != null ? (Vector2)waypoints[i].position : Vector2.zero;
            }
            return result;
        }

        return BuiltInPath;
    }

    /// <summary>头顶血条：没有 Canvas 也能看见，纯调试用。</summary>
    private void OnGUI()
    {
        if (finished || health <= 0f) return;
        if (Camera.main == null) return;

        // 世界坐标 → 屏幕坐标；GUI 的 Y 轴是反的，所以要用 Screen.height 减一下
        Vector3 sp = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.55f);
        if (sp.z < 0f) return;

        const float w = 60f, h = 10f;
        var back = new Rect(sp.x - w / 2f, Screen.height - sp.y, w, h);

        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(back, Texture2D.whiteTexture);

        GUI.color = Color.green;
        float ratio = Mathf.Clamp01(health / maxHealth);
        GUI.DrawTexture(new Rect(back.x + 1, back.y + 1, (w - 2) * ratio, h - 2), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    /// <summary>Scene 视图里画出路径，方便对位置。</summary>
    private void OnDrawGizmos()
    {
        if (waypoints != null && waypoints.Length >= 2) return;

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        for (int i = 1; i < BuiltInPath.Length; i++)
        {
            Gizmos.DrawLine(BuiltInPath[i - 1], BuiltInPath[i]);
        }
    }
}
