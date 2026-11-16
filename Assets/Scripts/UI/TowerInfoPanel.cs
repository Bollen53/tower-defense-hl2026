using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 选中一座塔后弹出的信息面板：数值、升级按钮、出售按钮。
///
/// 结构约定：本脚本挂在「常驻」的空物体上，`panel` 指向一个默认隐藏的子面板。
/// 如果 panel 直接指向自己，本物体被关掉后 Awake/Start 都不会执行，按钮会全部失灵。
/// </summary>
public class TowerInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private TMP_Text upgradeLabel;
    [SerializeField] private TMP_Text sellLabel;

    private Tower current;

    private void Awake()
    {
        if (panel == null || panel == gameObject)
        {
            Debug.LogError("[TowerInfoPanel] panel 必须指向一个子面板（本物体要保持激活），" +
                           "否则塔的信息面板永远弹不出来。", this);
            enabled = false;
            return;
        }

        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgrade);
        if (sellButton != null) sellButton.onClick.AddListener(OnSell);

        panel.SetActive(false);
    }

    public void Show(Tower tower)
    {
        Detach();

        current = tower;
        if (current == null) { Hide(); return; }

        current.Changed += OnTowerChanged;

        panel.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        Detach();
        if (panel != null) panel.SetActive(false);
    }

    private void Detach()
    {
        if (current != null) current.Changed -= OnTowerChanged;
        current = null;
    }

    private void OnTowerChanged(Tower _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (current == null || current.Data == null || panel == null || !panel.activeSelf) return;

        var data = current.Data;
        var stats = current.Stats;

        if (titleText != null) titleText.text = $"{data.displayName}  Lv.{current.Level}";

        if (statsText != null)
        {
            string special = string.Empty;
            if (data.IsSplash) special = $"\n溅射半径：{data.splashRadius:0.0}";
            if (stats.slowMultiplier < 0.999f)
                special += $"\n减速：{(1f - stats.slowMultiplier) * 100f:0}% / {stats.slowDuration:0.0}s";

            statsText.text =
                $"伤害：{stats.damage:0.#}\n" +
                $"射程：{stats.range:0.0}\n" +
                $"攻击间隔：{stats.fireInterval:0.00}s" +
                special;
        }

        if (upgradeLabel != null)
            upgradeLabel.text = current.IsMaxLevel ? "已满级" : $"升级  {current.UpgradeCost}";

        if (upgradeButton != null) upgradeButton.interactable = !current.IsMaxLevel;
        if (sellLabel != null) sellLabel.text = $"出售  +{current.SellValue}";
    }

    private void OnUpgrade()
    {
        if (current == null) return;

        AudioManager.PlaySfx(Sfx.Click);
        current.TryUpgrade();
        Refresh();
    }

    private void OnSell()
    {
        if (current == null) return;

        AudioManager.PlaySfx(Sfx.Click);
        var tower = current;
        Hide();
        tower.Sell();
    }
}
