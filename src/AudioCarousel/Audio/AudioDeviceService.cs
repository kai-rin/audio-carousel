using NAudio.CoreAudioApi;

namespace AudioCarousel.Audio;

public sealed class AudioDeviceService : IAudioDeviceService
{
    private readonly DataFlow _flow;

    // One instance per direction: the "outputs" in the interface's member
    // names are microphones for a Capture instance.
    public AudioDeviceService(AudioFlow flow = AudioFlow.Render)
    {
        _flow = flow == AudioFlow.Capture ? DataFlow.Capture : DataFlow.Render;
    }

    public IReadOnlyList<AudioDevice> EnumerateActiveOutputs()
    {
        using var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(_flow, DeviceState.Active);
        var result = new List<AudioDevice>(devices.Count);
        foreach (var d in devices)
        {
            try
            {
                // FriendlyName reads the endpoint's property store, which can
                // throw for a device with a broken driver. Skip such devices
                // instead of letting the exception crash the whole cycle.
                result.Add(new AudioDevice(d.ID, d.FriendlyName));
            }
            catch (Exception)
            {
                // unreadable endpoint — skip
            }
            finally
            {
                d.Dispose();
            }
        }
        return result;
    }

    public string? GetDefaultOutputId(AudioRole role)
    {
        using var enumerator = new MMDeviceEnumerator();
        try
        {
            using var d = enumerator.GetDefaultAudioEndpoint(_flow, ToNAudioRole(role));
            return d.ID;
        }
        catch
        {
            return null;
        }
    }

    public void SetDefault(string endpointId, AudioRole role)
    {
        PolicyConfig.SetDefaultEndpoint(endpointId, (uint)role);
    }

    private static Role ToNAudioRole(AudioRole role) => role switch
    {
        AudioRole.Console => Role.Console,
        AudioRole.Multimedia => Role.Multimedia,
        AudioRole.Communications => Role.Communications,
        _ => Role.Multimedia,
    };
}
