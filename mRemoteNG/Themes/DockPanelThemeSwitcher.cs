using System;
using System.Reflection;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.Themes
{
    /// <summary>
    /// Refreshes file-based DockPanelSuite themes without closing or reparenting
    /// existing connection windows.
    /// </summary>
    internal static class DockPanelThemeSwitcher
    {
        private static readonly FieldInfo DockPanelThemeField =
            typeof(DockPanel).GetField("m_dockPanelTheme",
                                       BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(DockPanel).FullName, "m_dockPanelTheme");

        private static readonly FieldInfo AutoHideStripControlField =
            typeof(DockPanel).GetField("m_autoHideStripControl",
                                       BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(DockPanel).FullName, "m_autoHideStripControl");

        /// <summary>
        /// Applies the theme immediately when doing so cannot replace hosted content.
        /// Returns false only when DockPanelSuite would require closing existing panes.
        /// </summary>
        internal static bool Apply(DockPanel dockPanel, ThemeBase theme)
        {
            if (dockPanel == null)
                throw new ArgumentNullException(nameof(dockPanel));
            if (theme == null)
                throw new ArgumentNullException(nameof(theme));
            if (ReferenceEquals(dockPanel.Theme, theme))
                return true;

            ThemeBase previousTheme = dockPanel.Theme;

            // mRemoteNG file themes all use MremoteNGThemeBase. DockPanelSuite's
            // public setter ignores another instance of the same runtime type,
            // even when its palette differs. Swapping that field is safe here:
            // the factories and hosted control types remain compatible.
            if (previousTheme != null && previousTheme.GetType() == theme.GetType())
            {
                dockPanel.SuspendLayout();
                try
                {
                    DockPanelThemeField.SetValue(dockPanel, theme);
                }
                catch
                {
                    DockPanelThemeField.SetValue(dockPanel, previousTheme);
                    throw;
                }
                finally
                {
                    dockPanel.ResumeLayout(true, true);
                }

                RefreshDockChrome(dockPanel);
                return true;
            }

            // The public setter is fully supported while the panel is empty.
            if (dockPanel.Contents.Count == 0)
            {
                dockPanel.Theme = theme;
                return true;
            }

            // Different factory types require DockPanelSuite's destructive
            // close/reload cycle. Keep live sessions untouched; startup will
            // apply the persisted selection before panes are restored.
            return false;
        }

        private static void RefreshDockChrome(DockPanel dockPanel)
        {
            dockPanel.PerformLayout();
            dockPanel.Invalidate(true);

            foreach (DockPane pane in dockPanel.Panes)
            {
                pane.TabStripControl.Invalidate(true);
                pane.TabStripControl.Refresh();
                pane.Invalidate(true);
            }

            if (AutoHideStripControlField.GetValue(dockPanel) is System.Windows.Forms.Control autoHideStrip)
            {
                autoHideStrip.Invalidate(true);
                autoHideStrip.Refresh();
            }

            foreach (FloatWindow window in dockPanel.FloatWindows)
            {
                window.Invalidate(true);
                window.Refresh();
            }
        }
    }
}
