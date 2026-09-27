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
    private readonly IAudioDeviceService _inputAudio;
    // Which list the device editor shows: playback (false) or recording (true).
    private bool _editingInput;
    private Label _devicesHint = null!;
    private readonly ConfigSchema _workingCopy;
    private readonly Font _baseFont;
    private readonly Font _boldFont;
    private readonly ContextMenuStrip _addMenu = new();
    private readonly ImageList _statusImages = new() { ColorDepth = ColorDepth.Depth32Bit };
    private readonly List<Label> _hints = new();
    private readonly TableLayoutPanel _hotkeyTable;

    private readonly bool _isFirstRun;
    private readonly HotkeyTextBox _nextBox;
    private readonly Button _nextClearBtn;
    private readonly HotkeyTextBox _prevBox;
    private readonly Button _prevClearBtn;
    private readonly HotkeyTextBox _inputBox;
    private readonly Button _inputClearBtn;
    private readonly RadioButton _playbackTab;
    private readonly RadioButton _recordingTab;
    private readonly ListView _devicesList;
    private readonly Button _addBtn;
    private readonly Button _removeBtn;
    private readonly Button _upBtn;
    private readonly Button _downBtn;
    private readonly ComboBox _languageCombo;
    private readonly CheckBox _startupCheck;
    private readonly CheckBox _commsCheck;
    private readonly CheckBox _toastCheck;
    private readonly CheckBox _leftClickCheck;
    private readonly Button _okBtn;
    private readonly Button _cancelBtn;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ConfigSchema? Result { get; private set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<HotkeySpec?, HotkeySpec?, HotkeySpec?, HotkeyProbeResult>? HotkeyRegistrationProbe { get; set; }

    // Pre-filled into an empty "Next device" box on first run, so the
    // common path is just "add devices, press OK".
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public HotkeySpec? SuggestedHotkey { get; set; }

    public SettingsForm(ConfigSchema current, IAudioDeviceService audio, IAudioDeviceService inputAudio,
        bool isFirstRun, bool startupEnabled)
    {
        _audio = audio;
        _inputAudio = inputAudio;
        _isFirstRun = isFirstRun;
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

        // --- Devices -------------------------------------------------------
        root.Controls.Add(new Label
        {
            Text = Strings.Get("settings.cycleDevices"),
            AutoSize = true,
            Font = _boldFont,
            Margin = new Padding(0, 0, 0, 2),
        });
        // Playback / Recording switch for the single list below.
        var tabs = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 0),
        };
        _playbackTab = NewTab("settings.tabPlayback");
        _recordingTab = NewTab("settings.tabRecording");
        _playbackTab.Checked = true;
        MarkSelectedTab();
        tabs.Controls.AddRange(new Control[] { _playbackTab, _recordingTab });
        root.Controls.Add(tabs);
        _devicesHint = AddHint(Strings.Get("settings.devicesHint"), topMargin: 6);
        root.Controls.Add(_devicesHint);

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

        // --- Hotkeys -------------------------------------------------------
        // Below the device list: on first run you pick devices, then the key.
        root.Controls.Add(new Label
        {
            Text = Strings.Get("settings.hotkeys"),
            AutoSize = true,
            Font = _boldFont,
            Margin = new Padding(0, 16, 0, 4),
        });
        _hotkeyTable = NewRow(3);
        _hotkeyTable.RowCount = 3;
        _hotkeyTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _hotkeyTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _hotkeyTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        (_nextBox, _nextClearBtn) = AddHotkeyRow(_hotkeyTable, 0, "settings.hotkeyNext");
        (_prevBox, _prevClearBtn) = AddHotkeyRow(_hotkeyTable, 1, "settings.hotkeyPrevious");
        (_inputBox, _inputClearBtn) = AddHotkeyRow(_hotkeyTable, 2, "settings.hotkeyInput");
        root.Controls.Add(_hotkeyTable);
        root.Controls.Add(AddHint(Strings.Get("settings.hotkeyHint"), topMargin: 2));

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
        _toastCheck = new CheckBox
        {
            Text = Strings.Get("settings.showToast"),
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0),
        };
        root.Controls.Add(_toastCheck);
        _leftClickCheck = new CheckBox
        {
            Text = Strings.Get("settings.leftClickCycles"),
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0),
        };
        root.Controls.Add(_leftClickCheck);

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
        _nextClearBtn.Click += (_, _) => _nextBox.Value = null;
        _prevClearBtn.Click += (_, _) => _prevBox.Value = null;
        _inputClearBtn.Click += (_, _) => _inputBox.Value = null;
        _nextBox.ValueChanged += (_, _) => UpdateButtons();
        _prevBox.ValueChanged += (_, _) => UpdateButtons();
        _inputBox.ValueChanged += (_, _) => UpdateButtons();
        _recordingTab.CheckedChanged += (_, _) => SwitchList(_recordingTab.Checked);
        _addBtn.Click += OnAddClicked;
        _removeBtn.Click += (_, _) => RemoveSelected();
        _upBtn.Click += (_, _) => MoveSelected(-1);
        _downBtn.Click += (_, _) => MoveSelected(+1);
        _okBtn.Click += OnOkClicked;
        _devicesList.SelectedIndexChanged += (_, _) => UpdateButtons();
        _devicesList.KeyDown += OnDevicesKeyDown;
        _devicesList.Resize += (_, _) => FitColumn();

        // Load working copy into UI.
        _nextBox.Value = HotkeyParser.FromConfigEntry(_workingCopy.Hotkey);
        _prevBox.Value = HotkeyParser.FromConfigEntry(_workingCopy.HotkeyPrevious);
        _inputBox.Value = HotkeyParser.FromConfigEntry(_workingCopy.HotkeyInput);
        _startupCheck.Checked = _workingCopy.StartWithWindows;
        _commsCheck.Checked = _workingCopy.SwitchCommunications;
        _toastCheck.Checked = _workingCopy.ShowToast;
        _leftClickCheck.Checked = _workingCopy.LeftClickCycles;
        SelectLanguageItem(_workingCopy.Language);
        ResumeLayout(performLayout: true);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (_isFirstRun && _nextBox.Value is null && SuggestedHotkey is HotkeySpec suggestion)
            _nextBox.Value = suggestion;
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

    private List<DeviceEntry> CurrentList => _editingInput ? _workingCopy.InputDevices : _workingCopy.Devices;
    private IAudioDeviceService CurrentAudio => _editingInput ? _inputAudio : _audio;

    // Button-style radios barely differ when checked in dark mode; bold the
    // selected one so it's obvious which list is being edited.
    private void MarkSelectedTab()
    {
        _playbackTab.Font = _playbackTab.Checked ? _boldFont : _baseFont;
        _recordingTab.Font = _recordingTab.Checked ? _boldFont : _baseFont;
    }

    private void SwitchList(bool input)
    {
        if (_editingInput == input) return;
        _editingInput = input;
        _devicesHint.Text = Strings.Get(input ? "settings.inputDevicesHint" : "settings.devicesHint");
        MarkSelectedTab();
        RefreshDevicesList(selectIndex: InitialSelection());
    }

    private static RadioButton NewTab(string key) => new()
    {
        Text = Strings.Get(key),
        Appearance = Appearance.Button,
        AutoSize = true,
        MinimumSize = new Size(110, 30),
        TextAlign = ContentAlignment.MiddleCenter,
        Margin = new Padding(0, 0, 4, 0),
        UseVisualStyleBackColor = true,
    };

    private static TableLayoutPanel NewRow(int columns) => new()
    {
        ColumnCount = columns,
        RowCount = 1,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
    };

    private (HotkeyTextBox box, Button clear) AddHotkeyRow(TableLayoutPanel table, int row, string labelKey)
    {
        string label = Strings.Get(labelKey);
        var box = new HotkeyTextBox
        {
            Dock = DockStyle.Fill,
            AccessibleName = label.TrimEnd(':', ' ', '：'),
            Margin = new Padding(0, 3, 8, 3),
        };
        var clear = NewButton("settings.hotkeyClear");
        clear.Margin = new Padding(0, 2, 0, 2);
        table.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 8, 0),
        }, 0, row);
        table.Controls.Add(box, 1, row);
        table.Controls.Add(clear, 2, row);
        return (box, clear);
    }

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

    // Wrap hints and option texts at the list's real (DPI-scaled) width so a
    // long translation never widens the dialog past the list. GDI
    // (TextRenderer) word-wrapping broke Russian text mid-word here, so they
    // lay out with GDI+, which only breaks between words.
    private void FitHints()
    {
        var maxSize = new Size(_devicesList.Width, 0);
        // A docked table reports its current width as preferred, which would
        // pin the dialog at whatever width it had before; line it up with the list.
        _hotkeyTable.Dock = DockStyle.None;
        _hotkeyTable.MinimumSize = maxSize;
        _hotkeyTable.MaximumSize = maxSize;
        foreach (var hint in _hints)
        {
            hint.UseCompatibleTextRendering = true;
            hint.MaximumSize = maxSize;
        }
        // CheckBox never wraps while AutoSize is on, so size it explicitly:
        // full list width, height measured for the wrapped text.
        int glyph = LogicalToDeviceUnits(22);
        foreach (var check in new[] { _startupCheck, _commsCheck, _toastCheck, _leftClickCheck })
        {
            check.UseCompatibleTextRendering = true;
            check.AutoSize = false;
            check.CheckAlign = ContentAlignment.TopLeft;
            check.TextAlign = ContentAlignment.TopLeft;
            using var g = check.CreateGraphics();
            var text = g.MeasureString(check.Text, check.Font, maxSize.Width - glyph);
            check.Size = new Size(maxSize.Width, (int)Math.Ceiling(text.Height) + LogicalToDeviceUnits(4));
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

        var live = CurrentAudio.EnumerateActiveOutputs();
        DeviceMatcher.HealEndpointIds(CurrentList, live);
        var available = new HashSet<string>(live.Select(d => d.EndpointId), StringComparer.Ordinal);
        string? currentDefault = CurrentAudio.GetDefaultOutputId(AudioRole.Multimedia);

        _devicesList.BeginUpdate();
        _devicesList.Items.Clear();
        foreach (var d in CurrentList)
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
        if (CurrentList.Count == 0) return -1;
        string? current = CurrentAudio.GetDefaultOutputId(AudioRole.Multimedia);
        int idx = CurrentList.FindIndex(d => d.EndpointId == current);
        return idx >= 0 ? idx : 0;
    }

    private int SelectedIndex =>
        _devicesList.SelectedIndices.Count > 0 ? _devicesList.SelectedIndices[0] : -1;

    private void UpdateButtons()
    {
        int idx = SelectedIndex;
        _removeBtn.Enabled = idx >= 0;
        _upBtn.Enabled = idx > 0;
        _downBtn.Enabled = idx >= 0 && idx < CurrentList.Count - 1;
        _nextClearBtn.Enabled = _nextBox.Value is not null;
        _prevClearBtn.Enabled = _prevBox.Value is not null;
        _inputClearBtn.Enabled = _inputBox.Value is not null;
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
        var live = CurrentAudio.EnumerateActiveOutputs();
        // Heal so a churned-ID device counts as registered instead of a duplicate candidate.
        DeviceMatcher.HealEndpointIds(CurrentList, live);
        var registered = new HashSet<string>(CurrentList.Select(d => d.EndpointId), StringComparer.Ordinal);
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
                    CurrentList.Add(new DeviceEntry
                    {
                        EndpointId = d.EndpointId,
                        DisplayName = d.DisplayName,
                        AddedAt = DateTimeOffset.Now,
                    });
                    RefreshDevicesList(selectIndex: CurrentList.Count - 1);
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
        CurrentList.RemoveAt(idx);
        RefreshDevicesList(selectIndex: idx);
        if (CurrentList.Count == 0) _addBtn.Focus();
    }

    private void MoveSelected(int delta)
    {
        int idx = SelectedIndex;
        if (idx < 0) return;
        int target = idx + delta;
        if (target < 0 || target >= CurrentList.Count) return;
        (CurrentList[idx], CurrentList[target]) =
            (CurrentList[target], CurrentList[idx]);
        RefreshDevicesList(selectIndex: target);
        _devicesList.Focus();
    }

    private void OnOkClicked(object? sender, EventArgs e)
    {
        HotkeySpec? next = _nextBox.Value;
        HotkeySpec? prev = _prevBox.Value;
        HotkeySpec? input = _inputBox.Value;

        if ((prev is not null && prev == next) || (input is not null && (input == next || input == prev)))
        {
            Warn("error.hotkeysSame");
            (input is not null && (input == next || input == prev) ? _inputBox : _prevBox).Focus();
            return;
        }

        // First run: closing with nothing set leaves a tray icon that does
        // nothing useful, so make sure that's intended.
        if (_isFirstRun)
        {
            bool anyDevices = _workingCopy.Devices.Count > 0 || _workingCopy.InputDevices.Count > 0;
            if (!anyDevices && !Confirm("settings.confirmNoDevices"))
            {
                _addBtn.Focus();
                return;
            }
            if (anyDevices && next is null && prev is null && input is null && !Confirm("settings.confirmNoHotkey"))
            {
                _nextBox.Focus();
                return;
            }
        }

        // Validate hotkey registration before accepting.
        if ((next is not null || prev is not null || input is not null) && HotkeyRegistrationProbe is not null)
        {
            var probe = HotkeyRegistrationProbe(next, prev, input);
            if (probe.Next != HotkeyRegisterResult.Ok)
            {
                Warn(HotkeyErrorKey(probe.Next));
                _nextBox.Focus();
                return;
            }
            if (probe.Previous != HotkeyRegisterResult.Ok)
            {
                Warn(HotkeyErrorKey(probe.Previous));
                _prevBox.Focus();
                return;
            }
            if (probe.Input != HotkeyRegisterResult.Ok)
            {
                Warn(HotkeyErrorKey(probe.Input));
                _inputBox.Focus();
                return;
            }
        }

        _workingCopy.Hotkey = next is HotkeySpec n ? HotkeyParser.ToConfigEntry(n) : null;
        _workingCopy.HotkeyPrevious = prev is HotkeySpec p ? HotkeyParser.ToConfigEntry(p) : null;
        _workingCopy.HotkeyInput = input is HotkeySpec i ? HotkeyParser.ToConfigEntry(i) : null;
        _workingCopy.StartWithWindows = _startupCheck.Checked;
        _workingCopy.SwitchCommunications = _commsCheck.Checked;
        _workingCopy.ShowToast = _toastCheck.Checked;
        _workingCopy.LeftClickCycles = _leftClickCheck.Checked;
        _workingCopy.Language = ((LangItem)_languageCombo.SelectedItem!).Code;

        Result = _workingCopy;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static string HotkeyErrorKey(HotkeyRegisterResult result) =>
        result == HotkeyRegisterResult.InUse ? "error.hotkeyInUse" : "error.hotkeyInvalid";

    private void Warn(string key) =>
        MessageBox.Show(this, Strings.Get(key), Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private bool Confirm(string key) =>
        MessageBox.Show(this, Strings.Get(key), Strings.Get("app.title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;

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
