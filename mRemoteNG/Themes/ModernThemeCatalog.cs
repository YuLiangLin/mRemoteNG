using System.Collections.Generic;
using System.Drawing;

namespace mRemoteNG.Themes
{
    /// <summary>
    /// Built-in modern palettes layered over the existing VS2015 theme resources.
    /// Keeping the definitions semantic makes contrast and state colors consistent
    /// across dialogs, menus, lists, trees, tabs, and form controls.
    /// </summary>
    internal static class ModernThemeCatalog
    {
        internal static IEnumerable<ThemeInfo> CreateBuiltInThemes(ThemeInfo lightBase, ThemeInfo darkBase)
        {
            yield return CreateTheme(darkBase, "Midnight Slate",
                                     new ThemeColors(
                                         background: "#0F172A",
                                         surface: "#111C30",
                                         surfaceAlt: "#1E293B",
                                         border: "#334155",
                                         text: "#F8FAFC",
                                         mutedText: "#94A3B8",
                                         accent: "#075985",
                                         accentHover: "#0369A1",
                                         accentPressed: "#0C4A6E",
                                         onAccent: "#FFFFFF",
                                         selection: "#075985",
                                         selectionText: "#FFFFFF",
                                         input: "#172033"));

            yield return CreateTheme(darkBase, "Nord Frost",
                                     new ThemeColors(
                                         background: "#2E3440",
                                         surface: "#343B49",
                                         surfaceAlt: "#3B4252",
                                         border: "#4C566A",
                                         text: "#ECEFF4",
                                         mutedText: "#A7B0C0",
                                         accent: "#4C6F98",
                                         accentHover: "#5278A3",
                                         accentPressed: "#405D80",
                                         onAccent: "#FFFFFF",
                                         selection: "#45647F",
                                         selectionText: "#FFFFFF",
                                         input: "#303744"));

            yield return CreateTheme(darkBase, "Tokyo Night",
                                     new ThemeColors(
                                         background: "#1A1B26",
                                         surface: "#202231",
                                         surfaceAlt: "#24283B",
                                         border: "#3B4261",
                                         text: "#C0CAF5",
                                         mutedText: "#8992B3",
                                         accent: "#7AA2F7",
                                         accentHover: "#89B4FA",
                                         accentPressed: "#5F82CA",
                                         onAccent: "#111827",
                                         selection: "#33467C",
                                         selectionText: "#F5F7FF",
                                         input: "#1F2335"));

            yield return CreateTheme(darkBase, "Graphite Violet",
                                     new ThemeColors(
                                         background: "#18181B",
                                         surface: "#202024",
                                         surfaceAlt: "#27272A",
                                         border: "#3F3F46",
                                         text: "#FAFAFA",
                                         mutedText: "#A1A1AA",
                                         accent: "#6D28D9",
                                         accentHover: "#7C3AED",
                                         accentPressed: "#5B21B6",
                                         onAccent: "#FFFFFF",
                                         selection: "#5B21B6",
                                         selectionText: "#FFFFFF",
                                         input: "#222226"));

            yield return CreateTheme(lightBase, "Cloud Blue",
                                     new ThemeColors(
                                         background: "#F8FAFC",
                                         surface: "#FFFFFF",
                                         surfaceAlt: "#F1F5F9",
                                         border: "#CBD5E1",
                                         text: "#0F172A",
                                         mutedText: "#64748B",
                                         accent: "#1D4ED8",
                                         accentHover: "#2563EB",
                                         accentPressed: "#1E40AF",
                                         onAccent: "#FFFFFF",
                                         selection: "#DBEAFE",
                                         selectionText: "#172554",
                                         input: "#FFFFFF"));

            yield return CreateTheme(lightBase, "Mint Breeze",
                                     new ThemeColors(
                                         background: "#F5FBF8",
                                         surface: "#FFFFFF",
                                         surfaceAlt: "#EAF6F0",
                                         border: "#B8D8C9",
                                         text: "#18322A",
                                         mutedText: "#587166",
                                         accent: "#047857",
                                         accentHover: "#048565",
                                         accentPressed: "#065F46",
                                         onAccent: "#FFFFFF",
                                         selection: "#D1FAE5",
                                         selectionText: "#064E3B",
                                         input: "#FFFFFF"));
        }

        private static ThemeInfo CreateTheme(ThemeInfo baseTheme, string name, ThemeColors colors)
        {
            return ThemeSerializer.CreateVariant(baseTheme, name, CreatePalette(colors));
        }

        private static IReadOnlyDictionary<string, Color> CreatePalette(ThemeColors colors)
        {
            Color disabledBackground = Mix(colors.SurfaceAlt, colors.Background, 0.5f);
            Color disabledText = colors.MutedText;
            Color focus = colors.AccentHover;
            Color warning = ColorTranslator.FromHtml("#D97706");
            Color error = ColorTranslator.FromHtml("#DC2626");

            return new Dictionary<string, Color>
            {
                ["Dialog_Background"] = colors.Background,
                ["Dialog_Foreground"] = colors.Text,
                ["CommandBarMenuDefault_Background"] = colors.Surface,
                ["CommandBarMenuDefault_Foreground"] = colors.Text,

                ["Button_Background"] = colors.Accent,
                ["Button_Border"] = colors.AccentPressed,
                ["Button_Foreground"] = colors.OnAccent,
                ["Button_Hover_Background"] = colors.AccentHover,
                ["Button_Hover_Border"] = colors.AccentHover,
                ["Button_Hover_Foreground"] = colors.OnAccent,
                ["Button_Pressed_Background"] = colors.AccentPressed,
                ["Button_Pressed_Border"] = colors.AccentPressed,
                ["Button_Pressed_Foreground"] = colors.OnAccent,
                ["Button_Disabled_Background"] = disabledBackground,
                ["Button_Disabled_Border"] = colors.Border,
                ["Button_Disabled_Foreground"] = disabledText,

                ["CheckBox_Background"] = colors.Input,
                ["CheckBox_Border"] = colors.Border,
                ["CheckBox_Border_Hover"] = colors.AccentHover,
                ["CheckBox_Border_Pressed"] = colors.AccentPressed,
                ["CheckBox_Border_Disabled"] = colors.Border,
                ["CheckBox_Glyph"] = colors.Accent,
                ["CheckBox_Glyph_Disabled"] = disabledText,
                ["CheckBox_Text"] = colors.Text,
                ["CheckBox_Text_Disabled"] = disabledText,

                ["ComboBox_Background"] = colors.Input,
                ["ComboBox_Border"] = colors.Border,
                ["ComboBox_Foreground"] = colors.Text,
                ["ComboBox_MouseOver_Border"] = focus,
                ["ComboBox_Disabled_Background"] = disabledBackground,
                ["ComboBox_Disabled_Foreground"] = disabledText,
                ["ComboBox_PopUp"] = colors.Surface,
                ["ComboBox_PopUp_Border"] = colors.Border,
                ["ComboBox_Button_Background"] = colors.SurfaceAlt,
                ["ComboBox_Button_Border"] = colors.Border,
                ["ComboBox_Button_Foreground"] = colors.Text,
                ["ComboBox_Button_MouseOver_Background"] = colors.Selection,
                ["ComboBox_Button_MouseOver_Border"] = focus,
                ["ComboBox_Button_MouseOver_Foreground"] = colors.SelectionText,
                ["ComboBox_Button_Pressed_Background"] = colors.AccentPressed,
                ["ComboBox_Button_Pressed_Foreground"] = colors.SelectionText,

                ["GroupBox_Backgorund"] = colors.Background,
                ["GroupBox_Foreground"] = colors.Text,
                ["GroupBox_Line"] = colors.Border,
                ["GroupBox_Disabled_Background"] = disabledBackground,
                ["GroupBox_Disabled_Foreground"] = disabledText,
                ["GroupBox_Disabled_Line"] = colors.Border,

                ["List_Background"] = colors.Surface,
                ["List_Header_Background"] = colors.SurfaceAlt,
                ["List_Header_Foreground"] = colors.Text,
                ["List_Item_Background"] = colors.Surface,
                ["List_Item_Border"] = colors.Border,
                ["List_Item_Foreground"] = colors.Text,
                ["List_Item_Selected_Background"] = colors.Selection,
                ["List_Item_Selected_Border"] = colors.Accent,
                ["List_Item_Selected_Foreground"] = colors.SelectionText,
                ["List_Item_Disabled_Background"] = disabledBackground,
                ["List_Item_Disabled_Border"] = colors.Border,
                ["List_Item_Disabled_Foreground"] = disabledText,

                ["ProgressBar_Background"] = colors.SurfaceAlt,
                ["ProgressBar_Fill"] = colors.Accent,
                ["ProgressBar_Fill_Warning"] = warning,
                ["ProgressBar_Fill_Critical"] = error,

                ["Tab_Background"] = colors.Background,
                ["Tab_Item_Background"] = colors.SurfaceAlt,
                ["Tab_Item_Foreground"] = colors.Text,
                ["Tab_Item_Disabled_Background"] = disabledBackground,
                ["Tab_Item_Disabled_Foreground"] = disabledText,

                ["TextBox_Background"] = colors.Input,
                ["TextBox_Border"] = colors.Border,
                ["TextBox_Foreground"] = colors.Text,
                ["TextBox_Border_Focused"] = focus,
                ["TextBox_Focused_Background"] = colors.Input,
                ["TextBox_Focused_Foreground"] = colors.Text,
                ["TextBox_Border_Disabled"] = colors.Border,
                ["TextBox_Disabled_Background"] = disabledBackground,
                ["TextBox_Disabled_Foreground"] = disabledText,

                ["TreeView_Background"] = colors.Surface,
                ["TreeView_Foreground"] = colors.Text,
                ["Treeview_SelectedItem_Active_Background"] = colors.Selection,
                ["Treeview_SelectedItem_Active_Foreground"] = colors.SelectionText,
                ["Treeview_SelectedItem_Inactive_Background"] = colors.SurfaceAlt,
                ["Treeview_SelectedItem_Inactive_Foreground"] = colors.Text,

                ["ErrorText_Background"] = colors.Background,
                ["ErrorText_Foreground"] = error,
                ["WarningText_Background"] = colors.Background,
                ["WarningText_Foreground"] = warning
            };
        }

        private static Color Mix(Color first, Color second, float firstWeight)
        {
            float secondWeight = 1f - firstWeight;
            return Color.FromArgb(
                (int)(first.R * firstWeight + second.R * secondWeight),
                (int)(first.G * firstWeight + second.G * secondWeight),
                (int)(first.B * firstWeight + second.B * secondWeight));
        }

        private sealed class ThemeColors
        {
            internal ThemeColors(string background, string surface, string surfaceAlt, string border,
                                 string text, string mutedText, string accent, string accentHover,
                                 string accentPressed, string onAccent, string selection,
                                 string selectionText, string input)
            {
                Background = Parse(background);
                Surface = Parse(surface);
                SurfaceAlt = Parse(surfaceAlt);
                Border = Parse(border);
                Text = Parse(text);
                MutedText = Parse(mutedText);
                Accent = Parse(accent);
                AccentHover = Parse(accentHover);
                AccentPressed = Parse(accentPressed);
                OnAccent = Parse(onAccent);
                Selection = Parse(selection);
                SelectionText = Parse(selectionText);
                Input = Parse(input);
            }

            internal Color Background { get; }
            internal Color Surface { get; }
            internal Color SurfaceAlt { get; }
            internal Color Border { get; }
            internal Color Text { get; }
            internal Color MutedText { get; }
            internal Color Accent { get; }
            internal Color AccentHover { get; }
            internal Color AccentPressed { get; }
            internal Color OnAccent { get; }
            internal Color Selection { get; }
            internal Color SelectionText { get; }
            internal Color Input { get; }

            private static Color Parse(string value) => ColorTranslator.FromHtml(value);
        }
    }
}
