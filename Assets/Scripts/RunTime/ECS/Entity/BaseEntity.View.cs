using Animancer;
using UnityEngine;

public abstract partial class BaseEntity 
{
    /// <summary>
    /// 实体的表现成
    /// </summary>
    private EntityView _entityView;
    
    /// <summary>
    /// 游戏物体
    /// </summary>
    protected GameObject UnityGameObject;

    public GameObject GameObject => UnityGameObject;

    /// <summary>
    /// 绑定Unity 组件
    /// </summary>
    protected virtual void BindUnityComponent()
    {
        if (UnityGameObject == null)
        {
            return;
        }
        
        AnimancerComponent animatorComponent = UnityGameObject.GetComponent<AnimancerComponent>();

        GetTypeOfComponent<AnimatorComponent>()?.RegisterAnimator(animatorComponent);

        GetComponent<TransformComponent>().RegisterTransform(UnityGameObject.transform);
    }
}
