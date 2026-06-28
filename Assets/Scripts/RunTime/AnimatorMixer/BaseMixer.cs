using Animancer;
using UnityEngine;

/// <summary>
/// 动画回合器基类
/// </summary>
public abstract class BaseMixer
{
     protected TransitionAssetBase AssetBase;

     public TransitionAssetBase TransitionAssetBase => AssetBase;

     protected BaseMixer(TransitionAssetBase assetBase,AnimancerComponent animancer)
     {
          if (assetBase == null)
          {
               return;
          }
          
          this.AssetBase = assetBase;
          animancer.Play(AssetBase);
     }

     /// <summary>
     /// 设置混合器变量
     /// </summary>
     /// <param name="vector2"></param>
     public abstract void SetValue(Vector2 vector2);

     public abstract void DisPose();
}
