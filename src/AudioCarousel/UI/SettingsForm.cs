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
    // Logical (96-DPI) sizes; AutoScaleMode.Dpi scales them to the monitor.
    private const int ContentWidth = 560;
    private const int ListHeight = 200;

    private readonly IAudioDeviceService _audio;
    private readonly ConfigSchema _workingCopy;
    private readonly Font _baseFont;
    private readonly Font _boldFont;
    private readonly ContextMenuStrip _addMenu = new();
    private readonly ImageList _statusImages = new() { ColorDepth = ColorDepth.Depth32Bit };
    private readonly List<Label> _hints = new();

    private readonly HotkeyTextBox _hotkeyBox;
    private readonly Button _hotkeyClearBtn;
    private readonly ListView _devicesList;
    private readonly Button _addBtn;
    private readonly Button _removeBtn;
    private readonly Button _upBtn;
    private readonly Button _downBtn;
    private readonly ComboBox _languageCombo;
    private readonly CheckBox _startupCheck;
    private readonly CheckBox _commsCheck;
    private readonly Button _okBtn;
    private readonly Button _cancelBtn;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ConfigSchema? Result { get; private set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<HotkeySpec, HotkeyRegisterResult>? HotkeyRegistrationProbe { get; set; }

    public SettingsForm(ConfigSchema current, IAudioDeviceService audio, bool isFirstRun, bool startupEnabled)
    {
        _audio = audio;
        _workingCopy = current.Clone();
        _workingCopy.StartWithWindows = startupEnabled;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        _baseFont = new Font("Segoe UI", 10f);
        _boldFont = new Font(_baseFont, FontStyle.Bold);
        Font = _baseFont;

        Text = Strings.Get(isFirstRun ? "settings.titleFirstRun" : "settings.title");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var root = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16, 14, 16, 12),
        };

        // --- Hotkey --------------------------------------------------------
        var hotkeyRow = NewRow(3);
        hotkeyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        hotkeyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        hotkeyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var hotkeyLabel = new Label
        {
            Text = Strings.Get("settings.hotkey"),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 8, 0),
        };
        _hotkeyBox = new HotkeyTextBox
        {
            Dock = DockStyle.Fill,
            AccessibleName = Strings.Get("settings.hotkey").TrimEnd(':', ' ', '：'),
            Margin = new Padding(0, 3, 8, 3),
        };
        _hotkeyClearBtn = NewButton("settings.hotkeyClear");
        hotkeyRow.Controls.Add(hotkeyLabel, 0, 0);
        hotkeyRow.Controls.Add(_hotkeyBox, 1, 0);
        hotkeyRow.Controls.Add(_hotkeyClearBtn, 2, 0);
        root.Controls.Add(hotkeyRow);
        root.Controls.Add(AddHint(Strings.Get("settings.hotkeyHint"), topMargin: 2));

        // --- Devices -------------------------------------------------------
        root.Controls.Add(new Label
        {
            Text = Strings.Get("settings.cycleDevices"),
            AutoSize = true,
            Font = _boldFont,
            Margin = new Padding(0, 16, 0, 2),
        });
        root.Controls.Add(AddHint(Strings.Get("settings.devicesHint"), topMargin: 0));

        _devicesList = new ListView
        {
            Size = new Size(ContentWidth, ListHeight),
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            HeaderStyle = ColumnHeaderStyle.None,
            SmallImageList = _statusImages,
            AccessibleName = Strings.Get("settings.cycleDevices").TrimEnd(':', ' ', '：'),
            Margin = new Padding(0, 6, 0, 6),
        };
        _devicesList.Columns.Add("");
        root.Controls.Add(_devicesList);

        var deviceButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = Padding.Empty,
        };
        _addBtn = NewButton("settings.addDevice", suffix: " ▾");
        _removeBtn = NewButton("settings.remove");
        _upBtn = NewButton("settings.moveUp");
        _downBtn = NewButton("settings.moveDown");
        deviceButtons.Controls.AddRange(new Control[] { _addBtn, _removeBtn, _upBtn, _downBtn });
        root.Controls.Add(deviceButtons);

        // --- Options -------------------------------------------------------
        var langRow = NewRow(2);
        langRow.Margin = new Padding(0, 16, 0, 4);
        var langLabel = new Label
        {
            Text = Strings.Get("settings.language"),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 8, 0),
        };
        _languageCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 220,
            AccessibleName = Strings.Get("settings.language").TrimEnd(':', ' ', '：'),
        };
        _languageCombo.Items.AddRange(new object[]
        {
            new LangItem("auto",    Strings.Get("settings.languageAuto")),
            new LangItem("en",      Strings.Get("settings.languageEn")),
            new LangItem("ja",      Strings.Get("settings.languageJa")),
            new LangItem("zh-Hans", Strings.Get("settings.languageZhHans")),
            new LangItem("zh-Hant", Strings.Get("settings.languageZhHant")),
            new LangItem("es",      Strings.Get("settings.languageEs")),
            new LangItem("fr",      Strings.Get("settings.languageFr")),
            new LangItem("de",      Strings.Get("settings.languageDe")),
            new LangItem("pt-BR",   Strings.Get("settings.languagePtBr")),
            new LangItem("ru",      Strings.Get("settings.languageRu")),
            new LangItem("ko",      Strings.Get("settings.languageKo")),
        });
        langRow.Controls.Add(langLabel, 0, 0);
        langRow.Controls.Add(_languageCombo, 1, 0);
        root.Controls.Add(langRow);

        _startupCheck = new CheckBox
        {
            Text = Strings.Get("common.startWithWindows"),
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0),
        };
        root.Controls.Add(_startupCheck);
        _commsCheck = new CheckBox
        {
            Text = Strings.Get("settings.switchCommunications"),
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0),
        };
        root.Controls.Add(_commsCheck);

        // --- OK / Cancel ---------------------------------------------------
        var dialogButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 18, 0, 0),
        };
        _cancelBtn = NewButton("settings.cancel");
        _cancelBtn.DialogResult = DialogResult.Cancel;
        _okBtn = NewButton("settings.ok");
        dialogButtons.Controls.AddRange(new Control[] { _cancelBtn, _okBtn });
        root.Controls.Add(dialogButtons);

        AcceptButton = _okBtn;
        CancelButton = _cancelBtn;
        Controls.Add(root);

        // Wire events.
        _hotkeyClearBtn.Click += (_, _) => _hotkeyBox.Value = null;
        _hotkeyBox.ValueChanged += (_, _) => UpdateButtons();
        _addBtn.Click += OnAddClicked;
        _removeBtn.Click += (_, _) => RemoveSelected();
        _upBtn.Click += (_, _) => MoveSelected(-1);
        _downBtn.Click += (_, _) => MoveSelected(+1);
        _okBtn.Click += OnOkClicked;
        _devicesList.SelectedIndexChanged += (_, _) => UpdateButtons();
        _devicesList.KeyDown += OnDevicesKeyDown;
        _devicesList.Resize += (_, _) => FitColumn();

        // Load working copy into UI.
        _hotkeyBox.Value = HotkeyParser.FromConfigEntry(_workingCopy.Hotkey);
        _startupCheck.Checked = _workingCopy.StartWithWindows;
        _commsCheck.Checked = _workingCopy.SwitchCommunications;
        SelectLanguageItem(_workingCopy.Language);
        ResumeLayout(performLayout: true);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // DeviceDpi is final here, so the status dots match the list's scale.
        BuildStatusImages();
        FitHints();
        RefreshDevicesList(selectIndex: InitialSelection());
        FitColumn();
        // Start on the device list, never on the hotkey box: keyboard users
        // pressing Enter should hit OK, not start hotkey capture.
        ActiveControl = _devicesList.Items.Count > 0 ? _devicesList : _addBtn;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Opened from a tray click or a second launch: make sure it isn't
        // hidden behind the app the user was just in.
        Activate();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        BuildStatusImages();
        FitHints();
        RefreshDevicesList();
        FitColumn();
    }

    private static TableLayoutPanel NewRow(int columns) => new()
    {
        ColumnCount = columns,
        RowCount = 1,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
    };

    private Label AddHint(string text, int topMargin)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(ContentWidth, 0),
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, topMargin, 0, 0),
        };
        _hints.Add(label);
        return label;
    }

    // Wrap hints at the list's real (DPI-scaled) width. GDI (TextRenderer)
    // word-wrapping broke Russian text mid-word here, so hints lay out with
    // GDI+, which only breaks between words.
    private void FitHints()
    {
        foreach (var hint in _hints)
        {
            hint.UseCompatibleTextRendering = true;
            hint.MaximumSize = new Size(_devicesList.Width, 0);
        }
    }

    private static Button NewButton(string key, string suffix = "") => new()
    {
        Text = Strings.Get(key) + suffix,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        MinimumSize = new Size(88, 30),
        Padding = new Padding(8, 0, 8, 0),
        Margin = new Padding(0, 0, 6, 0),
        UseVisualStyleBackColor = true,
    };

    private void BuildStatusImages()
    {
        int size = LogicalToDeviceUnits(16);
        _statusImages.Images.Clear();
        _statusImages.ImageSize = new Size(size, size);
        _statusImages.Images.Add("online", DrawDot(size, Color.SeaGreen, filled: true));
        _statusImages.Images.Add("offline", DrawDot(size, SystemColors.GrayText, filled: false));
    }

    private static Bitmap DrawDot(int size, Color color, bool filled)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        float d = size * 0.5f;
        var rect = new RectangleF((size - d) / 2, (size - d) / 2, d, d);
        if (filled)
        {
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, rect);
        }
        else
        {
            using var pen = new Pen(color, Math.Max(1f, size / 12f));
            g.DrawEllipse(pen, rect);
        }
        return bmp;
    }

    private void FitColumn()
    {
        if (_devicesList.Columns.Count > 0)
            _devicesList.Columns[0].Width = _devicesList.ClientSize.Width;
    }

    private void RefreshDevicesList(int selectIndex = -1)
    {
        if (selectIndex < 0 && _devicesList.SelectedIndices.Count > 0)
            selectIndex = _devicesList.SelectedIndices[0];

        var live = _audio.EnumerateActiveOutputs();
        DeviceMatcher.HealEndpointIds(_workingCopy.Devices, live);
        var available = new HashSet<string>(live.Select(d => d.EndpointId), StringComparer.Ordinal);
        string? currentDefault = _audio.GetDefaultOutputId(AudioRole.Multimedia);

        _devicesList.BeginUpdate();
        _devicesList.Items.Clear();
        foreach (var d in _workingCopy.Devices)
        {
            bool isOnline = available.Contains(d.EndpointId);
            bool isCurrent = isOnline && d.EndpointId == currentDefault;
            // State is spelled out in the text (not just the icon/bold) so
            // screen readers announce it too.
            string display = d.DisplayName;
            if (isCurrent) display += "  " + Strings.Get("settings.current");
            else if (!isOnline) display += "  " + Strings.Get("common.offline");
            _devicesList.Items.Add(new ListViewItem(display)
            {
                ImageKey = isOnline ? "online" : "offline",
                Font = isCurrent ? _boldFont : _baseFont,
                ForeColor = isOnline ? SystemColors.WindowText : SystemColors.GrayText,
            });
        }
        _devicesList.EndUpdate();

        if (_devicesList.Items.Count > 0 && selectIndex >= 0)
        {
            int i = Math.Min(selectIndex, _devicesList.Items.Count - 1);
            _devicesList.Items[i].Selected = true;
            _devicesList.Items[i].Focused = true;
            _devicesList.EnsureVisible(i);
        }
        UpdateButtons();
    }

    // Select the device that's playing now (or the first one) so arrow keys
    // and Delete work immediately.
    private int InitialSelection()
    {
        if (_workingCopy.Devices.Count == 0) return -1;
        string? current = _audio.GetDefaultOutputId(AudioRole.Multimedia);
        int idx = _workingCopy.Devices.FindIndex(d => d.EndpointId == current);
        return idx >= 0 ? idx : 0;
    }

    private int SelectedIndex =>
        _devicesList.SelectedIndices.Count > 0 ? _devicesList.SelectedIndices[0] : -1;

    private void UpdateButtons()
    {
        int idx = SelectedIndex;
        _removeBtn.Enabled = idx >= 0;
        _upBtn.Enabled = idx > 0;
        _downBtn.Enabled = idx >= 0 && idx < _workingCopy.Devices.Count - 1;
        _hotkeyClearBtn.Enabled = _hotkeyBox.Value is not null;
    }

    private void OnDevicesKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Delete:
                RemoveSelected();
                e.Handled = true;
                break;
            case Keys.Up when e.Control || e.Alt:
                MoveSelected(-1);
                e.Handled = true;
                break;
            case Keys.Down when e.Control || e.Alt:
                MoveSelected(+1);
                e.Handled = true;
                break;
        }
    }

    private void OnAddClicked(object? sender, EventArgs e)
    {
        var live = _audio.EnumerateActiveOutputs();
        // Heal so a churned-ID device counts as registered instead of a duplicate candidate.
        DeviceMatcher.HealEndpointIds(_workingCopy.Devices, live);
        var registered = new HashSet<string>(_workingCopy.Devices.Select(d => d.EndpointId), StringComparer.Ordinal);
        var candidates = live.Where(d => !registered.Contains(d.EndpointId)).ToList();

        foreach (ToolStripItem old in _addMenu.Items.Cast<ToolStripItem>().ToList())
            old.Dispose();
        _addMenu.Items.Clear();

        if (candidates.Count == 0)
        {
            _addMenu.Items.Add(new ToolStripMenuItem(Strings.Get("settings.noNewDevices")) { Enabled = false });
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
                    RefreshDevicesList(selectIndex: _workingCopy.Devices.Count - 1);
                    _devicesList.Focus();
                };
                _addMenu.Items.Add(menuItem);
            }
        }
        _addMenu.Show(_addBtn, new Point(0, _addBtn.Height));
    }

    private void RemoveSelected()
    {
        int idx = SelectedIndex;
        if (idx < 0) return;
        _workingCopy.Devices.RemoveAt(idx);
        if (_workingCopy.CurrentIndex >= _workingCopy.Devices.Count)
            _workingCopy.CurrentIndex = 0;
        RefreshDevicesList(selectIndex: idx);
        if (_workingCopy.Devices.Count == 0) _addBtn.Focus();
    }

    private void MoveSelected(int delta)
    {
        int idx = SelectedIndex;
        if (idx < 0) return;
        int target = idx + delta;
        if (target < 0 || target >= _workingCopy.Devices.Count) return;
        (_workingCopy.Devices[idx], _workingCopy.Devices[target]) =
            (_workingCopy.Devices[target], _workingCopy.Devices[idx]);
        RefreshDevicesList(selectIndex: target);
        _devicesList.Focus();
    }

    private void OnOkClicked(object? sender, EventArgs e)
    {
        // Validate hotkey re-registration if set.
        if (_hotkeyBox.Value is HotkeySpec spec && HotkeyRegistrationProbe is not null)
        {
            var probe = HotkeyRegistrationProbe(spec);
            if (probe != HotkeyRegisterResult.Ok)
            {
                string key = probe == HotkeyRegisterResult.InUse ? "error.hotkeyInUse" : "error.hotkeyInvalid";
                MessageBox.Show(this, Strings.Get(key),
                    Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _hotkeyBox.Focus();
                return;
            }
        }

        _workingCopy.Hotkey = _hotkeyBox.Value is HotkeySpec s ? HotkeyParser.ToConfigEntry(s) : null;
        _workingCopy.StartWithWindows = _startupCheck.Checked;
        _workingCopy.SwitchCommunications = _commsCheck.Checked;
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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            // After the controls that reference them are gone.
            _addMenu.Dispose();
            _statusImages.Dispose();
            _boldFont.Dispose();
            _baseFont.Dispose();
        }
    }

    private sealed record LangItem(string Code, string Display)
    {
        public override string ToString() => Display;
    }
}
