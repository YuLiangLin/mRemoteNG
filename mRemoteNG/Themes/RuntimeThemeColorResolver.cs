using System.Drawing;

namespace mRemoteNG.Themes
{
    /// <summary>
    /// Resolves semantic colors from the theme currently previewed by the user.
    /// DockPanelSuite keeps parts of its original palette while panes are alive,
    /// so custom mRemoteNG painters use this resolver to stay in sync immediately.
    /// </summary>
    internal static class RuntimeThemeColorResolver
    {
        internal static Color Resolve(string colorKey, Color fallback)
        {
            try
            {
                ThemeManager themeManager = ThemeManager.getInstance();
                if (!themeManager.ActiveAndExtended)
                    return fallback;

                Color resolved = themeManager.ActiveTheme.ExtendedPalette.getColor(colorKey);
                return resolved.IsEmpty || resolved.A == 0 ? fallback : resolved;
            }
            catch
            {
                // Theme painting must never make a dock surface unusable.
                return fallback;
            }
        }
    }
}
