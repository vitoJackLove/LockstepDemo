using Unity.Mathematics.FixedPoint;
using UnityEngine;

[ClipName("锁定目标运动")]
public class LockTargetMoveClip : TaskClip
{
    [VariableName("移动速度")]
    public float speed;

    [VariableName("看向目标冷却")]
    public int lookAtCooling;

    private int _cooling;

    private BaseEntity _targetEntity;

    private fp3 _dir;
    
    public override void OnRunTimeEnter(BaseEntity context, int fps)
    {
        base.OnRunTimeEnter(context, fps);

        int entityId = 0;
        _targetEntity = context.GetSystem<EntitySystem>().GetEntity(entityId);
        _cooling = 0;
    }

    public override void RollBackEnter(BaseEntity context, int fps)
    {
        base.RollBackEnter(context, fps);
        
        int entityId = 0;
        _targetEntity = context.GetSystem<EntitySystem>().GetEntity(entityId);
        _cooling = 0;
    }

    public override void RunTimeTick(int currentFrameID, int fps, fp deltaTime, BaseEntity context)
    {
        base.RunTimeTick(currentFrameID, fps, deltaTime, context);

        if (_cooling == 0)
        {
            _dir = _targetEntity.transform.Position - context.transform.Position;
            
            fpquaternion quaternion  = fpmath1.LookRotation(_dir,fpmath1.up());

            context.transform.Rotation = quaternion;
            
            context.EntityDebug($"开始朝向 ： {_dir}");
        }

        _cooling++;

        if (_cooling == lookAtCooling)
        {
            _cooling = 0;
        }
        
        context.transform.Position = (context.transform.Forward * (fp)speed) + context.transform.Position;
    }

   /*public override void TakeSnapShot(PooledWriter writer,DevelSnapShotData shotData)
    {
        base.TakeSnapShot(writer, shotData);
 
        /*writer.WriteInt32(_cooling);
        writer.WriteVector3(_dir);
        
        shotData.WriterData($"_cooling = {_cooling}");
        shotData.WriterData($"_dir = {_dir}");#1#
    }

    public override void RollBackTo(PooledReader localSnapShot, PooledReader authoritySnapShot, RollBackData rollBackData)
    {
        base.RollBackTo(localSnapShot, authoritySnapShot, rollBackData);

        /*_cooling = reader.ReadInt32();
        _dir = reader.ReadVector3();#1#
    }*/
}
