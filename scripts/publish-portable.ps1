param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$Restore
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\MouseTune.App"
$publishDir = Join-Path $project "bin\$Configuration\net8.0-windows\$Runtime\publish"
$distDir = Join-Path $repoRoot "dist"
$distExe = Join-Path $distDir "MouseTune.exe"
$zipPath = Join-Path $distDir "MouseTune-portable-win-x64.zip"
$checksumPath = Join-Path $distDir "MouseTune.exe.sha256.txt"

$publishArgs = @(
    "publish",
    $project,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true"
)

if (-not $Restore) {
    $publishArgs += "--no-restore"
}

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

New-Item -ItemType Directory -Force -Path $distDir | Out-Null
Copy-Item -LiteralPath (Join-Path $publishDir "MouseTune.exe") -Destination $distExe -Force

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -LiteralPath $distExe -DestinationPath $zipPath
$hash = Get-FileHash -LiteralPath $distExe -Algorithm SHA256
"$($hash.Hash)  MouseTune.exe" | Set-Content -LiteralPath $checksumPath

Write-Host "Portable exe: $distExe"
Write-Host "Zip package:  $zipPath"
Write-Host "Checksum:     $checksumPath"
