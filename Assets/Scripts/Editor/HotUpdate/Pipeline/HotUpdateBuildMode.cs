namespace Rogue.Editor.HotUpdate.Pipeline
{
    /// <summary>
    /// 热更发布构建模式。
    /// </summary>
    public enum HotUpdateBuildMode
    {
        FullPackage = 0,
        CodePatch = 1,
        ResourcePatch = 2,
    }
}
