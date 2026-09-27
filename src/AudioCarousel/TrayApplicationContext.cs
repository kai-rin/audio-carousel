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
    private readonly TrayIcon _tray;
    private readonly ToastWindow _toast;
    private readonly HotkeyHost _hotkeyHost;
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
        _audio = new AudioDeviceService();

        // Creating the first control installs the WinForms synchronization
        // context, so posted work runs on this (UI) thread once the loop starts.
        _toast = new ToastWindow();
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _tray = new TrayIcon();
        _hotkeyHost = new HotkeyHost();

        _cycle = new CycleController(_config, _audio, this, PersistCurrentIndex);

        WireTrayEvents();
        var hotkeyResult = ApplyHotkeyFromConfig(showErrors: false);
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
        if (hotkeyResult is HotkeyRegisterResult result && result != HotkeyRegisterResult.Ok)
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
        var liveIds = new HashSet<string>(live.Select(d => d.EndpointId), StringComparer.Ordinal);
        string? currentId = _audio.GetDefaultOutputId(AudioRole.Multimedia);
        var rows = _config.Devices
            .Select(d => new TrayDeviceRow(
                d.EndpointId,
                d.DisplayName,
                liveIds.Contains(d.EndpointId),
                d.EndpointId == currentId))
            .ToList();
        _tray.SetDevices(rows);
        _tray.SetStartupChecked(IsStartupEnabled());
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

        using var form = new SettingsForm(_config, _audio, firstRun, IsStartupEnabled())
        {
            HotkeyRegistrationProbe = ProbeHotkey,
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
            _config.SwitchCommunications = newCfg.SwitchCommunications;
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
        ApplyHotkeyFromConfig(showErrors: true);
        RefreshTrayCurrentLabel();
    }

    private HotkeyRegisterResult ProbeHotkey(HotkeySpec spec)
    {
        // Try to register; if success, we re-apply from config in the OK path anyway.
        var result = _hotkeyHost.TryRegister(spec, () => _cycle.Cycle());
        if (result != HotkeyRegisterResult.Ok)
        {
            // Re-apply previous registration so we don't end up with no hotkey.
            ApplyHotkeyFromConfig(showErrors: false);
        }
        return result;
    }

    // Returns null when no hotkey is configured.
    private HotkeyRegisterResult? ApplyHotkeyFromConfig(bool showErrors)
    {
        var spec = HotkeyParser.FromConfigEntry(_config.Hotkey);
        if (spec is null)
        {
            _hotkeyHost.Unregister();
            return null;
        }
        var result = _hotkeyHost.TryRegister(spec.Value, () => _cycle.Cycle());
        if (result != HotkeyRegisterResult.Ok && showErrors)
        {
            MessageBox.Show(Strings.Get(HotkeyErrorKey(result)),
                Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        return result;
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
            _tray.Dispose();
            _toast.Dispose();
            // Let a just-queued save (e.g. the last cycle before Exit) land.
            _saveChain.Wait(TimeSpan.FromSeconds(3));
        }
        base.Dispose(disposing);
    }

    private void RefreshTrayCurrentLabel()
    {
        var live = _audio.EnumerateActiveOutputs();
        string? currentId = _audio.GetDefaultOutputId(AudioRole.Multimedia);
        if (currentId is not null && !_config.Devices.Any(d => d.EndpointId == currentId)
            && DeviceMatcher.HealEndpointIds(_config.Devices, live))
        {
            PersistCurrentIndex();
        }
        // Prefer the registered name; fall back to the live name so the
        // tooltip is right even when the current device isn't in the cycle.
        string? name = _config.Devices.FirstOrDefault(d => d.EndpointId == currentId)?.DisplayName;
        if (name is null && currentId is not null)
        {
            foreach (var d in live)
            {
                if (d.EndpointId == currentId) { name = d.DisplayName; break; }
            }
        }
        _tray.SetCurrentDeviceLabel(name);
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
    public void ShowToast(string text) => _toast.ShowMessage(text);
    public void ShowErrorToast(string text) => _toast.ShowMessage(text, isError: true);
    public void NotifyCurrentDeviceChanged() => RefreshTrayCurrentLabel();
}
