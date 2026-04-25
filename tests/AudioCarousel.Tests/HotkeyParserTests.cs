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
