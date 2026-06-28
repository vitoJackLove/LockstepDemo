using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheKiwiCoder
{
    [System.Serializable]
    public class Inverter : DecoratorNode
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

            switch (child.Update(tick,worldUpdateType))
            {
                case State.Running:
                    return State.Running;
                case State.Failure:
                    return State.Success;
                case State.Success:
                    return State.Failure;
            }

            return State.Failure;
        }
    }
}