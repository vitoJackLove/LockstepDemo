using UnityEditor;

/// <summary>
/// 序列化面板
/// </summary>
public class SkillTimelineInspector //: ScriptableObject
{
    public static void ShowInspector(SkillTimelineEditorWindow window, TaskClip clip)
    {
        //Selection.activeObject = clip;
    }

    public void OnValidate()
    {
        //if (Window != null) AETimelineFactory.Save(Window.Asset, Window.AssetPath);
    }
}