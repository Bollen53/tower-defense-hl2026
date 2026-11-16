using UnityEngine;

/// <summary>
/// 一个可建造格。挂在带 BoxCollider2D 的地块上，编辑器里摆一片就是建造区。
/// </summary>
public class BuildTile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer highlight;

    public Tower Tower { get; private set; }
    public bool IsFree => Tower == null;

    private void Awake()
    {
        if (highlight == null)
        {
            Debug.LogWarning("[BuildTile] highlight 未指定，鼠标悬停时不会有高亮提示。", this);
        }
        else
        {
            highlight.enabled = false;
        }
    }

    public void SetTower(Tower tower)
    {
        Tower = tower;
    }

    public void ClearTower()
    {
        Tower = null;
    }

    public void SetHighlight(bool on)
    {
        if (highlight != null) highlight.enabled = on;
    }
}
