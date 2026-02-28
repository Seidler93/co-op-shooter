param(
  [string]$UnityPath = $env:UNITY_PATH,
  [switch]$Clean
)

$ErrorActionPreference = "Stop"

# -----------------------------
# Resolve Paths
# -----------------------------
$launcherRoot = Resolve-Path (Join-Path $PSScriptRoot "..") | Select-Object -ExpandProperty Path
$repoRoot     = Resolve-Path (Join-Path $launcherRoot "..") | Select-Object -ExpandProperty Path

$projectPath  = Join-Path $repoRoot "game\CoopShooter"
$outDir       = Join-Path $repoRoot "builds\windows"
$exePath      = Join-Path $outDir "CoopShooter.exe"
$logPath      = Join-Path $repoRoot "unity-build.log"

# -----------------------------
# Validate Inputs
# -----------------------------
if (-not (Test-Path $projectPath)) {
  throw "Unity project not found at: $projectPath"
}

$buildScriptPath = Join-Path $projectPath "Assets\Editor\BuildWindows.cs"
if (-not (Test-Path $buildScriptPath)) {
  throw "Missing Unity build script: $buildScriptPath`nCreate Assets/Editor/BuildWindows.cs with BuildWindows.Build()."
}

if (-not $UnityPath -or -not (Test-Path $UnityPath)) {
  throw @"
UNITY_PATH not set or invalid.

Set it once, e.g.:
  setx UNITY_PATH "H:\Unity\6000.3.10f1\Editor\Unity.exe"

Or run:
  powershell -ExecutionPolicy Bypass -File .\scripts\build-game.ps1 -UnityPath "H:\Unity\6000.3.10f1\Editor\Unity.exe"
"@
}

# -----------------------------
# Display Build Info
# -----------------------------
Write-Host "========================================"
Write-Host "Unity Windows Build"
Write-Host "Unity:   $UnityPath"
Write-Host "Project: $projectPath"
Write-Host "Output:  $exePath"
Write-Host "Log:     $logPath"
Write-Host "========================================"

# -----------------------------
# Prepare Output Folder
# -----------------------------
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

if ($Clean) {
  Write-Host "Cleaning output folder..."
  Remove-Item -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $outDir "*")
}

# Recreate output folder after clean (important)
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# -----------------------------
# Run Unity Headless Build
# -----------------------------
& "$UnityPath" `
  -batchmode `
  -nographics `
  -quit `
  -projectPath "$projectPath" `
  -executeMethod BuildWindows.Build `
  -logFile "$logPath"

# Capture exit code immediately
$exit = $LASTEXITCODE

# -----------------------------
# Verify Build Output
# -----------------------------
if (-not (Test-Path $exePath)) {

  Write-Host ""
  Write-Host "Unity build failed."
  Write-Host "Exit code (may appear blank in some shells): $exit"
  Write-Host ""

  if (Test-Path $logPath) {
    Write-Host "---- Unity build log (last 200 lines) ----"
    Get-Content $logPath -Tail 200
  } else {
    Write-Host "No unity log found at: $logPath"
  }

  throw "Unity build failed - executable not produced."
}

Write-Host ""
Write-Host "Unity build completed successfully."
Write-Host "Exit code: $([int]$exit)"
Write-Host ""