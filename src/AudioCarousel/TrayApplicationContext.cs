using System.Diagnostics;
using System.Reflection;
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
    private readonly string _exeDir;
    private readonly string _exePath;
    private readonly ConfigStore _store;
    private readonly ConfigSchema _config;
    private readonly bool _freshlyCreated;
    private readonly StartupRegistration _startup;
    private readonly IAudioDeviceService _audio;
    private readonly TrayIcon _tray;
    private readonly ToastWindow _toast;
    private readonly HotkeyHost _hotkeyHost;
    private readonly CycleController _cycle;

    public TrayApplicationContext()
    {
        _exePath = Process.GetCurrentProcess().MainModule!.FileName!;
        _exeDir = Path.GetDirectoryName(_exePath)!;
        string configPath = Path.Combine(_exeDir, "audio-carousel.json");

        _store = new ConfigStore(configPath);
        bool wasCorrupted;
        (_config, _freshlyCreated, wasCorrupted) = _store.Load();

        // Apply language.
        Strings.SetLanguage(Strings.ResolveLanguage(_config.Language, Strings.IsCurrentUiCultureJapanese()));

        _startup = new StartupRegistration();
        if (_config.StartWithWindows)
        {
            _startup.EnsurePath(_exePath);
        }
        _audio = new AudioDeviceService();

        _toast = new ToastWindow();
        _tray = new TrayIcon();
        _hotkeyHost = new HotkeyHost();

        _cycle = new CycleController(_config, _audio, this, PersistCurrentIndex);

        WireTrayEvents();
        ApplyHotkeyFromConfig();
        RefreshTrayCurrentLabel();
        _tray.SetStartupChecked(_config.StartWithWindows);

        // Defer dialogs until after Application.Run starts the message loop.
        // SynchronizationContext.Current is null here in the constructor, so use
        // a one-shot UI-thread Timer.
        if (wasCorrupted)
        {
            DeferToUiThread(() =>
                MessageBox.Show(Strings.Get("error.configCorrupted"),
                    Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning));
        }
        if (_freshlyCreated)
        {
            DeferToUiThread(() => OpenSettings(firstRun: true));
        }
    }

    private static void DeferToUiThread(Action action)
    {
        var timer = new System.Windows.Forms.Timer { Interval = 1 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            action();
        };
        timer.Start();
    }

    private void WireTrayEvents()
    {
        _tray.CycleRequested    += () => _cycle.Cycle();
        _tray.SettingsRequested += () => OpenSettings(firstRun: false);
        _tray.AboutRequested    += ShowAbout;
        _tray.ExitRequested     += ExitApp;
        _tray.StartupToggled    += OnStartupToggled;
    }

    private void OpenSettings(bool firstRun)
    {
        using var form = new SettingsForm(_config, _audio, firstRun)
        {
            HotkeyRegistrationProbe = ProbeHotkey,
        };
        var result = form.ShowDialog();

        if (result == DialogResult.OK && form.Result is not null)
        {
            var newCfg = form.Result;
            // Copy newCfg back into _config (reference-stable for CycleController).
            _config.Hotkey = newCfg.Hotkey;
            _config.Devices = newCfg.Devices;
            _config.Language = newCfg.Language;
            _config.StartWithWindows = newCfg.StartWithWindows;
            if (_config.CurrentIndex >= _config.Devices.Count) _config.CurrentIndex = 0;

            Strings.SetLanguage(Strings.ResolveLanguage(_config.Language, Strings.IsCurrentUiCultureJapanese()));
            _tray.ApplyLabels();
            _tray.SetStartupChecked(_config.StartWithWindows);

            if (_config.StartWithWindows) _startup.Enable(_exePath);
            else _startup.Disable();

            _store.Save(_config);
        }

        // Always re-apply from current _config — this cleans up any leftover
        // hotkey registration left behind by a successful probe followed by Cancel.
        ApplyHotkeyFromConfig();
        RefreshTrayCurrentLabel();
    }

    private bool ProbeHotkey(HotkeySpec spec)
    {
        // Try to register; if success, we re-apply from config in the OK path anyway.
        bool ok = _hotkeyHost.TryRegister(spec, () => _cycle.Cycle());
        if (!ok)
        {
            // Re-apply previous registration so we don't end up with no hotkey.
            ApplyHotkeyFromConfig();
        }
        return ok;
    }

    private void ApplyHotkeyFromConfig()
    {
        var spec = HotkeyParser.FromConfigEntry(_config.Hotkey);
        if (spec is null)
        {
            _hotkeyHost.Unregister();
            return;
        }
        if (!_hotkeyHost.TryRegister(spec.Value, () => _cycle.Cycle()))
        {
            MessageBox.Show(Strings.Get("error.hotkeyInUse"),
                Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnStartupToggled(bool isChecked)
    {
        _config.StartWithWindows = isChecked;
        if (isChecked) _startup.Enable(_exePath);
        else _startup.Disable();
        _store.Save(_config);
    }

    private void ShowAbout()
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "dev";
        MessageBox.Show($"{Strings.Get("app.title")} v{version}\n\n{Strings.Get("about.body")}",
            Strings.Get("tray.about"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExitApp()
    {
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkeyHost.Dispose();
            _tray.Dispose();
            _toast.Dispose();
        }
        base.Dispose(disposing);
    }

    private void RefreshTrayCurrentLabel()
    {
        if (_config.Devices.Count == 0)
        {
            _tray.SetCurrentDeviceLabel(null);
            return;
        }
        string? currentId = _audio.GetDefaultOutputId(AudioRole.Multimedia);
        var match = _config.Devices.FirstOrDefault(d => d.EndpointId == currentId);
        _tray.SetCurrentDeviceLabel(match?.DisplayName ?? null);
    }

    private void PersistCurrentIndex()
    {
        // Fire-and-forget save on a thread-pool thread to avoid blocking the hotkey path.
        var snapshot = _config;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try { _store.Save(snapshot); } catch { /* swallow per spec */ }
        });
    }

    // === ICycleSink ===
    public void ShowToast(string text) => _toast.ShowMessage(text);
    public void ShowErrorToast(string text) => _toast.ShowMessage(text, isError: true);
    public void NotifyCurrentDeviceChanged() => RefreshTrayCurrentLabel();
}
