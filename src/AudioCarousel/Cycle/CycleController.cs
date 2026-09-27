using AudioCarousel.Audio;
using AudioCarousel.Config;
using AudioCarousel.I18n;

namespace AudioCarousel.Cycle;

/// <summary>Which device list a controller cycles.</summary>
public enum CycleTarget
{
    Output,
    Input,
}

public sealed class CycleController
{
    private static readonly AudioRole[] AllRoles =
        { AudioRole.Multimedia, AudioRole.Communications, AudioRole.Console };
    private static readonly AudioRole[] NonCommunicationRoles =
        { AudioRole.Multimedia, AudioRole.Console };

    private readonly ConfigSchema _config;
    private readonly IAudioDeviceService _audio;
    private readonly ICycleSink _sink;
    private readonly Action _persistConfig;
    private readonly CycleTarget _target;

    public CycleController(
        ConfigSchema config,
        IAudioDeviceService audio,
        ICycleSink sink,
        Action persistConfig,
        CycleTarget target = CycleTarget.Output)
    {
        _config = config;
        _audio = audio;
        _sink = sink;
        _persistConfig = persistConfig;
        _target = target;
    }

    // Read through the config every time: SettingsForm's OK path replaces
    // the list instances.
    private List<DeviceEntry> Devices =>
        _target == CycleTarget.Input ? _config.InputDevices : _config.Devices;

    private int CurrentIndex
    {
        get => _target == CycleTarget.Input ? _config.InputCurrentIndex : _config.CurrentIndex;
        set
        {
            if (_target == CycleTarget.Input) _config.InputCurrentIndex = value;
            else _config.CurrentIndex = value;
        }
    }

    public void Cycle() => Step(+1);

    /// <summary>Same as <see cref="Cycle"/>, walking the list backwards.</summary>
    public void CyclePrevious() => Step(-1);

    private void Step(int direction)
    {
        if (Devices.Count == 0)
        {
            _sink.ShowErrorToast(Strings.Get(_target == CycleTarget.Input
                ? "error.noInputDevicesConfigured"
                : "error.noDevicesConfigured"));
            return;
        }

        var live = _audio.EnumerateActiveOutputs();
        // Heal before building the available set and before the sync below, so a
        // re-bound entry (endpoint-ID churn) is both selectable and syncable.
        bool healed = DeviceMatcher.HealEndpointIds(Devices, live);
        var available = new HashSet<string>(
            live.Select(d => d.EndpointId),
            StringComparer.Ordinal);

        // Sync currentIndex with OS reality before moving.
        string? currentDefault = _audio.GetDefaultOutputId(AudioRole.Multimedia);
        if (currentDefault is not null)
        {
            int syncIndex = Devices.FindIndex(d => d.EndpointId == currentDefault);
            if (syncIndex >= 0) CurrentIndex = syncIndex;
        }

        int count = Devices.Count;
        int targetIndex = -1;
        for (int offset = 1; offset <= count; offset++)
        {
            int i = ((CurrentIndex + direction * offset) % count + count) % count;
            if (available.Contains(Devices[i].EndpointId))
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex < 0)
        {
            if (healed) _persistConfig();
            _sink.ShowErrorToast(Strings.Get("error.noDeviceAvailable"));
            return;
        }

        ApplySwitch(targetIndex, healed);
    }

    /// <summary>
    /// Switches directly to the registered device with the given endpoint ID
    /// (tray-menu selection). The ID is expected to be current (the menu is
    /// built after healing), so it is matched post-heal here as well.
    /// </summary>
    public void SwitchTo(string endpointId)
    {
        if (Devices.Count == 0) return;

        var live = _audio.EnumerateActiveOutputs();
        bool healed = DeviceMatcher.HealEndpointIds(Devices, live);

        int targetIndex = Devices.FindIndex(d => d.EndpointId == endpointId);
        bool isAvailable = live.Any(d => string.Equals(d.EndpointId, endpointId, StringComparison.Ordinal));
        if (targetIndex < 0 || !isAvailable)
        {
            if (healed) _persistConfig();
            _sink.ShowErrorToast(Strings.Get("error.noDeviceAvailable"));
            return;
        }

        ApplySwitch(targetIndex, healed);
    }

    private void ApplySwitch(int targetIndex, bool healed)
    {
        var target = Devices[targetIndex];
        var roles = _config.SwitchCommunications ? AllRoles : NonCommunicationRoles;

        var applied = new List<(AudioRole role, string? previous)>(roles.Length);
        try
        {
            foreach (var role in roles)
            {
                string? previous = _audio.GetDefaultOutputId(role);
                _audio.SetDefault(target.EndpointId, role);
                applied.Add((role, previous));
            }
        }
        catch (Exception)
        {
            RollBack(applied);
            // The heal is a config repair independent of the switch outcome.
            if (healed) _persistConfig();
            _sink.ShowErrorToast(Strings.Get("error.switchFailed"));
            return;
        }

        CurrentIndex = targetIndex;
        _persistConfig();
        // Mark microphone switches so they can't be mistaken for output ones.
        _sink.ShowToast(_target == CycleTarget.Input ? "\U0001F3A4 " + target.DisplayName : target.DisplayName);
        _sink.NotifyCurrentDeviceChanged();
    }

    // Best-effort: put already-switched roles back so a partial failure
    // doesn't leave playback and calls on different devices.
    private void RollBack(List<(AudioRole role, string? previous)> applied)
    {
        foreach (var (role, previous) in applied)
        {
            if (previous is null) continue;
            try { _audio.SetDefault(previous, role); }
            catch (Exception) { /* nothing more we can do; the error toast follows */ }
        }
    }
}
