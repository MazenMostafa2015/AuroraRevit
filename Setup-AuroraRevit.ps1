<#
.SYNOPSIS
Installs AuroraRevit from a published GitHub release for one Revit user.

.DESCRIPTION
Downloads the selected public AuroraRevit release, deploys the add-in and
proxy under %LOCALAPPDATA%\AuroraRevit, generates a user-local Revit manifest,
optionally stores the OpenAI proxy settings for the current Windows user, and
starts the local Aurora proxy when it is not already healthy.
#>
[CmdletBinding()]
param(
    [ValidateSet('2023', '2024', '2025')]
    [string]$RevitVersion = '2025',

    [ValidatePattern('^v\d+\.\d+\.\d+$')]
    [string]$ReleaseTag = 'v1.0.0',

    [string]$PackagePath,

    [string]$OpenAiApiKey,

    [string]$OpenAiModel = 'gpt-4o-mini',

    [switch]$SkipProxyStart
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Write-Info { param([string]$Message) Write-Host "[INFO]    $Message" -ForegroundColor Cyan }
function Write-Success { param([string]$Message) Write-Host "[SUCCESS] $Message" -ForegroundColor Green }
function Write-Warning { param([string]$Message) Write-Host "[WARNING] $Message" -ForegroundColor Yellow }
function Write-Failure { param([string]$Message) Write-Host "[ERROR]   $Message" -ForegroundColor Red }

function Test-AuroraProxy {
    try {
        $response = Invoke-WebRequest -Uri 'http://localhost:5000/health' -UseBasicParsing -TimeoutSec 2
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

function Get-PlainTextSecret {
    param([System.Security.SecureString]$Secret)

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Secret)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

try {
    if ($env:OS -ne 'Windows_NT' -or -not [Environment]::Is64BitOperatingSystem) {
        throw 'AuroraRevit setup requires 64-bit Windows.'
    }

    $repo = 'MazenMostafa2015/AuroraRevit'
    $assetName = 'AuroraRevit-Release-Revit2025.zip'
    $downloadUrl = "https://github.com/$repo/releases/download/$ReleaseTag/$assetName"
    $installRoot = Join-Path $env:LOCALAPPDATA 'AuroraRevit'
    $addinDirectory = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitVersion"
    $temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ("AuroraRevit-" + [Guid]::NewGuid().ToString('N'))
    $archivePath = Join-Path $temporaryRoot $assetName
    $extractRoot = Join-Path $temporaryRoot 'extracted'

    Write-Info "Preparing AuroraRevit $ReleaseTag for Revit $RevitVersion."
    if (-not [string]::IsNullOrWhiteSpace($PackagePath)) {
        $PackagePath = [IO.Path]::GetFullPath($PackagePath)
        if (-not (Test-Path -LiteralPath $PackagePath)) {
            throw "The local package was not found: $PackagePath"
        }
        Write-Info "Using local package: $PackagePath"
    }

    $revitExecutable = Join-Path ${env:ProgramFiles} "Autodesk\Revit $RevitVersion\Revit.exe"
    if (-not (Test-Path $revitExecutable)) {
        Write-Warning "Revit $RevitVersion was not found at the standard path. The add-in will still be installed for this Revit version."
    }

    New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null
    if ([string]::IsNullOrWhiteSpace($PackagePath)) {
        try {
            Invoke-WebRequest -Uri 'https://github.com' -UseBasicParsing -TimeoutSec 10 | Out-Null
        }
        catch {
            throw 'GitHub could not be reached. Pass -PackagePath with a local AuroraRevit release archive, or check the network and run setup again.'
        }

        Write-Info 'Downloading the published AuroraRevit release.'
        Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath -UseBasicParsing
        if (-not (Test-Path $archivePath) -or (Get-Item $archivePath).Length -lt 1MB) {
            throw "The release asset was not downloaded successfully: $downloadUrl"
        }
    }
    else {
        Copy-Item -LiteralPath $PackagePath -Destination $archivePath -Force
    }

    Write-Info 'Extracting the release package.'
    Expand-Archive -Path $archivePath -DestinationPath $extractRoot -Force
    $addinSource = Join-Path $extractRoot 'Release\RevitAddin'
    $proxySource = Join-Path $extractRoot 'publish\AiProxy'
    $proxyGuiSource = Join-Path $extractRoot 'publish\AiProxyGui'
    $assemblySource = Join-Path $addinSource 'AuroraRevit.RevitAddin.dll'
    if (-not (Test-Path $assemblySource) -or -not (Test-Path $proxySource)) {
        throw 'The downloaded archive does not contain the expected AuroraRevit release layout.'
    }

    Write-Info 'Deploying AuroraRevit to the current user profile.'
    New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
    foreach ($directoryName in @('RevitAddin', 'AiProxy', 'AiProxyGui')) {
        $destination = Join-Path $installRoot $directoryName
        if (Test-Path $destination) {
            Remove-Item -Path $destination -Recurse -Force
        }
    }
    Copy-Item -Path $addinSource -Destination (Join-Path $installRoot 'RevitAddin') -Recurse -Force
    Copy-Item -Path $proxySource -Destination (Join-Path $installRoot 'AiProxy') -Recurse -Force
    if (Test-Path $proxyGuiSource) {
        Copy-Item -Path $proxyGuiSource -Destination (Join-Path $installRoot 'AiProxyGui') -Recurse -Force
    }

    $assemblyPath = Join-Path $installRoot 'RevitAddin\AuroraRevit.RevitAddin.dll'
    if (-not (Test-Path $assemblyPath)) {
        throw 'AuroraRevit.RevitAddin.dll was not deployed successfully.'
    }

    New-Item -ItemType Directory -Path $addinDirectory -Force | Out-Null
    $manifestPath = Join-Path $addinDirectory 'AuroraRevit.addin'
    $xmlAssemblyPath = [Security.SecurityElement]::Escape($assemblyPath)
    $manifest = @"
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Aurora Revit AI Assistant</Name>
    <Assembly>$xmlAssemblyPath</Assembly>
    <AddInId>7B0A7C2A-6C4B-4DB7-9D3A-EF5E8B5CF901</AddInId>
    <FullClassName>AuroraRevit.RevitAddin.AuroraApplication</FullClassName>
    <VendorId>AURORA</VendorId>
    <VendorDescription>Aurora AI Assistant</VendorDescription>
  </AddIn>
  <AddIn Type="Command">
    <Text>Aurora AI Query</Text>
    <Description>Send a prompt to the local Aurora AI proxy.</Description>
    <Assembly>$xmlAssemblyPath</Assembly>
    <AddInId>9CFF34B5-31D7-43C5-89B1-64A0BD614A2E</AddInId>
    <FullClassName>AuroraRevit.RevitAddin.AuroraQueryCommand</FullClassName>
    <VendorId>AURORA</VendorId>
    <VendorDescription>Aurora AI Assistant</VendorDescription>
  </AddIn>
</RevitAddIns>
"@
    [IO.File]::WriteAllText($manifestPath, $manifest, (New-Object Text.UTF8Encoding($false)))
    Write-Success "Installed the AuroraRevit manifest at $manifestPath"

    if ([string]::IsNullOrWhiteSpace($OpenAiApiKey)) {
        $OpenAiApiKey = $env:OpenAI__ApiKey
    }
    if ([string]::IsNullOrWhiteSpace($OpenAiApiKey)) {
        Write-Warning 'No OpenAI API key was supplied. The proxy will install, but queries will not work until a key is configured.'
        $secureKey = Read-Host 'Enter an OpenAI API key now, or press Enter to configure it later' -AsSecureString
        if ($secureKey.Length -gt 0) {
            $OpenAiApiKey = Get-PlainTextSecret -Secret $secureKey
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($OpenAiApiKey)) {
        [Environment]::SetEnvironmentVariable('OpenAI__ApiKey', $OpenAiApiKey, 'User')
        $env:OpenAI__ApiKey = $OpenAiApiKey
        [Environment]::SetEnvironmentVariable('OpenAI__Model', $OpenAiModel, 'User')
        $env:OpenAI__Model = $OpenAiModel
        Write-Success 'Saved the Aurora proxy configuration for the current Windows user.'
    }

    if (-not $SkipProxyStart) {
        if (Test-AuroraProxy) {
            Write-Success 'Aurora proxy is already healthy at http://localhost:5000.'
        }
        else {
            $proxyGui = Join-Path $installRoot 'AiProxyGui\AuroraRevit.ProxyGui.exe'
            $proxyExe = Join-Path $installRoot 'AiProxy\AiProxy.exe'
            if (Test-Path $proxyGui) {
                Write-Info 'Starting the Aurora proxy desktop application.'
                Start-Process -FilePath $proxyGui -WorkingDirectory (Split-Path $proxyGui)
            }
            elseif (Test-Path $proxyExe) {
                Write-Info 'Starting the Aurora proxy in the background.'
                Start-Process -FilePath $proxyExe -WorkingDirectory (Split-Path $proxyExe) -WindowStyle Hidden
            }
            else {
                Write-Warning 'The proxy executable could not be found. The add-in was installed, but the proxy must be started manually.'
            }

            $healthy = $false
            foreach ($attempt in 1..15) {
                Start-Sleep -Seconds 1
                if (Test-AuroraProxy) {
                    $healthy = $true
                    break
                }
            }
            if ($healthy) {
                Write-Success 'Aurora proxy is healthy at http://localhost:5000.'
            }
            else {
                Write-Warning 'The proxy did not report healthy within 15 seconds. Open the Aurora proxy app and verify the OpenAI configuration before using the Revit pane.'
            }
        }
    }

    Write-Success "AuroraRevit setup is complete. Open Revit $RevitVersion and launch the Aurora AI Assistant pane."
}
catch {
    Write-Failure $_.Exception.Message
    exit 1
}
finally {
    if ($temporaryRoot -and (Test-Path $temporaryRoot)) {
        Remove-Item -Path $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
