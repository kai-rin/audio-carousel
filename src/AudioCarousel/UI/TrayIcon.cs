using System.Drawing;
using System.Windows.Forms;
using AudioCarousel.I18n;
using Microsoft.Win32;

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
    private readonly ToolStripMenuItem _cyclePrevItem;
    // "Microphone" submenu; only shown once microphones are registered.
    private readonly ToolStripMenuItem _micMenu;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _aboutItem;
    private readonly ToolStripMenuItem _exitItem;
    private readonly Font _titleFont;
    private readonly List<Image> _images = new();
    private Icon? _icon;
    private string? _inputHotkeyLabel;
    private FluentMenuRenderer _renderer = new(dark: true);

    public event Action? CycleRequested;
    public event Action? CyclePreviousRequested;
    public event Action? CycleInputRequested;
    public event Action<string>? InputDeviceSelected;
    public event Action? LeftClicked;
    public event Action? SettingsRequested;
    public event Action<bool>? StartupToggled;
    public event Action? AboutRequested;
    public event Action? ExitRequested;
    public event Action<string>? DeviceSelected;
    public event Action? MenuOpening;

    public TrayIcon()
    {
        _menu = new ContextMenuStrip();
        _titleFont = new Font(_menu.Font, FontStyle.Bold);
        // A menu item (not a label) so the logo lines up with the other icons;
        // the renderer draws it without hover and it does nothing on click.
        _titleItem = new ToolStripMenuItem
        {
            Font = _titleFont,
            Tag = FluentMenuRenderer.HeaderTag,
            AccessibleRole = AccessibleRole.StaticText,
        };
        _currentItem = new ToolStripMenuItem { Enabled = false };
        _cycleItem = new ToolStripMenuItem { ShowShortcutKeys = true };
        _cyclePrevItem = new ToolStripMenuItem { ShowShortcutKeys = true, Visible = false };
        _micMenu = new ToolStripMenuItem { Visible = false };
        _settingsItem = new ToolStripMenuItem();
        _startupItem = new ToolStripMenuItem { CheckOnClick = true };
        _aboutItem = new ToolStripMenuItem();
        _exitItem = new ToolStripMenuItem();

        _cycleItem.Click += (_, _) => CycleRequested?.Invoke();
        _cyclePrevItem.Click += (_, _) => CyclePreviousRequested?.Invoke();
        _settingsItem.Click += (_, _) => SettingsRequested?.Invoke();
        _startupItem.Click += (_, _) => StartupToggled?.Invoke(_startupItem.Checked);
        _aboutItem.Click += (_, _) => AboutRequested?.Invoke();
        _exitItem.Click += (_, _) => ExitRequested?.Invoke();

        _menu.Items.AddRange(new ToolStripItem[]
        {
            _titleItem,
            new ToolStripSeparator(),
            _currentItem,
            new ToolStripSeparator(),
            _cycleItem,
            _cyclePrevItem,
            _micMenu,
            new ToolStripSeparator(),
            _settingsItem,
            _startupItem,
            _aboutItem,
            _exitItem,
        });

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = _menu,
        };
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) LeftClicked?.Invoke();
        };
        _menu.Opening += (_, _) =>
        {
            MenuOpening?.Invoke();
            FluentMenuRenderer.ApplySpacing(_menu.Items);
            FluentMenuRenderer.ApplySpacing(_micMenu.DropDownItems);
        };
        // Windows 11 rounded popup corners (the submenu gets them too).
        _menu.Opened += (_, _) => FluentMenuRenderer.RoundCorners(_menu.Handle);
        _micMenu.DropDownOpened += (_, _) => FluentMenuRenderer.RoundCorners(_micMenu.DropDown.Handle);

        ApplyTheme();
        // Follow taskbar light/dark switches while running.
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        ApplyLabels();
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
            ApplyTheme();
    }

    // Tray glyph for the current taskbar theme, plus menu glyphs in the menu's
    // text color (dark mode remaps SystemColors).
    private void ApplyTheme()
    {
        var oldIcon = _icon;
        _icon = AppIcons.Tray(TrayTheme.TaskbarIsLight(), SystemInformation.SmallIconSize);
        _notifyIcon.Icon = _icon;
        oldIcon?.Dispose();

        _renderer = new FluentMenuRenderer(Application.IsDarkModeEnabled);
        _menu.Renderer = _renderer;

        foreach (var image in _images) image.Dispose();
        _images.Clear();
        _titleItem.Image = Track(AppIcons.App(_menu.ImageScalingSize).ToBitmap());
        _cycleItem.Image = Glyph(MenuGlyphs.Next);
        _cyclePrevItem.Image = Glyph(MenuGlyphs.Previous);
        _micMenu.Image = Glyph(MenuGlyphs.Microphone);
        _settingsItem.Image = Glyph(MenuGlyphs.Settings);
        _aboutItem.Image = Glyph(MenuGlyphs.Info);
        _exitItem.Image = Glyph(MenuGlyphs.Exit);
        foreach (var item in _deviceItems) item.Image = Glyph(MenuGlyphs.Speaker, onAccent: item.Checked);
        SetStartupChecked(_startupItem.Checked);
    }

    // Checked items sit on the brand-blue box, so their glyph is white.
    private Image? Glyph(char glyph, bool onAccent = false) =>
        MenuGlyphs.Render(glyph, _menu.ImageScalingSize.Height, onAccent ? Color.White : _renderer.Text) is Bitmap bmp
            ? Track(bmp)
            : null;

    private Image Track(Image image)
    {
        _images.Add(image);
        return image;
    }

    /// <summary>
    /// Rebuilds the per-device menu entries (right after the current-device
    /// label). Call from the MenuOpening event so the list is fresh.
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
            var item = DeviceItem(row, id => DeviceSelected?.Invoke(id));
            item.Image = Glyph(MenuGlyphs.Speaker, onAccent: row.IsCurrent);
            _menu.Items.Insert(insertAt++, item);
            _deviceItems.Add(item);
        }
    }

    /// <summary>Rebuilds the Microphone submenu; hidden when the list is empty.</summary>
    public void SetInputDevices(IReadOnlyList<TrayDeviceRow> rows)
    {
        foreach (ToolStripItem old in _micMenu.DropDownItems.Cast<ToolStripItem>().ToList())
            old.Dispose();
        _micMenu.DropDownItems.Clear();
        _micMenu.Visible = rows.Count > 0;
        if (rows.Count == 0) return;

        foreach (var row in rows)
        {
            var item = DeviceItem(row, id => InputDeviceSelected?.Invoke(id));
            item.Image = Glyph(MenuGlyphs.Microphone, onAccent: row.IsCurrent);
            _micMenu.DropDownItems.Add(item);
        }
        _micMenu.DropDownItems.Add(new ToolStripSeparator());
        var next = new ToolStripMenuItem(Strings.Get("tray.cycleInput"))
        {
            Image = Glyph(MenuGlyphs.Next),
            ShowShortcutKeys = true,
            ShortcutKeyDisplayString = _inputHotkeyLabel,
        };
        next.Click += (_, _) => CycleInputRequested?.Invoke();
        _micMenu.DropDownItems.Add(next);
    }

    private static ToolStripMenuItem DeviceItem(TrayDeviceRow row, Action<string> onSelect)
    {
        string text = row.IsOnline
            ? row.DisplayName
            : $"{row.DisplayName} {Strings.Get("common.offline")}";
        var item = new ToolStripMenuItem(text) { Checked = row.IsCurrent, Enabled = row.IsOnline };
        // The check only highlights the icon box; bold makes "current" obvious.
        if (row.IsCurrent) item.Font = new Font(item.Font, FontStyle.Bold);
        string endpointId = row.EndpointId;
        item.Click += (_, _) => onSelect(endpointId);
        return item;
    }

    /// <summary>
    /// Shows each hotkey next to its command, like a normal menu shortcut.
    /// "Previous device" is listed only when it has a hotkey.
    /// </summary>
    public void SetHotkeyLabels(string? next, string? previous, string? input)
    {
        _cycleItem.ShortcutKeyDisplayString = next;
        _cyclePrevItem.ShortcutKeyDisplayString = previous;
        _cyclePrevItem.Visible = previous is not null;
        _inputHotkeyLabel = input;
    }

    public void SetCurrentDeviceLabel(string? deviceName, string? inputName = null)
    {
        string current = Strings.Get("tray.currentPrefix") +
            (string.IsNullOrEmpty(deviceName) ? Strings.Get("tray.currentNone") : Truncate(deviceName, 60));
        _currentItem.Text = current;
        string text = current;
        if (inputName is not null)
            text += "\n" + Cycle.CycleController.MicrophoneLabel(Truncate(inputName, 50));
        // NotifyIcon.Text is limited to 127 characters.
        _notifyIcon.Text = Truncate(text, 127);
    }

    public void SetStartupChecked(bool isChecked)
    {
        _startupItem.Checked = isChecked;
        // Fluent check glyph on the blue box, matching the other icons.
        _startupItem.Image = isChecked ? Glyph(MenuGlyphs.Check, onAccent: true) : null;
    }

    public void ApplyLabels()
    {
        _titleItem.Text = Strings.Get("app.title");
        _cycleItem.Text = Strings.Get("tray.cycleNext");
        _cyclePrevItem.Text = Strings.Get("tray.cyclePrevious");
        _micMenu.Text = Strings.Get("tray.microphone");
        _settingsItem.Text = Strings.Get("tray.settings");
        _startupItem.Text = Strings.Get("common.startWithWindows");
        _aboutItem.Text = Strings.Get("tray.about");
        _exitItem.Text = Strings.Get("tray.exit");
    }

    public void ShowBalloon(string title, string text) =>
        _notifyIcon.ShowBalloonTip(10000, title, text, ToolTipIcon.Info);

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max - 1) + "…";

    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _titleFont.Dispose();
        _icon?.Dispose();
        foreach (var image in _images) image.Dispose();
    }
}
