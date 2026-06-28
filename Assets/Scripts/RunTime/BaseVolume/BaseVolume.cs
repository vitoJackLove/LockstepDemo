using PrimitiveDetection;
using Unity.Mathematics.FixedPoint;

public abstract class BaseVolume : IPool
{
    public virtual int OwnerId => 0;

    public abstract VolumeData VolumeData { get; }

    public abstract PrimitiveInfo PrimitiveInfo { get; }

    public abstract BasePrimitive Primitive { get; }

    public abstract void TransferHit(int fromId);

    public abstract void OnUpdate(fp deltaTime, WorldUpdateType worldUpdateType);

    public virtual void Clear()
    {
    }
}
