# Security Policy

## Supported Versions

Only the latest released version of Audio Carousel is supported with security
fixes. The project is a small single-developer utility; please always run the
most recent build from the [Releases](https://github.com/kai-rin/audio-carousel/releases) page.

## Reporting a Vulnerability

If you discover a security issue (for example, a way Audio Carousel could be
abused to escalate privileges, exfiltrate data, or be hijacked by another
process), **please do not file a public issue**. Instead, open a
[private security advisory](https://github.com/kai-rin/audio-carousel/security/advisories/new)
on GitHub, or contact the maintainer through GitHub directly.

For non-security bugs, use the regular issue tracker.

## Threat Model — what Audio Carousel does and does not do

- Runs entirely on the local machine. **No network requests** of any kind.
- Reads `audio-carousel.json` from the directory next to the executable.
- Writes a single `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` value
  if (and only if) "Start with Windows" is enabled by the user.
- Calls the undocumented Windows COM interface `IPolicyConfig` to switch the
  default audio endpoint. This is the standard technique used by similar
  tools (SoundSwitch, EarTrumpet, NirCmd) and requires no elevated privileges.
- Does **not** require, request, or use administrator privileges.
- Does **not** collect telemetry, write log files, or transmit any data.
