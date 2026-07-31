using System;
using System.ComponentModel;
using System.Windows.Forms;
using mRemoteNG.Themes;
using System.Linq;
using System.Collections.Generic;
using BrightIdeasSoftware;
using mRemoteNG.Properties;
using mRemoteNG.UI.TaskDialog;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Forms.OptionsPages
{
    [SupportedOSPlatform("windows")]
    public sealed partial class ThemePage
    {
        #region Private Fields

        private readonly ThemeManager _themeManager;
        private ThemeInfo _oriActiveTheme;
        private bool _hasThemePreview;
        private readonly List<ThemeInfo> modifiedThemes = [];

        #endregion

        public ThemePage()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            PageIcon = Resources.ImageConverter.GetImageAsIcon(Properties.Resources.AppearanceEditor_16x);
            _themeManager = ThemeManager.getInstance();
            if (!_themeManager.ThemingActive) return;
            _themeManager = ThemeManager.getInstance();
            _themeManager.ThemeChanged += ApplyTheme;
        }

        public override string PageName
        {
            get => Language.Theme;
            set { }
        }

        public override void ApplyLanguage()
        {
            base.ApplyLanguage();

            btnThemeDelete.Text = Language._Delete;
            btnThemeNew.Text = Language._New;
            labelRestart.Text = Language.OptionsThemeLivePreviewHint;
            keyCol.Text = Language.Element;
            ColorCol.Text = Language.Color;
            ColorNameCol.Text = Language.ColorName;
        }

        private new void ApplyTheme()
        {
            if (!_themeManager.ThemingActive)
                return;
            base.ApplyTheme();
        }

        public override void LoadSettings()
        {
            RevertThemePreview();

            //At first we cannot create or delete themes, depends later on the type of selected theme
            btnThemeNew.Enabled = false;
            btnThemeDelete.Enabled = false;
            //Load the list of themes
            cboTheme.Items.Clear();
            // ReSharper disable once CoVariantArrayConversion
            cboTheme.Items.AddRange(_themeManager.LoadThemes().OrderBy(x => x.Name).ToArray());
            cboTheme.SelectedItem = _themeManager.ActiveTheme;
            // Store the original active theme for reverting
            _oriActiveTheme = _themeManager.ActiveTheme;
            cboTheme_SelectionChangeCommitted(this, new EventArgs());
            cboTheme.DisplayMember = "Name";

            listPalette.FormatCell += ListPalette_FormatCell; //Color cell formatter
        }

        private void ListPalette_FormatCell(object sender, FormatCellEventArgs e)
        {
            if (e.ColumnIndex != ColorCol.Index) return;
            PseudoKeyColor colorElem = (PseudoKeyColor)e.Model;
            e.SubItem.BackColor = colorElem.Value;
        }


        public override void SaveSettings()
        {
            base.SaveSettings();

            Properties.OptionsThemePage.Default.ThemingActive = true;

            if (cboTheme.SelectedItem != null
            ) // LoadSettings calls SaveSettings, so these might be null the first time around
            {
                ThemeInfo selectedTheme = (ThemeInfo)cboTheme.SelectedItem;
                if (!ReferenceEquals(_themeManager.ActiveTheme, selectedTheme))
                    _themeManager.PreviewTheme(selectedTheme);

                _themeManager.CommitActiveTheme();
                _oriActiveTheme = selectedTheme;
                _hasThemePreview = false;
            }

            foreach (ThemeInfo updatedTheme in modifiedThemes)
            {
                _themeManager.updateTheme(updatedTheme);
            }
            modifiedThemes.Clear();
        }

        public override void RevertSettings()
        {
            base.RevertSettings();
            RevertThemePreview();

            // Clear the modified themes list without saving
            modifiedThemes.Clear();
        }

        #region Private Methods

        #region Event Handlers

        private void cboTheme_SelectionChangeCommitted(object sender, EventArgs e)
        {
            btnThemeNew.Enabled = false;
            btnThemeDelete.Enabled = false;

            // don't display listPalette if it's not an Extendable theme...
            listPalette.CellClick -= ListPalette_CellClick;
            listPalette.Enabled = false;
            listPalette.Visible = false;

            if (!_themeManager.ThemingActive) return;

            btnThemeNew.Enabled = true;

            ThemeInfo selectedTheme = (ThemeInfo)cboTheme.SelectedItem;
            if (selectedTheme == null)
                return;

            if (ReferenceEquals(sender, cboTheme) && !ReferenceEquals(_themeManager.ActiveTheme, selectedTheme))
            {
                _themeManager.PreviewTheme(selectedTheme);
                _hasThemePreview = _oriActiveTheme != null &&
                                   !string.Equals(selectedTheme.Name, _oriActiveTheme.Name,
                                                  StringComparison.Ordinal);
            }

            if (selectedTheme.IsExtendable)
            {
                // it's Extendable, so now we can do this more expensive operations...
                listPalette.ClearObjects();
                ColorMeList(selectedTheme);
                listPalette.Enabled = true;
                listPalette.Visible = true;
                listPalette.CellClick += ListPalette_CellClick;
            }

            if (selectedTheme.IsThemeBase) return;

            btnThemeDelete.Enabled = true;
        }

        private void RevertThemePreview()
        {
            if (!_hasThemePreview || _oriActiveTheme == null)
                return;

            _themeManager.PreviewTheme(_oriActiveTheme);
            _hasThemePreview = false;
        }

        /// <summary>
        /// Edit an object, since KeyValuePair value cannot be set without creating a new object, a parallel object model exist in the list
        /// besides the one in the active theme, so any modification must be done to the two models
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListPalette_CellClick(object sender, CellClickEventArgs e)
        {
            PseudoKeyColor colorElem = (PseudoKeyColor)e.Model;

            ColorDialog colorDlg = new()
            {
                AllowFullOpen = true,
                FullOpen = true,
                AnyColor = true,
                SolidColorOnly = false,
                Color = colorElem.Value
            };

            if (colorDlg.ShowDialog() != DialogResult.OK) return;
            modifiedThemes.Add(_themeManager.ActiveTheme);
            _themeManager.ActiveTheme.ExtendedPalette.replaceColor(colorElem.Key, colorDlg.Color);
            colorElem.Value = colorDlg.Color;
            listPalette.RefreshObject(e.Model);
            _themeManager.refreshUI();
        }

        private void ColorMeList(ThemeInfo ti)
        {
            foreach (KeyValuePair<string, System.Drawing.Color> colorElem in ti.ExtendedPalette.ExtColorPalette)
            {
                string display = ThemeColorLabelProvider.GetDisplayName(colorElem.Key);
                listPalette.AddObject(new PseudoKeyColor(colorElem.Key, colorElem.Value, display));
            }
        }

        private void btnThemeNew_Click(object sender, EventArgs e)
        {
            using (FrmInputBox frmInputBox = new(Language.OptionsThemeNewThemeCaption, Language.OptionsThemeNewThemeText, _themeManager.ActiveTheme.Name))
            {
                DialogResult dr = frmInputBox.ShowDialog();
                if (dr != DialogResult.OK) return;
                if (_themeManager.isThemeNameOk(frmInputBox.returnValue))
                {
                    ThemeInfo? addedTheme = _themeManager.addTheme(_themeManager.ActiveTheme, frmInputBox.returnValue);
                    if (addedTheme != null)
                        _themeManager.ActiveTheme = addedTheme;
                    LoadSettings();
                }
                else
                {
                    CTaskDialog.ShowTaskDialogBox(this, Language.Errors, Language.OptionsThemeNewThemeError, "", "", "", "", "", "", ETaskDialogButtons.Ok, ESysIcons.Error, ESysIcons.Information, 0);
                }
            }
        }

        private void btnThemeDelete_Click(object sender, EventArgs e)
        {
            DialogResult res = CTaskDialog.ShowTaskDialogBox(this, Language.Warnings,
                                                    Language.OptionsThemeDeleteConfirmation, "", "", "", "", "", "",
                                                    ETaskDialogButtons.YesNo,
                                                    ESysIcons.Question, ESysIcons.Information, 0);

            if (res != DialogResult.Yes) return;
            if (modifiedThemes.Contains(_themeManager.ActiveTheme))
                modifiedThemes.Remove(_themeManager.ActiveTheme);
            _themeManager.deleteTheme(_themeManager.ActiveTheme);
            LoadSettings();
        }

        #endregion

        #endregion
    }
}
