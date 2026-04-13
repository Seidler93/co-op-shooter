param(
  [string]$GameVersion = "",

  [string]$LauncherVersion = "",
  [string]$UnityPath = $env:UNITY_PATH,
  [string]$Bucket = "projectz",
  [string]$Channel = "stable",
  [string]$R2PublicBaseUrl = "https://pub-72a26ee483c14eb6b975bbb15ed9ba81.r2.dev",
  [string]$Summary = "Private beta Windows build.",
  [string[]]$Notes = @("Packaged from local Windows build."),
  [switch]$LauncherOnly,
  [switch]$SkipUnityBuild,
  [switch]$SkipGameUpload,
  [switch]$SkipLauncherBuild,
  [switch]$SkipWebsiteDeploy
)

$ErrorActionPreference = "Stop"

$launcherRoot = Resolve-Path (Join-Path $PSScriptRoot "..") | Select-Object -ExpandProperty Path
$repoRoot = Resolve-Path (Join-Path $launcherRoot "..") | Select-Object -ExpandProperty Path
$projectSettingsPath = Join-Path $repoRoot "game\CoopShooter\ProjectSettings\ProjectSettings.asset"
$websiteIndexPath = Join-Path $repoRoot "website\index.html"
$launcherPackagePath = Join-Path $launcherRoot "package.json"
$gameFeedBaseUrl = "$R2PublicBaseUrl/game-feed"

if ($LauncherOnly) {
  $SkipUnityBuild = $true
  $SkipGameUpload = $true
}

if (-not $GameVersion -and -not $LauncherOnly) {
  throw "GameVersion is required unless -LauncherOnly is specified."
}

function Invoke-RepoCommand {
  param(
    [Parameter(Mandatory = $true)]
    [string]$WorkingDirectory,

    [Parameter(Mandatory = $true)]
    [string]$FilePath,

    [string[]]$Arguments = @()
  )

  Push-Location $WorkingDirectory
  try {
    & $FilePath @Arguments

    if ($LASTEXITCODE -ne 0) {
      throw "Command failed: $FilePath $($Arguments -join ' ')"
    }
  } finally {
    Pop-Location
  }
}

if ($LauncherOnly) {
  Write-Host "== Launcher-only beta release =="
} else {
  Write-Host "== Beta release $GameVersion =="

  Write-Host "Updating Unity bundleVersion -> $GameVersion"
  $projectSettings = Get-Content -LiteralPath $projectSettingsPath -Raw
  $projectSettings = $projectSettings -replace "(?m)^(\s*bundleVersion:\s*).+$", "`${1}$GameVersion"
  Set-Content -LiteralPath $projectSettingsPath -Value $projectSettings -Encoding UTF8

  if (-not $SkipUnityBuild) {
    Write-Host "Building Unity game..."
    $buildArgs = @("-ExecutionPolicy", "Bypass", "-File", ".\scripts\build-game.ps1", "-Clean")

    if ($UnityPath) {
      $buildArgs += @("-UnityPath", $UnityPath)
    }

    Invoke-RepoCommand -WorkingDirectory $launcherRoot -FilePath "powershell" -Arguments $buildArgs
  }

  Write-Host "Packaging game build..."
  Invoke-RepoCommand -WorkingDirectory $launcherRoot -FilePath "powershell" -Arguments @(
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    ".\scripts\package-game-build.ps1",
    "-Version",
    $GameVersion,
    "-Channel",
    $Channel,
    "-BaseUrl",
    $gameFeedBaseUrl,
    "-Summary",
    $Summary,
    "-Notes",
    $Notes,
    "-Overwrite"
  )

  if (-not $SkipGameUpload) {
    Write-Host "Uploading game feed to R2..."
    Invoke-RepoCommand -WorkingDirectory $launcherRoot -FilePath "powershell" -Arguments @(
      "-ExecutionPolicy",
      "Bypass",
      "-File",
      ".\scripts\publish-game-r2.ps1",
      "-Bucket",
      $Bucket,
      "-Channel",
      $Channel
    )
  }
}

if ($LauncherVersion) {
  Write-Host "Updating launcher package version -> $LauncherVersion"
  Invoke-RepoCommand -WorkingDirectory $launcherRoot -FilePath "npm.cmd" -Arguments @(
    "version",
    $LauncherVersion,
    "--no-git-tag-version",
    "--allow-same-version"
  )
}

$launcherVersionForWebsite = (Get-Content -LiteralPath $launcherPackagePath -Raw | ConvertFrom-Json).version

if (-not $SkipLauncherBuild) {
  Write-Host "Building launcher icon and installer..."
  Invoke-RepoCommand -WorkingDirectory $launcherRoot -FilePath "npm.cmd" -Arguments @("run", "build:icon")
  Invoke-RepoCommand -WorkingDirectory $launcherRoot -FilePath "npm.cmd" -Arguments @("run", "dist")

  Write-Host "Uploading launcher update feed to R2..."
  Invoke-RepoCommand -WorkingDirectory $launcherRoot -FilePath "npm.cmd" -Arguments @("run", "publish:launcher:r2")
}

Write-Host "Updating website version labels..."
$websiteIndex = Get-Content -LiteralPath $websiteIndexPath -Raw
$websiteIndex = $websiteIndex -replace "Co-op-Shooter-Setup-[0-9]+\.[0-9]+\.[0-9]+\.exe", "Co-op-Shooter-Setup-$launcherVersionForWebsite.exe"
$websiteIndex = $websiteIndex -replace "Launcher v[0-9]+\.[0-9]+\.[0-9]+", "Launcher v$launcherVersionForWebsite"
if ($GameVersion) {
  $websiteIndex = $websiteIndex -replace "Game v[0-9]+\.[0-9]+\.[0-9]+", "Game v$GameVersion"
}
Set-Content -LiteralPath $websiteIndexPath -Value $websiteIndex -Encoding UTF8

if (-not $SkipWebsiteDeploy) {
  Write-Host "Deploying website..."
  Invoke-RepoCommand -WorkingDirectory (Join-Path $repoRoot "website") -FilePath "npx.cmd" -Arguments @("wrangler", "deploy")
}

Write-Host ""
Write-Host "Release path completed."
if ($GameVersion) {
  Write-Host "Game version:     $GameVersion"
}
Write-Host "Launcher version: $launcherVersionForWebsite"
if (-not $LauncherOnly) {
  Write-Host "Game manifest:    $gameFeedBaseUrl/manifests/windows-$Channel.json"
}
