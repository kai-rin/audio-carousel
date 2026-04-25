namespace AudioCarousel.Audio;

public readonly record struct AudioDevice(string EndpointId, string DisplayName);

public enum AudioRole
{
    Console = 0,
    Multimedia = 1,
    Communications = 2,
}
