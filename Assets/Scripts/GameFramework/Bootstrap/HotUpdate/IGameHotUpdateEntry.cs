using Cysharp.Threading.Tasks;

namespace Rogue.Bootstrap.HotUpdate
{
    /// <summary>
    /// 代码热更入口门面接口。
    /// </summary>
    public interface IGameHotUpdateEntry
    {
        /// <summary>
        /// 执行完整热更加载流程。
        /// </summary>
        UniTask<GameHotUpdateLoadResult> RunAsync(GameHotUpdateEntryOptions options);
    }
}
