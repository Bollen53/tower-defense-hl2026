using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 新手引导：一页页文字，点「下一步」翻页。
/// 默认只显示一次（PlayerPrefs 记录），也可以勾 showEveryLevel 每关都显示。
///
/// 结构约定：本脚本挂在「常驻」的空物体上，`panel` 指向默认隐藏的子面板。
/// ⚠️ nextButton 一定要拖：漏了的话面板盖住屏幕、游戏停在 0 倍速，没法退出。
/// </summary>
public class TutorialGuide : MonoBehaviour
{
    [Serializable]
    public class Step
    {
        [TextArea(2, 5)] public string text;
    }

    /// <summary>引导正在显示时，暂停菜单不要去抢 Esc。</summary>
    public static bool IsShowing { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text textLabel;
    [SerializeField] private TMP_Text stepLabel;
    [SerializeField] private Button nextButton;
    [SerializeField] private Step[] steps;

    [Header("行为")]
    [Tooltip("引导显示期间暂停游戏")]
    [SerializeField] private bool pauseGame = true;
    [Tooltip("每关都显示（默认只在第一次进第一关时显示）")]
    [SerializeField] private bool showEveryLevel = false;

    private int index;
    private bool pausedByMe;

    private void Awake()
    {
        if (panel == null || panel == gameObject)
        {
            Debug.LogError("[TutorialGuide] panel 必须指向一个子面板（本物体要保持激活）。", this);
            enabled = false;
            return;
        }

        panel.SetActive(false);
    }

    private void Start()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("[TutorialGuide] steps 是空的，不会显示引导。", this);
            return;
        }

        if (!showEveryLevel && SaveSystem.TutorialDone)
        {
            Debug.Log("[TutorialGuide] 新手引导已看过，跳过（想再看：删掉 PlayerPrefs 的 td_tutorial_done）。");
            return;
        }

        if (nextButton == null)
        {
            Debug.LogError("[TutorialGuide] nextButton 未指定：引导无法翻页，游戏会卡在 0 倍速。已自动关闭引导。", this);
            return;
        }

        index = 0;
        panel.SetActive(true);
        IsShowing = true;

        nextButton.onClick.AddListener(Next);

        if (pauseGame && GameManager.Instance != null)
        {
            GameManager.Instance.SetPaused(true);
            pausedByMe = true;
        }

        AudioManager.PlaySfx(Sfx.Click);
        Render();
    }

    private void OnDisable()
    {
        IsShowing = false;

        // 只恢复「自己暂停的」那一次，别把暂停菜单的暂停也解除了
        if (pausedByMe)
        {
            pausedByMe = false;
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        }
    }

    private void Render()
    {
        if (steps == null || index < 0 || index >= steps.Length) return;
        if (steps[index] == null) return;

        if (textLabel != null) textLabel.text = steps[index].text;
        if (stepLabel != null) stepLabel.text = $"{index + 1} / {steps.Length}";

        if (nextButton != null)
        {
            var label = nextButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = index == steps.Length - 1 ? "开始游戏" : "下一步";
        }
    }

    private void Next()
    {
        AudioManager.PlaySfx(Sfx.Click);
        index++;

        if (index >= steps.Length) Finish();
        else Render();
    }

    private void Finish()
    {
        SaveSystem.TutorialDone = true;

        if (panel != null) panel.SetActive(false);
        IsShowing = false;

        if (pausedByMe)
        {
            pausedByMe = false;
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        }
    }
}
