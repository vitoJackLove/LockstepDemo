using UnityEngine;
using PrimitiveDetection;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// 盒型几何体
/// </summary>
public class BoxPrimitive : BasePrimitive
{
    /// <summary>
    /// 中心点
    /// </summary>
    private fp3 _boxCenter;

    public fp3 boxCenter => this._boxCenter;

    /// <summary>
    /// 旋转角
    /// </summary>
    private fpquaternion _boxQuaternion;

    /// <summary>
    /// 盒子大小,z为0时变为平面Quad
    /// </summary>
    private fp3 size;

    private fp3 halfSize;

    public fp3 HalfSize => halfSize;

    // 顶点本地坐标
    public fp3[] LocalVertices = new fp3[8];

    // 顶点世界坐标
    public fp3[] Vertices = new fp3[8];

    public override void OnInit(PrimitiveInfo info, out bool result)
    {
        base.OnInit(info, out result);
        this._boxCenter = info.Center;
        this._boxQuaternion = info.Quaternion;

        if (info.BoxSize.x <= 0 || info.BoxSize.y <= 0 || info.BoxSize.z <= 0)
        {
            result = false;
        }

        this.size = fpmath.max(info.BoxSize, fp3.zero);
        halfSize.x = size.x / 2;
        halfSize.y = size.y / 2;
        halfSize.z = size.z / 2;
        LocalVertices = new fp3[8];
        Vertices = new fp3[8];
        GetLocalVertices();
        Transform.SetOrientationAndPos(this._boxQuaternion, this._boxCenter);
        GetVertices();
        PrimitiveType = PrimitiveEnum.BoxPrimitive;
    }

    public override void UpdateSelf(PrimitiveInfo info)
    {
        this._boxCenter = info.Center;
        this._boxQuaternion = info.Quaternion;
        this.size = fpmath.max(info.BoxSize, fp3.zero);
        GetLocalVertices();
        Transform.SetOrientationAndPos(this._boxQuaternion, this._boxCenter);
        GetVertices();
        PrimitiveType = PrimitiveEnum.BoxPrimitive;
        halfSize.x = size.x / 2;
        halfSize.y = size.y / 2;
        halfSize.z = size.z / 2;
        base.UpdateSelf(info);
    }

    /// <summary>
    /// 获取顶点本地坐标
    /// </summary>
    void GetLocalVertices()
    {
        for (int i = 0; i < 8; i++)
        {
            LocalVertices[i].x = PrimitiveExtension.BoxVMul(i, 0, HalfSize.x);
            LocalVertices[i].y = PrimitiveExtension.BoxVMul(i, 1, HalfSize.y);
            LocalVertices[i].z = PrimitiveExtension.BoxVMul(i, 2, HalfSize.z);
        }
    }

    /// <summary>
    /// 获取顶点世界坐标
    /// </summary>
    private void GetVertices()
    {
        for (int i = 0; i < LocalVertices.Length; i++)
        {
            Vertices[i] = Transform * LocalVertices[i];
        }
    }
#if UNITY_EDITOR

    public override void PrimitiveDebug(Color color)
    {
        base.PrimitiveDebug(color);
        
        /*if (!this.ShowInfo.isDrawShow)
        {
            return;
        }*/
        
        DrawDebugTools.DrawBox(this._boxCenter.ToVector3(), this._boxQuaternion, this.size.ToVector3(), color, this.leftTime);
    }

    public override void OnDrawGizmos()
    {
        // base.OnDrawGizmos(color);
        if (!this.ShowInfo.isDrawShow)
        {
            return;
        }

        Gizmos.color = this.ShowInfo.drawColor;

        fp3[] vertexPosArray = new fp3[8];

        for (int i = 0; i < 8; i++)
        {
            fp3 one = new fp3(1, 1, 1);

            vertexPosArray[i] = PrimitiveExtension.VMul(Vertices[i], one);
        }

        Gizmos.DrawLine(fpmath1.Fp3ToVector3(vertexPosArray[0]), fpmath1.Fp3ToVector3(vertexPosArray[1]));
        Gizmos.DrawLine(fpmath1.Fp3ToVector3(vertexPosArray[0]), fpmath1.Fp3ToVector3(vertexPosArray[3]));
        Gizmos.DrawLine(fpmath1.Fp3ToVector3(vertexPosArray[0]), fpmath1.Fp3ToVector3(vertexPosArray[4]));
        Gizmos.DrawLine(fpmath1.Fp3ToVector3(vertexPosArray[1]), fpmath1.Fp3ToVector3(vertexPosArray[2]));
        Gizmos.DrawLine(fpmath1.Fp3ToVector3(vertexPosArray[1]), fpmath1.Fp3ToVector3(vertexPosArray[5]));
        Gizmos.DrawLine(fpmath1.Fp3ToVector3(vertexPosArray[2]), fpmath1.Fp3ToVector3(vertexPosArray[3]));
        Gizmos.DrawLine(fpmath1.Fp3ToVector3(vertexPosArray[2]), fpmath1.Fp3ToVector3(vertexPosArray[6]));
        Gizmos.DrawLine(fpmath1.Fp3ToVector3(vertexPosArray[3]), fpmath1.Fp3ToVector3(vertexPosArray[7]));
        Gizmos.DrawLine(fpmath1.Fp3ToVector3(vertexPosArray[4]), fpmath1.Fp3ToVector3(vertexPosArray[5]));
        Gizmos.DrawLine(fpmath1.Fp3ToVector3(vertexPosArray[4]), fpmath1.Fp3ToVector3(vertexPosArray[7]));
        Gizmos.DrawLine(fpmath1.Fp3ToVector3(vertexPosArray[5]), fpmath1.Fp3ToVector3(vertexPosArray[6]));
        Gizmos.DrawLine(fpmath1.Fp3ToVector3(vertexPosArray[6]), fpmath1.Fp3ToVector3(vertexPosArray[7]));
    }
#endif
    public override bool InternalCheckPrimitive()
    {
        if (size.x <= 0 || size.y <= 0 || size.z <= 0)
        {
            //Log.Error("盒型几何形状的大小不符合规范。");
            return false;
        }

        return true;
    }

    public override void OnDispose()
    {
        FPoolHelper.Release<BoxPrimitive>(this);
    }

    public override void Clear()
    {
        base.Clear();
        LocalVertices = null;
        Vertices = null;
    }
}