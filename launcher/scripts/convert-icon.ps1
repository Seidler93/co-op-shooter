param(
  [string]$Source = (Join-Path $PSScriptRoot "..\assets\icon.png"),
  [string]$Destination = (Join-Path $PSScriptRoot "..\assets\icon.ico")
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$resolvedSource = (Resolve-Path -LiteralPath $Source).Path
$destinationDir = Split-Path -Parent $Destination

if (-not (Test-Path -LiteralPath $destinationDir)) {
  New-Item -ItemType Directory -Path $destinationDir | Out-Null
}

$sizes = @(256, 128, 64, 48, 32, 16)
$sourceBitmap = [System.Drawing.Bitmap]::new($resolvedSource)
$memoryStreams = @()
$writer = $null

try {
  $output = [System.IO.File]::Create($Destination)
  $writer = [System.IO.BinaryWriter]::new($output)

  $writer.Write([UInt16]0)
  $writer.Write([UInt16]1)
  $writer.Write([UInt16]$sizes.Count)

  $imageData = foreach ($size in $sizes) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.DrawImage($sourceBitmap, 0, 0, $size, $size)

    $stream = [System.IO.MemoryStream]::new()
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $memoryStreams += $stream

    [pscustomobject]@{
      Size = $size
      Bytes = $stream.ToArray()
    }

    $graphics.Dispose()
    $bitmap.Dispose()
  }

  $offset = 6 + (16 * $imageData.Count)

  foreach ($image in $imageData) {
    $writer.Write([byte]($(if ($image.Size -eq 256) { 0 } else { $image.Size })))
    $writer.Write([byte]($(if ($image.Size -eq 256) { 0 } else { $image.Size })))
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$image.Bytes.Length)
    $writer.Write([UInt32]$offset)
    $offset += $image.Bytes.Length
  }

  foreach ($image in $imageData) {
    $writer.Write($image.Bytes)
  }
} finally {
  if ($writer) {
    $writer.Dispose()
  }

  foreach ($stream in $memoryStreams) {
    $stream.Dispose()
  }

  $sourceBitmap.Dispose()
}

Write-Host "Created $Destination from $resolvedSource"
