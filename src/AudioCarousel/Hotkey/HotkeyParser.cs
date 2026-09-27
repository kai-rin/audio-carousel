using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AudioCarousel.Config;
using AudioCarousel.I18n;

namespace AudioCarousel.Hotkey;

public static partial class HotkeyParser
{
    public static HotkeySpec Parse(List<string> modifiers, string key)
    {
        var mod = HotkeyModifier.None;
        foreach (string m in modifiers)
        {
            // Forgiving for hand-edited config files: "ctrl" / "CTRL" both accepted.
            mod |= m.Trim().ToLowerInvariant() switch
            {
                "ctrl" => HotkeyModifier.Control,
                "alt" => HotkeyModifier.Alt,
                "shift" => HotkeyModifier.Shift,
                "win" => HotkeyModifier.Win,
                _ => throw new FormatException($"Unknown modifier: {m}"),
            };
        }

        string trimmed = key.Trim();
        // Keys is a [Flags] enum: Enum.TryParse accepts raw numbers ("65") and
        // flag combinations ("A, Shift"), neither of which names a single key.
        if (trimmed.Contains(',') || long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            throw new FormatException($"Unknown key: {key}");
        if (!Enum.TryParse<Keys>(trimmed, ignoreCase: true, out var parsedKey) || !IsRegistrableKey(parsedKey))
            throw new FormatException($"Unknown key: {key}");

        return new HotkeySpec(mod, parsedKey);
    }

    // A single virtual key (1..0xFE, no modifier flag bits) that is not itself a modifier key.
    private static bool IsRegistrableKey(Keys key) =>
        (key & ~Keys.KeyCode) == 0
        && (int)key is >= 1 and <= 0xFE
        && !IsModifierKey(key);

    public static bool IsModifierKey(Keys key) =>
        key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu
            or Keys.LWin or Keys.RWin;

    public static HotkeyValidation Validate(HotkeySpec spec)
    {
        if (spec.Key == Keys.Escape) return HotkeyValidation.Reserved;

        // Shift alone doesn't count: Shift+A is ordinary typing.
        bool hasSafeModifier = (spec.Modifiers & (HotkeyModifier.Control | HotkeyModifier.Alt | HotkeyModifier.Win)) != 0;
        bool isFunctionKey = spec.Key is >= Keys.F1 and <= Keys.F24;
        if (!hasSafeModifier && !isFunctionKey) return HotkeyValidation.NeedsModifier;

        return HotkeyValidation.Ok;
    }

    public static string Format(HotkeySpec spec)
    {
        var parts = ModifierNames(spec.Modifiers);
        parts.Add(KeyDisplayName(spec.Key));
        return string.Join(" + ", parts);
    }

    // Same as Format, but with modifier names in the current UI language (e.g. "Strg" in German).
    public static string FormatForDisplay(HotkeySpec spec)
    {
        var parts = new List<string>(5);
        if (spec.Modifiers.HasFlag(HotkeyModifier.Control)) parts.Add(Strings.Get("key.ctrl"));
        if (spec.Modifiers.HasFlag(HotkeyModifier.Alt)) parts.Add(Strings.Get("key.alt"));
        if (spec.Modifiers.HasFlag(HotkeyModifier.Shift)) parts.Add(Strings.Get("key.shift"));
        if (spec.Modifiers.HasFlag(HotkeyModifier.Win)) parts.Add(Strings.Get("key.win"));
        parts.Add(KeyDisplayName(spec.Key));
        return string.Join(" + ", parts);
    }

    private static string KeyDisplayName(Keys key)
    {
        switch (key)
        {
            case >= Keys.D0 and <= Keys.D9:
                return ((char)('0' + (key - Keys.D0))).ToString();
            case >= Keys.NumPad0 and <= Keys.NumPad9:
                return "Num " + (key - Keys.NumPad0).ToString(CultureInfo.InvariantCulture);
            case Keys.Return:
                return "Enter";
            case Keys.Prior:
                return "PageUp";
            case Keys.Next:
                return "PageDown";
            case Keys.Capital:
                return "CapsLock";
            case Keys.Back:
                return "Backspace";
        }

        if (IsOemKey(key))
        {
            // Show the character printed on the key for the active keyboard layout.
            // The high bit marks dead keys; the low word is the character.
            uint mapped = MapVirtualKeyW((uint)key, MAPVK_VK_TO_CHAR);
            char c = (char)(mapped & 0xFFFF);
            if (c != '\0') return char.ToUpperInvariant(c).ToString();
        }

        return key.ToString();
    }

    private static bool IsOemKey(Keys key) =>
        (int)key is (>= 0xBA and <= 0xC0) or (>= 0xDB and <= 0xDF) or 0xE2;

    public static HotkeyEntry ToConfigEntry(HotkeySpec spec)
    {
        // Enum name (not the friendly display name) so Parse can round-trip it.
        return new HotkeyEntry { Modifiers = ModifierNames(spec.Modifiers), Key = spec.Key.ToString() };
    }

    private static List<string> ModifierNames(HotkeyModifier mod)
    {
        var names = new List<string>(5);
        if (mod.HasFlag(HotkeyModifier.Control)) names.Add("Ctrl");
        if (mod.HasFlag(HotkeyModifier.Alt)) names.Add("Alt");
        if (mod.HasFlag(HotkeyModifier.Shift)) names.Add("Shift");
        if (mod.HasFlag(HotkeyModifier.Win)) names.Add("Win");
        return names;
    }

    public static HotkeySpec? FromConfigEntry(HotkeyEntry? entry)
    {
        // JSON deserialization does not enforce non-null annotations, so a
        // hand-edited or script-generated config can carry null lists/keys.
        if (entry?.Key is null || entry.Modifiers is null || entry.Modifiers.Contains(null!))
            return null;
        try
        {
            return Parse(entry.Modifiers, entry.Key);
        }
        catch (FormatException)
        {
            // A hand-edited entry that fails to parse degrades to "no hotkey"
            // instead of crashing startup (the JSON itself was valid, so the
            // corrupted-config recovery path never runs for this case).
            return null;
        }
    }

    private const uint MAPVK_VK_TO_CHAR = 2;

    [LibraryImport("user32.dll")]
    private static partial uint MapVirtualKeyW(uint uCode, uint uMapType);
}
