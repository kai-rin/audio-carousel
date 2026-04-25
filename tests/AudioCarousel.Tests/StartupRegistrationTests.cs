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
}
