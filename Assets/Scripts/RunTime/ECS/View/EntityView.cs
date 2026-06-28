using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 骨骼数据
/// </summary>
[InlineProperty(LabelWidth = 90)]
public struct BoneData
{
    [LabelText("绑点描述")] public string desc;
    [LabelText("骨骼Root")] public Transform bone;
}

public class EntityView : SerializedMonoBehaviour
{
    private BaseEntity _baseEntity;

    public BaseEntity BaseEntity => _baseEntity;
    
    public virtual void OnInit(BaseEntity baseEntity)
    {
         this._baseEntity = baseEntity;
         _baseEntity.GameObject.transform.localScale = baseEntity.transform.LocalScale.ToVector3();
         _baseEntity.GameObject.transform.position = baseEntity.transform.Position.ToVector3();
         _baseEntity.GameObject.transform.rotation = baseEntity.transform.Rotation;
         _baseEntity.GameObject.name = $"{baseEntity.EntityId} - {baseEntity.EntityType}";
    }
    
    public virtual void OnEntityDead() { }
}
