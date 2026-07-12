#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 在 Scene 视图与工厂窗口预览 PhysicsBodyConfig / CharacterController 碰撞体线框（仅 Editor）。
/// </summary>
[InitializeOnLoad]
public static class PhysicsBodyConfigGizmoDrawer
{
    private static readonly Color PhysicsGizmoColor = new Color(0.2f, 0.85f, 1f, 0.95f);

    /// <summary>
    /// 在 Scene 视图绘制 Rigidbody 范式复合碰撞体线框。
    /// </summary>
    /// <param name="config">物理体配置。</param>
    /// <param name="entityMatrix">实体世界变换矩阵。</param>
    public static void DrawPhysicsBodyGizmos(PhysicsBodyConfig config, Matrix4x4 entityMatrix)
    {
        if (config?.colliders == null)
        {
            return;
        }

        Color previousColor = Handles.color;
        Handles.color = PhysicsGizmoColor;

        for (int i = 0; i < config.colliders.Count; i++)
        {
            PhysicsColliderSetting collider = config.colliders[i];
            Matrix4x4 local = Matrix4x4.TRS(collider.localOffset, Quaternion.Euler(collider.localEuler), Vector3.one);
            Matrix4x4 world = entityMatrix * local;
            using (new Handles.DrawingScope(world))
            {
                DrawColliderShapeWireframe(collider);
            }
        }

        Handles.color = previousColor;
    }

    /// <summary>
    /// 在 Scene 视图绘制 CharacterController 范式胶囊线框。
    /// </summary>
    /// <param name="settings">Character Controller 配置。</param>
    /// <param name="entityMatrix">实体世界变换矩阵。</param>
    public static void DrawCharacterControllerGizmos(CharacterControllerSettings settings, Matrix4x4 entityMatrix)
    {
        if (settings == null)
        {
            return;
        }

        Color previousColor = Handles.color;
        Handles.color = PhysicsGizmoColor;

        Matrix4x4 world = entityMatrix * Matrix4x4.TRS(settings.center, Quaternion.identity, Vector3.one);
        using (new Handles.DrawingScope(world))
        {
            DrawCapsuleWireframe(settings.radius, settings.height);
        }

        Handles.color = previousColor;
    }

    /// <summary>
    /// 在工厂窗口 PreviewRenderUtility 区域绘制 Rigidbody 复合碰撞体线框叠加层。
    /// </summary>
    public static void DrawPhysicsBodyPreviewOverlay(
        Rect rect,
        Camera camera,
        PhysicsBodyConfig config,
        Matrix4x4 entityMatrix)
    {
        if (Event.current.type != EventType.Repaint || config?.colliders == null || camera == null)
        {
            return;
        }

        Handles.BeginGUI();
        Color previousColor = Handles.color;
        Handles.color = PhysicsGizmoColor;

        for (int i = 0; i < config.colliders.Count; i++)
        {
            PhysicsColliderSetting collider = config.colliders[i];
            Matrix4x4 local = Matrix4x4.TRS(collider.localOffset, Quaternion.Euler(collider.localEuler), Vector3.one);
            Matrix4x4 world = entityMatrix * local;
            DrawColliderShapeOverlay(rect, camera, collider, world);
        }

        Handles.color = previousColor;
        Handles.EndGUI();
    }

    /// <summary>
    /// 在工厂窗口 PreviewRenderUtility 区域绘制 CharacterController 胶囊线框叠加层。
    /// </summary>
    public static void DrawCharacterControllerPreviewOverlay(
        Rect rect,
        Camera camera,
        CharacterControllerSettings settings,
        Matrix4x4 entityMatrix)
    {
        if (Event.current.type != EventType.Repaint || settings == null || camera == null)
        {
            return;
        }

        Handles.BeginGUI();
        Color previousColor = Handles.color;
        Handles.color = PhysicsGizmoColor;

        Matrix4x4 world = entityMatrix * Matrix4x4.TRS(settings.center, Quaternion.identity, Vector3.one);
        DrawCapsuleOverlay(rect, camera, world, settings.radius, settings.height);

        Handles.color = previousColor;
        Handles.EndGUI();
    }

    private static void DrawColliderShapeWireframe(PhysicsColliderSetting collider)
    {
        switch (collider.shape)
        {
            case PhysicsShapeType.Box:
                Handles.DrawWireCube(Vector3.zero, collider.halfExtents * 2f);
                break;
            case PhysicsShapeType.Sphere:
                Handles.DrawWireDisc(Vector3.zero, Vector3.up, collider.radius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.forward, collider.radius);
                Handles.DrawWireDisc(Vector3.zero, Vector3.right, collider.radius);
                break;
            case PhysicsShapeType.Capsule:
                DrawCapsuleWireframe(collider.capsuleRadius, collider.capsuleHeight);
                break;
        }
    }

    private static void DrawCapsuleWireframe(float radius, float height)
    {
        float clampedRadius = Mathf.Max(0.01f, radius);
        float clampedHeight = Mathf.Max(clampedRadius * 2f, height);
        float halfLine = Mathf.Max(0f, clampedHeight * 0.5f - clampedRadius);
        Vector3 top = Vector3.up * halfLine;
        Vector3 bottom = Vector3.down * halfLine;

        Handles.DrawWireDisc(top, Vector3.up, clampedRadius);
        Handles.DrawWireDisc(bottom, Vector3.up, clampedRadius);
        Handles.DrawLine(top + Vector3.left * clampedRadius, bottom + Vector3.left * clampedRadius);
        Handles.DrawLine(top + Vector3.right * clampedRadius, bottom + Vector3.right * clampedRadius);
        Handles.DrawLine(top + Vector3.back * clampedRadius, bottom + Vector3.back * clampedRadius);
        Handles.DrawLine(top + Vector3.forward * clampedRadius, bottom + Vector3.forward * clampedRadius);
    }

    private static void DrawColliderShapeOverlay(
        Rect rect,
        Camera camera,
        PhysicsColliderSetting collider,
        Matrix4x4 worldMatrix)
    {
        switch (collider.shape)
        {
            case PhysicsShapeType.Box:
                DrawBoxOverlay(rect, camera, worldMatrix, collider.halfExtents * 2f);
                break;
            case PhysicsShapeType.Sphere:
                DrawSphereOverlay(rect, camera, worldMatrix, collider.radius);
                break;
            case PhysicsShapeType.Capsule:
                DrawCapsuleOverlay(rect, camera, worldMatrix, collider.capsuleRadius, collider.capsuleHeight);
                break;
        }
    }

    private static void DrawSphereOverlay(Rect rect, Camera camera, Matrix4x4 worldMatrix, float radius)
    {
        float clampedRadius = Mathf.Max(0.01f, radius);
        DrawCircleOverlay(rect, camera, worldMatrix, Vector3.zero, clampedRadius, Vector3.right, Vector3.up);
        DrawCircleOverlay(rect, camera, worldMatrix, Vector3.zero, clampedRadius, Vector3.right, Vector3.forward);
        DrawCircleOverlay(rect, camera, worldMatrix, Vector3.zero, clampedRadius, Vector3.forward, Vector3.up);
    }

    private static void DrawCapsuleOverlay(
        Rect rect,
        Camera camera,
        Matrix4x4 worldMatrix,
        float radius,
        float height)
    {
        float clampedRadius = Mathf.Max(0.01f, radius);
        float clampedHeight = Mathf.Max(clampedRadius * 2f, height);
        float halfLine = Mathf.Max(0f, clampedHeight * 0.5f - clampedRadius);
        Vector3 top = Vector3.up * halfLine;
        Vector3 bottom = Vector3.down * halfLine;

        DrawCircleOverlay(rect, camera, worldMatrix, top, clampedRadius, Vector3.right, Vector3.forward);
        DrawCircleOverlay(rect, camera, worldMatrix, bottom, clampedRadius, Vector3.right, Vector3.forward);
        DrawArcOverlay(rect, camera, worldMatrix, top, clampedRadius, Vector3.right, Vector3.up, 0f, 180f);
        DrawArcOverlay(rect, camera, worldMatrix, bottom, clampedRadius, Vector3.right, Vector3.up, 180f, 360f);
        DrawArcOverlay(rect, camera, worldMatrix, top, clampedRadius, Vector3.forward, Vector3.up, 0f, 180f);
        DrawArcOverlay(rect, camera, worldMatrix, bottom, clampedRadius, Vector3.forward, Vector3.up, 180f, 360f);
        DrawLineOverlay(rect, camera, worldMatrix, top + Vector3.left * clampedRadius, bottom + Vector3.left * clampedRadius);
        DrawLineOverlay(rect, camera, worldMatrix, top + Vector3.right * clampedRadius, bottom + Vector3.right * clampedRadius);
        DrawLineOverlay(rect, camera, worldMatrix, top + Vector3.back * clampedRadius, bottom + Vector3.back * clampedRadius);
        DrawLineOverlay(rect, camera, worldMatrix, top + Vector3.forward * clampedRadius, bottom + Vector3.forward * clampedRadius);
    }

    private static void DrawBoxOverlay(Rect rect, Camera camera, Matrix4x4 worldMatrix, Vector3 size)
    {
        Vector3 half = new Vector3(
            Mathf.Max(0.01f, size.x) * 0.5f,
            Mathf.Max(0.01f, size.y) * 0.5f,
            Mathf.Max(0.01f, size.z) * 0.5f);
        Vector3[] corners =
        {
            new Vector3(-half.x, -half.y, -half.z),
            new Vector3(-half.x, -half.y, half.z),
            new Vector3(-half.x, half.y, -half.z),
            new Vector3(-half.x, half.y, half.z),
            new Vector3(half.x, -half.y, -half.z),
            new Vector3(half.x, -half.y, half.z),
            new Vector3(half.x, half.y, -half.z),
            new Vector3(half.x, half.y, half.z),
        };

        DrawLineOverlay(rect, camera, worldMatrix, corners[0], corners[1]);
        DrawLineOverlay(rect, camera, worldMatrix, corners[0], corners[2]);
        DrawLineOverlay(rect, camera, worldMatrix, corners[0], corners[4]);
        DrawLineOverlay(rect, camera, worldMatrix, corners[3], corners[1]);
        DrawLineOverlay(rect, camera, worldMatrix, corners[3], corners[2]);
        DrawLineOverlay(rect, camera, worldMatrix, corners[3], corners[7]);
        DrawLineOverlay(rect, camera, worldMatrix, corners[5], corners[1]);
        DrawLineOverlay(rect, camera, worldMatrix, corners[5], corners[4]);
        DrawLineOverlay(rect, camera, worldMatrix, corners[5], corners[7]);
        DrawLineOverlay(rect, camera, worldMatrix, corners[6], corners[2]);
        DrawLineOverlay(rect, camera, worldMatrix, corners[6], corners[4]);
        DrawLineOverlay(rect, camera, worldMatrix, corners[6], corners[7]);
    }

    private static void DrawCircleOverlay(
        Rect rect,
        Camera camera,
        Matrix4x4 worldMatrix,
        Vector3 center,
        float radius,
        Vector3 axisA,
        Vector3 axisB)
    {
        const int SegmentCount = 48;
        Vector3[] points = new Vector3[SegmentCount + 1];
        for (int i = 0; i <= SegmentCount; i++)
        {
            float angle = i / (float)SegmentCount * Mathf.PI * 2f;
            Vector3 localPoint = center + axisA * (Mathf.Cos(angle) * radius) + axisB * (Mathf.Sin(angle) * radius);
            points[i] = WorldToPreviewPoint(rect, camera, worldMatrix, localPoint);
        }

        Handles.DrawAAPolyLine(2.5f, points);
    }

    private static void DrawArcOverlay(
        Rect rect,
        Camera camera,
        Matrix4x4 worldMatrix,
        Vector3 center,
        float radius,
        Vector3 axisA,
        Vector3 axisB,
        float startDegrees,
        float endDegrees)
    {
        const int SegmentCount = 24;
        Vector3[] points = new Vector3[SegmentCount + 1];
        for (int i = 0; i <= SegmentCount; i++)
        {
            float angle = Mathf.Lerp(startDegrees, endDegrees, i / (float)SegmentCount) * Mathf.Deg2Rad;
            Vector3 localPoint = center + axisA * (Mathf.Cos(angle) * radius) + axisB * (Mathf.Sin(angle) * radius);
            points[i] = WorldToPreviewPoint(rect, camera, worldMatrix, localPoint);
        }

        Handles.DrawAAPolyLine(2.5f, points);
    }

    private static void DrawLineOverlay(
        Rect rect,
        Camera camera,
        Matrix4x4 worldMatrix,
        Vector3 from,
        Vector3 to)
    {
        Handles.DrawAAPolyLine(
            2.5f,
            WorldToPreviewPoint(rect, camera, worldMatrix, from),
            WorldToPreviewPoint(rect, camera, worldMatrix, to));
    }

    private static Vector3 WorldToPreviewPoint(Rect rect, Camera camera, Matrix4x4 worldMatrix, Vector3 localPoint)
    {
        Vector3 worldPoint = worldMatrix.MultiplyPoint3x4(localPoint);
        Vector3 viewport = camera.WorldToViewportPoint(worldPoint);
        return new Vector3(
            rect.x + viewport.x * rect.width,
            rect.y + (1f - viewport.y) * rect.height,
            0f);
    }
}
#endif
