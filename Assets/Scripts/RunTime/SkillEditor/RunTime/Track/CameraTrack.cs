using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[TrackName(Name = "相机效果")]
[TrackColor(41f / 255, 0f / 255, 255f / 255)]
[TrackBindClip(Types = new []
        {
            typeof(CameraRotateClip),
        }
    )
]
[Serializable]
public class CameraTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.View;
}
