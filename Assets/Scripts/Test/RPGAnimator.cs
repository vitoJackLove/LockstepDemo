using System;
using Animancer;
using UnityEngine;

public class RPGAnimator : MonoBehaviour
{
    public AnimancerComponent _Animator;
    [SerializeField] private IKPuppetLookTarget _LookTarget;
    
    public void Awake()
    {
        //_Animator.Layers[0].ApplyAnimatorIK = true;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        _LookTarget.UpdateAnimatorIK(_Animator.Animator);
    }
}
