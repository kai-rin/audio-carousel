namespace AudioCarousel.Audio;

public readonly record struct AudioDevice(string EndpointId, string DisplayName);

public enum AudioFlow
{
    // Playback (speakers, headphones).
    Render,
    // Recording (microphones).
    Capture,
}

public enum AudioRole
{
    Console = 0,
    Multimedia = 1,
    Communications = 2,
}
