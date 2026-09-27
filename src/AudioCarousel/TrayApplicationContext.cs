using System.Diagnostics;
using System.Security;
using System.Windows.Forms;
using AudioCarousel.Audio;
using AudioCarousel.Config;
using AudioCarousel.Cycle;
using AudioCarousel.Hotkey;
using AudioCarousel.I18n;
using AudioCarousel.Startup;
using AudioCarousel.UI;

namespace AudioCarousel;

internal sealed class TrayApplicationContext : ApplicationContext, ICycleSink
{
    private const string RepoUrl = "https://github.com/kai-rin/audio-carousel";

    private readonly string _exePath;
    private readonly ConfigStore _store;
    private readonly ConfigSchema _config;
    private readonly bool _configUnreadable;
    private readonly StartupRegistration _startup;
    private readonly IAudioDeviceService _audio;
    private readonly IAudioDeviceService _inputAudio;
    private readonly CycleController _inputCycle;
    private readonly HotkeyHost _inputHotkeyHost;
    private readonly TrayIcon _tray;
    private readonly ToastWindow _toast;
    private readonly HotkeyHost _hotkeyHost;
    private readonly HotkeyHost _prevHotkeyHost;
    private readonly CycleController _cycle;
    private readonly SynchronizationContext _ui;
    private readonly DefaultDeviceWatcher? _watcher;
    private readonly RegisteredWaitHandle? _showSettingsWait;

    private Task _saveChain = Task.CompletedTask;
    private SettingsForm? _openSettings;
    private bool _exitAfterSettings;

    public TrayApplicationContext(WaitHandle? showSettingsSignal)
    {
        _exePath = Environment.ProcessPath ?? Application.ExecutablePath;
        string configPath = Path.Combine(Path.GetDirectoryName(_exePath)!, "audio-carousel.json");

        _store = new ConfigStore(configPath);
        var load = _store.Load();
        _config = load.Config;
        _configUnreadable = load.WasUnreadable;

        // Apply language.
        Strings.SetLanguage(Strings.ResolveLanguage(_config.Language, Strings.GetCurrentUiCultureName()));

        _startup = new StartupRegistration();
        if (_config.StartWithWindows)
        {
            // Portable app: keep the Run entry pointing at wherever the exe lives now.
            TryRegistry(() => _startup.EnsurePath(_exePath), showError: false);
        }
        _audio = new AudioDeviceService(AudioFlow.Render);
        _inputAudio = new AudioDeviceService(AudioFlow.Capture);

        // Creating the first control installs the WinForms synchronization
        // context, so posted work runs on this (UI) thread once the loop starts.
        _toast = new ToastWindow();
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _tray = new TrayIcon();
        _hotkeyHost = new HotkeyHost();
        _prevHotkeyHost = new HotkeyHost();
        _inputHotkeyHost = new HotkeyHost();

        _cycle = new CycleController(_config, _audio, this, PersistCurrentIndex);
        _inputCycle = new CycleController(_config, _inputAudio, this, PersistCurrentIndex, CycleTarget.Input);

        WireTrayEvents();
        var hotkeyResult = ApplyHotkeysFromConfig(showErrors: false);
        RefreshTrayCurrentLabel();
        _tray.SetStartupChecked(IsStartupEnabled());

        // Keep the tray label right when Windows or another app changes the
        // default device. Non-essential: the app works without it.
        try
        {
            _watcher = new DefaultDeviceWatcher(_ui);
            _watcher.Changed += RefreshTrayCurrentLabel;
        }
        catch (Exception)
        {
            _watcher = null;
        }

        // A second launch signals us instead of just saying "already running".
        if (showSettingsSignal is not null)
        {
            _showSettingsWait = ThreadPool.RegisterWaitForSingleObject(
                showSettingsSignal,
                (_, _) => _ui.Post(_ => OpenSettings(firstRun: false), null),
                null, Timeout.Infinite, executeOnlyOnce: false);
        }

        // Dialogs wait for Application.Run to start the message loop.
        if (load.WasUnreadable)
            PostMessageBox("error.configUnreadable", MessageBoxIcon.Warning);
        if (load.WasCorrupted)
            PostMessageBox("error.configCorrupted", MessageBoxIcon.Warning);
        if (hotkeyResult is HotkeyRegisterResult result)
            PostMessageBox(HotkeyErrorKey(result), MessageBoxIcon.Warning);
        if (load.FreshlyCreated)
            _ui.Post(_ => OpenSettings(firstRun: true), null);
    }

    private void PostMessageBox(string key, MessageBoxIcon icon) =>
        _ui.Post(_ => MessageBox.Show(Strings.Get(key), Strings.Get("app.title"), MessageBoxButtons.OK, icon), null);

    private static string HotkeyErrorKey(HotkeyRegisterResult result) =>
        result == HotkeyRegisterResult.InUse ? "error.hotkeyInUse" : "error.hotkeyInvalid";

    private void WireTrayEvents()
    {
        _tray.CycleRequested += () => _cycle.Cycle();
        _tray.CyclePreviousRequested += () => _cycle.CyclePrevious();
        _tray.CycleInputRequested += () => _inputCycle.Cycle();
        _tray.InputDeviceSelected += id => _inputCycle.SwitchTo(id);
        _tray.LeftClicked += () =>
        {
            if (_config.LeftClickCycles) _cycle.Cycle();
            else OpenSettings(firstRun: false);
        };
        _tray.SettingsRequested += () => OpenSettings(firstRun: false);
        _tray.AboutRequested += ShowAbout;
        _tray.ExitRequested += ExitApp;
        _tray.StartupToggled += OnStartupToggled;
        _tray.MenuOpening += RefreshTrayDeviceItems;
        _tray.DeviceSelected += id => _cycle.SwitchTo(id);
    }

    private void RefreshTrayDeviceItems()
    {
        var live = _audio.EnumerateActiveOutputs();
        if (DeviceMatcher.HealEndpointIds(_config.Devices, live))
        {
            PersistCurrentIndex();
        }
        _tray.SetDevices(BuildRows(_config.Devices, live, _audio));

        if (_config.InputDevices.Count > 0)
        {
            var liveInputs = _inputAudio.EnumerateActiveOutputs();
            if (DeviceMatcher.HealEndpointIds(_config.InputDevices, liveInputs))
                PersistCurrentIndex();
            _tray.SetInputDevices(BuildRows(_config.InputDevices, liveInputs, _inputAudio));
        }
        else
        {
            _tray.SetInputDevices(Array.Empty<TrayDeviceRow>());
        }
        _tray.SetStartupChecked(IsStartupEnabled());
    }

    private static List<TrayDeviceRow> BuildRows(
        List<DeviceEntry> devices, IReadOnlyList<AudioDevice> live, IAudioDeviceService audio)
    {
        var liveIds = new HashSet<string>(live.Select(d => d.EndpointId), StringComparer.Ordinal);
        string? currentId = audio.GetDefaultOutputId(AudioRole.Multimedia);
        return devices
            .Select(d => new TrayDeviceRow(
                d.EndpointId,
                d.DisplayName,
                liveIds.Contains(d.EndpointId),
                d.EndpointId == currentId))
            .ToList();
    }

    private void OpenSettings(bool firstRun)
    {
        // The tray menu stays usable while the modal dialog is up; never stack
        // a second settings window on top of the first.
        if (_openSettings is not null)
        {
            _openSettings.Activate();
            return;
        }

        using var form = new SettingsForm(_config, _audio, _inputAudio, firstRun, IsStartupEnabled())
        {
            HotkeyRegistrationProbe = ProbeHotkeys,
            SuggestedHotkey = firstRun && _config.Hotkey is null ? FindFreeHotkey() : null,
        };
        _openSettings = form;
        DialogResult result;
        try
        {
            result = form.ShowDialog();
        }
        finally
        {
            _openSettings = null;
        }

        if (result == DialogResult.OK && form.Result is not null && !_exitAfterSettings)
        {
            var newCfg = form.Result;
            // Copy newCfg back into _config (reference-stable for CycleController).
            _config.Hotkey = newCfg.Hotkey;
            _config.Devices = newCfg.Devices;
            _config.Language = newCfg.Language;
            _config.HotkeyPrevious = newCfg.HotkeyPrevious;
            _config.HotkeyInput = newCfg.HotkeyInput;
            _config.InputDevices = newCfg.InputDevices;
            if (_config.InputCurrentIndex >= _config.InputDevices.Count) _config.InputCurrentIndex = 0;
            _config.SwitchCommunications = newCfg.SwitchCommunications;
            _config.ShowToast = newCfg.ShowToast;
            _config.LeftClickCycles = newCfg.LeftClickCycles;
            if (_config.CurrentIndex >= _config.Devices.Count) _config.CurrentIndex = 0;

            Strings.SetLanguage(Strings.ResolveLanguage(_config.Language, Strings.GetCurrentUiCultureName()));
            _tray.ApplyLabels();

            if (newCfg.StartWithWindows != IsStartupEnabled())
                SetStartup(newCfg.StartWithWindows);
            _config.StartWithWindows = IsStartupEnabled();
            _tray.SetStartupChecked(_config.StartWithWindows);

            SaveConfigWithFeedback();
        }

        if (_exitAfterSettings)
        {
            ExitThread();
            return;
        }

        // Always re-apply from current _config — this cleans up any leftover
        // hotkey registration left behind by a successful probe followed by Cancel.
        ApplyHotkeysFromConfig(showErrors: true);
        RefreshTrayCurrentLabel();

        if (firstRun) ShowRunningBalloon();
    }

    // First free combination from a short list of rarely used ones.
    private static HotkeySpec? FindFreeHotkey()
    {
        var mods = HotkeyModifier.Control | HotkeyModifier.Alt;
        foreach (var key in new[] { Keys.F9, Keys.F10, Keys.F11, Keys.F12 })
        {
            var spec = new HotkeySpec(mods, key);
            using var probe = new HotkeyHost();
            if (probe.TryRegister(spec, () => { }) == HotkeyRegisterResult.Ok) return spec;
        }
        return null;
    }

    // The tray icon is easy to lose (Windows 11 starts new icons in the
    // overflow flyout), so say once where the app went and how to use it.
    private void ShowRunningBalloon()
    {
        var spec = HotkeyParser.FromConfigEntry(_config.Hotkey);
        string text;
        if (_config.Devices.Count == 0 && _config.InputDevices.Count == 0)
            text = Strings.Get("balloon.noDevices");
        else if (_config.Devices.Count == 0)
            // Microphone-only setup: the output hotkey/click wouldn't do anything.
            text = Strings.Get(_config.HotkeyInput is null ? "balloon.inputOnlyMenu" : "balloon.inputOnly");
        else if (spec is HotkeySpec s)
            text = string.Format(Strings.Get("balloon.withHotkey"), HotkeyParser.FormatForDisplay(s));
        else if (_config.LeftClickCycles)
            text = Strings.Get("balloon.noHotkey");
        else
            text = Strings.Get("balloon.noHotkeyMenu");
        _tray.ShowBalloon(Strings.Get("balloon.title"), text);
    }

    private HotkeyProbeResult ProbeHotkeys(HotkeySpec? next, HotkeySpec? previous, HotkeySpec? input)
    {
        // Free both first so swapping the two combinations doesn't collide
        // with our own registrations. On success, the OK path re-applies
        // from config anyway.
        _hotkeyHost.Unregister();
        _prevHotkeyHost.Unregister();
        _inputHotkeyHost.Unregister();
        var nextResult = next is HotkeySpec n
            ? _hotkeyHost.TryRegister(n, () => _cycle.Cycle())
            : HotkeyRegisterResult.Ok;
        var prevResult = previous is HotkeySpec p
            ? _prevHotkeyHost.TryRegister(p, () => _cycle.CyclePrevious())
            : HotkeyRegisterResult.Ok;
        var inputResult = input is HotkeySpec i
            ? _inputHotkeyHost.TryRegister(i, () => _inputCycle.Cycle())
            : HotkeyRegisterResult.Ok;
        if (nextResult != HotkeyRegisterResult.Ok || prevResult != HotkeyRegisterResult.Ok
            || inputResult != HotkeyRegisterResult.Ok)
        {
            // Re-apply the previous registrations so we don't end up with none.
            ApplyHotkeysFromConfig(showErrors: false);
        }
        return new HotkeyProbeResult(nextResult, prevResult, inputResult);
    }

    // Registers both configured hotkeys. Returns the first failure, or null
    // when everything that is configured registered fine.
    private HotkeyRegisterResult? ApplyHotkeysFromConfig(bool showErrors)
    {
        _hotkeyHost.Unregister();
        _prevHotkeyHost.Unregister();
        _inputHotkeyHost.Unregister();
        var nextFailure = Register(_hotkeyHost, _config.Hotkey, () => _cycle.Cycle());
        var prevFailure = Register(_prevHotkeyHost, _config.HotkeyPrevious, () => _cycle.CyclePrevious());
        var inputFailure = Register(_inputHotkeyHost, _config.HotkeyInput, () => _inputCycle.Cycle());
        var failure = nextFailure ?? prevFailure ?? inputFailure;
        if (failure is HotkeyRegisterResult f && showErrors)
        {
            MessageBox.Show(Strings.Get(HotkeyErrorKey(f)),
                Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        return failure;
    }

    private static HotkeyRegisterResult? Register(HotkeyHost host, HotkeyEntry? entry, Action onHotkey)
    {
        var spec = HotkeyParser.FromConfigEntry(entry);
        if (spec is null) return null;
        var result = host.TryRegister(spec.Value, onHotkey);
        return result == HotkeyRegisterResult.Ok ? null : result;
    }

    private void OnStartupToggled(bool isChecked)
    {
        SetStartup(isChecked);
        _config.StartWithWindows = IsStartupEnabled();
        // Reflect what actually happened, e.g. when the registry write failed.
        _tray.SetStartupChecked(_config.StartWithWindows);
        SaveConfigWithFeedback();
    }

    private void SetStartup(bool enable) =>
        TryRegistry(() =>
        {
            if (enable) _startup.Enable(_exePath);
            else _startup.Disable();
        }, showError: true);

    private bool IsStartupEnabled()
    {
        try { return _startup.IsEnabled(); }
        catch (Exception ex) when (IsRegistryError(ex)) { return false; }
    }

    private void TryRegistry(Action action, bool showError)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (IsRegistryError(ex))
        {
            if (showError)
            {
                MessageBox.Show(Strings.Get("error.startupFailed"),
                    Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private static bool IsRegistryError(Exception ex) =>
        ex is UnauthorizedAccessException or SecurityException or IOException;

    // Explicit user-initiated saves surface failures as a localized message
    // instead of the raw unhandled-exception dialog.
    private void SaveConfigWithFeedback()
    {
        try
        {
            _store.Save(_config);
        }
        catch (Exception)
        {
            MessageBox.Show(Strings.Get(_configUnreadable ? "error.configUnreadable" : "error.saveFailed"),
                Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowAbout()
    {
        var page = new TaskDialogPage
        {
            Caption = Strings.Get("tray.about"),
            Heading = $"{Strings.Get("app.title")} {AppVersion.Display}",
            Text = $"{Strings.Get("about.body")}\n\n<a href=\"{RepoUrl}\">{RepoUrl}</a>",
            EnableLinks = true,
            Icon = TaskDialogIcon.Information,
            Buttons = { TaskDialogButton.OK },
        };
        page.LinkClicked += (_, e) =>
        {
            try { Process.Start(new ProcessStartInfo(e.LinkHref) { UseShellExecute = true }); }
            catch (Exception) { /* no default browser; the URL is visible in the dialog */ }
        };
        TaskDialog.ShowDialog(page);
    }

    private void ExitApp()
    {
        if (_openSettings is not null)
        {
            // Unwind the modal loop first; OpenSettings exits once it returns.
            _exitAfterSettings = true;
            _openSettings.Close();
            return;
        }
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _showSettingsWait?.Unregister(null);
            _watcher?.Dispose();
            _hotkeyHost.Dispose();
            _prevHotkeyHost.Dispose();
            _inputHotkeyHost.Dispose();
            _tray.Dispose();
            _toast.Dispose();
            // Let a just-queued save (e.g. the last cycle before Exit) land.
            _saveChain.Wait(TimeSpan.FromSeconds(3));
        }
        base.Dispose(disposing);
    }

    private void RefreshTrayCurrentLabel()
    {
        string? output = CurrentDeviceName(_config.Devices, _audio);
        // The microphone line only appears once microphones are in use.
        string? input = _config.InputDevices.Count > 0 ? CurrentDeviceName(_config.InputDevices, _inputAudio) : null;
        _tray.SetCurrentDeviceLabel(output, input);
    }

    private string? CurrentDeviceName(List<DeviceEntry> devices, IAudioDeviceService audio)
    {
        var live = audio.EnumerateActiveOutputs();
        string? currentId = audio.GetDefaultOutputId(AudioRole.Multimedia);
        if (currentId is not null && !devices.Any(d => d.EndpointId == currentId)
            && DeviceMatcher.HealEndpointIds(devices, live))
        {
            PersistCurrentIndex();
        }
        // Prefer the registered name; fall back to the live name so the
        // tooltip is right even when the current device isn't in the cycle.
        string? name = devices.FirstOrDefault(d => d.EndpointId == currentId)?.DisplayName;
        if (name is null && currentId is not null)
        {
            foreach (var d in live)
            {
                if (d.EndpointId == currentId) { name = d.DisplayName; break; }
            }
        }
        return name;
    }

    private void PersistCurrentIndex()
    {
        // Snapshot on the UI thread; the worker serializes a private copy, so it
        // never races with later edits. Chaining keeps saves in order.
        var snapshot = _config.Clone();
        _saveChain = _saveChain.ContinueWith(_ =>
        {
            try { _store.Save(snapshot); } catch { /* best-effort background save */ }
        }, TaskScheduler.Default);
    }

    // === ICycleSink ===
    public void ShowToast(string text)
    {
        // Errors always show; the success toast is optional.
        if (_config.ShowToast) _toast.ShowMessage(text);
    }
    public void ShowErrorToast(string text) => _toast.ShowMessage(text, isError: true);
    public void NotifyCurrentDeviceChanged() => RefreshTrayCurrentLabel();
}
