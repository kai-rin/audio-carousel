# Audio Carousel — publish a single self-contained executable.
#
# We do NOT use NativeAOT or trimming:
# - Windows Forms is incompatible with NativeAOT (NETSDK1175).
# - PublishTrimmed strips runtime COM interop machinery that NAudio.CoreAudioApi
#   depends on, causing System.NotSupportedException at runtime.
# Result: ~108 MB self-contained single-file exe. Big, but reliably working.
#
# Output: publish/AudioCarousel.exe

param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$PublishDir  = Join-Path $ProjectRoot 'publish'

$PublishedExe = Join-Path $PublishDir 'AudioCarousel.exe'
$ConfigPath   = Join-Path $PublishDir 'audio-carousel.json'

# The exe in publish/ may be the maintainer's daily-driver instance; wiping the
# folder under it fails half-way with a confusing file-in-use error.
$running = Get-Process -Name AudioCarousel -ErrorAction SilentlyContinue |
  Where-Object { $_.Path -and ([IO.Path]::GetFullPath($_.Path) -eq [IO.Path]::GetFullPath($PublishedExe)) }
if ($running) {
  throw "publish/AudioCarousel.exe is running (PID $($running.Id -join ', ')). Exit it from the tray first."
}

# publish/audio-carousel.json can be a live config; carry it across the wipe.
$savedConfig = $null
if (Test-Path $ConfigPath) {
  $savedConfig = Join-Path ([IO.Path]::GetTempPath()) ("audio-carousel.json.publish-" + [guid]::NewGuid().ToString('N'))
  Copy-Item $ConfigPath $savedConfig
}

if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }

# A publish-mode restore rewrites packages.lock.json with ILLink + win-x64
# entries that break CI's --locked-mode (NU1004). Put the file back afterwards.
$LockFile  = Join-Path $ProjectRoot 'src\AudioCarousel\packages.lock.json'
$savedLock = [IO.File]::ReadAllBytes($LockFile)

$extra = @()
if ($Version) { $extra += "-p:Version=$Version" }

dotnet publish (Join-Path $ProjectRoot 'src\AudioCarousel\AudioCarousel.csproj') `
  -c Release `
  -r win-x64 `
  -p:IsPublishing=true `
  -p:PublishAot=false `
  -p:PublishSingleFile=true `
  -p:SelfContained=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  @extra `
  -o $PublishDir
$publishExit = $LASTEXITCODE
[IO.File]::WriteAllBytes($LockFile, $savedLock)
if ($publishExit -ne 0) {
  if ($savedConfig) { Write-Warning "Publish failed; your config was kept at $savedConfig" }
  exit $publishExit
}

# The .pdb is not shipped (release.yml zips only the exe); keep publish/ clean.
Remove-Item (Join-Path $PublishDir '*.pdb') -ErrorAction SilentlyContinue

if ($savedConfig) {
  Move-Item $savedConfig $ConfigPath -Force
  Write-Host "Restored existing audio-carousel.json" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Output:" -ForegroundColor Green
Get-Item (Join-Path $PublishDir 'AudioCarousel.exe') |
  Format-Table Name, @{Name='SizeMB'; Expression={[math]::Round($_.Length / 1MB, 2)}}
