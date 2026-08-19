[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $RevitApiPath = "C:\Program Files\Autodesk\Revit 2025"
)

$ErrorActionPreference = "Stop"
$solutionRoot = $PSScriptRoot
$solutionPath = Join-Path $solutionRoot "AuroraRevit.sln"
$revitProject = Join-Path $solutionRoot "RevitAddin\RevitAddin.csproj"
$proxyProject = Join-Path $solutionRoot "AiProxy\AiProxy.csproj"
$releaseRoot = Join-Path $solutionRoot "Release"
$revitRelease = Join-Path $releaseRoot "RevitAddin"
$manifestRoot = Join-Path $releaseRoot "Manifests"

if (Test-Path -LiteralPath $releaseRoot) {
    Remove-Item -LiteralPath $releaseRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null

Write-Host "Building RevitAddin Release output..." -ForegroundColor Cyan
& msbuild $revitProject /t:Build /p:Configuration=Release /p:Platform=AnyCPU /p:RevitApiPath="$RevitApiPath"
if ($LASTEXITCODE -ne 0) {
    throw "RevitAddin build failed with exit code $LASTEXITCODE."
}

Write-Host "Publishing self-contained AiProxy for win-x64..." -ForegroundColor Cyan
& dotnet publish $proxyProject -c Release -p:PublishProfile=WinX64SelfContained
if ($LASTEXITCODE -ne 0) {
    throw "AiProxy publish failed with exit code $LASTEXITCODE."
}

Write-Host "Publishing self-contained AiProxy GUI for win-x64..." -ForegroundColor Cyan
& dotnet publish (Join-Path $solutionRoot "AiProxy.Desktop\AiProxy.Desktop.csproj") -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o (Join-Path $releaseRoot "AiProxyGui")
if ($LASTEXITCODE -ne 0) {
    throw "AiProxy GUI publish failed with exit code $LASTEXITCODE."
}

Write-Host "Generating Revit 2023, 2024, and 2025 manifests..." -ForegroundColor Cyan
& powershell -NoProfile -ExecutionPolicy Bypass -File `
    (Join-Path $solutionRoot "RevitAddin\Generate-RevitManifests.ps1") `
    -AssemblyPath (Join-Path $revitRelease "AuroraRevit.RevitAddin.dll") `
    -OutputRoot $manifestRoot
if ($LASTEXITCODE -ne 0) {
    throw "Manifest generation failed with exit code $LASTEXITCODE."
}

Copy-Item -LiteralPath (Join-Path $solutionRoot "Start-AuroraRevit.ps1") -Destination $releaseRoot -Force
Copy-Item -LiteralPath (Join-Path $solutionRoot "README.md") -Destination $releaseRoot -Force

Write-Host "Release package prepared at: $releaseRoot" -ForegroundColor Green
Get-ChildItem -LiteralPath $releaseRoot -Recurse -File | Select-Object FullName, Length
