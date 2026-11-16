using UnityEngine;

/// <summary>
/// 关卡列表的唯一数据源。场景名字必须和 Build Settings 里的一模一样。
/// 加新关卡：往两个数组里各加一项即可。
/// </summary>
public static class LevelConfig
{
    public const string MenuSceneName = "MainMenu";

    public static readonly string[] SceneNames =
    {
        "Level_01",
        "Level_02",
        "Level_03",
    };

    public static readonly string[] DisplayNames =
    {
        "第一关 · 山谷小径",
        "第二关 · 蜿蜒长廊",
        "第三关 · 熔岩要塞",
    };

    public static readonly string[] Descriptions =
    {
        "教学关：一条直路，学会建造和升级。",
        "蜿蜒长廊：路径更长、敌人更密，考验射程覆盖。",
        "熔岩要塞：重甲与治疗敌人成群出现，需要混合阵容。",
    };

    public static int Count => SceneNames.Length;

    /// <summary>
    /// 找不到返回 -1（不是 0）。
    /// 返回 0 会让「测试场景」被当成第一关，一打赢就错误解锁第二关。
    /// </summary>
    public static int IndexOfScene(string sceneName)
    {
        for (int i = 0; i < SceneNames.Length; i++)
        {
            if (SceneNames[i] == sceneName) return i;
        }

        Debug.LogWarning($"[LevelConfig] 场景 '{sceneName}' 不在 SceneNames 列表里；" +
                         "检查 Build Settings 里的场景名是否完全一致（含大小写）。");
        return -1;
    }

    public static bool HasNext(int index)
    {
        return index >= 0 && index + 1 < SceneNames.Length;
    }

    public static void LoadLevel(int index)
    {
        index = Mathf.Clamp(index, 0, SceneNames.Length - 1);
        SceneFader.Instance.LoadScene(SceneNames[index]);
    }
}
