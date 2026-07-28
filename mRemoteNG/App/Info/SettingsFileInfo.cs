using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Connection;

namespace mRemoteNG.App.Info
{
    [SupportedOSPlatform("windows")]
    public static class SettingsFileInfo
    {
        private static readonly string? AssemblyPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(ConnectionInfo))?.Location);
        private static readonly string ExecutablePath = ResolveExecutableDirectory(Environment.ProcessPath, AppContext.BaseDirectory);

        // Single-file deployments extract assemblies under %TEMP%\.net, while
        // Environment.ProcessPath continues to identify the portable executable.
        public static string SettingsPath => Runtime.IsPortableEdition ? ExecutablePath : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\" + Application.ProductName;

        public static string LayoutFileName { get; } = "pnlLayout.xml";
        public static string ExtAppsFilesName { get; } = "extApps.xml";
        public static string ThemesFileName { get; } = "Themes.xml";
        public static string LocalConnectionProperties { get; } = "LocalConnectionProperties.xml";

        public static string ThemeFolder { get; } =
            SettingsPath != null ? Path.Combine(SettingsPath, "Themes") : String.Empty;

        public static string InstalledThemeFolder { get; } =
            AssemblyPath != null ? Path.Combine(AssemblyPath, "Themes") : String.Empty;

        internal static string ResolveExecutableDirectory(string? processPath, string baseDirectory)
        {
            string? processDirectory = string.IsNullOrWhiteSpace(processPath)
                ? null
                : Path.GetDirectoryName(processPath);

            return string.IsNullOrWhiteSpace(processDirectory)
                ? Path.TrimEndingDirectorySeparator(Path.GetFullPath(baseDirectory))
                : processDirectory;
        }
    }
}
