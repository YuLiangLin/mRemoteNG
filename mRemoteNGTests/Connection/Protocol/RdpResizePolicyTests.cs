using System.Drawing;
using mRemoteNG.Connection.Protocol.RDP;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol
{
    [TestFixture]
    public class RdpResizePolicyTests
    {
        [TestCase(RDPResolutions.SmartSize, false)]
        [TestCase(RDPResolutions.Fullscreen, true)]
        [TestCase(RDPResolutions.FitToWindow, false)]
        public void UsesDynamicSessionResize_ReturnsExpectedValue(
            RDPResolutions resolution,
            bool expected)
        {
            Assert.That(RdpResizePolicy.UsesDynamicSessionResize(resolution), Is.EqualTo(expected));
        }

        [TestCase(1200, 800, 1200, 800)]
        [TestCase(100, 150, 200, 200)]
        [TestCase(9000, 10000, 8192, 8192)]
        public void NormalizeDesktopSize_ClampsToRdpClientLimits(
            int width,
            int height,
            int expectedWidth,
            int expectedHeight)
        {
            Assert.That(
                RdpResizePolicy.NormalizeDesktopSize(new Size(width, height)),
                Is.EqualTo(new Size(expectedWidth, expectedHeight)));
        }

        [TestCase(0, 0, 1600, 900, 1920, 1080, 0, 0, 1600, 900)]
        [TestCase(10, 20, 1200, 900, 1920, 1080, 10, 132, 1200, 675)]
        [TestCase(10, 20, 900, 1200, 1920, 1080, 10, 367, 900, 506)]
        public void CalculateAspectFitBounds_PreservesRemoteAspectRatioAndCenters(
            int viewportX,
            int viewportY,
            int viewportWidth,
            int viewportHeight,
            int contentWidth,
            int contentHeight,
            int expectedX,
            int expectedY,
            int expectedWidth,
            int expectedHeight)
        {
            var viewport = new Rectangle(viewportX, viewportY, viewportWidth, viewportHeight);
            var contentSize = new Size(contentWidth, contentHeight);
            var expected = new Rectangle(expectedX, expectedY, expectedWidth, expectedHeight);

            Assert.That(
                RdpResizePolicy.CalculateAspectFitBounds(viewport, contentSize),
                Is.EqualTo(expected));
        }
    }
}
