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

    // Pressing the hotkey before adding any device must explain itself
    // instead of silently doing nothing.
    [Fact]
    public void Cycle_EmptyDevices_ShowsHowToAddDevices()
    {
        var (c, a, s, _, saves) = Build();
        c.Cycle();
        Assert.Empty(a.SetCalls);
        Assert.Empty(s.Toasts);
        Assert.Single(s.ErrorToasts);
        Assert.Equal(Strings.Get("error.noDevicesConfigured"), s.ErrorToasts[0]);
        Assert.Equal(0, saves.Count);
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
    public void SwitchTo_SwitchesToRequestedDevice()
    {
        var (c, a, s, cfg, saves) = Build(("a", "A", true), ("b", "B", true), ("c", "C", true));
        cfg.CurrentIndex = 0;

        c.SwitchTo("c");

        Assert.Equal(2, cfg.CurrentIndex);
        Assert.Equal(3, a.SetCalls.Count); // 3 roles
        Assert.All(a.SetCalls, call => Assert.Equal("c", call.id));
        Assert.Equal("C", s.Toasts[^1]);
        Assert.Equal(1, saves.Count);
    }

    [Fact]
    public void SwitchTo_OfflineDevice_ShowsErrorToast()
    {
        var (c, a, s, cfg, _) = Build(("a", "A", true), ("b", "B", false));
        cfg.CurrentIndex = 0;

        c.SwitchTo("b");

        Assert.Empty(a.SetCalls);
        Assert.Single(s.ErrorToasts);
        Assert.Equal(0, cfg.CurrentIndex);
    }

    [Fact]
    public void SwitchTo_UnknownId_ShowsErrorToast()
    {
        var (c, a, s, _, _) = Build(("a", "A", true));

        c.SwitchTo("no-such-id");

        Assert.Empty(a.SetCalls);
        Assert.Single(s.ErrorToasts);
    }

    [Fact]
    public void SwitchTo_SetDefaultThrows_DoesNotAdvanceIndex()
    {
        var (c, a, s, cfg, saves) = Build(("a", "A", true), ("b", "B", true));
        cfg.CurrentIndex = 0;
        a.SetDefaultException = (_, _) => new InvalidOperationException("boom");

        c.SwitchTo("b");

        Assert.Equal(0, cfg.CurrentIndex);
        Assert.Single(s.ErrorToasts);
        Assert.Equal(0, saves.Count);
    }

    [Fact]
    public void SwitchTo_HealedId_Switches()
    {
        // Menu was built after healing, so it passes the live id even though the
        // config previously held a stale one.
        var (c, a, s, cfg, saves) = Build(("stale-lg", "LG", false));
        a.ActiveOutputs.Add(new AudioDevice("live-lg", "LG"));

        c.SwitchTo("live-lg");

        Assert.Equal("live-lg", cfg.Devices[0].EndpointId);
        Assert.Equal(3, a.SetCalls.Count);
        Assert.Equal("LG", s.Toasts[^1]);
        Assert.Equal(1, saves.Count);
    }

    [Fact]
    public void CyclePrevious_GoesBackAndWraps()
    {
        var (c, _, s, cfg, _) = Build(("a", "A", true), ("b", "B", true), ("c", "C", true));
        cfg.CurrentIndex = 1;
        c.CyclePrevious();
        Assert.Equal(0, cfg.CurrentIndex);
        Assert.Equal("A", s.Toasts[^1]);
        c.CyclePrevious();
        Assert.Equal(2, cfg.CurrentIndex);
        Assert.Equal("C", s.Toasts[^1]);
    }

    [Fact]
    public void CyclePrevious_SkipsOfflineDevices()
    {
        var (c, _, _, cfg, _) = Build(("a", "A", true), ("b", "B", false), ("c", "C", true));
        cfg.CurrentIndex = 2;
        c.CyclePrevious();
        Assert.Equal(0, cfg.CurrentIndex);
    }

    [Fact]
    public void CyclePrevious_SyncsWithOsDefaultFirst()
    {
        var (c, a, _, cfg, _) = Build(("a", "A", true), ("b", "B", true), ("c", "C", true));
        cfg.CurrentIndex = 0;
        a.Defaults[AudioRole.Multimedia] = "c";
        c.CyclePrevious();
        Assert.Equal(1, cfg.CurrentIndex);
    }

    // Microphones cycle through their own list and index; the output list
    // and its index are never touched.
    [Fact]
    public void InputTarget_CyclesInputListOnly()
    {
        Strings.SetLanguage(Language.English);
        var audio = new FakeAudioDeviceService();
        var cfg = new ConfigSchema();
        cfg.Devices.Add(new DeviceEntry { EndpointId = "spk", DisplayName = "Speakers" });
        cfg.CurrentIndex = 0;
        cfg.InputDevices.Add(new DeviceEntry { EndpointId = "mic1", DisplayName = "Mic 1" });
        cfg.InputDevices.Add(new DeviceEntry { EndpointId = "mic2", DisplayName = "Mic 2" });
        audio.ActiveOutputs.Add(new AudioDevice("mic1", "Mic 1"));
        audio.ActiveOutputs.Add(new AudioDevice("mic2", "Mic 2"));
        audio.Defaults[AudioRole.Multimedia] = "mic1";
        var sink = new FakeCycleSink();
        var c = new CycleController(cfg, audio, sink, () => { }, CycleTarget.Input);

        c.Cycle();

        Assert.Equal(1, cfg.InputCurrentIndex);
        Assert.Equal(0, cfg.CurrentIndex);
        Assert.All(audio.SetCalls, call => Assert.Equal("mic2", call.id));
        Assert.Equal("Mic 2", sink.Toasts[^1]);
        Assert.Equal(ToastKind.Input, sink.ToastKinds[^1]);
    }

    // Windows already names most microphones "Microphone (...)" / "マイク (...)";
    // don't print "Microphone: Microphone (...)".
    [Theory]
    [InlineData(Language.English, "Yeti Nano", "Microphone: Yeti Nano")]
    [InlineData(Language.English, "Microphone (USB Audio)", "Microphone (USB Audio)")]
    [InlineData(Language.Japanese, "マイク (Yeti Nano)", "マイク (Yeti Nano)")]
    [InlineData(Language.Japanese, "Headset", "マイク: Headset")]
    // Device names follow the Windows display language, not the app's UI language.
    [InlineData(Language.English, "マイク (Yeti Nano)", "マイク (Yeti Nano)")]
    [InlineData(Language.German, "Microphone (USB)", "Microphone (USB)")]
    public void MicrophoneLabel_AvoidsDoublePrefix(Language lang, string name, string expected)
    {
        Strings.SetLanguage(lang);
        Assert.Equal(expected, CycleController.MicrophoneLabel(name));
        Strings.SetLanguage(Language.English);
    }

    [Fact]
    public void InputTarget_Empty_ShowsInputSpecificHint()
    {
        Strings.SetLanguage(Language.English);
        var cfg = new ConfigSchema();
        cfg.Devices.Add(new DeviceEntry { EndpointId = "spk", DisplayName = "Speakers" });
        var sink = new FakeCycleSink();
        var c = new CycleController(cfg, new FakeAudioDeviceService(), sink, () => { }, CycleTarget.Input);

        c.Cycle();

        Assert.Single(sink.ErrorToasts);
        Assert.Equal(Strings.Get("error.noInputDevicesConfigured"), sink.ErrorToasts[0]);
    }

    // Users who keep a dedicated headset for calls (Teams/Discord use the
    // Communications role) can opt out of having it switched.
    [Fact]
    public void Cycle_SwitchCommunicationsOff_LeavesCommunicationsRoleAlone()
    {
        var (c, a, _, cfg, _) = Build(("a", "A", true), ("b", "B", true));
        cfg.SwitchCommunications = false;
        a.Defaults[AudioRole.Communications] = "headset";

        c.Cycle();

        Assert.Equal(2, a.SetCalls.Count);
        Assert.DoesNotContain(a.SetCalls, call => call.role == AudioRole.Communications);
        Assert.Equal("headset", a.Defaults[AudioRole.Communications]);
    }

    // A failure part-way through must not leave the roles split across devices.
    [Fact]
    public void Cycle_FailureOnLaterRole_RollsBackEarlierRoles()
    {
        var (c, a, s, cfg, _) = Build(("a", "A", true), ("b", "B", true));
        a.Defaults[AudioRole.Multimedia] = "a";
        a.Defaults[AudioRole.Console] = "a";
        a.Defaults[AudioRole.Communications] = "a";
        a.SetDefaultException = (id, role) =>
            id == "b" && role == AudioRole.Communications ? new InvalidOperationException("boom") : null;

        c.Cycle();

        Assert.Equal("a", a.Defaults[AudioRole.Multimedia]);
        Assert.Equal("a", a.Defaults[AudioRole.Console]);
        Assert.Equal("a", a.Defaults[AudioRole.Communications]);
        Assert.Equal(0, cfg.CurrentIndex);
        Assert.Single(s.ErrorToasts);
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
    public List<ToastKind> ToastKinds { get; } = new();
    public List<string> ErrorToasts { get; } = new();
    public void ShowToast(string deviceName, ToastKind kind)
    {
        Toasts.Add(deviceName);
        ToastKinds.Add(kind);
    }
    public void ShowErrorToast(string text) => ErrorToasts.Add(text);
    public void NotifyCurrentDeviceChanged() { }
}
