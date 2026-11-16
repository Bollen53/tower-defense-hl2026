using UnityEngine;

/// <summary>
/// 建造/选中时的攻击范围圈。
/// 场景里建一个空物体挂它，子物体是一个圆的 SpriteRenderer。
/// 圆图不要求正好 1 个世界单位——代码会按 sprite 实际尺寸归一化。
/// </summary>
public class RangeIndicator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer circleSprite;
    [SerializeField] private Color validColor = new Color(0.4f, 0.9f, 0.5f, 0.35f);
    [SerializeField] private Color invalidColor = new Color(0.95f, 0.3f, 0.25f, 0.35f);

    private void Awake()
    {
        if (circleSprite == null)
        {
            Debug.LogWarning("[RangeIndicator] circleSprite 未指定，建造时看不到范围圈。", this);
        }
        Hide();
    }

    public void Show(Vector3 worldPos, float radius, bool valid = true)
    {
        if (circleSprite == null || circleSprite.sprite == null) return;

        transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);

        // 按 sprite 的实际世界尺寸归一化，圆图多大都能正确显示成「radius 半径」
        float unitSize = circleSprite.sprite.bounds.size.x;
        if (unitSize <= 0.0001f) unitSize = 1f;

        circleSprite.transform.localScale = Vector3.one * (radius * 2f / unitSize);
        circleSprite.color = valid ? validColor : invalidColor;
        circleSprite.enabled = true;
    }

    public void Hide()
    {
        if (circleSprite != null) circleSprite.enabled = false;
    }
}
