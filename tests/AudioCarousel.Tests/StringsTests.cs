using AudioCarousel.I18n;
using Xunit;

namespace AudioCarousel.Tests;

// Strings._current is global mutable state. Any test that touches it must share
// this collection so xUnit serializes them across classes.
[CollectionDefinition("StringsState", DisableParallelization = true)]
public class StringsStateCollection { }

[Collection("StringsState")]
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
    [InlineData(Language.English, "Cycle next")]
    [InlineData(Language.Japanese, "次のデバイスへ")]
    [InlineData(Language.ChineseSimplified, "切换到下一个设备")]
    [InlineData(Language.ChineseTraditional, "切換到下一個裝置")]
    [InlineData(Language.Spanish, "Siguiente dispositivo")]
    [InlineData(Language.French, "Périphérique suivant")]
    [InlineData(Language.German, "Nächstes Gerät")]
    [InlineData(Language.PortugueseBrazil, "Próximo dispositivo")]
    [InlineData(Language.Russian, "Следующее устройство")]
    [InlineData(Language.Korean, "다음 장치")]
    public void Get_ReturnsTranslation_ForEachLanguage(Language lang, string expected)
    {
        Strings.SetLanguage(lang);
        Assert.Equal(expected, Strings.Get("tray.cycleNext"));
        Strings.SetLanguage(Language.English); // reset
    }

    [Fact]
    public void Get_LanguageSelfNames_AreSameAcrossAllLanguages()
    {
        // Language self-names like "Español" should appear identically in every UI language.
        foreach (Language lang in Enum.GetValues<Language>())
        {
            Strings.SetLanguage(lang);
            Assert.Equal("Español", Strings.Get("settings.languageEs"));
            Assert.Equal("한국어", Strings.Get("settings.languageKo"));
            Assert.Equal("简体中文", Strings.Get("settings.languageZhHans"));
        }
        Strings.SetLanguage(Language.English);
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("en")]
    [InlineData("ja")]
    [InlineData("zh-Hans")]
    [InlineData("zh-Hant")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("pt-BR")]
    [InlineData("ru")]
    [InlineData("ko")]
    public void ResolveLanguage_AcceptsValidConfigValues(string value)
    {
        var lang = Strings.ResolveLanguage(value, currentUiCultureName: "ja-JP");
        Assert.Contains(lang, Enum.GetValues<Language>());
    }

    [Theory]
    [InlineData("en", Language.English)]
    [InlineData("ja", Language.Japanese)]
    [InlineData("zh-Hans", Language.ChineseSimplified)]
    [InlineData("zh-CN", Language.ChineseSimplified)]
    [InlineData("zh-SG", Language.ChineseSimplified)]
    [InlineData("zh-Hant", Language.ChineseTraditional)]
    [InlineData("zh-TW", Language.ChineseTraditional)]
    [InlineData("zh-HK", Language.ChineseTraditional)]
    [InlineData("zh-MO", Language.ChineseTraditional)]
    [InlineData("es", Language.Spanish)]
    [InlineData("fr", Language.French)]
    [InlineData("de", Language.German)]
    [InlineData("pt", Language.PortugueseBrazil)]
    [InlineData("pt-BR", Language.PortugueseBrazil)]
    [InlineData("pt-PT", Language.PortugueseBrazil)]
    [InlineData("ru", Language.Russian)]
    [InlineData("ko", Language.Korean)]
    public void ResolveLanguage_ExplicitCodes(string code, Language expected)
    {
        Assert.Equal(expected, Strings.ResolveLanguage(code, currentUiCultureName: ""));
    }

    [Theory]
    [InlineData("ja-JP", Language.Japanese)]
    [InlineData("zh-CN", Language.ChineseSimplified)]
    [InlineData("zh-Hans-CN", Language.ChineseSimplified)]
    [InlineData("zh-TW", Language.ChineseTraditional)]
    [InlineData("zh-HK", Language.ChineseTraditional)]
    [InlineData("zh-Hant-TW", Language.ChineseTraditional)]
    [InlineData("es-ES", Language.Spanish)]
    [InlineData("es-MX", Language.Spanish)]
    [InlineData("fr-FR", Language.French)]
    [InlineData("de-DE", Language.German)]
    [InlineData("pt-BR", Language.PortugueseBrazil)]
    [InlineData("pt-PT", Language.PortugueseBrazil)]
    [InlineData("ru-RU", Language.Russian)]
    [InlineData("ko-KR", Language.Korean)]
    [InlineData("en-US", Language.English)]
    [InlineData("th-TH", Language.English)]   // unsupported -> English fallback
    [InlineData("vi-VN", Language.English)]
    [InlineData("", Language.English)]
    public void ResolveLanguage_AutoFromCulture(string culture, Language expected)
    {
        Assert.Equal(expected, Strings.ResolveLanguage("auto", culture));
    }

    [Fact]
    public void ResolveLanguage_UnknownConfigValue_FallsBackToCultureDetection()
    {
        // An unknown config value (e.g., a typo or future code we don't know) should
        // fall through to culture detection rather than throwing.
        Assert.Equal(Language.Japanese, Strings.ResolveLanguage("xx-bogus", "ja-JP"));
        Assert.Equal(Language.English, Strings.ResolveLanguage("xx-bogus", "en-US"));
    }
}
