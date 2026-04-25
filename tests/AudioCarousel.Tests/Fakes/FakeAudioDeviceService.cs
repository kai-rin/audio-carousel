using AudioCarousel.Audio;

namespace AudioCarousel.Tests.Fakes;

public sealed class FakeAudioDeviceService : IAudioDeviceService
{
    public List<AudioDevice> ActiveOutputs { get; } = new();
    public Dictionary<AudioRole, string?> Defaults { get; } = new()
    {
        [AudioRole.Console] = null,
        [AudioRole.Multimedia] = null,
        [AudioRole.Communications] = null,
    };
    public List<(string id, AudioRole role)> SetCalls { get; } = new();
    public Func<string, AudioRole, Exception?>? SetDefaultException { get; set; }

    public IReadOnlyList<AudioDevice> EnumerateActiveOutputs() => ActiveOutputs.ToList();

    public string? GetDefaultOutputId(AudioRole role) => Defaults[role];

    public void SetDefault(string endpointId, AudioRole role)
    {
        var ex = SetDefaultException?.Invoke(endpointId, role);
        if (ex is not null) throw ex;
        SetCalls.Add((endpointId, role));
        Defaults[role] = endpointId;
    }
}
