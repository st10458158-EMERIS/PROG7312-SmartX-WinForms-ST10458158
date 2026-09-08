$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host 'SmartX prerequisite and build verification' -ForegroundColor Cyan

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host 'FAIL: dotnet was not found.' -ForegroundColor Red
    exit 1
}
Write-Host "PASS: .NET SDK $(dotnet --version)" -ForegroundColor Green

if (Get-Command sqllocaldb -ErrorAction SilentlyContinue) {
    $instances = sqllocaldb info
    if ($instances -match 'MSSQLLocalDB') {
        Write-Host 'PASS: SQL Server LocalDB MSSQLLocalDB is installed.' -ForegroundColor Green
    }
    else {
        Write-Host 'WARNING: sqllocaldb is available, but MSSQLLocalDB was not listed.' -ForegroundColor Yellow
    }
}
else {
    Write-Host 'WARNING: sqllocaldb command was not found. The API requires SQL Server LocalDB by default.' -ForegroundColor Yellow
}

Set-Location -LiteralPath $root
Write-Host 'Restoring solution...' -ForegroundColor Cyan
dotnet restore .\SmartX_WinForms.sln

Write-Host 'Building solution...' -ForegroundColor Cyan
dotnet build .\SmartX_WinForms.sln --no-restore

Write-Host 'PASS: Solution build completed. You can now run Start-SmartX.ps1 or configure multiple startup projects in Visual Studio.' -ForegroundColor Green
