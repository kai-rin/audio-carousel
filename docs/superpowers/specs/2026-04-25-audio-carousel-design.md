# Audio Carousel — Design Spec

- **Date:** 2026-04-25
- **Status:** Draft (awaiting user approval)
- **Inspiration:** [PeekDesktop](https://github.com/shanselman/PeekDesktop) (no installer, single .exe, lightweight tray app)

## 1. Goal

A Windows tray utility that cycles the system default audio output device through a user-configured list with a single global hotkey, showing a brief on-screen toast on each switch.

**Example flow:** Press `F16` → switch to "LG ULTRAGEAR+" → press again → "iFi (by AMR) HD+ USB Audio" → press again → loop back.

## 2. Non-functional Requirements (most important)

- **No installer.** Single `AudioCarousel.exe`, drop anywhere, run.
- **Portable.** Config lives next to the exe. No `%APPDATA%` writes (Run-key in `HKCU` is the only registry touch, and only when "Start with Windows" is on).
- **Lightweight.** Target resident memory ≤ 30 MB. Target binary size ≤ 15 MB.
- **No telemetry, no logs, no auto-update.**
- **No admin rights required** at any point.
- **YAGNI ruthlessly.** Match PeekDesktop's level of restraint.

## 3. Tech Stack

- **Language/Runtime:** C# / .NET 9
- **UI Framework:** WinForms
- **Publish mode:** `PublishSingleFile=true`, `PublishAot=true`, `SelfContained=true`, `win-x64`
- **External NuGet deps:**
  - `NAudio.CoreAudioApi` — `IMMDeviceEnumerator` wrapper for device enumeration
  - `IPolicyConfig` COM interface declared inline (`[ComImport]`) — not in NAudio
- **AOT constraints:**
  - JSON: `System.Text.Json` source generator (`[JsonSerializable]`)
  - No reflection-based serialization or dynamic loading

## 4. Architecture

```
┌─────────────────────────────────────────────────────┐
│ AudioCarousel.exe (NativeAOT, single-file)          │
│                                                     │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────┐  │
│  │ TrayIcon    │  │ HotkeyHost   │  │ ToastWindow│  │
│  │ (NotifyIcon)│  │ (NativeWindow│  │ (frameless │  │
│  │             │  │  + RegHotKey)│  │  TopMost)  │  │
│  └──────┬──────┘  └──────┬───────┘  └──────▲─────┘  │
│         │                │                  │       │
│         │  ┌─────────────▼──────────────┐   │       │
│         └─▶│  CycleController           ├───┘       │
│            │  - currentIndex            │            │
│            │  - cycle() → switch+toast  │            │
│            └──────────┬─────────────────┘            │
│                       │                              │
│            ┌──────────▼────────────┐                 │
│            │  AudioDeviceService   │                 │
│            │  - enumerate()        │                 │
│            │  - setDefault(id)     │                 │
│            │  (NAudio.CoreAudioApi │                 │
│            │   + IPolicyConfig)    │                 │
│            └───────────────────────┘                 │
│                                                      │
│            ┌─────────────────┐  ┌────────────────┐   │
│            │ SettingsWindow  │  │ ConfigStore    │   │
│            │ (WinForms)      │◀─▶│ (JSON, portable)│  │
│            └─────────────────┘  └────────────────┘   │
└──────────────────────────────────────────────────────┘
```

### 4.1 Component Responsibilities

| Component | Single Purpose |
|---|---|
| `Program` | Entry point, single-instance Mutex, `Application.Run()` w/o main form |
| `TrayIcon` | NotifyIcon, context menu, current-device label refresh |
| `HotkeyHost` | `RegisterHotKey` / `UnregisterHotKey`, `WM_HOTKEY` dispatch |
| `CycleController` | Cycle logic, currentIndex management, sync with OS default |
| `AudioDeviceService` | Only place that touches COM (`MMDeviceEnumerator`, `IPolicyConfig`) |
| `ToastWindow` | Frameless TopMost notification window with fade in/out |
| `SettingsWindow` | Config editor UI |
| `ConfigStore` | JSON load/save, schema versioning, atomic writes |
| `Strings` | i18n string table (en/ja) |
| `StartupRegistration` | Registry Run-key write/delete, exe-path drift fix |

### 4.2 Startup Flow

1. Acquire named Mutex `Global\AudioCarousel.SingleInstance`. If taken → `MessageBox("Already running")` then exit.
2. `ConfigStore.Load()` from `<exe-dir>\audio-carousel.json`. If missing, create with defaults.
3. If `startWithWindows == true` and registered exe path differs from current path, fix the registry value.
4. Initialize `Strings` (apply language setting).
5. Construct `TrayIcon`, `HotkeyHost`, `CycleController`, `AudioDeviceService`.
6. If `hotkey != null`, register it.
7. If config was just created in this startup (file did not previously exist), open `SettingsWindow` automatically with `(First-time setup)` title suffix.
8. `Application.Run()` (message loop, no main form).

## 5. Configuration File

`audio-carousel.json` (UTF-8, 2-space indent), located in the same directory as the exe.

```json
{
  "version": 1,
  "language": "auto",
  "hotkey": {
    "modifiers": ["Ctrl", "Alt"],
    "key": "F16"
  },
  "devices": [
    {
      "endpointId": "{0.0.0.00000000}.{abc12345-...}",
      "displayName": "LG ULTRAGEAR+",
      "addedAt": "2026-04-25T17:00:00+09:00"
    },
    {
      "endpointId": "{0.0.0.00000000}.{def67890-...}",
      "displayName": "iFi (by AMR) HD+ USB Audio",
      "addedAt": "2026-04-25T17:01:00+09:00"
    }
  ],
  "currentIndex": 0,
  "startWithWindows": true
}
```

### Field semantics

| Field | Notes |
|---|---|
| `version` | Always `1` for now. Reserved for future schema migration. |
| `language` | `"auto"` (use `CultureInfo.CurrentUICulture`), `"en"`, or `"ja"`. |
| `hotkey` | Object or `null`. `null` = cycle disabled (manual pause). |
| `hotkey.modifiers` | Array of `"Ctrl" \| "Alt" \| "Shift" \| "Win"`. Empty array allowed (modifier-less hotkey, e.g., `F16`). |
| `hotkey.key` | Human-readable key name mapped to `Keys` enum (e.g., `"F16"`, `"A"`, `"OemTilde"`). |
| `devices[].endpointId` | `MMDevice.ID` from CoreAudio. Persistent across reboots and re-plug. |
| `devices[].displayName` | Friendly name. Auto-refreshed from device when device is online. |
| `devices[].addedAt` | ISO-8601 with offset. Audit only; not used for logic. |
| `currentIndex` | Last-active position in the cycle. Persisted on each switch (fire-and-forget). Clamped to valid range on load. |
| `startWithWindows` | Reflects HKCU Run-key state. |

> **Correction (2026-06-10):** the "persistent across reboots and re-plug" assumption on
> `endpointId` is **false** for NVIDIA HDA DisplayPort/HDMI endpoints — the driver mints a
> new endpoint GUID across reboots / display re-enumeration (verified via
> `HKLM\...\MMDevices\Audio\Render`: 5 NOTPRESENT + 1 Active endpoint for the same monitor,
> all sharing one HDAUDIO instance path, so no per-monitor hardware key exists either).
> USB/Bluetooth endpoint IDs are stable. `endpointId` is therefore treated as a cache: when
> it matches no active device, the app falls back to `displayName` matching and self-heals
> the stored ID (`DeviceMatcher.HealEndpointIds`, called from `CycleController`,
> `SettingsForm`, and the tray current-device label refresh).

### Save timing

- `SettingsWindow` "OK" → full save.
- `CycleController` switch → save `currentIndex` only (atomic write of full file is acceptable; size <2 KB).

### Atomic writes

Write to `audio-carousel.json.tmp` then `File.Move(tmp, real, overwrite: true)` to avoid corruption on power loss.

### Corruption recovery

If JSON parse fails on load: rename to `audio-carousel.json.bak`, show MessageBox, start with defaults.

## 6. Cycle Logic

### 6.1 Algorithm (`CycleController.Cycle()`)

```
1. If devices is empty: return (no toast).
2. available = AudioDeviceService.EnumerateActiveOutputIds()  // HashSet<string>
3. // Sync currentIndex with OS reality
   currentDefault = AudioDeviceService.GetDefaultOutputId(role: Multimedia)
   syncIndex = devices.FindIndex(d => d.endpointId == currentDefault)
   if syncIndex >= 0: currentIndex = syncIndex
4. startIndex = (currentIndex + 1) % devices.Count
5. for offset in 0..devices.Count:
       i = (startIndex + offset) % devices.Count
       if devices[i].endpointId in available:
           targetIndex = i; break
   else:
       toast(Strings.Get("error.noDeviceAvailable")); return
6. for role in [Multimedia, Communications, Console]:
       AudioDeviceService.SetDefault(devices[targetIndex].endpointId, role)
   // If any SetDefault throws COM error: toast "error.switchFailed", do NOT update currentIndex.
7. currentIndex = targetIndex
8. ConfigStore.SaveCurrentIndexAsync()  // fire-and-forget
9. ToastWindow.Show(devices[targetIndex].displayName)
10. TrayIcon.RefreshCurrentDeviceLabel()
```

### 6.2 Why three roles

Windows internally maintains three default-endpoint roles: `eMultimedia`, `eCommunications`, `eConsole`. The Windows Sound settings UI sets all three together when the user picks an output device. Audio Carousel matches this behavior.

### 6.3 Why sync currentIndex with OS default first

If the user switched the default device through another path (Windows Sound settings, Bluetooth auto-switch, another app), our cached `currentIndex` would be stale and the next Cycle would jump to a confusing target. Re-syncing first guarantees "next" means "next from where I am right now."

### 6.4 Toast behavior

- **Position:** active-monitor work-area bottom-right, 16 px inset.
- **Size:** auto-fit to text, max width 600 px (ellipsize beyond).
- **Duration:** 1.5 s total (200 ms fade in, hold, 200 ms fade out).
- **Re-trigger:** if a toast is already visible, replace text and reset the 1.5 s timer; never stack.
- **No focus stealing:** `WS_EX_NOACTIVATE`.
- **DPI:** Per-Monitor V2.
- **Style:** dark semi-transparent rounded rectangle, white text, no icon. Single line, device name only.

## 7. Hotkey

- Single global cycle hotkey (one only — no per-device hotkeys).
- Modifier-less hotkeys allowed (e.g., `F16` alone). User responsibility to avoid stealing common typing keys.
- `Win` modifier supported.
- Re-registered atomically on `SettingsWindow` "OK". Failure (already-in-use) → MessageBox, window stays open, no save.
- `hotkey == null` in config → no registration → cycle inactive (manual `Cycle next` from tray menu still works).

## 8. UI

### 8.1 Tray menu (right-click)

```
┌─────────────────────────────┐
│ Audio Carousel              │  (disabled title row)
├─────────────────────────────┤
│ Current: LG ULTRAGEAR+      │  (dynamic, disabled)
├─────────────────────────────┤
│ Cycle next                  │
├─────────────────────────────┤
│ Settings...                 │
│ ☑ Start with Windows        │
├─────────────────────────────┤
│ About                       │
│ Exit                        │
└─────────────────────────────┘
```

- **Left-click** on tray icon: same as `Cycle next`.
- **Start with Windows** toggle: applies immediately and saves.
- **About:** small dialog with version + GitHub URL.

### 8.2 Settings window

```
┌─ Audio Carousel — Settings ────────────────────────┐
│                                                    │
│ Hotkey:  [ Ctrl + Alt + F16            ] [Clear]   │
│          (Click and press a key combination)       │
│                                                    │
│ Cycle devices (in order):                          │
│ ┌────────────────────────────────────────────────┐ │
│ │ ★ ● LG ULTRAGEAR+                       (bold) │ │  ← current OS default
│ │   ● iFi (by AMR) HD+ USB Audio                 │ │
│ │   ○ Bose QC45                        (offline) │ │
│ └────────────────────────────────────────────────┘ │
│ [Add device ▾] [Remove] [↑] [↓]                    │
│                                                    │
│ Language: [ Auto ▾ ]                               │
│ ☑ Start with Windows                               │
│                                                    │
│              [ Cancel ]  [ OK ]                    │
└────────────────────────────────────────────────────┘
```

- **Hotkey field:** focus → placeholder "Press a key combination..."; first key event captured. `Esc` cancels capture. `[Clear]` removes the hotkey.
- **Device list icons:** `●` connected, `○` offline. Current OS default gets `★` prefix and bold text.
- **Add device ▾:** dropdown of currently-connected output devices not yet in the list. "(no new devices available)" if none.
- **Remove:** deletes the selected device from the list (per user request — drivers update / unused BT devices etc).
- **↑ / ↓:** reorder. Drag-and-drop not implemented (YAGNI).
- **Language:** `Auto` / `English` / `日本語`. Applied immediately on selection (UI re-renders).
- **OK:** validate hotkey re-registration, save config. **Cancel:** discard changes.

### 8.3 First-launch UX

When `audio-carousel.json` did not exist at startup (and was just created with defaults), `SettingsWindow` opens automatically with title suffix `(First-time setup)`. If the user closes it without configuring, the app stays in tray as idle. On subsequent launches the file now exists, so settings does **not** auto-open even if `devices` is still empty — the user can re-open it any time from the tray menu.

## 9. Distribution

```bash
dotnet publish -c Release -r win-x64 \
  -p:PublishSingleFile=true \
  -p:PublishAot=true \
  -p:SelfContained=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

- Output: single `AudioCarousel.exe`, target ≤ 15 MB.
- Distribution: GitHub Releases, zip containing `AudioCarousel.exe` only.
- No code signing in v1 (acceptable for self-distributed tool; user accepts SmartScreen warning on first run).

## 10. Start with Windows

- Mechanism: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, value name `AudioCarousel`, value data = absolute exe path.
- Toggle ON: write the value.
- Toggle OFF: delete the value.
- **Path drift correction:** on every startup, if `startWithWindows == true` and the registered path differs from the current process exe path, rewrite the value. This handles the user moving the exe.

## 11. Internationalization

### 11.1 Strategy

Hand-written string table in `Strings.cs` with `Dictionary<string, (string en, string ja)>`. ~20-30 entries total covering tray menu, settings window, and error messages.

### 11.2 Selection

- Config `language: "auto"` (default) → `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja"` ? Japanese : English.
- Config `language: "en"` → English forced.
- Config `language: "ja"` → Japanese forced.

### 11.3 No external resource files

Strings are compiled into the exe. No `.resx`, no satellite assemblies, no runtime resource lookup — fully AOT-compatible.

## 12. Error Handling

| Event | Behavior |
|---|---|
| Config JSON parse failure | Backup to `audio-carousel.json.bak`, MessageBox notification, continue with defaults |
| `RegisterHotKey` failure (in-use) | Settings OK: inline MessageBox, window stays open. Startup: MessageBox, continue with hotkey unregistered |
| `IPolicyConfig::SetDefaultEndpoint` HRESULT failure | Toast "Failed to switch device", do not advance currentIndex |
| Device enumeration failure | Silently skip; next Cycle retries |
| Unhandled exception | `AppDomain.UnhandledException` → MessageBox with stack, exit. **No log file written.** |

## 13. Out of Scope (v1)

- ARM64 binary
- Per-device hotkeys
- Drag-and-drop reordering in settings
- Native Windows toast (AUMID registration conflicts with portability)
- `IMMNotificationClient` device-change watcher (settings re-enumerates on open; not needed for cycle path)
- Code signing
- Auto-update
- Logging / telemetry
- Languages beyond en/ja

## 14. Dependencies Summary

| Component | Source |
|---|---|
| .NET 9 SDK | dotnet.microsoft.com |
| `NAudio.CoreAudioApi` | NuGet |
| `IPolicyConfig` COM interface | Inline `[ComImport]` declaration in source |
| Everything else | .NET BCL |

## 15. System Requirements

- Windows 10 1809 or later, or Windows 11
- x64
- No admin rights required
