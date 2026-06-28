using Ase.Serializing;
using UnityEngine;

namespace TheKiwiCoder
{
    [System.Serializable]
    public class Sequencer : CompositeNode
    {
        private int _runIndex;
        
        protected override void OnStart(WorldUpdateType worldUpdateType)
        {
            _runIndex = 0;
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate(uint tick,WorldUpdateType worldUpdateType)
        {
            for (int i = _runIndex; i < children.Count; ++i)
            {
                var childStatus = children[i].Update(tick,worldUpdateType);

                if (childStatus == State.Running)
                {
                    _runIndex = i;
                    
                    return State.Running;
                }
                
                if (childStatus == State.Failure)
                {
                    return State.Failure;
                }
            }

            return State.Success;
        }

        public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
        {
            base.TakeSnapShot(hardWriter, softWriter);

            hardWriter.WriteInt32Data($"Sequencer.RunIndex", _runIndex);
        }

        public override void RollBackTo(PooledReader authoritySnapShot)
        {
            base.RollBackTo(authoritySnapShot);
            
            int authorityRunIndex = authoritySnapShot.ReadInt32();
            _runIndex = authorityRunIndex;
        }
    }
}