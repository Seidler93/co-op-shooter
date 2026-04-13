param(
  [string]$Bucket = "projectz",
  [string]$Channel = "stable",
  [string]$FeedRoot = "",
  [string]$RemotePrefix = "game-feed"
)

$ErrorActionPreference = "Stop"

$launcherRoot = Resolve-Path (Join-Path $PSScriptRoot "..") | Select-Object -ExpandProperty Path
$repoRoot = Resolve-Path (Join-Path $launcherRoot "..") | Select-Object -ExpandProperty Path

if (-not $FeedRoot) {
  $FeedRoot = Join-Path $repoRoot "distribution\game-feed"
}

$resolvedFeedRoot = Resolve-Path -LiteralPath $FeedRoot | Select-Object -ExpandProperty Path
$manifestPath = Join-Path $resolvedFeedRoot "manifests\windows-$Channel.json"

if (-not (Test-Path -LiteralPath $manifestPath)) {
  throw "Could not find game manifest: $manifestPath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$artifactName = $manifest.platforms.win32.fileName

if (-not $artifactName) {
  throw "Game manifest is missing platforms.win32.fileName."
}

$artifactPath = Join-Path $resolvedFeedRoot "downloads\$artifactName"

if (-not (Test-Path -LiteralPath $artifactPath)) {
  throw "Could not find game artifact: $artifactPath"
}

$uploadTargets = @(
  @{
    LocalPath = $artifactPath
    ObjectKey = "$RemotePrefix/downloads/$artifactName"
    CacheControl = "public, max-age=31536000, immutable"
  },
  @{
    LocalPath = $manifestPath
    ObjectKey = "$RemotePrefix/manifests/windows-$Channel.json"
    CacheControl = "no-cache"
  }
)

foreach ($target in $uploadTargets) {
  Write-Host "Uploading $($target.LocalPath) -> r2://$Bucket/$($target.ObjectKey)"
  & npx.cmd wrangler r2 object put "$Bucket/$($target.ObjectKey)" --file "$($target.LocalPath)" --cache-control "$($target.CacheControl)" --remote

  if ($LASTEXITCODE -ne 0) {
    throw "Wrangler upload failed for $($target.ObjectKey)."
  }
}

Write-Host "Game feed uploaded to R2 bucket '$Bucket'."
