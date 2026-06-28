using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheKiwiCoder
{
    [System.Serializable]
    public class RootNode : Node
    {
        [SerializeReference] [HideInInspector] public Node child;

        protected override void OnStart(WorldUpdateType worldUpdateType)
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate(uint tick,WorldUpdateType worldUpdateType)
        {
            if (child != null)
            {
                return child.Update(tick,worldUpdateType);
            }
            else
            {
                return State.Failure;
            }
        }
    }
}