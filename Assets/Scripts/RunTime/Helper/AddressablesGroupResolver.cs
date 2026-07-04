using System;

/// <summary>
/// 将运行时资产路径解析为 Addressables Group 名称的纯静态逻辑。
/// </summary>
public static class AddressablesGroupResolver
{
    public const string Config = "Config";
    public const string UI = "UI";
    public const string SceneCore = "Scene-Core";
    public const string BattleEntity = "Battle-Entity";
    public const string BattleContent = "Battle-Content";
    public const string Map = "Map";
    public const string SceneBattle = "Scene-Battle";
    public const string RemoteDlc = "Remote-DLC";
    public const string HotUpdateCodeLocal = "HotUpdate-Code-Local";
    public const string HotUpdateCodeRemote = "HotUpdate-Code-Remote";

    public static string ResolveGroupName(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        assetPath = assetPath.Replace("\\", "/");

        if (assetPath.StartsWith("Assets/HotUpdate/Code/AOT/", StringComparison.Ordinal))
        {
            return HotUpdateCodeLocal;
        }

        if (assetPath.StartsWith("Assets/HotUpdate/Code/", StringComparison.Ordinal))
        {
            return HotUpdateCodeRemote;
        }

        if (assetPath.StartsWith("Assets/GameAssetConfig/", StringComparison.Ordinal)
            || assetPath.StartsWith("Assets/Config/", StringComparison.Ordinal))
        {
            return Config;
        }

        if (assetPath.StartsWith("Assets/Prefabs/UI/", StringComparison.Ordinal))
        {
            return UI;
        }

        if (string.Equals(assetPath, "Assets/Scene/Launcher.unity", StringComparison.Ordinal))
        {
            return SceneCore;
        }

        if (assetPath.StartsWith("Assets/Scene/", StringComparison.Ordinal))
        {
            return SceneBattle;
        }

        if (assetPath.Contains("/Battle/Hero/", StringComparison.Ordinal)
            || assetPath.Contains("/Battle/Monster/", StringComparison.Ordinal))
        {
            return BattleEntity;
        }

        if (assetPath.StartsWith("Assets/Prefabs/Battle/", StringComparison.Ordinal))
        {
            return BattleContent;
        }

        if (assetPath.StartsWith("Assets/Prefabs/Map/", StringComparison.Ordinal)
            || assetPath.StartsWith("Assets/Prefabs/RoguelikeMap/", StringComparison.Ordinal)
            || assetPath.StartsWith("Assets/Prefabs/NavMesh/", StringComparison.Ordinal))
        {
            return Map;
        }

        return null;
    }
}
