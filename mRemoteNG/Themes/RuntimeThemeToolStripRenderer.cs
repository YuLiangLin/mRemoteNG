using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace mRemoteNG.Themes
{
    /// <summary>
    /// Paints WinForms command bars from the active extended palette instead of
    /// relying on system colors hidden inside ToolStripProfessionalRenderer.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class RuntimeThemeToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly ExtendedColorPalette _palette;

        internal RuntimeThemeToolStripRenderer(ExtendedColorPalette palette)
            : base(new RuntimeThemeToolStripColorTable(palette))
        {
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            Color background = e.ToolStrip is ToolStripDropDown
                ? Color("List_Background")
                : Color("CommandBarMenuDefault_Background");

            using SolidBrush brush = new(background);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            Color border = Color("List_Item_Border");
            using Pen pen = new(border);

            if (e.ToolStrip is ToolStripDropDown)
            {
                Rectangle bounds = e.ToolStrip.ClientRectangle;
                bounds.Width -= 1;
                bounds.Height -= 1;
                e.Graphics.DrawRectangle(pen, bounds);
                return;
            }

            int bottom = e.ToolStrip.ClientRectangle.Bottom - 1;
            e.Graphics.DrawLine(pen, 0, bottom, e.ToolStrip.ClientRectangle.Right, bottom);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = !e.Item.Enabled
                ? Color("Button_Disabled_Foreground")
                : e.Item.Pressed
                    ? Color("Button_Pressed_Foreground")
                    : e.Item.Selected
                        ? Color("Button_Hover_Foreground")
                        : Color("CommandBarMenuDefault_Foreground");

            base.OnRenderItemText(e);
        }

        private Color Color(string key) => _palette.getColor(key);
    }

    [SupportedOSPlatform("windows")]
    internal static class RuntimeThemeToolStripStyler
    {
        internal static RuntimeThemeToolStripRenderer Apply(ToolStrip toolStrip,
                                                            ExtendedColorPalette palette)
        {
            if (toolStrip == null)
                throw new ArgumentNullException(nameof(toolStrip));
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            RuntimeThemeToolStripRenderer renderer = new(palette);
            Apply(toolStrip, renderer, palette);
            return renderer;
        }

        private static void Apply(ToolStrip toolStrip, ToolStripRenderer renderer,
                                  ExtendedColorPalette palette)
        {
            toolStrip.Renderer = renderer;
            toolStrip.BackColor = toolStrip is ToolStripDropDown
                ? palette.getColor("List_Background")
                : palette.getColor("CommandBarMenuDefault_Background");
            toolStrip.ForeColor = palette.getColor("CommandBarMenuDefault_Foreground");

            foreach (ToolStripItem item in toolStrip.Items)
            {
                item.ForeColor = toolStrip.ForeColor;
                if (item is ToolStripDropDownItem dropDownItem && dropDownItem.HasDropDownItems)
                    Apply(dropDownItem.DropDown, renderer, palette);
            }

            toolStrip.Invalidate(true);
        }
    }

    [SupportedOSPlatform("windows")]
    internal sealed class RuntimeThemeToolStripColorTable : ProfessionalColorTable
    {
        private readonly ExtendedColorPalette _palette;

        internal RuntimeThemeToolStripColorTable(ExtendedColorPalette palette)
        {
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            UseSystemColors = false;
        }

        private Color MenuBackground => Color("CommandBarMenuDefault_Background");
        private Color PopupBackground => Color("List_Background");
        private Color Border => Color("List_Item_Border");
        private Color Hover => Color("Button_Hover_Background");
        private Color HoverBorder => Color("Button_Hover_Border");
        private Color Pressed => Color("Button_Pressed_Background");
        private Color PressedBorder => Color("Button_Pressed_Border");
        private Color Checked => Color("List_Item_Selected_Background");
        private Color Color(string key) => _palette.getColor(key);

        public override Color MenuStripGradientBegin => MenuBackground;
        public override Color MenuStripGradientEnd => MenuBackground;
        public override Color ToolStripGradientBegin => MenuBackground;
        public override Color ToolStripGradientMiddle => MenuBackground;
        public override Color ToolStripGradientEnd => MenuBackground;
        public override Color ToolStripDropDownBackground => PopupBackground;
        public override Color ToolStripBorder => Border;
        public override Color RaftingContainerGradientBegin => MenuBackground;
        public override Color RaftingContainerGradientEnd => MenuBackground;
        public override Color ImageMarginGradientBegin => PopupBackground;
        public override Color ImageMarginGradientMiddle => PopupBackground;
        public override Color ImageMarginGradientEnd => PopupBackground;
        public override Color MenuItemBorder => HoverBorder;
        public override Color MenuItemSelected => Hover;
        public override Color MenuItemSelectedGradientBegin => Hover;
        public override Color MenuItemSelectedGradientEnd => Hover;
        public override Color MenuItemPressedGradientBegin => Pressed;
        public override Color MenuItemPressedGradientMiddle => Pressed;
        public override Color MenuItemPressedGradientEnd => Pressed;
        public override Color ButtonSelectedBorder => HoverBorder;
        public override Color ButtonSelectedGradientBegin => Hover;
        public override Color ButtonSelectedGradientMiddle => Hover;
        public override Color ButtonSelectedGradientEnd => Hover;
        public override Color ButtonPressedBorder => PressedBorder;
        public override Color ButtonPressedGradientBegin => Pressed;
        public override Color ButtonPressedGradientMiddle => Pressed;
        public override Color ButtonPressedGradientEnd => Pressed;
        public override Color ButtonCheckedGradientBegin => Checked;
        public override Color ButtonCheckedGradientMiddle => Checked;
        public override Color ButtonCheckedGradientEnd => Checked;
        public override Color ButtonCheckedHighlight => Checked;
        public override Color ButtonCheckedHighlightBorder => Color("List_Item_Selected_Border");
        public override Color CheckBackground => Checked;
        public override Color CheckSelectedBackground => Hover;
        public override Color CheckPressedBackground => Pressed;
        public override Color GripDark => Border;
        public override Color GripLight => MenuBackground;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => MenuBackground;
        public override Color OverflowButtonGradientBegin => MenuBackground;
        public override Color OverflowButtonGradientMiddle => MenuBackground;
        public override Color OverflowButtonGradientEnd => MenuBackground;
    }
}
