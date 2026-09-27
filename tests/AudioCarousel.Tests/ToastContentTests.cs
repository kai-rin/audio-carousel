using AudioCarousel.Cycle;
using AudioCarousel.I18n;
using AudioCarousel.UI;
using Xunit;

namespace AudioCarousel.Tests;

[Collection("StringsState")]
public class ToastContentTests
{
    [Fact]
    public void OutputSwitch_ShowsDeviceWithOutputSubtitleAndSpeaker()
    {
        Strings.SetLanguage(Language.English);
        var c = ToastContent.ForSwitch("Speakers", ToastKind.Output);
        Assert.Equal("Speakers", c.Title);
        Assert.Equal(Strings.Get("toast.outputChanged"), c.Subtitle);
        Assert.Equal(MenuGlyphs.Speaker, c.Glyph);
        Assert.False(c.IsError);
    }

    [Fact]
    public void InputSwitch_UsesMicrophoneGlyphAndSubtitle()
    {
        Strings.SetLanguage(Language.English);
        var c = ToastContent.ForSwitch("Yeti Nano", ToastKind.Input);
        Assert.Equal("Yeti Nano", c.Title);
        Assert.Equal(Strings.Get("toast.inputChanged"), c.Subtitle);
        Assert.Equal(MenuGlyphs.Microphone, c.Glyph);
    }

    [Fact]
    public void Error_HasWarningGlyphAndNoSubtitle()
    {
        var c = ToastContent.ForError("boom");
        Assert.Equal("boom", c.Title);
        Assert.Null(c.Subtitle);
        Assert.Equal(MenuGlyphs.Warning, c.Glyph);
        Assert.True(c.IsError);
    }
}
