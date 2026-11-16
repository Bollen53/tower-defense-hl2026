using TMPro;
using UnityEngine;

/// <summary>
/// 关卡内 HUD：金币、生命、波次进度、布防倒计时。
/// 只负责「把数值显示出来」，规则判断都在 GameManager / WaveManager 里。
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("文本")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text countdownText;

    [Header("引用")]
    [SerializeField] private WaveManager waveManager;
    [Tooltip("右上角暂停按钮（可选，Esc 也能暂停）")]
    [SerializeField] private UnityEngine.UI.Button pauseButton;
    [SerializeField] private PauseMenu pauseMenu;

    private GameManager gm;

    private void Start()
    {
        // 所有 Awake 都执行完了，这里能安全拿到单例
        gm = GameManager.Instance;

        if (pauseButton != null)
        {
            if (pauseMenu != null)
            {
                pauseButton.onClick.AddListener(() =>
                {
                    AudioManager.PlaySfx(Sfx.Click);
                    pauseMenu.Pause();
                });
            }
            else
            {
                Debug.LogWarning("[HUD] pauseButton 拖了但 pauseMenu 没拖，暂停按钮不会生效。", this);
            }
        }

        if (gm != null)
        {
            gm.GoldChanged += OnGoldChanged;
            gm.LivesChanged += OnLivesChanged;

            OnGoldChanged(gm.Gold);
            OnLivesChanged(gm.Lives);
        }
        else
        {
            Debug.LogError("[HUD] 场景里找不到 GameManager，金币/生命不会刷新。", this);
        }

        if (waveManager != null)
        {
            waveManager.WaveChanged += OnWaveChanged;
            waveManager.CountdownChanged += OnCountdownChanged;
            waveManager.LevelCleared += OnLevelCleared;

            OnWaveChanged(0, waveManager.TotalWaves);
            OnCountdownChanged(0f);
        }
        else
        {
            Debug.LogWarning("[HUD] waveManager 未指定，波次进度不会显示。", this);
        }
    }

    private void OnDestroy()
    {
        // 用缓存的引用退订，不要依赖 Instance 在销毁时还活着
        if (gm != null)
        {
            gm.GoldChanged -= OnGoldChanged;
            gm.LivesChanged -= OnLivesChanged;
        }

        if (waveManager != null)
        {
            waveManager.WaveChanged -= OnWaveChanged;
            waveManager.CountdownChanged -= OnCountdownChanged;
            waveManager.LevelCleared -= OnLevelCleared;
        }
    }

    private void OnGoldChanged(int gold)
    {
        if (goldText != null) goldText.text = gold.ToString();
    }

    private void OnLivesChanged(int lives)
    {
        if (livesText != null) livesText.text = lives.ToString();
    }

    private void OnWaveChanged(int current, int total)
    {
        if (waveText == null) return;

        waveText.text = current <= 0 ? $"准备中  0/{total}" : $"波次  {current}/{total}";
    }

    private void OnCountdownChanged(float seconds)
    {
        if (countdownText == null) return;

        if (seconds <= 0.05f)
        {
            countdownText.text = string.Empty;
            return;
        }

        countdownText.text = $"下一波  {Mathf.CeilToInt(seconds)}";
    }

    private void OnLevelCleared()
    {
        if (countdownText != null) countdownText.text = string.Empty;
    }
}
