using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

[Serializable]
public class VolumeData : IPool
{
    [LabelText("受击盒中心偏移")] public fp3 offset;

    [LabelText("受击盒角度偏移")] public fp3 eulerOffset;

    [LabelText("受击盒形状")] public PrimitiveEnum primitiveEnum = PrimitiveEnum.SpherePrimitive;

    [LabelText("受击盒参数")]
    [Tooltip("方形:x,y,z\n 球:半径\n 胶囊:半径,长度\n 扇形：半径,扇形角度\n 环形：内圆半径,半径,角度")]
    public List<fp> primitiveParam = new List<fp>();

    [LabelText("是否显示受击盒")]
    public bool isShow;

    [LabelText("受击盒颜色")]
    public Color color = Color.white;

    public static VolumeData Create(HitColliderEditorSetting setting)
    {
        VolumeData data = FPoolHelper.Get<VolumeData>();
        
        data.offset = fpmath1.Vector3ToFp3(setting.offset);
        
        data.eulerOffset = fpmath1.Vector3ToFp3(setting.eulerOffset);
        
        data.primitiveEnum = setting.primitiveEnum;
        
        data.isShow = setting.isShow;
        
        data.color = setting.color;

        if (data.primitiveEnum == PrimitiveEnum.BoxPrimitive)
        {
            data.primitiveParam = new List<fp>() { (fp)setting.x, (fp)setting.y, (fp)setting.z };
        }
        else if (data.primitiveEnum == PrimitiveEnum.SpherePrimitive)
        {
            data.primitiveParam = new List<fp>() { (fp)setting.spRadius };
        }
        else if (data.primitiveEnum == PrimitiveEnum.CapsulePrimitive)
        {
            data.primitiveParam = new List<fp>() { (fp)setting.capRadius, (fp)setting.height };
        }
        else
        {
            data.primitiveEnum = PrimitiveEnum.NONE;
            data.primitiveParam = new List<fp>();
        }

        return data;
    }

    public void OnDispose()
    {
        FPoolHelper.Release<VolumeData>(this);
    }

    public void Clear()
    {
        primitiveParam.Clear();
    }
}