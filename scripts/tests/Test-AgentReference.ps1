[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$reference = Join-Path $root 'docs\reference'
$indexPath = Join-Path $reference 'README.md'
$skillPath = Join-Path $root '.github\skills\valheim-api-discovery\SKILL.md'
$instructionsPath = Join-Path $root 'AGENTS.md'
$index = Get-Content -LiteralPath $indexPath -Raw
$instructions = Get-Content -LiteralPath $instructionsPath -Raw
$skill = Get-Content -LiteralPath $skillPath -Raw
$checks = 0

function Assert-Reference([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
}

Assert-Reference ($instructions.Contains('(docs/reference/README.md)')) 'Root instructions must link the reference index.'
Assert-Reference ($instructions.Contains('(.github/skills/valheim-api-discovery/SKILL.md)')) 'Root instructions must link the discovery skill.'
Assert-Reference ($instructions.Contains('before searching libraries or inspecting assemblies')) 'Root instructions must require consult-before-discovery.'
Assert-Reference ($instructions.Contains('As part of the same task, update the reference')) 'Root instructions must require automatic maintenance.'
Assert-Reference ($skill -match '\A---\r?\nname: valheim-api-discovery\r?\ndescription: "[^\r\n]+"\r?\n---') 'Discovery skill must have matching name and a quoted description.'

$topics = @(Get-ChildItem -LiteralPath $reference -Filter '*.md' -File | Where-Object Name -ne 'README.md')
Assert-Reference ($topics.Count -gt 0) 'Reference must contain topic pages.'
foreach ($topic in $topics) {
    $text = Get-Content -LiteralPath $topic.FullName -Raw
    Assert-Reference ($index.Contains("($($topic.Name))")) "Topic missing from reference index: $($topic.Name)"
    $status = [regex]::Match($text, '(?m)^Status: (Verified|Needs revalidation|Unknown / not investigated)(?: - [^\r\n]+)?\.\r?$')
    Assert-Reference ($status.Success) "Missing or invalid status: $($topic.Name)"
    $date = [regex]::Match($text, '(?m)^Last verified: (\d{4}-\d{2}-\d{2}|Never)\.\r?$')
    Assert-Reference ($date.Success) "Missing or invalid verification date: $($topic.Name)"
    if ($date.Groups[1].Value -eq 'Never') {
        Assert-Reference ($status.Groups[1].Value -eq 'Unknown / not investigated') "Only unknown topics may use Last verified: Never: $($topic.Name)"
    }
    else {
        $parsed = [datetime]::MinValue
        Assert-Reference ([datetime]::TryParseExact($date.Groups[1].Value, 'yyyy-MM-dd',
            [cultureinfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::None, [ref]$parsed)) "Impossible verification date: $($topic.Name)"
    }
    foreach ($field in @('Evidence', 'Limitations')) {
        Assert-Reference ($text -match "(?m)^${field}: \S") "Missing ${field}: $($topic.Name)"
    }
    foreach ($heading in @('Use when', 'API and behavior', 'Pitfalls', 'Unknowns')) {
        Assert-Reference ($text -match "(?m)^## $([regex]::Escape($heading))\r?$") "Missing section ${heading}: $($topic.Name)"
    }
}

$files = @($indexPath, $skillPath, $instructionsPath, (Join-Path $root 'README.md'),
    (Join-Path $root 'docs\Development.md'),
    (Join-Path $root '.github\skills\valheim-mod-build\SKILL.md')) + @($topics.FullName)
foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file -Raw
    Assert-Reference (-not [char]::IsWhiteSpace($text[$text.Length - 1])) "Unexpected whitespace at EOF: $file"
    foreach ($link in [regex]::Matches($text, '\[[^\]\r\n]+\]\(([^)\r\n]+)\)')) {
        $target = $link.Groups[1].Value.Trim('<', '>')
        if ($target -match '^[a-zA-Z][a-zA-Z0-9+.-]*:' -or $target.StartsWith('#')) { continue }
        $relative = ($target -split '#', 2)[0]
        $resolved = Join-Path (Split-Path -Parent $file) ($relative.Replace('/', '\'))
        Assert-Reference (Test-Path -LiteralPath $resolved) "Broken local file link in ${file}: $target"
    }
}

Write-Output "Verified $checks reference structure, file-link, and workflow checks. Agent compliance and game behavior require separate verification."