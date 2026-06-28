using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 测试骨骼曲线
/// </summary>
public class MeasurementSkeletonCurveWindow
{
    /// <summary>
    /// 游戏物体绑定的动画
    /// </summary>
    private Animator _animator;
    
    [LabelText("游戏物体")]
    [OnValueChanged("OnCreateGo")]
    public GameObject GameObject;

    [LabelText("受击盒")]
    public GameObject Box;

    [LabelText("动画片段")]
    public AnimationClip Clip;
    
    [LabelText("实体骨骼数据")]
    public SkeletonWorldSpaceCurveData SkeletonData;
    
    [LabelText("骨骼名字")]
    public string SkeletonName;
    
    [LabelText("动画进度")]
    [ProgressBar(0, 100, r: 1, g: 1, b: 1, Height = 30)]
    [OnValueChanged("OnProgressBarChanged")]
    public short ColoredProgressBar = 50;
    
    private void OnProgressBarChanged()
    {
        if (_animator == null)
        {
            Debug.LogError($"游戏物体没有动画机...");
        }
        
        WorldSpaceData data = SkeletonData.GetCurveData(SkeletonName);

        float time = (float)ColoredProgressBar / 100 * Clip.length;
        
        Clip.SampleAnimation(GameObject, time);
        
        Vector3 worldPosition = data.GetPosition(time);

        Box.transform.position = worldPosition;
    }

    public void OnCreateGo()
    {
        _animator = GameObject.GetComponent<Animator>();

        if (_animator == null)
        {
            Debug.LogError($"游戏物体没有动画机...");
        }
    }
}
