using NUnit.Framework;
using Rogue.Editor.HotUpdate.Pipeline;
using Rogue.Editor.HotUpdate.Pipeline.Steps;

public class AotChangeDetectorStepTests
{
    [TearDown]
    public void TearDown()
    {
        HotUpdateManifest.ResetManifestPathForTests();
    }

    [Test]
    public void HasAotChanges_WhenNoManifest_ReturnsFalse()
    {
        HotUpdateManifest.ResetManifestPathForTests();
        Assert.IsFalse(AotChangeDetectorStep.HasAotChanges());
    }
}
