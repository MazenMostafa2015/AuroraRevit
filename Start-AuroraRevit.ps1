[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $ReleaseRoot = (Join-Path $PSScriptRoot "Release")
)

$ErrorActionPreference = "Stop"
$proxyDirectory = Join-Path $ReleaseRoot "AiProxy"
$proxyPath = Join-Path $proxyDirectory "AiProxy.exe"
$proxyGuiPath = Join-Path $ReleaseRoot "AiProxyGui\AuroraRevit.ProxyGui.exe"
$logDirectory = Join-Path $ReleaseRoot "Logs"
$stdoutLog = Join-Path $logDirectory "AiProxy.stdout.log"
$stderrLog = Join-Path $logDirectory "AiProxy.stderr.log"

Write-Host "Aurora Revit Assistant startup" -ForegroundColor Cyan
Write-Host "Checking for .NET 8..."

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnet8Installed = $false
if ($null -ne $dotnetCommand) {
    $runtimeList = & $dotnetCommand.Source --list-runtimes 2>$null
    $dotnet8Installed = @($runtimeList | Where-Object { $_ -match '^Microsoft\.NETCore\.App 8\.' }).Count -gt 0
}

if ($dotnet8Installed) {
    Write-Host ".NET 8 runtime detected." -ForegroundColor Green
}
else {
    Write-Warning ".NET 8 runtime was not detected. The published AiProxy.exe is self-contained and normally includes the runtime; continuing with the bundled executable."
}

$launcherPath = if (Test-Path -LiteralPath $proxyGuiPath) { $proxyGuiPath } else { $proxyPath }
if (-not (Test-Path -LiteralPath $launcherPath)) {
    throw "Neither AuroraRevit.ProxyGui.exe nor AiProxy.exe was found under '$ReleaseRoot'. Build the release package first, or pass -ReleaseRoot with the correct package path."
}

if (-not (Test-Path -LiteralPath $logDirectory)) {
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
}

$existingProxy = Get-Process -Name "AuroraRevit.ProxyGui", "AiProxy" -ErrorAction SilentlyContinue
if ($null -ne $existingProxy) {
    Write-Host "AiProxy is already running. Reusing the existing process." -ForegroundColor Yellow
}
else {
    $startInfo = @{
        FilePath               = $launcherPath
        ArgumentList           = if ($launcherPath -eq $proxyGuiPath) { @() } else { @("--urls", "http://localhost:5000") }
        WorkingDirectory       = Split-Path -Parent $launcherPath
        WindowStyle            = if ($launcherPath -eq $proxyGuiPath) { "Normal" } else { "Hidden" }
        CreateNoWindow         = $true
        RedirectStandardOutput = $stdoutLog
        RedirectStandardError  = $stderrLog
        PassThru                = $true
    }

    $proxyProcess = Start-Process @startInfo
    Start-Sleep -Milliseconds 800
    if ($proxyProcess.HasExited) {
        throw "AiProxy.exe exited immediately with code $($proxyProcess.ExitCode). Review '$stderrLog'."
    }

    Write-Host "Aurora proxy launcher started with process ID $($proxyProcess.Id)." -ForegroundColor Green
}

Start-Sleep -Milliseconds 800
$activeEndpoint = @("http://localhost:5000", "http://localhost:5001") | Where-Object {
    try {
        Invoke-RestMethod -Uri ($_ + "/health") -TimeoutSec 2 -ErrorAction Stop | Out-Null
        $true
    }
    catch {
        $false
    }
} | Select-Object -First 1

if ($null -eq $activeEndpoint) {
    $activeEndpoint = "http://localhost:5000 or http://localhost:5001"
}

Write-Host "Proxy endpoint: $activeEndpoint" -ForegroundColor Gray
Write-Host "Open Revit 2023, 2024, or 2025 now, then open the Aurora AI Assistant dockable panel." -ForegroundColor Cyan
