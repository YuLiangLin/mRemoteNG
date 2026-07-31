using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.Themes;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI.Tabs
{
    /// <summary>
    /// Theme-aware caption for docked tool windows.
    /// DockPanelSuite's built-in caption reads its startup palette, so it does
    /// not follow mRemoteNG's live theme preview while panes remain open.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [ToolboxItem(false)]
    internal sealed class MremoteNGDockPaneCaption : DockPaneCaptionBase
    {
        private const int TextGapTop = 3;
        private const int TextGapBottom = 2;
        private const int TextGapLeft = 6;
        private const int TextGapRight = 4;
        private const int ButtonGapTop = 4;
        private const int ButtonGapBottom = 3;
        private const int ButtonGapBetween = 1;
        private const int ButtonGapRight = 5;

        private static readonly TextFormatFlags CaptionTextFormat =
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.SingleLine |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix;

        private enum CaptionGlyph
        {
            Close,
            AutoHide,
            Options
        }

        private sealed class CaptionButton(
            CaptionGlyph glyph,
            Func<Color> captionBackground,
            Func<Color> captionForeground,
            Func<bool> isAutoHidden) : Control
        {
            private bool _hovered;
            private bool _pressed;

            internal CaptionButton(
                CaptionGlyph glyph,
                Func<Color> captionBackground,
                Func<Color> captionForeground)
                : this(glyph, captionBackground, captionForeground, () => false)
            {
            }

            protected override Size DefaultSize => new(16, 15);

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                Color background = captionBackground();
                if (_pressed)
                {
                    background = RuntimeThemeColorResolver.Resolve(
                        "Button_Pressed_Background",
                        background);
                }
                else if (_hovered)
                {
                    background = RuntimeThemeColorResolver.Resolve(
                        "Button_Hover_Background",
                        background);
                }

                e.Graphics.Clear(background);

                if (!_hovered && !_pressed)
                    return;

                Color border = RuntimeThemeColorResolver.Resolve(
                    _pressed ? "Button_Pressed_Border" : "Button_Hover_Border",
                    captionForeground());
                using Pen borderPen = new(border);
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Color foreground = Enabled
                    ? RuntimeThemeColorResolver.Resolve(
                        _pressed
                            ? "Button_Pressed_Foreground"
                            : _hovered
                                ? "Button_Hover_Foreground"
                                : "Tab_Item_Foreground",
                        captionForeground())
                    : RuntimeThemeColorResolver.Resolve(
                        "Button_Disabled_Foreground",
                        SystemColors.GrayText);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using Pen glyphPen = new(foreground, 1.5F)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };

                switch (glyph)
                {
                    case CaptionGlyph.Close:
                        e.Graphics.DrawLine(glyphPen, 5, 4, 11, 10);
                        e.Graphics.DrawLine(glyphPen, 11, 4, 5, 10);
                        break;
                    case CaptionGlyph.AutoHide:
                        DrawAutoHideGlyph(e.Graphics, glyphPen, isAutoHidden());
                        break;
                    case CaptionGlyph.Options:
                        using (SolidBrush dotBrush = new(foreground))
                        {
                            e.Graphics.FillEllipse(dotBrush, 4, 6, 2, 2);
                            e.Graphics.FillEllipse(dotBrush, 7, 6, 2, 2);
                            e.Graphics.FillEllipse(dotBrush, 10, 6, 2, 2);
                        }
                        break;
                }
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                _hovered = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hovered = false;
                _pressed = false;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button != MouseButtons.Left)
                    return;

                _pressed = true;
                Invalidate();
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                _pressed = false;
                Invalidate();
            }

            protected override void OnEnabledChanged(EventArgs e)
            {
                base.OnEnabledChanged(e);
                Invalidate();
            }

            private static void DrawAutoHideGlyph(Graphics graphics, Pen pen, bool autoHidden)
            {
                GraphicsState state = graphics.Save();
                if (autoHidden)
                {
                    graphics.TranslateTransform(8, 7);
                    graphics.RotateTransform(90);
                    graphics.TranslateTransform(-8, -7);
                }

                graphics.DrawLine(pen, 5, 4, 11, 4);
                graphics.DrawLine(pen, 6, 4, 6, 7);
                graphics.DrawLine(pen, 10, 4, 10, 7);
                graphics.DrawLine(pen, 5, 7, 11, 7);
                graphics.DrawLine(pen, 8, 7, 8, 12);
                graphics.Restore(state);
            }
        }

        private readonly CaptionButton _buttonClose;
        private readonly CaptionButton _buttonAutoHide;
        private readonly CaptionButton _buttonOptions;
        private readonly ToolTip _toolTip = new();

        internal MremoteNGDockPaneCaption(DockPane pane)
            : base(pane)
        {
            SuspendLayout();

            _buttonClose = new CaptionButton(
                CaptionGlyph.Close,
                () => CaptionBackground,
                () => CaptionForeground);
            _buttonClose.Click += (_, _) => DockPane.CloseActiveContent();
            _toolTip.SetToolTip(_buttonClose, "Close");

            _buttonAutoHide = new CaptionButton(
                CaptionGlyph.AutoHide,
                () => CaptionBackground,
                () => CaptionForeground,
                () => DockPane.IsAutoHide);
            _buttonAutoHide.Click += (_, _) => ToggleAutoHide();
            _toolTip.SetToolTip(_buttonAutoHide, "Auto hide");

            _buttonOptions = new CaptionButton(
                CaptionGlyph.Options,
                () => CaptionBackground,
                () => CaptionForeground);
            _buttonOptions.Click += (_, _) =>
                ShowTabPageContextMenu(PointToClient(Control.MousePosition));
            _toolTip.SetToolTip(_buttonOptions, "Options");

            Controls.AddRange([_buttonClose, _buttonAutoHide, _buttonOptions]);
            SetButtons();
            ResumeLayout();
        }

        private Font TextFont => DockPane.DockPanel.Theme.Skin.DockPaneStripSkin.TextFont;

        private Color CaptionBackground =>
            RuntimeThemeColorResolver.Resolve(
                DockPane.IsActivePane
                    ? "Treeview_SelectedItem_Active_Background"
                    : "Tab_Item_Background",
                DockPane.IsActivePane
                    ? DockPane.DockPanel.Theme.ColorPalette.ToolWindowCaptionActive.Background
                    : DockPane.DockPanel.Theme.ColorPalette.ToolWindowCaptionInactive.Background);

        private Color CaptionForeground =>
            RuntimeThemeColorResolver.Resolve(
                DockPane.IsActivePane
                    ? "Treeview_SelectedItem_Active_Foreground"
                    : "Tab_Item_Foreground",
                DockPane.IsActivePane
                    ? DockPane.DockPanel.Theme.ColorPalette.ToolWindowCaptionActive.Text
                    : DockPane.DockPanel.Theme.ColorPalette.ToolWindowCaptionInactive.Text);

        private Color CaptionBorder =>
            RuntimeThemeColorResolver.Resolve(
                "List_Item_Border",
                DockPane.DockPanel.Theme.ColorPalette.ToolWindowBorder);

        protected override bool CanDragAutoHide => true;

        protected override int MeasureHeight()
        {
            int textHeight = TextFont.Height + TextGapTop + TextGapBottom;
            int buttonHeight = _buttonClose.Height + ButtonGapTop + ButtonGapBottom;
            return Math.Max(textHeight, buttonHeight);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color background = CaptionBackground;
            if (BackColor != background)
                BackColor = background;

            e.Graphics.Clear(background);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            SetButtons();
            SetButtonsPosition();
            base.OnPaint(e);
            if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
                return;

            using Pen borderPen = new(CaptionBorder);
            e.Graphics.DrawLine(borderPen, ClientRectangle.Left, ClientRectangle.Top,
                                ClientRectangle.Right - 1, ClientRectangle.Top);
            e.Graphics.DrawLine(borderPen, ClientRectangle.Left, ClientRectangle.Top,
                                ClientRectangle.Left, ClientRectangle.Bottom - 1);
            e.Graphics.DrawLine(borderPen, ClientRectangle.Right - 1, ClientRectangle.Top,
                                ClientRectangle.Right - 1, ClientRectangle.Bottom - 1);

            Rectangle textBounds = ClientRectangle;
            textBounds.X += TextGapLeft;
            textBounds.Width -= TextGapLeft + TextGapRight + VisibleButtonsWidth();
            textBounds.Y += TextGapTop;
            textBounds.Height -= TextGapTop + TextGapBottom;

            TextFormatFlags flags = CaptionTextFormat;
            if (RightToLeft == RightToLeft.Yes)
                flags |= TextFormatFlags.RightToLeft | TextFormatFlags.Right;

            textBounds = DrawHelper.RtlTransform(this, textBounds);
            TextRenderer.DrawText(e.Graphics, DockPane.CaptionText, TextFont,
                                  textBounds, CaptionForeground, flags);

            DrawGrip(e.Graphics, textBounds);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            SetButtons();
            SetButtonsPosition();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            SetButtons();
            SetButtonsPosition();
        }

        protected override void OnRefreshChanges()
        {
            SetButtons();
            SetButtonsPosition();
            _buttonClose.Invalidate();
            _buttonAutoHide.Invalidate();
            _buttonOptions.Invalidate();
            Invalidate(true);
        }

        protected override void OnRightToLeftChanged(EventArgs e)
        {
            base.OnRightToLeftChanged(e);
            PerformLayout();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _toolTip.Dispose();

            base.Dispose(disposing);
        }

        private void SetButtons()
        {
            _buttonClose.Enabled = DockPane.ActiveContent?.DockHandler.CloseButton == true;
            _buttonClose.Visible = DockPane.ActiveContent?.DockHandler.CloseButtonVisible == true;
            _buttonAutoHide.Visible = !DockPane.IsFloat;
            _buttonOptions.Visible = HasTabPageContextMenu;
        }

        private void SetButtonsPosition()
        {
            int x = ClientRectangle.Right - ButtonGapRight;
            int y = ClientRectangle.Top + ButtonGapTop;

            PositionButton(_buttonClose, ref x, y);
            PositionButton(_buttonAutoHide, ref x, y);
            PositionButton(_buttonOptions, ref x, y);
        }

        private void PositionButton(CaptionButton button, ref int x, int y)
        {
            if (!button.Visible)
                return;

            Size size = button.Size;
            x -= size.Width;
            Rectangle bounds =
                DrawHelper.RtlTransform(this, new Rectangle(new Point(x, y), size));
            if (button.Bounds != bounds)
                button.Bounds = bounds;
            x -= ButtonGapBetween;
        }

        private int VisibleButtonsWidth()
        {
            int width = ButtonGapRight;
            if (_buttonClose.Visible)
                width += _buttonClose.Width + ButtonGapBetween;
            if (_buttonAutoHide.Visible)
                width += _buttonAutoHide.Width + ButtonGapBetween;
            if (_buttonOptions.Visible)
                width += _buttonOptions.Width + ButtonGapBetween;
            return width;
        }

        private void DrawGrip(Graphics graphics, Rectangle textBounds)
        {
            int captionWidth = TextRenderer.MeasureText(
                graphics,
                DockPane.CaptionText,
                TextFont,
                textBounds.Size,
                CaptionTextFormat).Width;
            int start = textBounds.Left + Math.Min(captionWidth + 6, textBounds.Width);
            int end = textBounds.Right - 2;
            if (end <= start)
                return;

            using Pen gripPen = new(CaptionForeground)
            {
                DashStyle = DashStyle.Dot
            };
            int center = ClientRectangle.Height / 2;
            graphics.DrawLine(gripPen, start, center - 2, end, center - 2);
            graphics.DrawLine(gripPen, start + 2, center + 1, end, center + 1);
        }

        private void ToggleAutoHide()
        {
            DockPane.DockState = DockHelper.ToggleAutoHideState(DockPane.DockState);
            if (!DockHelper.IsDockStateAutoHide(DockPane.DockState))
                return;

            DockPane.DockPanel.ActiveAutoHideContent = null;
            DockPane.NestedDockingStatus.NestedPanes.SwitchPaneWithFirstChild(DockPane);
        }
    }
}
