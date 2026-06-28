using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 技能TineLine 系统
/// </summary>
public class SkillTimeLineSystem : BaseSystem
{
    /// <summary>
    /// 初始化一个技能TimeLine
    /// </summary>
    /// <param name="assetsPath">资产路径</param>
    /// <param name="baseEntity">执行者</param>
    /// <param name="onTimeLineStopAction"></param>
    public async Task<SkillTimelineLauncher> InitSkillTimeLine(string assetsPath, BaseEntity baseEntity , Action onTimeLineStopAction = null)
    {
        GameObject skillTimelineGo = await baseEntity.GetSystem<EntityViewSystem>().SyncGetEntityView(assetsPath);

        if (skillTimelineGo == null)
        {
            return null;
        }

        string skillTimeLineType;

        if (baseEntity.EntityUpdateType == EntityUpdateType.AuthorityEntity)
        {
            skillTimeLineType = "logic";
        }
        else
        {
            skillTimeLineType = "View";
        }

        skillTimelineGo.name = $"{skillTimelineGo.name}" + skillTimeLineType;

        skillTimelineGo.transform.SetParent(baseEntity.BaseWorld.SkillTimeLineRoot);

        SkillTimelineLauncher skillTimelineLauncher = skillTimelineGo.GetComponent<SkillTimelineLauncher>();

        skillTimelineLauncher.InitAssets(baseEntity, onTimeLineStopAction);
        
        return skillTimelineLauncher;
    }
}