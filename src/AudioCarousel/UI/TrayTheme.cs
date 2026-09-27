using Microsoft.Win32;

namespace AudioCarousel.UI;

/// <summary>Whether the taskbar (and so the tray) uses the light theme.</summary>
public static class TrayTheme
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    /// <summary>
    /// Interprets the raw <c>SystemUsesLightTheme</c> registry value. Missing
    /// or unexpected values mean dark: that is the Windows 10/11 default for
    /// the taskbar, where a light (navy) glyph would disappear.
    /// </summary>
    public static bool IsLight(object? systemUsesLightTheme) =>
        systemUsesLightTheme is int value && value != 0;

    public static bool TaskbarIsLight()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey, writable: false);
            return IsLight(key?.GetValue("SystemUsesLightTheme"));
        }
        catch (Exception)
        {
            return false;
        }
    }
}
