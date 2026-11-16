using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单：开始游戏 / 选择关卡 / 设置 / 退出。
/// 放在 MainMenu 场景，同时把 AudioManager、SettingsPanel 也放在这个场景。
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button levelsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [SerializeField] private GameObject levelSelectPanel;
    [SerializeField] private SettingsPanel settingsPanel;

    private void Start()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();

        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (levelsButton != null) levelsButton.onClick.AddListener(ToggleLevelSelect);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);

        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
    }

    private void StartGame()
    {
        AudioManager.PlaySfx(Sfx.Click);

        // 直接进「已解锁的最高关」，回头刷老关可以点关卡选择
        int index = Mathf.Clamp(SaveSystem.UnlockedLevel, 0, LevelConfig.Count - 1);
        LevelConfig.LoadLevel(index);
    }

    private void ToggleLevelSelect()
    {
        AudioManager.PlaySfx(Sfx.Click);
        if (levelSelectPanel == null) return;
        levelSelectPanel.SetActive(!levelSelectPanel.activeSelf);
    }

    private void OpenSettings()
    {
        AudioManager.PlaySfx(Sfx.Click);
        if (settingsPanel != null) settingsPanel.Show();
    }

    private void Quit()
    {
        AudioManager.PlaySfx(Sfx.Click);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
