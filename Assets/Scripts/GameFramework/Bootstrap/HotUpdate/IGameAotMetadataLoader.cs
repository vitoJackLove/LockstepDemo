using Cysharp.Threading.Tasks;

namespace Rogue.Bootstrap.HotUpdate
{
    /// <summary>
    /// AOT 元数据加载策略接口。
    /// </summary>
    public interface IGameAotMetadataLoader
    {
        /// <summary>
        /// 为 AOT 程序集补充元数据。
        /// </summary>
        UniTask<int> LoadAsync(GameHotUpdateEntryOptions options);
    }
}
