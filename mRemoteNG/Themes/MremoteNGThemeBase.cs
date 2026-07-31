using System.Drawing;
using System.Runtime.Versioning;
using mRemoteNG.UI.Tabs;
using WeifenLuo.WinFormsUI.Docking;
using WeifenLuo.WinFormsUI.ThemeVS2015;

namespace mRemoteNG.Themes
{
    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Visual Studio 2015 Light theme.
    /// </summary>
    public class MremoteNGThemeBase : VS2015ThemeBase
    {
        public MremoteNGThemeBase(byte[] themeResource)
            : base(themeResource)
        {
            Measures.SplitterSize = 3;
            Measures.AutoHideSplitterSize = 3;
            Measures.DockPadding = 2;
            ShowAutoHideContentOnHover = false;
        }
    }

    [SupportedOSPlatform("windows")]
    public class MremoteDockPaneStripFactory : DockPanelExtender.IDockPaneStripFactory
    {
        public DockPaneStripBase CreateDockPaneStrip(DockPane pane) => new DockPaneStripNG(pane);
    }

    public class MremoteDockPaneCaptionFactory : DockPanelExtender.IDockPaneCaptionFactory
    {
        public DockPaneCaptionBase CreateDockPaneCaption(DockPane pane) =>
            new MremoteNGDockPaneCaption(pane);
    }

    public class MremoteAutoHideStripFactory : DockPanelExtender.IAutoHideStripFactory
    {
        public AutoHideStripBase CreateAutoHideStrip(DockPanel panel) =>
            new MremoteNGAutoHideStrip(panel);
    }

    public class MremoteDockPaneSplitterFactory : DockPanelExtender.IDockPaneSplitterControlFactory
    {
        public DockPane.SplitterControlBase CreateSplitterControl(DockPane pane) =>
            new MremoteNGDockPaneSplitter(pane);
    }

    public class MremoteWindowSplitterFactory : DockPanelExtender.IWindowSplitterControlFactory
    {
        public SplitterBase CreateSplitterControl(ISplitterHost host) =>
            new MremoteNGWindowSplitter(host);
    }

    public class MremoteFloatWindowFactory : DockPanelExtender.IFloatWindowFactory
    {
        public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane, Rectangle bounds)
        {
            Rectangle? activeDocumentBounds = (dockPanel?.ActiveDocument as ConnectionTab)?.Bounds;

            // dockPanel is non-null per the IFloatWindowFactory.CreateFloatWindow contract
            return new FloatWindowNG(dockPanel!, pane, activeDocumentBounds ?? bounds);
        }

        public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane)
        {
            return new FloatWindowNG(dockPanel, pane);
        }
    }
}
