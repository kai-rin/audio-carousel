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
