using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.Automation;

namespace AudioCarousel.UI;

public sealed class ToastWindow : Form
{
    private const int FadeMs = 200;
    private const int HoldMs = 1500;
    private const int TickMs = 16;

    // Layout in 96-DPI units; scaled to the target monitor's DPI on each show.
    private const int EdgeMargin = 16;
    private const int PadX = 24;
    private const int PadY = 14;
    private const int MaxWidth = 600;
    private const int CornerRadius = 8;
    private const float FontPx = 16f; // 12pt at 96 DPI

    private static readonly Color NormalBack = Color.FromArgb(28, 28, 30);
    private static readonly Color ErrorBack = Color.FromArgb(120, 28, 28);

    private readonly System.Windows.Forms.Timer _holdTimer;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private string _text = "";
    private FadeState _state = FadeState.Hidden;
    private Font? _font;
    private int _padX = PadX;
    private int _padY = PadY;

    private enum FadeState { Hidden, FadingIn, Holding, FadingOut }

    public ToastWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        Opacity = 0;
        BackColor = NormalBack;
        ForeColor = Color.White;
        AccessibleRole = AccessibleRole.Alert;

        _holdTimer = new System.Windows.Forms.Timer { Interval = HoldMs };
        _holdTimer.Tick += (_, _) => { _holdTimer.Stop(); StartFadeOut(); };

        _fadeTimer = new System.Windows.Forms.Timer { Interval = TickMs };
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
        AccessibleName = text;
        BackColor = isError ? ErrorBack : NormalBack;
        LayoutOnActiveMonitor();

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
            _fadeTimer.Stop();
            _holdTimer.Stop();
            _state = FadeState.Holding;
            Opacity = 1;
            _holdTimer.Start();
            Invalidate();
        }

        // The toast never takes focus, so screen readers would not notice it
        // without an explicit UIA notification.
        AccessibilityObject.RaiseAutomationNotification(
            AutomationNotificationKind.Other,
            AutomationNotificationProcessing.ImportantMostRecent,
            text);
    }

    private void LayoutOnActiveMonitor()
    {
        var cursor = Cursor.Position;
        var work = Screen.FromPoint(cursor).WorkingArea;
        float scale = GetDpiForPoint(cursor) / 96f;

        var oldFont = _font;
        _font = new Font("Segoe UI", FontPx * scale, FontStyle.Regular, GraphicsUnit.Pixel);
        Font = _font;
        oldFont?.Dispose();

        _padX = (int)Math.Round(PadX * scale);
        _padY = (int)Math.Round(PadY * scale);
        int maxWidth = (int)Math.Round(MaxWidth * scale);
        int margin = (int)Math.Round(EdgeMargin * scale);

        // Measure with the same GDI text engine used for drawing so the
        // computed width never truncates the text it was measured for.
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
        var size = TextRenderer.MeasureText(_text, _font, new Size(int.MaxValue, int.MaxValue), flags);
        int width = Math.Min(maxWidth, size.Width + _padX * 2);
        int height = size.Height + _padY * 2;

        SetBounds(work.Right - width - margin, work.Bottom - height - margin, width, height);

        // Clip the window itself to the rounded shape; painting a rounded
        // rectangle on a same-colored rectangular window shows no corners.
        using var path = RoundedRect(new Rectangle(0, 0, width, height), (int)Math.Round(CornerRadius * scale));
        var oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
    }

    // Layout is recomputed for the target monitor on every show; don't let
    // WinForms resize the window to its own suggestion when it crosses DPIs.
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        e.Cancel = true;
        base.OnDpiChanged(e);
    }

    private void StartFadeOut()
    {
        _state = FadeState.FadingOut;
        _fadeTimer.Start();
    }

    private void FadeTick(object? sender, EventArgs e)
    {
        double step = (double)TickMs / FadeMs;
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
        var rect = new Rectangle(_padX, _padY, Width - _padX * 2, Height - _padY * 2);
        TextRenderer.DrawText(e.Graphics, _text, Font, rect, ForeColor,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.Left
            | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
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

    private static uint GetDpiForPoint(Point pt)
    {
        const uint MONITOR_DEFAULTTONEAREST = 2;
        const int MDT_EFFECTIVE_DPI = 0;
        IntPtr monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        return GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0 && dpiX > 0
            ? dpiX
            : 96;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _holdTimer.Dispose();
            _fadeTimer.Dispose();
            _font?.Dispose();
            Region?.Dispose();
        }
        base.Dispose(disposing);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(Point pt, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
}
