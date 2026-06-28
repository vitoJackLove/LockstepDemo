using System.Collections;
using System.Collections.Generic;
using Ase.Serializing;

namespace TheKiwiCoder
{
    public partial class BehaviourTreeInstance
    {
        public void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
        {
            runtimeTree.TakeSnapShot(hardWriter, softWriter);
        }

        public void RollBackTo(PooledReader authoritySnapShot)
        {
            runtimeTree.RollBackTo(authoritySnapShot);
        }
    }
}

