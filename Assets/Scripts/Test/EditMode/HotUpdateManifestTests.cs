using System.IO;
using NUnit.Framework;
using Rogue.Editor.HotUpdate.Pipeline;
using UnityEngine;

public class HotUpdateManifestTests
{
    private string _tempDir;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Application.temporaryCachePath, "HotUpdateManifestTests");
        Directory.CreateDirectory(_tempDir);
        HotUpdateManifest.SetManifestPathForTests(Path.Combine(_tempDir, "HotUpdateManifest.json"));
    }

    [TearDown]
    public void TearDown()
    {
        HotUpdateManifest.ResetManifestPathForTests();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Test]
    public void SaveAndLoad_RoundTripsContentStatePath()
    {
        var manifest = new HotUpdateManifestData
        {
            AppVersion = "1.0.0",
            ContentStatePath = "C:/fake/content_state.bin",
        };

        HotUpdateManifest.Save(manifest);
        HotUpdateManifestData loaded = HotUpdateManifest.Load();

        Assert.AreEqual("1.0.0", loaded.AppVersion);
        Assert.AreEqual("C:/fake/content_state.bin", loaded.ContentStatePath);
    }

    [Test]
    public void TryResolveContentStatePath_UsesManifestFirst()
    {
        HotUpdateManifest.Save(new HotUpdateManifestData
        {
            ContentStatePath = Path.Combine(_tempDir, "from_manifest.bin"),
        });
        File.WriteAllText(Path.Combine(_tempDir, "from_manifest.bin"), "x");

        Assert.IsTrue(HotUpdateManifest.TryResolveContentStatePath(out string path));
        Assert.AreEqual(Path.Combine(_tempDir, "from_manifest.bin"), path);
    }
}
