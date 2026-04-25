namespace AudioCarousel.Audio;

public interface IAudioDeviceService
{
    IReadOnlyList<AudioDevice> EnumerateActiveOutputs();
    string? GetDefaultOutputId(AudioRole role);
    void SetDefault(string endpointId, AudioRole role);
}
