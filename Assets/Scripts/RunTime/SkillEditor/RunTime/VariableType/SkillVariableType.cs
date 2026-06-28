using System;
using UnityEngine;

/// <summary>
/// 变量类型
/// </summary>
public static class SkillVariableType
{
    /// <summary>
    /// 技能编辑器的变量类型
    /// </summary>
    public static Type[] SkillTimeLineVariableType =
    {
        typeof(int),
        typeof(float),
        typeof(bool),
        typeof(string),
        typeof(Vector3),
        typeof(Vector2),
        typeof(AnimationCurve),
        typeof(CommandType),
        typeof(ParamType),
    };
}
