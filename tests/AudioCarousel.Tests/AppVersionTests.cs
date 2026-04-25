using Xunit;

namespace AudioCarousel.Tests;

public class AppVersionTests
{
    [Theory]
    [InlineData("1.2.3+abc123", "1.2.3")]
    [InlineData("1.0.0-dev+deadbeef", "1.0.0-dev")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("1.0.0-dev", "1.0.0-dev")]
    [InlineData("1.0.0.0", "1.0.0.0")]
    public void Format_StripsBuildMetadataAfterPlus(string raw, string expected)
    {
        Assert.Equal(expected, AppVersion.Format(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Format_FallsBackToDevForNullOrBlank(string? raw)
    {
        Assert.Equal("dev", AppVersion.Format(raw));
    }
}
