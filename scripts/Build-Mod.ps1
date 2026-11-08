[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ModDirectory,
    [string]$ValheimInstallDir,
    [string]$BepInExCoreDir,
    [string]$R2ModmanProfileDir,
    [switch]$Deploy
)

$ErrorActionPreference = 'Stop'
$modRoot = (Resolve-Path -LiteralPath $ModDirectory).Path
$sourceDirectory = Join-Path $modRoot 'src'
$projects = @(Get-ChildItem -LiteralPath $sourceDirectory -Filter '*.csproj' -File)
if ($projects.Count -ne 1) { throw "Expected one project in $sourceDirectory; found $($projects.Count)." }

[xml]$project = Get-Content -LiteralPath $projects[0].FullName -Raw
$properties = $project.Project.PropertyGroup | Where-Object { $_.AssemblyName -or $_.TargetFramework } | Select-Object -First 1
$assemblyName = [string]$properties.AssemblyName
$targetFramework = [string]$properties.TargetFramework
if (-not $assemblyName -or -not $targetFramework) { throw "Project must define AssemblyName and TargetFramework: $($projects[0].FullName)" }
$usesCommonLibrary = @($project.Project.ItemGroup.ProjectReference |
    Where-Object { $_.Include -like '*ValheimMods.Common.csproj' }).Count -gt 0

$buildArgs = @('build', $projects[0].FullName, '--configuration', 'Release', '--verbosity', 'minimal')
if ($ValheimInstallDir) { $buildArgs += "-p:ValheimInstallDir=$ValheimInstallDir" }
if ($BepInExCoreDir) { $buildArgs += "-p:BepInExCoreDir=$BepInExCoreDir" }
if ($R2ModmanProfileDir) { $buildArgs += "-p:R2ModmanProfileDir=$R2ModmanProfileDir" }

& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw "Build failed for $assemblyName." }

$buildDll = Join-Path $sourceDirectory "bin\Release\$targetFramework\$assemblyName.dll"
if (-not (Test-Path -LiteralPath $buildDll)) { throw "Build output not found: $buildDll" }

$distDirectory = Join-Path $modRoot 'dist'
New-Item -ItemType Directory -Path $distDirectory -Force | Out-Null
Copy-Item -LiteralPath $buildDll -Destination (Join-Path $distDirectory "$assemblyName.dll") -Force

$commonDll = Join-Path $sourceDirectory "bin\Release\$targetFramework\ValheimMods.Common.dll"
if ($usesCommonLibrary) {
    if (-not (Test-Path -LiteralPath $commonDll)) { throw "Shared runtime library output not found: $commonDll" }
    Copy-Item -LiteralPath $commonDll -Destination (Join-Path $distDirectory 'ValheimMods.Common.dll') -Force
}

if ($Deploy) {
    if (-not $R2ModmanProfileDir) {
        if ($BepInExCoreDir) {
            $R2ModmanProfileDir = Split-Path -Parent (Split-Path -Parent $BepInExCoreDir)
        }
        else {
            $R2ModmanProfileDir = Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles\Valheim Mod Building'
        }
    }

    if (-not (Test-Path -LiteralPath (Join-Path $R2ModmanProfileDir 'BepInEx\core'))) {
        throw "BepInEx profile not found: $R2ModmanProfileDir"
    }
    $pluginDirectory = Join-Path $R2ModmanProfileDir "BepInEx\plugins\$assemblyName"
    New-Item -ItemType Directory -Path $pluginDirectory -Force | Out-Null
    Copy-Item -LiteralPath $buildDll -Destination (Join-Path $pluginDirectory "$assemblyName.dll") -Force
    if ($usesCommonLibrary) {
        Copy-Item -LiteralPath $commonDll -Destination (Join-Path $pluginDirectory 'ValheimMods.Common.dll') -Force
    }
    Write-Output "Deployed $assemblyName to $pluginDirectory"
}