using Cysharp.Threading.Tasks;
using Rogue.Bootstrap.HotUpdate;
using UnityEngine;

namespace Rogue
{
    public partial class GameEntry
    {
        /// <summary>
        /// 在正式组件初始化之前执行代码热更加载。
        /// </summary>
        private async UniTask LoadHotUpdateAssembliesAsync()
        {
            GameHotUpdateEntryComponent entryComponent = GetComponent<GameHotUpdateEntryComponent>();
            if (entryComponent == null)
            {
                GameLog.Warn(GameLogChannel.Bootstrap,
                    "未找到 GameHotUpdateEntryComponent，跳过代码热更加载。");
                return;
            }

            await entryComponent.RunAsync();
        }
    }
}
