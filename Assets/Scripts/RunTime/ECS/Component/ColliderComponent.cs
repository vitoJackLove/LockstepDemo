using System.Collections.Generic;

/// <summary>
/// 受击盒组件
/// </summary>
public class ColliderComponent : BaseComponent
{
    public override void OnStart(object data = null)
    {
        base.OnStart(data);

        List<HitColliderEditorSetting> dataList = 
            Entity.GetData<List<HitColliderEditorSetting>>(ComponentDataKey.ColliderData);
        
        RegisterColliderData(dataList);
    }

    private void RegisterColliderData(List<HitColliderEditorSetting> colliderEditorSettings)
    {
        if (colliderEditorSettings == null || colliderEditorSettings.Count == 0)
        {
            return;
        }
        
        Dictionary<string, VolumeData> volumeDataDic = new Dictionary<string, VolumeData>();

        foreach (var data in colliderEditorSettings)
        {
            VolumeData volumeData = VolumeData.Create(data);
            
            volumeDataDic.Add(data.key, volumeData);
        }

        Entity.GetSystem<VolumeSystem>()?.RegisterHitVolume(Entity,volumeDataDic);
    }

    public override void OnEntityDead(object data)
    {
        base.OnEntityDead(data);

        Entity.GetSystem<VolumeSystem>()?.UnRegisterHitVolume(Entity.EntityId);
    }
}
