using Unity.Mathematics.FixedPoint;
using UnityEngine;

public class FTransform : IFTransform , IPool
{
    private fp3 _position;
    
    public static FTransform Create(EntityCreateData createData)
    {
        FTransform transform = FPoolHelper.Get<FTransform>();
        transform.Position = createData.Position;
        transform.LocalScale = createData.Scale;
        transform.Rotation = fpmath1.EulerXYZ(createData.EulerAngles);
        
        return transform;
    }

    public virtual fp3 Position
    {
        get
        {
            return _position;
        }
        set
        {
            _position = value;
        }
    }

    public virtual fp3 LocalScale
    {
        get;
        set;
    }

    public virtual fpquaternion Rotation
    {
        get;
        set;
    }

    public fp3 EulerAngles
    {
        get => this.Rotation.ToEulerAngles();
        set => this.Rotation = fpmath1.EulerXYZ(value);
    }

    public fp3 Forward => fpmath1.forward(Rotation);

    public void Clear()
    {
        this.Position = fp3.zero;
        this.LocalScale = fp3.zero;
        this.Rotation = fpquaternion.identity;
    }
}
