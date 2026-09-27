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

    // JSON deserialization does not enforce non-null annotations, so a config
    // like {"modifiers": null} or {"key": null} loads without error and must
    // degrade to "no hotkey" here rather than throw NullReferenceException.
    [Fact]
    public void FromConfigEntry_NullModifiers_ReturnsNull()
    {
        var entry = new Config.HotkeyEntry { Modifiers = null!, Key = "F16" };
        Assert.Null(HotkeyParser.FromConfigEntry(entry));
    }

    [Fact]
    public void FromConfigEntry_NullKey_ReturnsNull()
    {
        var entry = new Config.HotkeyEntry { Modifiers = new() { "Ctrl" }, Key = null! };
        Assert.Null(HotkeyParser.FromConfigEntry(entry));
    }

    [Fact]
    public void FromConfigEntry_NullModifierElement_ReturnsNull()
    {
        var entry = new Config.HotkeyEntry { Modifiers = new() { "Ctrl", null! }, Key = "A" };
        Assert.Null(HotkeyParser.FromConfigEntry(entry));
    }

    // Keys is a [Flags] enum, so Enum.TryParse happily accepts modifier bits,
    // flag combinations and raw numbers. None of those is a registrable key.
    [Theory]
    [InlineData("Control")]
    [InlineData("Shift")]
    [InlineData("Alt")]
    [InlineData("A, Shift")]
    [InlineData("65")]
    [InlineData("None")]
    [InlineData("ControlKey")]
    [InlineData("LWin")]
    public void Parse_NonRegistrableKey_Throws(string key)
    {
        Assert.Throws<FormatException>(() => HotkeyParser.Parse(new() { "Ctrl" }, key));
    }

    // A bare typing key registered globally would swallow that key in every app.
    [Theory]
    [InlineData(HotkeyModifier.None, Keys.A)]
    [InlineData(HotkeyModifier.None, Keys.Enter)]
    [InlineData(HotkeyModifier.None, Keys.Tab)]
    [InlineData(HotkeyModifier.None, Keys.Space)]
    [InlineData(HotkeyModifier.Shift, Keys.A)]
    [InlineData(HotkeyModifier.None, Keys.D1)]
    public void Validate_TypingKeyWithoutCtrlAltWin_NeedsModifier(HotkeyModifier mods, Keys key)
    {
        Assert.Equal(HotkeyValidation.NeedsModifier, HotkeyParser.Validate(new HotkeySpec(mods, key)));
    }

    [Theory]
    [InlineData(HotkeyModifier.None, Keys.F16)]
    [InlineData(HotkeyModifier.None, Keys.F1)]
    [InlineData(HotkeyModifier.None, Keys.F24)]
    [InlineData(HotkeyModifier.Control | HotkeyModifier.Alt, Keys.A)]
    [InlineData(HotkeyModifier.Win, Keys.F9)]
    [InlineData(HotkeyModifier.Alt, Keys.Enter)]
    public void Validate_SafeCombination_IsOk(HotkeyModifier mods, Keys key)
    {
        Assert.Equal(HotkeyValidation.Ok, HotkeyParser.Validate(new HotkeySpec(mods, key)));
    }

    [Fact]
    public void Validate_EscapeIsReserved()
    {
        // Esc cancels capture in the settings dialog; it can never be recorded.
        Assert.Equal(HotkeyValidation.Reserved,
            HotkeyParser.Validate(new HotkeySpec(HotkeyModifier.Control, Keys.Escape)));
    }

    [Theory]
    [InlineData(Keys.D1, "1")]
    [InlineData(Keys.D0, "0")]
    [InlineData(Keys.NumPad5, "Num 5")]
    [InlineData(Keys.Enter, "Enter")]
    [InlineData(Keys.PageUp, "PageUp")]
    [InlineData(Keys.PageDown, "PageDown")]
    [InlineData(Keys.F16, "F16")]
    public void Format_UsesFriendlyKeyNames(Keys key, string expected)
    {
        Assert.Equal(expected, HotkeyParser.Format(new HotkeySpec(HotkeyModifier.None, key)));
    }

    [Fact]
    public void ToConfigEntry_PageUp_RoundtripsDespiteEnumAlias()
    {
        // Keys.Prior == Keys.PageUp; whichever name is written must parse back.
        var spec = new HotkeySpec(HotkeyModifier.Control, Keys.PageUp);
        var entry = HotkeyParser.ToConfigEntry(spec);
        Assert.Equal(spec, HotkeyParser.Parse(entry.Modifiers, entry.Key));
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
