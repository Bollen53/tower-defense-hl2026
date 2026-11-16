using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Paused,
    Won,
    Lost
}

/// <summary>
/// 单局游戏总控：金币、生命、胜负、暂停、场景跳转。
/// 每个「关卡场景」里放一个空物体挂上它；主菜单场景不需要。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("开局数值")]
    [SerializeField] private int startGold = 160;
    [SerializeField] private int startLives = 10;

    public int Gold { get; private set; }
    public int Lives { get; private set; }
    public GameState State { get; private set; } = GameState.Playing;
    public bool IsPlaying => State == GameState.Playing;
    public bool IsFinished => State == GameState.Won || State == GameState.Lost;

    // 事件：UI 只订阅，不需要每帧去问 GameManager 要数值
    public event Action<int> GoldChanged;
    public event Action<int> LivesChanged;
    public event Action<GameState> StateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Gold = startGold;
        Lives = startLives;
        State = GameState.Playing;
        Time.timeScale = 1f;   // 从暂停菜单重开时，别让上一局的 0 倍速带过来
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------------- 资源 ----------------

    public void AddGold(int amount)
    {
        if (IsFinished) return;   // 结算瞬间的击杀不再加钱，避免「赢了还在涨」

        Gold = Mathf.Max(0, Gold + amount);
        GoldChanged?.Invoke(Gold);
    }

    /// <summary>够钱就扣掉并返回 true，不够返回 false（调用方负责弹提示）。</summary>
    public bool TrySpendGold(int amount)
    {
        if (IsFinished) return false;
        if (Gold < amount) return false;

        Gold -= amount;
        GoldChanged?.Invoke(Gold);
        return true;
    }

    public void LoseLives(int amount)
    {
        if (State != GameState.Playing) return;

        Lives = Mathf.Max(0, Lives - amount);
        LivesChanged?.Invoke(Lives);

        if (Lives <= 0) LoseGame();
    }

    // ---------------- 状态 ----------------

    public void SetPaused(bool paused)
    {
        if (IsFinished) return;

        State = paused ? GameState.Paused : GameState.Playing;
        Time.timeScale = paused ? 0f : 1f;
        StateChanged?.Invoke(State);
    }

    public void WinGame()
    {
        if (IsFinished) return;

        State = GameState.Won;
        Time.timeScale = 1f;

        // 通关即解锁下一关（PlayerPrefs 存在本机）
        int index = LevelConfig.IndexOfScene(SceneManager.GetActiveScene().name);
        if (index >= 0 && index + 1 < LevelConfig.Count) SaveSystem.UnlockLevel(index + 1);

        StateChanged?.Invoke(State);
    }

    public void LoseGame()
    {
        if (IsFinished) return;

        State = GameState.Lost;
        Time.timeScale = 1f;
        StateChanged?.Invoke(State);
    }

    // ---------------- 场景跳转（按钮上直接绑这几个方法） ----------------

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneFader.Instance.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToNextLevel()
    {
        Time.timeScale = 1f;

        int current = LevelConfig.IndexOfScene(SceneManager.GetActiveScene().name);
        int next = current + 1;

        if (current >= 0 && next < LevelConfig.Count) LevelConfig.LoadLevel(next);
        else BackToMenu();
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneFader.Instance.LoadScene(LevelConfig.MenuSceneName);
    }
}
