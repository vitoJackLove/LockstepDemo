using NUnit.Framework;

public class HotUpdateAddressablesResolverTests
{
    [Test]
    public void ResolveGroupName_AotMetadata_ReturnsHotUpdateCodeLocal()
    {
        Assert.AreEqual(
            AddressablesGroupResolver.HotUpdateCodeLocal,
            AddressablesGroupResolver.ResolveGroupName(
                "Assets/HotUpdate/Code/AOT/mscorlib.dll.bytes"));
    }

    [Test]
    public void ResolveGroupName_RuntimeDll_ReturnsHotUpdateCodeRemote()
    {
        Assert.AreEqual(
            AddressablesGroupResolver.HotUpdateCodeRemote,
            AddressablesGroupResolver.ResolveGroupName(
                "Assets/HotUpdate/Code/Game.Runtime.dll.bytes"));
    }

    [Test]
    public void ResolveGroupName_Manifest_ReturnsHotUpdateCodeRemote()
    {
        Assert.AreEqual(
            AddressablesGroupResolver.HotUpdateCodeRemote,
            AddressablesGroupResolver.ResolveGroupName(
                "Assets/HotUpdate/Code/manifest.json"));
    }
}
