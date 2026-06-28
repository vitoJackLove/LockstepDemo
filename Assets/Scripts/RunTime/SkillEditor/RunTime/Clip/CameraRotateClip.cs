using Cinemachine;
using Unity.Mathematics;
using Unity.Mathematics.FixedPoint;
using UnityEngine;

[ClipName("相机震动")]
public class CameraRotateClip : TaskClip
{
    public float shakerTime;

    public float shakerIntensity;
    
    /// <summary>
    /// 虚拟相机
    /// </summary>
    public CinemachineVirtualCamera virtualCamera;

    public NoiseSettings _NoiseSettings;

    /// <summary>
    /// 位置
    /// </summary>
    private CinemachineFramingTransposer _framingTransposer;

    private CinemachineBasicMultiChannelPerlin _cinemachineBasicMultiChannelPerlin;

    /// <summary>
    /// 
    /// </summary>
    private CinemachinePOV _cinemachinePov;

    private float _time;

    public override void EditorEnter(GameObject context, int fps, int currentFrameID)
    {
        base.EditorEnter(context, fps, currentFrameID);

        _framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();

        _cinemachinePov = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
        
        _cinemachineBasicMultiChannelPerlin = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        _cinemachineBasicMultiChannelPerlin.m_NoiseProfile = _NoiseSettings;

        _time = shakerTime;
    }

    public override void RunTimeTick(int currentFrameID, int fps, fp deltaTime, BaseEntity context)
    {
        
    }

    public override void EditorTick(int currentFrameID, int fps, fp deltaTime, GameObject context)
    {
        if (_framingTransposer == null)
        {
            return;
        }

        if (_cinemachinePov == null)
        {
            return;
        }
        
        _time -= (float)deltaTime * currentFrameID;

        _cinemachinePov.m_HorizontalAxis.Value = 100;
        _cinemachinePov.m_VerticalAxis.Value = 100;
    }

    public override void EditorExit(GameObject context, int fps, int currentFrameID)
    {
        base.EditorExit(context, fps, currentFrameID);

       

        _time = 0;
    }
}
