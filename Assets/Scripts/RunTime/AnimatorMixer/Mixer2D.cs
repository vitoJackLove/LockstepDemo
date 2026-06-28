using Animancer;
using UnityEngine;

/// <summary>
/// 2d 混合器
/// </summary>
public class Mixer2D : BaseMixer
{
    /// <summary>
    /// 变量插值
    /// </summary>
    private SmoothedVector2Parameter _smoothedParameter;
    
    public Mixer2D(TransitionAssetBase assetBase,StringAsset paramOne,StringAsset paramTwo,
        AnimancerComponent animancer,float smoothTime) : base(assetBase,animancer)
    {
        _smoothedParameter = new SmoothedVector2Parameter(
            animancer,
            paramOne,
            paramTwo,
            smoothTime);
    }

    public override void SetValue(Vector2 vector2)
    {
        _smoothedParameter.TargetValue = vector2;
    }

    public override void DisPose()
    {
        _smoothedParameter.Dispose();
        _smoothedParameter = null;
    }
}
