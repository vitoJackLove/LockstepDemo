using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class WorldSpaceData
{
    [LabelText("骨骼点名字")]
    public string skeletonName;
    
    public AnimationCurve WorldPositionX = new AnimationCurve();
    public AnimationCurve WorldPositionY = new AnimationCurve();
    public AnimationCurve WorldPositionZ= new AnimationCurve();
        
    public AnimationCurve WorldRotationX= new AnimationCurve();
    public AnimationCurve WorldRotationY= new AnimationCurve();
    public AnimationCurve WorldRotationZ= new AnimationCurve();
    public AnimationCurve WorldRotationW= new AnimationCurve();

    /// <summary>
    /// 添加位移曲线
    /// </summary>
    /// <param name="time"></param>
    /// <param name="position"></param>
    public void AddPositionCurve(float time, Vector3 position)
    {
        WorldPositionX.AddKey(time, position.x);
        WorldPositionY.AddKey(time, position.y);
        WorldPositionZ.AddKey(time, position.z);
    }

    public Vector3 GetPosition(float time)
    {
        return new Vector3(WorldPositionX.Evaluate(time), WorldPositionY.Evaluate(time),
            WorldPositionZ.Evaluate(time));
    }

    /// <summary>
    /// 添加位移曲线
    /// </summary>
    /// <param name="time"></param>
    /// <param name="rotation"></param>
    public void AddRotationCurve(float time, Quaternion rotation)
    {
        WorldRotationX.AddKey(time, rotation.x);
        WorldRotationY.AddKey(time, rotation.y);
        WorldRotationZ.AddKey(time, rotation.z);
        WorldRotationW.AddKey(time, rotation.w);
    }
}
