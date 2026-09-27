using System.Windows.Forms;

namespace AudioCarousel.Hotkey;

[Flags]
public enum HotkeyModifier
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
}

public readonly record struct HotkeySpec(HotkeyModifier Modifiers, Keys Key);

public enum HotkeyValidation
{
    Ok,
    // No Ctrl/Alt/Win on a non-function key: registering it would swallow typing.
    NeedsModifier,
    // Key the capture UI itself uses (Esc cancels capture).
    Reserved,
}
