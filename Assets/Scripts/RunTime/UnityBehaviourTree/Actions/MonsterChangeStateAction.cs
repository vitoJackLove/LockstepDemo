
namespace TheKiwiCoder
{
    [System.Serializable]
    public class ChangeBehaviourTreeStateAction : ActionNode
    {
        public BehaviourTreeState state;
        
        protected override void OnStart(WorldUpdateType worldUpdateType)
        {
            
        }

        protected override void OnStop()
        {
            
        }

        protected override State OnUpdate(uint tick,WorldUpdateType worldUpdateType)
        {
            return State.Success;
        }
    }
}


