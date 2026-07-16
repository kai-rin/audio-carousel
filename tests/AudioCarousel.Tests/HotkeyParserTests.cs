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
    public void FromConfigEntry_Null_ReturnsNull()
    {
        Assert.Null(HotkeyParser.FromConfigEntry(null));
    }

    [Fact]
    public void FromConfigEntry_Valid_ReturnsSpec()
    {
        var entry = new Config.HotkeyEntry { Modifiers = new() { "Ctrl" }, Key = "F16" };
        var spec = HotkeyParser.FromConfigEntry(entry);
        Assert.Equal(new HotkeySpec(HotkeyModifier.Control, Keys.F16), spec);
    }

    // Hand-edited configs must not crash the app at startup: an unparsable
    // hotkey entry degrades to "no hotkey" instead of throwing.
    [Fact]
    public void FromConfigEntry_UnknownKey_ReturnsNull()
    {
        var entry = new Config.HotkeyEntry { Modifiers = new() { "Ctrl" }, Key = "NotAKey" };
        Assert.Null(HotkeyParser.FromConfigEntry(entry));
    }

    [Fact]
    public void FromConfigEntry_UnknownModifier_ReturnsNull()
    {
        var entry = new Config.HotkeyEntry { Modifiers = new() { "Hyper" }, Key = "A" };
        Assert.Null(HotkeyParser.FromConfigEntry(entry));
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
