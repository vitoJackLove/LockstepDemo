using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 游戏入口。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public partial class GameEntry : MonoBehaviour
    {
        /// <summary>
        /// 热更层启动回调，由 Game.Runtime 在 RuntimeInitializeOnLoad 时注册。
        /// </summary>
        public static IGameHotUpdateBootstrap HotUpdateBootstrap { get; set; }

        private async void Start()
        {
            await Init();
        }

        private async Cysharp.Threading.Tasks.UniTask Init()
        {
            await PreBootstrapAddressablesAsync();
            await LoadHotUpdateAssembliesAsync();
            await InitOptionalComponent();
            HotUpdateBootstrap?.RegisterUiGroups();
            InitService();
        }

        private void InitService()
        {
            if (ClientAgentGameEntryMode.IsRunning)
            {
                return;
            }

            HotUpdateBootstrap?.OpenStartForm();
        }

        private void Update()
        {
            HotUpdateBootstrap?.Update();
        }

        private void FixedUpdate()
        {
            HotUpdateBootstrap?.FixedUpdate();
        }

        private void LateUpdate()
        {
            HotUpdateBootstrap?.LateUpdate();
        }

        private void OnDestroy()
        {
            HotUpdateBootstrap?.Close();
        }

        private void OnApplicationQuit()
        {
            HotUpdateBootstrap?.Close();
        }
    }
}
