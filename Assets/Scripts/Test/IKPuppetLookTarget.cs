using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKPuppetLookTarget : MonoBehaviour
{
    [SerializeField] private Transform _BodyTarget;
    [SerializeField] private AvatarIKGoal _Type;// Determines which limb this target applies to.
    
    [SerializeField, Range(0, 1)] private float _Weight = 1;
    [SerializeField, Range(0, 1)] private float _BodyWeight = 0.3f;
    [SerializeField, Range(0, 1)] private float _HeadWeight = 0.6f;
    [SerializeField, Range(0, 1)] private float _EyesWeight = 1;
    [SerializeField, Range(0, 1)] private float _ClampWeight = 0.5f;

    public void UpdateAnimatorIK(Animator animator)
    {
        animator.SetLookAtWeight(_Weight, _BodyWeight, _HeadWeight, _EyesWeight, _ClampWeight);
        animator.SetLookAtPosition(_BodyTarget.transform.position);
    }
}
