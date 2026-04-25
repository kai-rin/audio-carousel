using System.Text.Json;
using AudioCarousel.Config;
using Xunit;

namespace AudioCarousel.Tests;

public class ConfigStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly string _path;

    public ConfigStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "AudioCarousel-Tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _path = Path.Combine(_dir, "audio-carousel.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void Load_WhenFileMissing_CreatesDefaultsAndReportsFreshlyCreated()
    {
        var store = new ConfigStore(_path);

        var (config, freshlyCreated, wasCorrupted) = store.Load();

        Assert.True(freshlyCreated);
        Assert.False(wasCorrupted);
        Assert.True(File.Exists(_path));
        Assert.Equal(1, config.Version);
        Assert.Equal("auto", config.Language);
        Assert.Null(config.Hotkey);
        Assert.Empty(config.Devices);
        Assert.Equal(0, config.CurrentIndex);
        Assert.False(config.StartWithWindows);
    }

    [Fact]
    public void SaveThenLoad_RoundtripsAllValues()
    {
        var store = new ConfigStore(_path);
        var original = new ConfigSchema
        {
            Language = "ja",
            Hotkey = new HotkeyEntry { Modifiers = new() { "Ctrl" }, Key = "F16" },
            Devices = new()
            {
                new DeviceEntry
                {
                    EndpointId = "id-1",
                    DisplayName = "Speaker",
                    AddedAt = DateTimeOffset.Now,
                },
            },
            CurrentIndex = 0,
            StartWithWindows = true,
        };

        store.Save(original);
        var (loaded, freshlyCreated, wasCorrupted) = store.Load();

        Assert.False(freshlyCreated);
        Assert.False(wasCorrupted);
        Assert.Equal("ja", loaded.Language);
        Assert.NotNull(loaded.Hotkey);
        Assert.Equal("F16", loaded.Hotkey!.Key);
        Assert.Single(loaded.Devices);
        Assert.True(loaded.StartWithWindows);
    }

    [Fact]
    public void Load_WhenFileCorrupt_BacksUpAndReturnsDefaults()
    {
        File.WriteAllText(_path, "{ this is not valid json");
        var store = new ConfigStore(_path);

        var (config, freshlyCreated, wasCorrupted) = store.Load();

        Assert.True(freshlyCreated);
        Assert.True(wasCorrupted);
        Assert.True(File.Exists(_path + ".bak"));
        Assert.Empty(config.Devices);
    }

    [Fact]
    public void Load_ClampsCurrentIndexToValidRange()
    {
        var store = new ConfigStore(_path);
        store.Save(new ConfigSchema
        {
            Devices = new() { new DeviceEntry { EndpointId = "a", DisplayName = "A" } },
            CurrentIndex = 999,
        });

        var (loaded, _, _) = store.Load();
        Assert.Equal(0, loaded.CurrentIndex);
    }
}
