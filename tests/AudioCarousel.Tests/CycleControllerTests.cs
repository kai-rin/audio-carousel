using AudioCarousel.Audio;
using AudioCarousel.Config;
using AudioCarousel.Cycle;
using AudioCarousel.I18n;
using AudioCarousel.Tests.Fakes;
using Xunit;

namespace AudioCarousel.Tests;

[Collection("StringsState")]
public class CycleControllerTests
{
    private sealed class PersistCounter
    {
        public int Count { get; private set; }
        public void Increment() => Count++;
    }

    private static (CycleController c, FakeAudioDeviceService a, FakeCycleSink s, ConfigSchema cfg, PersistCounter saves)
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
        var saves = new PersistCounter();
        var controller = new CycleController(cfg, audio, sink, saves.Increment);
        return (controller, audio, sink, cfg, saves);
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
        var (c, a, s, cfg, saves) = Build(("a", "A", true));
        c.Cycle();
        Assert.Equal("a", cfg.Devices[cfg.CurrentIndex].EndpointId);
        // 3 roles x 1 device
        Assert.Equal(3, a.SetCalls.Count);
        Assert.Single(s.Toasts);
        Assert.Equal("A", s.Toasts[0]);
        Assert.Equal(1, saves.Count);
    }

    [Fact]
    public void Cycle_FailureDoesNotPersist()
    {
        var (c, a, _, _, saves) = Build(("a", "A", true), ("b", "B", true));
        a.SetDefaultException = (_, _) => new InvalidOperationException("boom");
        c.Cycle();
        Assert.Equal(0, saves.Count);
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
    public void Cycle_StaleId_HealsByNameAndSwitches()
    {
        // Persisted ID went stale (NVIDIA HDA churn); a live device has the same name.
        var (c, a, s, cfg, saves) = Build(("stale-lg", "LG", false));
        a.ActiveOutputs.Add(new AudioDevice("live-lg", "LG"));

        c.Cycle();

        Assert.Equal("live-lg", cfg.Devices[0].EndpointId);
        Assert.Equal(3, a.SetCalls.Count);
        Assert.All(a.SetCalls, call => Assert.Equal("live-lg", call.id));
        Assert.Equal("LG", s.Toasts[^1]);
        Assert.Equal(1, saves.Count);
    }

    [Fact]
    public void Cycle_HealedEntrySyncsCurrentIndexBeforeAdvancing()
    {
        // OS default is the healed entry's NEW id — sync must see it after healing,
        // so the cycle advances to the other device instead of re-selecting LG.
        var (c, a, s, cfg, _) = Build(("b", "B", true), ("stale-lg", "LG", false));
        a.ActiveOutputs.Add(new AudioDevice("live-lg", "LG"));
        a.Defaults[AudioRole.Multimedia] = "live-lg";
        cfg.CurrentIndex = 0;

        c.Cycle();

        Assert.Equal(0, cfg.CurrentIndex);
        Assert.Equal("B", s.Toasts[^1]);
    }

    [Fact]
    public void Cycle_StaleId_NoNameMatch_SkippedAsBefore()
    {
        var (c, _, s, cfg, _) = Build(("stale-lg", "LG", false), ("b", "B", true));
        cfg.CurrentIndex = 1;

        c.Cycle();

        Assert.Equal("stale-lg", cfg.Devices[0].EndpointId);
        Assert.Equal(1, cfg.CurrentIndex);
        Assert.Equal("B", s.Toasts[^1]);
    }

    [Fact]
    public void Cycle_AllStale_NoNameMatch_ErrorToastAndNoSave()
    {
        var (c, a, s, _, saves) = Build(("stale-lg", "LG", false));

        c.Cycle();

        Assert.Empty(a.SetCalls);
        Assert.Single(s.ErrorToasts);
        Assert.Equal(0, saves.Count);
    }

    [Fact]
    public void Cycle_HealPersistsEvenWhenSetDefaultFails()
    {
        var (c, a, s, cfg, saves) = Build(("stale-lg", "LG", false));
        a.ActiveOutputs.Add(new AudioDevice("live-lg", "LG"));
        a.SetDefaultException = (_, _) => new InvalidOperationException("boom");

        c.Cycle();

        Assert.Single(s.ErrorToasts);
        Assert.Equal("live-lg", cfg.Devices[0].EndpointId);
        Assert.Equal(1, saves.Count); // healed ID must survive even on switch failure
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
    public void ShowToast(string text) => Toasts.Add(text);
    public void ShowErrorToast(string text) => ErrorToasts.Add(text);
    public void NotifyCurrentDeviceChanged() { }
}
