using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using PrimitiveDetection;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

/// <summary>
/// 配置多个受击盒的文件
/// </summary>
public class ColliderEntityView : EntityView , IColliderEntityView
{
    [LabelText("绑点数据")]
    [DictionaryDrawerSettings(KeyLabel = "骨骼Key", ValueLabel = "骨骼数据")]
    public Dictionary<string, BoneData> boneList = new Dictionary<string, BoneData>();
    
    [LabelText("受击盒数据")] [DictionaryDrawerSettings(KeyLabel = "受击盒Key", ValueLabel = "受击盒数据")]
    
    public Dictionary<string, HitColliderEditorSetting> settingDict = new ();

    private Dictionary<string, BasePrimitive> _primitives = new ();

    public Dictionary<string, HitColliderEditorSetting> Primitives => settingDict;

    /// <summary>
    /// 根据类型获取实体内部坐标信息
    /// </summary>
    /// <returns></returns>
    public Transform GetBoneRoot(string boneKey)
    {
        if (boneList.ContainsKey(boneKey))
        {
            return this.boneList[boneKey].bone;
        }
            
        return null;
    }
    
    private void ReUpdate()
    {
        var dic = settingDict.GetEnumerator();

        while (dic.MoveNext())
        {
            if (dic.Current.Value == null)
            {
                continue;
            }

            VolumeData volumeData = VolumeData.Create(dic.Current.Value);

            if (!_primitives.TryGetValue(dic.Current.Key, out BasePrimitive pri))
            {
                if (dic.Current.Value.primitiveEnum == PrimitiveEnum.NONE)
                {
                    volumeData?.OnDispose();
                    continue;
                }

                _primitives.Add(dic.Current.Key, GetPrimitive(volumeData, out bool res));
                volumeData?.OnDispose();
                continue;
            }

            if (dic.Current.Value.primitiveEnum != pri.PrimitiveType)
            {
                pri?.OnDispose();

                if (dic.Current.Value.primitiveEnum == PrimitiveEnum.NONE)
                {
                    _primitives.Remove(dic.Current.Key);
                    volumeData?.OnDispose();
                    continue;
                }

                _primitives.Add(dic.Current.Key, GetPrimitive(volumeData, out bool res));
                volumeData?.OnDispose();
                continue;
            }

            PrimitiveInfo info = new PrimitiveInfo();

            if (!GetInfo(ref info, volumeData))
            {
                volumeData?.OnDispose();
                continue;
            }

            pri.OnInit(info, out bool result);
            volumeData?.OnDispose();
        }

        dic.Dispose();
    }
    
    private BasePrimitive GetPrimitive(VolumeData volumeData, out bool res)
    {
        res = false;

        if (!CheckPrimitiveType(volumeData.primitiveEnum, out Type p1Type))
        {
            return null;
        }

        BasePrimitive one = (BasePrimitive)Activator.CreateInstance(p1Type);

        PrimitiveInfo info = new PrimitiveInfo();

        if (!GetInfo(ref info, volumeData))
        {
            return null;
        }

        one.OnInit(info, out bool result);

        if (!result)
        {
            Debug.LogWarning($"检查{one.PrimitiveType}出错，检查配置参数信息。");
        }

        res = true;

        return one;
    }

    private bool GetInfo(ref PrimitiveInfo info, VolumeData volumeData)
    {
        if (!CheckPrimitiveType(volumeData.primitiveEnum, out Type p1Type))
        {
            return false;
        }

        info = new PrimitiveInfo()
        {
            Type = volumeData.primitiveEnum,
            Center = fpmath1.Vector3ToFp3(this.transform.position + this.transform.rotation * fpmath1.Fp3ToVector3(volumeData.offset)),
            Quaternion = Quaternion.Euler(fpmath1.Fp3ToVector3(volumeData.eulerOffset)) * this.transform.rotation
        };

        if (!InitPrimitiveInfo(ref info, volumeData.primitiveParam))
        {
            return false;
        }

        info.showInfo = new PrimitiveShowInfo
        {
            isDrawShow = volumeData.isShow,
            drawColor = volumeData.color,
        };

        return true;
    }

    private void UpdatePri()
    {
        var dic = _primitives.GetEnumerator();

        while (dic.MoveNext())
        {
            dic.Current.Value.OnDispose();
        }

        dic.Dispose();
        _primitives.Clear();
        ReUpdate();
    }

    private bool CheckPrimitiveType(PrimitiveEnum primitiveEnum, out Type pType)
    {
        switch (primitiveEnum)
        {
            case PrimitiveEnum.NONE:
                pType = null;
                return false;
            case PrimitiveEnum.BoxPrimitive:
                pType = typeof(BoxPrimitive);
                return true;
            case PrimitiveEnum.CapsulePrimitive:
                pType = typeof(CapsulePrimitive);
                return true;
            case PrimitiveEnum.SpherePrimitive:
                pType = typeof(SpherePrimitive);
                return true;
            default:
                pType = null;
                return false;
        }
    }

    private bool InitPrimitiveInfo(ref PrimitiveInfo info, List<fp> param)
    {
        switch (info.Type)
        {
            case PrimitiveEnum.NONE:
                Debug.LogWarning($"受击盒形状未知");
                return false;
            case PrimitiveEnum.BoxPrimitive:
                if (param.Count < 3)
                {
                    Debug.LogWarning($"方形受击盒参数少于3个");
                    return false;
                }

                info.BoxSize = new fp3(param[0], param[1], param[2]);
                return true;
            case PrimitiveEnum.CapsulePrimitive:
                if (param.Count < 2)
                {
                    Debug.LogWarning($"胶囊受击盒参数少于2个");
                    return false;
                }

                info.Radius = param[0];
                info.Height = param[1];
                return true;

            case PrimitiveEnum.SpherePrimitive:
                if (param.Count < 1)
                {
                    Debug.LogWarning($"球形受击盒参数少于1个");
                    return false;
                }

                info.Radius = param[0];
                return true;
        }

        return false;
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Color defaultColor = Gizmos.color;

        Gizmos.color = Color.white;
        UpdatePri();
        // ReUpdate();

        foreach (var primitive in _primitives)
        {
            primitive.Value.OnDrawGizmos();
        }

        Gizmos.color = defaultColor;
    }
#endif
}


[Serializable]
public class HitColliderEditorSetting
{
    [LabelText("Key")] public string key;
    
    [LabelText("受击盒形状")] public PrimitiveEnum primitiveEnum = PrimitiveEnum.NONE;

    [LabelText("受击盒中心偏移")] public Vector3 offset;

    [LabelText("受击盒角度偏移")] public Vector3 eulerOffset;

    [LabelText("半径")] [ShowIf("primitiveEnum", PrimitiveEnum.SpherePrimitive)]
    public float spRadius;

    [LabelText("半径")] [ShowIf("primitiveEnum", PrimitiveEnum.CapsulePrimitive)]
    public float capRadius;

    [LabelText("长度")] [ShowIf("primitiveEnum", PrimitiveEnum.CapsulePrimitive)]
    public float height;

    [LabelText("x轴方向长度")] [ShowIf("primitiveEnum", PrimitiveEnum.BoxPrimitive)]
    public float x;

    [LabelText("y轴方向长度")] [ShowIf("primitiveEnum", PrimitiveEnum.BoxPrimitive)]
    public float y;

    [LabelText("z轴方向长度")] [ShowIf("primitiveEnum", PrimitiveEnum.BoxPrimitive)]
    public float z;

    [LabelText("受击盒权重")] [Tooltip("从1开始且默认值为1。0认为无效")] [SerializeField]
    public int weight = 1;

    [LabelText("受击盒伤害倍率")] [SerializeField]
    public float damageMagnification = 1;

    [LabelText("是否显示受击盒")] [SerializeField]
    public bool isShow = true;

    [LabelText("受击盒颜色")] [SerializeField] public Color color = Color.white;

    public void Clear()
    {
        primitiveEnum = PrimitiveEnum.NONE;
        offset = Vector3.zero;
        eulerOffset = Vector3.zero;
        spRadius = 0;
        capRadius = 0;
        height = 0;
        x = 0;
        y = 0;
        z = 0;
        weight = 1;
        damageMagnification = 1;
        isShow = true;
        color = Color.white;
    }
}