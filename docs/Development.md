# Development Guide

## Prerequisites

- .NET SDK supporting the project's C# language version.
- A local Steam installation of Valheim.
- A r2modman Valheim profile with BepInExPack_Valheim installed.

Valheim runs Unity Mono under BepInEx 5. The plugins target `netstandard2.1` to match the current game assemblies, not the .NET Core runtime. NuGet dependencies restore automatically; Valheim, Unity, BepInEx, and Harmony compile references come from the local game and profile.

## Build

From the repository root:

```powershell
dotnet build .\ValheimMods.sln --configuration Release
.\AutoRepair\scripts\build.ps1
.\CraftFromStorage\scripts\build.ps1
```

The solution build writes normal project output under each mod's `src/bin/`. Each wrapper also copies only its plugin DLL into that mod's `dist/` directory.

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

Keep each mod's prepared `dist/` DLL and versioned ZIP in version control so users can download and install without rebuilding. Continue excluding intermediate `bin/`, `obj/`, dependencies and debug output.

Do not regenerate distribution ZIPs after every code, documentation or artwork edit. During iteration, build and deploy only as needed for validation. Once the changes are satisfactory and testing is complete, run the package wrapper once, inspect the archive and include the prepared distribution files in the eventual commit. Existing ZIPs remain the last prepared snapshot until that step; do not assume they match subsequent source or icon edits.

```powershell
.\AutoRepair\scripts\pack.ps1
```

This builds Release and writes `AutoRepair/dist/AutoRepair-<version>.zip`. The wrapper checks `PluginVersion` against the manifest; the shared packager checks the project version against the manifest.

The shared packager creates a temporary ZIP alongside the destination, then reopens it and verifies its exact entry count and each entry's SHA-256 against its source file. Only a complete, verified ZIP atomically replaces an existing archive. Read, validation or replacement failures preserve the previous ZIP and clean up the temporary archive. The build step still updates the standalone `dist/` DLL before archiving.

Run filesystem regression tests without building or changing any real distributions:

```powershell
.\scripts\tests\Test-PackageThunderstore.ps1
```

These tests exercise new packages, replacement, locked inputs/destinations, archive validation failure and temporary-file cleanup in an isolated fixture with a stub build step.

The ZIP contains only:

- `manifest.json`
- `README.md` from `AutoRepair/Thunderstore/README.md`
- `icon.png` from `AutoRepair/Thunderstore/icon.png` (256x256)
- `CHANGELOG.md` from the mod root
- `plugins/AutoRepair.dll`

The repository overview, mod developer README, and this guide are not packaged. Inspect the ZIP before release. Packaging does not upload to Thunderstore; publishing is a separate action. Keep account credentials out of the repository and use a new version number for each published release.

## CraftFromStorage

CraftFromStorage follows the same wrappers and package layout:

```powershell
.\CraftFromStorage\scripts\build.ps1 -Deploy
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\CraftFromStorage
dotnet run --project .\CraftFromStorage\tests\CraftFromStorage.Tests.csproj --configuration Release
.\scripts\Render-ModIcon.ps1 -ModDirectory .\CraftFromStorage
```

Run `.\CraftFromStorage\scripts\pack.ps1` only after review and testing are complete, not on each iteration.

Deployment targets `BepInEx/plugins/CraftFromStorage/CraftFromStorage.dll` in the selected profile. The local ZIP is `CraftFromStorage/dist/CraftFromStorage-1.0.0.zip`; it contains the same four root metadata files as AutoRepair and only `plugins/CraftFromStorage.dll`.

Each mod has an independent production-linked test executable requiring .NET SDK 10 and a test-only HarmonyX package:

```powershell
dotnet run --project .\AutoRepair\tests\AutoRepair.Tests.csproj --configuration Release
dotnet run --project .\CraftFromStorage\tests\CraftFromStorage.Tests.csproj --configuration Release
```

Each test project links only its own mod's production code. Mod-specific plugin doubles live in `TestDoubles/`, Harmony integration checks in `Integration/`, and CraftFromStorage's standalone behavioral checks in `Behavior/`. Neither mod's tests depend on the other mod.

Test entry points only run suites and print totals. Each suite registers descriptive test methods with the shared `TestRunner`; each test has a short XML `<summary>` stating its expected behavior and arranges its own initial state. Related assertions may stay together when they evaluate one scenario, but unrelated scenarios must not depend on state left by earlier tests. Reuse fixture builders instead of copying setup code. Harmony patches are installed once per suite and always removed in `finally`.

The runner reports each test's class/method name, identifies that method when an assertion fails, and prints separate test and assertion totals. To add coverage, write a documented scenario method and register it with `runner.Run(...)` in the relevant suite.

Both projects reference [ValheimMods.Testing](../tests/ValheimMods.Testing/), a neutral test-support library organized into `GameStubs/`, `UnityStubs/` and `Logging/`. It contains no mod-specific types or production references. Its API doubles retain game/Unity names and namespaces so production patches can target them unchanged.

The tests apply real Harmony patches to game stubs, not a live Unity instance. The profile's older Harmony remains the production reference, while a .NET-compatible HarmonyX version runs each test executable. The signature validator uses the profile's Mono.Cecil DLL to check Harmony bindings against the installed game. Neither replaces the [in-game test checklist](../CraftFromStorage/README.md#in-game-review-checklist), especially multiplayer ownership and inventory persistence. Keep other craft-from-storage plugins disabled during testing.

The shared icon renderer uses an isolated headless Microsoft Edge profile to rasterize the original SVG with the shared frame, then writes a 256x256 PNG. It does not launch Valheim.

## Shared tools and artwork

Per-mod wrappers call the root `scripts/Build-Mod.ps1` and `scripts/Package-Thunderstore.ps1`, passing the mod directory. New mods can use the same `src/`, `scripts/`, `Thunderstore/`, and `dist/` layout.

Reusable development tools also live in the root `scripts/` folder and take `-ModDirectory`:

```powershell
.\scripts\Render-ModIcon.ps1 -ModDirectory .\AutoRepair
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\AutoRepair
```

`Render-ModIcon.ps1` reads `<Mod>/assets/icon.svg`, inlines its shared frame reference, and writes `<Mod>/Thunderstore/icon.png`. Microsoft Edge is required; override its executable with `-EdgePath`.

`Validate-ModPatches.ps1` reads `AssemblyName` from the mod's single `src/*.csproj` and inspects `dist/<AssemblyName>.dll`; build the mod first. It accepts `-ValheimInstallDir`, `-R2ModmanProfileDir`, and `-BepInExCoreDir`. It checks class-level Harmony patches declared with a target type, method name and optional parameter type array, and named or positional target arguments. Unsupported declaration forms fail explicitly; this is a signature check, not a complete Harmony/runtime verifier.

The shared frame source lives in `assets/icons/valheim-wood-frame.svg`. AutoRepair's composition is `AutoRepair/assets/icon.svg`, and the generated package PNG is `AutoRepair/Thunderstore/icon.png`. Changing an SVG requires regenerating the PNG; packaging uses the PNG and does not render the SVG automatically.