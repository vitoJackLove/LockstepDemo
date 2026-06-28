using Unity.Mathematics.FixedPoint;
using UnityEngine;

public interface IFTransform 
{
    /// <summary>
    /// 位置
    /// </summary>
    fp3 Position { get; set; }

    /// <summary>
    /// 缩放
    /// </summary>
    fp3 LocalScale { get; set; }

    /// <summary>
    /// 旋转
    /// </summary>
    fpquaternion Rotation { get; set; }

    /// <summary>
    /// 欧拉角
    /// </summary>
    fp3 EulerAngles { get; set; }
}
