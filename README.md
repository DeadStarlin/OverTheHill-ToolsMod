# Unlimited Tools — over the hill

MelonLoader mod with configurable unlimited tool use and tool reach.
Built against your MelonLoader 0.7.3 IL2CPP assemblies.

## Install

1. Exit the game.
2. Copy `bin\Release\net6.0\OTHUnlimitedTools.dll` into
   `D:\SteamLibrary\steamapps\common\over the hill\Mods`.
3. Start the game and check the MelonLoader log for `Unlimited Tools` and any patch errors.
4. Equip a consumable tool, use it twice, and confirm its effect occurs both times
   and it remains available. Check that the regular winch still works.

Only copy the DLL. To uninstall, exit the game and remove that DLL.
Already-consumed tools need the game's normal refill/re-equip first.

## Settings

With your installed ModConfigurator, open its mod settings and select **Unlimited
Tools**. It discovers the mod's MelonLoader preferences automatically.

| Option | Default | Choices |
| --- | --- | --- |
| Unlimited tool use | On | On / Off |
| Tool range multiplier | 1x | 1x, 1.25x, 1.5x, 2x, 4x, 10x |

The range dropdown uses enum names `x1`, `x1_25`, `x1_5`, `x2`, `x4`, `x10`.
Both settings work independently: you can use normal consumption with longer reach.
Range changes use each object's original range, so switching presets never stacks
multipliers. For example, 20 m becomes 200 m at 10x and returns to 20 m at 1x.

Without ModConfigurator, launch once to create the settings, exit, and edit the
`[OTHUnlimitedTools]` section in `UserData/MelonPreferences.cfg`:

```toml
[OTHUnlimitedTools]
UnlimitedUse = true
RangeMultiplier = "x10"
```

Changes through ModConfigurator update active range values immediately. Reselect
the tool to refresh any cached selection buttons or reach graphics, and detach
and reconnect an existing winch rope to use a changed reach.

The multiplier covers regular winch reach (including vehicle upgrade modifiers),
portable winch anchor placement, repair-kit targeting, and shared terrain-object
placement used by the jack, planks and camp. Placement height allowance and cursor
projection depth also scale so the cursor can reach farther terrain. Clearance,
collision, slope and other placement checks still apply. Tools without a distance
limit, such as the compass and cloud-clearing bell, retain their existing effects.

## Behavior and limits

When unlimited use is on, the patch skips `CarInventory.RPC_ConsumeItem(uint)`. Native disassembly confirms
both `AskForItemConsumption` overloads and the generated RPC receiver call it.
This prevents clearing the available-tool slot without skipping the surrounding
tool action. Purchases and equipping are not patched. Turning unlimited use off
allows normal consumption on the next use.

In multiplayer this also affects consumption requests processed by the modded
instance, potentially including other players when it has authority. An unmodded
host may still consume tools. Multiplayer and actual gameplay have not been tested.
Game updates can change this implementation; back up your save before testing.
Large winch ranges can affect rope physics and performance; the multiplier changes
reach, not pulling force or rope simulation settings.

## Build and verification

Install a .NET SDK (6 or newer) and the .NET 6 runtime. Launch the game with
MelonLoader once so `MelonLoader/Il2CppAssemblies` contains the generated references.
Both scripts build Release and automatically find an installed .NET 6 runtime.
They can be called from any working directory and do not install the mod.

Windows PowerShell (the game path below is the default):

```powershell
.\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\over the hill'
# Optional: -RuntimeDir 'C:\Program Files\dotnet\shared\Microsoft.NETCore.App\6.0.36'
```

Linux (pass your actual Steam library game directory):

```sh
sh build.sh "$HOME/.local/share/Steam/steamapps/common/over the hill"
# Optional second argument: /usr/share/dotnet/shared/Microsoft.NETCore.App/6.0.36
```

Output: `bin/Release/net6.0/OTHUnlimitedTools.dll`. The Linux script builds against
the MelonLoader assemblies from the game's installation; it does not set up Proton
or MelonLoader. You can also run `dotnet build UnlimitedTools.csproj -c Release`
with `-p:GameDir=...` and `-p:RuntimeDir=...`. The project uses .NET 6 runtime
assemblies directly, without downloading a .NET 6 targeting pack.

On the original Windows setup, `./tests/build.ps1` checks building from another
directory and rejection of missing game/runtime references.

Run `dotnet run --project tests/ToolRanges.Tests.csproj` to check all presets,
live updates, restoration to 1x, invalid values, repeated registration, reopening
tools, and cleanup. These standalone tests require a .NET 8 SDK or newer.

Run `dotnet run --project inspect/inspect.csproj` from this directory to check
the relevant native calls in this exact game build and the compiled prefix.
These checks do not prove the patch installs or works inside the running game.
