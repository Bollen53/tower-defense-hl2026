using UnityEngine;

/// <summary>
/// 【临时测试脚本】没有 UI 时用键盘选塔。
/// 按 1 / 2 / 3 选塔型，然后点地图上的地块建造。
///
/// 正式版用 BuildButton（UI 按钮）调用 BuildManager.SelectTowerType，不需要这个脚本。
/// </summary>
public class DevBuildKeys : MonoBehaviour
{
    [SerializeField] private BuildManager buildManager;
    [SerializeField] private TowerData arrow;
    [SerializeField] private TowerData cannon;
    [SerializeField] private TowerData frost;

    private void Update()
    {
        if (buildManager == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) Select(arrow, "箭塔");
        if (Input.GetKeyDown(KeyCode.Alpha2)) Select(cannon, "炮塔");
        if (Input.GetKeyDown(KeyCode.Alpha3)) Select(frost, "冰塔");

        // 右键取消选择（和正式版行为一致）
        if (Input.GetMouseButtonDown(1)) buildManager.ClearSelection();
    }

    private void Select(TowerData data, string label)
    {
        if (data == null)
        {
            Debug.LogWarning($"[DevBuildKeys] {label} 的数据没拖进来");
            return;
        }

        buildManager.SelectTowerType(data);
        Debug.Log($"[DevBuildKeys] 已选择 {label}，点地块建造（右键取消）");
    }
}
