[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryScripts = Split-Path -Parent $PSScriptRoot
$fixture = Join-Path ([System.IO.Path]::GetTempPath()) ("ValheimMod-package-test-" + [guid]::NewGuid())
$mod = Join-Path $fixture 'TestMod'
$scripts = Join-Path $fixture 'scripts'
$archivePath = Join-Path $mod 'dist\TestMod-1.0.0.zip'
$passed = 0

function Assert-Condition([bool]$Condition, [string]$Label) {
    if (-not $Condition) { throw $Label }
    $script:passed++
    Write-Output "PASS $Label"
}

function Invoke-Package([switch]$ExpectFailure) {
    $failed = $false
    try {
        & (Join-Path $scripts 'Package-Thunderstore.ps1') -ModDirectory $mod
    }
    catch {
        if (-not $ExpectFailure) { throw }
        $failed = $true
        Write-Output "Expected packaging failure: $($_.Exception.Message)"
    }
    if ($ExpectFailure -and -not $failed) { throw 'Packaging unexpectedly succeeded.' }
}

try {
    New-Item -ItemType Directory -Path $scripts, (Join-Path $mod 'src'), (Join-Path $mod 'Thunderstore') | Out-Null
    'test repository license' | Set-Content -LiteralPath (Join-Path $fixture 'LICENSE')
    Copy-Item -LiteralPath (Join-Path $repositoryScripts 'Package-Thunderstore.ps1') -Destination $scripts
    # Isolate archive handling from compilation and real distribution files.
    @'
param([string]$ModDirectory)
$dist = Join-Path $ModDirectory 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $dist 'TestMod.dll'), 'test plugin payload')
$commonSource = Join-Path (Join-Path $ModDirectory 'src') 'ValheimMods.Common.dll'
if (Test-Path -LiteralPath $commonSource) {
    Copy-Item -LiteralPath $commonSource -Destination (Join-Path $dist 'ValheimMods.Common.dll') -Force
}
'@ | Set-Content -LiteralPath (Join-Path $scripts 'Build-Mod.ps1')
    '<Project><PropertyGroup><AssemblyName>TestMod</AssemblyName><Version>1.0.0</Version></PropertyGroup></Project>' |
        Set-Content -LiteralPath (Join-Path $mod 'src\TestMod.csproj')
    '{"name":"TestMod","version_number":"1.0.0"}' |
        Set-Content -LiteralPath (Join-Path $mod 'Thunderstore\manifest.json')
    'package readme' | Set-Content -LiteralPath (Join-Path $mod 'Thunderstore\README.md')
    'changelog' | Set-Content -LiteralPath (Join-Path $mod 'CHANGELOG.md')
    $icon = Join-Path $mod 'Thunderstore\icon.png'
    [System.IO.File]::WriteAllBytes($icon, [byte[]]@(1, 2, 3, 4))

    Invoke-Package
    Assert-Condition (Test-Path -LiteralPath $archivePath) 'creates first validated archive'
    $archive = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $names = @($archive.Entries.FullName | Sort-Object)
        $expected = @('manifest.json', 'README.md', 'icon.png', 'CHANGELOG.md', 'LICENSE', 'plugins/TestMod.dll') | Sort-Object
        Assert-Condition (-not (Compare-Object $expected $names)) 'archive contains exactly the six player-facing files'
    }
    finally {
        $archive.Dispose()
    }

    'updated package readme' | Set-Content -LiteralPath (Join-Path $mod 'Thunderstore\README.md')
    '<Project><PropertyGroup><AssemblyName>TestMod</AssemblyName><Version>1.0.0</Version></PropertyGroup><ItemGroup><ProjectReference Include="ValheimMods.Common.csproj" /></ItemGroup></Project>' |
        Set-Content -LiteralPath (Join-Path $mod 'src\TestMod.csproj')
    [System.IO.File]::WriteAllText((Join-Path $mod 'src\ValheimMods.Common.dll'), 'shared runtime library')
    Invoke-Package
    $hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
    $archive = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
    $reader = [System.IO.StreamReader]::new($archive.GetEntry('README.md').Open())
    try {
        Assert-Condition ($reader.ReadToEnd().Trim() -eq 'updated package readme') 'replaces existing archive with new contents'
        $library = $archive.GetEntry('plugins/ValheimMods.Common.dll')
        Assert-Condition ($library -and $library.Length -gt 0) 'includes the shared runtime library when present'
        $libraryReader = [System.IO.StreamReader]::new($library.Open())
        try {
            Assert-Condition ($libraryReader.ReadToEnd() -eq 'shared runtime library') 'packages the exact shared-library build output'
        }
        finally {
            $libraryReader.Dispose()
        }
    }
    finally {
        $reader.Dispose()
        $archive.Dispose()
    }

    $locked = [System.IO.File]::Open($icon, 'Open', 'Read', 'None')
    try {
        Invoke-Package -ExpectFailure
        Assert-Condition ((Get-FileHash -LiteralPath $archivePath).Hash -eq $hash) 'input read failure preserves previous ZIP byte for byte'
    }
    finally {
        $locked.Dispose()
    }

    $locked = [System.IO.File]::Open($archivePath, 'Open', 'Read', 'Read')
    try {
        Invoke-Package -ExpectFailure
        Assert-Condition ((Get-FileHash -LiteralPath $archivePath).Hash -eq $hash) 'replacement failure preserves previous ZIP byte for byte'
    }
    finally {
        $locked.Dispose()
    }

    [System.IO.File]::WriteAllBytes($icon, [byte[]]@())
    Invoke-Package -ExpectFailure
    Assert-Condition ((Get-FileHash -LiteralPath $archivePath).Hash -eq $hash) 'archive validation failure preserves previous ZIP byte for byte'
    Assert-Condition (@(Get-ChildItem -LiteralPath (Join-Path $mod 'dist') -Filter '*.tmp' -Force).Count -eq 0) 'failure paths clean up temporary archives'

    Remove-Item -LiteralPath $archivePath
    Invoke-Package -ExpectFailure
    Assert-Condition (-not (Test-Path -LiteralPath $archivePath)) 'failed first package does not leave a partial distribution ZIP'
    Write-Output "$passed packaging assertions passed."
}
finally {
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}