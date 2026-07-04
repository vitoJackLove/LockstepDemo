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

        private async Cysharp.Threading.Tasks.UniTask PreBootstrapAddressablesAsync()
        {
            AddressablesBootstrapComponent bootstrap =
                GameEntryRunTime.GetComponent<AddressablesBootstrapComponent>();
            if (bootstrap == null)
            {
                GameLog.Warn(GameLogChannel.Bootstrap,
                    "未找到 AddressablesBootstrapComponent，PreBootstrap 跳过。");
                return;
            }

            await bootstrap.PreBootstrapAsync();
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

            AddressablesBootstrapComponent addressablesBootstrap =
                GameEntryRunTime.GetComponent<AddressablesBootstrapComponent>();
            if (addressablesBootstrap != null)
            {
                await addressablesBootstrap.InitializeAsync();
            }

            UI.Init();
            Scene.Init();
            await DataTable.InitAsync();
            Observer.Init();
            Camera.Init();
            Canvas.Init();
            
            ConfigureTransport(new TcpFrameSyncTransport(TcpClient));
        }

        /// <summary>
        /// 切换帧同步传输层，联机走 TCP，单机走本地回环。
        /// </summary>
        public static void ConfigureTransport(IFrameSyncTransport transport)
        {
            if (transport == null)
            {
                return;
            }

            FrameSyncTransport?.Shutdown();
            FrameSyncTransport = transport;
            FrameSyncTransport.Initialize();
        }
    }
}

