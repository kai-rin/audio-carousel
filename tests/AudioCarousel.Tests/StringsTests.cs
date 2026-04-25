using AudioCarousel.I18n;
using Xunit;

namespace AudioCarousel.Tests;

public class StringsTests
{
    [Fact]
    public void Get_DefaultsToEnglish()
    {
        Strings.SetLanguage(Language.English);
        Assert.Equal("Cycle next", Strings.Get("tray.cycleNext"));
    }

    [Fact]
    public void Get_JapaneseAfterSet()
    {
        Strings.SetLanguage(Language.Japanese);
        Assert.Equal("次のデバイスへ", Strings.Get("tray.cycleNext"));
        Strings.SetLanguage(Language.English); // reset
    }

    [Fact]
    public void Get_UnknownKey_ReturnsKeyAsIs()
    {
        Strings.SetLanguage(Language.English);
        Assert.Equal("nonexistent.key", Strings.Get("nonexistent.key"));
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("en")]
    [InlineData("ja")]
    public void ResolveLanguage_AcceptsValidConfigValues(string value)
    {
        var lang = Strings.ResolveLanguage(value, currentUiCultureIsJapanese: true);
        Assert.True(lang == Language.English || lang == Language.Japanese);
    }

    [Fact]
    public void ResolveLanguage_AutoOnJapaneseSystem_ReturnsJapanese()
    {
        Assert.Equal(Language.Japanese,
            Strings.ResolveLanguage("auto", currentUiCultureIsJapanese: true));
    }

    [Fact]
    public void ResolveLanguage_AutoOnNonJapaneseSystem_ReturnsEnglish()
    {
        Assert.Equal(Language.English,
            Strings.ResolveLanguage("auto", currentUiCultureIsJapanese: false));
    }
}
