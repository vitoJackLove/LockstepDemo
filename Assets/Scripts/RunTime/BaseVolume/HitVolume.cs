using PrimitiveDetection;
using Unity.Mathematics.FixedPoint;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 纯粹受击盒信息与本地实体的绑定
/// </summary>
public class HitVolume : BaseVolume
{
    /// <summary>
    /// 拥有者实体本地Id
    /// </summary>
    private int _ownerId;

    /// <summary>
    /// 纯粹受击盒的固定信息
    /// </summary>
    private VolumeData _volumeData;

    /// <summary>
    /// 拥有者实体
    /// </summary>
    private BaseEntity _ownerEntity;

    /// <summary>
    /// 更新的形状信息
    /// </summary>
    private PrimitiveInfo _primitiveInfo;

    private BasePrimitive _primitive;

    public EntityUpdateType EntityUpdateType => _ownerEntity.EntityUpdateType;

    public EntityState EntityState;

    /// <summary>
    /// 部位名称
    /// </summary>
    private string key;

    public override int OwnerId => _ownerId;

    public string Key => key;

    public override VolumeData VolumeData => _volumeData;

    public static HitVolume Create(BaseEntity ownerEntity, string key, VolumeData volumeData)
    {
        HitVolume data = FPoolHelper.Get<HitVolume>();
        data._ownerId = ownerEntity.EntityId;
        data.key = key;
        data._volumeData = volumeData;
        data._ownerEntity = ownerEntity;
        data._primitiveInfo.Type = volumeData.primitiveEnum;
        
        fp3 offset = ownerEntity.transform.Rotation * volumeData.offset;
        
        data._primitiveInfo.Center = ownerEntity.transform.Position + offset;

        
        data._primitiveInfo.Quaternion = fpmath1.EulerXYZ(volumeData.eulerOffset) 
                                         * ownerEntity.transform.Rotation;
        
        data._primitiveInfo = global::Primitive.InitPrimitiveInfo(ref data._primitiveInfo, data._volumeData.primitiveParam);
        
        data._primitive = global::Primitive.CreatePrimitive(data._primitiveInfo);
        
        data.EntityState = EntityState.Survival;
        
        return data;
    }

    public override void OnUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        if (EntityState != EntityState.Survival)
        {
            return;
        }
        
        if (worldUpdateType == WorldUpdateType.Authority && _ownerEntity.EntityUpdateType ==
            EntityUpdateType.AuthorityEntity)
        {
            fp3 offset = this._ownerEntity.transform.Rotation * _volumeData.offset;
            _primitiveInfo.Center = this._ownerEntity.transform.Position + offset;
            _primitiveInfo.Quaternion = fpmath1.EulerXYZ(_volumeData.eulerOffset) * this._ownerEntity.transform.Rotation;
            _primitive.UpdateSelf(_primitiveInfo);
        }

        if ((worldUpdateType == WorldUpdateType.Local || worldUpdateType == WorldUpdateType.RollBack) 
            && _ownerEntity.EntityUpdateType == EntityUpdateType.LocalEntity)
        {
            fp3 offset = this._ownerEntity.transform.Rotation * _volumeData.offset;
            _primitiveInfo.Center = this._ownerEntity.transform.Position + offset;
            _primitiveInfo.Quaternion = fpmath1.EulerXYZ(_volumeData.eulerOffset) * this._ownerEntity.transform.Rotation;
            _primitive.UpdateSelf(_primitiveInfo);
        }
    }

    public override PrimitiveInfo PrimitiveInfo
    {
        get
        {
            // _primitiveInfo = PrimitiveSystem.InitPrimitiveInfo(ref _primitiveInfo, _volumeData.primitiveParam);
            return _primitiveInfo;
        }
    }

    public override BasePrimitive Primitive
    {
        get { return _primitive; }
    }

    public bool AdjustCapsuleRadius(fp radius)
    {
        if (_primitiveInfo.Type == PrimitiveEnum.CapsulePrimitive)
        {
            _primitiveInfo.Radius = radius;
            return true;
        }

        return false;
    }

    public override void TransferHit(int fromId)
    {
        if (_ownerEntity.EntityType == EntityType.BulletEntity)
        {
            _ownerEntity.GetComponent<BulletTriggerComponent>().OnBulletTrigger(fromId);
        }
        else
        {
            //_ownerEntity.GetTypeOfComponent<HitComponent>().AttackEntity(fromId);
        }
    }

    public void PrimitiveDebug()
    {
        if (EntityState == EntityState.Survival)
        {
            _primitive.PrimitiveDebug(_volumeData.color);
        }
    }

    public override void Clear()
    {
        _ownerId = 0;
        _ownerEntity = null;
        _volumeData = null;
        _primitive.OnDispose();
    }
}