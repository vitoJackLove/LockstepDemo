using System.Collections.Generic;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 使用 DrawDebugTools 在 Game 视图绘制 KCC 物理碰撞核与 CharacterController 胶囊。
/// </summary>
public static class KccPhysicsDebugDrawer
{
    public static readonly Color PhysicsColor = new Color(0.2f, 0.85f, 1f, 0.95f);
    public static readonly Color TriggerColor = new Color(1f, 0.65f, 0.1f, 0.85f);

    private const float DrawLifeTime = 0f;
    private const int SphereSegments = 12;

    public static void DrawCollider(IFPCollider collider)
    {
        if (collider == null)
        {
            return;
        }

        Color color = collider.IsTrigger ? TriggerColor : PhysicsColor;

        switch (collider.ShapeType)
        {
            case FPShapeType.Box:
                DrawBox(collider.GetBoxShape(), color);
                break;
            case FPShapeType.Sphere:
                DrawSphere(collider.GetSphereShape(), color);
                break;
            case FPShapeType.Capsule:
                DrawCapsule(collider.Position, collider.GetCapsuleShape(), color);
                break;
        }
    }

    public static void DrawMotor(FPKinematicCharacterMotor motor)
    {
        if (motor == null)
        {
            return;
        }

        fp3 center = motor.TransientPosition +
                     motor.TransientRotation * new fp3((fp)0, motor.CapsuleYOffset, (fp)0);

        DrawDebugTools.DrawCapsule(
            fpmath1.Fp3ToVector3(center),
            (float)motor.CapsuleHeight * 0.5f,
            (float)motor.CapsuleRadius,
            motor.TransientRotation,
            PhysicsColor,
            DrawLifeTime);
    }

    public static void DrawWorldColliders()
    {
        IReadOnlyList<IFPCollider> colliders = FPCollisionWorld.Instance.Colliders;
        for (int i = 0; i < colliders.Count; i++)
        {
            DrawCollider(colliders[i]);
        }
    }

    public static void DrawCharacterMotors(FPKinematicCharacterSystem system)
    {
        if (system == null)
        {
            return;
        }

        IReadOnlyList<FPKinematicCharacterMotor> motors = system.CharacterMotors;
        for (int i = 0; i < motors.Count; i++)
        {
            DrawMotor(motors[i]);
        }
    }

    private static void DrawBox(FPBoxShape box, Color color)
    {
        if (box.HalfExtents.x <= (fp)0 && box.HalfExtents.y <= (fp)0 && box.HalfExtents.z <= (fp)0)
        {
            return;
        }

        DrawDebugTools.DrawBox(
            fpmath1.Fp3ToVector3(box.Center),
            box.Rotation,
            fpmath1.Fp3ToVector3(box.HalfExtents * (fp)2),
            color,
            DrawLifeTime);
    }

    private static void DrawSphere(FPSphereShape sphere, Color color)
    {
        if (sphere.Radius <= (fp)0)
        {
            return;
        }

        DrawDebugTools.DrawSphere(
            fpmath1.Fp3ToVector3(sphere.Center),
            (float)sphere.Radius,
            SphereSegments,
            color,
            DrawLifeTime);
    }

    private static void DrawCapsule(fp3 worldCenter, FPCapsuleShape shape, Color color)
    {
        if (shape.Radius <= (fp)0 || shape.Height <= (fp)0)
        {
            return;
        }

        DrawDebugTools.DrawCapsule(
            fpmath1.Fp3ToVector3(worldCenter),
            (float)shape.Height * 0.5f,
            (float)shape.Radius,
            shape.Rotation,
            color,
            DrawLifeTime);
    }
}
