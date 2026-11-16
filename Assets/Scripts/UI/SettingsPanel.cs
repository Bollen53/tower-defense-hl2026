using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置面板：主音量 / 音乐音量。值存 PlayerPrefs，下次启动还在。
/// 主菜单和暂停菜单可以共用同一个面板。
///
/// 结构约定：本脚本挂在「常驻」的空物体上，`panel` 指向默认隐藏的子面板。
/// 拖动滑块时只改内存值，关闭面板时才真正写盘（每帧 PlayerPrefs.Save 会卡）。
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TMP_Text masterValueText;
    [SerializeField] private TMP_Text musicValueText;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (panel == null || panel == gameObject)
        {
            Debug.LogError("[SettingsPanel] panel 必须指向一个子面板（本物体要保持激活）。", this);
            enabled = false;
            return;
        }

        if (masterSlider != null)
        {
            masterSlider.minValue = 0f;
            masterSlider.maxValue = 1f;
            masterSlider.value = SaveSystem.MasterVolume;
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.value = SaveSystem.MusicVolume;
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        if (closeButton != null) closeButton.onClick.AddListener(Hide);

        RefreshLabels();
        panel.SetActive(false);
    }

    public void Show()
    {
        if (panel == null) return;

        panel.SetActive(true);
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(SaveSystem.MasterVolume);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(SaveSystem.MusicVolume);
        RefreshLabels();
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);

        SaveSystem.Flush();   // 关闭时统一写盘
    }

    private void OnMasterChanged(float value)
    {
        SaveSystem.MasterVolume = value;
        if (AudioManager.Instance != null) AudioManager.Instance.ApplyVolume();
        RefreshLabels();
    }

    private void OnMusicChanged(float value)
    {
        SaveSystem.MusicVolume = value;
        if (AudioManager.Instance != null) AudioManager.Instance.ApplyVolume();
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (masterValueText != null)
            masterValueText.text = $"{Mathf.RoundToInt(SaveSystem.MasterVolume * 100f)}%";
        if (musicValueText != null)
            musicValueText.text = $"{Mathf.RoundToInt(SaveSystem.MusicVolume * 100f)}%";
    }
}
