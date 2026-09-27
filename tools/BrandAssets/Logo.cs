using System.Globalization;
using System.Text;

namespace BrandAssets;

/// <summary>
/// The Audio Carousel mark — headphones around a "sync" loop — as SVG.
/// Single source for the app/tray icons and the logo in the hero image.
/// Drawn on a 100×100 grid.
/// </summary>
public static class Logo
{
    public const string Navy = "#101C36";
    public const string Blue = "#2563EB";
    public const string LightBlue = "#60A5FA";

    public enum Detail
    {
        // 32 px and up: two slim arrows.
        Full,
        // 24 px: heavier strokes, bigger loop.
        Simplified,
        // 16–20 px: one bold arrow, clearly separated ear cups.
        Tiny,
    }

    public static Detail DetailFor(int pixelSize) =>
        pixelSize <= 20 ? Detail.Tiny : pixelSize <= 24 ? Detail.Simplified : Detail.Full;

    public static string Svg(double size, Detail detail, string head = Navy, string arrow = Blue)
    {
        var svg = new StringBuilder();
        svg.Append(Invariant($"<svg width=\"{size}\" height=\"{size}\" viewBox=\"0 0 100 100\" style=\"display:block\">"));
        switch (detail)
        {
            case Detail.Tiny:
                Band(svg, "M17 56 a33 33 0 0 1 66 0", 11, head);
                Cup(svg, 4, 50, 19, 40, 8, head);
                Cup(svg, 77, 50, 19, 40, 8, head);
                ArrowArc(svg, from: 250, to: 520, r: 15, stroke: 10, headSize: 12, arrow, cy: 69);
                break;
            case Detail.Simplified:
                Band(svg, "M17 60 a33 33 0 0 1 66 0", 14, head);
                Cup(svg, 5, 52, 22, 36, 9, head);
                Cup(svg, 73, 52, 22, 36, 9, head);
                ArrowArc(svg, 200, 318, 20, 11, 11, arrow);
                ArrowArc(svg, 20, 138, 20, 11, 11, arrow);
                break;
            default:
                Band(svg, "M16 60 a34 34 0 0 1 68 0", 10, head);
                Cup(svg, 7, 52, 19, 36, 8, head);
                Cup(svg, 74, 52, 19, 36, 8, head);
                ArrowArc(svg, 200, 318, 17, 7.5, 7, arrow);
                ArrowArc(svg, 20, 138, 17, 7.5, 7, arrow);
                break;
        }
        svg.Append("</svg>");
        return svg.ToString();
    }

    private static void Band(StringBuilder svg, string d, double width, string color) =>
        svg.Append(Invariant($"<path d=\"{d}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{width}\" stroke-linecap=\"round\"/>"));

    private static void Cup(StringBuilder svg, double x, double y, double w, double h, double rx, string color) =>
        svg.Append(Invariant($"<rect x=\"{x}\" y=\"{y}\" width=\"{w}\" height=\"{h}\" rx=\"{rx}\" fill=\"{color}\"/>"));

    // One clockwise arc from `from` to `to` degrees (0° = 3 o'clock) around
    // (50, cy), with an arrowhead at `to`.
    private static void ArrowArc(StringBuilder svg, double from, double to, double r, double stroke,
        double headSize, string color, double cy = 66)
    {
        (double X, double Y) At(double deg, double radius)
        {
            double a = deg * Math.PI / 180;
            return (50 + radius * Math.Cos(a), cy + radius * Math.Sin(a));
        }

        var s = At(from, r);
        var e = At(to, r);
        // Arrowhead: base straddles the ring at `to`, tip runs ahead along it.
        var inner = At(to, r - headSize);
        var outer = At(to, r + headSize);
        var tip = At(to + headSize * 1.9 * 180 / (Math.PI * r), r);
        int largeArc = to - from > 180 ? 1 : 0;
        svg.Append(Invariant(
            $"<path d=\"M {s.X:0.###} {s.Y:0.###} A {r} {r} 0 {largeArc} 1 {e.X:0.###} {e.Y:0.###}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{stroke}\" stroke-linecap=\"round\"/>"));
        svg.Append(Invariant(
            $"<polygon points=\"{inner.X:0.###},{inner.Y:0.###} {outer.X:0.###},{outer.Y:0.###} {tip.X:0.###},{tip.Y:0.###}\" fill=\"{color}\" stroke=\"{color}\" stroke-width=\"{stroke * 0.25}\" stroke-linejoin=\"round\"/>"));
    }

    private static string Invariant(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
}
