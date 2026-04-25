# Third-Party Notices

Audio Carousel is distributed as a self-contained single-file executable
that bundles the .NET runtime and the third-party libraries listed below.
Each component is used under its own license. Audio Carousel itself is
licensed under the [MIT License](LICENSE).

## Bundled at runtime (inside `AudioCarousel.exe`)

| Component | Version | License | Source |
|-----------|---------|---------|--------|
| .NET runtime + Windows Forms | 9.x | MIT | https://github.com/dotnet/runtime, https://github.com/dotnet/winforms |
| NAudio.Wasapi (provides `NAudio.CoreAudioApi`) | 2.2.1 | MIT | https://github.com/naudio/NAudio |

## Test-only dependencies (not redistributed)

| Component | Version | License | Source |
|-----------|---------|---------|--------|
| xUnit | 2.9.2 | Apache-2.0 | https://github.com/xunit/xunit |
| xunit.runner.visualstudio | 2.8.2 | Apache-2.0 | https://github.com/xunit/visualstudio.xunit |
| Microsoft.NET.Test.Sdk | 17.11.1 | MIT | https://github.com/microsoft/vstest |

The full text of each license is reproduced in the upstream repositories
linked above. The MIT License terms in [LICENSE](LICENSE) apply to Audio
Carousel itself and not to the bundled components, which retain their
original licenses.
