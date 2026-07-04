using System;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;

namespace Rogue.Bootstrap.HotUpdate
{
    /// <summary>
    /// 编辑器环境下的热更新程序集加载策略，直接复用 Unity 已加载的程序集。
    /// </summary>
    public sealed class EditorHotUpdateAssemblyLoader : IGameHotUpdateAssemblyLoader
    {
        public UniTask<Assembly> LoadAsync(GameHotUpdateEntryOptions options)
        {
            Assembly editorAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == options.HotUpdateAssemblyName);

            if (editorAssembly == null)
            {
                throw new InvalidOperationException(
                    $"编辑器中未找到热更新程序集: {options.HotUpdateAssemblyName}");
            }

            GameLog.Info(GameLogChannel.Bootstrap,
                $"编辑器复用热更新程序集: {editorAssembly.FullName}");
            return UniTask.FromResult(editorAssembly);
        }
    }
}
