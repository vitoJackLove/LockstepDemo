using System;
using UnityEngine;

namespace Rogue
{
    public partial class GameEntry :MonoBehaviour
    {
        public static GameSettingComponent GameSetting
        {
            get;
            private set;
        }
        
        public static ResourceComponent Resource
        {
            get;
            private set;
        }
        
        public static UIComponent UI
        {
            get;
            private set;
        }

        public static SceneComponent Scene
        {
            get; 
            private set; 
        }
        
        public static DataTableComponent DataTable
        {
            get; 
            private set; 
        }
        
        public static FrameSyncClientComponent TcpClient
        {
            get; 
            private set; 
        }

        /// <summary>
        /// 当前帧同步传输层，联机走 TCP，单机走本地回环。
        /// </summary>
        public static IFrameSyncTransport FrameSyncTransport
        {
            get;
            private set;
        }

        /// <summary>
        /// 当前激活的会话配置，由 UI 切换模式时更新。
        /// </summary>
        public static IGameSessionProfile ActiveSessionProfile
        {
            get;
            private set;
        }
        
        public static ObserverComponent Observer
        {
            get; 
            private set; 
        }

        public static CameraComponent Camera
        {
            get; 
            private set; 
        }
        
        public static CanvasComponent Canvas
        {
            get; 
            private set; 
        }

        private async Cysharp.Threading.Tasks.UniTask InitOptionalComponent()
        {
            GameSetting = GameEntryRunTime.GetComponent<GameSettingComponent>();
            Resource = GameEntryRunTime.GetComponent<ResourceComponent>();
            UI = GameEntryRunTime.GetComponent<UIComponent>();
            Scene = GameEntryRunTime.GetComponent<SceneComponent>();
            DataTable = GameEntryRunTime.GetComponent<DataTableComponent>();
            Observer = GameEntryRunTime.GetComponent<ObserverComponent>();   
            TcpClient = GameEntryRunTime.GetComponent<FrameSyncClientComponent>();
            Camera = GameEntryRunTime.GetComponent<CameraComponent>();
            Canvas = GameEntryRunTime.GetComponent<CanvasComponent>();
            
            GameSetting.Init();
            Resource.Init();
            UI.Init();
            Scene.Init();
            await DataTable.InitAsync();
            Observer.Init();
            Camera.Init();
            Canvas.Init();
            
            ConfigureSession(GameSessionFactory.CreateOnline(TcpClient));
        }

        /// <summary>
        /// 切换会话模式并重建 Transport，单机模式下不会建立 TCP 连接。
        /// </summary>
        public static void ConfigureSession(GameSessionContext sessionContext)
        {
            if (sessionContext == null)
            {
                return;
            }

            FrameSyncTransport?.Shutdown();
            FrameSyncTransport = sessionContext.Transport;
            ActiveSessionProfile = sessionContext.Profile;
            FrameSyncTransport.Initialize();
        }
    }
}

