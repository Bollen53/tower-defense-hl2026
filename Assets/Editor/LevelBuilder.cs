using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 关卡搭建工具（编辑器扩展）
///
/// ⚠️ 必须放在 Assets/Editor/ 目录下。
/// 菜单：Tools > TD > 关卡搭建工具
///
/// 两个功能：
///   ① 按矩形批量铺地块（建造区）
///   ② 按坐标文本生成路径（Path 物体 + P0..Pn + WaypointPath 自动绑定）
/// </summary>
public class LevelBuilder : EditorWindow
{
    // ---- 建造区 ----
    private GameObject tilePrefab;
    private Transform tileParent;
    private int startX = -5;
    private int startY = -2;
    private int width = 4;
    private int height = 6;

    // ---- 路径 ----
    private string pathName = "Path_A";
    private string pathText = "-9,4\n-6,4\n-6,-3\n-1,-3\n-1,4\n4,4\n4,-3\n9,-3";

    private Vector2 scroll;

    [MenuItem("Tools/TD/关卡搭建工具")]
    private static void Open()
    {
        var win = GetWindow<LevelBuilder>("关卡搭建工具");
        win.minSize = new Vector2(360, 520);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        // ---------------- 建造区 ----------------
        GUILayout.Label("① 建造区（矩形铺地块）", EditorStyles.boldLabel);
        GUILayout.Space(4);

        tilePrefab = (GameObject)EditorGUILayout.ObjectField("地块预制体", tilePrefab, typeof(GameObject), false);
        tileParent = (Transform)EditorGUILayout.ObjectField("父物体（留空自动建 Tiles）", tileParent, typeof(Transform), true);

        GUILayout.Space(4);
        startX = EditorGUILayout.IntField("起点 X", startX);
        startY = EditorGUILayout.IntField("起点 Y", startY);
        width = Mathf.Max(1, EditorGUILayout.IntField("宽（格）", width));
        height = Mathf.Max(1, EditorGUILayout.IntField("高（格）", height));

        GUILayout.Label($"→ 将生成 {width}×{height} = {width * height} 个地块", EditorStyles.miniLabel);

        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("铺地块", GUILayout.Height(26))) GenerateTiles();
        GUI.backgroundColor = Color.white;

        GUILayout.Space(14);

        // ---------------- 路径 ----------------
        GUILayout.Label("② 路径（每行一个坐标，如  -9,4 ）", EditorStyles.boldLabel);
        GUILayout.Space(4);

        pathName = EditorGUILayout.TextField("路径名", pathName);
        pathText = EditorGUILayout.TextArea(pathText, GUILayout.Height(130));
        GUILayout.Label("支持 (x,y) 括号写法；生成后自动挂 WaypointPath 并绑定", EditorStyles.miniLabel);

        GUI.backgroundColor = new Color(1f, 0.8f, 0.5f);
        if (GUILayout.Button("生成路径", GUILayout.Height(26))) GeneratePath();
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);
        GUILayout.Label("所有生成都支持 Ctrl+Z 撤销", EditorStyles.miniLabel);

        EditorGUILayout.EndScrollView();
    }

    // ---------------- 铺地块 ----------------

    private void GenerateTiles()
    {
        if (tilePrefab == null)
        {
            EditorUtility.DisplayDialog("缺少预制体", "先把地块预制体拖进「地块预制体」槽。", "好");
            return;
        }

        Transform container = tileParent;
        if (container == null)
        {
            var root = new GameObject("Tiles");
            Undo.RegisterCreatedObjectUndo(root, "创建地块容器");
            container = root.transform;
        }

        int count = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, container);
                go.transform.position = new Vector3(startX + x, startY + y, 0f);
                go.name = $"Tile_{startX + x}_{startY + y}";
                Undo.RegisterCreatedObjectUndo(go, "生成地块");
                count++;
            }
        }

        Debug.Log($"[LevelBuilder] 生成 {count} 个地块：X {startX}~{startX + width - 1}，Y {startY}~{startY + height - 1}");
    }

    // ---------------- 生成路径 ----------------

    private void GeneratePath()
    {
        var points = new List<Vector2>();

        foreach (var raw in pathText.Split('\n'))
        {
            string line = raw.Trim().Replace("(", "").Replace(")", "");
            if (line.Length == 0) continue;

            var parts = line.Split(',');
            if (parts.Length < 2) continue;

            if (float.TryParse(parts[0].Trim(), out float x) &&
                float.TryParse(parts[1].Trim(), out float y))
            {
                points.Add(new Vector2(x, y));
            }
        }

        if (points.Count < 2)
        {
            EditorUtility.DisplayDialog("坐标不够", "至少要有 2 个有效坐标，每行一个，如：-9,4", "好");
            return;
        }

        var root = new GameObject(string.IsNullOrEmpty(pathName) ? "Path" : pathName);
        Undo.RegisterCreatedObjectUndo(root, "生成路径");

        var waypointPath = root.AddComponent<WaypointPath>();

        // 用 SerializedObject 直接给私有字段赋值，省得手动拖
        var so = new SerializedObject(waypointPath);
        var arrayProp = so.FindProperty("points");
        arrayProp.arraySize = points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            var p = new GameObject($"P{i}");
            p.transform.SetParent(root.transform);
            p.transform.position = new Vector3(points[i].x, points[i].y, 0f);

            Undo.RegisterCreatedObjectUndo(p, "生成路径点");
            arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = p.transform;
        }

        so.ApplyModifiedProperties();

        // 计算总长，方便判断设计是否合格
        float total = 0f;
        for (int i = 1; i < points.Count; i++) total += Vector2.Distance(points[i - 1], points[i]);

        Debug.Log($"[LevelBuilder] 生成路径 {points.Count} 个点，总长 {total:0.0} 单位（步兵 1.5 速需 {total / 1.5f:0.0} 秒走完）");
    }
}
