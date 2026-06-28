using UnityEditor;
using UnityEngine;

/// <summary>
/// 将配置表中的 SkillTimeLine 相对路径解析为编辑器可用的资产路径。
/// </summary>
public static class SkillTimelinePathUtility
{
    private const string BattleSkillRoot = "Assets/Prefabs/Battle/";

    /// <summary>
    /// 将 Hero/Monster 配置里的路径转换为 SkillLineAsset 的 AssetDatabase 路径。
    /// </summary>
    public static string ResolveEditorAssetPath(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            return string.Empty;
        }

        string normalized = configPath.Trim().Replace('\\', '/');

        if (!normalized.StartsWith("Assets/"))
        {
            normalized = $"{BattleSkillRoot}{normalized.TrimStart('/')}";
        }

        if (!normalized.EndsWith(".asset"))
        {
            normalized = $"{normalized}.asset";
        }

        return normalized;
    }

    /// <summary>
    /// 加载配置路径对应的 SkillLineAsset；不存在时返回 null。
    /// </summary>
    public static SkillLineAsset LoadSkillLineAsset(string configPath)
    {
        string assetPath = ResolveEditorAssetPath(configPath);
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<SkillLineAsset>(assetPath);
    }

    /// <summary>
    /// 判断配置路径是否能在项目中找到对应 SkillLineAsset。
    /// </summary>
    public static bool TryResolveAsset(string configPath, out SkillLineAsset asset, out string assetPath)
    {
        assetPath = ResolveEditorAssetPath(configPath);
        asset = string.IsNullOrEmpty(assetPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<SkillLineAsset>(assetPath);

        return asset != null;
    }

    /// <summary>
    /// 在 Project 窗口定位 SkillLineAsset。
    /// </summary>
    public static void PingSkillAsset(string configPath)
    {
        if (!TryResolveAsset(configPath, out SkillLineAsset asset, out _))
        {
            Debug.LogWarning($"未找到 SkillTimeLine 资产，配置路径: {configPath}");
            return;
        }

        EditorGUIUtility.PingObject(asset);
        Selection.activeObject = asset;
    }
}
