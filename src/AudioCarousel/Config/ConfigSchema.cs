using System.Text.Json.Serialization;

namespace AudioCarousel.Config;

public sealed class ConfigSchema
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "auto";

    [JsonPropertyName("hotkey")]
    public HotkeyEntry? Hotkey { get; set; }

    [JsonPropertyName("devices")]
    public List<DeviceEntry> Devices { get; set; } = new();

    [JsonPropertyName("currentIndex")]
    public int CurrentIndex { get; set; }

    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows { get; set; }

    // Whether cycling also moves the Communications role (the device
    // Teams/Discord/Zoom use for calls). Absent in older files => true.
    [JsonPropertyName("switchCommunications")]
    public bool SwitchCommunications { get; set; } = true;

    public ConfigSchema Clone() => new()
    {
        Version = Version,
        Language = Language,
        Hotkey = Hotkey is null ? null : new HotkeyEntry
        {
            Modifiers = new List<string>(Hotkey.Modifiers),
            Key = Hotkey.Key,
        },
        Devices = Devices.Select(d => new DeviceEntry
        {
            EndpointId = d.EndpointId,
            DisplayName = d.DisplayName,
            AddedAt = d.AddedAt,
        }).ToList(),
        CurrentIndex = CurrentIndex,
        StartWithWindows = StartWithWindows,
        SwitchCommunications = SwitchCommunications,
    };
}

public sealed class HotkeyEntry
{
    [JsonPropertyName("modifiers")]
    public List<string> Modifiers { get; set; } = new();

    [JsonPropertyName("key")]
    public string Key { get; set; } = "";
}

public sealed class DeviceEntry
{
    [JsonPropertyName("endpointId")]
    public string EndpointId { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("addedAt")]
    public DateTimeOffset AddedAt { get; set; }
}
