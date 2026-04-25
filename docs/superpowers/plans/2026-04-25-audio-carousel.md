# Audio Carousel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a lightweight Windows tray utility (single .exe, no installer) that cycles the system default audio output through a user-configured device list via a global hotkey, with a brief on-screen toast on each switch.

**Architecture:** WinForms application on .NET 9 with NativeAOT publish. Logic-heavy components (config, cycle algorithm, hotkey parsing, i18n) are pure C# with unit tests. UI/COM components (tray, toast, settings dialog, audio device service, hotkey host) are exercised via manual smoke tests.

**Tech Stack:**
- .NET 9 SDK, C# 13, WinForms
- `System.Text.Json` source generator (AOT-compatible JSON)
- `NAudio.CoreAudioApi` NuGet package for `MMDeviceEnumerator` wrapper
- Inline `[ComImport]` declaration for `IPolicyConfig` (not in NAudio)
- xUnit for unit tests

**Spec:** See `docs/superpowers/specs/2026-04-25-audio-carousel-design.md`.

**Project layout:**

```
audio-carousel/
├── AudioCarousel.sln
├── src/AudioCarousel/
│   ├── AudioCarousel.csproj
│   ├── Program.cs                 # entry, Mutex, app bootstrap
│   ├── TrayApplicationContext.cs  # ApplicationContext (no main form)
│   ├── Config/
│   │   ├── ConfigSchema.cs        # POCO + JsonSerializable
│   │   ├── ConfigJsonContext.cs   # source generator context
│   │   └── ConfigStore.cs         # load/save, atomic, recovery
│   ├── Hotkey/
│   │   ├── HotkeySpec.cs          # value type for parsed hotkey
│   │   ├── HotkeyParser.cs        # string<->spec
│   │   └── HotkeyHost.cs          # NativeWindow + RegisterHotKey
│   ├── Audio/
│   │   ├── AudioDevice.cs         # value type {id, name}
│   │   ├── IAudioDeviceService.cs # interface (testable)
│   │   ├── AudioDeviceService.cs  # NAudio + IPolicyConfig impl
│   │   └── PolicyConfig.cs        # COM declaration
│   ├── Cycle/
│   │   ├── ICycleSink.cs          # toast + tray-refresh interface
│   │   └── CycleController.cs     # cycle algorithm
│   ├── Startup/
│   │   └── StartupRegistration.cs # HKCU Run-key
│   ├── I18n/
│   │   ├── Language.cs            # enum
│   │   └── Strings.cs             # static string table
│   ├── UI/
│   │   ├── TrayIcon.cs            # NotifyIcon + context menu
│   │   ├── ToastWindow.cs         # frameless TopMost form
│   │   ├── HotkeyTextBox.cs       # capture-style input
│   │   └── SettingsForm.cs        # main config dialog
│   └── Resources/tray.ico
├── tests/AudioCarousel.Tests/
│   ├── AudioCarousel.Tests.csproj
│   ├── HotkeyParserTests.cs
│   ├── ConfigStoreTests.cs
│   ├── StringsTests.cs
│   ├── CycleControllerTests.cs
│   ├── StartupRegistrationTests.cs
│   └── Fakes/FakeAudioDeviceService.cs
├── scripts/publish.ps1
└── README.md
```

---

## Task 1: Solution & Project Scaffold

**Files:**
- Create: `AudioCarousel.sln`
- Create: `src/AudioCarousel/AudioCarousel.csproj`
- Create: `tests/AudioCarousel.Tests/AudioCarousel.Tests.csproj`
- Create: `.gitignore`

- [ ] **Step 1: Verify .NET 9 SDK is installed**

Run: `dotnet --list-sdks`
Expected: a line starting with `9.` (e.g., `9.0.100 [...]`). If not installed, install from https://dotnet.microsoft.com/download/dotnet/9.0 first.

- [ ] **Step 2: Initialize git repo**

```bash
cd /d/AIworkspace/audio-carousel
git init
git branch -m main
```

- [ ] **Step 3: Create .gitignore**

Write `.gitignore`:
```
bin/
obj/
*.user
.vs/
publish/
*.bak
```

- [ ] **Step 4: Create solution and projects**

```bash
cd /d/AIworkspace/audio-carousel
dotnet new sln -n AudioCarousel
dotnet new winforms -n AudioCarousel -o src/AudioCarousel --framework net9.0
dotnet new xunit  -n AudioCarousel.Tests -o tests/AudioCarousel.Tests --framework net9.0
dotnet sln add src/AudioCarousel/AudioCarousel.csproj
dotnet sln add tests/AudioCarousel.Tests/AudioCarousel.Tests.csproj
dotnet add tests/AudioCarousel.Tests/AudioCarousel.Tests.csproj reference src/AudioCarousel/AudioCarousel.csproj
```

- [ ] **Step 5: Configure src csproj for AOT and single-file publish**

Replace `src/AudioCarousel/AudioCarousel.csproj` contents with:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>AudioCarousel</RootNamespace>
    <AssemblyName>AudioCarousel</AssemblyName>
    <ApplicationIcon>Resources\tray.ico</ApplicationIcon>

    <!-- AOT settings (apply at publish only; dev build stays JIT) -->
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>false</InvariantGlobalization>
    <RuntimeIdentifier Condition="'$(IsPublishing)' == 'true'">win-x64</RuntimeIdentifier>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NAudio.CoreAudioApi" Version="2.2.1" />
  </ItemGroup>
</Project>
```

Note: tray.ico will be added in Task 17. The `ApplicationIcon` line is fine even before the file exists for the build (it warns but doesn't error). If it does error, comment that line out and re-enable in Task 17.

- [ ] **Step 6: Configure tests csproj**

Replace `tests/AudioCarousel.Tests/AudioCarousel.Tests.csproj` contents with:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWindowsForms>true</UseWindowsForms>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\AudioCarousel\AudioCarousel.csproj" />
  </ItemGroup>
</Project>
```

Delete the boilerplate `tests/AudioCarousel.Tests/UnitTest1.cs` if it exists.

- [ ] **Step 7: Delete WinForms boilerplate from src**

Delete: `src/AudioCarousel/Form1.cs`, `src/AudioCarousel/Form1.Designer.cs`, `src/AudioCarousel/Form1.resx`, `src/AudioCarousel/Program.cs`. We will create a new `Program.cs` later.

Create a placeholder `src/AudioCarousel/Program.cs`:
```csharp
namespace AudioCarousel;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
    }
}
```

- [ ] **Step 8: Verify build**

Run: `dotnet build`
Expected: Build succeeded, 0 errors. Warnings about missing icon are OK.

- [ ] **Step 9: Verify tests run (none yet)**

Run: `dotnet test`
Expected: "No test is available" or 0 tests passed, exit 0.

- [ ] **Step 10: Commit**

```bash
git add .gitignore AudioCarousel.sln src/ tests/ docs/
git commit -m "chore: scaffold solution and projects"
```

---

## Task 2: Config Schema & JSON Source Generator

**Files:**
- Create: `src/AudioCarousel/Config/ConfigSchema.cs`
- Create: `src/AudioCarousel/Config/ConfigJsonContext.cs`
- Create: `tests/AudioCarousel.Tests/ConfigSchemaTests.cs`

- [ ] **Step 1: Write failing roundtrip test**

Create `tests/AudioCarousel.Tests/ConfigSchemaTests.cs`:
```csharp
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
        Assert.Equal(original.CurrentIndex, roundtripped.CurrentIndex);
        Assert.Equal(original.StartWithWindows, roundtripped.StartWithWindows);
    }

    [Fact]
    public void Hotkey_CanBeNull()
    {
        var config = new ConfigSchema { Hotkey = null };
        string json = JsonSerializer.Serialize(config, ConfigJsonContext.Default.ConfigSchema);
        Assert.Contains("\"hotkey\": null", json);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~ConfigSchemaTests"`
Expected: Build error — types `ConfigSchema`, `HotkeyEntry`, `DeviceEntry`, `ConfigJsonContext` not defined.

- [ ] **Step 3: Create ConfigSchema POCO**

Create `src/AudioCarousel/Config/ConfigSchema.cs`:
```csharp
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
```

- [ ] **Step 4: Create JSON source generator context**

Create `src/AudioCarousel/Config/ConfigJsonContext.cs`:
```csharp
using System.Text.Json.Serialization;

namespace AudioCarousel.Config;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    PropertyNamingPolicy = null)]
[JsonSerializable(typeof(ConfigSchema))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~ConfigSchemaTests"`
Expected: 2 tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/AudioCarousel/Config/ tests/AudioCarousel.Tests/ConfigSchemaTests.cs
git commit -m "feat(config): add schema POCOs and JSON source generator context"
```

---

## Task 3: ConfigStore (load, save, recovery)

**Files:**
- Create: `src/AudioCarousel/Config/ConfigStore.cs`
- Create: `tests/AudioCarousel.Tests/ConfigStoreTests.cs`

- [ ] **Step 1: Write failing test for default-when-missing**

Create `tests/AudioCarousel.Tests/ConfigStoreTests.cs`:
```csharp
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

        var (config, freshlyCreated) = store.Load();

        Assert.True(freshlyCreated);
        Assert.True(File.Exists(_path));
        Assert.Equal(1, config.Version);
        Assert.Equal("auto", config.Language);
        Assert.Null(config.Hotkey);
        Assert.Empty(config.Devices);
        Assert.Equal(0, config.CurrentIndex);
        Assert.False(config.StartWithWindows);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~ConfigStoreTests"`
Expected: Build error — `ConfigStore` not defined.

- [ ] **Step 3: Implement ConfigStore minimal**

Create `src/AudioCarousel/Config/ConfigStore.cs`:
```csharp
using System.Text.Json;

namespace AudioCarousel.Config;

public sealed class ConfigStore
{
    private readonly string _path;
    private readonly object _lock = new();

    public ConfigStore(string path)
    {
        _path = path;
    }

    public (ConfigSchema config, bool freshlyCreated) Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_path))
            {
                var defaults = new ConfigSchema();
                SaveInternal(defaults);
                return (defaults, true);
            }

            try
            {
                string json = File.ReadAllText(_path);
                var loaded = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.ConfigSchema);
                if (loaded is null) throw new InvalidDataException("config deserialized to null");
                ClampCurrentIndex(loaded);
                return (loaded, false);
            }
            catch (Exception)
            {
                string backup = _path + ".bak";
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(_path, backup);
                var defaults = new ConfigSchema();
                SaveInternal(defaults);
                return (defaults, true);
            }
        }
    }

    public void Save(ConfigSchema config)
    {
        lock (_lock)
        {
            ClampCurrentIndex(config);
            SaveInternal(config);
        }
    }

    private void SaveInternal(ConfigSchema config)
    {
        string tmp = _path + ".tmp";
        string json = JsonSerializer.Serialize(config, ConfigJsonContext.Default.ConfigSchema);
        File.WriteAllText(tmp, json);
        if (File.Exists(_path))
            File.Replace(tmp, _path, destinationBackupFileName: null);
        else
            File.Move(tmp, _path);
    }

    private static void ClampCurrentIndex(ConfigSchema config)
    {
        if (config.Devices.Count == 0)
        {
            config.CurrentIndex = 0;
            return;
        }
        if (config.CurrentIndex < 0 || config.CurrentIndex >= config.Devices.Count)
            config.CurrentIndex = 0;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~ConfigStoreTests"`
Expected: 1 test passes.

- [ ] **Step 5: Add roundtrip test**

Append to `ConfigStoreTests.cs` inside the class:
```csharp
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
    var (loaded, freshlyCreated) = store.Load();

    Assert.False(freshlyCreated);
    Assert.Equal("ja", loaded.Language);
    Assert.NotNull(loaded.Hotkey);
    Assert.Equal("F16", loaded.Hotkey!.Key);
    Assert.Single(loaded.Devices);
    Assert.True(loaded.StartWithWindows);
}
```

Run: `dotnet test --filter "FullyQualifiedName~ConfigStoreTests"`
Expected: 2 tests pass.

- [ ] **Step 6: Add corruption recovery test**

Append to `ConfigStoreTests.cs`:
```csharp
[Fact]
public void Load_WhenFileCorrupt_BacksUpAndReturnsDefaults()
{
    File.WriteAllText(_path, "{ this is not valid json");
    var store = new ConfigStore(_path);

    var (config, freshlyCreated) = store.Load();

    Assert.True(freshlyCreated);
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

    var (loaded, _) = store.Load();
    Assert.Equal(0, loaded.CurrentIndex);
}
```

Run: `dotnet test --filter "FullyQualifiedName~ConfigStoreTests"`
Expected: 4 tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/AudioCarousel/Config/ConfigStore.cs tests/AudioCarousel.Tests/ConfigStoreTests.cs
git commit -m "feat(config): add ConfigStore with atomic save and corruption recovery"
```

---

## Task 4: HotkeyParser

**Files:**
- Create: `src/AudioCarousel/Hotkey/HotkeySpec.cs`
- Create: `src/AudioCarousel/Hotkey/HotkeyParser.cs`
- Create: `tests/AudioCarousel.Tests/HotkeyParserTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/AudioCarousel.Tests/HotkeyParserTests.cs`:
```csharp
using System.Windows.Forms;
using AudioCarousel.Hotkey;
using Xunit;

namespace AudioCarousel.Tests;

public class HotkeyParserTests
{
    [Fact]
    public void Parse_ModifierLessF16_ReturnsSpec()
    {
        var spec = HotkeyParser.Parse(new() { }, "F16");
        Assert.Equal(HotkeyModifier.None, spec.Modifiers);
        Assert.Equal(Keys.F16, spec.Key);
    }

    [Fact]
    public void Parse_CtrlAltA_ReturnsSpec()
    {
        var spec = HotkeyParser.Parse(new() { "Ctrl", "Alt" }, "A");
        Assert.Equal(HotkeyModifier.Control | HotkeyModifier.Alt, spec.Modifiers);
        Assert.Equal(Keys.A, spec.Key);
    }

    [Fact]
    public void Parse_AllFourModifiers_Works()
    {
        var spec = HotkeyParser.Parse(new() { "Ctrl", "Alt", "Shift", "Win" }, "F1");
        Assert.Equal(
            HotkeyModifier.Control | HotkeyModifier.Alt | HotkeyModifier.Shift | HotkeyModifier.Win,
            spec.Modifiers);
    }

    [Fact]
    public void Parse_UnknownModifier_Throws()
    {
        Assert.Throws<FormatException>(() => HotkeyParser.Parse(new() { "Hyper" }, "A"));
    }

    [Fact]
    public void Parse_UnknownKey_Throws()
    {
        Assert.Throws<FormatException>(() => HotkeyParser.Parse(new() { "Ctrl" }, "NotAKey"));
    }

    [Fact]
    public void Format_RoundtripsToReadableString()
    {
        var spec = new HotkeySpec(
            HotkeyModifier.Control | HotkeyModifier.Alt,
            Keys.F16);
        Assert.Equal("Ctrl + Alt + F16", HotkeyParser.Format(spec));
    }

    [Fact]
    public void Format_NoModifier_OnlyKey()
    {
        var spec = new HotkeySpec(HotkeyModifier.None, Keys.F16);
        Assert.Equal("F16", HotkeyParser.Format(spec));
    }

    [Fact]
    public void ToConfigEntry_RoundtripsThroughEntry()
    {
        var spec = new HotkeySpec(HotkeyModifier.Control | HotkeyModifier.Win, Keys.F13);
        var entry = HotkeyParser.ToConfigEntry(spec);
        var roundtripped = HotkeyParser.Parse(entry.Modifiers, entry.Key);
        Assert.Equal(spec, roundtripped);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~HotkeyParserTests"`
Expected: Build error — types not defined.

- [ ] **Step 3: Define HotkeySpec value type**

Create `src/AudioCarousel/Hotkey/HotkeySpec.cs`:
```csharp
using System.Windows.Forms;

namespace AudioCarousel.Hotkey;

[Flags]
public enum HotkeyModifier
{
    None    = 0,
    Alt     = 0x0001,
    Control = 0x0002,
    Shift   = 0x0004,
    Win     = 0x0008,
}

public readonly record struct HotkeySpec(HotkeyModifier Modifiers, Keys Key);
```

These flag values match `MOD_*` constants used by `RegisterHotKey`.

- [ ] **Step 4: Implement HotkeyParser**

Create `src/AudioCarousel/Hotkey/HotkeyParser.cs`:
```csharp
using System.Windows.Forms;
using AudioCarousel.Config;

namespace AudioCarousel.Hotkey;

public static class HotkeyParser
{
    public static HotkeySpec Parse(List<string> modifiers, string key)
    {
        var mod = HotkeyModifier.None;
        foreach (string m in modifiers)
        {
            mod |= m switch
            {
                "Ctrl"  => HotkeyModifier.Control,
                "Alt"   => HotkeyModifier.Alt,
                "Shift" => HotkeyModifier.Shift,
                "Win"   => HotkeyModifier.Win,
                _ => throw new FormatException($"Unknown modifier: {m}"),
            };
        }

        if (!Enum.TryParse<Keys>(key, ignoreCase: false, out var parsedKey))
            throw new FormatException($"Unknown key: {key}");

        return new HotkeySpec(mod, parsedKey);
    }

    public static string Format(HotkeySpec spec)
    {
        var parts = new List<string>(5);
        if (spec.Modifiers.HasFlag(HotkeyModifier.Control)) parts.Add("Ctrl");
        if (spec.Modifiers.HasFlag(HotkeyModifier.Alt))     parts.Add("Alt");
        if (spec.Modifiers.HasFlag(HotkeyModifier.Shift))   parts.Add("Shift");
        if (spec.Modifiers.HasFlag(HotkeyModifier.Win))     parts.Add("Win");
        parts.Add(spec.Key.ToString());
        return string.Join(" + ", parts);
    }

    public static HotkeyEntry ToConfigEntry(HotkeySpec spec)
    {
        var mods = new List<string>(4);
        if (spec.Modifiers.HasFlag(HotkeyModifier.Control)) mods.Add("Ctrl");
        if (spec.Modifiers.HasFlag(HotkeyModifier.Alt))     mods.Add("Alt");
        if (spec.Modifiers.HasFlag(HotkeyModifier.Shift))   mods.Add("Shift");
        if (spec.Modifiers.HasFlag(HotkeyModifier.Win))     mods.Add("Win");
        return new HotkeyEntry { Modifiers = mods, Key = spec.Key.ToString() };
    }

    public static HotkeySpec? FromConfigEntry(HotkeyEntry? entry)
    {
        if (entry is null) return null;
        return Parse(entry.Modifiers, entry.Key);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~HotkeyParserTests"`
Expected: 8 tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/AudioCarousel/Hotkey/ tests/AudioCarousel.Tests/HotkeyParserTests.cs
git commit -m "feat(hotkey): add HotkeySpec and parser/formatter"
```

---

## Task 5: I18n Strings Table

**Files:**
- Create: `src/AudioCarousel/I18n/Language.cs`
- Create: `src/AudioCarousel/I18n/Strings.cs`
- Create: `tests/AudioCarousel.Tests/StringsTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/AudioCarousel.Tests/StringsTests.cs`:
```csharp
using AudioCarousel.I18n;
using Xunit;

namespace AudioCarousel.Tests;

public class StringsTests
{
    [Fact]
    public void Get_DefaultsToEnglish()
    {
        Strings.SetLanguage(Language.English);
        Assert.Equal("Cycle next", Strings.Get("tray.cycleNext"));
    }

    [Fact]
    public void Get_JapaneseAfterSet()
    {
        Strings.SetLanguage(Language.Japanese);
        Assert.Equal("次のデバイスへ", Strings.Get("tray.cycleNext"));
        Strings.SetLanguage(Language.English); // reset
    }

    [Fact]
    public void Get_UnknownKey_ReturnsKeyAsIs()
    {
        Strings.SetLanguage(Language.English);
        Assert.Equal("nonexistent.key", Strings.Get("nonexistent.key"));
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("en")]
    [InlineData("ja")]
    public void ResolveLanguage_AcceptsValidConfigValues(string value)
    {
        var lang = Strings.ResolveLanguage(value, currentUiCultureIsJapanese: true);
        Assert.True(lang == Language.English || lang == Language.Japanese);
    }

    [Fact]
    public void ResolveLanguage_AutoOnJapaneseSystem_ReturnsJapanese()
    {
        Assert.Equal(Language.Japanese,
            Strings.ResolveLanguage("auto", currentUiCultureIsJapanese: true));
    }

    [Fact]
    public void ResolveLanguage_AutoOnNonJapaneseSystem_ReturnsEnglish()
    {
        Assert.Equal(Language.English,
            Strings.ResolveLanguage("auto", currentUiCultureIsJapanese: false));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~StringsTests"`
Expected: Build error — types not defined.

- [ ] **Step 3: Define Language enum**

Create `src/AudioCarousel/I18n/Language.cs`:
```csharp
namespace AudioCarousel.I18n;

public enum Language
{
    English,
    Japanese,
}
```

- [ ] **Step 4: Implement Strings**

Create `src/AudioCarousel/I18n/Strings.cs`:
```csharp
namespace AudioCarousel.I18n;

public static class Strings
{
    private static Language _current = Language.English;

    private static readonly Dictionary<string, (string en, string ja)> Table = new()
    {
        ["app.title"]                 = ("Audio Carousel",                        "Audio Carousel"),
        ["tray.title"]                = ("Audio Carousel",                        "Audio Carousel"),
        ["tray.currentPrefix"]        = ("Current: ",                             "現在: "),
        ["tray.currentNone"]          = ("(no device selected)",                  "(デバイス未選択)"),
        ["tray.cycleNext"]            = ("Cycle next",                            "次のデバイスへ"),
        ["tray.settings"]             = ("Settings...",                           "設定..."),
        ["tray.startWithWindows"]     = ("Start with Windows",                    "Windows起動時に開始"),
        ["tray.about"]                = ("About",                                 "バージョン情報"),
        ["tray.exit"]                 = ("Exit",                                  "終了"),

        ["settings.title"]            = ("Audio Carousel — Settings",             "Audio Carousel — 設定"),
        ["settings.titleFirstRun"]    = ("Audio Carousel — Settings (First-time setup)", "Audio Carousel — 設定 (初回セットアップ)"),
        ["settings.hotkey"]           = ("Hotkey:",                               "ホットキー:"),
        ["settings.hotkeyHint"]       = ("(Click and press a key combination)",   "(クリックしてキーを押してください)"),
        ["settings.hotkeyClear"]      = ("Clear",                                 "クリア"),
        ["settings.hotkeyEmpty"]      = ("(none)",                                "(未設定)"),
        ["settings.cycleDevices"]     = ("Cycle devices (in order):",             "切替デバイス (順序):"),
        ["settings.addDevice"]        = ("Add device",                            "デバイス追加"),
        ["settings.remove"]           = ("Remove",                                "削除"),
        ["settings.moveUp"]           = ("Up",                                    "上へ"),
        ["settings.moveDown"]         = ("Down",                                  "下へ"),
        ["settings.language"]         = ("Language:",                             "言語:"),
        ["settings.languageAuto"]     = ("Auto",                                  "自動"),
        ["settings.languageEn"]       = ("English",                               "English"),
        ["settings.languageJa"]       = ("日本語",                                "日本語"),
        ["settings.startWithWindows"] = ("Start with Windows",                    "Windows起動時に開始"),
        ["settings.ok"]               = ("OK",                                    "OK"),
        ["settings.cancel"]           = ("Cancel",                                "キャンセル"),
        ["settings.offline"]          = ("(offline)",                             "(未接続)"),
        ["settings.noNewDevices"]     = ("(no new devices available)",            "(追加可能なデバイスがありません)"),

        ["error.alreadyRunning"]      = ("Audio Carousel is already running.",    "Audio Carouselはすでに起動しています。"),
        ["error.hotkeyInUse"]         = ("Hotkey already in use by another application.", "このホットキーは他のアプリに使用されています。"),
        ["error.switchFailed"]        = ("Failed to switch device",               "デバイス切替に失敗しました"),
        ["error.noDeviceAvailable"]   = ("No registered audio device available",  "切替可能なデバイスがありません"),
        ["error.configCorrupted"]     = ("Configuration file was corrupted. A backup was saved as audio-carousel.json.bak and defaults are now in use.", "設定ファイルが破損していました。audio-carousel.json.bakにバックアップを保存し、デフォルト設定で起動します。"),
        ["error.unhandled"]           = ("An unexpected error occurred:",         "予期しないエラーが発生しました:"),

        ["about.body"]                = ("Audio Carousel — switch the default audio output device with a global hotkey.\n\nhttps://github.com/<owner>/audio-carousel", "Audio Carousel — グローバルホットキーで音声出力デバイスを切り替えます。\n\nhttps://github.com/<owner>/audio-carousel"),
    };

    public static void SetLanguage(Language lang) => _current = lang;
    public static Language Current => _current;

    public static string Get(string key)
    {
        if (!Table.TryGetValue(key, out var entry)) return key;
        return _current == Language.Japanese ? entry.ja : entry.en;
    }

    public static Language ResolveLanguage(string configValue, bool currentUiCultureIsJapanese)
    {
        return configValue switch
        {
            "ja" => Language.Japanese,
            "en" => Language.English,
            _    => currentUiCultureIsJapanese ? Language.Japanese : Language.English,
        };
    }

    public static bool IsCurrentUiCultureJapanese() =>
        System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja";
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~StringsTests"`
Expected: 6 tests pass (one Theory expands to 3, plus 3 explicit Facts = 6).

- [ ] **Step 6: Commit**

```bash
git add src/AudioCarousel/I18n/ tests/AudioCarousel.Tests/StringsTests.cs
git commit -m "feat(i18n): add English/Japanese string table"
```

---

## Task 6: AudioDevice & IAudioDeviceService Interface

**Files:**
- Create: `src/AudioCarousel/Audio/AudioDevice.cs`
- Create: `src/AudioCarousel/Audio/IAudioDeviceService.cs`
- Create: `tests/AudioCarousel.Tests/Fakes/FakeAudioDeviceService.cs`

- [ ] **Step 1: Define AudioDevice value type**

Create `src/AudioCarousel/Audio/AudioDevice.cs`:
```csharp
namespace AudioCarousel.Audio;

public readonly record struct AudioDevice(string EndpointId, string DisplayName);

public enum AudioRole
{
    Console = 0,
    Multimedia = 1,
    Communications = 2,
}
```

The enum integer values intentionally match the Win32 `ERole` constants used by `IPolicyConfig`.

- [ ] **Step 2: Define interface**

Create `src/AudioCarousel/Audio/IAudioDeviceService.cs`:
```csharp
namespace AudioCarousel.Audio;

public interface IAudioDeviceService
{
    IReadOnlyList<AudioDevice> EnumerateActiveOutputs();
    string? GetDefaultOutputId(AudioRole role);
    void SetDefault(string endpointId, AudioRole role);
}
```

- [ ] **Step 3: Create test fake**

Create `tests/AudioCarousel.Tests/Fakes/FakeAudioDeviceService.cs`:
```csharp
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
```

- [ ] **Step 4: Verify build**

Run: `dotnet build`
Expected: Build succeeds, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/AudioCarousel/Audio/AudioDevice.cs src/AudioCarousel/Audio/IAudioDeviceService.cs tests/AudioCarousel.Tests/Fakes/
git commit -m "feat(audio): add AudioDevice value type and IAudioDeviceService interface"
```

---

## Task 7: PolicyConfig COM Interface

**Files:**
- Create: `src/AudioCarousel/Audio/PolicyConfig.cs`

- [ ] **Step 1: Declare IPolicyConfig and helper class**

Create `src/AudioCarousel/Audio/PolicyConfig.cs`:
```csharp
using System.Runtime.InteropServices;

namespace AudioCarousel.Audio;

// IPolicyConfig is undocumented but stable from Windows Vista through Windows 11.
// CLSID and IID values are well-known.
[Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPolicyConfig
{
    [PreserveSig] int GetMixFormat();
    [PreserveSig] int GetDeviceFormat();
    [PreserveSig] int ResetDeviceFormat();
    [PreserveSig] int SetDeviceFormat();
    [PreserveSig] int GetProcessingPeriod();
    [PreserveSig] int SetProcessingPeriod();
    [PreserveSig] int GetShareMode();
    [PreserveSig] int SetShareMode();
    [PreserveSig] int GetPropertyValue();
    [PreserveSig] int SetPropertyValue();

    [PreserveSig]
    int SetDefaultEndpoint(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
        uint role);

    [PreserveSig] int SetEndpointVisibility();
}

[ComImport]
[Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
internal class PolicyConfigClient
{
}

internal static class PolicyConfig
{
    public static void SetDefaultEndpoint(string deviceId, uint role)
    {
        var client = (IPolicyConfig)new PolicyConfigClient();
        try
        {
            int hr = client.SetDefaultEndpoint(deviceId, role);
            if (hr < 0) Marshal.ThrowExceptionForHR(hr);
        }
        finally
        {
            Marshal.ReleaseComObject(client);
        }
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeds. AOT may emit a warning about COM interop trimming — note it for later but proceed.

- [ ] **Step 3: Commit**

```bash
git add src/AudioCarousel/Audio/PolicyConfig.cs
git commit -m "feat(audio): add IPolicyConfig COM declaration"
```

---

## Task 8: AudioDeviceService Implementation

**Files:**
- Create: `src/AudioCarousel/Audio/AudioDeviceService.cs`

- [ ] **Step 1: Implement service using NAudio + PolicyConfig**

Create `src/AudioCarousel/Audio/AudioDeviceService.cs`:
```csharp
using NAudio.CoreAudioApi;

namespace AudioCarousel.Audio;

public sealed class AudioDeviceService : IAudioDeviceService
{
    public IReadOnlyList<AudioDevice> EnumerateActiveOutputs()
    {
        using var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        var result = new List<AudioDevice>(devices.Count);
        foreach (var d in devices)
        {
            try
            {
                result.Add(new AudioDevice(d.ID, d.FriendlyName));
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
            using var d = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, ToNAudioRole(role));
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
        AudioRole.Console        => Role.Console,
        AudioRole.Multimedia     => Role.Multimedia,
        AudioRole.Communications => Role.Communications,
        _ => Role.Multimedia,
    };
}
```

- [ ] **Step 2: Manual smoke test (write a temporary console-style verification)**

Add a temporary `Verify()` method to test by opening a development scratch session. From `Program.cs`, replace the body with:
```csharp
using AudioCarousel.Audio;

namespace AudioCarousel;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var svc = new AudioDeviceService();
        Console.WriteLine("=== Active output devices ===");
        foreach (var d in svc.EnumerateActiveOutputs())
            Console.WriteLine($"  {d.EndpointId}  {d.DisplayName}");

        Console.WriteLine($"Current Multimedia default: {svc.GetDefaultOutputId(AudioRole.Multimedia)}");
        Console.ReadLine();
    }
}
```

Run: `dotnet run --project src/AudioCarousel`

Expected: Console window opens listing your active output devices and the current default. Press Enter to exit. **Do not commit this Program.cs body** — it will be replaced in Task 16.

- [ ] **Step 3: Revert Program.cs to placeholder**

Restore `src/AudioCarousel/Program.cs`:
```csharp
namespace AudioCarousel;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
    }
}
```

- [ ] **Step 4: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add src/AudioCarousel/Audio/AudioDeviceService.cs src/AudioCarousel/Program.cs
git commit -m "feat(audio): implement AudioDeviceService using NAudio and PolicyConfig"
```

---

## Task 9: CycleController

**Files:**
- Create: `src/AudioCarousel/Cycle/ICycleSink.cs`
- Create: `src/AudioCarousel/Cycle/CycleController.cs`
- Create: `tests/AudioCarousel.Tests/CycleControllerTests.cs`

- [ ] **Step 1: Define ICycleSink**

Create `src/AudioCarousel/Cycle/ICycleSink.cs`:
```csharp
namespace AudioCarousel.Cycle;

public interface ICycleSink
{
    void ShowToast(string text);
    void ShowErrorToast(string text);
    void NotifyCurrentDeviceChanged();
}
```

- [ ] **Step 2: Write failing tests**

Create `tests/AudioCarousel.Tests/CycleControllerTests.cs`:
```csharp
using AudioCarousel.Audio;
using AudioCarousel.Config;
using AudioCarousel.Cycle;
using AudioCarousel.I18n;
using AudioCarousel.Tests.Fakes;
using Xunit;

namespace AudioCarousel.Tests;

public class CycleControllerTests
{
    private static (CycleController c, FakeAudioDeviceService a, FakeCycleSink s, ConfigSchema cfg, Action saved)
        Build(params (string id, string name, bool active)[] devices)
    {
        Strings.SetLanguage(Language.English);
        var audio = new FakeAudioDeviceService();
        var cfg = new ConfigSchema();
        foreach (var (id, name, active) in devices)
        {
            cfg.Devices.Add(new DeviceEntry { EndpointId = id, DisplayName = name });
            if (active) audio.ActiveOutputs.Add(new AudioDevice(id, name));
        }
        var sink = new FakeCycleSink();
        int savedCount = 0;
        Action saveCallback = () => savedCount++;
        var controller = new CycleController(cfg, audio, sink, saveCallback);
        return (controller, audio, sink, cfg, () => { });
    }

    [Fact]
    public void Cycle_EmptyDevices_DoesNothing()
    {
        var (c, a, s, _, _) = Build();
        c.Cycle();
        Assert.Empty(a.SetCalls);
        Assert.Empty(s.Toasts);
        Assert.Empty(s.ErrorToasts);
    }

    [Fact]
    public void Cycle_SingleDevice_StaysOnSame()
    {
        var (c, a, s, cfg, _) = Build(("a", "A", true));
        c.Cycle();
        Assert.Equal("a", cfg.Devices[cfg.CurrentIndex].EndpointId);
        // 3 roles × 1 device
        Assert.Equal(3, a.SetCalls.Count);
        Assert.Single(s.Toasts);
        Assert.Equal("A", s.Toasts[0]);
    }

    [Fact]
    public void Cycle_TwoDevices_AdvancesAndWraps()
    {
        var (c, _, s, cfg, _) = Build(("a", "A", true), ("b", "B", true));
        cfg.CurrentIndex = 0;
        c.Cycle();
        Assert.Equal(1, cfg.CurrentIndex);
        Assert.Equal("B", s.Toasts[^1]);
        c.Cycle();
        Assert.Equal(0, cfg.CurrentIndex);
        Assert.Equal("A", s.Toasts[^1]);
    }

    [Fact]
    public void Cycle_SkipsOfflineDevices()
    {
        var (c, _, s, cfg, _) = Build(
            ("a", "A", true),
            ("b", "B", false),
            ("c", "C", true));
        cfg.CurrentIndex = 0;
        c.Cycle();
        Assert.Equal(2, cfg.CurrentIndex); // skipped b
        Assert.Equal("C", s.Toasts[^1]);
    }

    [Fact]
    public void Cycle_AllOffline_ShowsErrorToast()
    {
        var (c, a, s, cfg, _) = Build(("a", "A", false), ("b", "B", false));
        c.Cycle();
        Assert.Empty(a.SetCalls);
        Assert.Single(s.ErrorToasts);
        Assert.Equal(Strings.Get("error.noDeviceAvailable"), s.ErrorToasts[0]);
    }

    [Fact]
    public void Cycle_SyncsCurrentIndexWithOsDefault()
    {
        var (c, a, s, cfg, _) = Build(
            ("a", "A", true),
            ("b", "B", true),
            ("c", "C", true));
        cfg.CurrentIndex = 0;
        // OS reports default as "b"
        a.Defaults[AudioRole.Multimedia] = "b";

        c.Cycle();
        // Should sync to b (index 1) and advance to c (index 2)
        Assert.Equal(2, cfg.CurrentIndex);
        Assert.Equal("C", s.Toasts[^1]);
    }

    [Fact]
    public void Cycle_SetDefaultThrows_DoesNotAdvanceIndex()
    {
        var (c, a, s, cfg, _) = Build(("a", "A", true), ("b", "B", true));
        cfg.CurrentIndex = 0;
        a.SetDefaultException = (id, role) => new InvalidOperationException("boom");

        c.Cycle();

        Assert.Equal(0, cfg.CurrentIndex);
        Assert.Single(s.ErrorToasts);
    }
}

internal sealed class FakeCycleSink : ICycleSink
{
    public List<string> Toasts { get; } = new();
    public List<string> ErrorToasts { get; } = new();
    public int CurrentDeviceChangedNotifications { get; private set; }
    public void ShowToast(string text) => Toasts.Add(text);
    public void ShowErrorToast(string text) => ErrorToasts.Add(text);
    public void NotifyCurrentDeviceChanged() => CurrentDeviceChangedNotifications++;
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~CycleControllerTests"`
Expected: Build error — `CycleController` not defined.

- [ ] **Step 4: Implement CycleController**

Create `src/AudioCarousel/Cycle/CycleController.cs`:
```csharp
using AudioCarousel.Audio;
using AudioCarousel.Config;
using AudioCarousel.I18n;

namespace AudioCarousel.Cycle;

public sealed class CycleController
{
    private static readonly AudioRole[] AllRoles =
        { AudioRole.Multimedia, AudioRole.Communications, AudioRole.Console };

    private readonly ConfigSchema _config;
    private readonly IAudioDeviceService _audio;
    private readonly ICycleSink _sink;
    private readonly Action _persistCurrentIndex;

    public CycleController(
        ConfigSchema config,
        IAudioDeviceService audio,
        ICycleSink sink,
        Action persistCurrentIndex)
    {
        _config = config;
        _audio = audio;
        _sink = sink;
        _persistCurrentIndex = persistCurrentIndex;
    }

    public void Cycle()
    {
        if (_config.Devices.Count == 0) return;

        var available = new HashSet<string>(
            _audio.EnumerateActiveOutputs().Select(d => d.EndpointId),
            StringComparer.Ordinal);

        // Sync currentIndex with OS reality before advancing.
        string? currentDefault = _audio.GetDefaultOutputId(AudioRole.Multimedia);
        if (currentDefault is not null)
        {
            int syncIndex = _config.Devices.FindIndex(d => d.EndpointId == currentDefault);
            if (syncIndex >= 0) _config.CurrentIndex = syncIndex;
        }

        int count = _config.Devices.Count;
        int startIndex = (_config.CurrentIndex + 1) % count;
        int targetIndex = -1;
        for (int offset = 0; offset < count; offset++)
        {
            int i = (startIndex + offset) % count;
            if (available.Contains(_config.Devices[i].EndpointId))
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex < 0)
        {
            _sink.ShowErrorToast(Strings.Get("error.noDeviceAvailable"));
            return;
        }

        var target = _config.Devices[targetIndex];

        try
        {
            foreach (var role in AllRoles)
                _audio.SetDefault(target.EndpointId, role);
        }
        catch (Exception)
        {
            _sink.ShowErrorToast(Strings.Get("error.switchFailed"));
            return;
        }

        _config.CurrentIndex = targetIndex;
        _persistCurrentIndex();
        _sink.ShowToast(target.DisplayName);
        _sink.NotifyCurrentDeviceChanged();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~CycleControllerTests"`
Expected: 7 tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/AudioCarousel/Cycle/ tests/AudioCarousel.Tests/CycleControllerTests.cs
git commit -m "feat(cycle): add CycleController with offline-skip and default sync"
```

---

## Task 10: HotkeyHost (NativeWindow + RegisterHotKey)

**Files:**
- Create: `src/AudioCarousel/Hotkey/HotkeyHost.cs`

- [ ] **Step 1: Implement HotkeyHost**

Create `src/AudioCarousel/Hotkey/HotkeyHost.cs`:
```csharp
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AudioCarousel.Hotkey;

public sealed class HotkeyHost : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 1;

    private readonly MessageOnlyWindow _window;
    private bool _registered;
    private Action? _onHotkey;

    public HotkeyHost()
    {
        _window = new MessageOnlyWindow(OnMessage);
    }

    public bool TryRegister(HotkeySpec spec, Action onHotkey)
    {
        Unregister();
        _onHotkey = onHotkey;
        bool ok = RegisterHotKey(_window.Handle, HOTKEY_ID, (uint)spec.Modifiers, (uint)spec.Key);
        _registered = ok;
        return ok;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(_window.Handle, HOTKEY_ID);
            _registered = false;
        }
        _onHotkey = null;
    }

    private void OnMessage(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            _onHotkey?.Invoke();
    }

    public void Dispose()
    {
        Unregister();
        _window.DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private sealed class MessageOnlyWindow : NativeWindow
    {
        public delegate void MessageHandler(ref Message m);
        private readonly MessageHandler _handler;

        public MessageOnlyWindow(MessageHandler handler)
        {
            _handler = handler;
            CreateHandle(new CreateParams { Caption = "AudioCarousel.Hotkey", Parent = (IntPtr)(-3) });
        }

        protected override void WndProc(ref Message m)
        {
            _handler(ref m);
            base.WndProc(ref m);
        }
    }
}
```

The parent handle `(IntPtr)(-3)` is `HWND_MESSAGE`, creating a message-only window invisible to the user.

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/AudioCarousel/Hotkey/HotkeyHost.cs
git commit -m "feat(hotkey): add HotkeyHost message-only window"
```

---

## Task 11: ToastWindow

**Files:**
- Create: `src/AudioCarousel/UI/ToastWindow.cs`

- [ ] **Step 1: Implement ToastWindow**

Create `src/AudioCarousel/UI/ToastWindow.cs`:
```csharp
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AudioCarousel.UI;

public sealed class ToastWindow : Form
{
    private const int FadeMs = 200;
    private const int HoldMs = 1500;
    private const int Margin = 16;
    private const int PadX = 24;
    private const int PadY = 14;
    private const int MaxWidth = 600;

    private readonly System.Windows.Forms.Timer _holdTimer;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private string _text = "";
    private FadeState _state = FadeState.Hidden;
    private bool _isError;

    private enum FadeState { Hidden, FadingIn, Holding, FadingOut }

    public ToastWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        Opacity = 0;
        BackColor = Color.FromArgb(28, 28, 30);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Point);

        _holdTimer = new System.Windows.Forms.Timer { Interval = HoldMs };
        _holdTimer.Tick += (_, _) => { _holdTimer.Stop(); StartFadeOut(); };

        _fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _fadeTimer.Tick += FadeTick;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x08000000 /* WS_EX_NOACTIVATE */ | 0x00000080 /* WS_EX_TOOLWINDOW */;
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    public void ShowMessage(string text, bool isError = false)
    {
        _text = text;
        _isError = isError;
        BackColor = isError ? Color.FromArgb(120, 28, 28) : Color.FromArgb(28, 28, 30);
        AdjustSize();
        PositionOnActiveMonitor();

        if (_state == FadeState.Hidden)
        {
            Opacity = 0;
            Show();
            _state = FadeState.FadingIn;
            _fadeTimer.Start();
        }
        else
        {
            // Already on screen — replace text and reset hold.
            _holdTimer.Stop();
            _state = FadeState.Holding;
            Opacity = 1;
            _holdTimer.Start();
            Invalidate();
        }
    }

    private void AdjustSize()
    {
        using var g = CreateGraphics();
        var size = g.MeasureString(_text, Font);
        int width = Math.Min(MaxWidth, (int)Math.Ceiling(size.Width) + PadX * 2);
        int height = (int)Math.Ceiling(size.Height) + PadY * 2;
        Size = new Size(width, height);
    }

    private void PositionOnActiveMonitor()
    {
        var screen = Screen.FromPoint(Cursor.Position);
        var work = screen.WorkingArea;
        Location = new Point(
            work.Right - Width - Margin,
            work.Bottom - Height - Margin);
    }

    private void StartFadeOut()
    {
        _state = FadeState.FadingOut;
        _fadeTimer.Start();
    }

    private void FadeTick(object? sender, EventArgs e)
    {
        double step = 16.0 / FadeMs;
        switch (_state)
        {
            case FadeState.FadingIn:
                Opacity = Math.Min(1, Opacity + step);
                if (Opacity >= 1)
                {
                    _fadeTimer.Stop();
                    _state = FadeState.Holding;
                    _holdTimer.Start();
                }
                break;
            case FadeState.FadingOut:
                Opacity = Math.Max(0, Opacity - step);
                if (Opacity <= 0)
                {
                    _fadeTimer.Stop();
                    _state = FadeState.Hidden;
                    Hide();
                }
                break;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 8);
        using var bg = new SolidBrush(BackColor);
        g.FillPath(bg, path);

        var rect = new Rectangle(PadX, PadY, Width - PadX * 2, Height - PadY * 2);
        TextRenderer.DrawText(g, _text, Font, rect, ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _holdTimer.Dispose();
            _fadeTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/AudioCarousel/UI/ToastWindow.cs
git commit -m "feat(ui): add ToastWindow with fade in/out and replace-on-show"
```

---

## Task 12: StartupRegistration

**Files:**
- Create: `src/AudioCarousel/Startup/StartupRegistration.cs`
- Create: `tests/AudioCarousel.Tests/StartupRegistrationTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/AudioCarousel.Tests/StartupRegistrationTests.cs`:
```csharp
using AudioCarousel.Startup;
using Microsoft.Win32;
using Xunit;

namespace AudioCarousel.Tests;

public class StartupRegistrationTests : IDisposable
{
    private readonly string _testValueName;

    public StartupRegistrationTests()
    {
        // Use a unique value name so tests don't collide with the real one.
        _testValueName = "AudioCarousel-TEST-" + Guid.NewGuid().ToString("N");
    }

    public void Dispose()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        key?.DeleteValue(_testValueName, throwOnMissingValue: false);
    }

    [Fact]
    public void Enable_WritesExePathToRunKey()
    {
        var reg = new StartupRegistration(_testValueName);
        reg.Enable(@"C:\path\to\exe.exe");

        Assert.True(reg.IsEnabled());
        Assert.Equal(@"C:\path\to\exe.exe", reg.GetRegisteredPath());
    }

    [Fact]
    public void Disable_RemovesValue()
    {
        var reg = new StartupRegistration(_testValueName);
        reg.Enable(@"C:\path\to\exe.exe");
        reg.Disable();

        Assert.False(reg.IsEnabled());
        Assert.Null(reg.GetRegisteredPath());
    }

    [Fact]
    public void EnsurePath_FixesDriftedPath()
    {
        var reg = new StartupRegistration(_testValueName);
        reg.Enable(@"C:\old\path.exe");
        reg.EnsurePath(@"C:\new\path.exe");

        Assert.Equal(@"C:\new\path.exe", reg.GetRegisteredPath());
    }

    [Fact]
    public void EnsurePath_NoOpWhenDisabled()
    {
        var reg = new StartupRegistration(_testValueName);
        reg.EnsurePath(@"C:\anything.exe");
        Assert.False(reg.IsEnabled());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~StartupRegistrationTests"`
Expected: Build error — `StartupRegistration` not defined.

- [ ] **Step 3: Implement StartupRegistration**

Create `src/AudioCarousel/Startup/StartupRegistration.cs`:
```csharp
using Microsoft.Win32;

namespace AudioCarousel.Startup;

public sealed class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly string _valueName;

    public StartupRegistration(string valueName = "AudioCarousel")
    {
        _valueName = valueName;
    }

    public bool IsEnabled() => GetRegisteredPath() is not null;

    public string? GetRegisteredPath()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(_valueName) as string;
    }

    public void Enable(string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        key.SetValue(_valueName, exePath, RegistryValueKind.String);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(_valueName, throwOnMissingValue: false);
    }

    public void EnsurePath(string currentExePath)
    {
        string? registered = GetRegisteredPath();
        if (registered is null) return; // not enabled — nothing to fix
        if (!string.Equals(registered, currentExePath, StringComparison.OrdinalIgnoreCase))
            Enable(currentExePath);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~StartupRegistrationTests"`
Expected: 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/AudioCarousel/Startup/ tests/AudioCarousel.Tests/StartupRegistrationTests.cs
git commit -m "feat(startup): add HKCU Run-key registration with path-drift fix"
```

---

## Task 13: HotkeyTextBox Custom Control

**Files:**
- Create: `src/AudioCarousel/UI/HotkeyTextBox.cs`

- [ ] **Step 1: Implement HotkeyTextBox**

Create `src/AudioCarousel/UI/HotkeyTextBox.cs`:
```csharp
using System.Windows.Forms;
using AudioCarousel.Hotkey;
using AudioCarousel.I18n;

namespace AudioCarousel.UI;

public sealed class HotkeyTextBox : TextBox
{
    private HotkeySpec? _spec;
    private bool _capturing;

    public HotkeyTextBox()
    {
        ReadOnly = true;
        Cursor = Cursors.Default;
        TabStop = true;
        Render();
    }

    public HotkeySpec? Value
    {
        get => _spec;
        set { _spec = value; Render(); }
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _capturing = true;
        Text = "Press a key combination... (Esc cancels)";
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _capturing = false;
        Render();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (!_capturing) return base.ProcessCmdKey(ref msg, keyData);

        var key = keyData & Keys.KeyCode;
        if (key == Keys.None || key == Keys.Escape)
        {
            _capturing = false;
            Render();
            FindForm()?.SelectNextControl(this, true, true, true, true);
            return true;
        }
        if (IsModifierOnly(key)) return true; // wait for non-modifier key

        var mods = HotkeyModifier.None;
        if ((keyData & Keys.Control) != 0) mods |= HotkeyModifier.Control;
        if ((keyData & Keys.Alt)     != 0) mods |= HotkeyModifier.Alt;
        if ((keyData & Keys.Shift)   != 0) mods |= HotkeyModifier.Shift;
        // Win key isn't reported via keyData — handled by raw WM if needed; skip for v1.

        _spec = new HotkeySpec(mods, key);
        _capturing = false;
        Render();
        FindForm()?.SelectNextControl(this, true, true, true, true);
        return true;
    }

    private static bool IsModifierOnly(Keys key) =>
        key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu
            or Keys.LWin or Keys.RWin;

    public void Render()
    {
        Text = _spec is null
            ? Strings.Get("settings.hotkeyEmpty")
            : HotkeyParser.Format(_spec.Value);
    }
}
```

Note: Win-key chord support is intentionally limited in v1. Users who want `Win + X` can use the modifier checkboxes-free form (most use F13–F24 alone per spec section 7).

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/AudioCarousel/UI/HotkeyTextBox.cs
git commit -m "feat(ui): add HotkeyTextBox capture control"
```

---

## Task 14: TrayIcon

**Files:**
- Create: `src/AudioCarousel/UI/TrayIcon.cs`

- [ ] **Step 1: Implement TrayIcon**

Create `src/AudioCarousel/UI/TrayIcon.cs`:
```csharp
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using AudioCarousel.I18n;

namespace AudioCarousel.UI;

public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _titleItem;
    private readonly ToolStripMenuItem _currentItem;
    private readonly ToolStripMenuItem _cycleItem;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _aboutItem;
    private readonly ToolStripMenuItem _exitItem;

    public event Action? CycleRequested;
    public event Action? SettingsRequested;
    public event Action<bool>? StartupToggled;
    public event Action? AboutRequested;
    public event Action? ExitRequested;

    public TrayIcon()
    {
        _menu = new ContextMenuStrip();
        _titleItem    = new ToolStripMenuItem { Enabled = false };
        _currentItem  = new ToolStripMenuItem { Enabled = false };
        _cycleItem    = new ToolStripMenuItem();
        _settingsItem = new ToolStripMenuItem();
        _startupItem  = new ToolStripMenuItem { CheckOnClick = true };
        _aboutItem    = new ToolStripMenuItem();
        _exitItem     = new ToolStripMenuItem();

        _cycleItem.Click    += (_, _) => CycleRequested?.Invoke();
        _settingsItem.Click += (_, _) => SettingsRequested?.Invoke();
        _startupItem.Click  += (_, _) => StartupToggled?.Invoke(_startupItem.Checked);
        _aboutItem.Click    += (_, _) => AboutRequested?.Invoke();
        _exitItem.Click     += (_, _) => ExitRequested?.Invoke();

        _menu.Items.AddRange(new ToolStripItem[]
        {
            _titleItem,
            _currentItem,
            new ToolStripSeparator(),
            _cycleItem,
            new ToolStripSeparator(),
            _settingsItem,
            _startupItem,
            new ToolStripSeparator(),
            _aboutItem,
            _exitItem,
        });

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadEmbeddedIcon() ?? SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = _menu,
        };
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) CycleRequested?.Invoke();
        };

        ApplyLabels();
    }

    public void SetCurrentDeviceLabel(string? deviceName)
    {
        _currentItem.Text = string.IsNullOrEmpty(deviceName)
            ? Strings.Get("tray.currentPrefix") + Strings.Get("tray.currentNone")
            : Strings.Get("tray.currentPrefix") + deviceName;
        _notifyIcon.Text = deviceName is null
            ? Strings.Get("app.title")
            : $"{Strings.Get("app.title")} — {Truncate(deviceName, 50)}";
    }

    public void SetStartupChecked(bool isChecked) => _startupItem.Checked = isChecked;

    public void ApplyLabels()
    {
        _titleItem.Text    = Strings.Get("tray.title");
        _cycleItem.Text    = Strings.Get("tray.cycleNext");
        _settingsItem.Text = Strings.Get("tray.settings");
        _startupItem.Text  = Strings.Get("tray.startWithWindows");
        _aboutItem.Text    = Strings.Get("tray.about");
        _exitItem.Text     = Strings.Get("tray.exit");
    }

    private static Icon? LoadEmbeddedIcon()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("AudioCarousel.Resources.tray.ico");
        return stream is null ? null : new Icon(stream);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max - 1) + "…";

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/AudioCarousel/UI/TrayIcon.cs
git commit -m "feat(ui): add TrayIcon with context menu and event hooks"
```

---

## Task 15: SettingsForm

**Files:**
- Create: `src/AudioCarousel/UI/SettingsForm.cs`

- [ ] **Step 1: Implement SettingsForm**

Create `src/AudioCarousel/UI/SettingsForm.cs`:
```csharp
using System.Drawing;
using System.Windows.Forms;
using AudioCarousel.Audio;
using AudioCarousel.Config;
using AudioCarousel.Hotkey;
using AudioCarousel.I18n;

namespace AudioCarousel.UI;

public sealed class SettingsForm : Form
{
    private readonly IAudioDeviceService _audio;
    private readonly ConfigSchema _workingCopy;
    private readonly bool _isFirstRun;

    private readonly HotkeyTextBox _hotkeyBox;
    private readonly Button _hotkeyClearBtn;
    private readonly ListView _devicesList;
    private readonly Button _addBtn;
    private readonly Button _removeBtn;
    private readonly Button _upBtn;
    private readonly Button _downBtn;
    private readonly ComboBox _languageCombo;
    private readonly CheckBox _startupCheck;
    private readonly Button _okBtn;
    private readonly Button _cancelBtn;

    public ConfigSchema? Result { get; private set; }
    public Func<HotkeySpec, bool>? HotkeyRegistrationProbe { get; set; }

    public SettingsForm(ConfigSchema current, IAudioDeviceService audio, bool isFirstRun)
    {
        _audio = audio;
        _workingCopy = Clone(current);
        _isFirstRun = isFirstRun;

        Text = Strings.Get(isFirstRun ? "settings.titleFirstRun" : "settings.title");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 460);
        Font = new Font("Segoe UI", 9f);

        var hotkeyLabel = new Label { Text = Strings.Get("settings.hotkey"), Left = 16, Top = 18, AutoSize = true };
        _hotkeyBox = new HotkeyTextBox { Left = 100, Top = 14, Width = 320 };
        _hotkeyClearBtn = new Button { Text = Strings.Get("settings.hotkeyClear"), Left = 432, Top = 13, Width = 100 };
        var hotkeyHint = new Label { Text = Strings.Get("settings.hotkeyHint"), Left = 100, Top = 40, AutoSize = true, ForeColor = Color.Gray };

        var devicesLabel = new Label { Text = Strings.Get("settings.cycleDevices"), Left = 16, Top = 78, AutoSize = true };
        _devicesList = new ListView
        {
            Left = 16, Top = 100, Width = 528, Height = 200,
            View = View.Details, FullRowSelect = true, HideSelection = false,
            HeaderStyle = ColumnHeaderStyle.None,
            OwnerDraw = true,
        };
        _devicesList.Columns.Add("Device", 528 - 4);
        _devicesList.DrawColumnHeader += (_, e) => e.DrawDefault = true;
        _devicesList.DrawSubItem += DrawDeviceItem;
        _devicesList.DrawItem += (_, e) => { /* handled per-subitem */ };

        _addBtn    = new Button { Text = Strings.Get("settings.addDevice") + " ▾", Left = 16,  Top = 308, Width = 140 };
        _removeBtn = new Button { Text = Strings.Get("settings.remove"),           Left = 162, Top = 308, Width = 90  };
        _upBtn     = new Button { Text = Strings.Get("settings.moveUp"),           Left = 258, Top = 308, Width = 60  };
        _downBtn   = new Button { Text = Strings.Get("settings.moveDown"),         Left = 322, Top = 308, Width = 60  };

        var langLabel = new Label { Text = Strings.Get("settings.language"), Left = 16, Top = 358, AutoSize = true };
        _languageCombo = new ComboBox
        {
            Left = 100, Top = 354, Width = 160,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _languageCombo.Items.AddRange(new object[]
        {
            new LangItem("auto", Strings.Get("settings.languageAuto")),
            new LangItem("en",   Strings.Get("settings.languageEn")),
            new LangItem("ja",   Strings.Get("settings.languageJa")),
        });

        _startupCheck = new CheckBox
        {
            Text = Strings.Get("settings.startWithWindows"),
            Left = 280, Top = 357, AutoSize = true,
        };

        _okBtn     = new Button { Text = Strings.Get("settings.ok"),     Left = 360, Top = 410, Width = 90, DialogResult = DialogResult.None };
        _cancelBtn = new Button { Text = Strings.Get("settings.cancel"), Left = 454, Top = 410, Width = 90, DialogResult = DialogResult.Cancel };
        AcceptButton = _okBtn;
        CancelButton = _cancelBtn;

        Controls.AddRange(new Control[]
        {
            hotkeyLabel, _hotkeyBox, _hotkeyClearBtn, hotkeyHint,
            devicesLabel, _devicesList,
            _addBtn, _removeBtn, _upBtn, _downBtn,
            langLabel, _languageCombo, _startupCheck,
            _okBtn, _cancelBtn,
        });

        // Wire events.
        _hotkeyClearBtn.Click += (_, _) => { _hotkeyBox.Value = null; };
        _addBtn.Click += OnAddClicked;
        _removeBtn.Click += OnRemoveClicked;
        _upBtn.Click += (_, _) => Move(-1);
        _downBtn.Click += (_, _) => Move(+1);
        _okBtn.Click += OnOkClicked;

        // Load working copy into UI.
        _hotkeyBox.Value = HotkeyParser.FromConfigEntry(_workingCopy.Hotkey);
        _startupCheck.Checked = _workingCopy.StartWithWindows;
        SelectLanguageItem(_workingCopy.Language);
        RefreshDevicesList();
    }

    private void RefreshDevicesList()
    {
        _devicesList.Items.Clear();
        var available = _audio.EnumerateActiveOutputs()
            .ToDictionary(d => d.EndpointId, d => d.DisplayName, StringComparer.Ordinal);
        string? currentDefault = _audio.GetDefaultOutputId(AudioRole.Multimedia);

        foreach (var d in _workingCopy.Devices)
        {
            bool isOnline = available.ContainsKey(d.EndpointId);
            bool isCurrent = isOnline && d.EndpointId == currentDefault;
            string display = isOnline ? d.DisplayName : $"{d.DisplayName} {Strings.Get("settings.offline")}";
            var item = new ListViewItem(display)
            {
                Tag = new DeviceRow(d.EndpointId, isOnline, isCurrent),
                Font = isCurrent ? new Font(Font, FontStyle.Bold) : Font,
            };
            _devicesList.Items.Add(item);
        }
    }

    private void DrawDeviceItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Item!.Selected)
            e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);

        var row = (DeviceRow)e.Item.Tag!;
        string marker = row.IsCurrent ? "★ " : "  ";
        string statusDot = row.IsOnline ? "●" : "○";

        var brush = e.Item.Selected ? SystemBrushes.HighlightText : SystemBrushes.WindowText;
        var statusBrush = row.IsOnline ? Brushes.SeaGreen : Brushes.Gray;
        if (e.Item.Selected) statusBrush = SystemBrushes.HighlightText;

        var rect = e.Bounds;
        e.Graphics.DrawString(marker, e.Item.Font, brush, rect.Left + 4, rect.Top + 2);
        e.Graphics.DrawString(statusDot, e.Item.Font, statusBrush, rect.Left + 22, rect.Top + 2);
        e.Graphics.DrawString(e.Item.Text, e.Item.Font, brush, rect.Left + 42, rect.Top + 2);
    }

    private void OnAddClicked(object? sender, EventArgs e)
    {
        var registered = new HashSet<string>(_workingCopy.Devices.Select(d => d.EndpointId), StringComparer.Ordinal);
        var candidates = _audio.EnumerateActiveOutputs()
            .Where(d => !registered.Contains(d.EndpointId))
            .ToList();

        var menu = new ContextMenuStrip();
        if (candidates.Count == 0)
        {
            menu.Items.Add(new ToolStripMenuItem(Strings.Get("settings.noNewDevices")) { Enabled = false });
        }
        else
        {
            foreach (var d in candidates)
            {
                var item = new ToolStripMenuItem(d.DisplayName);
                item.Click += (_, _) =>
                {
                    _workingCopy.Devices.Add(new DeviceEntry
                    {
                        EndpointId = d.EndpointId,
                        DisplayName = d.DisplayName,
                        AddedAt = DateTimeOffset.Now,
                    });
                    RefreshDevicesList();
                };
                menu.Items.Add(item);
            }
        }
        menu.Show(_addBtn, new Point(0, _addBtn.Height));
    }

    private void OnRemoveClicked(object? sender, EventArgs e)
    {
        if (_devicesList.SelectedIndices.Count == 0) return;
        int idx = _devicesList.SelectedIndices[0];
        _workingCopy.Devices.RemoveAt(idx);
        if (_workingCopy.CurrentIndex >= _workingCopy.Devices.Count)
            _workingCopy.CurrentIndex = 0;
        RefreshDevicesList();
    }

    private void Move(int delta)
    {
        if (_devicesList.SelectedIndices.Count == 0) return;
        int idx = _devicesList.SelectedIndices[0];
        int target = idx + delta;
        if (target < 0 || target >= _workingCopy.Devices.Count) return;
        (_workingCopy.Devices[idx], _workingCopy.Devices[target]) =
            (_workingCopy.Devices[target], _workingCopy.Devices[idx]);
        RefreshDevicesList();
        _devicesList.Items[target].Selected = true;
        _devicesList.Items[target].Focused = true;
    }

    private void OnOkClicked(object? sender, EventArgs e)
    {
        // Validate hotkey re-registration if set.
        if (_hotkeyBox.Value is HotkeySpec spec && HotkeyRegistrationProbe is not null)
        {
            if (!HotkeyRegistrationProbe(spec))
            {
                MessageBox.Show(this, Strings.Get("error.hotkeyInUse"),
                    Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        _workingCopy.Hotkey = _hotkeyBox.Value is HotkeySpec s ? HotkeyParser.ToConfigEntry(s) : null;
        _workingCopy.StartWithWindows = _startupCheck.Checked;
        _workingCopy.Language = ((LangItem)_languageCombo.SelectedItem!).Code;

        Result = _workingCopy;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void SelectLanguageItem(string code)
    {
        for (int i = 0; i < _languageCombo.Items.Count; i++)
        {
            if (((LangItem)_languageCombo.Items[i]!).Code == code)
            {
                _languageCombo.SelectedIndex = i;
                return;
            }
        }
        _languageCombo.SelectedIndex = 0;
    }

    private static ConfigSchema Clone(ConfigSchema src) => new()
    {
        Version = src.Version,
        Language = src.Language,
        Hotkey = src.Hotkey is null ? null : new HotkeyEntry
        {
            Modifiers = new List<string>(src.Hotkey.Modifiers),
            Key = src.Hotkey.Key,
        },
        Devices = src.Devices.Select(d => new DeviceEntry
        {
            EndpointId = d.EndpointId,
            DisplayName = d.DisplayName,
            AddedAt = d.AddedAt,
        }).ToList(),
        CurrentIndex = src.CurrentIndex,
        StartWithWindows = src.StartWithWindows,
    };

    private sealed record LangItem(string Code, string Display)
    {
        public override string ToString() => Display;
    }

    private sealed record DeviceRow(string EndpointId, bool IsOnline, bool IsCurrent);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: Build succeeds. Warnings related to OwnerDraw event handlers are OK.

- [ ] **Step 3: Commit**

```bash
git add src/AudioCarousel/UI/SettingsForm.cs
git commit -m "feat(ui): add SettingsForm with device list, hotkey, language, startup"
```

---

## Task 16: Program & TrayApplicationContext (wire everything)

**Files:**
- Modify: `src/AudioCarousel/Program.cs`
- Create: `src/AudioCarousel/TrayApplicationContext.cs`

- [ ] **Step 1: Replace Program.cs**

Replace `src/AudioCarousel/Program.cs`:
```csharp
using System.Reflection;
using System.Windows.Forms;
using AudioCarousel.I18n;

namespace AudioCarousel;

internal static class Program
{
    private const string MutexName = @"Global\AudioCarousel.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            // Resolve language for the message box.
            Strings.SetLanguage(Strings.ResolveLanguage("auto", Strings.IsCurrentUiCultureJapanese()));
            MessageBox.Show(Strings.Get("error.alreadyRunning"),
                Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show($"{Strings.Get("error.unhandled")}\n\n{ex}",
                Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        Application.ThreadException += (_, args) =>
        {
            MessageBox.Show($"{Strings.Get("error.unhandled")}\n\n{args.Exception}",
                Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        using var ctx = new TrayApplicationContext();
        Application.Run(ctx);
    }
}
```

- [ ] **Step 2: Create TrayApplicationContext**

Create `src/AudioCarousel/TrayApplicationContext.cs`:
```csharp
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using AudioCarousel.Audio;
using AudioCarousel.Config;
using AudioCarousel.Cycle;
using AudioCarousel.Hotkey;
using AudioCarousel.I18n;
using AudioCarousel.Startup;
using AudioCarousel.UI;

namespace AudioCarousel;

internal sealed class TrayApplicationContext : ApplicationContext, ICycleSink
{
    private readonly string _exeDir;
    private readonly string _exePath;
    private readonly ConfigStore _store;
    private readonly ConfigSchema _config;
    private readonly bool _freshlyCreated;
    private readonly StartupRegistration _startup;
    private readonly IAudioDeviceService _audio;
    private readonly TrayIcon _tray;
    private readonly ToastWindow _toast;
    private readonly HotkeyHost _hotkeyHost;
    private readonly CycleController _cycle;

    public TrayApplicationContext()
    {
        _exePath = Process.GetCurrentProcess().MainModule!.FileName!;
        _exeDir = Path.GetDirectoryName(_exePath)!;
        string configPath = Path.Combine(_exeDir, "audio-carousel.json");

        _store = new ConfigStore(configPath);
        (_config, _freshlyCreated) = _store.Load();

        // Apply language.
        Strings.SetLanguage(Strings.ResolveLanguage(_config.Language, Strings.IsCurrentUiCultureJapanese()));

        _startup = new StartupRegistration();
        if (_config.StartWithWindows)
        {
            _startup.EnsurePath(_exePath);
        }
        _audio = new AudioDeviceService();

        _toast = new ToastWindow();
        _tray = new TrayIcon();
        _hotkeyHost = new HotkeyHost();

        _cycle = new CycleController(_config, _audio, this, PersistCurrentIndex);

        WireTrayEvents();
        ApplyHotkeyFromConfig();
        RefreshTrayCurrentLabel();
        _tray.SetStartupChecked(_config.StartWithWindows);

        if (_freshlyCreated)
        {
            // Defer SettingsForm.ShowDialog until after Application.Run starts
            // the message loop. SynchronizationContext.Current is null here in
            // the constructor, so use a one-shot UI-thread Timer.
            var firstRunTimer = new System.Windows.Forms.Timer { Interval = 1 };
            firstRunTimer.Tick += (_, _) =>
            {
                firstRunTimer.Stop();
                firstRunTimer.Dispose();
                OpenSettings(firstRun: true);
            };
            firstRunTimer.Start();
        }
    }

    private void WireTrayEvents()
    {
        _tray.CycleRequested    += () => _cycle.Cycle();
        _tray.SettingsRequested += () => OpenSettings(firstRun: false);
        _tray.AboutRequested    += ShowAbout;
        _tray.ExitRequested     += ExitApp;
        _tray.StartupToggled    += OnStartupToggled;
    }

    private void OpenSettings(bool firstRun)
    {
        using var form = new SettingsForm(_config, _audio, firstRun)
        {
            HotkeyRegistrationProbe = ProbeHotkey,
        };
        var result = form.ShowDialog();

        if (result == DialogResult.OK && form.Result is not null)
        {
            var newCfg = form.Result;
            // Copy newCfg back into _config (reference-stable for CycleController).
            _config.Hotkey = newCfg.Hotkey;
            _config.Devices = newCfg.Devices;
            _config.Language = newCfg.Language;
            _config.StartWithWindows = newCfg.StartWithWindows;
            if (_config.CurrentIndex >= _config.Devices.Count) _config.CurrentIndex = 0;

            Strings.SetLanguage(Strings.ResolveLanguage(_config.Language, Strings.IsCurrentUiCultureJapanese()));
            _tray.ApplyLabels();
            _tray.SetStartupChecked(_config.StartWithWindows);

            if (_config.StartWithWindows) _startup.Enable(_exePath);
            else _startup.Disable();

            _store.Save(_config);
        }

        // Always re-apply from current _config — this cleans up any leftover
        // hotkey registration left behind by a successful probe followed by Cancel.
        ApplyHotkeyFromConfig();
        RefreshTrayCurrentLabel();
    }

    private bool ProbeHotkey(HotkeySpec spec)
    {
        // Try to register; if success, we re-apply from config in the OK path anyway.
        bool ok = _hotkeyHost.TryRegister(spec, () => _cycle.Cycle());
        if (!ok)
        {
            // Re-apply previous registration so we don't end up with no hotkey.
            ApplyHotkeyFromConfig();
        }
        return ok;
    }

    private void ApplyHotkeyFromConfig()
    {
        var spec = HotkeyParser.FromConfigEntry(_config.Hotkey);
        if (spec is null)
        {
            _hotkeyHost.Unregister();
            return;
        }
        if (!_hotkeyHost.TryRegister(spec.Value, () => _cycle.Cycle()))
        {
            MessageBox.Show(Strings.Get("error.hotkeyInUse"),
                Strings.Get("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnStartupToggled(bool isChecked)
    {
        _config.StartWithWindows = isChecked;
        if (isChecked) _startup.Enable(_exePath);
        else _startup.Disable();
        _store.Save(_config);
    }

    private void ShowAbout()
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "dev";
        MessageBox.Show($"{Strings.Get("app.title")} v{version}\n\n{Strings.Get("about.body")}",
            Strings.Get("tray.about"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExitApp()
    {
        _hotkeyHost.Dispose();
        _tray.Dispose();
        _toast.Dispose();
        ExitThread();
    }

    private void RefreshTrayCurrentLabel()
    {
        if (_config.Devices.Count == 0)
        {
            _tray.SetCurrentDeviceLabel(null);
            return;
        }
        string? currentId = _audio.GetDefaultOutputId(AudioRole.Multimedia);
        var match = _config.Devices.FirstOrDefault(d => d.EndpointId == currentId);
        _tray.SetCurrentDeviceLabel(match?.DisplayName ?? null);
    }

    private void PersistCurrentIndex()
    {
        // Fire-and-forget save on a thread-pool thread to avoid blocking the hotkey path.
        var snapshot = _config;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try { _store.Save(snapshot); } catch { /* swallow per spec */ }
        });
    }

    // === ICycleSink ===
    public void ShowToast(string text) => _toast.ShowMessage(text);
    public void ShowErrorToast(string text) => _toast.ShowMessage(text, isError: true);
    public void NotifyCurrentDeviceChanged() => RefreshTrayCurrentLabel();
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 4: Manual smoke test — launch app**

Run: `dotnet run --project src/AudioCarousel`

Expected:
- Tray icon appears in system tray (default OS icon since tray.ico not yet added).
- Right-click → menu shows all entries.
- On first run, the SettingsForm opens automatically with `(First-time setup)` in the title.
- Add at least 2 audio devices, set hotkey (e.g., Ctrl+Alt+F12 if you don't have F16 mapped), check "Start with Windows", click OK.
- Press the hotkey → device switches, toast appears at bottom-right of active monitor for 1.5s.
- Right-click tray → "Current: <device>" label updates.
- Click tray icon (left-click) → cycles to next device.
- "Exit" terminates the app.

If anything misbehaves, fix it now before proceeding.

- [ ] **Step 5: Verify config file is portable**

Check that `audio-carousel.json` was created next to the AudioCarousel.exe (in `src/AudioCarousel/bin/Debug/net9.0-windows/`). Inspect its contents.

- [ ] **Step 6: Commit**

```bash
git add src/AudioCarousel/Program.cs src/AudioCarousel/TrayApplicationContext.cs
git commit -m "feat: wire program entry, tray context, and full app lifecycle"
```

---

## Task 17: Tray Icon Resource

**Files:**
- Create: `src/AudioCarousel/Resources/tray.ico`
- Modify: `src/AudioCarousel/AudioCarousel.csproj`

- [ ] **Step 1: Obtain or generate tray.ico**

Create a simple tray icon. Two options:

**Option A** (quick): Download a free speaker icon from https://icons8.com/ or https://fonts.google.com/icons (Material "speaker" — export as 32px PNG, then convert to .ico via https://icoconvert.com/). Requires manual user step.

**Option B** (programmatic, no external sites): create the icon from a built-in glyph using PowerShell:
```powershell
# Run from project root in PowerShell
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap 32, 32
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.Clear([System.Drawing.Color]::Transparent)
$brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$font = New-Object System.Drawing.Font 'Segoe UI Symbol', 22, ([System.Drawing.FontStyle]::Bold)
$g.DrawString([char]0x1F50A, $font, $brush, 0, 0)
$ico = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
$out = [System.IO.File]::OpenWrite('src/AudioCarousel/Resources/tray.ico')
$ico.Save($out)
$out.Close()
```

Use whichever is convenient. The result should be a 32×32 .ico file at `src/AudioCarousel/Resources/tray.ico`.

- [ ] **Step 2: Embed icon as resource and re-enable ApplicationIcon**

Modify `src/AudioCarousel/AudioCarousel.csproj` — add inside the `<Project>` element (after `</PropertyGroup>`):
```xml
  <ItemGroup>
    <EmbeddedResource Include="Resources\tray.ico" />
  </ItemGroup>
```

Confirm `<ApplicationIcon>Resources\tray.ico</ApplicationIcon>` in the PropertyGroup is uncommented.

- [ ] **Step 3: Verify**

Run: `dotnet build`, then `dotnet run --project src/AudioCarousel`.
Expected: Tray icon now shows the custom icon. Window taskbar icon (if any error dialogs appear) also uses it.

- [ ] **Step 4: Commit**

```bash
git add src/AudioCarousel/Resources/tray.ico src/AudioCarousel/AudioCarousel.csproj
git commit -m "feat(ui): add tray.ico embedded resource"
```

---

## Task 18: Publish Script & README

**Files:**
- Create: `scripts/publish.ps1`
- Create: `README.md`

- [ ] **Step 1: Create publish script**

Create `scripts/publish.ps1`:
```powershell
# Audio Carousel — publish a single self-contained AOT executable.
# Output: publish/AudioCarousel.exe

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$PublishDir  = Join-Path $ProjectRoot 'publish'

if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }

dotnet publish (Join-Path $ProjectRoot 'src\AudioCarousel\AudioCarousel.csproj') `
  -c Release `
  -r win-x64 `
  -p:IsPublishing=true `
  -p:PublishSingleFile=true `
  -p:PublishAot=true `
  -p:SelfContained=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $PublishDir

Write-Host ""
Write-Host "Output:" -ForegroundColor Green
Get-Item (Join-Path $PublishDir 'AudioCarousel.exe') |
  Format-Table Name, @{Name='SizeMB'; Expression={[math]::Round($_.Length / 1MB, 2)}}
```

- [ ] **Step 2: Run publish**

Run: `pwsh ./scripts/publish.ps1`
Expected: `publish/AudioCarousel.exe` is created. Size should be in the 5–15 MB range. AOT may emit warnings about NAudio reflection patterns — note them but don't fail; if the exe runs correctly in step 3 the warnings are benign.

If publish fails outright due to AOT-incompatible patterns in NAudio, fall back to JIT single-file by removing `-p:PublishAot=true` from the script. Document the fallback in README.

- [ ] **Step 3: Smoke test the published exe**

Copy `publish/AudioCarousel.exe` to a fresh empty folder, double-click to launch, verify:
- Tray icon appears.
- Settings dialog opens (first run).
- Adding device + hotkey + cycling works.
- `audio-carousel.json` is created next to the exe.

If everything works, the build is good.

- [ ] **Step 4: Create README**

Create `README.md`:
```markdown
# Audio Carousel

A lightweight Windows tray utility that switches the system default audio output
device through a configured list with a single global hotkey.

Inspired by [PeekDesktop](https://github.com/shanselman/PeekDesktop): no
installer, single executable, portable configuration.

## Usage

1. Download `AudioCarousel.exe` from Releases and put it in any folder.
2. Double-click to launch. The tray icon appears and the Settings window opens
   on first run.
3. Add the audio output devices you want to cycle, set a hotkey (e.g., `F16`,
   `Ctrl+Alt+A`), and click OK.
4. Press the hotkey to cycle through devices. A toast at the bottom-right of
   the active monitor shows the new device name.
5. Optionally enable "Start with Windows" from the tray menu or settings.

Configuration lives in `audio-carousel.json` next to the executable. The only
registry write is `HKCU\...\Run` when "Start with Windows" is on, and only the
current user is affected. No admin rights are required.

## Build

Requires .NET 9 SDK on Windows.

```bash
dotnet build
dotnet test
```

## Publish single-file executable

```powershell
pwsh ./scripts/publish.ps1
```

Output: `publish/AudioCarousel.exe`.

If AOT publish fails with NAudio-related errors, remove the `-p:PublishAot=true`
flag from `scripts/publish.ps1` to fall back to single-file JIT (slightly larger
exe, otherwise identical behavior).

## System requirements

- Windows 10 1809 or later, or Windows 11
- x64
- No admin rights
```

- [ ] **Step 5: Final test pass**

Run: `dotnet test`
Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add scripts/ README.md
git commit -m "build: add publish script and README"
```

---

## Self-Review Checklist (run after writing the plan)

- ✅ Spec section 1 (Goal) → Tasks 9, 10, 11 (cycle, hotkey, toast)
- ✅ Spec section 2 (Non-functional: portable, single exe, lightweight) → Tasks 1 (csproj AOT settings), 18 (publish script)
- ✅ Spec section 3 (Tech stack) → Tasks 1, 2, 7
- ✅ Spec section 4 (Architecture) → Tasks 6–16 cover each component
- ✅ Spec section 5 (Config schema) → Tasks 2, 3
- ✅ Spec section 6 (Cycle logic — 3 roles, sync with OS, skip offline) → Task 9 with tests
- ✅ Spec section 7 (Hotkey — modifier-less allowed, null = disabled) → Tasks 4, 10, 13
- ✅ Spec section 8.1 (Tray menu) → Task 14
- ✅ Spec section 8.2 (Settings UI — current bold + ★, language combo, etc.) → Task 15
- ✅ Spec section 8.3 (First-launch UX — auto-open only when file didn't exist) → Task 16
- ✅ Spec section 9 (Distribution) → Task 18
- ✅ Spec section 10 (Start with Windows — HKCU Run-key, path drift) → Task 12, 16
- ✅ Spec section 11 (i18n — auto/en/ja, hand table) → Task 5
- ✅ Spec section 12 (Error handling) → Task 16 (Mutex, unhandled), Task 9 (switch failure), Task 3 (config corruption)
- ✅ No placeholders. All "TBD" patterns absent.
- ✅ Type consistency: `HotkeySpec`, `HotkeyEntry`, `AudioDevice`, `AudioRole` consistent across tasks. `IAudioDeviceService` and `ICycleSink` signatures match between definition and consumption.

---

## Execution Handoff

**Plan complete and saved to `docs/superpowers/plans/2026-04-25-audio-carousel.md`. Two execution options:**

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
