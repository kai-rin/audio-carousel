using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;

namespace BrandAssets;

/// <summary>
/// Renders every icon size on one transparent HTML sheet (one Edge launch),
/// crops each frame and writes app.ico, tray-light.ico and tray-dark.ico.
/// </summary>
public static class Icons
{
    public static readonly int[] Sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };
    private const int Gap = 10;
    private const int RowHeight = 256 + Gap;

    private static readonly (string File, string Kind)[] Variants =
    {
        ("app.ico", "app"),
        ("tray-light.ico", "trayLight"),
        ("tray-dark.ico", "trayDark"),
    };

    public static void Build(string repoRoot, string workDir)
    {
        int width = Sizes.Sum(s => s + Gap);
        int height = Variants.Length * RowHeight;
        string html = Path.Combine(workDir, "icon-sheet.html");
        string png = Path.Combine(workDir, "icon-sheet.png");
        File.WriteAllText(html, SheetHtml(width, height), Encoding.UTF8);
        EdgeRenderer.Screenshot(html, png, width, height, transparent: true);

        using var sheet = new Bitmap(png);
        string outDir = Path.Combine(repoRoot, "src", "AudioCarousel", "Resources");
        for (int row = 0; row < Variants.Length; row++)
        {
            var frames = new List<Bitmap>();
            int x = 0;
            foreach (int size in Sizes)
            {
                frames.Add(sheet.Clone(new Rectangle(x, row * RowHeight, size, size), PixelFormat.Format32bppArgb));
                x += size + Gap;
            }
            string path = Path.Combine(outDir, Variants[row].File);
            IcoWriter.Write(frames, path);
            frames.ForEach(f => f.Dispose());
            Console.WriteLine($"wrote {Path.GetRelativePath(repoRoot, path)}");
        }
    }

    private static string SheetHtml(int width, int height)
    {
        var body = new StringBuilder();
        for (int row = 0; row < Variants.Length; row++)
        {
            int x = 0;
            foreach (int size in Sizes)
            {
                body.Append(string.Create(CultureInfo.InvariantCulture,
                    $"<div style=\"position:absolute;left:{x}px;top:{row * RowHeight}px;width:{size}px;height:{size}px\">"));
                body.Append(IconMarkup(Variants[row].Kind, size));
                body.Append("</div>");
                x += size + Gap;
            }
        }
        return $$"""
            <!doctype html>
            <html><head><meta charset="utf-8"><style>
            html,body{margin:0;background:transparent}
            .sheet{position:relative;width:{{width}}px;height:{{height}}px}
            </style></head><body><div class="sheet">{{body}}</div></body></html>
            """;
    }

    private static string IconMarkup(string kind, int size)
    {
        var detail = Logo.DetailFor(size);
        if (kind == "app")
        {
            // Light tile so the icon stays visible on dark Explorer/taskbar backgrounds.
            double radius = size * 0.22;
            double inset = size * (detail == Logo.Detail.Full ? 0.1 : 0.06);
            double border = Math.Max(1, size / 64.0);
            return string.Create(CultureInfo.InvariantCulture,
                $"<div style=\"width:{size}px;height:{size}px;border-radius:{radius}px;background:linear-gradient(180deg,#FFFFFF 0%,#E8EFFC 100%);box-shadow:inset 0 0 0 {border}px #C9D6EC;display:flex;align-items:center;justify-content:center\">{Logo.Svg(size - inset * 2, detail)}</div>");
        }
        bool dark = kind == "trayDark";
        return Logo.Svg(size, detail, dark ? "#FFFFFF" : Logo.Navy, dark ? Logo.LightBlue : Logo.Blue);
    }
}

/// <summary>
/// Minimal .ico writer: frames up to 64 px as 32-bit BMP (what the shell and
/// LoadImage handle everywhere), larger ones as PNG (the standard for 256 px).
/// </summary>
public static class IcoWriter
{
    public static void Write(IReadOnlyList<Bitmap> frames, string path)
    {
        var blobs = frames.Select(f => f.Width >= 256 ? Png(f) : Bmp(f)).ToList();
        using var stream = File.Create(path);
        using var w = new BinaryWriter(stream);
        w.Write((ushort)0);
        w.Write((ushort)1);
        w.Write((ushort)frames.Count);
        int offset = 6 + 16 * frames.Count;
        for (int i = 0; i < frames.Count; i++)
        {
            byte dim = frames[i].Width >= 256 ? (byte)0 : (byte)frames[i].Width;
            w.Write(dim);
            w.Write(dim);
            w.Write((byte)0);
            w.Write((byte)0);
            w.Write((ushort)1);
            w.Write((ushort)32);
            w.Write(blobs[i].Length);
            w.Write(offset);
            offset += blobs[i].Length;
        }
        foreach (var blob in blobs) w.Write(blob);
    }

    private static byte[] Png(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    private static byte[] Bmp(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        // BITMAPINFOHEADER; height is doubled to cover the AND mask.
        bw.Write(40); bw.Write(w); bw.Write(h * 2); bw.Write((ushort)1); bw.Write((ushort)32);
        bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
        for (int y = h - 1; y >= 0; y--)
        {
            for (int x = 0; x < w; x++)
            {
                var c = bmp.GetPixel(x, y);
                bw.Write(c.B); bw.Write(c.G); bw.Write(c.R); bw.Write(c.A);
            }
        }
        // AND mask: all zero (alpha carries transparency), rows padded to 32 bits.
        int andRow = (w + 31) / 32 * 4;
        bw.Write(new byte[andRow * h]);
        return ms.ToArray();
    }
}
