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

    // Optional second hotkey that walks the list backwards.
    [JsonPropertyName("hotkeyPrevious")]
    public HotkeyEntry? HotkeyPrevious { get; set; }

    [JsonPropertyName("devices")]
    public List<DeviceEntry> Devices { get; set; } = new();

    [JsonPropertyName("currentIndex")]
    public int CurrentIndex { get; set; }

    // Microphones / recording devices, cycled independently of outputs.
    [JsonPropertyName("inputDevices")]
    public List<DeviceEntry> InputDevices { get; set; } = new();

    [JsonPropertyName("inputCurrentIndex")]
    public int InputCurrentIndex { get; set; }

    [JsonPropertyName("hotkeyInput")]
    public HotkeyEntry? HotkeyInput { get; set; }

    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows { get; set; }

    // Whether cycling also moves the Communications role (the device
    // Teams/Discord/Zoom use for calls). Absent in older files => true.
    [JsonPropertyName("switchCommunications")]
    public bool SwitchCommunications { get; set; } = true;

    [JsonPropertyName("showToast")]
    public bool ShowToast { get; set; } = true;

    // Left-click on the tray icon: cycle (true, the original behavior) or
    // open Settings (false).
    [JsonPropertyName("leftClickCycles")]
    public bool LeftClickCycles { get; set; } = true;

    // Defaults for a config the app creates itself (first run / reset).
    // Property initializers stay the "field missing in an older file" values,
    // so existing users keep their behavior.
    public static ConfigSchema NewInstallDefaults() => new() { LeftClickCycles = false };

    public ConfigSchema Clone() => new()
    {
        Version = Version,
        Language = Language,
        Hotkey = CloneHotkey(Hotkey),
        HotkeyPrevious = CloneHotkey(HotkeyPrevious),
        HotkeyInput = CloneHotkey(HotkeyInput),
        Devices = CloneDevices(Devices),
        CurrentIndex = CurrentIndex,
        InputDevices = CloneDevices(InputDevices),
        InputCurrentIndex = InputCurrentIndex,
        StartWithWindows = StartWithWindows,
        SwitchCommunications = SwitchCommunications,
        ShowToast = ShowToast,
        LeftClickCycles = LeftClickCycles,
    };

    private static List<DeviceEntry> CloneDevices(List<DeviceEntry> devices) =>
        devices.Select(d => new DeviceEntry
        {
            EndpointId = d.EndpointId,
            DisplayName = d.DisplayName,
            AddedAt = d.AddedAt,
        }).ToList();

    private static HotkeyEntry? CloneHotkey(HotkeyEntry? entry) => entry is null ? null : new HotkeyEntry
    {
        Modifiers = new List<string>(entry.Modifiers),
        Key = entry.Key,
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
