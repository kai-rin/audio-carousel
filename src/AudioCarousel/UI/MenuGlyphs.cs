using System.Drawing;
using System.Drawing.Text;

namespace AudioCarousel.UI;

/// <summary>
/// Windows' own icon font glyphs (Segoe Fluent Icons on Windows 11, Segoe
/// MDL2 Assets on Windows 10) rendered to bitmaps for menus and the toast,
/// so the UI matches the system look without shipping image files.
/// </summary>
public static class MenuGlyphs
{
    // Code points shared by both fonts.
    public const char Speaker = '\uE767';
    public const char Microphone = '\uE720';
    public const char Next = '\uE72C';      // Refresh (clockwise loop)
    public const char Previous = '\uE7A7';  // Undo (counter-clockwise)
    public const char Settings = '\uE713';
    public const char Info = '\uE946';
    public const char Exit = '\uE7E8';      // Power
    public const char Warning = '\uE7BA';
    public const char Close = '\uE8BB';
    public const char Check = '\uE73E';
    public const char ChevronRight = '\uE76C';

    private static readonly Lazy<string?> FontName = new(() =>
    {
        using var fonts = new InstalledFontCollection();
        var names = fonts.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return names.Contains("Segoe Fluent Icons") ? "Segoe Fluent Icons"
            : names.Contains("Segoe MDL2 Assets") ? "Segoe MDL2 Assets"
            : null;
    });

    public static bool Available => FontName.Value is not null;

    /// <summary>Glyph centered in a transparent square bitmap; null if no icon font.</summary>
    public static Bitmap? Render(char glyph, int size, Color color)
    {
        if (FontName.Value is not string fontName) return null;
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        // GDI+ (not GDI) text so the glyph keeps real alpha on a transparent bitmap.
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var font = new Font(fontName, size * 0.75f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        g.DrawString(glyph.ToString(), font, brush, new RectangleF(0, 0, size, size), format);
        return bmp;
    }
}
