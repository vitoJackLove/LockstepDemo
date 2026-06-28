using Animancer;
using UnityEngine;

/// <summary>
/// 线性混合器
/// </summary>
public class LinearMixer : BaseMixer
{
    private Parameter<float> _parameter;
    
    public LinearMixer(TransitionAssetBase assetBase, StringAsset stringAsset, AnimancerComponent animancer) : 
        base(assetBase,animancer)
    {
        _parameter = animancer.Parameters.GetOrCreate<float>(stringAsset);
    }

    public override void SetValue(Vector2 vector2)
    {
        _parameter.SetValue(vector2.x);
    }

    public override void DisPose()
    {
        _parameter = null;
    }
}
