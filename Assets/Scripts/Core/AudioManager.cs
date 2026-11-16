using System;
using UnityEngine;

/// <summary>音效编号。新增音效只要在这里加一项，再到 AudioManager 里配 Clip。</summary>
public enum Sfx
{
    Click,       // 按钮点击
    Build,       // 建造
    Upgrade,     // 升级
    Sell,        // 出售
    TowerShoot,  // 塔开火
    EnemyDeath,  // 敌人死亡
    BaseHit,     // 敌人到达终点
    WaveStart,   // 新一波开始
    Win,
    Lose,
}

[Serializable]
public class SfxEntry
{
    public Sfx id;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
}

/// <summary>
/// 全局音频：BGM 循环 + 音效播一声。
/// 放在主菜单场景，DontDestroyOnLoad 一直带着；关卡场景不需要再放。
/// 没放也不会报错——所有静态入口都做了空判断（代价是关卡单独 Play 时是静音的）。
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("背景音乐")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip battleMusic;

    [Header("音效")]
    [SerializeField] private SfxEntry[] sfxList;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        ValidateClips();
        ApplyVolume();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------------- 静态入口（外部只需要这几个） ----------------

    public static void PlaySfx(Sfx id)
    {
        if (Instance != null) Instance.Play(id);
    }

    public static void PlayMusic(AudioClip clip)
    {
        if (Instance != null) Instance.SetMusic(clip);
    }

    // ---------------- 实例方法 ----------------

    public void Play(Sfx id)
    {
        var entry = Find(id);
        if (entry == null || entry.clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(entry.clip, entry.volume * SaveSystem.MasterVolume);
    }

    /// <summary>直接播一个 Clip（比如每个敌人配置自己的死亡音效）。</summary>
    public void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume * SaveSystem.MasterVolume);
    }

    public void SetMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;

        // 注意这里要同时判断 isPlaying：StopMusic 之后 clip 还在，
        // 只比较 clip 会导致「同一首 BGM 再也放不出来」。
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.volume = SaveSystem.MusicVolume * SaveSystem.MasterVolume;
        musicSource.Play();
    }

    public void PlayMenuMusic() => SetMusic(menuMusic);
    public void PlayBattleMusic() => SetMusic(battleMusic);

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    /// <summary>设置面板拖动滑块后调用。</summary>
    public void ApplyVolume()
    {
        if (musicSource == null) return;
        musicSource.volume = SaveSystem.MusicVolume * SaveSystem.MasterVolume;
    }

    private SfxEntry Find(Sfx id)
    {
        if (sfxList == null) return null;

        for (int i = 0; i < sfxList.Length; i++)
        {
            if (sfxList[i] != null && sfxList[i].id == id) return sfxList[i];
        }
        return null;
    }

    private void ValidateClips()
    {
        if (sfxList == null || sfxList.Length == 0)
        {
            Debug.LogWarning("[AudioManager] 音效列表是空的，游戏会没有音效。");
            return;
        }

        foreach (Sfx id in Enum.GetValues(typeof(Sfx)))
        {
            var entry = Find(id);
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"[AudioManager] 音效 {id} 没有配 Clip。");
            }
        }
    }
}
