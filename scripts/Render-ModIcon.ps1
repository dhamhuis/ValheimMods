[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ModDirectory,
    [string]$EdgePath = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $EdgePath)) { throw 'Microsoft Edge is required to render the SVG icon.' }
$modRoot = (Resolve-Path -LiteralPath $ModDirectory).Path
$repositoryRoot = Split-Path -Parent $PSScriptRoot
[xml]$icon = Get-Content -LiteralPath (Join-Path $modRoot 'assets\icon.svg') -Raw
[xml]$frame = Get-Content -LiteralPath (Join-Path $repositoryRoot 'assets\icons\valheim-wood-frame.svg') -Raw
$images = @($icon.DocumentElement.ChildNodes | Where-Object {
    $_.LocalName -eq 'image' -and $_.GetAttribute('href') -eq '../../assets/icons/valheim-wood-frame.svg'
})
if ($images.Count -ne 1) { throw 'Expected one image referencing the shared valheim-wood-frame.svg in assets\icon.svg.' }
$image = $images[0]
$icon.DocumentElement.ReplaceChild($icon.ImportNode($frame.DocumentElement, $true), $image) | Out-Null
$icon.DocumentElement.SetAttribute('width', '256')
$icon.DocumentElement.SetAttribute('height', '256')
$temporary = Join-Path ([System.IO.Path]::GetTempPath()) ("ValheimMod-icon-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $temporary | Out-Null
$svgPath = Join-Path $temporary 'icon.svg'
$screenshot = Join-Path $temporary 'screenshot.png'
$profile = Join-Path $temporary 'edge-profile'
try {
    $icon.Save($svgPath)
    $arguments = @(
        '--headless', '--disable-gpu', '--no-first-run', '--hide-scrollbars',
        '--force-device-scale-factor=1', '--window-size=512,512',
        "--user-data-dir=`"$profile`"", "--screenshot=`"$screenshot`"",
        ([uri]$svgPath).AbsoluteUri
    )
    $process = Start-Process -FilePath $EdgePath -ArgumentList $arguments -PassThru
    if (-not $process.WaitForExit(30000)) {
        Stop-Process -Id $process.Id
        throw 'Icon rendering timed out.'
    }
    if (-not (Test-Path -LiteralPath $screenshot)) { throw 'Edge did not produce an icon screenshot.' }
    Add-Type -AssemblyName System.Drawing
    $bitmap = [System.Drawing.Bitmap]::new($screenshot)
    try {
        $crop = $bitmap.Clone([System.Drawing.Rectangle]::new(0, 0, 256, 256), $bitmap.PixelFormat)
        try {
            $crop.Save((Join-Path $modRoot 'Thunderstore\icon.png'), [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $crop.Dispose() }
    }
    finally { $bitmap.Dispose() }
    Write-Output "Rendered $(Join-Path $modRoot 'Thunderstore\icon.png') (256x256)."
}
finally {
    if (Test-Path -LiteralPath $svgPath) { Remove-Item -LiteralPath $svgPath -Force }
    if (Test-Path -LiteralPath $screenshot) { Remove-Item -LiteralPath $screenshot -Force }
    if (Test-Path -LiteralPath $profile) { Remove-Item -LiteralPath $profile -Recurse -Force }
    Remove-Item -LiteralPath $temporary -Force
}