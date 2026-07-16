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
        var parts = ModifierNames(spec.Modifiers);
        parts.Add(spec.Key.ToString());
        return string.Join(" + ", parts);
    }

    public static HotkeyEntry ToConfigEntry(HotkeySpec spec)
    {
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
        if (entry is null) return null;
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
}
