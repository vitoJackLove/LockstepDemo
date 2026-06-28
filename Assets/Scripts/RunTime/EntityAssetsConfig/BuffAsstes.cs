using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Buff配置
/// </summary>
[CreateAssetMenu(menuName = "RogueLike/BuffAssets")]
public class BuffAssets : ScriptableObject, IAssetsConfig
{
    public List<BuffAssetsConfig> buffAssetsConfigList = new List<BuffAssetsConfig>();

    public Type GetDataTableType()
    {
        return typeof(BuffAssetsConfig);
    }

    public EntityAssetsConfig GetDataTable(int id)
    {
        for (int i = 0; i < buffAssetsConfigList.Count; i++)
        {
            if (buffAssetsConfigList[i].assetsId == id)
            {
                return buffAssetsConfigList[i];
            }
        }

        return null;
    }

    public List<EntityAssetsConfig> GetAllDataTable()
    {
        List<EntityAssetsConfig> list = new List<EntityAssetsConfig>();

        for (int i = 0; i < buffAssetsConfigList.Count; i++)
        {
            list.Add(buffAssetsConfigList[i]);
        }

        return list;
    }
}

[Serializable]
public class BuffAssetsConfig : EntityAssetsConfig
{
    [LabelText("生命周期")]
    public int lifeTime;
    
    [LabelText("监听的游戏事件")]
    public BattleExecuteTiming gameEventType;

    [LabelText("Buff图标")]
    public Sprite buffIcon;

    [LabelText("Buff描述")]
    public string buffDoc;

    [LabelText("BUFF效果")]
    public BuffEffectAssets buffEffectAssets;

    [LabelText("Buff条件")]
    public BuffConditionAssets buffConditionAssets;

    [LabelText("执行次数")]
    public int executeValue;
}
