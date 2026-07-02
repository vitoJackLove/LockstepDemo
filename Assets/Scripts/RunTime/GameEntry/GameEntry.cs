using UnityEngine;

namespace Rogue
{
    /// <summary>
    /// 游戏入口。
    /// </summary>
    public partial class GameEntry : MonoBehaviour
    {
        private async void Start()
        {
            await this.Init();
        }

        async Cysharp.Threading.Tasks.UniTask Init()
        {
            //Time.fixedDeltaTime = fpmath1.LogicDeltaTimeFloat;
            //Time.maximumDeltaTime = fpmath1.LogicDeltaTimeFloat * 5f;
            await InitOptionalComponent();
            GameEntry.UI.AddUIGroup(Content.UI.UIDefaultGroup);
            GameEntry.UI.AddUIGroup(Content.UI.UILoadingGroup);
            this.InitService();
        }

        void InitService()
        {
            //Screen.SetResolution(1080, 1920, FullScreenMode.Windowed);
            this.OpenStartForm();
        }

        /// <summary>
        /// 打开开始界面
        /// </summary>
        async void OpenStartForm()
        {
           await UI.OpenUIWindow<GameStartUpWindow>(AssetsPathHelper.UIWindowPathHelper($"GameStartUp"), Content.UI.UIDefaultGroup,
                null, null);
        }

        private void Update()
        {
            Game.Update();
        }

        private void FixedUpdate()
        {
            Game.FixedUpdate();
        }

        private void LateUpdate()
        {
            Game.LateUpdate();
        }

        private void OnDestroy()
        {
            Game.Close();
        }

        private void OnApplicationQuit()
        {
            Game.Close();
        }
    }
}
