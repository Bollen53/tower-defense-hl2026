using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>一波里的一种敌人：多少只、间隔多久出、从哪个出怪口出。</summary>
[Serializable]
public class SpawnGroup
{
    public EnemyData enemy;
    [Min(1)] public int count = 5;
    [Tooltip("同一组内两只之间隔多少秒")]
    public float interval = 1f;
    [Tooltip("这一组比「本组开始出怪」晚多少秒")]
    public float startDelay = 0f;

    [Tooltip("从第几条路径出生：0 = 第一个出怪口，1 = 第二个…（WaveManager 里 paths 的顺序）")]
    [Min(0)] public int pathIndex = 0;
}

/// <summary>一波敌人。</summary>
[Serializable]
public class Wave
{
    [Tooltip("这波打完到下一波之间的布防时间")]
    public float prepTime = 8f;

    [Tooltip("勾上：按顺序出（上一组全部出完，才开始下一组）。\n" +
             "不勾：所有组同时开始，用各组的 Start Delay 错开。")]
    public bool sequentialGroups = false;

    public List<SpawnGroup> groups = new List<SpawnGroup>();
    [TextArea] public string note;
}

/// <summary>
/// 一个关卡的全部波次配置。右键 Create > TD > Level Waves 建资产。
/// 每关至少 5 波（赛题硬性要求），难度靠敌人组合和出场顺序堆。
/// </summary>
[CreateAssetMenu(fileName = "Waves_", menuName = "TD/Level Waves", order = 2)]
public class LevelWaves : ScriptableObject
{
    [Tooltip("开局给玩家多久布防")]
    public float firstPrepTime = 15f;

    public List<Wave> waves = new List<Wave>();

    public int WaveCount => waves == null ? 0 : waves.Count;

    /// <summary>编辑器里点右键可以快速检查配置是否合法。</summary>
    private void OnValidate()
    {
        if (waves == null) return;

        for (int i = 0; i < waves.Count; i++)
        {
            var w = waves[i];
            if (w == null || w.groups == null) continue;

            for (int j = 0; j < w.groups.Count; j++)
            {
                var g = w.groups[j];
                if (g == null) continue;
                if (g.enemy == null) Debug.LogWarning($"[{name}] 第 {i + 1} 波第 {j + 1} 组的敌人配置为空");
                if (g.count <= 0) Debug.LogWarning($"[{name}] 第 {i + 1} 波第 {j + 1} 组的数量必须 >= 1");
                if (g.pathIndex < 0) Debug.LogWarning($"[{name}] 第 {i + 1} 波第 {j + 1} 组的 pathIndex 不能为负");
            }

            // 顺序模式下，后组的 startDelay 会和「等前组出完」叠加，容易误判，提示一下
            if (w.sequentialGroups)
            {
                for (int j = 1; j < w.groups.Count; j++)
                {
                    if (w.groups[j] != null && w.groups[j].startDelay > 0f)
                    {
                        Debug.Log($"[{name}] 第 {i + 1} 波是顺序模式，第 {j + 1} 组还有 {w.groups[j].startDelay} 秒延迟（会叠加在「等上一组」之后）");
                    }
                }
            }
        }
    }
}
