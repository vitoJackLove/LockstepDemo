using System;

[TrackName(Name = "子弹")]
[TrackColor(0, 0, 0)]
[Serializable]
[TrackBindClip(Types = new[]
        {
            typeof(CreateBulletClip),
            typeof(CreateFollowBulletClip),
            typeof(CreateMovementBulletClip),
        }
    )
]

public class BulletTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.Logic;
}
