using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;

public static class RuntimeAddressablesConfigurator
{
    private const string RuntimeGroupName = "Runtime Assets";

    [MenuItem("Tools/Addressables/Sync Runtime Assets")]
    public static void SyncRuntimeAssets()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            GameLog.Error(GameLogChannel.Resource, "AddressableAssetSettings not found.");
            return;
        }

        AddressableAssetGroup group = GetOrCreateRuntimeGroup(settings);
        HashSet<string> validGuids = new HashSet<string>();
        int changedCount = 0;
        int removedCount = 0;

        foreach (string assetPath in EnumerateRuntimeAssetPaths())
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                continue;
            }

            validGuids.Add(guid);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry == null)
            {
                continue;
            }

            if (entry.address != assetPath)
            {
                entry.SetAddress(assetPath, false);
                changedCount++;
            }
        }

        List<AddressableAssetEntry> staleEntries = new List<AddressableAssetEntry>();
        foreach (AddressableAssetEntry entry in group.entries)
        {
            if (!validGuids.Contains(entry.guid))
            {
                staleEntries.Add(entry);
            }
        }

        for (int i = 0; i < staleEntries.Count; i++)
        {
            settings.RemoveAssetEntry(staleEntries[i].guid, false);
            removedCount++;
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, group, true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        GameLog.Info(GameLogChannel.Resource, $"Runtime Addressables synced. Updated entries: {changedCount}, removed stale entries: {removedCount}");
    }

    [MenuItem("Tools/Addressables/Build Runtime Content")]
    public static void BuildRuntimeContent()
    {
        SyncRuntimeAssets();
        AddressablesBuildLayoutGuard.PrepareForAddressablesBuild(true);
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        if (!string.IsNullOrEmpty(result.Error))
        {
            GameLog.Error(GameLogChannel.Resource, $"Addressables build failed: {result.Error}");
            return;
        }

        GameLog.Info(GameLogChannel.Resource, $"Addressables build succeeded. Locations: {result.LocationCount}");
    }

    private static AddressableAssetGroup GetOrCreateRuntimeGroup(AddressableAssetSettings settings)
    {
        AddressableAssetGroup group = settings.FindGroup(RuntimeGroupName);
        if (group != null)
        {
            return group;
        }

        AddressableAssetGroup defaultGroup = settings.DefaultGroup;
        return settings.CreateGroup(
            RuntimeGroupName,
            false,
            false,
            false,
            defaultGroup == null ? null : defaultGroup.Schemas);
    }

    private static IEnumerable<string> EnumerateRuntimeAssetPaths()
    {
        foreach (string path in EnumerateFiles("Assets/GameAssetConfig", "*.asset", SearchOption.AllDirectories))
        {
            yield return path;
        }

        foreach (string path in EnumerateFiles("Assets/Config", "*.asset", SearchOption.AllDirectories))
        {
            yield return path;
        }

        foreach (string path in EnumerateFiles("Assets/Prefabs/UI/Window", "*.prefab"))
        {
            yield return path;
        }

        foreach (string path in EnumerateFiles("Assets/Prefabs/Battle", "*.prefab"))
        {
            yield return path;
        }

        foreach (string path in EnumerateFiles("Assets/Prefabs/NavMesh", "*.prefab"))
        {
            yield return path;
        }

        foreach (string path in EnumerateFiles("Assets/Prefabs/Map/MapCube", "*.prefab"))
        {
            yield return path;
        }

        foreach (string path in EnumerateFiles("Assets/Scene", "*.unity"))
        {
            yield return path;
        }
    }

    private static IEnumerable<string> EnumerateFiles(
        string folder,
        string searchPattern,
        SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (!Directory.Exists(folder))
        {
            yield break;
        }

        string[] paths = Directory.GetFiles(folder, searchPattern, searchOption);
        for (int i = 0; i < paths.Length; i++)
        {
            string assetPath = paths[i].Replace("\\", "/");
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
            {
                continue;
            }

            yield return assetPath;
        }
    }
}

[InitializeOnLoad]
public static class AddressablesBuildLayoutGuard
{
    private static Func<AddressableAssetSettings, AddressablesPlayerBuildResult> _previousBuildOverride;

    static AddressablesBuildLayoutGuard()
    {
        InstallPlayerBuildOverride();
        PrepareForAddressablesBuild(false);
    }

    [MenuItem("Tools/Addressables/Disable Build Layout Report")]
    public static void DisableBuildLayoutReportMenu()
    {
        PrepareForAddressablesBuild(true);
    }

    public static void PrepareForAddressablesBuild(bool logResult)
    {
        bool changed = false;

        if (ProjectConfigData.GenerateBuildLayout)
        {
            ProjectConfigData.GenerateBuildLayout = false;
            changed = true;
        }

        if (DisableAutoOpenAddressablesReport())
        {
            changed = true;
        }

        if (ProjectConfigData.BuildReportFilePaths.Count > 0)
        {
            ProjectConfigData.ClearBuildReportFilePaths();
            changed = true;
        }

        if (logResult)
        {
            string state = changed ? "disabled and cleared" : "already disabled";
            GameLog.Info(GameLogChannel.Resource, $"Addressables Build Layout report is {state}.");
        }
    }

    private static void InstallPlayerBuildOverride()
    {
        if (AddressablesPlayerBuildProcessor.BuildAddressablesOverride == BuildPlayerContentWithoutBuildLayout)
        {
            return;
        }

        _previousBuildOverride = AddressablesPlayerBuildProcessor.BuildAddressablesOverride;
        AddressablesPlayerBuildProcessor.BuildAddressablesOverride = BuildPlayerContentWithoutBuildLayout;
    }

    private static AddressablesPlayerBuildResult BuildPlayerContentWithoutBuildLayout(AddressableAssetSettings settings)
    {
        PrepareForAddressablesBuild(false);

        if (_previousBuildOverride != null && _previousBuildOverride != BuildPlayerContentWithoutBuildLayout)
        {
            return _previousBuildOverride(settings);
        }

        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        return result;
    }

    private static bool DisableAutoOpenAddressablesReport()
    {
        PropertyInfo property = typeof(ProjectConfigData).GetProperty(
            "AutoOpenAddressablesReport",
            BindingFlags.Static | BindingFlags.NonPublic);

        if (property == null || !property.CanRead || !property.CanWrite)
        {
            return false;
        }

        object value = property.GetValue(null);
        if (value is bool enabled && enabled)
        {
            property.SetValue(null, false);
            return true;
        }

        return false;
    }
}
