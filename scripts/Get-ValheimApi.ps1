[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyPath,
    [ValidateNotNullOrEmpty()]
    [string]$TypeName,
    [ValidateNotNullOrEmpty()]
    [string[]]$MemberName,
    [string]$BepInExCoreDir = (Join-Path $env:APPDATA 'r2modmanPlus-local\Valheim\profiles\Valheim Mod Building\BepInEx\core')
)

$ErrorActionPreference = 'Stop'
if ($PSBoundParameters.ContainsKey('TypeName') -ne $PSBoundParameters.ContainsKey('MemberName')) {
    throw 'Specify both -TypeName and -MemberName for targeted inspection, or neither for identity only.'
}
if ($PSBoundParameters.ContainsKey('TypeName') -and
    ([string]::IsNullOrWhiteSpace($TypeName) -or
        @($MemberName | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0)) {
    throw 'Type and member names must not be blank.'
}
$path = (Resolve-Path -LiteralPath $AssemblyPath).Path
Add-Type -Path (Join-Path $BepInExCoreDir 'Mono.Cecil.dll')
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($path)
try {
    Write-Output "Assembly: $($assembly.Name.FullName)"
    Write-Output "MVID: $($assembly.MainModule.Mvid)"
    Write-Output "SHA256: $((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)"
    if ($TypeName) {
        $type = $assembly.MainModule.GetType($TypeName)
        if ($null -eq $type) { throw "Type not found in inspected assembly: $TypeName" }
        $members = @($type.Methods) + @($type.Fields) + @($type.Properties)
        foreach ($name in $MemberName) {
            $matches = @($members | Where-Object { $_.Name -ceq $name })
            if ($matches.Count -eq 0) { throw "Member not found on inspected type: $TypeName.$name" }
            foreach ($member in $matches) {
                Write-Output "$($member.FullName) | $($member.Attributes)"
                if ($member -is [Mono.Cecil.MethodDefinition]) {
                    foreach ($parameter in $member.Parameters) {
                        Write-Output "  [$($parameter.Index)] $($parameter.ParameterType.FullName) $($parameter.Name) | $($parameter.Attributes)"
                    }
                }
                elseif ($member -is [Mono.Cecil.PropertyDefinition]) {
                    foreach ($accessor in @($member.GetMethod, $member.SetMethod)) {
                        if ($null -ne $accessor) {
                            Write-Output "  $($accessor.FullName) | $($accessor.Attributes)"
                        }
                    }
                }
            }
        }
    }
}
finally {
    $assembly.Dispose()
}