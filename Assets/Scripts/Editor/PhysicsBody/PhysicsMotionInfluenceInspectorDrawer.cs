#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 配置中心 / 快速创建窗口共用的「物理碰撞影响」IMGUI 绘制。
/// </summary>
public static class PhysicsMotionInfluenceInspectorDrawer
{
    public static void Draw(PhysicsMotionInfluence influence)
    {
        if (influence == null)
        {
            EditorGUILayout.HelpBox("PhysicsMotionInfluence 为空。", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("物理碰撞影响", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "勾选表示该轴可被物理/Motor 改写；未勾选轴免疫物理侧变化，寻路与行为树等逻辑层仍可正常驱动。",
            MessageType.None);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("位置", GUILayout.Width(40f));
        influence.positionX = EditorGUILayout.ToggleLeft("X", influence.positionX, GUILayout.Width(36f));
        influence.positionY = EditorGUILayout.ToggleLeft("Y", influence.positionY, GUILayout.Width(36f));
        influence.positionZ = EditorGUILayout.ToggleLeft("Z", influence.positionZ, GUILayout.Width(36f));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("旋转", GUILayout.Width(40f));
        influence.rotationX = EditorGUILayout.ToggleLeft("X", influence.rotationX, GUILayout.Width(36f));
        influence.rotationY = EditorGUILayout.ToggleLeft("Y", influence.rotationY, GUILayout.Width(36f));
        influence.rotationZ = EditorGUILayout.ToggleLeft("Z", influence.rotationZ, GUILayout.Width(36f));
        EditorGUILayout.EndHorizontal();
    }
}
#endif
