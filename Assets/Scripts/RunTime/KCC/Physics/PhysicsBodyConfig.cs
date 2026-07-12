using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Rigidbody 范式配置：实体级 bodyType + N 个复合碰撞体。
/// </summary>
[Serializable]
public class PhysicsBodyConfig
{
    [LabelText("刚体类型")]
    public PhysicsBodyType bodyType = PhysicsBodyType.Kinematic;

    [LabelText("复合碰撞体")]
    public List<PhysicsColliderSetting> colliders = new List<PhysicsColliderSetting>();
}

/// <summary>
/// 单个复合碰撞体 Authoring 数据。
/// </summary>
[Serializable]
public class PhysicsColliderSetting
{
    [LabelText("标识")]
    public string key = "body";

    [LabelText("形状")]
    public PhysicsShapeType shape = PhysicsShapeType.Box;

    [LabelText("本地偏移")]
    public Vector3 localOffset;

    [LabelText("本地欧拉角")]
    public Vector3 localEuler;

    [LabelText("碰撞层")]
    public FPCollisionLayer layer = FPCollisionLayer.Default;

    [LabelText("Trigger")]
    public bool isTrigger;

    [LabelText("盒体半尺寸")]
    [ShowIf("shape", PhysicsShapeType.Box)]
    public Vector3 halfExtents = new Vector3(0.5f, 1f, 0.5f);

    [LabelText("球体半径")]
    [ShowIf("shape", PhysicsShapeType.Sphere)]
    public float radius = 0.5f;

    [LabelText("胶囊半径")]
    [ShowIf("shape", PhysicsShapeType.Capsule)]
    public float capsuleRadius = 0.4f;

    [LabelText("胶囊高度")]
    [ShowIf("shape", PhysicsShapeType.Capsule)]
    public float capsuleHeight = 1.8f;

    [LabelText("胶囊轴 (0=X,1=Y,2=Z)")]
    [ShowIf("shape", PhysicsShapeType.Capsule)]
    public int directionAxis = 1;
}
