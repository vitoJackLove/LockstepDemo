using System;

[TrackName(Name = "音频")]
[TrackColor(253f / 255, 194f / 255, 4f / 255)]
[TrackBindClip(Types = new []
        {
            typeof(PlayAudioClip),
        }
    )
]
[Serializable]
public class AudioTrack : StandardTrack
{
    public override TrackType TrackType => TrackType.View;
}