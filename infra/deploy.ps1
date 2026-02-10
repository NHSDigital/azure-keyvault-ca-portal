param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    [string]$Location = "ukwest"
)

# Connect to Azure if not connected
if (-not (Get-AzContext)) {
    Connect-AzAccount
}

# Create Resource Group if it doesn't exist
$rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if (-not $rg) {
    Write-Host "Creating Resource Group '$ResourceGroupName' in '$Location'..."
    New-AzResourceGroup -Name $ResourceGroupName -Location $Location
}

# Deploy Bicep
Write-Host "Deploying Bicep template..."

$scriptPath = $PSScriptRoot
$templateFile = Join-Path $scriptPath "bicep\main.bicep"
$paramFile = Join-Path $scriptPath "bicep\main.bicepparam"

$deploymentArgs = @{
    ResourceGroupName = $ResourceGroupName
    TemplateFile      = $templateFile
    Verbose           = $true
}

if (Test-Path $paramFile) {
    Write-Host "Using parameter file: $paramFile"
    $deploymentArgs['TemplateParameterFile'] = $paramFile
}

New-AzResourceGroupDeployment @deploymentArgs
