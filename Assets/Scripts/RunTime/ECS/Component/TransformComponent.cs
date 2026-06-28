using Ase.Serializing;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

public class TransformComponent : BaseComponent
{
    private Transform _unityTransform;

    public void RegisterTransform(Transform transform)
    {
        _unityTransform = transform;
    }

    public override void TakeSnapShot(BaseSnapShotData hardWriter, BaseSnapShotData softWriter)
    {
        hardWriter.WriterFp3Data("Entity Position", Entity.transform.Position);
        hardWriter.WriterFpQuaternionData("Entity Rotation", Entity.transform.Rotation);
    }

    public override void HardRollBackTo(PooledReader authoritySnapShot)
    {
        fp3 authorityPosition = BaseSnapShotData.ReadFp3Data(authoritySnapShot);
        fpquaternion authorityRotation = BaseSnapShotData.ReadFpQuaternionData(authoritySnapShot);

        Entity.transform.Position = authorityPosition;
        Entity.transform.Rotation = authorityRotation;

        if (Entity.EntityType == EntityType.MonsterEntity)
        {
            Entity.EntityDebug($"Rollback position: {authorityPosition}");
        }
    }

    public override void OnUpdate(fp deltaTime)
    {
        base.OnUpdate(deltaTime);

        if (_unityTransform)
        {
            _unityTransform.position = fpmath1.Fp3ToVector3(Entity.transform.Position);
            _unityTransform.rotation = Entity.transform.Rotation;
        }
    }

    public Transform UnityTransform => _unityTransform;
}
