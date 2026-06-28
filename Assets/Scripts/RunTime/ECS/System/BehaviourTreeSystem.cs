using System.Collections.Generic;
using System.Threading.Tasks;
using TheKiwiCoder;
using UnityEngine;

/// <summary>
/// 行为树系统
/// </summary>
public class BehaviourTreeSystem : BaseSystem
{
    /// <summary>
    /// 执行ID
    /// </summary>
    private uint _executeId;
    
    private Dictionary<uint, BehaviourTreeData> _behaviourTreeDic = new();

    public override void OnInit(object data = null)
    {
        base.OnInit(data);
        
        _executeId = 0;
    }

    public async Task<BehaviourTreeData> ExecuteBehaviourTree(string assetsPath, BaseEntity entity)
    {
        GameObject behaviourTreeGo = await GetSystem<EntityViewSystem>().SyncGetEntityView(assetsPath);

        if (entity.EntityUpdateType == EntityUpdateType.AuthorityEntity)
        {
            behaviourTreeGo.name = $"{behaviourTreeGo.name}" + "logic";
        }
        else
        {
            behaviourTreeGo.name = $"{behaviourTreeGo.name}" + "View";
        }

        behaviourTreeGo.transform.SetParent(entity.BaseWorld.BehaviourTreeRoot);

        BehaviourTreeInstance instanceRunner = behaviourTreeGo.GetComponent<BehaviourTreeInstance>();

        _executeId++;

        BehaviourTreeData data = BehaviourTreeData.Create(instanceRunner, entity);

        return data;
    }
}
