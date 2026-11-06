[CmdletBinding()]
param(
    [string]$ValheimInstallDir,
    [string]$BepInExCoreDir,
    [string]$R2ModmanProfileDir
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = Split-Path -Parent $projectRoot
$thunderstoreDirectory = Join-Path $projectRoot 'Thunderstore'
$manifestPath = Join-Path $thunderstoreDirectory 'manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$pluginSource = Get-Content -LiteralPath (Join-Path $projectRoot 'src\AutoRepairPlugin.cs') -Raw
$versionPattern = 'PluginVersion\s*=\s*"' + [regex]::Escape([string]$manifest.version_number) + '"'
if ($pluginSource -notmatch $versionPattern) {
    throw 'PluginVersion and Thunderstore manifest version_number do not match.'
}

$sharedPack = Join-Path $repositoryRoot 'scripts\Package-Thunderstore.ps1'
$arguments = @{ ModDirectory = $projectRoot }
if ($ValheimInstallDir) { $arguments.ValheimInstallDir = $ValheimInstallDir }
if ($BepInExCoreDir) { $arguments.BepInExCoreDir = $BepInExCoreDir }
if ($R2ModmanProfileDir) { $arguments.R2ModmanProfileDir = $R2ModmanProfileDir }

& $sharedPack @arguments