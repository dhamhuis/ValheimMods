[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ModDirectory,
    [string]$ValheimInstallDir,
    [string]$BepInExCoreDir,
    [string]$R2ModmanProfileDir
)

$ErrorActionPreference = 'Stop'
$modRoot = (Resolve-Path -LiteralPath $ModDirectory).Path
$sourceDirectory = Join-Path $modRoot 'src'
$projects = @(Get-ChildItem -LiteralPath $sourceDirectory -Filter '*.csproj' -File)
if ($projects.Count -ne 1) { throw "Expected one project in $sourceDirectory; found $($projects.Count)." }

[xml]$project = Get-Content -LiteralPath $projects[0].FullName -Raw
$properties = $project.Project.PropertyGroup | Where-Object { $_.AssemblyName -or $_.TargetFramework } | Select-Object -First 1
$assemblyName = [string]$properties.AssemblyName
$projectVersion = [string]$properties.Version
$packageDirectory = Join-Path $modRoot 'Thunderstore'
$manifestPath = Join-Path $packageDirectory 'manifest.json'
if (-not (Test-Path -LiteralPath $manifestPath)) { throw "Package manifest not found: $manifestPath" }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

if ($manifest.version_number -ne $projectVersion) {
    throw "Project Version ($projectVersion) and manifest version_number ($($manifest.version_number)) do not match."
}
if (-not $manifest.name) { throw 'Thunderstore manifest must define name.' }

foreach ($requiredFile in @('README.md', 'icon.png')) {
    if (-not (Test-Path -LiteralPath (Join-Path $packageDirectory $requiredFile))) {
        throw "Required package file not found: $requiredFile"
    }
}
if (-not (Test-Path -LiteralPath (Join-Path $modRoot 'CHANGELOG.md'))) { throw 'CHANGELOG.md not found.' }

$buildScript = Join-Path $PSScriptRoot 'Build-Mod.ps1'
$buildArguments = @{ ModDirectory = $modRoot }
if ($ValheimInstallDir) { $buildArguments.ValheimInstallDir = $ValheimInstallDir }
if ($BepInExCoreDir) { $buildArguments.BepInExCoreDir = $BepInExCoreDir }
if ($R2ModmanProfileDir) { $buildArguments.R2ModmanProfileDir = $R2ModmanProfileDir }
& $buildScript @buildArguments

$pluginDll = Join-Path $modRoot "dist\$assemblyName.dll"
if (-not (Test-Path -LiteralPath $pluginDll)) { throw "Plugin DLL not found: $pluginDll" }

$distDirectory = Join-Path $modRoot 'dist'
$archivePath = Join-Path $distDirectory "$($manifest.name)-$($manifest.version_number).zip"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (Test-Path -LiteralPath $archivePath) { Remove-Item -LiteralPath $archivePath -Force }
$archive = [System.IO.Compression.ZipFile]::Open($archivePath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($fileName in @('manifest.json', 'README.md', 'icon.png')) {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $packageDirectory $fileName), $fileName, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $modRoot 'CHANGELOG.md'), 'CHANGELOG.md', [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $pluginDll, "plugins/$assemblyName.dll", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
}
finally {
    $archive.Dispose()
}

Write-Output "Created $archivePath"