using System;

[TrackName(Name = "运动")]
[TrackColor(0f / 255, 255f / 255, 240f / 255)]
[Serializable]
[TrackBindClip(Types = new[]
        {
            typeof(LineMoveClip),
            typeof(OpenMovementClip),
            typeof(LockTargetMoveClip),
            typeof(MoveToPositionClip),
        }
    )
]
public class MovementTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.Logic;
}
