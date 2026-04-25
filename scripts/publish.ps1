# Audio Carousel — publish a single self-contained executable.
#
# We do NOT use NativeAOT or trimming:
# - Windows Forms is incompatible with NativeAOT (NETSDK1175).
# - PublishTrimmed strips runtime COM interop machinery that NAudio.CoreAudioApi
#   depends on, causing System.NotSupportedException at runtime.
# Result: ~108 MB self-contained single-file exe. Big, but reliably working.
#
# Output: publish/AudioCarousel.exe

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$PublishDir  = Join-Path $ProjectRoot 'publish'

if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }

dotnet publish (Join-Path $ProjectRoot 'src\AudioCarousel\AudioCarousel.csproj') `
  -c Release `
  -r win-x64 `
  -p:IsPublishing=true `
  -p:PublishAot=false `
  -p:PublishSingleFile=true `
  -p:SelfContained=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $PublishDir

Write-Host ""
Write-Host "Output:" -ForegroundColor Green
Get-Item (Join-Path $PublishDir 'AudioCarousel.exe') |
  Format-Table Name, @{Name='SizeMB'; Expression={[math]::Round($_.Length / 1MB, 2)}}
