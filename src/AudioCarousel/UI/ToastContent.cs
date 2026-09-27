using AudioCarousel.Cycle;
using AudioCarousel.I18n;

namespace AudioCarousel.UI;

/// <summary>What the switch toast shows: bold title, optional subtitle, icon glyph.</summary>
public sealed record ToastContent(string Title, string? Subtitle, char Glyph, bool IsError)
{
    public static ToastContent ForSwitch(string deviceName, ToastKind kind) => kind == ToastKind.Input
        ? new ToastContent(deviceName, Strings.Get("toast.inputChanged"), MenuGlyphs.Microphone, IsError: false)
        : new ToastContent(deviceName, Strings.Get("toast.outputChanged"), MenuGlyphs.Speaker, IsError: false);

    public static ToastContent ForError(string message) =>
        new(message, Subtitle: null, MenuGlyphs.Warning, IsError: true);
}
