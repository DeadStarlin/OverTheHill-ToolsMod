#!/bin/sh
set -eu

if [ "$#" -lt 1 ] || [ "$#" -gt 2 ]; then
    echo "Usage: sh build.sh GAME_DIR [DOTNET_6_RUNTIME_DIR]" >&2
    exit 2
fi

command -v dotnet >/dev/null 2>&1 || {
    echo "Install a .NET SDK and the .NET 6 runtime first." >&2
    exit 1
}

game_dir=$1
runtime_dir=${2:-}
if [ -z "$runtime_dir" ]; then
    runtimes=$(dotnet --list-runtimes)
    runtime_dir=$(printf '%s\n' "$runtimes" | awk '
        $1 == "Microsoft.NETCore.App" && $2 ~ /^6\.0\.[0-9]+$/ {
            version = $2
            sub(/^[^[]*\[/, "")
            sub(/\]$/, "")
            path = $0 "/" version
        }
        END { print path }
    ')
    if [ -z "$runtime_dir" ]; then
        echo "Install the .NET 6 runtime, or supply its assembly directory as the second argument." >&2
        exit 1
    fi
fi

for file in \
    "$game_dir/MelonLoader/net6/MelonLoader.dll" \
    "$game_dir/MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll" \
    "$runtime_dir/System.Private.CoreLib.dll" \
    "$runtime_dir/System.Runtime.dll" \
    "$runtime_dir/netstandard.dll"
do
    if [ ! -f "$file" ]; then
        echo "Required build reference missing: $file" >&2
        exit 1
    fi
done

game_dir=$(CDPATH= cd -- "$game_dir" && pwd)
runtime_dir=$(CDPATH= cd -- "$runtime_dir" && pwd)
script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
exec dotnet build "$script_dir/UnlimitedTools.csproj" -c Release \
    "-p:GameDir=$game_dir" "-p:RuntimeDir=$runtime_dir"
