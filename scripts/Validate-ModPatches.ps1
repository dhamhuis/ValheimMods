[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ModDirectory,
    [string]$ValheimInstallDir = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim',
    [string]$R2ModmanProfileDir = (Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles\Valheim Mod Building'),
    [string]$BepInExCoreDir
)

$ErrorActionPreference = 'Stop'
$modRoot = (Resolve-Path -LiteralPath $ModDirectory).Path
$projects = @(Get-ChildItem -LiteralPath (Join-Path $modRoot 'src') -Filter '*.csproj' -File)
if ($projects.Count -ne 1) { throw "Expected one project in $modRoot\src; found $($projects.Count)." }
[xml]$project = Get-Content -LiteralPath $projects[0].FullName -Raw
$properties = $project.Project.PropertyGroup | Where-Object { $_.AssemblyName -or $_.TargetFramework } | Select-Object -First 1
$assemblyName = [string]$properties.AssemblyName
if (-not $assemblyName) { throw "Project must define AssemblyName: $($projects[0].FullName)" }
$pluginPath = Join-Path $modRoot "dist\$assemblyName.dll"
if (-not (Test-Path -LiteralPath $pluginPath)) { throw "Plugin DLL not found: $pluginPath. Run the mod's build wrapper first." }
if (-not $BepInExCoreDir) { $BepInExCoreDir = Join-Path $R2ModmanProfileDir 'BepInEx\core' }
Add-Type -Path (Join-Path $BepInExCoreDir 'Mono.Cecil.dll')
$game = $null
$plugin = $null
$common = $null
$checked = 0
try {
    $game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $ValheimInstallDir 'valheim_Data\Managed\assembly_valheim.dll'))
    $plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($pluginPath)
    $assemblies = @($plugin)
    if (@($plugin.MainModule.GetTypeReferences() | Where-Object {
        $_.FullName -eq 'ValheimMods.Common.Crafting.RecipeStorage'
    }).Count -gt 0) {
        $commonPath = Join-Path $modRoot 'dist\ValheimMods.Common.dll'
        if (-not (Test-Path -LiteralPath $commonPath)) { throw "Shared recipe engine DLL not found: $commonPath" }
        $common = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($commonPath)
        $assemblies += $common
    }
    foreach ($type in ($assemblies | ForEach-Object { $_.MainModule.Types })) {
        $attributes = @($type.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' })
        if ($attributes.Count -eq 0) { continue }
        if ($attributes.Count -ne 1) { throw "$($type.Name): only a single class-level HarmonyPatch attribute is supported." }
        $attribute = $attributes[0]
        $argumentTypes = $attribute.ConstructorArguments.Type.FullName -join ','
        if ($argumentTypes -notin @('System.Type,System.String', 'System.Type,System.String,System.Type[]')) {
            throw "$($type.Name): unsupported HarmonyPatch declaration ($argumentTypes)."
        }
        $targetName = $attribute.ConstructorArguments[0].Value.FullName
        $methodName = [string]$attribute.ConstructorArguments[1].Value
        $targetType = $game.MainModule.Types | Where-Object FullName -eq $targetName
        $targets = @($targetType.Methods | Where-Object Name -eq $methodName)
        if ($attribute.ConstructorArguments.Count -eq 3) {
            $signature = @($attribute.ConstructorArguments[2].Value | ForEach-Object { $_.Value.FullName }) -join ','
            $targets = @($targets | Where-Object { ($_.Parameters.ParameterType.FullName -join ',') -eq $signature })
        }
        if ($targets.Count -ne 1) { throw "$($type.Name): expected one target $targetName.$methodName, found $($targets.Count)." }
        $target = $targets[0]
        foreach ($method in $type.Methods) {
            $annotations = @($method.CustomAttributes | Where-Object {
                $_.AttributeType.FullName -in @('HarmonyLib.HarmonyPrefix', 'HarmonyLib.HarmonyPostfix', 'HarmonyLib.HarmonyFinalizer')
            })
            if ($annotations.Count -eq 0) { continue }
            foreach ($parameter in $method.Parameters) {
                if ($parameter.Name -eq '__instance') {
                    if ($target.IsStatic) { throw "$($type.Name): __instance cannot bind a static target." }
                    if ($parameter.ParameterType.FullName -ne $targetName) { throw "$($type.Name): incompatible __instance type." }
                    continue
                }
                if ($parameter.Name -in @('__state', '__result', '__exception')) { continue }
                if ($parameter.Name -match '^__(\d+)$') {
                    if ([int]$Matches[1] -ge $target.Parameters.Count) { throw "$($type.Name): Harmony argument index out of range: $($parameter.Name)." }
                    $original = $target.Parameters[[int]$Matches[1]]
                }
                else {
                    $original = $target.Parameters | Where-Object Name -eq $parameter.Name
                }
                if (-not $original) { throw "$($type.Name): unknown Harmony argument $($parameter.Name)." }
                if ($original.ParameterType.FullName.TrimEnd('&') -ne $parameter.ParameterType.FullName.TrimEnd('&')) {
                    throw "$($type.Name): incompatible Harmony argument $($parameter.Name)."
                }
            }
            $checked++
        }
        Write-Output "Verified $($type.Name) -> $($target.FullName)"
    }
    if ($checked -eq 0) { throw "No supported Harmony patch methods found in $assemblyName." }
    Write-Output "Verified $checked Harmony patch method signatures for $assemblyName against the installed game. In-game startup is still required."
}
finally {
    if ($common) { $common.Dispose() }
    if ($plugin) { $plugin.Dispose() }
    if ($game) { $game.Dispose() }
}