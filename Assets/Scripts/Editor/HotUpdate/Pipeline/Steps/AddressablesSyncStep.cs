using System.Collections.Generic;
using System.Collections;
using System.IO;
using Rogue.Editor.HotUpdate.Internal;
using Rogue.Editor.HotUpdate.Pipeline;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace Rogue.Editor.HotUpdate.Pipeline.Steps
{
    /// <summary>
    /// 同步运行时资产到 Addressables 分组。
    /// </summary>
    public static class AddressablesSyncStep
    {
        private static readonly string[] ObsoleteGroupNames = { "Runtime Assets", "Entity" };

        private static readonly (string GroupName, string Label, bool UseRemoteLoadPath, bool IncludeInBuild, BundledAssetGroupSchema.BundlePackingMode BundleMode)[] GroupDefinitions =
        {
            (AddressablesGroupResolver.Config, "config", true, true, BundledAssetGroupSchema.BundlePackingMode.PackTogether),
            (AddressablesGroupResolver.UI, "ui", false, true, BundledAssetGroupSchema.BundlePackingMode.PackTogether),
            (AddressablesGroupResolver.SceneCore, "scene-core", false, true, BundledAssetGroupSchema.BundlePackingMode.PackSeparately),
            (AddressablesGroupResolver.BattleEntity, "battle-entity", true, true, BundledAssetGroupSchema.BundlePackingMode.PackSeparately),
            (AddressablesGroupResolver.BattleContent, "battle-content", true, true, BundledAssetGroupSchema.BundlePackingMode.PackSeparately),
            (AddressablesGroupResolver.Map, "map", true, true, BundledAssetGroupSchema.BundlePackingMode.PackTogether),
            (AddressablesGroupResolver.SceneBattle, "scene-battle", true, true, BundledAssetGroupSchema.BundlePackingMode.PackSeparately),
            (AddressablesGroupResolver.RemoteDlc, "remote-dlc", true, false, BundledAssetGroupSchema.BundlePackingMode.PackSeparately),
            (AddressablesGroupResolver.HotUpdateCodeLocal, "hotupdate-aot", false, true, BundledAssetGroupSchema.BundlePackingMode.PackTogether),
            (AddressablesGroupResolver.HotUpdateCodeRemote, "hotupdate-runtime", true, true, BundledAssetGroupSchema.BundlePackingMode.PackTogether),
        };

        /// <summary>
        /// 在热更 Pipeline 中同步运行时资源到 Addressables 分组。
        /// </summary>
        /// <param name="context">热更构建上下文。</param>
        public static void Execute(HotUpdateBuildContext context)
        {
            context.LogLine("[AddressablesSync] 开始同步运行时资源...");
            ExecuteSyncOnly();
            context.LogLine("[AddressablesSync] 同步完成。");
        }

        /// <summary>
        /// 供 BulletFactory 等遗留调用方使用的无上下文入口。
        /// </summary>
        public static void ExecuteSyncOnly()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                GameLog.Error(GameLogChannel.Resource, "AddressableAssetSettings not found.");
                return;
            }

            EnsureProfileRemoteBaseUrlVariable(settings);
            AddressablesRemoteCatalogConfigurator.EnsureRemoteCatalogEnabled(settings);
            EnsureLabels(settings);

            for (int i = 0; i < GroupDefinitions.Length; i++)
            {
                GetOrCreateGroup(settings, GroupDefinitions[i].GroupName);
            }

            Dictionary<string, HashSet<string>> validGuidsByGroup = new Dictionary<string, HashSet<string>>();
            int changedCount = 0;

            foreach (string assetPath in EnumerateRuntimeAssetPaths())
            {
                string groupName = AddressablesGroupResolver.ResolveGroupName(assetPath);
                if (groupName == null)
                {
                    continue;
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                AddressableAssetGroup group = GetOrCreateGroup(settings, groupName);
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

                ApplyLabel(settings, entry, groupName);
                validGuidsByGroup.GetOrCreateSet(groupName).Add(guid);
            }

            int removedCount = RemoveStaleEntries(settings, validGuidsByGroup);
            RemoveObsoleteGroups(settings);

            AddressableAssetGroup configGroup = settings.FindGroup(AddressablesGroupResolver.Config);
            if (configGroup != null)
            {
                settings.DefaultGroup = configGroup;
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupAdded, settings, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            GameLog.Info(GameLogChannel.Resource,
                $"Runtime Addressables synced. Updated addresses: {changedCount}, removed stale entries: {removedCount}");
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group != null)
            {
                ApplyGroupSchemas(settings, group, groupName);
                return group;
            }

            group = settings.CreateGroup(groupName, false, false, false, null);
            ApplyGroupSchemas(settings, group, groupName);
            return group;
        }

        private static void ApplyGroupSchemas(AddressableAssetSettings settings, AddressableAssetGroup group, string groupName)
        {
            GroupDefinition definition = GetGroupDefinition(groupName);
            if (definition == null)
            {
                return;
            }

            BundledAssetGroupSchema bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
            if (bundledSchema == null)
            {
                bundledSchema = group.AddSchema<BundledAssetGroupSchema>();
            }

            // UseRemoteLoadPath 标记可 Content Update 的远程组：Build/Load 均绑定 Remote Profile。
            // IncludeInBuild=true 时 Unity 仍会把基线 Bundle 打进 Player（混合本地首包 + CDN 热更）。
            bool useRemotePaths = definition.UseRemoteLoadPath;
            bundledSchema.BuildPath.SetVariableByName(
                settings,
                useRemotePaths ? AddressableAssetSettings.kRemoteBuildPath : AddressableAssetSettings.kLocalBuildPath);
            bundledSchema.LoadPath.SetVariableByName(
                settings,
                useRemotePaths ? AddressableAssetSettings.kRemoteLoadPath : AddressableAssetSettings.kLocalLoadPath);
            bundledSchema.BundleMode = definition.BundleMode;
            bundledSchema.IncludeInBuild = definition.IncludeInBuild;
        }

        private static GroupDefinition GetGroupDefinition(string groupName)
        {
            for (int i = 0; i < GroupDefinitions.Length; i++)
            {
                if (GroupDefinitions[i].GroupName == groupName)
                {
                    return new GroupDefinition(GroupDefinitions[i]);
                }
            }

            return null;
        }

        private static void EnsureLabels(AddressableAssetSettings settings)
        {
            for (int i = 0; i < GroupDefinitions.Length; i++)
            {
                settings.AddLabel(GroupDefinitions[i].Label, false);
            }
        }

        private static void ApplyLabel(AddressableAssetSettings settings, AddressableAssetEntry entry, string groupName)
        {
            GroupDefinition definition = GetGroupDefinition(groupName);
            if (definition == null || string.IsNullOrEmpty(definition.Label))
            {
                return;
            }

            settings.AddLabel(definition.Label, false);
            entry.SetLabel(definition.Label, true, false);
        }

        private static void EnsureProfileRemoteBaseUrlVariable(AddressableAssetSettings settings)
        {
            const string variableName = "AddressablesRemoteBaseUrl";
            string value = settings.profileSettings.GetValueByName(settings.activeProfileId, variableName);
            if (string.IsNullOrEmpty(value))
            {
                GameLog.Warn(GameLogChannel.Resource,
                    $"Addressables profile variable '{variableName}' is missing. Run profile setup or edit AddressableAssetSettings.");
            }
        }

        private static int RemoveStaleEntries(
            AddressableAssetSettings settings,
            Dictionary<string, HashSet<string>> validGuidsByGroup)
        {
            int removedCount = 0;
            for (int i = 0; i < GroupDefinitions.Length; i++)
            {
                string groupName = GroupDefinitions[i].GroupName;
                AddressableAssetGroup group = settings.FindGroup(groupName);
                if (group == null)
                {
                    continue;
                }

                HashSet<string> validGuids = validGuidsByGroup.TryGetValue(groupName, out HashSet<string> guids)
                    ? guids
                    : new HashSet<string>();

                List<AddressableAssetEntry> staleEntries = new List<AddressableAssetEntry>();
                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (!validGuids.Contains(entry.guid))
                    {
                        staleEntries.Add(entry);
                    }
                }

                for (int j = 0; j < staleEntries.Count; j++)
                {
                    settings.RemoveAssetEntry(staleEntries[j].guid, false);
                    removedCount++;
                }
            }

            return removedCount;
        }

        private static void RemoveObsoleteGroups(AddressableAssetSettings settings)
        {
            for (int i = 0; i < ObsoleteGroupNames.Length; i++)
            {
                AddressableAssetGroup obsoleteGroup = settings.FindGroup(ObsoleteGroupNames[i]);
                if (obsoleteGroup == null)
                {
                    continue;
                }

                List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>(obsoleteGroup.entries);
                for (int j = 0; j < entries.Count; j++)
                {
                    settings.RemoveAssetEntry(entries[j].guid, false);
                }

                settings.RemoveGroup(obsoleteGroup);
            }
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

            foreach (string path in EnumerateFiles("Assets/Prefabs/UI", "*.prefab", SearchOption.AllDirectories))
            {
                yield return path;
            }

            foreach (string path in EnumerateFiles("Assets/Prefabs/Battle", "*.prefab", SearchOption.AllDirectories))
            {
                yield return path;
            }

            foreach (string path in EnumerateFiles("Assets/Prefabs/Map", "*.prefab", SearchOption.AllDirectories))
            {
                yield return path;
            }

            foreach (string path in EnumerateFiles("Assets/Prefabs/RoguelikeMap", "*.prefab", SearchOption.AllDirectories))
            {
                yield return path;
            }

            foreach (string path in EnumerateFiles("Assets/Prefabs/NavMesh", "*.prefab", SearchOption.AllDirectories))
            {
                yield return path;
            }

            foreach (string path in EnumerateFiles("Assets/Scene", "*.unity", SearchOption.AllDirectories))
            {
                yield return path;
            }

            foreach (string path in EnumerateFiles("Assets/HotUpdate/Code/AOT", "*.bytes", SearchOption.TopDirectoryOnly))
            {
                yield return path;
            }

            foreach (string path in EnumerateFiles("Assets/HotUpdate/Code", "*.bytes", SearchOption.TopDirectoryOnly))
            {
                yield return path;
            }

            foreach (string path in EnumerateFiles("Assets/HotUpdate/Code", "manifest.json", SearchOption.TopDirectoryOnly))
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

        private static HashSet<string> GetOrCreateSet(this Dictionary<string, HashSet<string>> dictionary, string key)
        {
            if (!dictionary.TryGetValue(key, out HashSet<string> set))
            {
                set = new HashSet<string>();
                dictionary[key] = set;
            }

            return set;
        }

        private sealed class GroupDefinition
        {
            public GroupDefinition(
                (string GroupName, string Label, bool UseRemoteLoadPath, bool IncludeInBuild, BundledAssetGroupSchema.BundlePackingMode BundleMode) source)
            {
                GroupName = source.GroupName;
                Label = source.Label;
                UseRemoteLoadPath = source.UseRemoteLoadPath;
                IncludeInBuild = source.IncludeInBuild;
                BundleMode = source.BundleMode;
            }

            public string GroupName { get; }
            public string Label { get; }
            public bool UseRemoteLoadPath { get; }
            public bool IncludeInBuild { get; }
            public BundledAssetGroupSchema.BundlePackingMode BundleMode { get; }
        }
    }
}
