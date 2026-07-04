namespace Rogue.Editor.HotUpdate.Pipeline.Steps
{
    /// <summary>
    /// Addressables Content Update 构建范围。
    /// </summary>
    public enum ContentUpdateScope
    {
        /// <summary>仅热更代码相关组（HotUpdate-Code-Remote）。</summary>
        CodeOnly = 0,

        /// <summary>资源热更组（排除 HotUpdate-Code-*）。</summary>
        ResourceOnly = 1,
    }
}
