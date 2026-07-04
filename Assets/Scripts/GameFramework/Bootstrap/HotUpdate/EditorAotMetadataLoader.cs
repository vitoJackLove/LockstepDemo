using Cysharp.Threading.Tasks;

namespace Rogue.Bootstrap.HotUpdate
{
    /// <summary>
    /// 编辑器环境下的 AOT 元数据加载策略，跳过磁盘读取。
    /// </summary>
    public sealed class EditorAotMetadataLoader : IGameAotMetadataLoader
    {
        public UniTask<int> LoadAsync(GameHotUpdateEntryOptions options)
        {
            GameLog.Info(GameLogChannel.Bootstrap, "编辑器跳过 AOT 元数据文件加载。");
            return UniTask.FromResult(0);
        }
    }
}
