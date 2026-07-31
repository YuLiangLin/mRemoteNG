using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using mRemoteNG.Themes;
using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNGTests.Themes;

[TestFixture]
public class ThemeSerializerTests
{
    [Test]
    public void SaveToXmlFile_WithEmptyBaseThemeUri_ThrowsArgumentException()
    {
        ThemeInfo baseTheme = new("baseTheme", new VS2015LightTheme(), "", VisualStudioToolStripExtender.VsVersion.Vs2015);
        ThemeInfo themeToSave = new("newTheme", new VS2015LightTheme(), "", VisualStudioToolStripExtender.VsVersion.Vs2015);

        Assert.Throws<ArgumentException>(() => ThemeSerializer.SaveToXmlFile(themeToSave, baseTheme));
    }

    [Test]
    public void SaveToXmlFile_WithWhitespaceBaseThemeUri_ThrowsArgumentException()
    {
        ThemeInfo baseTheme = new("baseTheme", new VS2015LightTheme(), "   ", VisualStudioToolStripExtender.VsVersion.Vs2015);
        ThemeInfo themeToSave = new("newTheme", new VS2015LightTheme(), "", VisualStudioToolStripExtender.VsVersion.Vs2015);

        Assert.Throws<ArgumentException>(() => ThemeSerializer.SaveToXmlFile(themeToSave, baseTheme));
    }

    [Test]
    public void SaveToXmlFile_WithBaseThemeUriWithoutDirectory_ThrowsArgumentException()
    {
        ThemeInfo baseTheme = new("baseTheme", new VS2015LightTheme(), "base.vstheme", VisualStudioToolStripExtender.VsVersion.Vs2015);
        ThemeInfo themeToSave = new("newTheme", new VS2015LightTheme(), "", VisualStudioToolStripExtender.VsVersion.Vs2015);

        Assert.Throws<ArgumentException>(() => ThemeSerializer.SaveToXmlFile(themeToSave, baseTheme));
    }

    [Test]
    public void SaveToXmlFile_WithValidBaseThemeUri_CopiesThemeToSameDirectory()
    {
        string testDirectory = Path.Combine(Path.GetTempPath(), "mRemoteNGTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
        string baseThemePath = Path.Combine(testDirectory, "base.vstheme");
        string expectedPath = Path.Combine(testDirectory, "newTheme.vstheme");
        File.WriteAllText(baseThemePath, "theme");
        ThemeInfo baseTheme = new("baseTheme", new VS2015LightTheme(), baseThemePath, VisualStudioToolStripExtender.VsVersion.Vs2015);
        ThemeInfo themeToSave = new("newTheme", new VS2015LightTheme(), "", VisualStudioToolStripExtender.VsVersion.Vs2015);

        try
        {
            ThemeSerializer.SaveToXmlFile(themeToSave, baseTheme);

            Assert.That(themeToSave.URI, Is.EqualTo(expectedPath));
            Assert.That(File.Exists(expectedPath), Is.True);
        }
        finally
        {
            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);
        }
    }

    [Test]
    public void CreateVariant_OverridesPaletteWithoutChangingBaseTheme()
    {
        string sourcePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Themes", "vs2015dark.vstheme");
        ThemeInfo baseTheme = ThemeSerializer.LoadFromXmlFile(sourcePath);
        Color originalBackground = baseTheme.ExtendedPalette.getColor("Dialog_Background");
        Color variantBackground = Color.FromArgb(15, 23, 42);

        ThemeInfo variant = ThemeSerializer.CreateVariant(
            baseTheme,
            "Midnight Slate",
            new Dictionary<string, Color> { ["Dialog_Background"] = variantBackground });

        Assert.Multiple(() =>
        {
            Assert.That(variant.Name, Is.EqualTo("Midnight Slate"));
            Assert.That(variant.ExtendedPalette.getColor("Dialog_Background"), Is.EqualTo(variantBackground));
            Assert.That(baseTheme.ExtendedPalette.getColor("Dialog_Background"), Is.EqualTo(originalBackground));
            Assert.That(variant.IsThemeBase, Is.True);
            Assert.That(variant.IsExtendable, Is.True);
            Assert.That(variant.URI, Is.EqualTo(sourcePath));
        });
    }

    [Test]
    public void ModernThemeCatalog_ProvidesDistinctAccessibleThemes()
    {
        string themeDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "Themes");
        ThemeInfo lightBase = ThemeSerializer.LoadFromXmlFile(Path.Combine(themeDirectory, "vs2015light.vstheme"));
        ThemeInfo darkBase = ThemeSerializer.LoadFromXmlFile(Path.Combine(themeDirectory, "vs2015dark.vstheme"));

        List<ThemeInfo> themes = ModernThemeCatalog.CreateBuiltInThemes(lightBase, darkBase).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(themes, Has.Count.EqualTo(6));
            Assert.That(themes.Select(theme => theme.Name), Is.Unique);
            Assert.That(themes.Select(theme => theme.Name),
                        Is.EquivalentTo(new[]
                        {
                            "Midnight Slate",
                            "Nord Frost",
                            "Tokyo Night",
                            "Graphite Violet",
                            "Cloud Blue",
                            "Mint Breeze"
                        }));

            foreach (ThemeInfo theme in themes)
            {
                Color background = theme.ExtendedPalette.getColor("Dialog_Background");
                Color foreground = theme.ExtendedPalette.getColor("Dialog_Foreground");
                Assert.That(ContrastRatio(background, foreground), Is.GreaterThanOrEqualTo(4.5),
                            $"{theme.Name} dialog text contrast");

                AssertThemeContrast(theme, "Button_Background", "Button_Foreground");
                AssertThemeContrast(theme, "Button_Hover_Background", "Button_Hover_Foreground");
                AssertThemeContrast(theme, "Button_Pressed_Background", "Button_Pressed_Foreground");
            }
        });
    }

    private static void AssertThemeContrast(ThemeInfo theme, string backgroundKey, string foregroundKey)
    {
        Color background = theme.ExtendedPalette.getColor(backgroundKey);
        Color foreground = theme.ExtendedPalette.getColor(foregroundKey);
        Assert.That(ContrastRatio(background, foreground), Is.GreaterThanOrEqualTo(4.5),
                    $"{theme.Name} {backgroundKey} text contrast");
    }

    private static double ContrastRatio(Color first, Color second)
    {
        double firstLuminance = RelativeLuminance(first);
        double secondLuminance = RelativeLuminance(second);
        double lighter = Math.Max(firstLuminance, secondLuminance);
        double darker = Math.Min(firstLuminance, secondLuminance);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Color color)
    {
        static double Linearize(byte component)
        {
            double channel = component / 255.0;
            return channel <= 0.04045
                ? channel / 12.92
                : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Linearize(color.R) +
               0.7152 * Linearize(color.G) +
               0.0722 * Linearize(color.B);
    }
}
