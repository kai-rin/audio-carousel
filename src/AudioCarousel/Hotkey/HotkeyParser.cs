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

        if (!Enum.TryParse<Keys>(key.Trim(), ignoreCase: true, out var parsedKey))
            throw new FormatException($"Unknown key: {key}");

        return new HotkeySpec(mod, parsedKey);
    }

    public static string Format(HotkeySpec spec)
    {
        var parts = new List<string>(5);
        if (spec.Modifiers.HasFlag(HotkeyModifier.Control)) parts.Add("Ctrl");
        if (spec.Modifiers.HasFlag(HotkeyModifier.Alt)) parts.Add("Alt");
        if (spec.Modifiers.HasFlag(HotkeyModifier.Shift)) parts.Add("Shift");
        if (spec.Modifiers.HasFlag(HotkeyModifier.Win)) parts.Add("Win");
        parts.Add(spec.Key.ToString());
        return string.Join(" + ", parts);
    }

    public static HotkeyEntry ToConfigEntry(HotkeySpec spec)
    {
        var mods = new List<string>(4);
        if (spec.Modifiers.HasFlag(HotkeyModifier.Control)) mods.Add("Ctrl");
        if (spec.Modifiers.HasFlag(HotkeyModifier.Alt)) mods.Add("Alt");
        if (spec.Modifiers.HasFlag(HotkeyModifier.Shift)) mods.Add("Shift");
        if (spec.Modifiers.HasFlag(HotkeyModifier.Win)) mods.Add("Win");
        return new HotkeyEntry { Modifiers = mods, Key = spec.Key.ToString() };
    }

    public static HotkeySpec? FromConfigEntry(HotkeyEntry? entry)
    {
        if (entry is null) return null;
        return Parse(entry.Modifiers, entry.Key);
    }
}
