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
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(_valueName) as string;
    }

    public void Enable(string exePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        key.SetValue(_valueName, exePath, RegistryValueKind.String);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(_valueName, throwOnMissingValue: false);
    }

    public void EnsurePath(string currentExePath)
    {
        string? registered = GetRegisteredPath();
        if (registered is null) return; // not enabled — nothing to fix
        if (!string.Equals(registered, currentExePath, StringComparison.OrdinalIgnoreCase))
            Enable(currentExePath);
    }
}
