using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Character Controller 范式配置（单一胶囊，对齐 Unity CC Inspector）。
/// </summary>
[Serializable]
public class CharacterControllerSettings
{
    [LabelText("半径")]
    public float radius = 0.5f;

    [LabelText("高度")]
    public float height = 2f;

    [LabelText("中心偏移")]
    public Vector3 center;

    [LabelText("Step Offset")]
    public float stepOffset = 0.3f;

    [LabelText("Slope Limit")]
    public float slopeLimit = 45f;

    [LabelText("Skin Width")]
    public float skinWidth = 0.08f;

    [LabelText("碰撞层")]
    public FPCollisionLayer layer = FPCollisionLayer.Hero;
}
