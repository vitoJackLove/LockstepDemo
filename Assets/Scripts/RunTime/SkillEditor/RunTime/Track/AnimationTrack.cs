using System;

[TrackName(Name = "动画")]
[TrackColor(127f / 255, 252f / 255, 228f / 255)]
[TrackBindClip(Types = new[]
        {
            typeof(PlayAnimationClip),
            typeof(ChangeAnimatorParamClip),
        }
    )
]
[Serializable]
public class SkillAnimationTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.View;
}

