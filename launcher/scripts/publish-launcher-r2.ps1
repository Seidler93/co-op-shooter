param(
  [string]$Bucket = "projectz",
  [string]$ReleaseDir = (Join-Path $PSScriptRoot "..\release")
)

$ErrorActionPreference = "Stop"

$resolvedReleaseDir = Resolve-Path -LiteralPath $ReleaseDir
$latestYmlPath = Join-Path $resolvedReleaseDir "latest.yml"

if (-not (Test-Path -LiteralPath $latestYmlPath)) {
  throw "Could not find latest.yml in $resolvedReleaseDir. Build the launcher first with npm run dist."
}

$latestContent = Get-Content -LiteralPath $latestYmlPath -Raw
$installerName = [regex]::Match($latestContent, "(?m)^path:\s*(.+)$").Groups[1].Value.Trim()

if (-not $installerName) {
  throw "Could not read installer path from latest.yml."
}

$installerPath = Join-Path $resolvedReleaseDir $installerName
$blockmapPath = "$installerPath.blockmap"

$uploadTargets = @(
  @{
    LocalPath = $installerPath
    ObjectKey = $installerName
    CacheControl = "public, max-age=31536000, immutable"
  },
  @{
    LocalPath = $blockmapPath
    ObjectKey = "$installerName.blockmap"
    CacheControl = "public, max-age=31536000, immutable"
  },
  @{
    LocalPath = $latestYmlPath
    ObjectKey = "latest.yml"
    CacheControl = "no-cache"
  }
)

foreach ($target in $uploadTargets) {
  if (-not (Test-Path -LiteralPath $target.LocalPath)) {
    throw "Missing launcher release file: $($target.LocalPath)"
  }
}

foreach ($target in $uploadTargets) {
  Write-Host "Uploading $($target.LocalPath) -> r2://$Bucket/$($target.ObjectKey)"
  & npx.cmd wrangler r2 object put "$Bucket/$($target.ObjectKey)" --file "$($target.LocalPath)" --cache-control "$($target.CacheControl)" --remote

  if ($LASTEXITCODE -ne 0) {
    throw "Wrangler upload failed for $($target.ObjectKey)."
  }
}

Write-Host "Launcher update feed uploaded to R2 bucket '$Bucket'."
