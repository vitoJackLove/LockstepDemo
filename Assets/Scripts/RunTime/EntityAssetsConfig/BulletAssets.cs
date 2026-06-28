using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "RogueLike/BulletAssets")]
public class BulletAssets :  ScriptableObject ,IAssetsConfig
{
    public List<BulletAssetsConfig> bulletAssetsConfigList = new List<BulletAssetsConfig>();
    
    public Type GetDataTableType()
    {
        return typeof(BulletAssetsConfig);
    }

    public EntityAssetsConfig GetDataTable(int id)
    {
        for (int i = 0; i < bulletAssetsConfigList.Count; i++)
        {
            if (bulletAssetsConfigList[i].assetsId == id)
            {
                return bulletAssetsConfigList[i];
            }
        }

        return null;
    }

    public List<EntityAssetsConfig> GetAllDataTable()
    {
        List<EntityAssetsConfig> list = new List<EntityAssetsConfig>();

        for (int i = 0; i < bulletAssetsConfigList.Count; i++)
        {
            list.Add(bulletAssetsConfigList[i]);
        }

        return list;
    }
}

[Serializable]
public class BulletAssetsConfig : EntityAssetsConfig
{
    [LabelText("子弹名字")]
    public string heroName;
     
    [LabelText("攻击力")]
    public float attack;

    [LabelText("存活时间(帧)")]
    public int lifeTime = -1;

    [LabelText("攻击次数")]
    public int attackNumber;

    [LabelText("受击盒数据")] 
    public List<HitColliderEditorSetting> colliderDataList = new ();
}