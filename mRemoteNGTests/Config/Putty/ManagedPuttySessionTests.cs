using System;
using System.Runtime.Versioning;
using Microsoft.Win32;
using mRemoteNG.Config.Putty;
using mRemoteNG.Tools.WindowsRegistry;
using NSubstitute;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Putty
{
    [SupportedOSPlatform("windows")]
    public class ManagedPuttySessionTests
    {
        private const string SessionsKey = @"Software\SimonTatham\PuTTY\Sessions";
        private const string SessionKey = SessionsKey + @"\" + ManagedPuttySession.SessionName;

        private IRegistry _registry = null!;
        private ManagedPuttySession _managedSession = null!;

        [SetUp]
        public void SetUp()
        {
            _registry = Substitute.For<IRegistry>();
            _managedSession = new ManagedPuttySession(_registry);
        }

        [Test]
        public void EnsureCreated_WritesModernTerminalSettingsAndCompletionMarker()
        {
            _registry.GetSubKeyNames(RegistryHive.CurrentUser, SessionsKey)
                .Returns(Array.Empty<string>(), [ManagedPuttySession.SessionName]);
            _registry.GetIntegerValue(
                    RegistryHive.CurrentUser,
                    SessionKey,
                    "mRemoteNGThemeVersion",
                    0)
                .Returns(1);

            bool created = _managedSession.EnsureCreated();

            Assert.That(created, Is.True);
            _registry.Received().SetValue(
                RegistryHive.CurrentUser,
                SessionKey,
                "Font",
                "Cascadia Mono",
                RegistryValueKind.String);
            _registry.Received().SetValue(
                RegistryHive.CurrentUser,
                SessionKey,
                "LineCodePage",
                "UTF-8",
                RegistryValueKind.String);
            _registry.Received().SetValue(
                RegistryHive.CurrentUser,
                SessionKey,
                "TerminalType",
                "xterm-256color",
                RegistryValueKind.String);
            _registry.Received().SetValue(
                RegistryHive.CurrentUser,
                SessionKey,
                "mRemoteNGThemeVersion",
                1,
                RegistryValueKind.DWord);
        }

        [Test]
        public void EnsureCreated_PreservesAnExistingCurrentSession()
        {
            ConfigureCurrentSession();

            bool created = _managedSession.EnsureCreated();

            Assert.That(created, Is.True);
            _registry.DidNotReceiveWithAnyArgs()
                .SetValue(default, default!, default!, default!, default);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Default Settings")]
        public void ResolveSessionName_UsesManagedSessionForUncustomizedConnections(string configuredSession)
        {
            ConfigureCurrentSession();

            string resolved = _managedSession.ResolveSessionName(configuredSession);

            Assert.That(resolved, Is.EqualTo(ManagedPuttySession.SessionName));
        }

        [Test]
        public void ResolveSessionName_PreservesExplicitCustomSession()
        {
            ConfigureCurrentSession();

            string resolved = _managedSession.ResolveSessionName("Operations");

            Assert.That(resolved, Is.EqualTo("Operations"));
        }

        [Test]
        public void ResolveSessionName_FallsBackWhenManagedSessionIsUnavailable()
        {
            _registry.GetSubKeyNames(RegistryHive.CurrentUser, SessionsKey)
                .Returns(Array.Empty<string>());

            string resolved = _managedSession.ResolveSessionName(ManagedPuttySession.SessionName);

            Assert.That(resolved, Is.EqualTo("Default Settings"));
        }

        private void ConfigureCurrentSession()
        {
            _registry.GetSubKeyNames(RegistryHive.CurrentUser, SessionsKey)
                .Returns([ManagedPuttySession.SessionName]);
            _registry.GetIntegerValue(
                    RegistryHive.CurrentUser,
                    SessionKey,
                    "mRemoteNGThemeVersion",
                    0)
                .Returns(1);
        }
    }
}
