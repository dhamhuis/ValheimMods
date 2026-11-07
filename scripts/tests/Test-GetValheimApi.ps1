[CmdletBinding()]
param(
    [string]$BepInExCoreDir = (Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles\Valheim Mod Building\BepInEx\core')
)

$ErrorActionPreference = 'Stop'
$inspector = Join-Path (Split-Path -Parent $PSScriptRoot) 'Get-ValheimApi.ps1'
Add-Type -Path (Join-Path $BepInExCoreDir 'Mono.Cecil.dll')
$fixture = Join-Path ([System.IO.Path]::GetTempPath()) ("ValheimMod-api-test-" + [guid]::NewGuid())
$assembly = $null
$passed = 0

function Assert-Api([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:passed++
    Write-Output "PASS $Message"
}

try {
    New-Item -ItemType Directory -Path $fixture | Out-Null
    $path = Join-Path $fixture 'ApiFixture.dll'
    $assembly = [Mono.Cecil.AssemblyDefinition]::CreateAssembly(
        [Mono.Cecil.AssemblyNameDefinition]::new('ApiFixture', [version]'1.2.3.4'),
        'ApiFixture', [Mono.Cecil.ModuleKind]::Dll)
    $module = $assembly.MainModule
    $type = [Mono.Cecil.TypeDefinition]::new('Fixture', 'Inventory',
        [Mono.Cecil.TypeAttributes]::Public, $module.TypeSystem.Object)
    $module.Types.Add($type)
    $method = [Mono.Cecil.MethodDefinition]::new('Count',
        [Mono.Cecil.MethodAttributes]'Public, Static', $module.TypeSystem.Int32)
    $method.Parameters.Add([Mono.Cecil.ParameterDefinition]::new('name',
        [Mono.Cecil.ParameterAttributes]::None, $module.TypeSystem.String))
    $method.Body.Instructions.Add([Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0))
    $method.Body.Instructions.Add([Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret))
    $type.Methods.Add($method)
    $overload = [Mono.Cecil.MethodDefinition]::new('Count',
        [Mono.Cecil.MethodAttributes]'Public, Static', $module.TypeSystem.Int32)
    $overload.Body.Instructions.Add([Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0))
    $overload.Body.Instructions.Add([Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret))
    $type.Methods.Add($overload)
    $getter = [Mono.Cecil.MethodDefinition]::new('get_Amount',
        [Mono.Cecil.MethodAttributes]'Public, Static, SpecialName, HideBySig', $module.TypeSystem.Int32)
    $getter.Body.Instructions.Add([Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0))
    $getter.Body.Instructions.Add([Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret))
    $type.Methods.Add($getter)
    $setter = [Mono.Cecil.MethodDefinition]::new('set_Amount',
        [Mono.Cecil.MethodAttributes]'Private, Static, SpecialName, HideBySig', $module.TypeSystem.Void)
    $setter.Parameters.Add([Mono.Cecil.ParameterDefinition]::new('value',
        [Mono.Cecil.ParameterAttributes]::None, $module.TypeSystem.Int32))
    $setter.Body.Instructions.Add([Mono.Cecil.Cil.Instruction]::Create([Mono.Cecil.Cil.OpCodes]::Ret))
    $type.Methods.Add($setter)
    $property = [Mono.Cecil.PropertyDefinition]::new('Amount',
        [Mono.Cecil.PropertyAttributes]::None, $module.TypeSystem.Int32)
    $property.GetMethod = $getter
    $property.SetMethod = $setter
    $type.Properties.Add($property)
    $nested = [Mono.Cecil.TypeDefinition]::new('', 'Item',
        [Mono.Cecil.TypeAttributes]::NestedPublic, $module.TypeSystem.Object)
    $nested.Fields.Add([Mono.Cecil.FieldDefinition]::new('Stack',
        [Mono.Cecil.FieldAttributes]::Public, $module.TypeSystem.Int32))
    $type.NestedTypes.Add($nested)
    $assembly.Write($path)
    $assembly.Dispose()
    $assembly = $null

    $identity = @(& $inspector -AssemblyPath $path -BepInExCoreDir $BepInExCoreDir)
    Assert-Api ($identity.Count -eq 3 -and $identity[0] -match 'ApiFixture, Version=1.2.3.4') 'identity-only output is bounded and identifies the fixture'
    Assert-Api ($identity[2] -ceq "SHA256: $((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)") 'fingerprint matches the inspected DLL'
    $output = @(& $inspector -AssemblyPath $path -BepInExCoreDir $BepInExCoreDir -TypeName Fixture.Inventory -MemberName Count,Amount)
    Assert-Api (@($output | Where-Object { $_ -match '^System.Int32 Fixture.Inventory::Count\(' }).Count -eq 2) 'all selected method overloads are reported'
    Assert-Api ($output -contains '  [0] System.String name | None') 'original parameter name and type are reported'
    Assert-Api ([bool]($output -match 'get_Amount\(\) \| .*Public') -and [bool]($output -match 'set_Amount\(System.Int32\) \| .*Private')) 'property getter and setter visibility are distinct'
    $output = @(& $inspector -AssemblyPath $path -BepInExCoreDir $BepInExCoreDir -TypeName 'Fixture.Inventory/Item' -MemberName Stack)
    Assert-Api ([bool]($output -match 'Fixture.Inventory/Item::Stack')) 'nested type fields are inspected'

    $invalid = @(
        @{ Label = 'type without member'; Selection = @{ TypeName = 'Fixture.Inventory' }; Error = 'Specify both' },
        @{ Label = 'member without type'; Selection = @{ MemberName = @('Count') }; Error = 'Specify both' },
        @{ Label = 'empty type and member'; Selection = @{ TypeName = ''; MemberName = @() }; Error = 'Cannot validate argument' },
        @{ Label = 'empty member array'; Selection = @{ TypeName = 'Fixture.Inventory'; MemberName = @() }; Error = 'Cannot validate argument' },
        @{ Label = 'blank type'; Selection = @{ TypeName = ' '; MemberName = @('Count') }; Error = 'must not be blank' },
        @{ Label = 'blank member among valid names'; Selection = @{ TypeName = 'Fixture.Inventory'; MemberName = @('Count', ' ') }; Error = 'must not be blank' },
        @{ Label = 'missing type'; Selection = @{ TypeName = 'Missing'; MemberName = @('Count') }; Error = 'Type not found' },
        @{ Label = 'incorrect type case'; Selection = @{ TypeName = 'fixture.inventory'; MemberName = @('Count') }; Error = 'Type not found' },
        @{ Label = 'incorrect member case'; Selection = @{ TypeName = 'Fixture.Inventory'; MemberName = @('count') }; Error = 'Member not found' }
    )
    foreach ($case in $invalid) {
        $selection = $case.Selection
        $failure = $null
        try {
            & $inspector -AssemblyPath $path -BepInExCoreDir $BepInExCoreDir @selection | Out-Null
        }
        catch {
            $failure = $_
        }
        Assert-Api ($null -ne $failure -and $failure.Exception.Message -match $case.Error) "explicit expected error for $($case.Label)"
    }
    Assert-Api ((Get-ChildItem -LiteralPath $fixture -File).Count -eq 1) 'inspector writes no files'
    Assert-Api ($identity[2] -ceq "SHA256: $((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)") 'inspection preserves the DLL byte for byte'
    Write-Output "Verified $passed API inspector regression checks using a synthetic assembly; no Valheim installation required."
}
finally {
    if ($null -ne $assembly) { $assembly.Dispose() }
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}