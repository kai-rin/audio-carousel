using System.Net;
using System.Text;
using System.Windows.Forms;
using AudioCarousel.Hotkey;
using AudioCarousel.I18n;
using AudioCarousel.UI;

namespace BrandAssets;

/// <summary>
/// The README hero image: marketing copy on the left, a faithful mock-up of
/// the app on the right (taskbar, tooltip, tray menu, toast). Every UI label,
/// hotkey name and icon glyph is taken from the app assembly itself, and the
/// menu/toast styling mirrors FluentMenuRenderer and ToastWindow.
/// </summary>
public static class Hero
{
    public const int Width = 1448;
    public const int Height = 1086;

    private static readonly (Language Lang, string Code, string File, HeroCopy Copy)[] Locales =
    {
        (Language.English, "en", "hero-en.png", HeroCopy.En),
        (Language.Japanese, "ja", "hero-ja.png", HeroCopy.Ja),
        (Language.ChineseSimplified, "zh-Hans", "hero-zh-Hans.png", HeroCopy.ZhHans),
    };

    public static void Build(string repoRoot, string workDir)
    {
        foreach (var (lang, code, file, copy) in Locales)
        {
            Strings.SetLanguage(lang);
            string html = Path.Combine(workDir, $"hero-{code}.html");
            File.WriteAllText(html, Page(code, copy), Encoding.UTF8);
            string png = Path.Combine(repoRoot, "docs", "images", file);
            EdgeRenderer.Screenshot(html, png, Width, Height, transparent: false);
            Console.WriteLine($"wrote {Path.GetRelativePath(repoRoot, png)} ({new FileInfo(png).Length / 1024} KiB)");
        }
        Strings.SetLanguage(Language.English);
    }

    private static string E(string text) => WebUtility.HtmlEncode(text);

    private static string Glyph(char glyph, string cls = "g") => $"<span class=\"{cls}\">&#x{(int)glyph:X};</span>";

    private static string Hotkey(Keys key) =>
        E(HotkeyParser.FormatForDisplay(new HotkeySpec(HotkeyModifier.Control | HotkeyModifier.Alt, key)));

    // Keep the config file name on one line (browsers break at its hyphen).
    private static string Body(string text) =>
        E(text).Replace("audio-carousel.json", "<span class=\"nowrap\">audio-carousel.json</span>");

    private static string Page(string code, HeroCopy t)
    {
        bool cjk = code != "en";
        string textFont = code switch
        {
            "ja" => "\"Yu Gothic UI\", \"Segoe UI\", sans-serif",
            "zh-Hans" => "\"Microsoft YaHei UI\", \"Segoe UI\", sans-serif",
            _ => "\"Segoe UI\", sans-serif",
        };

        var features = new StringBuilder();
        foreach (var f in t.Features)
        {
            features.Append($"""
                <div class="feature">{FeatureSvg(f.Icon)}<div>
                  <div class="ftitle">{E(f.Title)}</div><div class="fbody">{Body(f.Body)}</div>
                </div></div>
                """);
        }

        var band = new StringBuilder();
        for (int i = 0; i < t.Band.Length; i++)
        {
            band.Append($"""
                <div class="bitem">{BandSvg(i)}<div>
                  <div class="btitle">{E(t.Band[i].Title)}</div><div class="bbody">{E(t.Band[i].Body)}</div>
                </div></div>
                """);
        }

        return $$"""
            <!doctype html>
            <html lang="{{code}}"><head><meta charset="utf-8"><style>
            * { box-sizing: border-box; }
            html, body { margin: 0; }
            body { width: {{Width}}px; height: {{Height}}px; position: relative; overflow: hidden; background: #FFFFFF;
                   color: #0F172A; font-family: {{textFont}}; word-break: {{(code == "ja" ? "auto-phrase" : "normal")}}; }
            .g { font-family: "Segoe Fluent Icons", "Segoe MDL2 Assets"; font-weight: 400; line-height: 1; display: inline-block; }
            .nowrap { white-space: nowrap; }
            .header { position: absolute; left: 56px; top: 36px; display: flex; align-items: center; gap: 30px; }
            .title { font-family: "Segoe UI", sans-serif; font-size: 78px; font-weight: 900; color: #101C36; letter-spacing: -1px; line-height: 1.05; }
            .tagline { font-size: {{(cjk ? 27 : 29)}}px; font-weight: 600; color: #47566E; margin-top: 10px; }
            .accent { font-size: {{(cjk ? 27 : 29)}}px; font-weight: 700; color: #2563EB; margin-top: 2px; }
            .keys { position: absolute; right: 168px; top: 58px; display: flex; align-items: center; gap: 10px; padding: 14px 18px;
                    border: 2px solid #D8E0EC; border-radius: 18px; background: #FFFFFF; font-family: "Segoe UI", sans-serif; }
            .key { min-width: 64px; height: 58px; padding: 0 16px; border-radius: 12px; background: #FFFFFF; border: 2px solid #D8E0EC;
                   box-shadow: 0 4px 0 #C9D3E3; font-weight: 700; font-size: 24px; display: grid; place-items: center; }
            .key.hot { background: #2563EB; color: #FFFFFF; border-color: #1D4ED8; box-shadow: 0 4px 0 #1D4ED8; }
            .plus { font-size: 24px; color: #47566E; font-weight: 700; }
            .note { position: absolute; right: {{(cjk ? 26 : 22)}}px; top: 60px; width: {{(cjk ? 150 : 130)}}px; text-align: center;
                    color: #2563EB; transform: rotate(-8deg); line-height: 1.1; white-space: {{(cjk ? "nowrap" : "normal")}};
                    font-family: {{(cjk ? textFont : "\"Ink Free\", \"Segoe Print\", cursive")}}; font-weight: 700; font-size: {{(cjk ? 16 : 27)}}px; }
            .rule { position: absolute; left: 56px; top: 222px; width: 730px; height: 2px; background: #D8E0EC; }
            .features { position: absolute; left: 56px; top: 252px; width: 740px; display: grid; grid-template-columns: 1fr 1fr; column-gap: 34px; row-gap: 18px; }
            .feature { display: flex; gap: 18px; align-items: flex-start; height: 138px; }
            .ftitle { font-size: {{(cjk ? 22 : 24)}}px; font-weight: 700; line-height: 1.25; }
            .fbody { font-size: 17px; font-weight: 500; color: #47566E; line-height: 1.45; margin-top: 6px; }
            .panel { position: absolute; left: 826px; top: 214px; width: 570px; height: 674px; border-radius: 22px;
                     background: linear-gradient(180deg, #EEF3FC 0%, #E4EDFD 100%); border: 2px solid #D8E0EC; }
            .ui { font-family: "Segoe UI", {{textFont}}; }
            .tooltip { position: absolute; right: 26px; top: 22px; background: #2C2C2C; border: 1px solid #3F3F3F; color: #FFFFFF; border-radius: 7px;
                       padding: 7px 11.5px; font-size: 15px; white-space: nowrap; box-shadow: 0 8px 20px rgba(0,0,0,.25); }
            .taskbar { position: absolute; right: 20px; top: 70px; width: 330px; height: 50.6px; background: #1C1C1C; border-radius: 9px;
                       display: flex; align-items: center; justify-content: flex-end; gap: 16px; padding: 0 16px; color: #FFFFFF; font-size: 14px; }
            .tb-icon { width: 37px; height: 37px; border-radius: 5px; background: #3A3A3A; display: grid; place-items: center; }
            .menu { position: absolute; right: 30px; top: 128px; width: 400px; background: #2C2C2C; border: 1px solid #3F3F3F; border-radius: 8px;
                    padding: 4px 0; box-shadow: 0 16px 40px rgba(0,0,0,.35); font-size: 14px; color: #FFFFFF; }
            .row { display: flex; align-items: center; height: 32px; margin: 0 4px; padding-right: 8px; border-radius: 4px; }
            .row.head { height: 34px; font-weight: 700; }
            .row.hot { background: #3D3D3D; }
            .row.bold { font-weight: 700; }
            .row.dim { color: #8A8A8A; }
            .ico { width: 34px; display: grid; place-items: center; }
            .box { width: 22px; height: 22px; display: grid; place-items: center; border-radius: 4px; font-size: 14px; }
            .box.on { background: #2563EB; color: #FFFFFF; }
            .tile { width: 20px; height: 20px; border-radius: 4.5px; background: linear-gradient(180deg,#FFFFFF,#E8EFFC); display: grid; place-items: center; }
            .label { flex: 1; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
            .shortcut { color: #A8A8A8; margin-left: 16px; font-size: 13px; }
            .sep { height: 1px; background: #454545; margin: 4px 8px 4px 38px; }
            .arrow { position: absolute; left: 22px; top: 420px; }
            .toast { position: absolute; right: 20px; bottom: 22px; display: flex; align-items: center; gap: 16px; background: #101C36;
                     border-radius: 9px; overflow: hidden; padding: 16px 46px 16px 26px; box-shadow: 0 18px 44px rgba(16,28,54,.35); }
            .toast .bar { position: absolute; left: 0; top: 0; bottom: 0; width: 6px; background: #2563EB; }
            .toast .cap { font-size: 14px; color: #AFBDD6; }
            .toast .name { font-size: 19.5px; font-weight: 600; color: #FFFFFF; white-space: nowrap; }
            .toast .sub { font-size: 15px; color: #AFBDD6; white-space: nowrap; }
            .toast .x { position: absolute; right: 14px; top: 12px; color: #AFBDD6; font-size: 11.5px; }
            .band { position: absolute; left: 40px; right: 40px; top: 912px; height: 140px; border-radius: 20px; border: 2px solid #D8E0EC;
                    display: grid; grid-template-columns: repeat(4, 1fr); align-items: center; padding: 0 12px; }
            .bitem { display: flex; gap: 16px; align-items: center; padding: 0 20px; height: 96px; border-left: 2px solid #D8E0EC; }
            .bitem:first-child { border-left: none; }
            .btitle { font-size: 19px; font-weight: 700; }
            .bbody { font-size: {{(cjk ? 14 : 15)}}px; font-weight: 500; color: #47566E; line-height: 1.4; margin-top: 4px; }
            </style></head><body>

            <div class="header">{{Logo.Svg(150, Logo.Detail.Full)}}<div>
              <div class="title">{{E(Strings.Get("app.title"))}}</div>
              <div class="tagline">{{E(t.Tagline)}}</div>
              <div class="accent">{{E(t.TaglineAccent)}}</div>
            </div></div>

            <div class="keys"><div class="key">Ctrl</div><div class="plus">+</div><div class="key">Alt</div><div class="plus">+</div><div class="key hot">F9</div></div>
            <div class="note">{{E(t.ExampleHotkey)}}
              <svg width="70" height="40" viewBox="0 0 70 40" style="display:block;margin:4px auto 0">
                <path d="M62 6 C 50 32, 24 36, 8 22" fill="none" stroke="#2563EB" stroke-width="2.5" stroke-linecap="round"/>
                <path d="M8 22 l12 -2 M8 22 l4 11" fill="none" stroke="#2563EB" stroke-width="2.5" stroke-linecap="round"/>
              </svg></div>

            <div class="rule"></div>
            <div class="features">{{features}}</div>

            <div class="panel ui">
              <div class="tooltip">{{E(Strings.Get("tray.currentPrefix") + t.Headphones)}}</div>
              <div class="taskbar">{{Glyph('')}}<div class="tb-icon">{{Logo.Svg(23, Logo.Detail.Simplified, "#FFFFFF", Logo.LightBlue)}}</div>{{Glyph('')}}{{Glyph(MenuGlyphs.Speaker)}}<div>10:30</div></div>
              <div class="menu">
                <div class="row head"><div class="ico"><div class="tile">{{Logo.Svg(17, Logo.Detail.Simplified)}}</div></div>{{E(Strings.Get("app.title"))}}</div>
                <div class="sep"></div>
                {{Row(MenuGlyphs.Speaker, E(t.Speakers))}}
                {{Row(MenuGlyphs.Speaker, E(t.Headphones), cls: "bold", on: true)}}
                {{Row(MenuGlyphs.Speaker, E(t.Monitor + " " + Strings.Get("common.offline")), cls: "dim")}}
                <div class="sep"></div>
                {{Row(MenuGlyphs.Next, E(Strings.Get("tray.cycleNext")), cls: "hot", shortcut: Hotkey(Keys.F9))}}
                {{Row(MenuGlyphs.Previous, E(Strings.Get("tray.cyclePrevious")), shortcut: Hotkey(Keys.F8))}}
                {{Row(MenuGlyphs.Microphone, E(Strings.Get("tray.microphone")), submenu: true)}}
                <div class="sep"></div>
                {{Row(MenuGlyphs.Settings, E(Strings.Get("tray.settings")))}}
                {{Row(MenuGlyphs.Check, E(Strings.Get("common.startWithWindows")), on: true)}}
                {{Row(MenuGlyphs.Info, E(Strings.Get("tray.about")))}}
                {{Row(MenuGlyphs.Exit, E(Strings.Get("tray.exit")))}}
              </div>
              <svg class="arrow" width="60" height="120" viewBox="0 0 60 120">
                <path d="M40 4 C 4 30, 4 80, 34 112" fill="none" stroke="#2563EB" stroke-width="3" stroke-dasharray="7 7" stroke-linecap="round"/>
                <path d="M34 112 l-14 -2 M34 112 l2 -14" fill="none" stroke="#2563EB" stroke-width="3" stroke-linecap="round"/>
              </svg>
              <div class="toast"><div class="bar"></div><span class="g" style="font-size:31px;color:#FFFFFF">&#x{{(int)MenuGlyphs.Speaker:X}};</span>
                <div><div class="cap">{{E(Strings.Get("app.title"))}}</div><div class="name">{{E(t.Headphones)}}</div><div class="sub">{{E(Strings.Get("toast.outputChanged"))}}</div></div>
                <span class="g x">&#x{{(int)MenuGlyphs.Close:X}};</span></div>
            </div>

            <div class="band">{{band}}</div>
            </body></html>
            """;
    }

    private static string Row(char glyph, string label, string cls = "", bool on = false, string? shortcut = null, bool submenu = false)
    {
        string tail = (shortcut is null ? "" : $"<div class=\"shortcut\">{shortcut}</div>")
            + (submenu ? Glyph(MenuGlyphs.ChevronRight).Replace("class=\"g\"", "class=\"g\" style=\"font-size:10px\"") : "");
        return $"<div class=\"row {cls}\"><div class=\"ico\"><div class=\"box{(on ? " on" : "")}\">{Glyph(glyph)}</div></div><div class=\"label\">{label}</div>{tail}</div>";
    }

    private const string Stroke = "fill=\"none\" stroke=\"#2563EB\" stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\"";

    // Line icons for the feature grid (48×48 grid, stroke only).
    private static string FeatureSvg(FeatureIcon icon)
    {
        string inner = icon switch
        {
            FeatureIcon.Bolt => $"<path {Stroke} d=\"M27 4 L10 27 h13 l-3 17 L38 20 H25 z\"/>",
            FeatureIcon.List => $"<g {Stroke}><circle cx=\"9\" cy=\"12\" r=\"3\"/><circle cx=\"9\" cy=\"24\" r=\"3\"/><path d=\"M9 33 v6 M6 36 l3 3 3-3\"/><path d=\"M18 12 h24 M18 24 h24 M18 37 h24\"/></g>",
            FeatureIcon.Mic => $"<g {Stroke}><rect x=\"17\" y=\"5\" width=\"14\" height=\"24\" rx=\"7\"/><path d=\"M10 22 a14 14 0 0 0 28 0 M24 36 v7 M16 43 h16\"/></g>",
            FeatureIcon.Toast => $"<g {Stroke}><rect x=\"5\" y=\"9\" width=\"38\" height=\"30\" rx=\"5\"/><path d=\"M12 30 h14\"/><circle cx=\"35\" cy=\"18\" r=\"3\"/></g>",
            FeatureIcon.Mouse => $"<g {Stroke}><rect x=\"12\" y=\"5\" width=\"24\" height=\"38\" rx=\"12\"/><path d=\"M24 5 v14 M12 19 h24\"/></g>",
            FeatureIcon.Globe => $"<g {Stroke}><circle cx=\"24\" cy=\"24\" r=\"19\"/><path d=\"M5 24 h38 M24 5 c-8 8 -8 30 0 38 M24 5 c8 8 8 30 0 38\"/></g>",
            FeatureIcon.Download => $"<g {Stroke}><path d=\"M24 5 v22 M15 18 l9 9 9-9\"/><path d=\"M6 28 v10 a3 3 0 0 0 3 3 h30 a3 3 0 0 0 3-3 V28\"/></g>",
            FeatureIcon.Phone => $"<path {Stroke} d=\"M14 6 h6 l3 9 -5 3 a22 22 0 0 0 12 12 l3-5 9 3 v6 a4 4 0 0 1-4 4 C22 38 10 26 10 10 a4 4 0 0 1 4-4 z\"/>",
            _ => "",
        };
        return $"<svg width=\"54\" height=\"54\" viewBox=\"0 0 48 48\" style=\"display:block;flex-shrink:0\">{inner}</svg>";
    }

    // Icons for the bottom band: light, private, plays well, simple.
    private static string BandSvg(int index)
    {
        const string s = "fill=\"none\" stroke=\"#101C36\" stroke-width=\"2.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"";
        string inner = index switch
        {
            0 => $"<path {s} d=\"M38 8 C18 8 8 20 10 38 C28 40 40 30 38 8 z M10 38 L26 22\"/>",
            1 => $"<g {s}><path d=\"M24 4 L40 10 v12 c0 11 -7 18 -16 22 C15 40 8 33 8 22 V10 z\"/><path d=\"M17 24 l5 5 9-10\"/></g>",
            2 => $"<path {s} d=\"M8 18 h8 a4 4 0 1 1 8 0 h8 v8 a4 4 0 1 1 0 8 v8 H8 v-8 a4 4 0 1 0 0-8 z\"/>",
            _ => $"<g {s}><rect x=\"6\" y=\"9\" width=\"36\" height=\"30\" rx=\"4\"/><path d=\"M6 17 h36\"/></g>",
        };
        return $"<svg width=\"52\" height=\"52\" viewBox=\"0 0 48 48\" style=\"display:block;flex-shrink:0\">{inner}</svg>";
    }
}
