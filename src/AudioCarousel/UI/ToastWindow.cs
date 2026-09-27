using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.Automation;
using AudioCarousel.I18n;

namespace AudioCarousel.UI;

/// <summary>
/// The switch notification: navy card with a colored accent bar, icon, app
/// caption, bold title and a subtitle, plus a close glyph. Same design as
/// promo/src/components/Toast.tsx (the README hero image renders that).
/// Never takes focus; a click anywhere dismisses it, hovering pauses it.
/// </summary>
public sealed class ToastWindow : Form
{
    private const int FadeMs = 200;
    private const int HoldMs = 2200;
    private const int TickMs = 16;

    // Layout in 96-DPI units; scaled to the target monitor's DPI on each show.
    private const int EdgeMargin = 16;
    private const int AccentWidth = 5;
    private const int PadX = 18;
    private const int PadY = 14;
    private const int IconBox = 36;
    private const int IconGap = 14;
    private const int CloseBox = 28;
    private const int MinTextWidth = 200;
    private const int MaxTextWidth = 380;
    private const int CornerRadius = 8;

    private static readonly Color Accent = Color.FromArgb(0x25, 0x63, 0xEB);    // blue
    private static readonly Color ErrorAccent = Color.FromArgb(0xEF, 0x44, 0x44);

    // Navy card in dark mode (the brand look), white card in light mode.
    private sealed record Theme(Color Back, Color Title, Color Sub, Color Icon, Color ErrorIcon, Color? Border);
    private static readonly Theme DarkTheme = new(
        Color.FromArgb(0x10, 0x1C, 0x36), Color.White, Color.FromArgb(0xAF, 0xBD, 0xD6),
        Color.White, Color.FromArgb(0xFC, 0xA5, 0xA5), Border: null);
    private static readonly Theme LightTheme = new(
        Color.White, Color.FromArgb(0x0F, 0x17, 0x2A), Color.FromArgb(0x47, 0x56, 0x6E),
        Color.FromArgb(0x10, 0x1C, 0x36), Color.FromArgb(0xDC, 0x26, 0x26), Border: Color.FromArgb(0xD8, 0xE0, 0xEC));
    private Theme _theme = DarkTheme;

    private readonly System.Windows.Forms.Timer _holdTimer;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private ToastContent _content = new("", null, MenuGlyphs.Speaker, false);
    private FadeState _state = FadeState.Hidden;
    private float _scale = 1f;
    private Font? _captionFont;
    private Font? _titleFont;
    private Font? _subFont;
    private Bitmap? _icon;
    private Bitmap? _close;
    private Rectangle _iconRect, _captionRect, _titleRect, _subRect, _closeRect;

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
        BackColor = DarkTheme.Back;
        ForeColor = DarkTheme.Title;
        AccessibleRole = AccessibleRole.Alert;
        Cursor = Cursors.Hand;

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

    public void ShowContent(ToastContent content)
    {
        _content = content;
        _theme = Application.IsDarkModeEnabled ? DarkTheme : LightTheme;
        BackColor = _theme.Back;
        AccessibleName = content.Subtitle is null ? content.Title : $"{content.Title}. {content.Subtitle}";
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
            // Already on screen — replace content and reset hold.
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
            AccessibleName);
    }

    private int S(int logical) => (int)Math.Round(logical * _scale);

    private void LayoutOnActiveMonitor()
    {
        var cursor = Cursor.Position;
        var work = Screen.FromPoint(cursor).WorkingArea;
        _scale = GetDpiForPoint(cursor) / 96f;

        ReplaceFonts();
        ReplaceBitmaps();

        const TextFormatFlags oneLine = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
        const TextFormatFlags wrap = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
        string caption = Strings.Get("app.title");
        int maxText = S(MaxTextWidth);

        var captionSize = TextRenderer.MeasureText(caption, _captionFont, new Size(maxText, int.MaxValue), oneLine);
        // Titles (device names, error messages) may wrap to a second line.
        var titleSize = TextRenderer.MeasureText(_content.Title, _titleFont, new Size(maxText, int.MaxValue), wrap);
        var subSize = _content.Subtitle is null
            ? Size.Empty
            : TextRenderer.MeasureText(_content.Subtitle, _subFont, new Size(maxText, int.MaxValue), oneLine);
        int textWidth = Math.Clamp(Math.Max(captionSize.Width, Math.Max(titleSize.Width, subSize.Width)), S(MinTextWidth), maxText);
        int titleHeight = Math.Min(titleSize.Height, _titleFont!.Height * 2);

        int left = S(AccentWidth) + S(PadX);
        int textLeft = left + S(IconBox) + S(IconGap);
        int y = S(PadY);
        _captionRect = new Rectangle(textLeft, y, textWidth, captionSize.Height);
        y += captionSize.Height + S(2);
        _titleRect = new Rectangle(textLeft, y, textWidth, titleHeight);
        y += titleHeight;
        if (_content.Subtitle is not null)
        {
            y += S(2);
            _subRect = new Rectangle(textLeft, y, textWidth, subSize.Height);
            y += subSize.Height;
        }
        else
        {
            _subRect = Rectangle.Empty;
        }
        int height = y + S(PadY);
        int width = textLeft + textWidth + S(CloseBox) + S(6);

        _iconRect = new Rectangle(left, (height - S(IconBox)) / 2, S(IconBox), S(IconBox));
        _closeRect = new Rectangle(width - S(CloseBox) - S(4), S(4), S(CloseBox), S(CloseBox));

        int margin = S(EdgeMargin);
        SetBounds(work.Right - width - margin, work.Bottom - height - margin, width, height);

        // Clip the window itself to the rounded shape; painting a rounded
        // rectangle on a same-colored rectangular window shows no corners.
        using var path = RoundedRect(new Rectangle(0, 0, width, height), S(CornerRadius));
        var oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
        Invalidate();
    }

    private void ReplaceFonts()
    {
        _captionFont?.Dispose();
        _titleFont?.Dispose();
        _subFont?.Dispose();
        _captionFont = new Font("Segoe UI", 12f * _scale, FontStyle.Regular, GraphicsUnit.Pixel);
        _titleFont = new Font("Segoe UI Semibold", 17f * _scale, FontStyle.Regular, GraphicsUnit.Pixel);
        _subFont = new Font("Segoe UI", 13f * _scale, FontStyle.Regular, GraphicsUnit.Pixel);
    }

    private void ReplaceBitmaps()
    {
        _icon?.Dispose();
        _close?.Dispose();
        _icon = MenuGlyphs.Render(_content.Glyph, S(IconBox), _content.IsError ? _theme.ErrorIcon : _theme.Icon);
        _close = MenuGlyphs.Render(MenuGlyphs.Close, S(12), _theme.Sub);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        using (var accent = new SolidBrush(_content.IsError ? ErrorAccent : Accent))
            g.FillRectangle(accent, 0, 0, S(AccentWidth), Height);

        if (_icon is not null) g.DrawImage(_icon, _iconRect);
        if (_close is not null)
        {
            g.DrawImage(_close,
                _closeRect.X + (_closeRect.Width - _close.Width) / 2,
                _closeRect.Y + (_closeRect.Height - _close.Height) / 2);
        }

        if (_theme.Border is Color border)
        {
            // Rounded window region clips the corners; a hairline keeps the
            // white card from dissolving into light app backgrounds.
            using var pen = new Pen(border);
            using var outline = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), S(CornerRadius));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawPath(pen, outline);
            g.SmoothingMode = SmoothingMode.None;
        }

        TextRenderer.DrawText(g, Strings.Get("app.title"), _captionFont, _captionRect, _theme.Sub,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, _content.Title, _titleFont, _titleRect, _theme.Title,
            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        if (_content.Subtitle is not null)
        {
            TextRenderer.DrawText(g, _content.Subtitle, _subFont, _subRect, _theme.Sub,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }
    }

    // Click anywhere (the × included) to dismiss.
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_state is FadeState.FadingIn or FadeState.Holding)
        {
            _holdTimer.Stop();
            StartFadeOut();
        }
    }

    // Hovering keeps it up so it can be read (or dismissed) at leisure.
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (_state == FadeState.Holding) _holdTimer.Stop();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_state == FadeState.Holding) _holdTimer.Start();
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
                    if (!ClientRectangle.Contains(PointToClient(Cursor.Position))) _holdTimer.Start();
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
            _captionFont?.Dispose();
            _titleFont?.Dispose();
            _subFont?.Dispose();
            _icon?.Dispose();
            _close?.Dispose();
            Region?.Dispose();
        }
        base.Dispose(disposing);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(Point pt, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
}
