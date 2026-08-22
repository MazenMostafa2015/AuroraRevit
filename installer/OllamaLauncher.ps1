$ErrorActionPreference = 'Stop'

$ollama = Get-Command ollama.exe -ErrorAction SilentlyContinue
if (-not $ollama) {
    Start-Process 'https://ollama.com/download/windows'
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show('Ollama was not found. The official Windows download page has been opened.', 'AuroraRevit AI (Local)') | Out-Null
    exit 0
}

$ready = $false
try {
    $client = New-Object System.Net.Http.HttpClient
    $client.Timeout = [TimeSpan]::FromMilliseconds(600)
    $response = $client.GetAsync('http://localhost:11434/api/tags').GetAwaiter().GetResult()
    $ready = $response.IsSuccessStatusCode
    $client.Dispose()
} catch {
    $ready = $false
}

if (-not $ready) {
    Start-Process -FilePath $ollama.Source -ArgumentList 'serve' -WindowStyle Hidden
    Start-Sleep -Milliseconds 700
}

Start-Process 'http://localhost:11434'
