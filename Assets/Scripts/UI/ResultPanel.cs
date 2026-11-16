using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 胜利 / 失败结算面板。监听 GameManager 的状态变化，不主动轮询。
///
/// 结构约定：本脚本挂在「常驻」的空物体上，`panel` 指向默认隐藏的子面板。
/// </summary>
public class ResultPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private GameManager gm;

    private void Awake()
    {
        if (panel == null || panel == gameObject)
        {
            Debug.LogError("[ResultPanel] panel 必须指向一个子面板（本物体要保持激活），" +
                           "否则结算面板永远弹不出来。", this);
            enabled = false;
            return;
        }

        panel.SetActive(false);

        if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
        if (menuButton != null) menuButton.onClick.AddListener(OnMenu);
        if (nextButton != null) nextButton.onClick.AddListener(OnNext);
    }

    private void Start()
    {
        gm = GameManager.Instance;

        if (gm == null)
        {
            Debug.LogError("[ResultPanel] 场景里找不到 GameManager，结算面板不会弹出。", this);
            return;
        }

        gm.StateChanged += OnStateChanged;
    }

    private void OnDestroy()
    {
        if (gm != null) gm.StateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.Won) ShowWin();
        else if (state == GameState.Lost) ShowLose();
    }

    private void ShowWin()
    {
        if (panel != null) panel.SetActive(true);
        if (titleText != null) titleText.text = "防守成功！";
        if (bodyText != null) bodyText.text = "所有波次都已清除，干得漂亮。";

        AudioManager.PlaySfx(Sfx.Win);

        int index = LevelConfig.IndexOfScene(SceneManager.GetActiveScene().name);
        bool hasNext = LevelConfig.HasNext(index);
        if (nextButton != null) nextButton.gameObject.SetActive(hasNext);
    }

    private void ShowLose()
    {
        if (panel != null) panel.SetActive(true);
        if (titleText != null) titleText.text = "防线失守";
        if (bodyText != null) bodyText.text = "敌人到达了终点。调整布防，再来一次。";

        AudioManager.PlaySfx(Sfx.Lose);
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    private void OnRestart()
    {
        AudioManager.PlaySfx(Sfx.Click);
        if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
    }

    private void OnMenu()
    {
        AudioManager.PlaySfx(Sfx.Click);
        if (GameManager.Instance != null) GameManager.Instance.BackToMenu();
    }

    private void OnNext()
    {
        AudioManager.PlaySfx(Sfx.Click);
        if (GameManager.Instance != null) GameManager.Instance.GoToNextLevel();
    }
}
