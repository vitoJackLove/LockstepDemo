using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheKiwiCoder
{
    [System.Serializable]
    public class Failure : DecoratorNode
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

            var state = child.Update(tick,worldUpdateType);
            
            if (state == State.Success)
            {
                return State.Failure;
            }

            return state;
        }
    }
}