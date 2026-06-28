using System;

[TrackName(Name = "状态")]
[TrackColor(1, 0.52f, 0.97f)]
[Serializable]
[TrackBindClip(Types = new[]
        {
            typeof(ChangeStateClip),
        }
    )
]
public class StateTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.Logic;
}
