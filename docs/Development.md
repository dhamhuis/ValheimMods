# Development Guide

## Prerequisites

- .NET SDK supporting the project's C# language version.
- A local Steam installation of Valheim.
- A r2modman Valheim profile with BepInExPack_Valheim installed.

Valheim runs Unity Mono under BepInEx 5. AutoRepair targets `netstandard2.1` to match the current game assemblies, not the .NET Core runtime. NuGet dependencies restore automatically; Valheim, Unity, BepInEx, and Harmony compile references come from the local game and profile.

## Build

From the repository root:

```powershell
dotnet build .\ValheimMods.sln --configuration Release
.\AutoRepair\scripts\build.ps1
```

The solution build writes normal project output under `AutoRepair/src/bin/`. The wrapper also copies the plugin to `AutoRepair/dist/AutoRepair.dll`.

The default paths are the standard Steam installation and the r2modman `Valheim Mod Building` profile. Override them when needed:

```powershell
.\AutoRepair\scripts\build.ps1 -ValheimInstallDir 'D:\SteamLibrary\steamapps\common\Valheim' -R2ModmanProfileDir 'C:\path\to\profile'
```

`-BepInExCoreDir` can specify a separate `BepInEx/core` directory. These path options are also accepted by the package wrapper.

## Deploy and test

```powershell
.\AutoRepair\scripts\build.ps1 -Deploy -R2ModmanProfileDir 'C:\path\to\profile'
```

Deployment copies only the plugin DLL to the profile's `BepInEx/plugins/AutoRepair` folder. Remove or relocate any older AutoRepair DLL first so only one copy is installed. Never copy game assemblies, publicized references, Unity, Harmony, or BepInEx DLLs into the plugin folder.

For manual installation, copy `AutoRepair/dist/AutoRepair.dll` to that same plugin folder on the test machine, which must have BepInExPack installed.

Restart Valheim through the selected r2modman profile after replacing the DLL. Check `BepInEx/LogOutput.log` for the verified repair-patch startup message and any errors. Test broken and partly worn equipment, undamaged gear, wrong-station items, and an unusable station. The scripts do not launch or terminate the game.

## Package

```powershell
.\AutoRepair\scripts\pack.ps1
```

This builds Release and writes `AutoRepair/dist/AutoRepair-<version>.zip`. The wrapper checks `PluginVersion` against the manifest; the shared packager checks the project version against the manifest.

The ZIP contains only:

- `manifest.json`
- `README.md` from `AutoRepair/Thunderstore/README.md`
- `icon.png` from `AutoRepair/Thunderstore/icon.png` (256x256)
- `CHANGELOG.md` from the mod root
- `plugins/AutoRepair.dll`

The repository overview, mod developer README, and this guide are not packaged. Inspect the ZIP before release. Packaging does not upload to Thunderstore; publishing is a separate action. Keep account credentials out of the repository and use a new version number for each published release.

## Shared tools and artwork

Per-mod wrappers call the root `scripts/Build-Mod.ps1` and `scripts/Package-Thunderstore.ps1`, passing the mod directory. New mods can use the same `src/`, `scripts/`, `Thunderstore/`, and `dist/` layout.

The shared frame source lives in `assets/icons/valheim-wood-frame.svg`. AutoRepair's composition is `AutoRepair/assets/icon.svg`, and the generated package PNG is `AutoRepair/Thunderstore/icon.png`. Changing an SVG requires regenerating the PNG; packaging uses the PNG and does not render the SVG automatically.