using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace mRemoteNG.App.Update
{
    [SupportedOSPlatform("windows")]
    internal static class PortableUpdateInstaller
    {
        internal const string ApplyUpdateArgument = "--apply-portable-update";

        private const string ExpectedProductName = "mRemoteNG Connection Manager";
        private const string CleanupSourceEnvironmentVariable = "MREMOTENG_UPDATE_SOURCE";
        private const string CleanupBackupEnvironmentVariable = "MREMOTENG_UPDATE_BACKUP";
        private const string CleanupProcessEnvironmentVariable = "MREMOTENG_UPDATE_PROCESS_ID";
        private const int ProcessExitTimeoutMilliseconds = 120_000;

        internal static string UpdateDirectory =>
            Path.Combine(Path.GetTempPath(), "mRemoteNG", "updates");

        internal static string CreateDownloadPath(string fileName)
        {
            string safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeFileName) ||
                !safeFileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                safeFileName = "mRemoteNG.exe";
            }

            Directory.CreateDirectory(UpdateDirectory);
            return Path.Combine(UpdateDirectory, $"{Guid.NewGuid():N}-{safeFileName}");
        }

        internal static bool TryRun(string[] args)
        {
            if (args.Length == 0 ||
                !args[0].Equals(ApplyUpdateArgument, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                if (!TryParseApplyArguments(args, out string targetPath, out int processId))
                {
                    throw new ArgumentException("The portable update arguments are invalid.");
                }

                string sourcePath = GetCurrentExecutablePath();
                ApplyUpdate(sourcePath, targetPath, processId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"mRemoteNG could not install the downloaded update.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    "mRemoteNG Update",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return true;
        }

        internal static bool TryParseApplyArguments(string[] args, out string targetPath, out int processId)
        {
            targetPath = string.Empty;
            processId = 0;

            if (args.Length != 3 ||
                !args[0].Equals(ApplyUpdateArgument, StringComparison.Ordinal) ||
                !int.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out processId) ||
                processId <= 0)
            {
                return false;
            }

            try
            {
                targetPath = Path.GetFullPath(args[1]);
                return targetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                targetPath = string.Empty;
                processId = 0;
                return false;
            }
        }

        internal static void Start(string updateFilePath)
        {
            string sourcePath = Path.GetFullPath(updateFilePath);
            string targetPath = GetCurrentExecutablePath();

            ValidateUpdateExecutable(sourcePath);
            ValidateInstalledExecutable(targetPath);

            ProcessStartInfo startInfo = new(sourcePath)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(targetPath) ?? AppContext.BaseDirectory
            };
            startInfo.ArgumentList.Add(ApplyUpdateArgument);
            startInfo.ArgumentList.Add(targetPath);
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

            if (Process.Start(startInfo) == null)
            {
                throw new InvalidOperationException("The portable update process could not be started.");
            }
        }

        internal static void CompletePendingCleanup()
        {
            string? sourcePath = Environment.GetEnvironmentVariable(CleanupSourceEnvironmentVariable);
            string? backupPath = Environment.GetEnvironmentVariable(CleanupBackupEnvironmentVariable);
            string? updaterProcessId = Environment.GetEnvironmentVariable(CleanupProcessEnvironmentVariable);

            if (string.IsNullOrWhiteSpace(sourcePath) &&
                string.IsNullOrWhiteSpace(backupPath) &&
                string.IsNullOrWhiteSpace(updaterProcessId))
            {
                return;
            }

            if (int.TryParse(updaterProcessId, NumberStyles.None, CultureInfo.InvariantCulture, out int processId))
            {
                try
                {
                    WaitForProcessExit(processId, 10_000, validateTargetPath: null);
                }
                catch
                {
                    // The updated application must still start if cleanup cannot wait for the updater.
                }
            }

            string currentExecutable = GetCurrentExecutablePath();
            string expectedBackupPath = GetBackupPath(currentExecutable);

            if (!string.IsNullOrWhiteSpace(backupPath) &&
                PathsEqual(backupPath, expectedBackupPath))
            {
                TryDeleteFile(expectedBackupPath);
            }

            if (!string.IsNullOrWhiteSpace(sourcePath) &&
                IsPathWithinDirectory(sourcePath, UpdateDirectory))
            {
                TryDeleteFile(sourcePath);
                TryDeleteEmptyUpdateDirectory();
            }

            Environment.SetEnvironmentVariable(CleanupSourceEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(CleanupBackupEnvironmentVariable, null);
            Environment.SetEnvironmentVariable(CleanupProcessEnvironmentVariable, null);
        }

        internal static void ReplaceExecutableFiles(
            string sourcePath,
            string targetPath,
            string backupPath,
            string stagingPath)
        {
            TryDeleteFile(stagingPath);
            TryDeleteFile(backupPath);

            File.Copy(sourcePath, stagingPath, overwrite: true);
            File.Replace(stagingPath, targetPath, backupPath, ignoreMetadataErrors: true);
        }

        internal static bool IsPathWithinDirectory(string path, string directory)
        {
            try
            {
                string fullPath = Path.GetFullPath(path);
                string fullDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) +
                                       Path.DirectorySeparatorChar;
                return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return false;
            }
        }

        private static void ApplyUpdate(string sourcePath, string targetPath, int processId)
        {
            ValidateUpdateExecutable(sourcePath);
            ValidateInstalledExecutable(targetPath);
            WaitForProcessExit(processId, ProcessExitTimeoutMilliseconds, targetPath);

            string backupPath = GetBackupPath(targetPath);
            string stagingPath = targetPath + ".update-staging";

            try
            {
                ReplaceExecutableFiles(sourcePath, targetPath, backupPath, stagingPath);
                StartUpdatedApplication(targetPath, sourcePath, backupPath);
            }
            catch
            {
                TryDeleteFile(stagingPath);
                RestoreBackup(targetPath, backupPath);
                TryStartApplication(targetPath);
                throw;
            }
        }

        private static void StartUpdatedApplication(string targetPath, string sourcePath, string backupPath)
        {
            ProcessStartInfo startInfo = new(targetPath)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(targetPath) ?? AppContext.BaseDirectory
            };
            startInfo.Environment[CleanupSourceEnvironmentVariable] = sourcePath;
            startInfo.Environment[CleanupBackupEnvironmentVariable] = backupPath;
            startInfo.Environment[CleanupProcessEnvironmentVariable] =
                Environment.ProcessId.ToString(CultureInfo.InvariantCulture);

            if (Process.Start(startInfo) == null)
            {
                throw new InvalidOperationException("The updated mRemoteNG executable could not be started.");
            }
        }

        private static void WaitForProcessExit(int processId, int timeoutMilliseconds, string? validateTargetPath)
        {
            try
            {
                using Process process = Process.GetProcessById(processId);
                if (!string.IsNullOrWhiteSpace(validateTargetPath))
                {
                    string? processPath = process.MainModule?.FileName;
                    if (string.IsNullOrWhiteSpace(processPath) ||
                        !Path.GetFullPath(processPath).Equals(
                            Path.GetFullPath(validateTargetPath),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "The process that requested the update does not match the installed mRemoteNG executable.");
                    }
                }

                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    throw new TimeoutException("mRemoteNG did not exit before the update timeout expired.");
                }
            }
            catch (ArgumentException)
            {
                // The process already exited before the updater opened its handle.
            }
        }

        private static void ValidateUpdateExecutable(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The downloaded update executable does not exist.", path);
            }

            if (!IsPathWithinDirectory(path, UpdateDirectory))
            {
                throw new InvalidOperationException("The downloaded update is outside the trusted update directory.");
            }

            ValidateProductName(path);
        }

        private static void ValidateInstalledExecutable(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The installed mRemoteNG executable does not exist.", path);
            }

            ValidateProductName(path);
        }

        private static void ValidateProductName(string path)
        {
            string? productName = FileVersionInfo.GetVersionInfo(path).ProductName;
            if (!ExpectedProductName.Equals(productName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"'{path}' is not an mRemoteNG executable.");
            }
        }

        private static string GetCurrentExecutablePath()
        {
            string? processPath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(processPath))
            {
                throw new InvalidOperationException("The current executable path could not be determined.");
            }

            return Path.GetFullPath(processPath);
        }

        private static string GetBackupPath(string targetPath)
        {
            return targetPath + ".update-backup";
        }

        private static bool PathsEqual(string firstPath, string secondPath)
        {
            try
            {
                return Path.GetFullPath(firstPath).Equals(
                    Path.GetFullPath(secondPath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return false;
            }
        }

        private static void RestoreBackup(string targetPath, string backupPath)
        {
            if (!File.Exists(backupPath))
            {
                return;
            }

            if (File.Exists(targetPath))
            {
                File.Replace(backupPath, targetPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(backupPath, targetPath);
            }
        }

        private static void TryStartApplication(string targetPath)
        {
            try
            {
                Process.Start(new ProcessStartInfo(targetPath)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(targetPath) ?? AppContext.BaseDirectory
                });
            }
            catch
            {
                // The original exception is more useful to the user.
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Cleanup is best effort; a later launch can retry.
            }
        }

        private static void TryDeleteEmptyUpdateDirectory()
        {
            try
            {
                if (Directory.Exists(UpdateDirectory) &&
                    Directory.GetFileSystemEntries(UpdateDirectory).Length == 0)
                {
                    Directory.Delete(UpdateDirectory);
                }
            }
            catch
            {
                // Cleanup is best effort.
            }
        }
    }
}
