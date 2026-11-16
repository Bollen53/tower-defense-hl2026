using UnityEngine;

/// <summary>
/// 敌人头顶血条。用两个 SpriteRenderer 拼（背景 + 填充），
/// 填充条的 Transform 缩小 X 轴就是掉血。
///
/// 血条从「左边」开始缩，前提是填充条的 **pivot 居中**（Sprite 默认就是居中）。
/// 如果你的 fill 图把 pivot 改到了左边，把 fillBaseLocalPos 的修正去掉即可。
/// </summary>
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyHealth health;
    [Tooltip("填充条：一个 SpriteRenderer 的 Transform")]
    [SerializeField] private Transform fill;
    [SerializeField] private SpriteRenderer fillRenderer;

    [Header("颜色（血多 → 血少）")]
    [SerializeField] private Color highColor = new Color(0.35f, 0.9f, 0.4f);
    [SerializeField] private Color midColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color lowColor = new Color(0.95f, 0.3f, 0.25f);

    private Vector3 fillBaseScale = Vector3.one;
    private float fillWidth = 1f;
    private Vector3 fillBaseLocalPos;

    private void Awake()
    {
        if (health == null) health = GetComponentInParent<EnemyHealth>();
        if (health == null)
        {
            Debug.LogError("[WorldHealthBar] 找不到父级 EnemyHealth，血条不会更新。", this);
            enabled = false;
            return;
        }

        if (fill == null)
        {
            Debug.LogError("[WorldHealthBar] fill 未指定，血条不会更新。", this);
            enabled = false;
            return;
        }

        fillBaseScale = fill.localScale;
        fillWidth = fillBaseScale.x;
        fillBaseLocalPos = fill.localPosition;
    }

    private void OnEnable()
    {
        if (health != null) health.HealthChanged += Refresh;
    }

    private void OnDisable()
    {
        if (health != null) health.HealthChanged -= Refresh;
    }

    private void Refresh(EnemyHealth h)
    {
        if (fill == null) return;

        float n = h.Normalized;
        fill.localScale = new Vector3(fillWidth * n, fillBaseScale.y, fillBaseScale.z);

        // 让左端固定：center pivot 的图，中心要往左移「减少的一半」，左边缘才不动
        fill.localPosition = new Vector3(
            fillBaseLocalPos.x - fillWidth * (1f - n) * 0.5f,
            fillBaseLocalPos.y,
            fillBaseLocalPos.z);

        if (fillRenderer != null)
        {
            fillRenderer.color = n > 0.6f ? highColor : (n > 0.3f ? midColor : lowColor);
        }
    }
}
