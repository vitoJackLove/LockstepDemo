using UnityEngine;

namespace TheKiwiCoder
{
    [System.Serializable]
    public class Log : ActionNode
    {
        public NodeProperty<string> logNumber;

        protected override void OnStart(WorldUpdateType worldUpdateType)
        {
            Debug.Log(logNumber.Value);
        }

        protected override void OnStop()
        {
            
        }

        protected override State OnUpdate(uint tick, WorldUpdateType worldUpdateType)
        {
            return State.Success;
        }
    }
}