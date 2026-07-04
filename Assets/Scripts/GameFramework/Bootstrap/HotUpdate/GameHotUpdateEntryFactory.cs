namespace Rogue.Bootstrap.HotUpdate
{
    /// <summary>
    /// 代码热更入口工厂，根据运行环境组装策略组合。
    /// </summary>
    public static class GameHotUpdateEntryFactory
    {
        /// <summary>
        /// 创建当前平台可用的热更入口。
        /// </summary>
        public static IGameHotUpdateEntry Create()
        {
#if UNITY_EDITOR
            return new GameHotUpdateEntry(
                new EditorAotMetadataLoader(),
                new EditorHotUpdateAssemblyLoader(),
                usedEditorAssembly: true);
#else
            return new GameHotUpdateEntry(
                new AddressablesAotMetadataLoader(),
                new AddressablesHotUpdateAssemblyLoader(),
                usedEditorAssembly: false);
#endif
        }
    }
}
