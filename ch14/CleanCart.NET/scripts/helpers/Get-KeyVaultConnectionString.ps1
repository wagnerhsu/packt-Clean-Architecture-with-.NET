#requires -Version 7.4

param(
    [Parameter(Mandatory = $true)]
    [string]$KeyVaultName,

    [string]$SecretName = "SqlServer--ConnectionString"
)

function ExitOnError {
    param ([string]$Message)
    Write-Error $Message
    exit 1
}

Write-Host "Preparing Azure authentication..."

# Ensure Azure CLI exists
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    ExitOnError "Azure CLI is required. Install from https://learn.microsoft.com/cli/azure/install-azure-cli"
}

# ------------------------------------------------------------
# Tenant Configuration (cached locally)
# ------------------------------------------------------------
$configDir = Join-Path $PSScriptRoot "../.config"
$configDir = [System.IO.Path]::GetFullPath($configDir)

$tenantFile = Join-Path $configDir "tenant-id"

if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir | Out-Null
}

if (Test-Path $tenantFile) {
    $tenantId = Get-Content $tenantFile
    Write-Host "Using cached Azure tenant: $tenantId" -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "Azure Authentication"
    Write-Host "Enter the Azure Tenant ID (Directory ID):"

    $tenantId = Read-Host "Tenant ID"

    if (-not $tenantId) {
        ExitOnError "Tenant ID is required."
    }

    $tenantId | Out-File $tenantFile
    Write-Host "Tenant ID saved for future runs." -ForegroundColor Yellow
}

# ------------------------------------------------------------
# Azure Authentication
# ------------------------------------------------------------
Write-Host "Signing into Azure tenant $tenantId..."
az login --tenant $tenantId | Out-Null

# ------------------------------------------------------------
# Locate Key Vault Subscription
# ------------------------------------------------------------
Write-Host "Searching for Key Vault '$KeyVaultName'..."

$subscriptions = az account list --query "[].id" --output tsv
$keyVaultSubscription = $null

foreach ($sub in $subscriptions) {
    az account set --subscription $sub | Out-Null

    $exists = az keyvault show `
        --name $KeyVaultName `
        --query name `
        --output tsv 2>$null

    if ($exists) {
        $keyVaultSubscription = $sub
        break
    }
}

if (-not $keyVaultSubscription) {
    ExitOnError "Could not locate Key Vault '$KeyVaultName'."
}

az account set --subscription $keyVaultSubscription

$subscriptionName = az account show --query name --output tsv
Write-Host "Using subscription: $subscriptionName" -ForegroundColor Green

# ------------------------------------------------------------
# Retrieve Secret
# ------------------------------------------------------------
Write-Host "Retrieving connection string..."

$connString = az keyvault secret show `
    --vault-name $KeyVaultName `
    --name $SecretName `
    --query value `
    --output tsv

if (-not $connString) {
    ExitOnError "Failed to retrieve secret '$SecretName'."
}

Write-Host "Connection string retrieved." -ForegroundColor Green

return $connString