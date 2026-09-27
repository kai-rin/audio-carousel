using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AudioCarousel.UI;

/// <summary>
/// Windows 11-style context menu look for the tray menu: no image-margin
/// gutter, rounded hover highlight, brand-blue checked state, flat border.
/// The README hero image draws its menu mock-up with the same palette
/// (promo/src/hero/UiMocks.tsx) — keep them in sync.
/// </summary>
public sealed class FluentMenuRenderer : ToolStripProfessionalRenderer
{
    public static readonly Color Accent = Color.FromArgb(0x25, 0x63, 0xEB);

    /// <summary>Tag for a non-interactive header row (no hover highlight).</summary>
    public const string HeaderTag = "header";

    /// <summary>Windows 11 row height: extra vertical padding on every item.</summary>
    public static void ApplySpacing(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            if (item is ToolStripMenuItem) item.Padding = new Padding(0, 4, 0, 4);
        }
    }

    private readonly Palette _p;

    public FluentMenuRenderer(bool dark) : base(new Table(dark ? Palette.Dark : Palette.Light))
    {
        _p = dark ? Palette.Dark : Palette.Light;
        RoundedEdges = false;
    }

    public Color Text => _p.Text;

    public sealed record Palette(Color Back, Color Border, Color Hover, Color Separator, Color Text, Color Dim)
    {
        public static readonly Palette Dark = new(
            Color.FromArgb(0x2C, 0x2C, 0x2C), Color.FromArgb(0x3F, 0x3F, 0x3F), Color.FromArgb(0x3D, 0x3D, 0x3D),
            Color.FromArgb(0x45, 0x45, 0x45), Color.White, Color.FromArgb(0x8A, 0x8A, 0x8A));

        public static readonly Palette Light = new(
            Color.FromArgb(0xF9, 0xF9, 0xF9), Color.FromArgb(0xDD, 0xDD, 0xDD), Color.FromArgb(0xEA, 0xEA, 0xEA),
            Color.FromArgb(0xE0, 0xE0, 0xE0), Color.FromArgb(0x1A, 0x1A, 0x1A), Color.FromArgb(0x8A, 0x8A, 0x8A));
    }

    private sealed class Table(Palette p) : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => p.Back;
        public override Color ImageMarginGradientBegin => p.Back;
        public override Color ImageMarginGradientMiddle => p.Back;
        public override Color ImageMarginGradientEnd => p.Back;
        public override Color MenuBorder => p.Border;
        public override Color MenuItemBorder => p.Hover;
        public override Color MenuItemSelected => p.Hover;
        public override Color SeparatorDark => p.Separator;
        public override Color SeparatorLight => p.Separator;
        public override Color CheckBackground => Accent;
        public override Color CheckSelectedBackground => Accent;
        public override Color CheckPressedBackground => Accent;
    }

    // No contrasting gutter behind the icons.
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(_p.Back);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(_p.Border);
        var r = e.AffectedBounds;
        e.Graphics.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
    }

    // Rounded, inset hover highlight instead of the classic full-width bar.
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled || Equals(e.Item.Tag, HeaderTag)) return;
        var r = new Rectangle(4, 1, e.Item.Width - 8, e.Item.Height - 2);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Rounded(r, 4);
        using var brush = new SolidBrush(_p.Hover);
        e.Graphics.FillPath(brush, path);
    }

    // Brand-blue rounded box behind a checked item's icon.
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var r = e.ImageRectangle;
        r.Inflate(3, 3);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = Rounded(r, 4))
        using (var brush = new SolidBrush(Accent))
            e.Graphics.FillPath(brush, path);
        if (e.Image is not null) e.Graphics.DrawImage(e.Image, e.ImageRectangle);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? _p.Text : _p.Dim;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = _p.Text;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        int y = e.Item.Height / 2;
        using var pen = new Pen(_p.Separator);
        e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>Ask DWM for Windows 11 rounded corners on a popup (no-op on Windows 10).</summary>
    public static void RoundCorners(IntPtr handle)
    {
        const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        int roundSmall = 3;
        _ = DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref roundSmall, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
