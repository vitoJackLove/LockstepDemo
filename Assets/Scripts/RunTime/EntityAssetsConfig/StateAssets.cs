using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "RogueLike/StateAssets")]
public class StateAssets : ScriptableObject ,IAssetsConfig
{
    public List<StateAssetsConfig> StateAssetsConfigList = new List<StateAssetsConfig>();

    public Type GetDataTableType()
    {
        return typeof(StateAssetsConfig);
    }

    public EntityAssetsConfig GetDataTable(int id)
    {
        for (int i = 0; i < StateAssetsConfigList.Count; i++)
        {
            if (StateAssetsConfigList[i].assetsId == id)
            {
                return StateAssetsConfigList[i];
            }
        }

        return null;
    }

    public List<EntityAssetsConfig> GetAllDataTable()
    {
        List<EntityAssetsConfig> list = new List<EntityAssetsConfig>();

        for (int i = 0; i < StateAssetsConfigList.Count; i++)
        {
            list.Add(StateAssetsConfigList[i]);
        }

        return list;
    }
}

[Serializable]
public class StateAssetsConfig : EntityAssetsConfig
{
    [LabelText("优先级")] public int priority;

    [LabelText("状态名字")] public string stateName;
    
    [LabelText("是否可以移动")] public bool isCanMove;
    
    [LabelText("是否可以旋转")] public bool isCanRotate;
    
    [LabelText("是否可以是否技能")] public bool isCanPlaySkill;
}
