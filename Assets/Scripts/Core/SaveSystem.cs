using UnityEngine;

/// <summary>
/// 本地存档：关卡解锁进度、新手引导是否看过、音量设置。
/// 用 PlayerPrefs 就够，比赛要求的是「能存能读」，不是非得上 JSON。
///
/// 注意：音量的 setter **不立即写盘**（拖滑块时每帧写盘会卡），
/// 面板关闭或退出时统一调一次 Flush()。
/// </summary>
public static class SaveSystem
{
    private const string KeyUnlocked = "td_unlocked_level"; // 已解锁的最大关卡下标
    private const string KeyTutorial = "td_tutorial_done";
    private const string KeyMasterVolume = "td_volume_master";
    private const string KeyMusicVolume = "td_volume_music";

    // ---------- 关卡进度 ----------

    public static int UnlockedLevel
    {
        get => PlayerPrefs.GetInt(KeyUnlocked, 0);
        private set
        {
            PlayerPrefs.SetInt(KeyUnlocked, Mathf.Max(0, value));
            PlayerPrefs.Save();   // 解锁是低频操作，直接落盘
        }
    }

    public static bool IsLevelUnlocked(int index) => index <= UnlockedLevel;

    public static void UnlockLevel(int index)
    {
        if (index > UnlockedLevel) UnlockedLevel = index;
    }

    // ---------- 新手引导 ----------

    public static bool TutorialDone
    {
        get => PlayerPrefs.GetInt(KeyTutorial, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KeyTutorial, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    // ---------- 音量（0~1） ----------

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(KeyMasterVolume, 0.8f);
        set => PlayerPrefs.SetFloat(KeyMasterVolume, Mathf.Clamp01(value));
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(KeyMusicVolume, 0.5f);
        set => PlayerPrefs.SetFloat(KeyMusicVolume, Mathf.Clamp01(value));
    }

    /// <summary>把内存里的设置真正写盘。面板关闭时调用一次即可。</summary>
    public static void Flush()
    {
        PlayerPrefs.Save();
    }
}
