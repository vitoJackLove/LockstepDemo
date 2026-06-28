using System.Collections.Generic;
using Ase.Serializing;

/// <summary>
/// 角色状态组件
/// </summary>
public class HeroStateComponent : StateComponent
{
    public override async void OnStart(object data = null)
    {
        base.OnStart(data);
        
        List<StateConfig> stateList = Entity.GetData<List<StateConfig>>(ComponentDataKey.StateData);

        for (int i = 0; i < stateList.Count; i++)
        {
            StateConfig config = stateList[i];
 
            SkillTimelineLauncher logicTimeLine = await Entity.GetSystem<SkillTimeLineSystem>()
                .InitSkillTimeLine(config.assetsPath, Entity);

            StateTimeline.Add(config.stateId, logicTimeLine);
        }
    }

    /// <summary>
    /// 打断状态
    /// </summary>
    /// <param name="config"></param>
    /// <param name="isRollBackBreakState"></param>
    protected override void BreakState(StateAssetsConfig config,bool isRollBackBreakState)
    {
        base.BreakState(config, isRollBackBreakState);
        
        if (!config.isCanPlaySkill)
        {
            Entity.GetComponent<SkillComponent>().BreakCurrentExecuteSkill(isRollBackBreakState);
        }
        
        if (CurrentStateTimeLine != null)
        {
            CurrentStateTimeLine.ForceExecuteStop(false);
            
            CurrentStateTimeLine = null;
        }
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    /// <param name="config"></param>
    protected override void EnableState(StateAssetsConfig config)
    {
        base.EnableState(config);
        
        if (StateTimeline.TryGetValue(config.assetsId, out var timeLine))
        {
            if (timeLine != null)
            {
                CurrentStateTimeLine = timeLine;
                
                CurrentStateTimeLine.RefreshInitState();
            }
        }
    }
}
