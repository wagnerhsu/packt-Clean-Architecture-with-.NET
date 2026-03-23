# Ensure script is running in PowerShell Core 7.4 or later
#requires -Version 7.4

# ------------------------------------------------------------
# Guard: Must be run from the chapter solution root
# ------------------------------------------------------------
if (-not (Test-Path "./CleanCart.NET.sln")) {
    Write-Error "This script must be run from the CleanCart.NET solution root."
    Write-Error "Example:"
    Write-Error "  cd ch08/CleanCart.NET"
    Write-Error "  .\scripts\Initialize-Database.ps1"
    exit 1
}

# ------------------------------------------------------------
# Azure Key Vault Configuration
# ------------------------------------------------------------
$keyVaultName = "podyssey-local"
$secretName = "SqlServer--ConnectionString"

# ------------------------------------------------------------
# Retrieve connection string via helper
# ------------------------------------------------------------
$helperPath = Join-Path $PSScriptRoot "helpers/Get-KeyVaultConnectionString.ps1"

if (-not (Test-Path $helperPath)) {
    Write-Error "Helper script not found: $helperPath"
    exit 1
}

$connString = & $helperPath `
    -KeyVaultName $keyVaultName `
    -SecretName $secretName

if (-not $connString) {
    Write-Error "Failed to retrieve connection string from helper."
    exit 1
}

# ------------------------------------------------------------
# Parse Connection String
# ------------------------------------------------------------
$connectionParts = @{}
$connString.Split(";") | ForEach-Object {
    if ($_ -match "=") {
        $k,$v = $_.Split("=",2)
        $connectionParts[$k.Trim()] = $v.Trim()
    }
}

$saPassword = $connectionParts["Password"]
$server = $connectionParts["Server"]

if (-not $saPassword) {
    Write-Error "Could not determine SQL Server password from connection string."
    exit 1
}

# Extract port (example: localhost,4000)
if ($server -match ",(\d+)$") {
    $port = $Matches[1]
}
else {
    $port = "1433"
}

Write-Host "Using SQL Server port: $port"

# ------------------------------------------------------------
# SQL Server Container Setup
# ------------------------------------------------------------
Write-Host "Starting SQL Server Docker container..."

docker rm odyssey_sqlserver -f 2>$null | Out-Null

docker run `
    --name odyssey_sqlserver `
    -e "ACCEPT_EULA=Y" `
    -e "SA_PASSWORD=$saPassword" `
    -p "$port`:1433" `
    -d mcr.microsoft.com/mssql/server:2022-latest | Out-Null

# ------------------------------------------------------------
# Wait for SQL Server to be available
# ------------------------------------------------------------
Write-Host "Waiting for SQL Server to start..."

while ((Test-Connection localhost -TcpPort $port -Detailed).Status -ne 'Success') {
    Write-Host "SQL Server not ready yet. Waiting 5 seconds..."
    Start-Sleep -Seconds 5
}

Write-Host "SQL Server is ready." -ForegroundColor Green

# ------------------------------------------------------------
# Apply Migrations
# ------------------------------------------------------------
Write-Host "Applying EF Core migrations..."

& "$PSScriptRoot/Start-Migrations.ps1"