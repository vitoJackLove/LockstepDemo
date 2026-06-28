using TheKiwiCoder;

[System.Serializable]
public class MaintainTargetLockAction : ActionNode
{
    protected override void OnStart(WorldUpdateType worldUpdateType)
    {
    }

    protected override void OnStop()
    {
    }

    protected override State OnUpdate(uint tick, WorldUpdateType worldUpdateType)
    {
        EnemyDetectionComponent detectionComponent = context.Entity.GetComponent<EnemyDetectionComponent>();

        if (detectionComponent != null)
        {
            // 只维护锁定位置和锁定旋转，不驱动实体 Transform。
            detectionComponent.UpdateLock(tick, worldUpdateType);
        }

        return State.Success;
    }
}
