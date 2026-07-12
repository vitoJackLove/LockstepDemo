using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 运行时 KCC 物理碰撞核可视化。由 <see cref="BaseWorld.RefreshKccPhysicsDebugDraw"/> 在 Transform 同步后提交线框，
/// 与 <see cref="DrawDebugTools"/> 同帧渲染，避免 LateUpdate 提交导致的 1 帧相位差。
/// </summary>
public sealed class KccPhysicsDebugView : MonoBehaviour
{
    private BaseWorld _world;

    public static bool ForceEnabled = true;

    public void Bind(BaseWorld world)
    {
        _world = world;
    }

    /// <summary>
    /// 提交本帧 KCC 碰撞核线框到 DrawDebugTools。须在 WorldSystem.Update（含 Transform 同步）之后、DrawDebugTools.Update 之前调用。
    /// </summary>
    public void Draw()
    {
        if (!ForceEnabled || DrawDebugTools.Instance == null)
        {
            return;
        }

        if (DrawDebugTools.Instance.m_DDTSettings != null &&
            !DrawDebugTools.Instance.m_DDTSettings.m_EnableKccPhysicsVisualization)
        {
            return;
        }

        KccPhysicsDebugDrawer.DrawWorldColliders();

        FPKinematicCharacterSystem kccSystem = _world?.GetSystem<FPKinematicCharacterSystem>();
        KccPhysicsDebugDrawer.DrawCharacterMotors(kccSystem);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!ForceEnabled || DrawDebugTools.Instance != null)
        {
            return;
        }

        IReadOnlyList<IFPCollider> colliders = FPCollisionWorld.Instance.Colliders;
        Gizmos.color = KccPhysicsDebugDrawer.PhysicsColor;
        for (int i = 0; i < colliders.Count; i++)
        {
            DrawColliderGizmo(colliders[i]);
        }
    }

    private static void DrawColliderGizmo(IFPCollider collider)
    {
        if (collider == null)
        {
            return;
        }

        switch (collider.ShapeType)
        {
            case FPShapeType.Box:
                FPBoxShape box = collider.GetBoxShape();
                Gizmos.matrix = Matrix4x4.TRS(
                    fpmath1.Fp3ToVector3(box.Center),
                    box.Rotation,
                    Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, fpmath1.Fp3ToVector3(box.HalfExtents * (fp)2));
                Gizmos.matrix = Matrix4x4.identity;
                break;
            case FPShapeType.Sphere:
                FPSphereShape sphere = collider.GetSphereShape();
                Gizmos.DrawWireSphere(fpmath1.Fp3ToVector3(sphere.Center), (float)sphere.Radius);
                break;
        }
    }
#endif
}
