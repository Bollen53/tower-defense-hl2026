using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 建造管理：鼠标悬停高亮地块、点击建造、点击已有塔查看升级。
/// 屏幕坐标 → 世界坐标 → 点查询地块，是 2D 点击最稳的做法。
/// </summary>
public class BuildManager : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("通用塔预制体（挂 Tower + SpriteRenderer）")]
    [SerializeField] private GameObject towerPrefab;
    [Tooltip("塔的父物体，留空就放在本物体下")]
    [SerializeField] private Transform towerParent;
    [Tooltip("只检测这一层上的地块（必须勾，默认 Nothing 时点不到任何格子）")]
    [SerializeField] private LayerMask tileMask;
    [SerializeField] private RangeIndicator rangeIndicator;
    [SerializeField] private TowerInfoPanel infoPanel;

    public TowerData SelectedData { get; private set; }
    public event Action<TowerData> SelectionChanged;

    private Camera cam;
    private BuildTile hovered;
    private Tower selectedTower;

    private void Awake()
    {
        cam = Camera.main;
        if (cam == null)
            Debug.LogError("[BuildManager] 找不到 MainCamera：场景相机的 Tag 必须是 MainCamera。");

        if (towerPrefab == null)
            Debug.LogError("[BuildManager] towerPrefab 未指定，点击地块不会建塔。");

        if (tileMask.value == 0)
            Debug.LogWarning("[BuildManager] tileMask 是 Nothing，鼠标永远打不到地块；把 BuildTile 层勾上。");

        if (towerParent == null) towerParent = transform;
        if (infoPanel != null) infoPanel.Hide();
        if (rangeIndicator != null) rangeIndicator.Hide();
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            ClearHover();   // 暂停时别让格子一直亮着
            return;
        }

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        UpdateHover();

        if (Input.GetMouseButtonDown(0)) HandleClick();
        else if (Input.GetMouseButtonDown(1)) ClearSelection();
    }

    // ---------------- 悬停 ----------------

    private void UpdateHover()
    {
        var tile = TileUnderMouse();
        if (tile != hovered)
        {
            if (hovered != null) hovered.SetHighlight(false);
            hovered = tile;
            if (hovered != null) hovered.SetHighlight(true);
        }

        if (rangeIndicator == null) return;

        if (selectedTower != null)
        {
            // 选中已有的塔：范围圈要一直显示，而不是只闪一帧
            rangeIndicator.Show(selectedTower.transform.position, selectedTower.Stats.range, true);
        }
        else if (SelectedData != null && hovered != null && hovered.IsFree)
        {
            rangeIndicator.Show(hovered.transform.position, SelectedData.GetLevel(1).range);
        }
        else
        {
            rangeIndicator.Hide();
        }
    }

    private void ClearHover()
    {
        if (hovered != null) hovered.SetHighlight(false);
        hovered = null;
    }

    // ---------------- 点击 ----------------

    private void HandleClick()
    {
        // 点到 UI 就不算点地图
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var tile = TileUnderMouse();

        if (tile == null)
        {
            ClearSelection();
            return;
        }

        if (tile.Tower != null)
        {
            SelectTower(tile.Tower);
            return;
        }

        if (SelectedData == null)
        {
            ToastMessage.Show("先在下方选一座塔");
            return;
        }

        TryBuild(tile);
    }

    private BuildTile TileUnderMouse()
    {
        if (cam == null) return null;

        Vector2 world = cam.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.OverlapPoint(world, tileMask);
        if (hit == null) return null;

        // 用 GetComponentInParent：碰撞体挂在地块的子物体上也能找到
        return hit.GetComponentInParent<BuildTile>();
    }

    // ---------------- 选择 ----------------

    public void SelectTowerType(TowerData data)
    {
        DetachSelectedTower();

        SelectedData = data;
        if (infoPanel != null) infoPanel.Hide();
        if (rangeIndicator != null) rangeIndicator.Hide();

        SelectionChanged?.Invoke(data);
    }

    private void SelectTower(Tower tower)
    {
        DetachSelectedTower();

        selectedTower = tower;
        SelectedData = null;
        SelectionChanged?.Invoke(null);

        if (tower != null) tower.Sold += OnTowerSold;
        if (infoPanel != null) infoPanel.Show(tower);
        if (rangeIndicator != null && tower != null)
            rangeIndicator.Show(tower.transform.position, tower.Stats.range, true);
    }

    public void ClearSelection()
    {
        DetachSelectedTower();
        SelectedData = null;
        SelectionChanged?.Invoke(null);

        if (infoPanel != null) infoPanel.Hide();
        if (rangeIndicator != null) rangeIndicator.Hide();
    }

    private void DetachSelectedTower()
    {
        if (selectedTower != null) selectedTower.Sold -= OnTowerSold;
        selectedTower = null;
    }

    private void OnTowerSold(Tower tower)
    {
        // 卖掉之后范围圈和信息面板要跟着消失，否则会残留一个指向已销毁塔的面板
        if (selectedTower == tower) ClearSelection();
    }

    // ---------------- 建造 ----------------

    private void TryBuild(BuildTile tile)
    {
        if (GameManager.Instance == null) return;

        if (towerPrefab == null)
        {
            Debug.LogError("[BuildManager] towerPrefab 未指定，无法建造（看 Console）。");
            ToastMessage.Show("建造配置缺失");
            return;
        }

        int cost = SelectedData.BuildCost;
        if (!GameManager.Instance.TrySpendGold(cost))
        {
            ToastMessage.Show($"金币不足，需要 {cost}");
            return;
        }

        var go = Instantiate(towerPrefab, tile.transform.position, Quaternion.identity, towerParent);
        var tower = go != null ? go.GetComponent<Tower>() : null;

        if (tower == null)
        {
            Debug.LogError("[BuildManager] towerPrefab 上没有 Tower 脚本");
            GameManager.Instance.AddGold(cost);   // 退还，避免吞钱
            if (go != null) Destroy(go);
            return;
        }

        tower.Setup(SelectedData, tile);
        tile.SetTower(tower);
        AudioManager.PlaySfx(Sfx.Build);
    }
}
