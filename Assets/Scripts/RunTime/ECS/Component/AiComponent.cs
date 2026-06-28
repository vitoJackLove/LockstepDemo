using Ase.Serializing;
using Unity.Mathematics.FixedPoint;

/// <summary>
/// AI组件
/// </summary>
public class AiComponent : BaseComponent
{
    private BehaviourTreeData _behaviourTreeData;
    
    public override async void OnStart(object data = null)
    {
        base.OnStart(data);

        string treeAssetsPath = Entity.GetData<string>(ComponentDataKey.BehaviourTreeConfig);

        _behaviourTreeData = await Entity.GetSystem<BehaviourTreeSystem>().ExecuteBehaviourTree(treeAssetsPath, Entity);
    }

    public override void OnFixedUpdate(fp deltaTime, WorldUpdateType worldUpdateType)
    {
        base.OnFixedUpdate(deltaTime, worldUpdateType);

        if (_behaviourTreeData != null)
        {
            uint tick = worldUpdateType == WorldUpdateType.Authority
                ? Entity.BaseWorld.AuthorityTick
                : Entity.BaseWorld.LocalTick;

            _behaviourTreeData.FixUpdate(deltaTime, tick, worldUpdateType);
        }
    }

    public void Pause()
    {
        _behaviourTreeData.Pause();
    }

    public void Recover()
    {
        _behaviourTreeData.Recover();
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        if (_behaviourTreeData != null)
        {
            _behaviourTreeData.TakeSnapShot(hardWriter, softWriter);
        }

    }

    public override void HardRollBackTo(PooledReader authoritySnapShot)
    {
        if (_behaviourTreeData != null)
        {
            _behaviourTreeData.RollBackTo(authoritySnapShot);
        }
    }
}
