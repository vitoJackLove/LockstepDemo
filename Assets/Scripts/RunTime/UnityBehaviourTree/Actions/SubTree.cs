using UnityEngine;

namespace TheKiwiCoder
{
    [System.Serializable]
    public class SubTree : ActionNode
    {
        [Tooltip("Behaviour tree asset to run as a subtree")]
        public BehaviourTree treeAsset;

        [HideInInspector] public BehaviourTree treeInstance;

        public override void OnInit()
        {
            if (treeAsset)
            {
                treeInstance = treeAsset.Clone();
                treeInstance.Bind(context);
            }
        }

        protected override void OnStart(WorldUpdateType worldUpdateType)
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate(uint tick,WorldUpdateType worldUpdateType)
        {
            if (treeInstance)
            {
                return treeInstance.Tick(tick, worldUpdateType);
            }

            return State.Failure;
        }
    }
}