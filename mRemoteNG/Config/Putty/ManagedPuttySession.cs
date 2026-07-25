using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Win32;
using mRemoteNG.Tools.WindowsRegistry;

namespace mRemoteNG.Config.Putty
{
    [SupportedOSPlatform("windows")]
    internal sealed class ManagedPuttySession
    {
        public const string SessionName = "mRemoteNG-Modern";

        private const string DefaultSessionName = "Default Settings";
        private const string SessionsKey = @"Software\SimonTatham\PuTTY\Sessions";
        private const string SessionKey = SessionsKey + @"\" + SessionName;
        private const string VersionValueName = "mRemoteNGThemeVersion";
        private const int CurrentVersion = 1;

        private static readonly IReadOnlyList<RegistrySetting> ModernDarkSettings =
        [
            new("Font", "Cascadia Mono", RegistryValueKind.String),
            new("FontHeight", 14, RegistryValueKind.DWord),
            new("FontIsBold", 0, RegistryValueKind.DWord),
            new("FontCharSet", 0, RegistryValueKind.DWord),
            new("FontQuality", 3, RegistryValueKind.DWord),
            new("LineCodePage", "UTF-8", RegistryValueKind.String),
            new("UTF8Override", 1, RegistryValueKind.DWord),
            new("TerminalType", "xterm-256color", RegistryValueKind.String),
            new("ANSIColour", 1, RegistryValueKind.DWord),
            new("Xterm256Colour", 1, RegistryValueKind.DWord),
            new("TrueColour", 1, RegistryValueKind.DWord),
            new("UseSystemColours", 0, RegistryValueKind.DWord),
            new("BoldAsColour", 1, RegistryValueKind.DWord),
            new("ScrollbackLines", 10000, RegistryValueKind.DWord),
            new("Colour0", "248,248,242", RegistryValueKind.String),
            new("Colour1", "255,255,255", RegistryValueKind.String),
            new("Colour2", "40,42,54", RegistryValueKind.String),
            new("Colour3", "68,71,90", RegistryValueKind.String),
            new("Colour4", "40,42,54", RegistryValueKind.String),
            new("Colour5", "80,250,123", RegistryValueKind.String),
            new("Colour6", "33,34,44", RegistryValueKind.String),
            new("Colour7", "98,114,164", RegistryValueKind.String),
            new("Colour8", "255,85,85", RegistryValueKind.String),
            new("Colour9", "255,110,110", RegistryValueKind.String),
            new("Colour10", "80,250,123", RegistryValueKind.String),
            new("Colour11", "105,255,148", RegistryValueKind.String),
            new("Colour12", "241,250,140", RegistryValueKind.String),
            new("Colour13", "255,255,165", RegistryValueKind.String),
            new("Colour14", "189,147,249", RegistryValueKind.String),
            new("Colour15", "214,172,255", RegistryValueKind.String),
            new("Colour16", "255,121,198", RegistryValueKind.String),
            new("Colour17", "255,146,223", RegistryValueKind.String),
            new("Colour18", "139,233,253", RegistryValueKind.String),
            new("Colour19", "164,255,255", RegistryValueKind.String),
            new("Colour20", "248,248,242", RegistryValueKind.String),
            new("Colour21", "255,255,255", RegistryValueKind.String)
        ];

        private readonly IRegistry _registry;

        public ManagedPuttySession(IRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public bool EnsureCreated()
        {
            if (IsCurrent())
                return true;

            foreach (RegistrySetting setting in ModernDarkSettings)
                _registry.SetValue(RegistryHive.CurrentUser, SessionKey, setting.Name, setting.Value, setting.Kind);

            // Written last so an interrupted write is repaired on the next launch.
            _registry.SetValue(RegistryHive.CurrentUser, SessionKey, VersionValueName, CurrentVersion, RegistryValueKind.DWord);
            return IsCurrent();
        }

        public string ResolveSessionName(string configuredSessionName)
        {
            string requestedSession = string.IsNullOrWhiteSpace(configuredSessionName)
                ? DefaultSessionName
                : configuredSessionName;

            if (requestedSession.Equals(SessionName, StringComparison.OrdinalIgnoreCase))
                return IsCurrent() ? SessionName : DefaultSessionName;

            bool usesBuiltInDefault = requestedSession.Equals(DefaultSessionName, StringComparison.OrdinalIgnoreCase);
            return usesBuiltInDefault && IsCurrent() ? SessionName : requestedSession;
        }

        private bool IsCurrent()
        {
            return _registry.GetSubKeyNames(RegistryHive.CurrentUser, SessionsKey)
                       .Contains(SessionName, StringComparer.OrdinalIgnoreCase)
                   && _registry.GetIntegerValue(
                       RegistryHive.CurrentUser,
                       SessionKey,
                       VersionValueName,
                       defaultValue: 0) == CurrentVersion;
        }

        private sealed record RegistrySetting(string Name, object Value, RegistryValueKind Kind);
    }
}
