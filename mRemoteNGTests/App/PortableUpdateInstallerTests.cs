using System;
using System.IO;
using mRemoteNG.App.Update;
using NUnit.Framework;

namespace mRemoteNGTests.App;

[TestFixture]
public class PortableUpdateInstallerTests
{
    [Test]
    public void TryParseApplyArgumentsAcceptsExpectedArguments()
    {
        string target = Path.Combine(Path.GetTempPath(), "mRemoteNG.exe");
        string[] args = [PortableUpdateInstaller.ApplyUpdateArgument, target, "42"];

        bool parsed = PortableUpdateInstaller.TryParseApplyArguments(
            args,
            out string parsedTarget,
            out int processId);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(parsedTarget, Is.EqualTo(Path.GetFullPath(target)));
            Assert.That(processId, Is.EqualTo(42));
        });
    }

    [TestCaseSource(nameof(InvalidArgumentCases))]
    public void TryParseApplyArgumentsRejectsInvalidArguments(string[] args)
    {
        bool parsed = PortableUpdateInstaller.TryParseApplyArguments(args, out _, out _);

        Assert.That(parsed, Is.False);
    }

    [Test]
    public void CreateDownloadPathStaysInsideUpdateDirectory()
    {
        string downloadPath = PortableUpdateInstaller.CreateDownloadPath(@"..\mRemoteNG.exe");

        Assert.Multiple(() =>
        {
            Assert.That(
                PortableUpdateInstaller.IsPathWithinDirectory(
                    downloadPath,
                    PortableUpdateInstaller.UpdateDirectory),
                Is.True);
            Assert.That(Path.GetFileName(downloadPath), Does.EndWith("-mRemoteNG.exe"));
        });
    }

    [Test]
    public void IsPathWithinDirectoryRejectsInvalidPath()
    {
        Assert.That(
            PortableUpdateInstaller.IsPathWithinDirectory("\0", PortableUpdateInstaller.UpdateDirectory),
            Is.False);
    }

    [Test]
    public void ReplaceExecutableFilesKeepsRollbackBackup()
    {
        string testDirectory = Path.Combine(
            Path.GetTempPath(),
            "mRemoteNG-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);

        string sourcePath = Path.Combine(testDirectory, "source.exe");
        string targetPath = Path.Combine(testDirectory, "target.exe");
        string backupPath = targetPath + ".update-backup";
        string stagingPath = targetPath + ".update-staging";

        try
        {
            File.WriteAllText(sourcePath, "new");
            File.WriteAllText(targetPath, "old");

            PortableUpdateInstaller.ReplaceExecutableFiles(
                sourcePath,
                targetPath,
                backupPath,
                stagingPath);

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllText(targetPath), Is.EqualTo("new"));
                Assert.That(File.ReadAllText(backupPath), Is.EqualTo("old"));
                Assert.That(File.Exists(stagingPath), Is.False);
            });
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    private static object[] InvalidArgumentCases =
    [
        Array.Empty<string>(),
        new[] { "--other", "mRemoteNG.exe", "42" },
        new[] { PortableUpdateInstaller.ApplyUpdateArgument, "mRemoteNG.exe" },
        new[] { PortableUpdateInstaller.ApplyUpdateArgument, "mRemoteNG.exe", "not-a-process" },
        new[] { PortableUpdateInstaller.ApplyUpdateArgument, "mRemoteNG.txt", "42" }
    ];
}
