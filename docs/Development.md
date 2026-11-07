# Development Guide

## Prerequisites

For library selection, existing API knowledge, and discovery procedures, start with the [mod API reference](reference/README.md). It distinguishes installed assembly signatures from repository behavior and live-game verification.

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
.\SupplyFromStorage\scripts\build.ps1
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
.\SupplyFromStorage\scripts\build.ps1 -Deploy -R2ModmanProfileDir 'C:\path\to\profile'
```

Deployment copies only the plugin DLL to the profile's `BepInEx/plugins/<ModName>` folder. Remove or relocate any older copy first so only one copy of each plugin is installed. Never copy game assemblies, publicized references, Unity, Harmony, or BepInEx DLLs into the plugin folder.

For manual installation, copy the mod's `dist/<ModName>.dll` to the same plugin folder on the test machine, which must have BepInExPack installed.

Restart Valheim through the selected r2modman profile after replacing the DLL. Check `BepInEx/LogOutput.log` for each plugin's startup message and any errors. The scripts do not launch or terminate the game.

## Package

Keep each mod's prepared `dist/` DLL and versioned ZIP in version control so users can download and install without rebuilding. Continue excluding intermediate `bin/`, `obj/`, dependencies and debug output.

Do not regenerate distribution ZIPs after every code, documentation or artwork edit. During iteration, build and deploy only as needed for validation. Once the changes are satisfactory and testing is complete, run the package wrapper once, inspect the archive and include the prepared distribution files in the eventual commit. Existing ZIPs remain the last prepared snapshot until that step; do not assume they match subsequent source or icon edits.

```powershell
.\AutoRepair\scripts\pack.ps1
.\CraftFromStorage\scripts\pack.ps1
.\SupplyFromStorage\scripts\pack.ps1
```

Each package wrapper checks the plugin version against its manifest; the shared packager checks the project version against the manifest. The shared packager creates a temporary ZIP alongside the destination, then reopens it and verifies its exact entry count and each entry's SHA-256 against its source file. Only a complete, verified ZIP atomically replaces an existing archive. Read, validation or replacement failures preserve the previous ZIP and clean up the temporary archive. The build step still updates the standalone `dist/` DLL before archiving.

Run filesystem regression tests without building or changing any real distributions:

```powershell
.\scripts\tests\Test-PackageThunderstore.ps1
```

These tests exercise new packages, replacement, locked inputs/destinations, archive validation failure and temporary-file cleanup in an isolated fixture with a stub build step.

The ZIP contains only:

- `manifest.json`
- `README.md` from the mod's `Thunderstore/` folder
- `icon.png` from the mod's `Thunderstore/` folder (256x256)
- `CHANGELOG.md` from the mod root
- `plugins/<ModName>.dll`

The repository overview, mod developer README, and this guide are not packaged. Inspect the ZIP before release. Packaging does not upload to Thunderstore; publishing is a separate action. Keep account credentials out of the repository and use a new version number for each published release.

## CraftFromStorage

CraftFromStorage follows the shared wrappers and package layout:

```powershell
.\CraftFromStorage\scripts\build.ps1 -Deploy
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\CraftFromStorage
dotnet run --project .\CraftFromStorage\tests\CraftFromStorage.Tests.csproj --configuration Release
.\scripts\Render-ModIcon.ps1 -ModDirectory .\CraftFromStorage
```

Run `.\CraftFromStorage\scripts\pack.ps1` only after review and testing are complete, not on each iteration. Deployment targets `BepInEx/plugins/CraftFromStorage/CraftFromStorage.dll` in the selected profile. The local ZIP is `CraftFromStorage/dist/CraftFromStorage-1.0.0.zip`; it contains the same four root metadata files as the other mods and only `plugins/CraftFromStorage.dll`.

Each mod has an independent production-linked test executable requiring .NET SDK 10 and a test-only HarmonyX package:

```powershell
dotnet run --project .\AutoRepair\tests\AutoRepair.Tests.csproj --configuration Release
dotnet run --project .\CraftFromStorage\tests\CraftFromStorage.Tests.csproj --configuration Release
dotnet run --project .\SupplyFromStorage\tests\SupplyFromStorage.Tests.csproj --configuration Release
```

Each test project links only its own mod's production code. Mod-specific plugin doubles live in `TestDoubles/`, Harmony integration checks in `Integration/`, and CraftFromStorage's standalone behavioral checks in `Behavior/`. The mods' tests do not depend on one another.

Test entry points only run suites and print totals. Each suite registers descriptive test methods with the shared `TestRunner`; each test has a short XML `<summary>` stating its expected behavior and arranges its own initial state. Related assertions may stay together when they evaluate one scenario, but unrelated scenarios must not depend on state left by earlier tests. Reuse fixture builders instead of copying setup code. Harmony patches are installed once per suite and always removed in `finally`.

The runner reports each test's class/method name, identifies that method when an assertion fails, and prints separate test and assertion totals. To add coverage, write a documented scenario method and register it with `runner.Run(...)` in the relevant suite.

All three projects reference [ValheimMods.Testing](../tests/ValheimMods.Testing/), a neutral test-support library organized into `GameStubs/`, `UnityStubs/` and `Logging/`. It contains no mod-specific types or production references. Its API doubles retain game/Unity names and namespaces so production patches can target them unchanged.

The tests apply HarmonyX patches to game stubs, not a live Unity instance. The profile's older Harmony remains the production reference, while a .NET-compatible HarmonyX version runs each test executable. The signature validator uses the profile's Mono.Cecil DLL to check Harmony bindings against the installed game. Neither replaces the [in-game test checklist](../CraftFromStorage/README.md#in-game-review-checklist), especially multiplayer ownership and inventory persistence. Keep other craft-from-storage plugins disabled during CraftFromStorage tests.

The shared icon renderer uses an isolated headless Microsoft Edge profile to rasterize the original SVG with the shared frame, then writes a 256x256 PNG. It does not launch Valheim.

## SupplyFromStorage preview

```powershell
.\SupplyFromStorage\scripts\build.ps1
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\SupplyFromStorage
dotnet run --project .\SupplyFromStorage\tests\SupplyFromStorage.Tests.csproj --configuration Release
.\SupplyFromStorage\scripts\build.ps1 -Deploy
.\scripts\Render-ModIcon.ps1 -ModDirectory .\SupplyFromStorage
```

Fuel automation and processing-input automation are separate BepInEx settings, both enabled by default. The mod hooks fireplace interaction and cooking-station/smelter fuel/input switch callbacks, transferring at most one item per valid E action. Vanilla repeat timing governs held E; station update callbacks do not supply items. On `Smelter.OnAddOre`, a prefix preserves accepted player-inventory inputs and supplies from storage only if vanilla inventory has no accepted item; it skips the misleading no-processable-items message only after confirming a storage transfer, while preserving the vanilla full-station message. It does not insert cooking ingredients or fermenter bases. To avoid competing ownership RPCs and duplicate consumption, only locally owned machines and containers are used; remote-owned storage is skipped. Vanilla interactions that already consume a player-inventory item, including fire toggles, are preserved. CraftFromStorage's crafting/count/removal patches are not modified, and SupplyFromStorage does not patch those APIs.

The [fuel and processing API reference](reference/Processing.md) records the verified interaction callback signatures, inspected `Smelter.OnAddOre` feedback path and unresolved prefab/input mappings. The focused .NET 10 test executable links SupplyFromStorage's production access, transfer and Harmony patch code to the shared test helpers, with test-only station doubles. Its conversion-list cases model several item names on a `Smelter` double; they do not prove that the corresponding live prefabs use that component or accept those inputs. The suite also exercises one-item-per-action behavior, hold-repeat guards, capacity, no load/update automation, storage-aware smelter feedback, the inclusive 20m player-centered range, inaccessible/busy and stale storage, retry throttling, independent toggles and the CraftFromStorage inventory-hook boundary. Stubs do not emulate live Unity, Valheim networking or persistence. The signature validator confirms Harmony bindings against the installed game assembly; it does not prove runtime patch resolution. Verify each supported station prefab, held-E behavior, concurrent multiplayer access and saved inventory state in game before release.

## Shared tools and artwork

Reference maintenance tools do not build, deploy, or modify game assemblies:

```powershell
.\scripts\Get-ValheimApi.ps1 -AssemblyPath 'C:\path\to\assembly_valheim.dll'
.\scripts\Get-ValheimApi.ps1 -AssemblyPath 'C:\path\to\assembly_valheim.dll' -TypeName Inventory -MemberName CountItems,RemoveItem
.\scripts\tests\Test-AgentReference.ps1
.\scripts\tests\Test-GetValheimApi.ps1
```

The inspector uses the selected profile's Mono.Cecil to read metadata without executing game code. With no type it reports only assembly identity, MVID, and SHA-256; targeted inspection requires both a type and member names. Property output includes individual getter/setter visibility. Override `-BepInExCoreDir` for another profile; the inspector regression tests accept the same option and use a temporary synthetic DLL, not game assemblies. The reference validator checks structure, real verification dates (or `Never` for unknown topics), file links, and instruction/skill wiring, not API behavior or whether an agent followed instructions. See the [discovery skill](../.github/skills/valheim-api-discovery/SKILL.md) for a fresh-session acceptance check.

Per-mod wrappers call the root `scripts/Build-Mod.ps1` and `scripts/Package-Thunderstore.ps1`, passing the mod directory. New mods can use the same `src/`, `scripts/`, `Thunderstore/`, and `dist/` layout.

Reusable development tools also live in the root `scripts/` folder and take `-ModDirectory`:

```powershell
.\scripts\Render-ModIcon.ps1 -ModDirectory .\AutoRepair
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\AutoRepair
```

`Render-ModIcon.ps1` reads `<Mod>/assets/icon.svg`, inlines its shared panel and frame references, and writes `<Mod>/Thunderstore/icon.png`. Microsoft Edge is required; override its executable with `-EdgePath`.

`Validate-ModPatches.ps1` reads `AssemblyName` from the mod's single `src/*.csproj` and inspects `dist/<AssemblyName>.dll`; build the mod first. It accepts `-ValheimInstallDir`, `-R2ModmanProfileDir`, and `-BepInExCoreDir`. It checks class-level Harmony patches declared with a target type, method name and optional parameter type array, and named or positional target arguments. Unsupported declaration forms fail explicitly; this is a signature check, not a complete Harmony/runtime verifier.

All mod icons use the shared background in `assets/icons/valheim-panel.svg` and the overlay in `assets/icons/valheim-wood-frame.svg`. In each `<Mod>/assets/icon.svg`, reference the panel as the first visual layer and the frame as the last, with mod-specific graphics between them. The renderer requires exactly one reference to each and rejects duplicate SVG IDs across the combined artwork. Each mod owns its generated PNG in `<Mod>/Thunderstore/icon.png`. Changing an SVG requires regenerating affected PNGs; packaging uses the PNG and does not render the SVG automatically.