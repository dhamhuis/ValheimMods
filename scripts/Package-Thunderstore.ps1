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
$temporaryPath = Join-Path $distDirectory (".$($manifest.name)-$([guid]::NewGuid()).tmp")
$files = [ordered]@{
    'manifest.json' = $manifestPath
    'README.md' = (Join-Path $packageDirectory 'README.md')
    'icon.png' = (Join-Path $packageDirectory 'icon.png')
    'CHANGELOG.md' = (Join-Path $modRoot 'CHANGELOG.md')
    "plugins/$assemblyName.dll" = $pluginDll
}
try {
    $archive = [System.IO.Compression.ZipFile]::Open($temporaryPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $files.GetEnumerator()) {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $entry.Value, $entry.Key, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $archive.Dispose()
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($temporaryPath)
    try {
        if ($archive.Entries.Count -ne $files.Count) { throw 'Unexpected package entry count.' }
        foreach ($entry in $files.GetEnumerator()) {
            $archived = $archive.GetEntry($entry.Key)
            if (-not $archived -or $archived.Length -eq 0) { throw "Missing or empty package entry: $($entry.Key)" }
            $stream = $archived.Open()
            $hash = [System.Security.Cryptography.SHA256]::Create()
            try {
                $actualHash = [BitConverter]::ToString($hash.ComputeHash($stream)).Replace('-', '')
                $expectedHash = (Get-FileHash -LiteralPath $entry.Value -Algorithm SHA256).Hash
                if ($actualHash -ne $expectedHash) { throw "Package entry differs from its source: $($entry.Key)" }
            }
            finally {
                $hash.Dispose()
                $stream.Dispose()
            }
        }
    }
    finally {
        $archive.Dispose()
    }

    if (Test-Path -LiteralPath $archivePath) {
        [System.IO.File]::Replace($temporaryPath, $archivePath, [NullString]::Value)
    }
    else {
        [System.IO.File]::Move($temporaryPath, $archivePath)
    }
}
finally {
    if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath -Force }
}

Write-Output "Created $archivePath"