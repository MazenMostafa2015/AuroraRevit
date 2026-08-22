[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("Release", "Source")]
    [string]$Mode = "Release",
    [string]$Tag = "v2.1.1",
    [string]$RepoRoot = "",
    [switch]$Force,
    [switch]$LaunchRevit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Repo = "MazenMostafa2015/AuroraRevit"
$UserExtension = Join-Path $env:APPDATA "pyRevit\Extensions\AuroraRevit.extension"
$BackupRoot = Join-Path $env:APPDATA "AuroraRevit\backups"
$Stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$BackupPath = Join-Path $BackupRoot $Stamp
$RequiredFiles = @(
    "RevitTools.tab\AIAssistant.panel\CommandLogger.pushbutton\script.py",
    "RevitTools.tab\AIAssistant.panel\CommandLine.pushbutton\script.py",
    "RevitTools.tab\AIAssistant.panel\UtilityTools\ai_router.py"
)

function Write-Step([string]$Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Stop-RevitSafely {
    $processes = @(Get-Process -Name "Revit" -ErrorAction SilentlyContinue)
    if ($processes.Count -eq 0) { return }

    if (-not $Force) {
        $answer = Read-Host "Revit is running. Close it now? [Y/n]"
        if ($answer -and $answer.ToLowerInvariant() -notin @("y", "yes")) {
            throw "Deployment cancelled because Revit is still running."
        }
    }

    foreach ($process in $processes) {
        Write-Host "Stopping Revit process $($process.Id)..."
        Stop-Process -Id $process.Id -Force
    }
    Start-Sleep -Seconds 2
}

function Backup-CurrentExtension {
    if (-not (Test-Path $UserExtension)) { return $null }
    New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null
    Write-Step "Backing up the current pyRevit extension"
    Copy-Item -Path $UserExtension -Destination $BackupPath -Recurse -Force
    Write-Host "Backup: $BackupPath"
    return $BackupPath
}

function Verify-PyRevitPayload([string]$Root) {
    $missing = @()
    foreach ($relative in $RequiredFiles) {
        if (-not (Test-Path (Join-Path $Root $relative))) {
            $missing += $relative
        }
    }
    if ($missing.Count -gt 0) {
        throw "Required AuroraRevit files are missing:`n$($missing -join "`n")"
    }
    Write-Host "Verified CommandLogger, CommandLine, and ai_router.py." -ForegroundColor Green
}

function Deploy-Source {
    if (-not $RepoRoot) { throw "-RepoRoot is required when -Mode Source is selected." }
    $sourceExtension = Join-Path (Resolve-Path $RepoRoot) "AuroraRevit.extension"
    if (-not (Test-Path $sourceExtension)) { throw "Source extension not found: $sourceExtension" }

    Write-Step "Deploying pyRevit files from the local repository"
    New-Item -ItemType Directory -Path (Split-Path $UserExtension) -Force | Out-Null
    if ($PSCmdlet.ShouldProcess($UserExtension, "Copy AuroraRevit pyRevit extension")) {
        New-Item -ItemType Directory -Path $UserExtension -Force | Out-Null
        Copy-Item -Path (Join-Path $sourceExtension "*") -Destination $UserExtension -Recurse -Force
    }
}

function Deploy-Release {
    $tempRoot = Join-Path ([IO.Path]::GetTempPath()) "AuroraRevit-$Stamp"
    New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
    $installer = Join-Path $tempRoot "AuroraRevit-Setup.exe"
    $releaseApi = "https://api.github.com/repos/$Repo/releases/tags/$Tag"

    Write-Step "Resolving GitHub release $Tag"
    $release = Invoke-RestMethod -Uri $releaseApi -Headers @{ "User-Agent" = "AuroraRevit-Updater" }
    $asset = @($release.assets) | Where-Object { $_.name -eq "AuroraRevit-Setup.exe" } | Select-Object -First 1
    if (-not $asset) { throw "AuroraRevit-Setup.exe was not found in GitHub release $Tag." }

    Write-Step "Downloading the official installer"
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $installer -UseBasicParsing
    $hash = (Get-FileHash -Path $installer -Algorithm SHA256).Hash
    Write-Host "Installer SHA256: $hash"

    if ($PSCmdlet.ShouldProcess($installer, "Run the AuroraRevit installer silently")) {
        $arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART"
        $process = Start-Process -FilePath $installer -ArgumentList $arguments -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw "AuroraRevit installer failed with exit code $($process.ExitCode)." }
    }
}

function Restore-Backup {
    if (-not (Test-Path $BackupPath)) { return }
    Write-Warning "Attempting to restore the previous pyRevit extension from $BackupPath"
    Remove-Item -Path $UserExtension -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path (Split-Path $UserExtension) -Force | Out-Null
    Copy-Item -Path (Join-Path $BackupPath "*") -Destination $UserExtension -Recurse -Force
}

try {
    Write-Host "AuroraRevit updater: $Mode / $Tag" -ForegroundColor White
    Stop-RevitSafely
    $null = Backup-CurrentExtension

    if ($Mode -eq "Source") {
        Deploy-Source
    } else {
        Deploy-Release
    }

    if (Test-Path $UserExtension) {
        Verify-PyRevitPayload $UserExtension
    } else {
        Write-Warning "The installer completed, but the expected pyRevit directory was not found yet: $UserExtension"
    }

    Write-Host "`nAuroraRevit update completed successfully." -ForegroundColor Green
    Write-Host "Restart Revit, reload pyRevit, then test CommandLogger and CommandLine."
    if ($LaunchRevit) {
        $revit = Get-ChildItem "C:\Program Files\Autodesk\Revit *\Revit.exe" -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
        if ($revit) { Start-Process $revit.FullName }
        else { Write-Warning "Revit.exe was not found under C:\Program Files\Autodesk." }
    }
}
catch {
    Write-Error $_
    Restore-Backup
    exit 1
}
