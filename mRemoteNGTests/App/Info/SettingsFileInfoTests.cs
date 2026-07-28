using System.IO;
using mRemoteNG.App.Info;
using NUnit.Framework;

namespace mRemoteNGTests.App.Info;

[TestFixture]
public class SettingsFileInfoTests
{
    [Test]
    public void ResolveExecutableDirectoryPrefersSingleFileHostPath()
    {
        string extractionDirectory = Path.Combine(
            Path.GetTempPath(),
            ".net",
            "mRemoteNG",
            "bundle-hash");
        string portableDirectory = Path.Combine(Path.GetTempPath(), "mRemoteNG-portable");
        string executablePath = Path.Combine(portableDirectory, "mRemoteNG.exe");

        string result = SettingsFileInfo.ResolveExecutableDirectory(
            executablePath,
            extractionDirectory);

        Assert.That(result, Is.EqualTo(portableDirectory));
    }

    [TestCase(null)]
    [TestCase("")]
    public void ResolveExecutableDirectoryFallsBackToBaseDirectory(string processPath)
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), "mRemoteNG") +
                               Path.DirectorySeparatorChar;

        string result = SettingsFileInfo.ResolveExecutableDirectory(
            processPath,
            baseDirectory);

        Assert.That(
            result,
            Is.EqualTo(Path.TrimEndingDirectorySeparator(baseDirectory)));
    }
}
