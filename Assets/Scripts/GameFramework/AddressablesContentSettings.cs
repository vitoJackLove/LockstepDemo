using UnityEngine;

/// <summary>
/// Addressables 内容分发与 Catalog 检查配置。
/// </summary>
[CreateAssetMenu(fileName = "AddressablesContentSettings", menuName = "Game/Addressables Content Settings")]
public class AddressablesContentSettings : ScriptableObject
{
    public const string DefaultAssetPath = "Assets/GameAssetConfig/AddressablesContentSettings.asset";
    /// <summary>
    /// 默认本地热更 HTTP 端口。避开 8080（Unity MCP for Cursor 常用该端口）。
    /// </summary>
    public const string DefaultLocalDevBaseUrl = "http://127.0.0.1:8765";
    public const int DefaultLocalDevHttpPort = 8765;

    [Tooltip("默认远程资源根 URL，不含 BuildTarget。例如 https://cdn.example.com/addressables")]
    public string DefaultRemoteBaseUrl = DefaultLocalDevBaseUrl;

    [Tooltip("启动时是否检查 Catalog 更新")]
    public bool EnableCatalogUpdateOnStartup = true;

    [Tooltip("为 true 时仅使用本地首包资源，不检查远程 Catalog")]
    public bool ForceLocalOnly = false;

    [Tooltip("Catalog 检查/更新超时（秒），超时后回退本地")]
    public float CatalogUpdateTimeoutSeconds = 10f;

    [Tooltip("PreBootstrap 阶段是否检查 Catalog 更新（含热更代码与资源）")]
    public bool EnableHotUpdateCatalogCheck = true;

    [Tooltip("batchmode（CI）下跳过 Catalog 检查")]
    public bool SkipCatalogCheckInBatchMode = true;

    [Tooltip("Editor Play 时优先使用的本地热更根 URL，不含 BuildTarget")]
    public string LocalDevRemoteBaseUrl = DefaultLocalDevBaseUrl;

    [Tooltip("本地热更 HTTP 静态服务端口，对应 ServerData 目录")]
    public int LocalDevHttpPort = DefaultLocalDevHttpPort;

    [Tooltip("Editor Play 时使用 LocalDevRemoteBaseUrl 拉取远程 Catalog")]
    public bool UseLocalDevRemoteInEditor = true;
}
