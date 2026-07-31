using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Themes;
using mRemoteNG.UI.Controls;
using mRemoteNG.UI.Tabs;
using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNGTests.Themes
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ThemeRuntimeTests
    {
        [TestCase("Button_Hover_Background", "按鈕 · 滑入 · 背景")]
        [TestCase("GroupBox_Backgorund", "群組框 · 背景")]
        [TestCase("Treeview_SelectedItem_Active_Foreground", "連線樹 · 選取項目 · 使用中 · 文字")]
        public void PaletteLabelsUseTraditionalChineseForTraditionalChineseUi(string key, string expected)
        {
            Assert.That(ThemeColorLabelProvider.GetDisplayName(key, CultureInfo.GetCultureInfo("zh-TW")),
                        Is.EqualTo(expected));
        }

        [Test]
        public void PaletteLabelsFallBackToEnglishForOtherLanguages()
        {
            Assert.That(
                ThemeColorLabelProvider.GetDisplayName("Button_Hover_Background",
                                                       CultureInfo.GetCultureInfo("ja-JP")),
                Is.EqualTo("Button · Hover · Background"));
        }

        [Test]
        public void RuntimeThemeSwitchPreservesExistingDockContentInstances()
        {
            string themeDirectory = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "Themes");
            ThemeInfo originalTheme =
                ThemeSerializer.LoadFromXmlFile(System.IO.Path.Combine(themeDirectory, "vs2015dark.vstheme"));
            ThemeInfo nextTheme = ThemeSerializer.CreateVariant(
                originalTheme,
                "Runtime Test",
                new System.Collections.Generic.Dictionary<string, Color>
                {
                    ["Dialog_Background"] = Color.FromArgb(15, 23, 42)
                });

            using Form host = new();
            using DockPanel dockPanel = new() { Dock = DockStyle.Fill };
            host.Controls.Add(dockPanel);
            host.CreateControl();
            dockPanel.CreateControl();

            DockPanelThemeSwitcher.Apply(dockPanel, originalTheme.Theme);

            using DockContent first = new() { Text = "First" };
            using DockContent second = new() { Text = "Second" };
            using TextBox sessionControl = new() { Text = "live session" };
            first.Controls.Add(sessionControl);
            first.Show(dockPanel, DockState.Document);
            second.Show(dockPanel, DockState.Document);

            DockPanelThemeSwitcher.Apply(dockPanel, nextTheme.Theme);

            IDockContent[] restored = dockPanel.Contents.Cast<IDockContent>().ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(dockPanel.Theme, Is.SameAs(nextTheme.Theme));
                Assert.That(restored, Has.Length.EqualTo(2));
                Assert.That(restored, Does.Contain(first));
                Assert.That(restored, Does.Contain(second));
                Assert.That(first.IsDisposed, Is.False);
                Assert.That(second.IsDisposed, Is.False);
                Assert.That(sessionControl.IsDisposed, Is.False);
                Assert.That(sessionControl.Parent, Is.SameAs(first));
            });
        }

        [Test]
        public void LivePreviewSynchronizesWinFormsColorMode()
        {
            string themeDirectory = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "Themes");
            ThemeInfo originalTheme =
                ThemeSerializer.LoadFromXmlFile(System.IO.Path.Combine(themeDirectory, "vs2015dark.vstheme"));
            ThemeInfo darkTheme = ThemeSerializer.CreateVariant(
                originalTheme,
                "Dark Color Mode Test",
                new System.Collections.Generic.Dictionary<string, Color>
                {
                    ["Dialog_Background"] = Color.FromArgb(15, 23, 42)
                });
            ThemeInfo lightTheme = ThemeSerializer.CreateVariant(
                originalTheme,
                "Light Color Mode Test",
                new System.Collections.Generic.Dictionary<string, Color>
                {
                    ["Dialog_Background"] = Color.White
                });

            ThemeManager themeManager = ThemeManager.getInstance();
            ThemeInfo activeThemeBeforeTest = themeManager.ActiveTheme;
            SystemColorMode colorModeBeforeTest = Application.ColorMode;

            try
            {
                themeManager.PreviewTheme(darkTheme);
                Assert.That(Application.ColorMode, Is.EqualTo(SystemColorMode.Dark));

                themeManager.PreviewTheme(lightTheme);
                Assert.That(Application.ColorMode, Is.EqualTo(SystemColorMode.Classic));
            }
            finally
            {
                themeManager.PreviewTheme(activeThemeBeforeTest);
                Application.SetColorMode(colorModeBeforeTest);
            }
        }

        [Test]
        public void LivePreviewColorsReachToolWindowAndAutoHideStrips()
        {
            string themeDirectory = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "Themes");
            ThemeInfo originalTheme =
                ThemeSerializer.LoadFromXmlFile(System.IO.Path.Combine(themeDirectory, "vs2015dark.vstheme"));
            Color stripBackground = Color.FromArgb(17, 29, 43);
            Color captionBackground = Color.FromArgb(31, 45, 61);
            Color selectedBackground = Color.FromArgb(53, 91, 137);
            ThemeInfo previewTheme = ThemeSerializer.CreateVariant(
                originalTheme,
                "Dock Strip Preview Test",
                new System.Collections.Generic.Dictionary<string, Color>
                {
                    ["Tab_Background"] = stripBackground,
                    ["Tab_Item_Background"] = captionBackground,
                    ["Tab_Item_Foreground"] = Color.White,
                    ["Treeview_SelectedItem_Active_Background"] = selectedBackground,
                    ["Treeview_SelectedItem_Active_Foreground"] = Color.White,
                    ["Treeview_SelectedItem_Inactive_Background"] = selectedBackground,
                    ["Treeview_SelectedItem_Inactive_Foreground"] = Color.White,
                    ["List_Item_Border"] = Color.FromArgb(71, 85, 105)
                });

            ThemeManager themeManager = ThemeManager.getInstance();
            ThemeInfo activeThemeBeforeTest = themeManager.ActiveTheme;

            try
            {
                using Form host = new() { ClientSize = new Size(640, 420) };
                using DockPanel dockPanel = new() { Dock = DockStyle.Fill };
                host.Controls.Add(dockPanel);
                host.CreateControl();
                dockPanel.CreateControl();
                DockPanelThemeSwitcher.Apply(dockPanel, originalTheme.Theme);

                using DockContent first = new() { Text = "Connections" };
                using DockContent second = new() { Text = "Config" };
                using DockContent notification = new() { Text = "Notifications" };
                first.Show(dockPanel, DockState.DockLeft);
                second.Show(first.Pane, null);
                notification.Show(dockPanel, DockState.DockBottomAutoHide);
                host.PerformLayout();
                dockPanel.PerformLayout();

                themeManager.PreviewTheme(previewTheme);
                DockPanelThemeSwitcher.Apply(dockPanel, previewTheme.Theme);

                DockPaneStripBase toolWindowStrip = first.Pane.TabStripControl;
                Assert.That(toolWindowStrip.Height, Is.GreaterThan(0));
                Assert.That(toolWindowStrip.Width, Is.GreaterThan(0));

                using Bitmap toolWindowBitmap =
                    new(toolWindowStrip.Width, toolWindowStrip.Height);
                toolWindowStrip.DrawToBitmap(toolWindowBitmap, toolWindowStrip.ClientRectangle);

                Control caption = GetPrivateControl(first.Pane, "m_captionControl");
                using Bitmap captionBitmap = new(caption.Width, caption.Height);
                caption.DrawToBitmap(captionBitmap, caption.ClientRectangle);
                Color expectedCaptionBackground =
                    first.Pane.IsActivePane ? selectedBackground : captionBackground;

                Control autoHideStrip = GetPrivateControl(dockPanel, "m_autoHideStripControl");
                using Bitmap autoHideBitmap = new(autoHideStrip.Width, autoHideStrip.Height);
                autoHideStrip.DrawToBitmap(autoHideBitmap, autoHideStrip.ClientRectangle);

                Assert.Multiple(() =>
                {
                    Assert.That(originalTheme.Theme.Extender.DockPaneCaptionFactory,
                                Is.TypeOf<MremoteDockPaneCaptionFactory>());
                    Assert.That(originalTheme.Theme.Extender.AutoHideStripFactory,
                                Is.TypeOf<MremoteAutoHideStripFactory>());
                    Assert.That(originalTheme.Theme.Extender.DockPaneSplitterControlFactory,
                                Is.TypeOf<MremoteDockPaneSplitterFactory>());
                    Assert.That(originalTheme.Theme.Extender.WindowSplitterControlFactory,
                                Is.TypeOf<MremoteWindowSplitterFactory>());
                    Assert.That(caption.GetType().Name, Is.EqualTo("MremoteNGDockPaneCaption"),
                                "DockPanel should create mRemoteNG's live-theme caption.");
                    Assert.That(autoHideStrip, Is.TypeOf<MremoteNGAutoHideStrip>(),
                                "DockPanel should create mRemoteNG's live-theme auto-hide strip.");
                    Assert.That(BitmapContainsColor(captionBitmap, expectedCaptionBackground), Is.True,
                                "Tool-window caption should use the active semantic caption background.");
                    Assert.That(BitmapContainsColor(toolWindowBitmap, stripBackground), Is.True,
                                "Tool-window strip should use the active semantic tab background.");
                    Assert.That(BitmapContainsColor(toolWindowBitmap, selectedBackground), Is.True,
                                "Selected tool-window tab should use the active semantic selection color.");
                    Assert.That(autoHideStrip.BackColor, Is.EqualTo(stripBackground),
                                "Auto-hide strip should refresh its background during live preview.");
                    Assert.That(BitmapContainsColor(autoHideBitmap, stripBackground), Is.True,
                                "Auto-hide strip should paint the full surface with the active theme.");
                });
            }
            finally
            {
                themeManager.PreviewTheme(activeThemeBeforeTest);
            }
        }

        [Test]
        public void LivePreviewColorsReachInactiveListSelectionsAndEveryHeaderState()
        {
            string themeDirectory = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "Themes");
            ThemeInfo originalTheme =
                ThemeSerializer.LoadFromXmlFile(System.IO.Path.Combine(themeDirectory, "vs2015dark.vstheme"));
            Color selectedBackground = Color.FromArgb(33, 71, 119);
            Color selectedForeground = Color.FromArgb(245, 247, 250);
            Color headerBackground = Color.FromArgb(24, 31, 42);
            Color headerForeground = Color.FromArgb(218, 224, 232);
            Color border = Color.FromArgb(75, 85, 99);
            ThemeInfo previewTheme = ThemeSerializer.CreateVariant(
                originalTheme,
                "List State Preview Test",
                new System.Collections.Generic.Dictionary<string, Color>
                {
                    ["List_Background"] = Color.FromArgb(17, 24, 39),
                    ["List_Item_Foreground"] = Color.FromArgb(209, 213, 219),
                    ["List_Item_Selected_Background"] = selectedBackground,
                    ["List_Item_Selected_Foreground"] = selectedForeground,
                    ["List_Header_Background"] = headerBackground,
                    ["List_Header_Foreground"] = headerForeground,
                    ["List_Item_Border"] = border
                });

            ThemeManager themeManager = ThemeManager.getInstance();
            ThemeInfo activeThemeBeforeTest = themeManager.ActiveTheme;

            try
            {
                themeManager.PreviewTheme(previewTheme);
                using MrngListView listView = new();
                listView.CreateControl();

                Assert.Multiple(() =>
                {
                    Assert.That(listView.UnfocusedSelectedBackColor, Is.EqualTo(selectedBackground));
                    Assert.That(listView.UnfocusedSelectedForeColor, Is.EqualTo(selectedForeground));
                    Assert.That(listView.HeaderFormatStyle.Normal.BackColor, Is.EqualTo(headerBackground));
                    Assert.That(listView.HeaderFormatStyle.Normal.ForeColor, Is.EqualTo(headerForeground));
                    Assert.That(listView.HeaderFormatStyle.Hot.BackColor, Is.EqualTo(selectedBackground));
                    Assert.That(listView.HeaderFormatStyle.Hot.ForeColor, Is.EqualTo(selectedForeground));
                    Assert.That(listView.HeaderFormatStyle.Pressed.BackColor, Is.EqualTo(selectedBackground));
                    Assert.That(listView.HeaderFormatStyle.Pressed.ForeColor, Is.EqualTo(selectedForeground));
                    Assert.That(listView.HeaderFormatStyle.Hot.FrameColor, Is.EqualTo(border));
                });
            }
            finally
            {
                themeManager.PreviewTheme(activeThemeBeforeTest);
            }
        }

        [Test]
        public void SemanticRendererPaintsMenuStripWithoutSystemLightBackground()
        {
            string themeDirectory = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "Themes");
            ThemeInfo originalTheme =
                ThemeSerializer.LoadFromXmlFile(System.IO.Path.Combine(themeDirectory, "vs2015dark.vstheme"));
            Color menuBackground = Color.FromArgb(19, 31, 47);
            Color menuForeground = Color.FromArgb(238, 242, 247);
            Color hoverBackground = Color.FromArgb(36, 92, 143);
            ThemeInfo previewTheme = ThemeSerializer.CreateVariant(
                originalTheme,
                "Command Bar Preview Test",
                new System.Collections.Generic.Dictionary<string, Color>
                {
                    ["CommandBarMenuDefault_Background"] = menuBackground,
                    ["CommandBarMenuDefault_Foreground"] = menuForeground,
                    ["List_Background"] = Color.FromArgb(24, 39, 57),
                    ["List_Item_Border"] = Color.FromArgb(62, 81, 102),
                    ["List_Item_Selected_Background"] = hoverBackground,
                    ["List_Item_Selected_Border"] = Color.FromArgb(81, 134, 181),
                    ["Button_Hover_Background"] = hoverBackground,
                    ["Button_Hover_Border"] = Color.FromArgb(81, 134, 181),
                    ["Button_Hover_Foreground"] = Color.White,
                    ["Button_Pressed_Background"] = Color.FromArgb(24, 61, 96),
                    ["Button_Pressed_Border"] = Color.FromArgb(62, 113, 158),
                    ["Button_Pressed_Foreground"] = Color.White,
                    ["Button_Disabled_Foreground"] = Color.FromArgb(126, 143, 160)
                });

            using MenuStrip menuStrip = new()
            {
                Size = new Size(360, 28)
            };
            menuStrip.Items.Add(new ToolStripMenuItem("File"));
            menuStrip.Items.Add(new ToolStripMenuItem("Sessions"));
            menuStrip.CreateControl();

            RuntimeThemeToolStripRenderer renderer =
                RuntimeThemeToolStripStyler.Apply(menuStrip, previewTheme.ExtendedPalette);

            using Bitmap bitmap = new(menuStrip.Width, menuStrip.Height);
            menuStrip.DrawToBitmap(bitmap, menuStrip.ClientRectangle);

            Assert.Multiple(() =>
            {
                Assert.That(menuStrip.Renderer, Is.SameAs(renderer));
                Assert.That(menuStrip.BackColor, Is.EqualTo(menuBackground));
                Assert.That(menuStrip.ForeColor, Is.EqualTo(menuForeground));
                Assert.That(BitmapContainsColor(bitmap, menuBackground), Is.True,
                            "The semantic menu background should cover the command bar.");
                Assert.That(bitmap.GetPixel(menuStrip.Width - 2, menuStrip.Height / 2),
                            Is.EqualTo(menuBackground),
                            "Unused MenuStrip space must not fall back to SystemColors.Control.");
            });
        }

        private static bool BitmapContainsColor(Bitmap bitmap, Color expected)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).ToArgb() == expected.ToArgb())
                        return true;
                }
            }

            return false;
        }

        private static Control GetPrivateControl(object owner, string fieldName)
        {
            FieldInfo field = null;
            for (System.Type type = owner.GetType(); type != null && field == null; type = type.BaseType)
            {
                field = type.GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);
            }

            if (field == null)
            {
                throw new AssertionException(
                    $"Expected {owner.GetType().Name}.{fieldName}.");
            }

            return field.GetValue(owner) as Control
                   ?? throw new AssertionException(
                       $"Expected {owner.GetType().Name}.{fieldName} to be a Control.");
        }
    }
}
