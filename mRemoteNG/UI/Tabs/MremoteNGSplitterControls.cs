using System.ComponentModel;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using WeifenLuo.WinFormsUI.Docking;
using WeifenLuo.WinFormsUI.ThemeVS2013;

namespace mRemoteNG.UI.Tabs
{
    [SupportedOSPlatform("windows")]
    [ToolboxItem(false)]
    internal sealed class MremoteNGDockPaneSplitter : DockPane.SplitterControlBase
    {
        internal MremoteNGDockPaneSplitter(DockPane pane)
            : base(pane)
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e) =>
            PaintSplitter(e.Graphics, ClientRectangle, DockPane.DockPanel);

        protected override void OnPaint(PaintEventArgs e)
        {
        }

        internal static void PaintSplitter(
            Graphics graphics,
            Rectangle bounds,
            DockPanel dockPanel)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            Color background = RuntimeThemeColorResolver.Resolve(
                "Tab_Background",
                dockPanel.Theme.ColorPalette.MainWindowActive.Background);
            Color border = RuntimeThemeColorResolver.Resolve(
                "List_Item_Border",
                dockPanel.Theme.ColorPalette.ToolWindowBorder);

            using SolidBrush backgroundBrush = new(background);
            graphics.FillRectangle(backgroundBrush, bounds);

            using Pen borderPen = new(border);
            if (bounds.Width < bounds.Height)
            {
                int x = bounds.Left + bounds.Width / 2;
                graphics.DrawLine(borderPen, x, bounds.Top, x, bounds.Bottom);
            }
            else
            {
                int y = bounds.Top + bounds.Height / 2;
                graphics.DrawLine(borderPen, bounds.Left, y, bounds.Right, y);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    [ToolboxItem(false)]
    internal sealed class MremoteNGWindowSplitter : VS2013WindowSplitterControl
    {
        private readonly ISplitterHost _host;

        internal MremoteNGWindowSplitter(ISplitterHost host)
            : base(host)
        {
            _host = host;
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e) =>
            MremoteNGDockPaneSplitter.PaintSplitter(
                e.Graphics,
                ClientRectangle,
                _host.DockPanel);

        protected override void OnPaint(PaintEventArgs e)
        {
        }
    }
}
