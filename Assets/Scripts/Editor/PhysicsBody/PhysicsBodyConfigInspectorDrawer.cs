#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 配置中心 / 快速创建窗口共用的 Rigidbody 物理体 IMGUI 绘制。
/// </summary>
public static class PhysicsBodyConfigInspectorDrawer
{
    public static void Draw(PhysicsBodyConfig body)
    {
        if (body == null)
        {
            EditorGUILayout.HelpBox("PhysicsBodyConfig 为空。", MessageType.Warning);
            return;
        }

        if (body.colliders == null)
        {
            body.colliders = new List<PhysicsColliderSetting>();
        }

        body.bodyType = (PhysicsBodyType)EditorGUILayout.EnumPopup("刚体类型", body.bodyType);

        if (body.bodyType != PhysicsBodyType.Static)
        {
            body.useGravity = EditorGUILayout.Toggle("启用重力", body.useGravity);
            if (body.useGravity)
            {
                body.gravity = EditorGUILayout.FloatField("重力加速度 Y", body.gravity);
            }

            EditorGUILayout.Space(4f);
            if (body.collisionInfluence == null)
            {
                body.collisionInfluence = PhysicsMotionInfluence.CreateRigidbodyDefault();
            }

            PhysicsMotionInfluenceInspectorDrawer.Draw(body.collisionInfluence);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("KCC 碰撞核", EditorStyles.boldLabel);

        int newCount = EditorGUILayout.IntField("数量", body.colliders.Count);
        if (newCount < 0)
        {
            newCount = 0;
        }

        while (body.colliders.Count < newCount)
        {
            body.colliders.Add(CreateDefaultCollider(body.colliders.Count));
        }

        while (body.colliders.Count > newCount)
        {
            body.colliders.RemoveAt(body.colliders.Count - 1);
        }

        for (int i = 0; i < body.colliders.Count; i++)
        {
            DrawCollider(body.colliders, i);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("添加碰撞核", GUILayout.Width(120f)))
        {
            body.colliders.Add(CreateDefaultCollider(body.colliders.Count));
        }

        EditorGUILayout.EndHorizontal();
    }

    private static PhysicsColliderSetting CreateDefaultCollider(int index)
    {
        return new PhysicsColliderSetting
        {
            key = index == 0 ? "body" : $"body_{index}",
            shape = PhysicsShapeType.Box,
            halfExtents = new Vector3(0.5f, 1f, 0.5f),
            layer = FPCollisionLayer.Monster,
        };
    }

    private static void DrawCollider(List<PhysicsColliderSetting> colliders, int index)
    {
        PhysicsColliderSetting collider = colliders[index];
        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField($"碰撞核 {index + 1}", EditorStyles.boldLabel);
            collider.key = EditorGUILayout.TextField("标识", collider.key);
            collider.shape = (PhysicsShapeType)EditorGUILayout.EnumPopup("形状", collider.shape);
            collider.localOffset = EditorGUILayout.Vector3Field("中心偏移", collider.localOffset);
            collider.localEuler = EditorGUILayout.Vector3Field("本地欧拉角", collider.localEuler);
            collider.layer = (FPCollisionLayer)EditorGUILayout.EnumPopup("碰撞层", collider.layer);
            collider.isTrigger = EditorGUILayout.Toggle("Trigger", collider.isTrigger);

            switch (collider.shape)
            {
                case PhysicsShapeType.Box:
                    collider.halfExtents = EditorGUILayout.Vector3Field("盒体半尺寸", collider.halfExtents);
                    break;
                case PhysicsShapeType.Sphere:
                    collider.radius = EditorGUILayout.FloatField("球体半径", collider.radius);
                    break;
                case PhysicsShapeType.Capsule:
                    collider.capsuleRadius = EditorGUILayout.FloatField("胶囊半径", collider.capsuleRadius);
                    collider.capsuleHeight = EditorGUILayout.FloatField("胶囊高度", collider.capsuleHeight);
                    collider.directionAxis = EditorGUILayout.IntPopup(
                        "胶囊轴",
                        collider.directionAxis,
                        new[] { "X", "Y", "Z" },
                        new[] { 0, 1, 2 });
                    break;
            }

            if (GUILayout.Button("删除此碰撞核", GUILayout.Width(120f)))
            {
                colliders.RemoveAt(index);
                GUIUtility.ExitGUI();
            }
        }
    }
}
#endif
