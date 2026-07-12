using System;
using Sirenix.OdinInspector;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 物理碰撞对实体 Transform 各轴的影响开关（仅约束物理/Motor 回写，不影响寻路、行为树等逻辑层）。
/// 勾选表示该轴可被物理碰撞/Motor 解算改写；未勾选表示该轴免疫物理侧位移/旋转，由逻辑层独占。
/// </summary>
[Serializable]
[InlineProperty(LabelWidth = 56)]
public class PhysicsMotionInfluence
{
    [HorizontalGroup("Pos", Title = "位置")]
    [LabelText("X")]
    public bool positionX = true;

    [HorizontalGroup("Pos")]
    [LabelText("Y")]
    public bool positionY = true;

    [HorizontalGroup("Pos")]
    [LabelText("Z")]
    public bool positionZ = true;

    [HorizontalGroup("Rot", Title = "旋转")]
    [LabelText("X")]
    public bool rotationX;

    [HorizontalGroup("Rot")]
    [LabelText("Y")]
    public bool rotationY = true;

    [HorizontalGroup("Rot")]
    [LabelText("Z")]
    public bool rotationZ;

    /// <summary>Character Controller 默认：位置三轴由 Motor 校正，绕 Y 旋转由 Motor 驱动。</summary>
    public static PhysicsMotionInfluence CreateCharacterControllerDefault()
    {
        return new PhysicsMotionInfluence
        {
            positionX = true,
            positionY = true,
            positionZ = true,
            rotationX = false,
            rotationY = true,
            rotationZ = false,
        };
    }

    /// <summary>Rigidbody 默认：位置三轴可被物理贴地/推挤影响；旋转不由物理改写（逻辑层可自由转向）。</summary>
    public static PhysicsMotionInfluence CreateRigidbodyDefault()
    {
        return new PhysicsMotionInfluence
        {
            positionX = true,
            positionY = true,
            positionZ = true,
            rotationX = false,
            rotationY = false,
            rotationZ = false,
        };
    }

    /// <summary>按轴混合世界位置：勾选轴取物理结果，未勾选轴保留当前 Transform。</summary>
    public fp3 BlendPosition(fp3 current, fp3 physics)
    {
        return new fp3(
            positionX ? physics.x : current.x,
            positionY ? physics.y : current.y,
            positionZ ? physics.z : current.z);
    }

    /// <summary>按轴混合物理层旋转（欧拉 XYZ）：勾选轴取物理/Motor 结果，未勾选轴保留当前 Transform。</summary>
    public fpquaternion BlendRotation(fpquaternion current, fpquaternion physicsRotation)
    {
        fp3 curEuler = current.ToEulerAngles();
        fp3 phyEuler = physicsRotation.ToEulerAngles();
        fp3 blended = new fp3(
            rotationX ? phyEuler.x : curEuler.x,
            rotationY ? phyEuler.y : curEuler.y,
            rotationZ ? phyEuler.z : curEuler.z);
        return fpmath1.EulerXYZ(blended);
    }
}
