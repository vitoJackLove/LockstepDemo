using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RogueLike/EffectAssets")]
public class EffectAssets : ScriptableObject,IAssetsConfig
{
    public List<EffectAssetsConfig> effectAssetsConfigList = new List<EffectAssetsConfig>();

    public Type GetDataTableType()
    {
        return typeof(EffectAssetsConfig);
    }

    public EntityAssetsConfig GetDataTable(int id)
    {
        for (int i = 0; i < effectAssetsConfigList.Count; i++)
        {
            if (effectAssetsConfigList[i].assetsId == id)
            {
                return effectAssetsConfigList[i];
            }
        }

        return null;
    }

    public List<EntityAssetsConfig> GetAllDataTable()
    {
        List<EntityAssetsConfig> list = new List<EntityAssetsConfig>();

        for (int i = 0; i < effectAssetsConfigList.Count; i++)
        {
            list.Add(effectAssetsConfigList[i]);
        }

        return list;
    }
}

[Serializable]
public class EffectAssetsConfig : EntityAssetsConfig
{
   
}