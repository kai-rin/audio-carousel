using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using AudioCarousel.I18n;

namespace AudioCarousel.UI;

public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _titleItem;
    private readonly ToolStripMenuItem _currentItem;
    private readonly ToolStripMenuItem _cycleItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _aboutItem;
    private readonly ToolStripMenuItem _exitItem;

    public event Action? CycleRequested;
    public event Action? SettingsRequested;
    public event Action<bool>? StartupToggled;
    public event Action? AboutRequested;
    public event Action? ExitRequested;

    public TrayIcon()
    {
        _menu = new ContextMenuStrip();
        _titleItem    = new ToolStripMenuItem { Enabled = false };
        _currentItem  = new ToolStripMenuItem { Enabled = false };
        _cycleItem    = new ToolStripMenuItem();
        _settingsItem = new ToolStripMenuItem();
        _startupItem  = new ToolStripMenuItem { CheckOnClick = true };
        _aboutItem    = new ToolStripMenuItem();
        _exitItem     = new ToolStripMenuItem();

        _cycleItem.Click    += (_, _) => CycleRequested?.Invoke();
        _settingsItem.Click += (_, _) => SettingsRequested?.Invoke();
        _startupItem.Click  += (_, _) => StartupToggled?.Invoke(_startupItem.Checked);
        _aboutItem.Click    += (_, _) => AboutRequested?.Invoke();
        _exitItem.Click     += (_, _) => ExitRequested?.Invoke();

        _menu.Items.AddRange(new ToolStripItem[]
        {
            _titleItem,
            _currentItem,
            new ToolStripSeparator(),
            _cycleItem,
            new ToolStripSeparator(),
            _settingsItem,
            _startupItem,
            new ToolStripSeparator(),
            _aboutItem,
            _exitItem,
        });

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadEmbeddedIcon() ?? SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = _menu,
        };
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) CycleRequested?.Invoke();
        };

        ApplyLabels();
    }

    public void SetCurrentDeviceLabel(string? deviceName)
    {
        _currentItem.Text = string.IsNullOrEmpty(deviceName)
            ? Strings.Get("tray.currentPrefix") + Strings.Get("tray.currentNone")
            : Strings.Get("tray.currentPrefix") + deviceName;
        _notifyIcon.Text = deviceName is null
            ? Strings.Get("app.title")
            : $"{Strings.Get("app.title")} — {Truncate(deviceName, 50)}";
    }

    public void SetStartupChecked(bool isChecked) => _startupItem.Checked = isChecked;

    public void ApplyLabels()
    {
        _titleItem.Text    = Strings.Get("tray.title");
        _cycleItem.Text    = Strings.Get("tray.cycleNext");
        _settingsItem.Text = Strings.Get("tray.settings");
        _startupItem.Text  = Strings.Get("tray.startWithWindows");
        _aboutItem.Text    = Strings.Get("tray.about");
        _exitItem.Text     = Strings.Get("tray.exit");
    }

    private static Icon? LoadEmbeddedIcon()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("AudioCarousel.Resources.tray.ico");
        return stream is null ? null : new Icon(stream);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max - 1) + "…";

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }
}
