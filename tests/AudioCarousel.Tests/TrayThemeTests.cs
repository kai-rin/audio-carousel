using AudioCarousel.UI;
using Xunit;

namespace AudioCarousel.Tests;

public class TrayThemeTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void IsLight_ReadsDword(int value, bool expected)
    {
        Assert.Equal(expected, TrayTheme.IsLight(value));
    }

    // Missing or odd values fall back to dark: the default taskbar, where a
    // navy glyph would be invisible.
    [Theory]
    [InlineData(null)]
    [InlineData("1")]
    [InlineData(1L)]
    public void IsLight_UnexpectedValue_IsDark(object? value)
    {
        Assert.False(TrayTheme.IsLight(value));
    }
}
