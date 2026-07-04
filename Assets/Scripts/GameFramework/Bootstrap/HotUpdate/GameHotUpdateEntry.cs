using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Rogue;
using Rogue.Network;
using UnityEngine;

namespace Rogue.Bootstrap.HotUpdate
{
    /// <summary>
    /// 代码热更入口门面，使用模板方法组织加载流程。
    /// </summary>
    public sealed class GameHotUpdateEntry : IGameHotUpdateEntry
    {
        private readonly IGameAotMetadataLoader _aotMetadataLoader;
        private readonly IGameHotUpdateAssemblyLoader _hotUpdateAssemblyLoader;
        private readonly bool _usedEditorAssembly;

        public GameHotUpdateEntry(
            IGameAotMetadataLoader aotMetadataLoader,
            IGameHotUpdateAssemblyLoader hotUpdateAssemblyLoader,
            bool usedEditorAssembly)
        {
            _aotMetadataLoader = aotMetadataLoader ?? throw new ArgumentNullException(nameof(aotMetadataLoader));
            _hotUpdateAssemblyLoader = hotUpdateAssemblyLoader ??
                                       throw new ArgumentNullException(nameof(hotUpdateAssemblyLoader));
            _usedEditorAssembly = usedEditorAssembly;
        }

        /// <summary>
        /// 模板方法：先补 AOT 元数据，再加载热更程序集，最后校验 Bootstrap。
        /// </summary>
        public async UniTask<GameHotUpdateLoadResult> RunAsync(GameHotUpdateEntryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            GameLog.Info(GameLogChannel.Bootstrap, "开始执行代码热更加载流程。");

            int loadedAotCount = await _aotMetadataLoader.LoadAsync(options);
            var hotUpdateAssembly = await _hotUpdateAssemblyLoader.LoadAsync(options);

            if (!Application.isEditor && GameEntry.HotUpdateBootstrap == null)
            {
                ActivateHotUpdateServices(hotUpdateAssembly);
            }

            EnsureBootstrapRegistered();

            GameLog.Info(GameLogChannel.Bootstrap,
                $"代码热更加载完成。AOT={loadedAotCount}, Assembly={hotUpdateAssembly.GetName().Name}");

            return new GameHotUpdateLoadResult(loadedAotCount, hotUpdateAssembly, _usedEditorAssembly);
        }

        private static void EnsureBootstrapRegistered()
        {
            if (GameEntry.HotUpdateBootstrap != null)
            {
                return;
            }

            throw new InvalidOperationException(
                "热更新程序集已加载，但 IGameHotUpdateBootstrap 尚未注册。请检查 HotUpdateRuntimeInitialize。");
        }

        /// <summary>
        /// Player 下 Assembly.Load 后不会触发 BeforeSceneLoad，需直接实例化热更类型完成注册。
        /// </summary>
        private static void ActivateHotUpdateServices(Assembly hotUpdateAssembly)
        {
            GameLog.Info(GameLogChannel.Bootstrap,
                $"Player 模式开始注册热更服务，程序集={hotUpdateAssembly.GetName().Name}");

            if (GameEntry.HotUpdateBootstrap == null)
            {
                GameEntry.HotUpdateBootstrap = CreateHotUpdateInstance<IGameHotUpdateBootstrap>(
                    hotUpdateAssembly,
                    "Rogue.GameHotUpdateBootstrap");
                GameLog.Info(GameLogChannel.Bootstrap, "IGameHotUpdateBootstrap 注册完成。");
            }

            if (NetworkPacketCodecProvider.Instance == null)
            {
                NetworkPacketCodecProvider.Instance = CreateHotUpdateInstance<INetworkPacketCodec>(
                    hotUpdateAssembly,
                    "Rogue.Network.NetworkProtobufCodec");
                GameLog.Info(GameLogChannel.Bootstrap, "INetworkPacketCodec 注册完成。");
            }
        }

        private static T CreateHotUpdateInstance<T>(Assembly assembly, string typeFullName) where T : class
        {
            Type type = assembly.GetType(typeFullName);
            if (type == null)
            {
                foreach (Type exportedType in assembly.GetExportedTypes())
                {
                    if (exportedType.FullName == typeFullName)
                    {
                        type = exportedType;
                        break;
                    }
                }
            }

            if (type == null)
            {
                throw new InvalidOperationException(
                    $"热更新程序集 {assembly.GetName().Name} 中未找到类型: {typeFullName}");
            }

            if (!typeof(T).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(
                    $"类型 {typeFullName} 未实现 {typeof(T).FullName}");
            }

            object instance = Activator.CreateInstance(type);
            if (instance is not T typedInstance)
            {
                throw new InvalidOperationException(
                    $"无法将 {typeFullName} 转换为 {typeof(T).FullName}");
            }

            return typedInstance;
        }

    }
}
