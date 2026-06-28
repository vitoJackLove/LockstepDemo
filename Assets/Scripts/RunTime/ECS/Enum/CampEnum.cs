using Sirenix.OdinInspector;

/// <summary>
/// 阵营
/// </summary>
public enum CampEnum 
{
    [LabelText("角色阵营")]
    CharacterCamp,

    [LabelText("怪物阵营")]
    MonsterCamp,

    [LabelText("中立阵营")]
    NeutralCamp,
    
    [LabelText("邪恶阵营")]
    EvilCamp,
}

public enum RelationsBetweenCamps
{
    [LabelText("友善")]
    Nice,
    [LabelText("敌对")]
    Adversarial,
    [LabelText("中立")]
    Neutral,
}
