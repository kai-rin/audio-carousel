using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using AudioCarousel.Audio;
using AudioCarousel.Config;
using AudioCarousel.Hotkey;
using AudioCarousel.I18n;

namespace AudioCarousel.UI;

public sealed class SettingsForm : Form
{
    private readonly IAudioDeviceService _audio;
    private readonly ConfigSchema _workingCopy;
    private readonly bool _isFirstRun;

    private readonly HotkeyTextBox _hotkeyBox;
    private readonly Button _hotkeyClearBtn;
    private readonly ListView _devicesList;
    private readonly Button _addBtn;
    private readonly Button _removeBtn;
    private readonly Button _upBtn;
    private readonly Button _downBtn;
    private readonly ComboBox _languageCombo;
    private readonly CheckBox _startupCheck;
    private readonly Button _okBtn;
    private readonly Button _cancelBtn;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ConfigSchema? Result { get; private set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<HotkeySpec, bool>? HotkeyRegistrationProbe { get; set; }

    public SettingsForm(ConfigSchema current, IAudioDeviceService audio, bool isFirstRun)
    {
        _audio = audio;
        _workingCopy = Clone(current);
        _isFirstRun = isFirstRun;

        Text = Strings.Get(isFirstRun ? "settings.titleFirstRun" : "settings.title");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 11f);
        ClientSize = new Size(680, 560);

        var hotkeyLabel = new Label { Text = Strings.Get("settings.hotkey"), Left = 20, Top = 22, AutoSize = true };
        _hotkeyBox = new HotkeyTextBox { Left = 130, Top = 18, Width = 390 };
        _hotkeyClearBtn = new Button { Text = Strings.Get("settings.hotkeyClear"), Left = 530, Top = 17, Width = 130, Height = 30 };
        var hotkeyHint = new Label { Text = Strings.Get("settings.hotkeyHint"), Left = 130, Top = 50, AutoSize = true, ForeColor = Color.Gray };

        var devicesLabel = new Label { Text = Strings.Get("settings.cycleDevices"), Left = 20, Top = 95, AutoSize = true };
        _devicesList = new ListView
        {
            Left = 20, Top = 122, Width = 640, Height = 244,
            View = View.Details, FullRowSelect = true, HideSelection = false,
            HeaderStyle = ColumnHeaderStyle.None,
            OwnerDraw = true,
        };
        _devicesList.Columns.Add("Device", 640 - 4);
        _devicesList.DrawColumnHeader += (_, e) => e.DrawDefault = true;
        _devicesList.DrawSubItem += DrawDeviceItem;
        _devicesList.DrawItem += (_, e) => { /* handled per-subitem */ };

        _addBtn    = new Button { Text = Strings.Get("settings.addDevice") + " ▾", Left = 20,  Top = 376, Width = 170, Height = 32 };
        _removeBtn = new Button { Text = Strings.Get("settings.remove"),           Left = 196, Top = 376, Width = 110, Height = 32 };
        _upBtn     = new Button { Text = Strings.Get("settings.moveUp"),           Left = 312, Top = 376, Width = 70,  Height = 32 };
        _downBtn   = new Button { Text = Strings.Get("settings.moveDown"),         Left = 388, Top = 376, Width = 70,  Height = 32 };

        var langLabel = new Label { Text = Strings.Get("settings.language"), Left = 20, Top = 437, AutoSize = true };
        _languageCombo = new ComboBox
        {
            Left = 130, Top = 432, Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _languageCombo.Items.AddRange(new object[]
        {
            new LangItem("auto", Strings.Get("settings.languageAuto")),
            new LangItem("en",   Strings.Get("settings.languageEn")),
            new LangItem("ja",   Strings.Get("settings.languageJa")),
        });

        _startupCheck = new CheckBox
        {
            Text = Strings.Get("settings.startWithWindows"),
            Left = 350, Top = 436, AutoSize = true,
        };

        _okBtn     = new Button { Text = Strings.Get("settings.ok"),     Left = 430, Top = 500, Width = 110, Height = 36, DialogResult = DialogResult.None };
        _cancelBtn = new Button { Text = Strings.Get("settings.cancel"), Left = 550, Top = 500, Width = 110, Height = 36, DialogResult = DialogResult.Cancel };
        AcceptButton = _okBtn;
        CancelButton = _cancelBtn;

        Controls.AddRange(new Control[]
        {
            hotkeyLabel, _hotkeyBox, _hotkeyClearBtn, hotkeyHint,
            devicesLabel, _devicesList,
            _addBtn, _removeBtn, _upBtn, _downBtn,
            langLabel, _languageCombo, _startupCheck,
            _okBtn, _cancelBtn,
        });

        // Wire events.
        _hotkeyClearBtn.Click += (_, _) => { _hotkeyBox.Value = null; };
        _addBtn.Click += OnAddClicked;
        _removeBtn.Click += OnRemoveClicked;
        _upBtn.Click += (_, _) => Move(-1);
        _downBtn.Click += (_, _) => Move(+1);
        _okBtn.Click += OnOkClicked;

        // Load working copy into UI.
        _hotkeyBox.Value = HotkeyParser.FromConfigEntry(_workingCopy.Hotkey);
        _startupCheck.Checked = _workingCopy.StartWithWindows;
        SelectLanguageItem(_workingCopy.Language);
        RefreshDevicesList();
    }

    private void RefreshDevicesList()
    {
        _devicesList.Items.Clear();
        var available = _audio.EnumerateActiveOutputs()
            .ToDictionary(d => d.EndpointId, d => d.DisplayName, StringComparer.Ordinal);
        string? currentDefault = _audio.GetDefaultOutputId(AudioRole.Multimedia);

        foreach (var d in _workingCopy.Devices)
        {
            bool isOnline = available.ContainsKey(d.EndpointId);
            bool isCurrent = isOnline && d.EndpointId == currentDefault;
            string display = isOnline ? d.DisplayName : $"{d.DisplayName} {Strings.Get("settings.offline")}";
            var item = new ListViewItem(display)
            {
                Tag = new DeviceRow(d.EndpointId, isOnline, isCurrent),
                Font = isCurrent ? new Font(Font, FontStyle.Bold) : Font,
            };
            _devicesList.Items.Add(item);
        }
    }

    private void DrawDeviceItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Item!.Selected)
            e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);

        var row = (DeviceRow)e.Item.Tag!;
        string marker = row.IsCurrent ? "★ " : "  ";
        string statusDot = row.IsOnline ? "●" : "○";

        var brush = e.Item.Selected ? SystemBrushes.HighlightText : SystemBrushes.WindowText;
        var statusBrush = row.IsOnline ? Brushes.SeaGreen : Brushes.Gray;
        if (e.Item.Selected) statusBrush = SystemBrushes.HighlightText;

        var rect = e.Bounds;
        e.Graphics.DrawString(marker, e.Item.Font, brush, rect.Left + 6, rect.Top + 2);
        e.Graphics.DrawString(statusDot, e.Item.Font, statusBrush, rect.Left + 30, rect.Top + 2);
        e.Graphics.DrawString(e.Item.Text, e.Item.Font, brush, rect.Left + 54, rect.Top + 2);
    }

    private void OnAddClicked(object? sender, EventArgs e)
    {
        var registered = new HashSet<string>(_workingCopy.Devices.Select(d => d.EndpointId), StringComparer.Ordinal);
        var candidates = _audio.EnumerateActiveOutputs()
            .Where(d => !registered.Contains(d.EndpointId))
            .ToList();

        var menu = new ContextMenuStrip();
        if (candidates.Count == 0)
        {
            menu.Items.Add(new ToolStripMenuItem(Strings.Get("settings.noNewDevices")) { Enabled = false });
        }
        else
        {
            foreach (var d in candidates)
            {
                var menuItem = new ToolStripMenuItem(d.DisplayName);
                menuItem.Click += (_, _) =>
                {
                    _workingCopy.Devices.Add(new DeviceEntry
                    {
                        EndpointId = d.EndpointId,
                        DisplayName = d.DisplayName,
                        AddedAt = DateTimeOffset.Now,
                    });
                    RefreshDevicesList();
                };
                menu.Items.Add(menuItem);
            }
        }
        menu.Show(_addBtn, new Point(0, _addBtn.Height));
    }

    private void OnRemoveClicked(object? sender, EventArgs e)
    {
        if (_devicesList.SelectedIndices.Count == 0) return;
        int idx = _devicesList.SelectedIndices[0];
        _workingCopy.Devices.RemoveAt(idx);
        if (_workingCopy.CurrentIndex >= _workingCopy.Devices.Count)
            _workingCopy.CurrentIndex = 0;
        RefreshDevicesList();
    }

    private new void Move(int delta)
    {
        if (_devicesList.SelectedIndices.Count == 0) return;
        int idx = _devicesList.SelectedIndices[0];
        int target = idx + delta;
        if (target < 0 || target >= _workingCopy.Devices.Count) return;
        (_workingCopy.Devices[idx], _workingCopy.Devices[target]) =
            (_workingCopy.Devices[target], _workingCopy.Devices[idx]);
        RefreshDevicesList();
        _devicesList.Items[target].Selected = true;
        _devicesList.Items[target].Focused = true;
    }

    private void OnOkClicked(object? sender, EventArgs e)
    {
        // Validate hotkey re-registration if set.
        if (_hotkeyBox.Value is HotkeySpec spec && HotkeyRegistrationProbe is not null)
        {
            if (!HotkeyRegistrationProbe(spec))
            {
                MessageBox.Show(this, Strings.Get("error.hotkeyInUse"),
                    Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        _workingCopy.Hotkey = _hotkeyBox.Value is HotkeySpec s ? HotkeyParser.ToConfigEntry(s) : null;
        _workingCopy.StartWithWindows = _startupCheck.Checked;
        _workingCopy.Language = ((LangItem)_languageCombo.SelectedItem!).Code;

        Result = _workingCopy;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void SelectLanguageItem(string code)
    {
        for (int i = 0; i < _languageCombo.Items.Count; i++)
        {
            if (((LangItem)_languageCombo.Items[i]!).Code == code)
            {
                _languageCombo.SelectedIndex = i;
                return;
            }
        }
        _languageCombo.SelectedIndex = 0;
    }

    private static ConfigSchema Clone(ConfigSchema src) => new()
    {
        Version = src.Version,
        Language = src.Language,
        Hotkey = src.Hotkey is null ? null : new HotkeyEntry
        {
            Modifiers = new List<string>(src.Hotkey.Modifiers),
            Key = src.Hotkey.Key,
        },
        Devices = src.Devices.Select(d => new DeviceEntry
        {
            EndpointId = d.EndpointId,
            DisplayName = d.DisplayName,
            AddedAt = d.AddedAt,
        }).ToList(),
        CurrentIndex = src.CurrentIndex,
        StartWithWindows = src.StartWithWindows,
    };

    private sealed record LangItem(string Code, string Display)
    {
        public override string ToString() => Display;
    }

    private sealed record DeviceRow(string EndpointId, bool IsOnline, bool IsCurrent);
}
