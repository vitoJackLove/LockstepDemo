using TheKiwiCoder;
using UnityEngine;

[System.Serializable]
public abstract class ConditionNode : Node
{
    public bool invert;
    
    [SerializeReference]
    [HideInInspector] 
    public Node child;
    
    protected abstract bool OnCheck();

    protected override State OnUpdate(uint tick, WorldUpdateType worldUpdateType)
    {
        if (child == null)
        {
            return State.Failure;
        }
        
        bool result = OnCheck();

        if (invert)
        {
            if (!result)
            {
                return child.Update(tick,worldUpdateType);
            }
        }
        else
        {
            if (result)
            {
                return child.Update(tick,worldUpdateType);
            }
        }
        
        return State.Running;
    }
}
