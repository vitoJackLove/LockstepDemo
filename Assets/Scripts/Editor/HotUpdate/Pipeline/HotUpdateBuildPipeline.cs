using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Rogue.Editor.HotUpdate.Pipeline.Steps;

namespace Rogue.Editor.HotUpdate.Pipeline
{
    /// <summary>
    /// 热更发布 Pipeline 编排器，提供首包、代码热更、资源热更三种构建入口。
    /// </summary>
    public static class HotUpdateBuildPipeline
    {
        private static bool _isRunning;

        /// <summary>
        /// 执行首包构建（代码 + 资源 + 可选 Player）。
        /// </summary>
        /// <param name="context">热更构建上下文。</param>
        /// <returns>构建结果。</returns>
        public static HotUpdateBuildResult RunFullPackage(HotUpdateBuildContext context)
        {
            return RunInternal(context, (ctx, logs) =>
            {
                PreflightCheckStep.Execute(ctx);
                HybridClrBuildStep.Execute(ctx, compileBeforeCopy: true);
                string serverData = AddressablesFullBuildStep.Execute(ctx);
                string playerPath = ctx.SkipPlayerBuild ? null : PlayerBuildStep.Execute(ctx);
                string contentState = ResolveLatestContentStatePath();
                ManifestWriteStep.Execute(
                    ctx,
                    ManifestWriteStep.CreatePatchEntry("full", ctx, serverData, playerPath),
                    contentState);
                return HotUpdateBuildResult.Succeeded(logs, serverData, playerPath, HotUpdateManifest.ManifestPath);
            });
        }

        /// <summary>
        /// 执行代码热更打包。
        /// </summary>
        /// <param name="context">热更构建上下文。</param>
        /// <returns>构建结果。</returns>
        public static HotUpdateBuildResult RunCodePatch(HotUpdateBuildContext context)
        {
            return RunInternal(context, (ctx, logs) =>
            {
                PreflightCheckStep.Execute(ctx);
                if (!ctx.ForceSkipAotChangeCheck && AotChangeDetectorStep.HasAotChanges())
                {
                    throw new InvalidOperationException("检测到 AOT 元数据变更，请使用首包构建。");
                }

                HybridClrBuildStep.Execute(ctx, compileBeforeCopy: true);
                string serverData = AddressablesContentUpdateStep.Execute(ctx, ContentUpdateScope.CodeOnly);
                ManifestWriteStep.Execute(
                    ctx,
                    ManifestWriteStep.CreatePatchEntry("code", ctx, serverData),
                    contentStatePath: null);
                return HotUpdateBuildResult.Succeeded(logs, serverData, null, HotUpdateManifest.ManifestPath);
            });
        }

        /// <summary>
        /// 执行资源热更打包。
        /// </summary>
        /// <param name="context">热更构建上下文。</param>
        /// <returns>构建结果。</returns>
        public static HotUpdateBuildResult RunResourcePatch(HotUpdateBuildContext context)
        {
            return RunInternal(context, (ctx, logs) =>
            {
                PreflightCheckStep.Execute(ctx);
                string changedGroupsSummary = AssetChangeSummaryStep.Execute(ctx);
                AddressablesSyncStep.Execute(ctx);
                string serverData = AddressablesContentUpdateStep.Execute(ctx, ContentUpdateScope.ResourceOnly);
                ManifestWriteStep.Execute(
                    ctx,
                    ManifestWriteStep.CreatePatchEntry("resource", ctx, serverData, changedGroupsCsv: changedGroupsSummary),
                    contentStatePath: null);
                return HotUpdateBuildResult.Succeeded(logs, serverData, null, HotUpdateManifest.ManifestPath);
            });
        }

        private static HotUpdateBuildResult RunInternal(
            HotUpdateBuildContext context,
            Func<HotUpdateBuildContext, List<string>, HotUpdateBuildResult> action)
        {
            if (_isRunning)
            {
                return HotUpdateBuildResult.Failed("已有构建任务正在运行，请等待完成后再试。", Array.Empty<string>());
            }

            List<string> logs = new List<string>();
            HotUpdateBuildContext wrappedContext = WrapContextWithLogCollector(context, logs);

            _isRunning = true;
            try
            {
                return action(wrappedContext, logs);
            }
            catch (OperationCanceledException ex)
            {
                return HotUpdateBuildResult.Failed($"构建已取消: {ex.Message}", logs);
            }
            catch (Exception ex)
            {
                wrappedContext.LogLine($"[Pipeline] 失败: {ex.Message}");
                return HotUpdateBuildResult.Failed(ex.Message, logs);
            }
            finally
            {
                _isRunning = false;
            }
        }

        private static HotUpdateBuildContext WrapContextWithLogCollector(
            HotUpdateBuildContext source,
            List<string> logs)
        {
            Progress<string> collector = new Progress<string>(message =>
            {
                logs.Add(message);
                source.Log.Report(message);
            });

            return new HotUpdateBuildContext(
                source.Mode,
                source.BuildTarget,
                source.Version,
                source.RemoteBaseUrl,
                source.DevelopmentBuild,
                collector,
                source.CancellationToken)
            {
                ResourceGroupFilter = source.ResourceGroupFilter,
                SkipPlayerBuild = source.SkipPlayerBuild,
                ForceSkipAotChangeCheck = source.ForceSkipAotChangeCheck,
            };
        }

        private static string ResolveLatestContentStatePath()
        {
            return HotUpdateContentStatePathUtility.ResolveAfterFullBuild();
        }
    }
}
