using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheKiwiCoder
{
    [System.Serializable]
    public class Selector : CompositeNode
    {
        protected override void OnStart(WorldUpdateType worldUpdateType)
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate(uint tick,WorldUpdateType worldUpdateType)
        {
            for (int i = 0; i < children.Count; ++i)
            {
                var childStatus = children[i].Update(tick,worldUpdateType);
                
                if (childStatus == State.Running)
                {
                    return State.Running;
                }
                else if (childStatus == State.Success)
                {
                    return State.Success;
                }
            }

            return State.Failure;
        }
    }
}