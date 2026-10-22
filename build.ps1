[CmdletBinding()]
param(
    [string]$GameDir = 'D:\SteamLibrary\steamapps\common\over the hill',
    [string]$RuntimeDir
)

$ErrorActionPreference = 'Stop'
Get-Command dotnet -ErrorAction Stop | Out-Null

if (-not $RuntimeDir) {
    $runtimes = & dotnet --list-runtimes
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    foreach ($line in $runtimes) {
        if ($line -match '^Microsoft.NETCore.App (6\.0\.\d+) \[(.+)\]$') {
            $RuntimeDir = Join-Path $Matches[2] $Matches[1]
        }
    }
    if (-not $RuntimeDir) {
        throw 'Install the .NET 6 runtime, or pass -RuntimeDir pointing to its assemblies.'
    }
}

foreach ($file in @(
    (Join-Path $GameDir 'MelonLoader/net6/MelonLoader.dll'),
    (Join-Path $GameDir 'MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll'),
    (Join-Path $RuntimeDir 'System.Private.CoreLib.dll'),
    (Join-Path $RuntimeDir 'System.Runtime.dll'),
    (Join-Path $RuntimeDir 'netstandard.dll')
)) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "Required build reference missing: $file"
    }
}

$GameDir = (Resolve-Path -LiteralPath $GameDir).Path
$RuntimeDir = (Resolve-Path -LiteralPath $RuntimeDir).Path
& dotnet build (Join-Path $PSScriptRoot 'UnlimitedTools.csproj') -c Release `
    "-p:GameDir=$GameDir" "-p:RuntimeDir=$RuntimeDir"
exit $LASTEXITCODE
