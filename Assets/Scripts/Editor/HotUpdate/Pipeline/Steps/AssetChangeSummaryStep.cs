using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rogue.Editor.HotUpdate.Pipeline;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Rogue.Editor.HotUpdate.Pipeline.Steps
{
    /// <summary>
    /// 资源热更前统计 Addressables 分组条目变化摘要。
    /// </summary>
    public static class AssetChangeSummaryStep
    {
        /// <summary>
        /// 同步资源并输出各 Group 条目数变化摘要。
        /// </summary>
        /// <param name="context">热更构建上下文。</param>
        /// <returns>变更摘要文本。</returns>
        public static string Execute(HotUpdateBuildContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            context.LogLine("[AssetSummary] 统计资源变更...");

            Dictionary<string, int> beforeCounts = CaptureGroupEntryCounts();

            StringBuilder summary = new StringBuilder();
            summary.AppendLine("资源分组同步前条目数:");

            IEnumerable<string> groupNames = beforeCounts.Keys
                .Where(name => !IsHotUpdateCodeGroup(name))
                .OrderBy(name => name);

            List<string> groupList = groupNames.ToList();
            for (int i = 0; i < groupList.Count; i++)
            {
                string groupName = groupList[i];
                string line = $"  {groupName}: {beforeCounts[groupName]}";
                summary.AppendLine(line);
                context.LogLine($"[AssetSummary] {line.Trim()}");
            }

            if (groupList.Count == 0)
            {
                summary.AppendLine("  （无可统计的资源分组）");
                context.LogLine("[AssetSummary] 无可统计的资源分组。");
            }

            return summary.ToString().TrimEnd();
        }

        private static Dictionary<string, int> CaptureGroupEntryCounts()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                return counts;
            }

            for (int i = 0; i < settings.groups.Count; i++)
            {
                AddressableAssetGroup group = settings.groups[i];
                if (group == null || IsHotUpdateCodeGroup(group.Name))
                {
                    continue;
                }

                counts[group.Name] = group.entries.Count;
            }

            return counts;
        }

        private static bool IsHotUpdateCodeGroup(string groupName)
        {
            return groupName == AddressablesGroupResolver.HotUpdateCodeLocal
                || groupName == AddressablesGroupResolver.HotUpdateCodeRemote;
        }
    }
}
