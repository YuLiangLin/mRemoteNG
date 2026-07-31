using System.Threading;
using System.Windows.Forms;
using System;
using System.Linq;
using System.Reflection;
using mRemoteNG.Properties;
using mRemoteNG.Resources.Language;
using mRemoteNG.Themes;
using mRemoteNG.UI.Controls;
using mRemoteNGTests.TestHelpers;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms.OptionsPages
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class OptionsThemePageTests : OptionsFormSetupAndTeardown
    {
        [Test]
        public void ThemePageLinkExistsInListView()
        {
            ListViewTester listViewTester = new("lstOptionPages", _optionsForm);
            Assert.That(listViewTester.Items[8].Text, Is.EqualTo(Language.Theme));
        }

        [Test]
        public void ThemeIconShownInListView()
        {
            ListViewTester listViewTester = new("lstOptionPages", _optionsForm);
            Assert.That(listViewTester.Items[8].ImageList, Is.Not.Null);
        }

        [Test]
        public void SelectingThemePageLoadsSettings()
        {
            SelectThemePage();
            Button buttonTester = _optionsForm.FindControl<Button>("btnThemeNew");
            Assert.That(buttonTester.Text, Is.EqualTo(Language._New));
        }

        [Test]
        public void ThemeSelectorIncludesModernThemes()
        {
            SelectThemePage();
            ComboBox themeSelector = _optionsForm.FindControl<ComboBox>("cboTheme");

            string[] themeNames = themeSelector.Items.Cast<ThemeInfo>().Select(theme => theme.Name).ToArray();

            Assert.That(themeNames,
                        Does.Contain("Midnight Slate")
                            .And.Contain("Nord Frost")
                            .And.Contain("Tokyo Night")
                            .And.Contain("Graphite Violet")
                            .And.Contain("Cloud Blue")
                            .And.Contain("Mint Breeze"));
        }

        [Test]
        public void ThemePaletteUsesItsLastColumnToCoverUnusedHeaderSpace()
        {
            SelectThemePage();
            MrngListView palette = _optionsForm.FindControl<MrngListView>("listPalette");

            Assert.That(palette.AllColumns[^1].FillsFreeSpace, Is.True);
        }

        [Test]
        public void SelectingThemePreviewsAndApplyPersistsWhileReloadRestoresIt()
        {
            SelectThemePage();
            ComboBox themeSelector = _optionsForm.FindControl<ComboBox>("cboTheme");
            ThemeManager themeManager = ThemeManager.getInstance();
            ThemeInfo originalTheme = themeManager.ActiveTheme;
            string persistedThemeName = OptionsThemePage.Default.ThemeName;
            bool persistedThemeActive = OptionsThemePage.Default.ThemingActive;
            bool persistedDarkFlag = OptionsThemePage.Default.IsActiveThemeDark;
            ThemeInfo previewTheme = themeSelector.Items.Cast<ThemeInfo>()
                                                  .First(theme => theme.Name == "Midnight Slate");

            try
            {
                PreviewTheme(themeSelector, previewTheme);

                Assert.Multiple(() =>
                {
                    Assert.That(themeManager.ActiveTheme, Is.SameAs(previewTheme));
                    Assert.That(OptionsThemePage.Default.ThemeName, Is.EqualTo(persistedThemeName));
                    Assert.That(_optionsForm.BackColor,
                                Is.EqualTo(previewTheme.ExtendedPalette.getColor("Dialog_Background")));
                });

                _optionsForm.ReloadAllSettings();
                Assert.That(themeManager.ActiveTheme, Is.SameAs(originalTheme));

                PreviewTheme(themeSelector, previewTheme);
                _optionsForm.SaveAllOptions();

                Assert.Multiple(() =>
                {
                    Assert.That(themeManager.ActiveTheme, Is.SameAs(previewTheme));
                    Assert.That(OptionsThemePage.Default.ThemeName, Is.EqualTo(previewTheme.Name));
                });
            }
            finally
            {
                themeManager.PreviewTheme(originalTheme);
                OptionsThemePage.Default.ThemeName = persistedThemeName;
                OptionsThemePage.Default.ThemingActive = persistedThemeActive;
                OptionsThemePage.Default.IsActiveThemeDark = persistedDarkFlag;
                OptionsThemePage.Default.Save();
            }
        }

        private void SelectThemePage()
        {
            ListViewTester listViewTester = new("lstOptionPages", _optionsForm);
            listViewTester.Select(Language.Theme);
        }

        private static void PreviewTheme(ComboBox themeSelector, ThemeInfo previewTheme)
        {
            themeSelector.SelectedItem = previewTheme;
            typeof(ComboBox).GetMethod("OnSelectionChangeCommitted",
                                       BindingFlags.Instance | BindingFlags.NonPublic)!
                            .Invoke(themeSelector, new object[] { EventArgs.Empty });
        }
    }
}
