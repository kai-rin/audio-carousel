namespace BrandAssets;

// Marketing copy for the README hero image (en / ja / zh-Hans).
// UI labels (menu items, toast text, hotkey names) are NOT here: Hero.cs reads
// them from the app assembly (Strings / HotkeyParser / MenuGlyphs), so they
// can't drift. Every claim below must stay backed by the app:
//
//  both ways ........ CycleController.CyclePrevious, ConfigSchema.HotkeyPrevious
//  pick devices ..... ConfigSchema.Devices; offline skip: CycleController uses active endpoints only
//  microphones ...... CycleTarget.Input, ConfigSchema.HotkeyInput
//  see what changed . ToastWindow / ToastContent, TrayIcon.SetCurrentDeviceLabel
//  tray-friendly .... TrayIcon device rows, ConfigSchema.LeftClickCycles (new installs: false)
//  calls stay put ... ConfigSchema.SwitchCommunications
//  languages/theme .. Strings (10 langs); Program: Application.SetColorMode(System);
//                     ToastWindow light/dark themes; FluentMenuRenderer light/dark palettes
//  portable ......... scripts/publish.ps1 (self-contained single file), ConfigStore next to the exe,
//                     StartupRegistration needs no elevation
//  light ............ measured 2026-09-27 (publish build, 4 fresh starts): working set 53.8–55.3 MB,
//                     0 ms CPU over 60 s idle (twice)
//  private .......... no network APIs in src (grep HttpClient/Socket/System.Net), SECURITY.md
//  plays well ....... switches the Windows default endpoint via IPolicyConfig

public enum FeatureIcon { Bolt, List, Mic, Toast, Mouse, Phone, Globe, Download }

public sealed record Feature(FeatureIcon Icon, string Title, string Body);

public sealed record BandItem(string Title, string Body);

public sealed record HeroCopy(
    string Tagline,
    string TaglineAccent,
    string ExampleHotkey,
    Feature[] Features,
    BandItem[] Band,
    // Sample devices, named the way Windows names them in that display language.
    string Speakers,
    string Headphones,
    string Monitor)
{
    public static readonly HeroCopy En = new(
        "Switch your default audio output — and mic.",
        "One hotkey. Anywhere.",
        "Example hotkey",
        new Feature[]
        {
            new(FeatureIcon.Bolt, "One hotkey, both ways", "Jump to the next device from any app. A second hotkey goes back."),
            new(FeatureIcon.List, "Pick which devices", "Cycle only the outputs you choose. Offline ones are skipped."),
            new(FeatureIcon.Mic, "Microphones too", "A separate mic list with its own hotkey."),
            new(FeatureIcon.Toast, "See what changed", "A toast names the new device; the tooltip shows the current one."),
            new(FeatureIcon.Mouse, "Tray-friendly", "Right-click to pick a device. Left-click opens Settings (or switches)."),
            new(FeatureIcon.Phone, "Calls stay put", "Optional: keep Teams, Discord or Zoom on your headset."),
            new(FeatureIcon.Globe, "Speaks your language", "10 languages. Follows your Windows theme, light or dark."),
            new(FeatureIcon.Download, "Portable", "One .exe — no installer, admin rights or .NET. Settings in audio-carousel.json."),
        },
        new BandItem[]
        {
            new("Light", "Idle: 0% CPU, ~55 MB RAM"),
            new("Private", "No network access. No telemetry."),
            new("Plays well with others", "Changes the Windows default — apps that use it follow along."),
            new("Simple by design", "Do one thing. Do it well."),
        },
        "Speakers (Realtek(R) Audio)",
        "Headphones (WH-1000XM5)",
        "Monitor (NVIDIA High Definition Audio)");

    public static readonly HeroCopy Ja = new(
        "既定の音声出力とマイクを切り替える。",
        "ホットキー1つで、どこからでも。",
        "例：ホットキー",
        new Feature[]
        {
            new(FeatureIcon.Bolt, "ホットキーで次へも前へも", "どのアプリからでも次のデバイスへ。2つ目のホットキーで前に戻れます。"),
            new(FeatureIcon.List, "巡回するデバイスを選べる", "選んだ出力だけを巡回。未接続のデバイスは飛ばします。"),
            new(FeatureIcon.Mic, "マイクも切り替え", "マイク用の一覧と専用のホットキー。"),
            new(FeatureIcon.Toast, "切り替えが見える", "トーストで新しいデバイス名を表示。ツールチップには現在のデバイス。"),
            new(FeatureIcon.Mouse, "トレイから操作", "右クリックでデバイスを選択。左クリックで設定（切り替えにも変更可）。"),
            new(FeatureIcon.Phone, "通話はそのまま（任意）", "Teams・Discord・Zoom はヘッドセットのままにできます。"),
            new(FeatureIcon.Globe, "10言語・ライト／ダーク", "Windows の表示言語とテーマに追従。"),
            new(FeatureIcon.Download, "ポータブル・導入不要", "exe 1つ。インストーラ・管理者権限・.NET は不要。設定は audio-carousel.json。"),
        },
        new BandItem[]
        {
            new("軽量", "待機中は CPU 0%・メモリ約 55 MB"),
            new("プライバシー", "ネットワーク通信なし。テレメトリなし。"),
            new("他のアプリと協調", "既定デバイスを切り替え。既定を使うアプリは自動で追従。"),
            new("シンプル設計", "やることは1つ。それを確実に。"),
        },
        "スピーカー (Realtek(R) Audio)",
        "ヘッドホン (WH-1000XM5)",
        "モニター (NVIDIA High Definition Audio)");

    public static readonly HeroCopy ZhHans = new(
        "切换默认音频输出和麦克风。",
        "一个热键，随处可用。",
        "示例热键",
        new Feature[]
        {
            new(FeatureIcon.Bolt, "热键可进可退", "在任何应用中切到下一个设备；第二个热键可以返回。"),
            new(FeatureIcon.List, "自选循环设备", "只在选中的输出之间循环，未连接的设备自动跳过。"),
            new(FeatureIcon.Mic, "麦克风也能切换", "独立的麦克风列表和专用热键。"),
            new(FeatureIcon.Toast, "切换一目了然", "提示显示新设备名称，工具提示显示当前设备。"),
            new(FeatureIcon.Mouse, "托盘操作", "右键选择设备。左键打开设置（也可改为切换）。"),
            new(FeatureIcon.Phone, "通话保持不变（可选）", "Teams、Discord、Zoom 可以继续使用耳机。"),
            new(FeatureIcon.Globe, "10 种语言 · 浅色/深色", "跟随 Windows 的显示语言和主题。"),
            new(FeatureIcon.Download, "便携，免安装", "只有一个 exe，无需安装程序、管理员权限或 .NET。设置保存在 audio-carousel.json。"),
        },
        new BandItem[]
        {
            new("轻量", "空闲时 CPU 0% · 内存约 55 MB"),
            new("隐私", "不联网，无遥测。"),
            new("兼容其他应用", "切换 Windows 默认设备，使用默认设备的应用随之切换。"),
            new("简单设计", "只做一件事，并把它做好。"),
        },
        "扬声器 (Realtek(R) Audio)",
        "耳机 (WH-1000XM5)",
        "显示器 (NVIDIA High Definition Audio)");
}
