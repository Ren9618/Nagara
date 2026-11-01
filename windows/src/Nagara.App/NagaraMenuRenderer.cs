using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Nagara.App;

/// <summary>A restrained, system-font menu that matches the settings window.</summary>
internal sealed class NagaraMenuRenderer : ToolStripProfessionalRenderer
{
    private static readonly Color Accent = Color.FromArgb(15, 118, 110);
    private static readonly Color Selected = Color.FromArgb(231, 245, 242);
    private static readonly Color Border = Color.FromArgb(216, 225, 222);

    public NagaraMenuRenderer()
        : base(new Colors())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var bounds = e.ImageRectangle;
        if (bounds.Width < 8 || bounds.Height < 8)
        {
            bounds = new Rectangle(
                8,
                e.Item.Bounds.Top + ((e.Item.Bounds.Height - 16) / 2),
                16,
                16);
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Accent, 2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };

        var points = new[]
        {
            new PointF(bounds.Left + (bounds.Width * 0.15f), bounds.Top + (bounds.Height * 0.52f)),
            new PointF(bounds.Left + (bounds.Width * 0.42f), bounds.Top + (bounds.Height * 0.76f)),
            new PointF(bounds.Left + (bounds.Width * 0.86f), bounds.Top + (bounds.Height * 0.25f)),
        };
        e.Graphics.DrawLines(pen, points);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Border);
        var bounds = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        e.Graphics.DrawRectangle(pen, bounds);
    }

    private sealed class Colors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(253, 254, 254);

        public override Color ImageMarginGradientBegin => Color.FromArgb(253, 254, 254);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(253, 254, 254);
        public override Color ImageMarginGradientEnd => Color.FromArgb(253, 254, 254);

        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Selected;
        public override Color MenuItemSelected => Selected;
        public override Color MenuItemSelectedGradientBegin => Selected;
        public override Color MenuItemSelectedGradientEnd => Selected;

        public override Color MenuItemPressedGradientBegin => Selected;
        public override Color MenuItemPressedGradientMiddle => Selected;
        public override Color MenuItemPressedGradientEnd => Selected;

        public override Color SeparatorDark => Color.FromArgb(228, 235, 232);
        public override Color SeparatorLight => Color.FromArgb(246, 249, 248);

        public override Color CheckBackground => Color.Transparent;
        public override Color CheckSelectedBackground => Selected;
        public override Color CheckPressedBackground => Selected;
    }
}
