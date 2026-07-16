using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using AudioCarousel.I18n;

namespace AudioCarousel.UI;

/// <summary>One registered device as shown in the tray menu.</summary>
public sealed record TrayDeviceRow(string EndpointId, string DisplayName, bool IsOnline, bool IsCurrent);

public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _titleItem;
    private readonly ToolStripMenuItem _currentItem;
    private readonly List<ToolStripMenuItem> _deviceItems = new();
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
    public event Action<string>? DeviceSelected;
    public event Action? MenuOpening;

    public TrayIcon()
    {
        _menu = new ContextMenuStrip();
        _titleItem = new ToolStripMenuItem { Enabled = false };
        _currentItem = new ToolStripMenuItem { Enabled = false };
        _cycleItem = new ToolStripMenuItem();
        _settingsItem = new ToolStripMenuItem();
        _startupItem = new ToolStripMenuItem { CheckOnClick = true };
        _aboutItem = new ToolStripMenuItem();
        _exitItem = new ToolStripMenuItem();

        _cycleItem.Click += (_, _) => CycleRequested?.Invoke();
        _settingsItem.Click += (_, _) => SettingsRequested?.Invoke();
        _startupItem.Click += (_, _) => StartupToggled?.Invoke(_startupItem.Checked);
        _aboutItem.Click += (_, _) => AboutRequested?.Invoke();
        _exitItem.Click += (_, _) => ExitRequested?.Invoke();

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
        _menu.Opening += (_, _) => MenuOpening?.Invoke();

        ApplyLabels();
    }

    /// <summary>
    /// Rebuilds the per-device menu entries (inserted right after the current-
    /// device label). Call from the MenuOpening event so the list is fresh.
    /// </summary>
    public void SetDevices(IReadOnlyList<TrayDeviceRow> rows)
    {
        foreach (var item in _deviceItems)
        {
            _menu.Items.Remove(item);
            item.Dispose();
        }
        _deviceItems.Clear();

        // With device rows visible, the checked row already conveys "current";
        // keep the label row only for the empty state.
        _currentItem.Visible = rows.Count == 0;

        int insertAt = _menu.Items.IndexOf(_currentItem) + 1;
        foreach (var row in rows)
        {
            string text = row.IsOnline
                ? row.DisplayName
                : $"{row.DisplayName} {Strings.Get("settings.offline")}";
            var item = new ToolStripMenuItem(text)
            {
                Checked = row.IsCurrent,
                Enabled = row.IsOnline,
            };
            string endpointId = row.EndpointId;
            item.Click += (_, _) => DeviceSelected?.Invoke(endpointId);
            _menu.Items.Insert(insertAt++, item);
            _deviceItems.Add(item);
        }
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
        _titleItem.Text = Strings.Get("tray.title");
        _cycleItem.Text = Strings.Get("tray.cycleNext");
        _settingsItem.Text = Strings.Get("tray.settings");
        _startupItem.Text = Strings.Get("tray.startWithWindows");
        _aboutItem.Text = Strings.Get("tray.about");
        _exitItem.Text = Strings.Get("tray.exit");
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
