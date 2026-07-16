using AudioCarousel.Startup;
using Microsoft.Win32;
using Xunit;

namespace AudioCarousel.Tests;

public class StartupRegistrationTests : IDisposable
{
    private readonly string _testValueName;

    public StartupRegistrationTests()
    {
        // Use a unique value name so tests don't collide with the real one.
        _testValueName = "AudioCarousel-TEST-" + Guid.NewGuid().ToString("N");
    }

    public void Dispose()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        key?.DeleteValue(_testValueName, throwOnMissingValue: false);
    }

    [Fact]
    public void Enable_WritesExePathToRunKey()
    {
        var reg = new StartupRegistration(_testValueName);
        reg.Enable(@"C:\path\to\exe.exe");

        Assert.True(reg.IsEnabled());
        Assert.Equal(@"C:\path\to\exe.exe", reg.GetRegisteredPath());
    }

    [Fact]
    public void Disable_RemovesValue()
    {
        var reg = new StartupRegistration(_testValueName);
        reg.Enable(@"C:\path\to\exe.exe");
        reg.Disable();

        Assert.False(reg.IsEnabled());
        Assert.Null(reg.GetRegisteredPath());
    }

    [Fact]
    public void EnsurePath_FixesDriftedPath()
    {
        var reg = new StartupRegistration(_testValueName);
        reg.Enable(@"C:\old\path.exe");
        reg.EnsurePath(@"C:\new\path.exe");

        Assert.Equal(@"C:\new\path.exe", reg.GetRegisteredPath());
    }

    [Fact]
    public void EnsurePath_NoOpWhenDisabled()
    {
        var reg = new StartupRegistration(_testValueName);
        reg.EnsurePath(@"C:\anything.exe");
        Assert.False(reg.IsEnabled());
    }

    // Run-key values must be quoted: an unquoted path containing spaces
    // (e.g. "D:\My Tools\AudioCarousel.exe") is ambiguous at startup time.
    [Fact]
    public void Enable_WritesQuotedValueToRegistry()
    {
        var reg = new StartupRegistration(_testValueName);
        reg.Enable(@"D:\My Tools\AudioCarousel.exe");

        Assert.Equal("\"D:\\My Tools\\AudioCarousel.exe\"", ReadRawValue());
        Assert.Equal(@"D:\My Tools\AudioCarousel.exe", reg.GetRegisteredPath());
    }

    [Fact]
    public void EnsurePath_UpgradesLegacyUnquotedValue()
    {
        WriteRawValue(@"D:\My Tools\AudioCarousel.exe"); // legacy unquoted format
        var reg = new StartupRegistration(_testValueName);
        reg.EnsurePath(@"D:\My Tools\AudioCarousel.exe");

        Assert.Equal("\"D:\\My Tools\\AudioCarousel.exe\"", ReadRawValue());
    }

    private string? ReadRawValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", writable: false);
        return key?.GetValue(_testValueName) as string;
    }

    private void WriteRawValue(string value)
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        key!.SetValue(_testValueName, value, RegistryValueKind.String);
    }
}
