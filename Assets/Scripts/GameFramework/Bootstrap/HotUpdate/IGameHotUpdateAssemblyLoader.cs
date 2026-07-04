using System.Reflection;
using Cysharp.Threading.Tasks;

namespace Rogue.Bootstrap.HotUpdate
{
    /// <summary>
    /// 热更新程序集加载策略接口。
    /// </summary>
    public interface IGameHotUpdateAssemblyLoader
    {
        /// <summary>
        /// 加载热更新主程序集。
        /// </summary>
        UniTask<Assembly> LoadAsync(GameHotUpdateEntryOptions options);
    }
}
