using Sirenix.OdinInspector;
using TheKiwiCoder;

[System.Serializable]
public class CheckEntityState : ConditionNode
{
    [LabelText("实体ID")]
    public NodeProperty<int> entityId;

    [LabelText("状态ID")] public int stateId;
    
    protected override void OnStart(WorldUpdateType worldUpdateType) { }

    protected override void OnStop() { }

    protected override bool OnCheck()
    {
        return true;
    }
}
