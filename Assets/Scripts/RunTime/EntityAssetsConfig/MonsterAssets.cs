using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


[CreateAssetMenu(menuName = "RogueLike/MonsterAssets")]
public class MonsterAssets : ScriptableObject ,IAssetsConfig
{
    public List<MonsterAssetsConfig> monsterAssetsConfigList = new List<MonsterAssetsConfig>();
    public Type GetDataTableType()
    {
        return typeof(MonsterAssetsConfig);
    }

    public EntityAssetsConfig GetDataTable(int id)
    {
        for (int i = 0; i < monsterAssetsConfigList.Count; i++)
        {
            if (monsterAssetsConfigList[i].assetsId == id)
            {
                return monsterAssetsConfigList[i];
            }
        }

        return null;
    }

    public List<EntityAssetsConfig> GetAllDataTable()
    {
        List<EntityAssetsConfig> list = new List<EntityAssetsConfig>();

        for (int i = 0; i < monsterAssetsConfigList.Count; i++)
        {
            list.Add(monsterAssetsConfigList[i]);
        }

        return list;
    }
}

[Serializable]
public class MonsterAssetsConfig : EntityAssetsConfig
{
    [LabelText("怪物头像")]
    public Sprite monsterIcon;
    
    [LabelText("怪物名字")]
    public string monsterName;

    [LabelText("血量")]
    public int hp;

    [LabelText("攻击距离")]
    public int attackDistance;
    
    [LabelText("初始行动次数")]
    public int initActionNumber;
    
    [LabelText("速度")]
    public int speed;
    
    [LabelText("攻击力")]
    public int attack;
     
    [LabelText("防御力")]
    public int defence;
    
    [LabelText("阵营")]
    public CampEnum campEnum;

    [LabelText("受击盒数据")] 
    public List<HitColliderEditorSetting> colliderDataList = new List<HitColliderEditorSetting>();

    [LabelText("怪物技能组")]
    public List<MonsterSkillConfig> monsterSkillList = new();
    
    [LabelText("怪物状态组")]
    public List<StateConfig> stateList = new List<StateConfig>();

    [LabelText("行为树路径")] public string treeAssetsPath;
    
    [LabelText("动画混合树")]
    public AnimatorBlendTree blendTree;
}

[Serializable]
public class MonsterSkillConfig
{
    [LabelText("技能ID")]
    public int skillId;
    
    [LabelText("TimeLine路径")]
    public string skillAssetsPath;
}
