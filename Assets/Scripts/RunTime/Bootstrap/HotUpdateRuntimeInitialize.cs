using Rogue.Network;
using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 在热更程序集加载后注册启动回调。
    /// </summary>
    public static class HotUpdateRuntimeInitialize
    {
        /// <summary>
        /// Editor 下由 Unity 在 BeforeSceneLoad 自动调用；Player 下由 AOT 层在 Assembly.Load 后手动调用。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RegisterServices()
        {
            GameEntry.HotUpdateBootstrap = new GameHotUpdateBootstrap();
            NetworkPacketCodecProvider.Instance = new NetworkProtobufCodec();
        }
    }
}
