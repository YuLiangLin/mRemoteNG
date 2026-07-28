using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using static System.Environment;


namespace mRemoteNG.App.Info
{
    [SupportedOSPlatform("windows")]
    public static class GeneralAppInfo
    {
        public const string UrlHome = "https://mremoteng.org";
        public const string UrlDonate = "https://mremoteng.org/contribute";
        public const string UrlForum = "https://github.com/orgs/mRemoteNG/discussions";
        public const string UrlChat = "https://app.element.io/#/room/#mremoteng:matrix.org";
        public const string UrlCommunity = "https://www.reddit.com/r/mRemoteNG";
        public const string UrlBugs = "https://github.com/mRemoteNG/mRemoteNG/issues/new";
        public const string UrlDocumentation = "https://mremoteng.readthedocs.io/en/latest/";
        public static readonly string ApplicationVersion = Application.ProductVersion;
        public static readonly string? ProductName = Application.ProductName;
        public static readonly string? Copyright = (Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute), false) as AssemblyCopyrightAttribute)?.Copyright;
        public static readonly string? HomePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

        //public static string ReportingFilePath = "";
        private static readonly string puttyPath = HomePath + "\\PuTTYNG.exe";

        public static string UserAgent
        {
            get
            {
                List<string> details =
                [
                    "compatible",
                    OSVersion.Platform == PlatformID.Win32NT
                        ? $"Windows NT {OSVersion.Version.Major}.{OSVersion.Version.Minor}"
                        : OSVersion.VersionString
                ];
                if (Is64BitProcess)
                {
                    details.Add("WOW64");
                }

                details.Add(Thread.CurrentThread.CurrentUICulture.Name);
                details.Add($".NET CLR {Environment.Version}");
                string detailsString = string.Join("; ", [.. details]);

                return $"Mozilla/5.0 ({detailsString}) {ProductName}/{ApplicationVersion}";
            }
        }

        public static string PuttyPath => puttyPath;

        public static Version? GetApplicationVersion() => ParseApplicationVersion(ApplicationVersion);

        internal static Version? ParseApplicationVersion(string? applicationVersion)
        {
            if (string.IsNullOrWhiteSpace(applicationVersion))
            {
                return null;
            }

            Match forkVersionMatch = Regex.Match(
                applicationVersion,
                @"(?<base>\d+\.\d+\.\d+)-yll\.(?<release>\d+)",
                RegexOptions.CultureInvariant);
            if (forkVersionMatch.Success &&
                System.Version.TryParse(
                    $"{forkVersionMatch.Groups["base"].Value}.{forkVersionMatch.Groups["release"].Value}",
                    out System.Version? forkVersion))
            {
                return forkVersion;
            }

            Match nightlyVersionMatch = Regex.Match(
                applicationVersion,
                @"(?<base>\d+\.\d+\.\d+)\s+\(Nightly Build\s+(?<build>\d+)\)",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (nightlyVersionMatch.Success &&
                System.Version.TryParse(
                    $"{nightlyVersionMatch.Groups["base"].Value}.{nightlyVersionMatch.Groups["build"].Value}",
                    out System.Version? nightlyVersion))
            {
                return nightlyVersion;
            }

            Match numericVersionMatch = Regex.Match(
                applicationVersion,
                @"(?<!\d)(?<version>\d+(?:\.\d+){1,3})(?![\d.-])",
                RegexOptions.CultureInvariant);

            return numericVersionMatch.Success &&
                   System.Version.TryParse(
                       numericVersionMatch.Groups["version"].Value,
                       out System.Version? numericVersion)
                ? numericVersion
                : null;
        }
    }
}
