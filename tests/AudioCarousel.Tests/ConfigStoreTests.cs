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

        var (config, freshlyCreated, wasCorrupted, _) = store.Load();

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
        var (loaded, freshlyCreated, wasCorrupted, _) = store.Load();

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

        var (config, freshlyCreated, wasCorrupted, _) = store.Load();

        Assert.True(freshlyCreated);
        Assert.True(wasCorrupted);
        Assert.True(File.Exists(_path + ".bak"));
        Assert.Empty(config.Devices);
    }

    // Portable-app promise: dropping the exe into an unwritable location must
    // not crash startup. Load's initial save is best-effort; the app runs with
    // an in-memory config.
    [Fact]
    public void Load_WhenDirectoryUnwritable_StillReturnsDefaultsWithoutThrowing()
    {
        string unwritablePath = Path.Combine(_dir, "no-such-subdir", "audio-carousel.json");
        var store = new ConfigStore(unwritablePath);

        var (config, freshlyCreated, wasCorrupted, _) = store.Load();

        Assert.True(freshlyCreated);
        Assert.False(wasCorrupted);
        Assert.Empty(config.Devices);
    }

    // A transient lock (antivirus, indexer, backup tool) is not corruption:
    // the user's config must not be renamed to .bak or overwritten.
    [Fact]
    public void Load_WhenFileLocked_ReportsUnreadableAndLeavesFileUntouched()
    {
        const string original = "{\"version\":1,\"language\":\"ja\",\"devices\":[]}";
        File.WriteAllText(_path, original);
        var store = new ConfigStore(_path);

        ConfigLoadResult result;
        using (new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            result = store.Load();
        }

        Assert.True(result.WasUnreadable);
        Assert.False(result.WasCorrupted);
        Assert.False(result.FreshlyCreated);
        Assert.False(File.Exists(_path + ".bak"));
        Assert.Equal(original, File.ReadAllText(_path));
    }

    // After an unreadable load the in-memory config is defaults; saving it
    // would silently wipe the user's real config.
    [Fact]
    public void Save_AfterUnreadableLoad_ThrowsAndDoesNotOverwrite()
    {
        const string original = "{\"version\":1,\"language\":\"ja\",\"devices\":[]}";
        File.WriteAllText(_path, original);
        var store = new ConfigStore(_path);

        ConfigLoadResult result;
        using (new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            result = store.Load();
        }

        Assert.Throws<InvalidOperationException>(() => store.Save(result.Config));
        Assert.Equal(original, File.ReadAllText(_path));
    }

    // System.Text.Json does not enforce non-null annotations, so a hand-edited
    // file can carry nulls anywhere. Load must normalize them instead of
    // letting them crash the tray/cycle/settings code later.
    [Fact]
    public void Load_NullsInHandEditedFile_AreNormalized()
    {
        File.WriteAllText(_path, """
            {
              "version": 1,
              "language": null,
              "hotkey": { "modifiers": null, "key": "F16" },
              "devices": [
                null,
                { "endpointId": null, "displayName": "Speaker" },
                { "endpointId": "id-2", "displayName": null },
                { "endpointId": null, "displayName": null }
              ],
              "currentIndex": 0,
              "startWithWindows": false
            }
            """);
        var store = new ConfigStore(_path);

        var result = store.Load();

        Assert.False(result.WasCorrupted);
        Assert.Equal("auto", result.Config.Language);
        Assert.Null(result.Config.Hotkey);
        Assert.Equal(2, result.Config.Devices.Count);
        Assert.All(result.Config.Devices, d =>
        {
            Assert.NotNull(d.EndpointId);
            Assert.NotNull(d.DisplayName);
        });
        Assert.Equal("Speaker", result.Config.Devices[0].DisplayName);
        Assert.Equal("id-2", result.Config.Devices[1].EndpointId);
    }

    [Fact]
    public void Load_NullsInPreviousHotkey_AreNormalized()
    {
        File.WriteAllText(_path,
            "{\"version\":1,\"hotkeyPrevious\":{\"modifiers\":null,\"key\":\"F15\"},\"devices\":[]}");
        var store = new ConfigStore(_path);

        var result = store.Load();

        Assert.False(result.WasCorrupted);
        Assert.Null(result.Config.HotkeyPrevious);
    }

    [Fact]
    public void Load_NullDevicesList_IsNormalizedNotTreatedAsCorrupt()
    {
        File.WriteAllText(_path, "{\"version\":1,\"devices\":null}");
        var store = new ConfigStore(_path);

        var result = store.Load();

        Assert.False(result.WasCorrupted);
        Assert.NotNull(result.Config.Devices);
        Assert.Empty(result.Config.Devices);
    }

    [Fact]
    public void Load_NullModifierElement_KeepsOtherModifiers()
    {
        File.WriteAllText(_path,
            "{\"version\":1,\"hotkey\":{\"modifiers\":[\"Ctrl\",null],\"key\":\"F16\"},\"devices\":[]}");
        var store = new ConfigStore(_path);

        var result = store.Load();

        Assert.NotNull(result.Config.Hotkey);
        Assert.Equal(new List<string> { "Ctrl" }, result.Config.Hotkey!.Modifiers);
    }

    [Fact]
    public void Load_DuplicateEndpointIds_KeepsFirstOnly()
    {
        File.WriteAllText(_path, """
            {"version":1,"devices":[
              {"endpointId":"a","displayName":"A"},
              {"endpointId":"a","displayName":"A again"}
            ]}
            """);
        var store = new ConfigStore(_path);

        var result = store.Load();

        Assert.Single(result.Config.Devices);
        Assert.Equal("A", result.Config.Devices[0].DisplayName);
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

        var (loaded, _, _, _) = store.Load();
        Assert.Equal(0, loaded.CurrentIndex);
    }
}
