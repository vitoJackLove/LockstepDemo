/// <summary>
/// 由服务器或会话层提供 Addressables 远程根 URL。
/// </summary>
public interface IAddressablesRemoteUrlProvider
{
    bool TryGetRemoteBaseUrl(out string baseUrl);
}
