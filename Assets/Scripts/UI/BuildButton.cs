using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 底部建造栏的一个按钮。挂在一个 Button 上，把 TowerData 拖进来即可。
/// </summary>
public class BuildButton : MonoBehaviour
{
    [SerializeField] private TowerData data;
    [SerializeField] private BuildManager buildManager;

    [Header("UI 引用（可选）")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private Button button;

    [Header("颜色")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f, 0.45f);
    [SerializeField] private Color unaffordableColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

    private GameManager gm;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();

        if (data == null)
        {
            Debug.LogError("[BuildButton] 没有配置 TowerData", this);
            if (button != null) button.interactable = false;
            enabled = false;
            return;
        }

        // 漏拖 buildManager 的话，这个按钮点了会完全没反应，所以自动找一次
        if (buildManager == null) buildManager = FindFirstObjectByType<BuildManager>();
        if (buildManager == null)
        {
            Debug.LogError("[BuildButton] 场景里找不到 BuildManager，这个按钮不会生效。", this);
            if (button != null) button.interactable = false;
            return;
        }

        if (nameText != null) nameText.text = data.displayName;
        if (costText != null) costText.text = data.BuildCost.ToString();
        if (icon != null) icon.sprite = data.icon;

        if (button != null) button.onClick.AddListener(OnClick);
        buildManager.SelectionChanged += OnSelectionChanged;
    }

    private void Start()
    {
        gm = GameManager.Instance;

        if (gm != null)
        {
            gm.GoldChanged += OnGoldChanged;
            OnGoldChanged(gm.Gold);
        }
    }

    private void OnDestroy()
    {
        if (buildManager != null) buildManager.SelectionChanged -= OnSelectionChanged;
        if (gm != null) gm.GoldChanged -= OnGoldChanged;
    }

    private void OnClick()
    {
        if (buildManager == null) return;

        AudioManager.PlaySfx(Sfx.Click);
        buildManager.SelectTowerType(data);
    }

    private void OnSelectionChanged(TowerData selected)
    {
        Refresh(selected == data, gm != null ? gm.Gold : 0);
    }

    private void OnGoldChanged(int gold)
    {
        bool selected = buildManager != null && buildManager.SelectedData == data;
        Refresh(selected, gold);
    }

    private void Refresh(bool selected, int gold)
    {
        if (data == null) return;

        bool affordable = gold >= data.BuildCost;

        if (background != null)
        {
            background.color = !affordable ? unaffordableColor : (selected ? selectedColor : normalColor);
        }

        if (button != null) button.interactable = affordable;
        if (costText != null) costText.color = affordable ? Color.white : new Color(1f, 0.4f, 0.4f);
    }
}
