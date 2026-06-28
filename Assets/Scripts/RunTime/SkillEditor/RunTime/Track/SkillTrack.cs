using System;

[TrackName(Name = "技能状态")]
[TrackColor(255f / 255, 246f / 255, 0f / 255)]
[Serializable]
[TrackBindClip(Types = new[]
        {
            typeof(ExecuteSkillStateClip),
            typeof(SkillDeroveWindowClip),
            typeof(SkillBreakWindowClip),
            typeof(ClearAttackIndexClip),
        }
    )
]

public class SkillTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.Logic;
}
