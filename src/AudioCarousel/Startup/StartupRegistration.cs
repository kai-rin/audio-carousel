using Microsoft.Win32;

namespace AudioCarousel.Startup;

public sealed class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    // Task Manager's Startup tab records enable/disable here without touching
    // the Run value. First byte odd (0x03/0x07) = disabled.
    private const string ApprovedKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private readonly string _valueName;

    public StartupRegistration(string valueName = "AudioCarousel")
    {
        _valueName = valueName;
    }

    /// <summary>True when Windows will actually launch the app at sign-in.</summary>
    public bool IsEnabled() => GetRegisteredPath() is not null && !IsDisabledInTaskManager();

    public string? GetRegisteredPath()
    {
        string? raw = GetRawValue();
        return raw is null ? null : Unquote(raw);
    }

    public void Enable(string exePath)
    {
        WriteRunValue(exePath);
        // An explicit enable from the app overrides an earlier Task Manager disable.
        DeleteApprovedMarker();
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(_valueName, throwOnMissingValue: false);
        DeleteApprovedMarker();
    }

    public void EnsurePath(string currentExePath)
    {
        string? raw = GetRawValue();
        if (raw is null) return; // not enabled — nothing to fix
        // Compare against the exact stored form so a legacy unquoted value is
        // also rewritten into the quoted format. Only the path is fixed; a
        // Task Manager disable is the user's choice and stays in effect.
        if (!string.Equals(raw, $"\"{currentExePath}\"", StringComparison.OrdinalIgnoreCase))
            WriteRunValue(currentExePath);
    }

    private void WriteRunValue(string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        // Quote the path: an unquoted Run value containing spaces is ambiguous
        // when Windows executes it at logon.
        key.SetValue(_valueName, $"\"{exePath}\"", RegistryValueKind.String);
    }

    private bool IsDisabledInTaskManager()
    {
        using var key = Registry.CurrentUser.OpenSubKey(ApprovedKeyPath, writable: false);
        return key?.GetValue(_valueName) is byte[] { Length: > 0 } data && (data[0] & 1) == 1;
    }

    private void DeleteApprovedMarker()
    {
        using var key = Registry.CurrentUser.OpenSubKey(ApprovedKeyPath, writable: true);
        key?.DeleteValue(_valueName, throwOnMissingValue: false);
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
