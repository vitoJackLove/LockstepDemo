using System.Reflection;

namespace Rogue.Bootstrap.HotUpdate
{
    /// <summary>
    /// 代码热更加载结果。
    /// </summary>
    public sealed class GameHotUpdateLoadResult
    {
        public GameHotUpdateLoadResult(int loadedAotAssemblyCount, Assembly hotUpdateAssembly, bool usedEditorAssembly)
        {
            LoadedAotAssemblyCount = loadedAotAssemblyCount;
            HotUpdateAssembly = hotUpdateAssembly;
            UsedEditorAssembly = usedEditorAssembly;
        }

        /// <summary>
        /// 成功补充元数据的 AOT 程序集数量。
        /// </summary>
        public int LoadedAotAssemblyCount { get; }

        /// <summary>
        /// 最终可用的热更新程序集。
        /// </summary>
        public Assembly HotUpdateAssembly { get; }

        /// <summary>
        /// 是否直接复用了编辑器已加载的程序集。
        /// </summary>
        public bool UsedEditorAssembly { get; }
    }
}
