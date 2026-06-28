using System;

[TrackName(Name = "Buff")]
[TrackColor(255f / 255, 0f / 255, 0f / 255)]
[TrackBindClip(Types = new []
        {
            typeof(PlayBuffClip),
        }
    )
]
[Serializable]
public class BuffTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.Logic;
}
