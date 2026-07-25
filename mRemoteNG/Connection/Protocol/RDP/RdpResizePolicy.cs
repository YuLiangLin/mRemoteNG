using System;
using System.Drawing;

namespace mRemoteNG.Connection.Protocol.RDP
{
    internal static class RdpResizePolicy
    {
        private const int MinimumDesktopDimension = 200;
        private const int MaximumDesktopDimension = 8192;

        internal static bool UsesDynamicSessionResize(RDPResolutions resolution)
        {
            return resolution == RDPResolutions.Fullscreen;
        }

        internal static Size NormalizeDesktopSize(Size requestedSize)
        {
            return new Size(
                Math.Clamp(requestedSize.Width, MinimumDesktopDimension, MaximumDesktopDimension),
                Math.Clamp(requestedSize.Height, MinimumDesktopDimension, MaximumDesktopDimension));
        }

        internal static Rectangle CalculateAspectFitBounds(Rectangle viewport, Size contentSize)
        {
            if (viewport.Width <= 0 || viewport.Height <= 0 ||
                contentSize.Width <= 0 || contentSize.Height <= 0)
            {
                return viewport;
            }

            double scale = Math.Min(
                viewport.Width / (double)contentSize.Width,
                viewport.Height / (double)contentSize.Height);
            int width = Math.Min(viewport.Width, (int)Math.Round(contentSize.Width * scale));
            int height = Math.Min(viewport.Height, (int)Math.Round(contentSize.Height * scale));

            return new Rectangle(
                viewport.X + (viewport.Width - width) / 2,
                viewport.Y + (viewport.Height - height) / 2,
                width,
                height);
        }
    }
}
