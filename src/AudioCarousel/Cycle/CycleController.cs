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
    private readonly Action _persistCurrentIndex;

    public CycleController(
        ConfigSchema config,
        IAudioDeviceService audio,
        ICycleSink sink,
        Action persistCurrentIndex)
    {
        _config = config;
        _audio = audio;
        _sink = sink;
        _persistCurrentIndex = persistCurrentIndex;
    }

    public void Cycle()
    {
        if (_config.Devices.Count == 0) return;

        var available = new HashSet<string>(
            _audio.EnumerateActiveOutputs().Select(d => d.EndpointId),
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
            _sink.ShowErrorToast(Strings.Get("error.noDeviceAvailable"));
            return;
        }

        var target = _config.Devices[targetIndex];

        try
        {
            foreach (var role in AllRoles)
                _audio.SetDefault(target.EndpointId, role);
        }
        catch (Exception)
        {
            _sink.ShowErrorToast(Strings.Get("error.switchFailed"));
            return;
        }

        _config.CurrentIndex = targetIndex;
        _persistCurrentIndex();
        _sink.ShowToast(target.DisplayName);
        _sink.NotifyCurrentDeviceChanged();
    }
}
