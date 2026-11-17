using System;
using UnityEngine;

/// <summary>
/// 【学习版】游戏总控：只管金币、生命、胜负。
/// 和正式版 GameManager 的区别：
///   - 没有暂停、没有场景跳转、没有存档（那些和"理解核心"无关）
///   - 用 OnGUI 直接在屏幕上画数值，省掉搭 UI 的步骤
///
/// 它自己不显示任何东西（OnGUI 只是临时调试用），
/// 只负责「改数值 + 广播事件」，谁关心谁订阅。
/// </summary>
public class TestGameManager : MonoBehaviour
{
    // 单例：全局唯一，谁都能通过 TestGameManager.Instance 拿到
    public static TestGameManager Instance { get; private set; }

    [Header("开局数值")]
    [SerializeField] private int startGold = 100;
    [SerializeField] private int startLives = 5;

    public int Gold { get; private set; }
    public int Lives { get; private set; }
    public bool IsOver { get; private set; }

    // 事件：数值变了就"喊一嗓子"，UI / 音效 / 成就系统都可以订阅
    public event Action<int> GoldChanged;
    public event Action<int> LivesChanged;

    private void Awake()
    {
        // 单例的标准写法：已经有一个就自杀，保证全局只有一个
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Gold = startGold;
        Lives = startLives;

        Debug.Log($"[TestGame] 开局 金币={Gold} 生命={Lives}");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------------- 金币 ----------------

    public void AddGold(int amount)
    {
        if (IsOver) return;              // 结算后不再变动，避免"赢了还在涨钱"

        Gold += amount;
        GoldChanged?.Invoke(Gold);       // ?. 表示"没人订阅就不喊"
    }

    /// <summary>够钱就扣掉返回 true，不够返回 false。调用方负责提示玩家。</summary>
    public bool TrySpendGold(int amount)
    {
        if (IsOver) return false;
        if (Gold < amount) return false;

        Gold -= amount;
        GoldChanged?.Invoke(Gold);
        return true;
    }

    // ---------------- 生命 ----------------

    public void EnemyEscaped(int damage)
    {
        if (IsOver) return;

        Lives = Mathf.Max(0, Lives - damage);
        LivesChanged?.Invoke(Lives);

        Debug.Log($"[TestGame] 漏怪！扣 {damage} 点，剩余 {Lives}");

        if (Lives <= 0)
        {
            IsOver = true;
            Debug.Log("[TestGame] 生命归零 → 游戏失败");
        }
    }

    // ---------------- 临时调试显示 ----------------
    // 没有 Canvas 也能看到数值，搭 UI 之前先用这个
    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 22 };
        GUI.color = Color.white;

        GUI.Label(new Rect(20, 15, 300, 30), $"金币: {Gold}", style);
        GUI.Label(new Rect(20, 45, 300, 30), $"生命: {Lives}", style);

        if (IsOver)
        {
            GUI.Label(new Rect(Screen.width / 2f - 60, Screen.height / 2f - 15, 200, 40), "GAME OVER", style);
        }
    }
}
