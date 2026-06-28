using System;

[TrackName(Name = "快照")]
[TrackColor(0f / 255, 255f / 255, 240f / 255)]
[Serializable]
[TrackBindClip(Types = new[]
        {
            typeof(TakeSnapShotClip),
        }
    )
]

public class RollBackTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.View;
}
