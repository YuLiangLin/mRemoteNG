using System.IO;
using System.Reflection;
using NUnit.Framework;
using VncSharpCore;

namespace mRemoteNGTests.Connection.Protocol;

public class VncSharpResourceTests
{
    [TestCase("VncSharpCore.Resources.screenshot.png")]
    [TestCase("VncSharpCore.Resources.vnccursor.cur")]
    public void RuntimeResourceIsEmbedded(string resourceName)
    {
        Assembly assembly = typeof(RemoteDesktop).Assembly;

        using Stream resource = assembly.GetManifestResourceStream(resourceName);

        Assert.That(resource, Is.Not.Null,
            $"VNC runtime resource '{resourceName}' must be embedded in VncSharpCore.dll.");
        Assert.That(resource.Length, Is.GreaterThan(0));
    }
}
