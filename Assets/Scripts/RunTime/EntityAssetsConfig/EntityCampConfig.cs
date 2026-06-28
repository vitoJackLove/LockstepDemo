using System;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "RogueLike/EntityCampConfig")]
public class EntityCampConfig : ScriptableObject
{
    [TableMatrix(HorizontalTitle = "0=角色阵营，1=怪物阵营，2=中立阵营，3=邪恶阵营", IsReadOnly = true)] [ShowInInspector]
    public RelationsBetweenCamps[,] ReadOnlyMatrix =
    {
        {RelationsBetweenCamps.Nice,RelationsBetweenCamps.Adversarial,RelationsBetweenCamps.Neutral,RelationsBetweenCamps.Adversarial},
        
        {RelationsBetweenCamps.Adversarial,RelationsBetweenCamps.Nice,RelationsBetweenCamps.Adversarial,RelationsBetweenCamps.Adversarial},
        
        {RelationsBetweenCamps.Neutral,RelationsBetweenCamps.Neutral,RelationsBetweenCamps.Neutral,RelationsBetweenCamps.Neutral},
        
        {RelationsBetweenCamps.Adversarial,RelationsBetweenCamps.Adversarial,RelationsBetweenCamps.Adversarial,RelationsBetweenCamps.Adversarial},
    };
}
