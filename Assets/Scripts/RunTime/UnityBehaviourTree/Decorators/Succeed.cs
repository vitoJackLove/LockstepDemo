namespace TheKiwiCoder
{
    [System.Serializable]
    public class Succeed : DecoratorNode
    {
        protected override void OnStart(WorldUpdateType worldUpdateType)
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate(uint tick,WorldUpdateType worldUpdateType)
        {
            if (child == null)
            {
                return State.Failure;
            }

            var state = child.Update(tick, worldUpdateType);
            
            if (state == State.Failure)
            {
                return State.Success;
            }

            return state;
        }
    }
}