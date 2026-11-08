[CmdletBinding()]
param(
    [string]$ValheimInstallDir,
    [string]$BepInExCoreDir,
    [string]$R2ModmanProfileDir,
    [switch]$Deploy
)

$ErrorActionPreference = 'Stop'
$modDirectory = Split-Path -Parent $PSScriptRoot
$repositoryRoot = Split-Path -Parent $modDirectory
$sharedBuild = Join-Path $repositoryRoot 'scripts\Build-Mod.ps1'
$arguments = @{ ModDirectory = $modDirectory }
if ($ValheimInstallDir) { $arguments.ValheimInstallDir = $ValheimInstallDir }
if ($BepInExCoreDir) { $arguments.BepInExCoreDir = $BepInExCoreDir }
if ($R2ModmanProfileDir) { $arguments.R2ModmanProfileDir = $R2ModmanProfileDir }
if ($Deploy) { $arguments.Deploy = $true }

& $sharedBuild @arguments