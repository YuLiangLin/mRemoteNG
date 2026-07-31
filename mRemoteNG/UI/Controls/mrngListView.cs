using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using BrightIdeasSoftware;
using mRemoteNG.App;
using mRemoteNG.Themes;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    //Simple coloring of ObjectListView
    //This is subclassed to avoid repeating the code in multiple places
    internal class MrngListView : ObjectListView
    {
        private const uint LvmFirst = 0x1000;
        private const uint LvmGetHeader = LvmFirst + 31;
        private const int WsVScroll = 0x00200000;
        private const int WmSize = 0x0005;
        private const int WmPaint = 0x000F;
        private const int WmStyleChanged = 0x007D;

        private CellBorderDecoration deco;
        private Pen _decorationPen;
        private readonly HeaderScrollCorner _headerScrollCorner = new();

        //Control if the gridlines are styled, must be set before the OnCreateControl is fired
        public bool DecorateLines { get; set; } = true;

        public MrngListView()
        {
            InitializeComponent();
            Controls.Add(_headerScrollCorner);
            ThemeManager.getInstance().ThemeChanged += ApplyTheme;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            ThemeManager themeManager = ThemeManager.getInstance();
            if (!themeManager.ActiveAndExtended)
                return;

            //List back color
            BackColor = themeManager.ActiveTheme.ExtendedPalette.getColor("List_Background");
            ForeColor = themeManager.ActiveTheme.ExtendedPalette.getColor("List_Item_Foreground");
            //Selected item
            Color selectedBackColor =
                themeManager.ActiveTheme.ExtendedPalette.getColor("List_Item_Selected_Background");
            Color selectedForeColor =
                themeManager.ActiveTheme.ExtendedPalette.getColor("List_Item_Selected_Foreground");
            SelectedBackColor = selectedBackColor;
            SelectedForeColor = selectedForeColor;
            UnfocusedSelectedBackColor = selectedBackColor;
            UnfocusedSelectedForeColor = selectedForeColor;

            //Header style
            HeaderUsesThemes = false;
            Color headerBackColor =
                themeManager.ActiveTheme.ExtendedPalette.getColor("List_Header_Background");
            Color headerForeColor =
                themeManager.ActiveTheme.ExtendedPalette.getColor("List_Header_Foreground");
            Color borderColor =
                themeManager.ActiveTheme.ExtendedPalette.getColor("List_Item_Border");
            HeaderFormatStyle headerStyle = new()
            {
                Normal =
                {
                    BackColor = headerBackColor,
                    ForeColor = headerForeColor,
                    FrameColor = borderColor,
                    FrameWidth = 1
                },
                Hot =
                {
                    BackColor = selectedBackColor,
                    ForeColor = selectedForeColor,
                    FrameColor = borderColor,
                    FrameWidth = 1
                },
                Pressed =
                {
                    BackColor = selectedBackColor,
                    ForeColor = selectedForeColor,
                    FrameColor = borderColor,
                    FrameWidth = 1
                }
            };
            HeaderFormatStyle = headerStyle;
            _headerScrollCorner.BackColor = headerBackColor;
            _headerScrollCorner.BorderColor = borderColor;
            //Border style
            if (DecorateLines)
            {
                UseCellFormatEvents = true;
                GridLines = false;
                _decorationPen?.Dispose();
                _decorationPen = new Pen(borderColor);
                deco = new CellBorderDecoration
                {
                    BorderPen = _decorationPen,
                    FillBrush = null,
                    BoundsPadding = Size.Empty,
                    CornerRounding = 0
                };
                FormatCell -= NGListView_FormatCell;
                FormatCell += NGListView_FormatCell;
            }

            if (Items != null && Items.Count != 0)
                BuildList();

            ApplyNativeTheme(themeManager.IsActiveThemeDark);
            UpdateHeaderScrollCorner();
            Invalidate(true);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            UpdateHeaderScrollCorner();
        }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);

            if (message.Msg == WmSize ||
                message.Msg == WmPaint ||
                message.Msg == WmStyleChanged)
            {
                UpdateHeaderScrollCorner();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.getInstance().ThemeChanged -= ApplyTheme;
                FormatCell -= NGListView_FormatCell;
                _decorationPen?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void ApplyNativeTheme(bool dark)
        {
            if (!IsHandleCreated)
                return;

            string themeName = dark ? "DarkMode_Explorer" : "Explorer";
            NativeMethods.SetWindowTheme(Handle, themeName, null);

            IntPtr headerHandle =
                NativeMethods.SendMessage(Handle, LvmGetHeader, IntPtr.Zero, IntPtr.Zero);
            if (headerHandle != IntPtr.Zero)
                NativeMethods.SetWindowTheme(headerHandle, themeName, null);
        }

        private void UpdateHeaderScrollCorner()
        {
            if (!IsHandleCreated || HeaderControl == null)
                return;

            bool hasVerticalScrollBar =
                (NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_STYLE) & WsVScroll) != 0;
            int headerHeight = HeaderControl.ClientRectangle.Height;
            int width = SystemInformation.VerticalScrollBarWidth;
            bool shouldShow =
                hasVerticalScrollBar && headerHeight > 0 && ClientSize.Width > width;

            if (!shouldShow)
            {
                _headerScrollCorner.Visible = false;
                return;
            }

            Rectangle bounds = new(
                ClientSize.Width - width - 1,
                1,
                width,
                headerHeight);
            if (_headerScrollCorner.Bounds != bounds)
                _headerScrollCorner.Bounds = bounds;
            if (!_headerScrollCorner.Visible)
                _headerScrollCorner.Visible = true;

            if (Controls.GetChildIndex(_headerScrollCorner) != 0)
                _headerScrollCorner.BringToFront();
        }

        private void NGListView_FormatCell(object sender, FormatCellEventArgs e)
        {
            if (e.Column.IsVisible)
            {
                e.SubItem.Decoration = deco;
            }
        }

        private void InitializeComponent()
        {
            ((ISupportInitialize)(this)).BeginInit();
            SuspendLayout();
            // 
            // NGListView
            // 
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ((ISupportInitialize)(this)).EndInit();
            ResumeLayout(false);
        }

        private sealed class HeaderScrollCorner : Control
        {
            internal Color BorderColor { get; set; } = SystemColors.ControlDark;

            internal HeaderScrollCorner()
            {
                Name = "ThemeHeaderScrollCorner";
                TabStop = false;
                Visible = false;
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.UserPaint,
                    true);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                e.Graphics.Clear(BackColor);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using Pen borderPen = new(BorderColor);
                e.Graphics.DrawLine(borderPen, 0, 0, 0, Height - 1);
                e.Graphics.DrawLine(borderPen, 0, Height - 1, Width - 1, Height - 1);
            }
        }
    }
}
