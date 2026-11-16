using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡选择面板。按钮顺序必须和 LevelConfig.SceneNames 一一对应。
/// 未解锁的关卡按钮会变灰，点了也进不去（避免玩家跳过顺序刷关）。
///
/// 本脚本挂在「关卡选择面板」这个物体本身上，关闭 = 把自己 SetActive(false)。
/// MainMenu 里的 levelSelectPanel 字段要拖同一个物体。
/// </summary>
public class LevelSelectPanel : MonoBehaviour
{
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (levelButtons != null && levelButtons.Length != LevelConfig.Count)
        {
            Debug.LogWarning($"[LevelSelectPanel] 按钮数量 {levelButtons.Length} 与关卡数量 {LevelConfig.Count} 不一致，" +
                             "多出来的按钮会加载到最后一关。", this);
        }

        if (levelButtons != null)
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int index = i;   // 闭包要拷贝一份，否则所有按钮都用最后一个 i
                var btn = levelButtons[i];
                if (btn == null) continue;

                btn.onClick.AddListener(() =>
                {
                    if (!SaveSystem.IsLevelUnlocked(index))
                    {
                        ToastMessage.Show("先通关前面的关卡");
                        return;
                    }

                    AudioManager.PlaySfx(Sfx.Click);
                    LevelConfig.LoadLevel(index);
                });
            }
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                AudioManager.PlaySfx(Sfx.Click);
                gameObject.SetActive(false);
            });
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (levelButtons == null) return;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null) continue;
            levelButtons[i].interactable = SaveSystem.IsLevelUnlocked(i);
        }
    }
}
