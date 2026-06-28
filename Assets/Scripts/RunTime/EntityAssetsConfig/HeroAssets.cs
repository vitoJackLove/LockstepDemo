using System;
using System.Collections.Generic;
using Animancer;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "RogueLike/HeroAssets")]
public class HeroAssets : ScriptableObject ,IAssetsConfig
{
     public List<HeroAssetsConfig> HeroAssetsConfigList = new List<HeroAssetsConfig>();

     public Type GetDataTableType()
     {
          return typeof(HeroAssetsConfig);
     }

     public EntityAssetsConfig GetDataTable(int id)
     {
          for (int i = 0; i < HeroAssetsConfigList.Count; i++)
          {
               if (HeroAssetsConfigList[i].assetsId == id)
               {
                    return HeroAssetsConfigList[i];
               }
          }

          return null;
     }

     public List<EntityAssetsConfig> GetAllDataTable()
     {
          List<EntityAssetsConfig> list = new List<EntityAssetsConfig>();

          for (int i = 0; i < HeroAssetsConfigList.Count; i++)
          {
               list.Add(HeroAssetsConfigList[i]);
          }

          return list;
     }
}

[Serializable]
public class HeroAssetsConfig : EntityAssetsConfig
{
     [LabelText("英雄头像")]
     public Sprite heroIcon;
     
     [LabelText("英雄名字")]
     public string heroName;
     
     [LabelText("血量")]
     public int hp;

     [LabelText("速度")]
     public int speed;

     [LabelText("旋转速度")]
     public int rotateSpeed;

     [LabelText("攻击力")]
     public int attack;
     
     [LabelText("防御力")]
     public int defence;

     [LabelText("阵营")]
     public CampEnum campEnum;

     [LabelText("人物初始携带的技能")]
     public List<HeroSkillConfig> initSkillList = new List<HeroSkillConfig>();

     [LabelText("受击盒数据")] 
     public List<HitColliderEditorSetting> colliderDataList = new List<HitColliderEditorSetting>();

     [LabelText("角色状态组")]
     public List<StateConfig> stateList = new List<StateConfig>();
     
     [LabelText("动画混合树")]
     public AnimatorBlendTree blendTree;
}

[Serializable]
public class HeroSkillConfig
{
     [LabelText("技能ID")]
     public int skillId;

     [LabelText("执行类型")] public WorldContent.CommandExecuteState commandState;
     
     [LabelText("按下的逻辑TimeLine路径")]
     [HideIf("commandState",WorldContent.CommandExecuteState.OnlyUp)]
     public string skillDownAssetsPath;
     
     [LabelText("抬起的逻辑TimeLine路径")]
     [HideIf("commandState",WorldContent.CommandExecuteState.OnlyDown)]
     public string skillUpAssetsPath;
     
     [ShowIf("commandState",WorldContent.CommandExecuteState.DownUp)]
     [LabelText("按下执行最长Tick")]
     public int maxDownTick;
     
     [LabelText("指令")]
     public CommandType keyCode;

     [LabelText("普攻段数的缓存Tick")]
     [ShowIf("keyCode",CommandType.Attack)]
     public int attackIndexCacheTick;

     [LabelText("普攻段数")]
     [ShowIf("keyCode",CommandType.Attack)]
     public int attackIndex;
     
}

[Serializable]
public class StateConfig
{
     [LabelText("状态ID")]
     public int stateId;

     [LabelText("状态资产路径")]
     public string assetsPath;
}

[Serializable]
public class AnimatorBlendTree
{
     [LabelText("混合树类型")]
     public BlendTreeType blendTreeType;

     [LabelText("混合树资产")]
     public TransitionAssetBase transitionAssetBase;

     [LabelText("变量1")]
     public StringAsset valueOne;

     [ShowIf("blendTreeType",BlendTreeType.Mixer2D)]
     [LabelText("变量2")]
     public StringAsset valueTwo;

     [ShowIf("blendTreeType", BlendTreeType.Mixer2D)] [LabelText("变量平滑过度")]
     public float smoothTime = 0.15f;
}

public enum BlendTreeType
{
     Line,
     Mixer2D,
}
