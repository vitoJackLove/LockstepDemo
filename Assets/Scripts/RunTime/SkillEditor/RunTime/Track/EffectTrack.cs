using System;

[TrackName(Name = "特效")]
[TrackColor(0, 1, 0.45f)]
[Serializable]
[TrackBindClip(Types = new[]
        {
            typeof(PlayEffectClip),
        }
    )
]
public class EffectTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.View;
}