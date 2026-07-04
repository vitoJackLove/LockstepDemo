using System;
using Rogue.Editor.HotUpdate.Pipeline.Steps;

/// <summary>
/// 遗留 Addressables 同步入口，转发至热更 Pipeline 的 AddressablesSyncStep。
/// </summary>
public static class RuntimeAddressablesConfigurator
{
    [Obsolete("请使用 Tools/发布/热更发布中心。此方法保留供内容工厂工具调用。")]
    public static void SyncRuntimeAssets()
    {
        AddressablesSyncStep.ExecuteSyncOnly();
    }
}
