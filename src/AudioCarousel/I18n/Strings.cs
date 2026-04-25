namespace AudioCarousel.I18n;

public static class Strings
{
    private static Language _current = Language.English;

    private static readonly Dictionary<string, (string en, string ja)> Table = new()
    {
        ["app.title"]                 = ("Audio Carousel",                        "Audio Carousel"),
        ["tray.title"]                = ("Audio Carousel",                        "Audio Carousel"),
        ["tray.currentPrefix"]        = ("Current: ",                             "現在: "),
        ["tray.currentNone"]          = ("(no device selected)",                  "(デバイス未選択)"),
        ["tray.cycleNext"]            = ("Cycle next",                            "次のデバイスへ"),
        ["tray.settings"]             = ("Settings...",                           "設定..."),
        ["tray.startWithWindows"]     = ("Start with Windows",                    "Windows起動時に開始"),
        ["tray.about"]                = ("About",                                 "バージョン情報"),
        ["tray.exit"]                 = ("Exit",                                  "終了"),

        ["settings.title"]            = ("Audio Carousel — Settings",             "Audio Carousel — 設定"),
        ["settings.titleFirstRun"]    = ("Audio Carousel — Settings (First-time setup)", "Audio Carousel — 設定 (初回セットアップ)"),
        ["settings.hotkey"]           = ("Hotkey:",                               "ホットキー:"),
        ["settings.hotkeyHint"]       = ("(Click and press a key combination)",   "(クリックしてキーを押してください)"),
        ["settings.hotkeyCapturing"]  = ("Press a key combination... (Esc cancels)", "キー組み合わせを押してください... (Escでキャンセル)"),
        ["settings.hotkeyClear"]      = ("Clear",                                 "クリア"),
        ["settings.hotkeyEmpty"]      = ("(none)",                                "(未設定)"),
        ["settings.cycleDevices"]     = ("Cycle devices (in order):",             "切替デバイス (順序):"),
        ["settings.addDevice"]        = ("Add device",                            "デバイス追加"),
        ["settings.remove"]           = ("Remove",                                "削除"),
        ["settings.moveUp"]           = ("Up",                                    "上へ"),
        ["settings.moveDown"]         = ("Down",                                  "下へ"),
        ["settings.language"]         = ("Language:",                             "言語:"),
        ["settings.languageAuto"]     = ("Auto",                                  "自動"),
        ["settings.languageEn"]       = ("English",                               "English"),
        ["settings.languageJa"]       = ("日本語",                                "日本語"),
        ["settings.startWithWindows"] = ("Start with Windows",                    "Windows起動時に開始"),
        ["settings.ok"]               = ("OK",                                    "OK"),
        ["settings.cancel"]           = ("Cancel",                                "キャンセル"),
        ["settings.offline"]          = ("(offline)",                             "(未接続)"),
        ["settings.noNewDevices"]     = ("(no new devices available)",            "(追加可能なデバイスがありません)"),

        ["error.alreadyRunning"]      = ("Audio Carousel is already running.",    "Audio Carouselはすでに起動しています。"),
        ["error.hotkeyInUse"]         = ("Hotkey already in use by another application.", "このホットキーは他のアプリに使用されています。"),
        ["error.switchFailed"]        = ("Failed to switch device",               "デバイス切替に失敗しました"),
        ["error.noDeviceAvailable"]   = ("No registered audio device available",  "切替可能なデバイスがありません"),
        ["error.configCorrupted"]     = ("Configuration file was corrupted. A backup was saved as audio-carousel.json.bak and defaults are now in use.", "設定ファイルが破損していました。audio-carousel.json.bakにバックアップを保存し、デフォルト設定で起動します。"),
        ["error.unhandled"]           = ("An unexpected error occurred:",         "予期しないエラーが発生しました:"),

        ["about.body"]                = ("Audio Carousel — switch the default audio output device with a global hotkey.\n\nhttps://github.com/kai-rin/audio-carousel", "Audio Carousel — グローバルホットキーで音声出力デバイスを切り替えます。\n\nhttps://github.com/kai-rin/audio-carousel"),
    };

    public static void SetLanguage(Language lang) => _current = lang;
    public static Language Current => _current;

    public static string Get(string key)
    {
        if (!Table.TryGetValue(key, out var entry)) return key;
        return _current == Language.Japanese ? entry.ja : entry.en;
    }

    public static Language ResolveLanguage(string configValue, bool currentUiCultureIsJapanese)
    {
        return configValue switch
        {
            "ja" => Language.Japanese,
            "en" => Language.English,
            _    => currentUiCultureIsJapanese ? Language.Japanese : Language.English,
        };
    }

    public static bool IsCurrentUiCultureJapanese() =>
        System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja";
}
