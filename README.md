# Audio Carousel

A lightweight Windows tray utility that switches the system default audio
output device through a configured list with a single global hotkey.

Inspired by [PeekDesktop](https://github.com/shanselman/PeekDesktop): no
installer, single executable, portable configuration.

## Usage

1. Download `AudioCarousel.exe` from the
   [Releases](https://github.com/kai-rin/audio-carousel/releases) page and
   put it in any folder.
2. Double-click to launch.

   > **First-run note:** because the binary is unsigned, Windows SmartScreen
   > may show "Windows protected your PC". Click **More info** → **Run
   > anyway**. Alternatively, right-click the .exe → Properties → check
   > **Unblock** → OK before launching.

3. The tray icon appears and the Settings window opens automatically on
   first run.
4. Add the audio output devices you want to cycle, set a hotkey
   (e.g. `F16`, `Ctrl+Alt+A`), and click OK.
5. Press the hotkey to cycle through devices. A toast at the bottom-right of
   the active monitor shows the new device name.
6. Optionally enable "Start with Windows" from the tray menu or settings.

Configuration lives in `audio-carousel.json` next to the executable. The
only registry write is `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
when "Start with Windows" is on, and only the current user is affected.
**No admin rights are required.**

## Build

Requires .NET 9 SDK on Windows.

```bash
dotnet build
dotnet test
```

## Publish single-file executable

```powershell
pwsh ./scripts/publish.ps1
```

Output: `publish/AudioCarousel.exe`.

> **Binary size note:** Windows Forms is incompatible with NativeAOT
> (`NETSDK1175`), and trimming strips runtime COM interop machinery that
> `NAudio.CoreAudioApi` depends on. The script therefore uses
> **untrimmed self-contained JIT single-file publish**, producing a
> ~108 MB executable. The size is the price of "drop the .exe anywhere
> and run, no .NET runtime needed."

## System requirements

- Windows 10 1809 or later, or Windows 11
- x64
- No admin rights

## Disclaimer

Audio Carousel calls `IPolicyConfig`, an **undocumented Windows COM
interface**, to switch the default audio endpoint. This is the same
technique used by many similar tools (SoundSwitch, EarTrumpet, NirCmd, ...)
and has been stable from Windows Vista through Windows 11. However, since
the API is unofficial, Microsoft could change or remove it in a future
Windows update without notice. If a future Windows update breaks Audio
Carousel, please open an issue.

The released binary is not code-signed (signing certificates cost real
money for a free tool). Windows SmartScreen will warn on first launch
until the binary's reputation builds up — see the *First-run note* under
*Usage*.

## Security

See [SECURITY.md](SECURITY.md) for the threat model and how to report
security issues.

## License

[MIT](LICENSE) — © 2026 kai-rin
