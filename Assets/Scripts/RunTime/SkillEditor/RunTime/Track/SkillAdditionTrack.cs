using System;

[TrackName(Name = "技能加成")]
[TrackColor(1, 0.52f, 0.97f)]
[Serializable]
[TrackBindClip(Types = new[]
        {
            typeof(SetSkillAdditionClip),
        }
    )
]
public class SkillAdditionTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.Logic;
}
