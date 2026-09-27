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
    public void Get_SaveFailed_HasTranslationInEveryLanguage()
    {
        foreach (Language lang in Enum.GetValues<Language>())
        {
            Strings.SetLanguage(lang);
            string text = Strings.Get("error.saveFailed");
            Assert.NotEqual("error.saveFailed", text); // key resolved
            Assert.False(string.IsNullOrWhiteSpace(text));
        }
        Strings.SetLanguage(Language.English);
    }

    [Theory]
    [InlineData("key.ctrl")]
    [InlineData("key.alt")]
    [InlineData("key.shift")]
    [InlineData("key.win")]
    [InlineData("error.hotkeyInvalid")]
    [InlineData("settings.hotkeyNeedsModifier")]
    public void Get_HotkeyKeys_HaveTranslationInEveryLanguage(string key)
    {
        foreach (Language lang in Enum.GetValues<Language>())
        {
            Strings.SetLanguage(lang);
            string text = Strings.Get(key);
            Assert.NotEqual(key, text); // key resolved
            Assert.False(string.IsNullOrWhiteSpace(text));
        }
        Strings.SetLanguage(Language.English);
    }

    // Strings.Get returns the key itself for unknown keys, so a renamed or
    // deleted key would silently show "settings.foo" in the UI. Scan the app
    // source for every literal key and make sure each one resolves.
    [Fact]
    public void EveryKeyUsedInSource_ExistsInTable()
    {
        string srcDir = Path.Combine(FindRepoRoot(), "src", "AudioCarousel");
        var keyPattern = new System.Text.RegularExpressions.Regex("Strings\\.Get\\(\"([^\"]+)\"\\)");
        var keys = Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .SelectMany(f => keyPattern.Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value))
            .Distinct()
            .ToList();

        Assert.NotEmpty(keys);
        Strings.SetLanguage(Language.English);
        Assert.All(keys, key => Assert.NotEqual(key, Strings.Get(key)));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AudioCarousel.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
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
