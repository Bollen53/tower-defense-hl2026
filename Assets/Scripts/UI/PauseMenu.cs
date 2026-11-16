using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停菜单：Esc 呼出，继续 / 重开 / 设置 / 回主菜单。
/// 暂停靠 Time.timeScale = 0，所有 Update 里的 deltaTime 都会变 0，游戏自然停住。
///
/// 结构约定：本脚本挂在「常驻」的空物体上，`panel` 指向默认隐藏的子面板。
/// ⚠️ 如果把 panel 指向自己，物体被关掉后 Update 不再执行，Esc 就永久失效了。
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private SettingsPanel settingsPanel;

    private void Awake()
    {
        if (panel == null || panel == gameObject)
        {
            Debug.LogError("[PauseMenu] panel 必须指向一个子面板（本物体要保持激活）。" +
                           "否则物体被关掉后 Esc 永久失效。", this);
            enabled = false;
            return;
        }

        panel.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (menuButton != null) menuButton.onClick.AddListener(BackToMenu);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
    }

    private void Update()
    {
        if (TutorialGuide.IsShowing) return;      // 引导期间不许用 Esc 抢控制权
        if (GameManager.Instance == null) return;
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        var state = GameManager.Instance.State;
        if (state == GameState.Won || state == GameState.Lost) return;

        if (panel.activeSelf) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (panel == null) return;
        if (GameManager.Instance != null && GameManager.Instance.IsFinished) return;   // 结算后不再允许暂停

        panel.SetActive(true);
        if (settingsPanel != null) settingsPanel.Hide();
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(true);
        AudioManager.PlaySfx(Sfx.Click);
    }

    public void Resume()
    {
        if (panel != null) panel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        AudioManager.PlaySfx(Sfx.Click);
    }

    private void Restart()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        AudioManager.PlaySfx(Sfx.Click);
        if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
    }

    private void BackToMenu()
    {
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        AudioManager.PlaySfx(Sfx.Click);
        if (GameManager.Instance != null) GameManager.Instance.BackToMenu();
    }

    private void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.Show();
        AudioManager.PlaySfx(Sfx.Click);
    }
}
