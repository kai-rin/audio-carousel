# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Audio Carousel — Windows tray utility that cycles the system default audio output device via a global hotkey. Single-developer, MIT-licensed, OSS on GitHub.

Spec: `docs/superpowers/specs/2026-04-25-audio-carousel-design.md`

## Tech

- C# / .NET 9 / WinForms (`net9.0-windows`), self-contained single-file publish (~108 MB)
- xUnit for tests, NuGet with `packages.lock.json` (use `--locked-mode` in CI)
- `NAudio.Wasapi` (provides `NAudio.CoreAudioApi` namespace) + inline `IPolicyConfig` COM declaration

## Commands

```bash
dotnet build                 # dev build (JIT, fast)
dotnet test                  # 87 unit tests, runs <1s
dotnet format                # fix EOL/whitespace per .editorconfig (CI runs --verify-no-changes)
pwsh ./scripts/publish.ps1   # produces publish/AudioCarousel.exe (~108 MB)
```

`scripts/publish.ps1` is the **only** supported way to produce a release exe. Do not construct `dotnet publish` flags by hand.

## Critical gotchas — do not change without reading the why

- **Do not enable `PublishAot`.** WinForms is incompatible with NativeAOT (`NETSDK1175`). The csproj has a conditional `<PublishAot Condition="'$(IsPublishing)' == 'true'">true</PublishAot>` line, but `publish.ps1` deliberately overrides it with `-p:PublishAot=false`. Leave both as they are.
- **Do not enable `PublishTrimmed`.** Trimming strips runtime COM interop machinery. `NAudio.CoreAudioApi.MMDeviceEnumerator` then throws `System.NotSupportedException: Built-in COM has been disabled` at the first enumerate or dispose. We tried `BuiltInComInteropSupport=true`, `_SuppressWinFormsTrimError=true`, `TrimmerRootAssembly` — all insufficient. Untrimmed is the only known-working configuration.
- **The 108 MB binary size is intentional**, not a regression to fix. Self-contained is the price of "drop the .exe anywhere and run, no .NET runtime needed." Switching to framework-dependent saves only ~1.5 MB resident memory in exchange for breaking the no-runtime-required promise; this tradeoff was explicitly considered and rejected.
- **`IPolicyConfig` is undocumented but stable** Windows COM, used by SoundSwitch / EarTrumpet / NirCmd. Keep `src/AudioCarousel/Audio/PolicyConfig.cs` as-is — the GUIDs and method order are load-bearing.

## Conventions

- **All user-facing strings go through `src/AudioCarousel/I18n/Strings.cs`** (10-language table: en, ja, zh-Hans, zh-Hant, es, fr, de, pt-BR, ru, ko). Use the `M(...)` helper to supply all 10 values in order, or `Same("X")` for proper nouns / language self-names. Never hardcode UI text in WinForms code; missing per-language entries fall back to English.
- **`.editorconfig` enforces CRLF line endings** for all source files. On Linux/macOS, set `git config core.autocrlf input` and run `dotnet format` after checkout if CI complains about EOL.
- **WFO1000 warnings**: properties on custom WinForms controls that return non-trivial types need `[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]` (e.g., `HotkeyTextBox.Value`).
- **Atomic config writes**: `ConfigStore.SaveInternal` writes to `<path>.tmp` then `File.Replace` with a 5-attempt retry (antivirus / search-indexer can transiently lock the file). Do not "simplify" this back to a direct write.
- **Tests that touch the real registry** (`StartupRegistrationTests`) use a unique value name (`AudioCarousel-TEST-<guid>`) and clean up in `Dispose`. Follow this pattern for any new registry-touching tests.
- **Tests that mutate `Strings._current`** must be in `[Collection("StringsState")]` (defined in `StringsTests.cs`). xUnit parallelizes test classes by default; without the collection, classes that call `Strings.SetLanguage` race against each other and produce flaky failures.

## Workflow

- **Branch**: `main` only (no feature branches for this project).
- **README is multilingual**: `README.md` (en), `README.ja.md`, `README.zh-Hans.md`. The language-switcher line `[English](README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md)` is identical in all three. When updating user-facing content, update all three or note the others as out-of-date.
- **Commit messages**: Japanese (per global preference). Code identifiers, comments, and file/symbol names stay English.
- **`git push` is gated by explicit user request.** This is a private repo; do not push automatically after a commit. Wait for the user to ask.
- **Publish smoke test**: after running `publish.ps1`, the exe lives in `publish/`. If the previous instance is still running, `Remove-Item publish/` fails. Always `taskkill.exe //F //IM AudioCarousel.exe` first. Smoke tests also write a `publish/audio-carousel.json` that should be cleaned up before commit/release.
- **Releases**: tag `v*.*.*` and push the tag. `.github/workflows/release.yml` strips the `v` prefix and passes the version as `./scripts/publish.ps1 -Version <ver>`, which forwards `-p:Version=<ver>` to `dotnet publish` to override the `1.0.0-dev` placeholder in csproj. The exe is attached to the GitHub Release automatically.
