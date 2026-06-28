using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "RogueLike/DamageTextAssets")]
[Serializable]
public class DamageTextAssets : ScriptableObject , IAssetsConfig
{
    [LabelText("伤害文字列表")]
    public List<DamageTextAssetsConfig> damageTextAssetsList = new List<DamageTextAssetsConfig>();

    public Type GetDataTableType()
    {
        return typeof(DamageTextAssetsConfig);
    }

    public EntityAssetsConfig GetDataTable(int id)
    {
        for (int i = 0; i < damageTextAssetsList.Count; i++)
        {
            if (damageTextAssetsList[i].assetsId == id)
            {
                return damageTextAssetsList[i];
            }
        }

        return null;
    }

    public List<EntityAssetsConfig> GetAllDataTable()
    {
        List<EntityAssetsConfig> list = new List<EntityAssetsConfig>();

        for (int i = 0; i < damageTextAssetsList.Count; i++)
        {
            list.Add(damageTextAssetsList[i]);
        }

        return list;
    }
}

[Serializable]
public class DamageTextAssetsConfig : EntityAssetsConfig
{
    [LabelText("文字Key")]
    public string damageKey;
}