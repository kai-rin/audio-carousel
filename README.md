# Audio Carousel

[English](README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md)

![Audio Carousel](docs/images/hero-en.png)

A lightweight Windows tray utility that switches the system default audio
output device through a list you choose, with a single global hotkey.

Inspired by [PeekDesktop](https://github.com/shanselman/PeekDesktop): no
installer, single executable, portable configuration.

---

## Features

- **One hotkey, one switch.** Press your hotkey from any app — game,
  meeting, browser — and the system default audio output jumps to the
  next device on your list. No need to open the Sound settings panel.
- **Pick exactly which devices to cycle.** A typical Windows PC —
  desktop or laptop — exposes many outputs: the motherboard or chassis
  analog jacks (speakers, headphone-out, line-out), monitor audio
  routed over DisplayPort or HDMI, USB DACs and headset dongles, every
  Bluetooth headset ever paired, virtual audio cables, and so on.
  Audio Carousel only rotates through the ones you selected, so the
  cycle stays short and predictable.
- **Forward and back.** One hotkey steps to the next device, an
  optional second one steps back to the previous device.
- **Microphones too.** Keep a separate list of recording devices (e.g.
  desk mic and headset mic) and switch between them with their own
  hotkey or from the tray's *Microphone* submenu.
- **Visual confirmation.** A toast appears at the bottom-right of the
  monitor your mouse is on, showing the new device's name (you can turn
  it off). The tray icon's tooltip always shows the current device.
- **Calls stay where you want them.** By default the device used for
  calls (Teams, Discord, Zoom) follows the switch too; untick one
  option in Settings to keep your headset for calls while music and
  games move around.
- **Tray-resident, mouse-friendly.** A left-click on the tray icon opens
  Settings, or — if you turn it on — cycles like the hotkey. Right-click
  for a menu that lists your devices — click one to switch to it
  directly, plus Next / Previous device, Settings, About, Exit.
- **Flexible hotkeys.** Modifier combinations (`Ctrl+Alt+A`) and the
  rarely-used function keys (`F13`–`F24`, common on programmable
  keyboards and macro pads) all work.
- **10-language UI.** English, 日本語, 简体中文, 繁體中文, Español,
  Français, Deutsch, Português (Brasil), Русский, 한국어. Auto-detects
  from your Windows display language; switch manually any time in
  Settings.
- **Plain-text JSON config.** Settings live in `audio-carousel.json`
  next to the .exe. Open it in any text editor to see exactly what's
  saved, change a hotkey or device name without launching the UI,
  version-control it with git, or generate it from a script. The
  Settings window just edits the same file.
- **Optional auto-start.** "Start with Windows" writes only to the
  current user's `HKCU\...\Run` key — no admin rights, no service, no
  scheduled task.
- **No installer, no admin, no .NET runtime.** Drop the .exe in any
  folder and run it. Self-contained build means you don't have to
  install anything else.
- **Offline-device aware.** Devices that are unplugged, powered off, or
  not currently connected appear grayed out in Settings; the cycle
  skips them automatically. This is especially handy for Bluetooth
  headphones — leave them in the cycle list permanently, and they'll
  rejoin the rotation only when they're actually connected.

## Download and run

1. Download `AudioCarousel-<version>-win-x64.zip` from the
   [Releases](https://github.com/kai-rin/audio-carousel/releases) page
   and extract it to any folder you like (e.g.
   `C:\Tools\AudioCarousel\`). The zip holds `AudioCarousel.exe` and
   the license files.

   > **Optional: verify the download.** Each release also lists a
   > `.sha256` checksum, and the build is attested by GitHub Actions:
   > `gh attestation verify AudioCarousel-<version>-win-x64.zip --repo kai-rin/audio-carousel`

2. Double-click `AudioCarousel.exe` to launch.

   > **First-run note:** because the binary is unsigned, Windows
   > SmartScreen may show "Windows protected your PC". Click
   > **More info → Run anyway**. Alternatively, before launching:
   > right-click the .exe → **Properties** → check **Unblock** → **OK**.
   > See *Disclaimer* below for why this happens.

3. The tray icon appears (near the clock) and the **Settings** window
   opens automatically on first run.

## First-time setup

In the Settings window:

1. **Add the audio outputs you want to cycle.** Click *Add device* and
   pick from the dropdown. Typical choices: speakers, headset, monitor
   speakers over HDMI/DisplayPort, Bluetooth earbuds. Two devices is
   enough to make the hotkey useful.
2. **Reorder if you like.** Use *Move up* / *Move down* (or
   `Ctrl+↑` / `Ctrl+↓`) to set the cycle order; `Delete` removes the
   selected device.
3. **Set a hotkey.** Click the *Next device* field (or Tab to it and
   press `Enter`), then press the keys you want — e.g. `F16`,
   `Ctrl+Alt+A`, `Ctrl+Shift+F9`. `F1`–`F24` work on their own; any
   other key needs `Ctrl`, `Alt` or `Win` so ordinary typing is never
   hijacked. `Esc` cancels, `Backspace` (or *Clear*) removes the hotkey.
   Optionally set a *Previous device* hotkey the same way.
4. **(Optional) UI language.** Defaults to *Auto* (follows Windows). Pick
   a specific language if you prefer.
5. **(Optional) Start with Windows.** Tick the checkbox to launch Audio
   Carousel automatically when you sign in.
6. **(Optional) Microphones.** Switch the list to *Recording*, add the
   microphones you want to cycle, and set a *Next microphone* hotkey.
   The calls option below applies to microphones as well.
7. **(Optional) Calls device.** Untick *Also switch the communications
   devices used for calls* if calls should stay on the devices you picked
   in Windows.
8. **(Optional) Toast and left-click.** Turn off the switch
   notification, or let a left-click on the tray icon switch devices
   (off by default, so a stray click can't change your audio
   mid-meeting — it opens Settings instead).
9. Click **OK**. A notification points out the tray icon and how to use
   it. On Windows 11 new tray icons start in the overflow flyout (the
   `^` next to the clock) — drag it onto the taskbar to keep it
   visible.

## Daily use

- **Press the hotkey** from any app → audio jumps to the next device
  on your list (the *Previous device* hotkey goes back). A toast at the
  bottom-right of the monitor your mouse is on confirms the switch.
- **Left-click the tray icon** opens Settings — or switches like the
  hotkey, if you turned that on. (Configs from before this option keep
  switching on left-click.)
- **Hover the tray icon** to see the current output — it stays accurate
  even when Windows or another app changes the device.
- **Launch `AudioCarousel.exe` again** to bring up Settings of the copy
  that is already running.
- **Right-click the tray icon** for the menu:
  - **Your registered devices** — the current one is checked; click any
    other to switch to it directly (offline devices are grayed out)
  - **Next device** / **Previous device** — same as the hotkeys
  - **Microphone** — your registered microphones and *Next microphone*
    (shown once you've added microphones)
  - **Settings** — reopen the settings window
  - **Start with Windows** — toggle without opening Settings
  - **About** — version info
  - **Exit** — quit the app

## Configuration and data location

- **Settings file:** `audio-carousel.json` in the same folder as the
  .exe. Edit it by hand if you like, or just use Settings. Saves go
  through a short-lived `audio-carousel.json.tmp`. A file that isn't
  valid JSON is kept as `audio-carousel.json.bak` and defaults are
  used; individual bad entries are ignored. If the file can't be read
  at all (e.g. another program has it locked), the app runs with
  defaults for that session and never overwrites your file.
- **Registry:** Audio Carousel writes one value under
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` when
  *Start with Windows* is on. Turning it off removes the entry. If you
  disabled the app in Task Manager's *Startup apps*, Audio Carousel shows
  it as off; turning it on again from the app also clears that
  Task Manager flag (`...\Explorer\StartupApproved\Run`). Only the
  current user is affected. No admin rights are required.
- **Updates:** the app never goes online, so it can't tell you about new
  versions. *Watch → Custom → Releases* on the GitHub repository to get
  notified.
- **No background services, no scheduled tasks, no telemetry.** The
  app is just the one .exe.

## Uninstall

1. Right-click the tray icon → **Settings** → uncheck *Start with
   Windows* → **OK** (this removes the registry entry).
2. Right-click the tray icon → **Exit**.
3. Delete the folder containing `AudioCarousel.exe` and
   `audio-carousel.json`.

If you skip step 1, you can also clean up later by deleting the
`AudioCarousel` value under
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run` in `regedit`.

## System requirements

- Windows 10 1809 or later, or Windows 11
- x64
- No admin rights required
- No .NET runtime required (self-contained build — that's why the exe
  is ~111 MB; the zip download is ~45 MB)

## Known limitations

- **Identical device names.** Some drivers (notably NVIDIA HDMI /
  DisplayPort audio) assign new device IDs after a reboot, so Audio
  Carousel re-finds devices by name. Two outputs with exactly the same
  name (e.g. two identical monitors) may swap places in that case —
  rename one in Windows Sound settings to avoid it.
- **Reserved hotkeys.** Combinations Windows keeps for itself (such as
  `Win+L`) can't be registered; Settings tells you when a combination
  is unavailable.

## Disclaimer

Audio Carousel calls `IPolicyConfig`, an undocumented Windows COM
interface, to switch the default audio endpoint. This is the same
technique used by many similar tools (SoundSwitch, EarTrumpet, NirCmd,
...) and has been stable from Windows Vista through Windows 11. However,
since the API is unofficial, Microsoft could change or remove it in a
future Windows update without notice. If a future Windows update breaks
Audio Carousel, please open an issue.

The released binary is not code-signed (code-signing certificates carry
an ongoing cost, and we've held off on adopting one while the project
remains free). Windows SmartScreen will warn on first launch until the
binary's reputation builds up — see the *First-run note* under
*Download and run*.

## Security

See [SECURITY.md](SECURITY.md) for the threat model and how to report
security issues.

## License

[MIT](LICENSE) — © 2026 Kairin.
Bundled third-party components and their licenses are listed in
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

---

# For developers

Everything below is for building from source. End users do not need any
of this — just download the release binary above.

## Tech stack

- C# / .NET 10 (LTS) / WinForms (`net10.0-windows`)
- xUnit for tests
- `NAudio.Wasapi` (`NAudio.CoreAudioApi` namespace) plus an inline
  `IPolicyConfig` COM declaration in `src/AudioCarousel/Audio/`

## Build and test

Requires the .NET 10 SDK on Windows.

```bash
dotnet build
dotnet test
dotnet format          # fixes whitespace/EOL per .editorconfig
```

## Publish single-file executable

```powershell
pwsh ./scripts/publish.ps1
```

Output: `publish/AudioCarousel.exe` (~111 MB, self-contained). The
script refuses to run while `publish/AudioCarousel.exe` is running, and
keeps an existing `publish/audio-carousel.json` across the rebuild.

`scripts/publish.ps1` is the **only** supported way to produce a
release exe. The flags are deliberately tuned; do not assemble
`dotnet publish` arguments by hand.

> **Binary size note.** Windows Forms is incompatible with NativeAOT
> (`NETSDK1175`), and trimming strips runtime COM interop machinery
> that `NAudio.CoreAudioApi` depends on. The script therefore uses an
> untrimmed self-contained JIT single-file publish. The ~111 MB size is
> the price of "drop the .exe anywhere and run, no .NET runtime
> needed" — this tradeoff was made deliberately.

## Project layout

See [CLAUDE.md](CLAUDE.md) for the architecture map, conventions, and
critical gotchas (don't enable AOT/trimming, atomic config writes,
test isolation rules, etc.). Design notes live under
`docs/superpowers/specs/`.

