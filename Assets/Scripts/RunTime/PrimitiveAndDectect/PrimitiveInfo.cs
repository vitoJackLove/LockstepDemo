using Unity.Mathematics.FixedPoint;
using UnityEngine;

public struct PrimitiveInfo
{
    /// <summary>
    /// 表现层
    /// </summary>
    public PrimitiveShowInfo showInfo;
    /// <summary>
    /// 几何体类型
    /// </summary>
    public PrimitiveEnum Type;

    /// <summary>
    /// 几何体中心
    /// </summary>
    public fp3 Center;

    /// <summary>
    /// 几何体转角
    /// </summary>
    public fpquaternion Quaternion { get; set; }

    /// <summary>
    /// 盒型几何体大小
    /// </summary>
    public fp3 BoxSize;

    /// <summary>
    /// 扇形扫掠角、环形扫掠角[0,360]
    /// </summary>
    public fp Angle;

    /// <summary>
    /// 圆形，扇形半径参数、环形半径参数、胶囊体半径参数
    /// </summary>
    public fp Radius;

    /// <summary>
    /// 环形内径参数
    /// </summary>
    public fp InternalRadius;

    /// <summary>
    /// 胶囊体高度参数
    /// </summary>
    public fp Height;
}

/// <summary>
/// 几何体表现层
/// </summary>
public struct PrimitiveShowInfo
{
    /// <summary>
    /// 是否绘制几何体
    /// </summary>
    public bool isDrawShow;

    /// <summary>
    /// 绘制颜色
    /// </summary>
    public Color drawColor;
}

