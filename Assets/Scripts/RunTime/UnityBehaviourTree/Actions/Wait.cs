using Ase.Serializing;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TheKiwiCoder
{
    [System.Serializable]
    public class Wait : ActionNode
    {
        [LabelText("等待的帧号")]
        public int waitTick;

        private int _tempTick;
        
        protected override void OnStart(WorldUpdateType worldUpdateType)
        {
            _tempTick = waitTick;
        }

        protected override void OnStop() { }

        protected override State OnUpdate(uint tick,WorldUpdateType worldUpdateType)
        {
           _tempTick--;
           
            if (_tempTick == 0)
            {
                return State.Success;
            }

            return State.Running;
        }

        public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
        {
            base.TakeSnapShot(hardWriter,softWriter);
            
            //快照帧是 帧头拍摄 所以要还原当
            hardWriter.WriteInt32Data($"等待的帧号", _tempTick);
        }

        public override void RollBackTo( PooledReader authoritySnapShot)
        {
            base.RollBackTo(authoritySnapShot);
            
            int authorityTempTick = authoritySnapShot.ReadInt32();
            _tempTick = authorityTempTick;
        }
    }
}