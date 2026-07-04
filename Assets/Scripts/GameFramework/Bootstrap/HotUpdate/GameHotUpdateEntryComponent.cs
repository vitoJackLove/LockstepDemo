using Cysharp.Threading.Tasks;

using UnityEngine;



namespace Rogue.Bootstrap.HotUpdate

{

    /// <summary>

    /// 挂在 GameEntry 上的代码热更入口配置组件。

    /// </summary>

    [DisallowMultipleComponent]

    public sealed class GameHotUpdateEntryComponent : MonoBehaviour

    {

        [SerializeField]

        private bool enabledOnStart = true;



        [SerializeField]

        private string hotUpdateAssemblyName = "Game.Runtime";



        [SerializeField]

        private string hotUpdateAssemblyAddress = HotUpdateCodePaths.DefaultRuntimeAssemblyAddress;



        [SerializeField]

        private string aotMetadataLabel = HotUpdateCodePaths.DefaultAotMetadataLabel;



        [SerializeField]

        private bool autoDiscoverAotAssemblies = true;



        [SerializeField]

        private string[] aotAssemblyNames =

        {

            "mscorlib",

            "System",

            "System.Core",

            "UnityEngine.CoreModule",

        };



        private IGameHotUpdateEntry _entry;



        /// <summary>

        /// 是否启用代码热更加载。

        /// </summary>

        public bool EnabledOnStart => enabledOnStart;



        /// <summary>

        /// 执行热更加载。

        /// </summary>

        public UniTask<GameHotUpdateLoadResult> RunAsync()

        {

            if (!enabledOnStart)

            {

                GameLog.Info(GameLogChannel.Bootstrap, "代码热更入口已禁用，跳过加载。");

                return UniTask.FromResult<GameHotUpdateLoadResult>(null);

            }



            _entry ??= GameHotUpdateEntryFactory.Create();

            return _entry.RunAsync(CreateOptions());

        }



        private GameHotUpdateEntryOptions CreateOptions()

        {

            return new GameHotUpdateEntryOptions(

                hotUpdateAssemblyName,

                hotUpdateAssemblyAddress,

                aotMetadataLabel,

                autoDiscoverAotAssemblies,

                aotAssemblyNames);

        }

    }

}


