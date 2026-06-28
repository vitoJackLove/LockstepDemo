using UnityEngine;

public delegate void ClipUIAction(SkillTimelineEditorWindow window,Rect rect, int[] hightLight, Color color,
    string clipName, float widthPerFrame,int trackIndex, int clipIndex);