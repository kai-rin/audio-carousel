using AudioCarousel.Audio;
using AudioCarousel.Config;
using AudioCarousel.I18n;

namespace AudioCarousel.Cycle;

public sealed class CycleController
{
    private static readonly AudioRole[] AllRoles =
        { AudioRole.Multimedia, AudioRole.Communications, AudioRole.Console };

    private readonly ConfigSchema _config;
    private readonly IAudioDeviceService _audio;
    private readonly ICycleSink _sink;
    private readonly Action _persistConfig;

    public CycleController(
        ConfigSchema config,
        IAudioDeviceService audio,
        ICycleSink sink,
        Action persistConfig)
    {
        _config = config;
        _audio = audio;
        _sink = sink;
        _persistConfig = persistConfig;
    }

    public void Cycle()
    {
        if (_config.Devices.Count == 0) return;

        var live = _audio.EnumerateActiveOutputs();
        // Heal before building the available set and before the sync below, so a
        // re-bound entry (endpoint-ID churn) is both selectable and syncable.
        bool healed = DeviceMatcher.HealEndpointIds(_config.Devices, live);
        var available = new HashSet<string>(
            live.Select(d => d.EndpointId),
            StringComparer.Ordinal);

        // Sync currentIndex with OS reality before advancing.
        string? currentDefault = _audio.GetDefaultOutputId(AudioRole.Multimedia);
        if (currentDefault is not null)
        {
            int syncIndex = _config.Devices.FindIndex(d => d.EndpointId == currentDefault);
            if (syncIndex >= 0) _config.CurrentIndex = syncIndex;
        }

        int count = _config.Devices.Count;
        int startIndex = (_config.CurrentIndex + 1) % count;
        int targetIndex = -1;
        for (int offset = 0; offset < count; offset++)
        {
            int i = (startIndex + offset) % count;
            if (available.Contains(_config.Devices[i].EndpointId))
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
        if (_config.Devices.Count == 0) return;

        var live = _audio.EnumerateActiveOutputs();
        bool healed = DeviceMatcher.HealEndpointIds(_config.Devices, live);

        int targetIndex = _config.Devices.FindIndex(d => d.EndpointId == endpointId);
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
        var target = _config.Devices[targetIndex];

        try
        {
            foreach (var role in AllRoles)
                _audio.SetDefault(target.EndpointId, role);
        }
        catch (Exception)
        {
            // The heal is a config repair independent of the switch outcome.
            if (healed) _persistConfig();
            _sink.ShowErrorToast(Strings.Get("error.switchFailed"));
            return;
        }

        _config.CurrentIndex = targetIndex;
        _persistConfig();
        _sink.ShowToast(target.DisplayName);
        _sink.NotifyCurrentDeviceChanged();
    }
}
