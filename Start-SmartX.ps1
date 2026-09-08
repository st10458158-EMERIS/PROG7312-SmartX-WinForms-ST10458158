$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiPath = Join-Path $root 'src\SmartX.Api'
$clientPath = Join-Path $root 'src\SmartX.WinForms'
$healthUrl = 'http://localhost:7000/api/health'

Write-Host 'SmartX startup helper' -ForegroundColor Cyan

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host 'ERROR: dotnet was not found. Install the .NET 10 SDK / Visual Studio workloads first.' -ForegroundColor Red
    exit 1
}

Write-Host 'Starting SmartX.Api on http://localhost:7000 ...' -ForegroundColor Cyan
$apiCommand = "Set-Location -LiteralPath '$apiPath'; dotnet run"
Start-Process powershell -ArgumentList '-NoExit', '-Command', $apiCommand | Out-Null

Write-Host 'Waiting for API health check...' -ForegroundColor Yellow
$ready = $false
for ($attempt = 1; $attempt -le 20; $attempt++) {
    Start-Sleep -Seconds 1
    try {
        $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 2
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
            $ready = $true
            break
        }
    }
    catch {
        Write-Host "  API not ready yet ($attempt/20)..." -ForegroundColor DarkGray
    }
}

if (-not $ready) {
    Write-Host 'The API did not become healthy within 20 seconds.' -ForegroundColor Red
    Write-Host 'Check the API PowerShell window for SQL Server LocalDB or build errors.' -ForegroundColor Yellow
    exit 1
}

Write-Host 'API is healthy. Starting SmartX.WinForms ...' -ForegroundColor Green
$clientCommand = "Set-Location -LiteralPath '$clientPath'; dotnet run"
Start-Process powershell -ArgumentList '-NoExit', '-Command', $clientCommand | Out-Null

Write-Host 'SmartX started successfully.' -ForegroundColor Green
