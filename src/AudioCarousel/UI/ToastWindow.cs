using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AudioCarousel.UI;

public sealed class ToastWindow : Form
{
    private const int FadeMs = 200;
    private const int HoldMs = 1500;
    private const int EdgeMargin = 16;
    private const int PadX = 24;
    private const int PadY = 14;
    private const int MaxWidth = 600;

    private readonly System.Windows.Forms.Timer _holdTimer;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private string _text = "";
    private FadeState _state = FadeState.Hidden;
    private bool _isError;

    private enum FadeState { Hidden, FadingIn, Holding, FadingOut }

    public ToastWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        Opacity = 0;
        BackColor = Color.FromArgb(28, 28, 30);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Point);

        _holdTimer = new System.Windows.Forms.Timer { Interval = HoldMs };
        _holdTimer.Tick += (_, _) => { _holdTimer.Stop(); StartFadeOut(); };

        _fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _fadeTimer.Tick += FadeTick;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x08000000 /* WS_EX_NOACTIVATE */ | 0x00000080 /* WS_EX_TOOLWINDOW */;
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    public void ShowMessage(string text, bool isError = false)
    {
        _text = text;
        _isError = isError;
        BackColor = isError ? Color.FromArgb(120, 28, 28) : Color.FromArgb(28, 28, 30);
        AdjustSize();
        PositionOnActiveMonitor();

        if (_state == FadeState.Hidden)
        {
            Opacity = 0;
            Show();
            _state = FadeState.FadingIn;
            _fadeTimer.Start();
        }
        else
        {
            // Already on screen — replace text and reset hold.
            _holdTimer.Stop();
            _state = FadeState.Holding;
            Opacity = 1;
            _holdTimer.Start();
            Invalidate();
        }
    }

    private void AdjustSize()
    {
        using var g = CreateGraphics();
        var size = g.MeasureString(_text, Font);
        int width = Math.Min(MaxWidth, (int)Math.Ceiling(size.Width) + PadX * 2);
        int height = (int)Math.Ceiling(size.Height) + PadY * 2;
        Size = new Size(width, height);
    }

    private void PositionOnActiveMonitor()
    {
        var screen = Screen.FromPoint(Cursor.Position);
        var work = screen.WorkingArea;
        Location = new Point(
            work.Right - Width - EdgeMargin,
            work.Bottom - Height - EdgeMargin);
    }

    private void StartFadeOut()
    {
        _state = FadeState.FadingOut;
        _fadeTimer.Start();
    }

    private void FadeTick(object? sender, EventArgs e)
    {
        double step = 16.0 / FadeMs;
        switch (_state)
        {
            case FadeState.FadingIn:
                Opacity = Math.Min(1, Opacity + step);
                if (Opacity >= 1)
                {
                    _fadeTimer.Stop();
                    _state = FadeState.Holding;
                    _holdTimer.Start();
                }
                break;
            case FadeState.FadingOut:
                Opacity = Math.Max(0, Opacity - step);
                if (Opacity <= 0)
                {
                    _fadeTimer.Stop();
                    _state = FadeState.Hidden;
                    Hide();
                }
                break;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 8);
        using var bg = new SolidBrush(BackColor);
        g.FillPath(bg, path);

        var rect = new Rectangle(PadX, PadY, Width - PadX * 2, Height - PadY * 2);
        TextRenderer.DrawText(g, _text, Font, rect, ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _holdTimer.Dispose();
            _fadeTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
