using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class HotUpdateTestAddressablesConfigurator
{
    private const string HotUpdateFolder = "Assets/HotUpdateTest";
    private const string HotUpdateGroupName = "TestHo";
    private const string LocalStandaloneLoadPath = "http://127.0.0.1:8080";
    private const string LanStandaloneLoadPath = "http://[PrivateIpAddress]:8080";

    public static readonly string CubeAddress = "Assets/HotUpdateTest/Cube.prefab";

    [MenuItem("Tools/Addressables/热更新测试/1. 同步 HotUpdateTest 资源")]
    public static void SyncHotUpdateTestAssets()
    {
        if (!TrySyncHotUpdateTestAssets(out string message, out string error))
        {
            ShowError(error);
            return;
        }

        GameLog.Info(GameLogChannel.Resource, message);
        EditorUtility.DisplayDialog("热更新测试", message, "确定");
    }

    [MenuItem("Tools/Addressables/热更新测试/2. 构建初始内容包")]
    public static void BuildInitialContent()
    {
        if (!TrySyncHotUpdateTestAssets(out string syncMessage, out string syncError))
        {
            ShowError(syncError);
            return;
        }

        GameLog.Info(GameLogChannel.Resource, syncMessage);
        if (!EnsureProjectCompiles(out string compileError))
        {
            ShowError(compileError);
            return;
        }

        AddressablesBuildLayoutGuard.PrepareForAddressablesBuild(true);
        AddressablesPlayerBuildResult result = BuildPlayerContentSafely();
        if (!string.IsNullOrEmpty(result.Error))
        {
            ShowError(FormatBuildFailure("初始内容包", result));
            return;
        }

        string message = $"初始内容包构建成功，输出目录 ServerData/[BuildTarget]，资源条目 {result.LocationCount} 个。";
        GameLog.Info(GameLogChannel.Resource, message);
        LogNextSteps(isContentUpdate: false);
        EditorUtility.DisplayDialog("热更新测试", message, "确定");
    }

    [MenuItem("Tools/Addressables/热更新测试/3. 构建增量热更包")]
    public static void BuildContentUpdate()
    {
        if (!TrySyncHotUpdateTestAssets(out string syncMessage, out string syncError))
        {
            ShowError(syncError);
            return;
        }

        GameLog.Info(GameLogChannel.Resource, syncMessage);

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            ShowError("未找到 AddressableAssetSettings。");
            return;
        }

        string contentStatePath = ContentUpdateScript.GetContentStateDataPath(false);
        if (!File.Exists(contentStatePath))
        {
            ShowError($"未找到 content state 文件：{contentStatePath}。请先执行“2. 构建初始内容包”。");
            return;
        }

        Dictionary<AddressableAssetEntry, List<AddressableAssetEntry>> modifiedEntries =
            ContentUpdateScript.GatherModifiedEntriesWithDependencies(settings, contentStatePath);
        if (modifiedEntries == null || modifiedEntries.Count == 0)
        {
            EditorUtility.DisplayDialog("热更新测试", "未检测到资源变更，请先修改 Red.mat 或 Cube.prefab。", "确定");
            return;
        }

        AddressablesBuildLayoutGuard.PrepareForAddressablesBuild(true);
        AddressablesPlayerBuildResult result = BuildPlayerContentSafely();
        if (!string.IsNullOrEmpty(result.Error))
        {
            ShowError(FormatBuildFailure("增量热更包", result));
            return;
        }

        string message = $"增量热更包构建成功，变更条目 {modifiedEntries.Count} 个，资源条目 {result.LocationCount} 个。";
        GameLog.Info(GameLogChannel.Resource, message);
        LogNextSteps(isContentUpdate: true);
        EditorUtility.DisplayDialog("热更新测试", message, "确定");
    }

    [MenuItem("Tools/Addressables/热更新测试/4. 打开本地 Hosting 配置")]
    public static void ConfigureLocalHosting()
    {
        EditorUtility.DisplayDialog(
            "热更新测试",
            "请在 Addressables Groups 窗口中：\n" +
            "1. 启用 Local Hosting 0\n" +
            "2. 端口设为 8080\n" +
            "3. Content Root 设为 ServerData/StandaloneWindows64",
            "确定");
        EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
    }

    [MenuItem("Tools/Addressables/热更新测试/5. 设置本机 Load Path (127.0.0.1:8080)")]
    public static void UseLocalStandaloneLoadPath()
    {
        if (!TrySetLoadPath(LocalStandaloneLoadPath, out string error))
        {
            ShowError(error);
            return;
        }

        EditorUtility.DisplayDialog(
            "热更新测试",
            $"已将 Local/Remote Load Path 设为 {LocalStandaloneLoadPath}。\n请重新构建 Addressables 和 Standalone 包。",
            "确定");
    }

    [MenuItem("Tools/Addressables/热更新测试/6. 设置局域网 Load Path ([PrivateIpAddress]:8080)")]
    public static void UseLanStandaloneLoadPath()
    {
        if (!TrySetLoadPath(LanStandaloneLoadPath, out string error))
        {
            ShowError(error);
            return;
        }

        EditorUtility.DisplayDialog(
            "热更新测试",
            $"已将 Local/Remote Load Path 设为 {LanStandaloneLoadPath}。\n适用于 Standalone 包运行在局域网其他设备上。",
            "确定");
    }

    private static bool EnsureProjectCompiles(out string error)
    {
        error = null;
        if (!EditorUtility.scriptCompilationFailed)
        {
            return true;
        }

        error =
            "项目存在 C# 编译错误，Addressables 无法构建。\n" +
            "请先打开 Console 修复红色报错，再重试。\n" +
            "常见原因：脚本被删除但仍有引用。";
        return false;
    }

    private static AddressablesPlayerBuildResult BuildPlayerContentSafely()
    {
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        return result;
    }

    private static AddressablesPlayerBuildResult BuildPlayerContentSafely(
        AddressableAssetSettings settings,
        string contentStatePath)
    {
        return ContentUpdateScript.BuildContentUpdate(settings, contentStatePath);
    }

    private static string FormatBuildFailure(string phase, AddressablesPlayerBuildResult result)
    {
        if (result.Error == "SBP ErrorError" && EditorUtility.scriptCompilationFailed)
        {
            return $"{phase}构建失败：项目仍有 C# 编译错误（SBP ErrorError）。\n请先修复 Console 中的红色报错。";
        }

        string message = $"{phase}构建失败：{result.Error}";
        if (!string.IsNullOrEmpty(result.OutputPath))
        {
            message += $"\n输出路径：{result.OutputPath}";
        }

        message += "\n请查看 Console 中 SBP / Addressables 的详细报错。";
        return message;
    }

    private static bool TrySyncHotUpdateTestAssets(out string message, out string error)
    {
        message = null;
        error = null;

        try
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                error = "未找到 AddressableAssetSettings，请先在 Window → Addressables → Groups 中初始化 Addressables。";
                return false;
            }

            AddressableAssetGroup group = settings.FindGroup(HotUpdateGroupName);
            if (group == null)
            {
                error = $"未找到 Addressables 分组 '{HotUpdateGroupName}'，请在 Groups 窗口中创建该分组。";
                return false;
            }

            if (!Directory.Exists(HotUpdateFolder))
            {
                error = $"热更新目录不存在：{HotUpdateFolder}";
                return false;
            }

            HashSet<string> validGuids = new HashSet<string>();
            int changedCount = 0;
            int syncedCount = 0;

            foreach (string assetPath in EnumerateHotUpdateAssetPaths())
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

                syncedCount++;
                if (entry.address != assetPath)
                {
                    entry.SetAddress(assetPath, false);
                    changedCount++;
                }
            }

            if (syncedCount == 0)
            {
                error = $"在 {HotUpdateFolder} 下未找到可同步的 .prefab 或 .mat 资源。";
                return false;
            }

            List<AddressableAssetEntry> staleEntries = new List<AddressableAssetEntry>();
            List<AddressableAssetEntry> snapshot = new List<AddressableAssetEntry>(group.entries);
            for (int i = 0; i < snapshot.Count; i++)
            {
                AddressableAssetEntry entry = snapshot[i];
                if (!validGuids.Contains(entry.guid))
                {
                    staleEntries.Add(entry);
                }
            }

            for (int i = 0; i < staleEntries.Count; i++)
            {
                settings.RemoveAssetEntry(staleEntries[i].guid, false);
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, group, true);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            message =
                $"已同步 {syncedCount} 个资源到分组 '{HotUpdateGroupName}'，更新地址 {changedCount} 个，移除过期条目 {staleEntries.Count} 个。";
            return true;
        }
        catch (System.Exception exception)
        {
            error = $"同步失败：{exception.Message}";
            Debug.LogException(exception);
            return false;
        }
    }

    private static bool TrySetLoadPath(string loadPath, out string error)
    {
        error = null;
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            error = "未找到 AddressableAssetSettings。";
            return false;
        }

        string profileId = settings.activeProfileId;
        AddressableAssetProfileSettings profileSettings = settings.profileSettings;
        if (profileSettings == null)
        {
            error = "未找到 Addressables Profile 配置。";
            return false;
        }

        profileSettings.SetValue(profileId, AddressableAssetSettings.kLocalLoadPath, loadPath);
        profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteLoadPath, loadPath);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        GameLog.Info(GameLogChannel.Resource, $"Load Path 已更新为 {loadPath}");
        return true;
    }

    private static IEnumerable<string> EnumerateHotUpdateAssetPaths()
    {
        string[] prefabPaths = Directory.GetFiles(HotUpdateFolder, "*.prefab", SearchOption.AllDirectories);
        for (int i = 0; i < prefabPaths.Length; i++)
        {
            yield return prefabPaths[i].Replace("\\", "/");
        }

        string[] materialPaths = Directory.GetFiles(HotUpdateFolder, "*.mat", SearchOption.AllDirectories);
        for (int i = 0; i < materialPaths.Length; i++)
        {
            yield return materialPaths[i].Replace("\\", "/");
        }
    }

    private static void LogNextSteps(bool isContentUpdate)
    {
        string phase = isContentUpdate ? "增量热更包" : "初始内容包";
        GameLog.Info(
            GameLogChannel.Resource,
            $"{phase}构建完成。下一步：1) 启用 Local Hosting(8080) 2) 构建 Standalone 包 3) 运行 exe 后按 U 拉取热更。");
    }

    private static void ShowError(string message)
    {
        GameLog.Error(GameLogChannel.Resource, message);
        EditorUtility.DisplayDialog("热更新测试", message, "确定");
    }
}
