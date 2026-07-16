using Microsoft.Win32;

namespace AudioCarousel.Startup;

public sealed class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly string _valueName;

    public StartupRegistration(string valueName = "AudioCarousel")
    {
        _valueName = valueName;
    }

    public bool IsEnabled() => GetRegisteredPath() is not null;

    public string? GetRegisteredPath()
    {
        string? raw = GetRawValue();
        return raw is null ? null : Unquote(raw);
    }

    public void Enable(string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        // Quote the path: an unquoted Run value containing spaces is ambiguous
        // when Windows executes it at logon.
        key.SetValue(_valueName, $"\"{exePath}\"", RegistryValueKind.String);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(_valueName, throwOnMissingValue: false);
    }

    public void EnsurePath(string currentExePath)
    {
        string? raw = GetRawValue();
        if (raw is null) return; // not enabled — nothing to fix
        // Compare against the exact stored form so a legacy unquoted value is
        // also rewritten into the quoted format.
        if (!string.Equals(raw, $"\"{currentExePath}\"", StringComparison.OrdinalIgnoreCase))
            Enable(currentExePath);
    }

    private string? GetRawValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(_valueName) as string;
    }

    private static string Unquote(string value) =>
        value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"')
            ? value[1..^1]
            : value;
}
