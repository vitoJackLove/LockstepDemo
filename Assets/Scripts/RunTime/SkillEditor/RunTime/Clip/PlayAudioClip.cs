using Unity.Mathematics.FixedPoint;
using UnityEngine;

[ClipName("播放音效")]
public class PlayAudioClip : TaskClip
{
    public AudioClip AudioClip;
    public Vector3 Position;
    public float Volume;

    private GameObject _temp;
    
    public override void RunTimeTick(int currentFrameID, int fps,fp deltaTime, BaseEntity context)
    {
        if (context == null) return;
        
        if (AudioClip != null)
        {
            AudioSource.PlayClipAtPoint(AudioClip, Position);
        }
    }

    public override void EditorTick(int currentFrameID, int fps,fp deltaTime, GameObject context)
    {
        if (context == null) return;
        
        if (currentFrameID == 0 && AudioClip != null)
        {
            var position = context.transform.TransformPoint(Position);
            
            _temp = new GameObject("One shot audio");
            _temp.transform.position = position;
            AudioSource audioSource = (AudioSource)_temp.AddComponent(typeof(AudioSource));
            audioSource.clip = AudioClip;
            audioSource.spatialBlend = 1f;
            audioSource.volume = Volume;
            audioSource.Play();
        }
    }

    public override void OnRunTimeExit(BaseEntity context)
    {
        base.OnRunTimeExit(context);
        
        if (_temp != null) GameObject.DestroyImmediate(_temp);
    }

    public override void EditorExit(GameObject context, int fps, int currentFrameID)
    {
        base.EditorExit(context, fps, currentFrameID);
        
        if (_temp != null) GameObject.DestroyImmediate(_temp);
    }
}