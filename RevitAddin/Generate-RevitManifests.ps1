[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $OutputRoot = (Join-Path $PSScriptRoot "Deployment"),

    [Parameter(Mandatory = $false)]
    [string] $AssemblyPath = (Join-Path $PSScriptRoot "bin\Release\AuroraRevit.RevitAddin.dll"),

    [Parameter(Mandatory = $false)]
    [ValidateSet("2023", "2024", "2025")]
    [string[]] $Versions = @("2023", "2024", "2025")
)

$ErrorActionPreference = "Stop"
$versions = $Versions
$normalizedAssemblyPath = [System.IO.Path]::GetFullPath($AssemblyPath)
$xmlAssemblyPath = [System.Security.SecurityElement]::Escape($normalizedAssemblyPath)

foreach ($version in $versions) {
    $versionDirectory = Join-Path $OutputRoot "Revit$version"
    New-Item -ItemType Directory -Path $versionDirectory -Force | Out-Null

    $manifest = @"
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Aurora Revit AI Assistant</Name>
    <Assembly>$xmlAssemblyPath</Assembly>
    <AddInId>7B0A7C2A-6C4B-4DB7-9D3A-EF5E8B5CF901</AddInId>
    <FullClassName>AuroraRevit.RevitAddin.AuroraApplication</FullClassName>
    <VendorId>AURORA</VendorId>
    <VendorDescription>Aurora AI Assistant for Autodesk Revit $version</VendorDescription>
  </AddIn>
  <AddIn Type="Command">
    <Text>Aurora AI Query</Text>
    <Description>Send a prompt to the local Aurora AI proxy.</Description>
    <Assembly>$xmlAssemblyPath</Assembly>
    <AddInId>9CFF34B5-31D7-43C5-89B1-64A0BD614A2E</AddInId>
    <FullClassName>AuroraRevit.RevitAddin.AuroraQueryCommand</FullClassName>
    <VendorId>AURORA</VendorId>
    <VendorDescription>Aurora AI Assistant for Autodesk Revit $version</VendorDescription>
  </AddIn>
</RevitAddIns>
"@

    $manifestPath = Join-Path $versionDirectory "AuroraRevit.addin"
    [System.IO.File]::WriteAllText($manifestPath, $manifest.Trim() + [Environment]::NewLine, [System.Text.Encoding]::UTF8)
    Write-Host "Generated $manifestPath"
}
