using System.Text.Json;
using AudioCarousel.Config;
using Xunit;

namespace AudioCarousel.Tests;

public class ConfigSchemaTests
{
    [Fact]
    public void Roundtrip_PreservesAllFields()
    {
        var original = new ConfigSchema
        {
            Version = 1,
            Language = "ja",
            Hotkey = new HotkeyEntry { Modifiers = new() { "Ctrl", "Alt" }, Key = "F16" },
            Devices = new()
            {
                new DeviceEntry
                {
                    EndpointId = "{0.0.0.00000000}.{abc}",
                    DisplayName = "LG ULTRAGEAR+",
                    AddedAt = DateTimeOffset.Parse("2026-04-25T17:00:00+09:00"),
                },
            },
            CurrentIndex = 0,
            StartWithWindows = true,
        };

        string json = JsonSerializer.Serialize(original, ConfigJsonContext.Default.ConfigSchema);
        var roundtripped = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.ConfigSchema);

        Assert.NotNull(roundtripped);
        Assert.Equal(original.Version, roundtripped!.Version);
        Assert.Equal(original.Language, roundtripped.Language);
        Assert.Equal(original.Hotkey!.Key, roundtripped.Hotkey!.Key);
        Assert.Equal(original.Hotkey.Modifiers, roundtripped.Hotkey.Modifiers);
        Assert.Single(roundtripped.Devices);
        Assert.Equal(original.Devices[0].EndpointId, roundtripped.Devices[0].EndpointId);
        Assert.Equal(original.Devices[0].DisplayName, roundtripped.Devices[0].DisplayName);
        Assert.Equal(original.Devices[0].AddedAt, roundtripped.Devices[0].AddedAt);
        Assert.Equal(original.CurrentIndex, roundtripped.CurrentIndex);
        Assert.Equal(original.StartWithWindows, roundtripped.StartWithWindows);
    }

    // Configs written before the option existed must keep today's behavior.
    [Fact]
    public void SwitchCommunications_MissingFromJson_DefaultsToTrue()
    {
        var loaded = JsonSerializer.Deserialize("{\"version\":1,\"devices\":[]}", ConfigJsonContext.Default.ConfigSchema);
        Assert.True(loaded!.SwitchCommunications);
    }

    // Files written before these options existed keep today's behavior.
    [Fact]
    public void NewOptions_MissingFromJson_KeepPreviousBehavior()
    {
        var loaded = JsonSerializer.Deserialize("{\"version\":1,\"devices\":[]}", ConfigJsonContext.Default.ConfigSchema);
        Assert.Null(loaded!.HotkeyPrevious);
        Assert.True(loaded.ShowToast);
        Assert.True(loaded.LeftClickCycles);
    }

    [Fact]
    public void Clone_CopiesNewOptions()
    {
        var original = new ConfigSchema
        {
            HotkeyPrevious = new HotkeyEntry { Modifiers = new() { "Ctrl" }, Key = "F15" },
            ShowToast = false,
            LeftClickCycles = false,
        };

        var copy = original.Clone();
        copy.HotkeyPrevious!.Modifiers.Add("Alt");

        Assert.Single(original.HotkeyPrevious!.Modifiers);
        Assert.Equal("F15", copy.HotkeyPrevious.Key);
        Assert.False(copy.ShowToast);
        Assert.False(copy.LeftClickCycles);
    }

    [Fact]
    public void Clone_IsDeepCopy()
    {
        var original = new ConfigSchema
        {
            Language = "ja",
            Hotkey = new HotkeyEntry { Modifiers = new() { "Ctrl" }, Key = "F16" },
            Devices = new() { new DeviceEntry { EndpointId = "a", DisplayName = "A" } },
            CurrentIndex = 0,
            StartWithWindows = true,
            SwitchCommunications = false,
        };

        var copy = original.Clone();
        copy.Devices[0].EndpointId = "changed";
        copy.Devices.Add(new DeviceEntry { EndpointId = "b" });
        copy.Hotkey!.Modifiers.Add("Alt");

        Assert.Equal("a", original.Devices[0].EndpointId);
        Assert.Single(original.Devices);
        Assert.Single(original.Hotkey!.Modifiers);
        Assert.False(copy.SwitchCommunications);
        Assert.True(copy.StartWithWindows);
        Assert.Equal("ja", copy.Language);
    }

    [Fact]
    public void Hotkey_CanBeNull()
    {
        var config = new ConfigSchema { Hotkey = null };
        string json = JsonSerializer.Serialize(config, ConfigJsonContext.Default.ConfigSchema);
        Assert.Contains("\"hotkey\": null", json);
    }
}
