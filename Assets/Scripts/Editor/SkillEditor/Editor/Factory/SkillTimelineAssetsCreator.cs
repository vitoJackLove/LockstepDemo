using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

public static class SkillTimelineAssetsCreator
{
    public const string SUFFIX = "asset";

    [MenuItem("Assets/Create/技能编辑器/SkillTimeLineAsset")]
    private static void CreatoFile()
    {
        TimelineAssetsCreatorEndAction creatorEndAction =
            ScriptableObject.CreateInstance<TimelineAssetsCreatorEndAction>();
        string name = GetName();
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(creatorEndAction.GetInstanceID(), creatorEndAction,
            name, null, null);
    }

    private static string GetName(string tempName = "SkillTimeLineAsset")
    {
        int i = 0;
        string name = $"{tempName}_{i}";
        string[] files = AssetDatabase.FindAssets(name);

        for (i += 1; files != null && files.Length > 0; i++)
        {
            name = $"{tempName}_{i}";
            files = AssetDatabase.FindAssets(name);
        }

        return $"{name}.{SUFFIX}";
    }
}

public class TimelineAssetsCreatorEndAction : EndNameEditAction
{
    //按回车执行Action方法
    public override void Action(int instanceId, string pathName, string resourceFile)
    {
        var asset = ScriptableObject.CreateInstance<SkillLineAsset>();
        AssetDatabase.CreateAsset(asset,pathName);
        Texture2D icon = EditorGUIUtility.IconContent("Light Icon").image as Texture2D;
        //刷新
        AssetDatabase.Refresh();
    }
}