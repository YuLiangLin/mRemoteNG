using System;
using mRemoteNG.App.Info;
using NUnit.Framework;

namespace mRemoteNGTests.App.Info;

[TestFixture]
public class GeneralAppInfoTests
{
    [TestCase("1.78.2 (Nightly Build 3630) x64", "1.78.2.3630")]
    [TestCase("1.78.3-yll.3 x64", "1.78.3.3")]
    [TestCase("1.78.3-yll.3+72ecec0 x64", "1.78.3.3")]
    [TestCase("1.78.3.2", "1.78.3.2")]
    public void ParseApplicationVersionRecognizesSupportedFormats(string productVersion, string expectedVersion)
    {
        Assert.That(
            GeneralAppInfo.ParseApplicationVersion(productVersion),
            Is.EqualTo(Version.Parse(expectedVersion)));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not-a-version")]
    public void ParseApplicationVersionRejectsInvalidValues(string? productVersion)
    {
        Assert.That(GeneralAppInfo.ParseApplicationVersion(productVersion), Is.Null);
    }

    [Test]
    public void ForkReleaseWithNewBaseVersionUpdatesPreviousNightlyBuild()
    {
        Version? installedVersion =
            GeneralAppInfo.ParseApplicationVersion("1.78.2 (Nightly Build 3630) x64");

        Assert.That(new Version(1, 78, 3, 2), Is.GreaterThan(installedVersion));
    }
}
