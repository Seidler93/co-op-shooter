param(
  [Parameter(Mandatory = $true)]
  [string]$Version,

  [string]$BuildDir = "",
  [string]$Channel = "stable",
  [string]$BaseUrl = "http://localhost:8080",
  [string]$ExecutableName = "CoopShooter.exe",
  [string]$Summary = "Private beta Windows build.",
  [string[]]$Notes = @("Packaged from local Windows build."),
  [switch]$Overwrite
)

$ErrorActionPreference = "Stop"

$launcherRoot = Resolve-Path (Join-Path $PSScriptRoot "..") | Select-Object -ExpandProperty Path
$repoRoot = Resolve-Path (Join-Path $launcherRoot "..") | Select-Object -ExpandProperty Path

if (-not $BuildDir) {
  $BuildDir = Join-Path $repoRoot "builds\windows"
}

$feedRoot = Join-Path $repoRoot "distribution\game-feed"
$downloadsRoot = Join-Path $feedRoot "downloads"
$manifestsRoot = Join-Path $feedRoot "manifests"
$artifactName = "coop-shooter-$Version-win64.zip"
$artifactPath = Join-Path $downloadsRoot $artifactName
$manifestPath = Join-Path $manifestsRoot "windows-$Channel.json"
$executablePath = Join-Path $BuildDir $ExecutableName

if (-not (Test-Path $BuildDir)) {
  throw "Build directory not found: $BuildDir"
}

if (-not (Test-Path $executablePath)) {
  throw "Expected executable not found: $executablePath"
}

New-Item -ItemType Directory -Force -Path $downloadsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $manifestsRoot | Out-Null

if ((Test-Path $artifactPath) -and -not $Overwrite) {
  throw "Artifact already exists: $artifactPath`nRe-run with -Overwrite to replace it."
}

if (Test-Path $artifactPath) {
  Remove-Item -LiteralPath $artifactPath -Force
}

Compress-Archive -Path (Join-Path $BuildDir "*") -DestinationPath $artifactPath -CompressionLevel Optimal

$manifest = [ordered]@{
  version = $Version
  publishedAt = (Get-Date).ToUniversalTime().ToString("o")
  summary = $Summary
  notes = $Notes
  noteSections = @(
    [ordered]@{
      title = "Build"
      items = $Notes
    }
  )
  platforms = [ordered]@{
    win32 = [ordered]@{
      downloadUrl = "$BaseUrl/downloads/$artifactName"
      launchExecutable = $ExecutableName
      fileName = $artifactName
    }
  }
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host ""
Write-Host "Game feed updated."
Write-Host "Artifact: $artifactPath"
Write-Host "Manifest: $manifestPath"
Write-Host "Manifest URL: $BaseUrl/manifests/windows-$Channel.json"
Write-Host ""
