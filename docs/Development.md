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
.\WorldClock\scripts\build.ps1
.\CraftFromStorage\scripts\build.ps1
.\CookFromStorage\scripts\build.ps1
.\SupplyFromStorage\scripts\build.ps1
```

The solution build writes normal project output under each mod's `src/bin/` and builds `ValheimMods.Common`, the shared runtime helper library. Wrappers copy the plugin DLL and, when referenced, `ValheimMods.Common.dll` into that mod's `dist/` directory.

The default paths are the standard Steam installation and the r2modman `Valheim Mod Building` profile. Override them when needed:

```powershell
.\AutoRepair\scripts\build.ps1 -ValheimInstallDir 'D:\SteamLibrary\steamapps\common\Valheim' -R2ModmanProfileDir 'C:\path\to\profile'
```

`-BepInExCoreDir` can specify a separate `BepInEx/core` directory. These path options are also accepted by the package wrapper.

## Deploy and test

```powershell
.\AutoRepair\scripts\build.ps1 -Deploy -R2ModmanProfileDir 'C:\path\to\profile'
.\CookFromStorage\scripts\build.ps1 -Deploy -R2ModmanProfileDir 'C:\path\to\profile'
.\SupplyFromStorage\scripts\build.ps1 -Deploy -R2ModmanProfileDir 'C:\path\to\profile'
.\WorldClock\scripts\build.ps1 -Deploy -R2ModmanProfileDir 'C:\path\to\profile'
```

Deployment copies the plugin DLL and any required shared runtime library to the profile's `BepInEx/plugins/<ModName>` folder. Keep `ValheimMods.Common.dll` beside plugins that reference it. Remove or relocate any older copy first so only one copy of each plugin is installed. Never copy game assemblies, publicized references, Unity, Harmony, or BepInEx DLLs into the plugin folder.

For manual installation, copy the mod's plugin DLL and any runtime dependency DLLs from `dist/` to the same plugin folder on the test machine, which must have BepInExPack installed.

Restart Valheim through the selected r2modman profile after replacing the DLL. Check `BepInEx/LogOutput.log` for each plugin's startup message and any errors. The scripts do not launch or terminate the game.

## Package

Keep each mod's prepared `dist/` DLL and versioned ZIP in version control so users can download and install without rebuilding. Continue excluding intermediate `bin/`, `obj/`, dependencies and debug output.

Do not regenerate distribution ZIPs after every code, documentation or artwork edit. During iteration, build and deploy only as needed for validation. Once the changes are satisfactory and testing is complete, run the package wrapper once, inspect the archive and include the prepared distribution files in the eventual commit. Existing ZIPs remain the last prepared snapshot until that step; do not assume they match subsequent source or icon edits.

```powershell
.\AutoRepair\scripts\pack.ps1
.\CraftFromStorage\scripts\pack.ps1
.\CookFromStorage\scripts\pack.ps1
.\SupplyFromStorage\scripts\pack.ps1
.\WorldClock\scripts\pack.ps1
```

Each package wrapper checks the plugin version against its manifest; the shared packager checks the project version against the manifest. The shared packager creates a temporary ZIP alongside the destination, then reopens it and verifies its exact entry count and each entry's SHA-256 against its source file. Only a complete, verified ZIP atomically replaces an existing archive. Read, validation or replacement failures preserve the previous ZIP and clean up the temporary archive. The build step still updates the standalone `dist/` DLL before archiving.

Run filesystem regression tests without building or changing any real distributions:

```powershell
.\scripts\tests\Test-PackageThunderstore.ps1
```

These tests exercise new packages, replacement, locked inputs/destinations, archive validation failure and temporary-file cleanup in an isolated fixture with a stub build step.

The ZIP contains only player-facing metadata, the plugin and any required shared runtime libraries:

- `manifest.json`
- `README.md` from the mod's `Thunderstore/` folder
- `icon.png` from the mod's `Thunderstore/` folder (256x256)
- `CHANGELOG.md` from the mod root
- `plugins/<ModName>.dll`
- `plugins/ValheimMods.Common.dll` when referenced

The repository overview, mod developer README, and this guide are not packaged. Inspect the ZIP before release. Packaging does not upload to Thunderstore; publishing is a separate action. Keep account credentials out of the repository and use a new version number for each published release.

### GitHub Releases

Use one companion GitHub release per mod, with tag `<Mod>-v<version>` and title `<Mod> <version>`. Thunderstore remains the primary installation channel for r2modman users. Attach the existing prepared ZIP from `<Mod>/dist/`, not files from `<Mod>/Thunderstore/`; do not rebuild just to create a GitHub release.

Before publishing, validate the archive and identify the committed source snapshot associated with its build. Compare the local ZIP's Git blob ID (`git hash-object`) with its blob at that commit (`git rev-parse '<commit>:AutoRepair/dist/AutoRepair-1.0.0.zip'`). Check the corresponding plugin and shared-library source and packaged metadata as well; matching ZIP bytes alone does not prove build provenance. Resolve uncertainty rather than tagging an arbitrary current branch tip.

Check existing tags, releases and drafts to avoid duplicates. Do not move tags or replace published assets without approval. Prefer authenticated GitHub CLI/API tools, with a signed-in shared browser as a fallback. The following PowerShell example assumes GitHub CLI is installed and authenticated, a new release is explicitly requested, and `$releaseCommit` is set to the verified full commit hash:

```powershell
gh release list --repo dhamhuis/ValheimMods
gh release create AutoRepair-v1.0.0 .\AutoRepair\dist\AutoRepair-1.0.0.zip --repo dhamhuis/ValheimMods --target $releaseCommit --title 'AutoRepair 1.0.0' --notes $releaseNotes
```

Set `$releaseNotes` to a player-facing feature/change summary, a Thunderstore package link, and manager/manual installation instructions including any required shared DLL. Report only supported testing and compatibility claims. Use `--prerelease` when a preview is requested. If an upload is interrupted, inspect and continue the matching draft instead of creating another release.

After publication, confirm the release is not a draft, the remote tag points to the intended commit, and the attached ZIP name and SHA-256 match the local source. Compare `Get-FileHash -Algorithm SHA256` with GitHub's displayed asset digest, or download the asset and hash it. Report any unresolved mismatch explicitly. Keep credentials and temporary browser state out of repository files. GitHub Releases do not publish to Thunderstore, replace tracked `dist/` files, or require GitHub Packages. The [packaging skill](../.github/skills/valheim-mod-package/SKILL.md) maintains the agent release checklist.

### Published package missing from a mod manager

A working Thunderstore package page does not guarantee that the mod-manager feed already includes the release. Compare the [individual package API](https://thunderstore.io/api/experimental/package/Hoose/SupplyFromStorage/) (substitute the package name) with the [Valheim package feed](https://thunderstore.io/c/valheim/api/v1/package/). Check the individual package's `latest.version_number`, `latest.is_active` and `community_listings` for Valheim, then check whether the feed contains that package and version. The legacy `valheim.thunderstore.io/api/v1/package/` URL redirects to this feed.

If the individual package exists and is active but the feed omits it, inspect the feed response's `Last-Modified`, `x-cache-status` and `Age` headers. A stale feed predating the upload explains both missing search results and an app-install link reporting "Mod does not exist"; restarting the manager cannot refresh Thunderstore's server-side feed. Wait for the feed to update, then refresh or restart the manager. If the discrepancy persists, contact Thunderstore support with the package URL and feed evidence. Do not rebuild, bump the version or re-upload solely to work around a stale feed. An `unreviewed` listing alone does not establish rejection or explain the discrepancy.

For an immediate workaround, use the package page's manual download and follow its manual installation instructions in the selected profile. Install BepInExPack through the manager first, keep required shared DLLs beside the plugin, and remove the manual copy before installing the same mod through the manager to avoid duplicate plugins.

## CraftFromStorage

CraftFromStorage follows the shared wrappers and package layout:

```powershell
.\CraftFromStorage\scripts\build.ps1 -Deploy
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\CraftFromStorage
dotnet run --project .\CraftFromStorage\tests\CraftFromStorage.Tests.csproj --configuration Release
.\scripts\Render-ModIcon.ps1 -ModDirectory .\CraftFromStorage
```

Run `.\CraftFromStorage\scripts\pack.ps1` only after review and testing are complete, not on each iteration. Deployment targets `BepInEx/plugins/CraftFromStorage/`; the prepared package includes `ValheimMods.Common.dll` beside the plugin.

## WorldClock

```powershell
.\WorldClock\scripts\build.ps1
dotnet run --project .\WorldClock\tests\WorldClock.Tests.csproj --configuration Release
.\WorldClock\scripts\build.ps1 -Deploy
.\scripts\Render-ModIcon.ps1 -ModDirectory .\WorldClock
```

WorldClock is a client-side HUD display with no Harmony patches or shared runtime dependency. The formatter tests do not validate live HUD layering or time progression; check its startup and screen behavior in the selected Valheim profile. Run `.\WorldClock\scripts\pack.ps1` only after review and testing are complete.

The four game-integration test executables require .NET SDK 10 and a test-only HarmonyX package. WorldClock has a separate .NET 10 formatter test executable with no Harmony dependency.

```powershell
dotnet run --project .\AutoRepair\tests\AutoRepair.Tests.csproj --configuration Release
dotnet run --project .\CraftFromStorage\tests\CraftFromStorage.Tests.csproj --configuration Release
dotnet run --project .\CookFromStorage\tests\CookFromStorage.Tests.csproj --configuration Release
dotnet run --project .\SupplyFromStorage\tests\SupplyFromStorage.Tests.csproj --configuration Release
dotnet run --project .\WorldClock\tests\WorldClock.Tests.csproj --configuration Release
```

Each storage-mod test project links its production code and the shared container helper source, without referencing a built game or common-library assembly. CraftFromStorage and CookFromStorage also link the shared recipe engine; it installs one verified patch set and registers independent food/non-food clients. CookFromStorage's recipe integration suite tests both categories alone, both together, and unloading one client. The signature validator includes the prepared common DLL for plugins using this engine. AutoRepair's test project only links its repair patch. Mod-specific plugin doubles live in `TestDoubles/`, Harmony integration checks in `Integration/`, and CraftFromStorage's standalone behavioral checks in `Behavior/`. The mods' tests do not depend on one another.

Test entry points only run suites and print totals. Each suite registers descriptive test methods with the shared `TestRunner`; each test has a short XML `<summary>` stating its expected behavior and arranges its own initial state. Related assertions may stay together when they evaluate one scenario, but unrelated scenarios must not depend on state left by earlier tests. Reuse fixture builders instead of copying setup code. Harmony patches are installed once per suite and always removed in `finally`.

The runner reports each test's class/method name, identifies that method when an assertion fails, and prints separate test and assertion totals. To add coverage, write a documented scenario method and register it with `runner.Run(...)` in the relevant suite.

All five mod test projects reference [ValheimMods.Testing](../tests/ValheimMods.Testing/), a neutral test-support library organized into `GameStubs/`, `UnityStubs/` and `Logging/`. It contains no mod-specific types or production references. Its API doubles retain game/Unity names and namespaces so production patches can target them unchanged.

The game-integration tests apply HarmonyX patches to game stubs, not a live Unity instance; WorldClock's suite tests only its pure time formatter. The profile's older Harmony remains the production reference, while a .NET-compatible HarmonyX version runs the integration test executables. The signature validator uses the profile's Mono.Cecil DLL to check Harmony bindings against the installed game. Neither replaces the [in-game test checklist](../CraftFromStorage/README.md#in-game-review-checklist), especially multiplayer ownership and inventory persistence. Keep other craft-from-storage plugins disabled during CraftFromStorage tests.

The shared icon renderer uses an isolated headless Microsoft Edge profile to rasterize the original SVG with the shared frame, then writes a 256x256 PNG. It does not launch Valheim.

## CookFromStorage

```powershell
.\CookFromStorage\scripts\build.ps1
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\CookFromStorage
dotnet run --project .\CookFromStorage\tests\CookFromStorage.Tests.csproj --configuration Release
.\CookFromStorage\scripts\build.ps1 -Deploy
.\scripts\Render-ModIcon.ps1 -ModDirectory .\CookFromStorage
```

CookFromStorage intercepts `CookingStation.OnInteract`, reached from direct interactions and food switches, only when vanilla has no finished item to collect and no accepted ingredient in the player's inventory. It selects a current `CookingStation.m_conversion` input from locally owned, accessible storage, checks fire/capacity/range again before mutation, and inserts through the station RPC. A separate `Fermenter.Interact` prefix supplies an accepted base only to an empty, sheltered, ward-accessible Fermenter with no accepted inventory input; held interactions, running batches, collection and explicit item use remain vanilla. Both paths reuse a local storage-input transaction helper, confirm station-specific committed state, and skip remote-owned stations/storage. The shared [Processing reference](reference/Processing.md) documents the inspected callbacks and limits. Stub tests and signature validation do not replace in-game checks of prefab conversions, multiplayer ownership or persistence. Test Fermenter input priority, roof/exposure messages, batch collection without reloading, and save/reload state before release.

Run `.\CookFromStorage\scripts\pack.ps1` after testing and review. The package includes `ValheimMods.Common.dll`, which must be installed beside CookFromStorage.

## SupplyFromStorage preview

```powershell
.\SupplyFromStorage\scripts\build.ps1
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\SupplyFromStorage
dotnet run --project .\SupplyFromStorage\tests\SupplyFromStorage.Tests.csproj --configuration Release
.\SupplyFromStorage\scripts\build.ps1 -Deploy
.\scripts\Render-ModIcon.ps1 -ModDirectory .\SupplyFromStorage
```

Fuel automation and processing-input automation are separate BepInEx settings, both enabled by default. The mod hooks fireplace interaction and cooking-station/smelter fuel/input switch callbacks, transferring at most one item per valid E action. Vanilla repeat timing governs held E; station update callbacks do not supply items. On `Smelter.OnAddOre`, a prefix preserves accepted player-inventory inputs and supplies from storage only if vanilla inventory has no accepted item; it skips the misleading no-processable-items message only after confirming a storage transfer, while preserving the vanilla full-station message. It does not insert cooking ingredients or fermenter bases. To avoid competing ownership RPCs and duplicate consumption, only locally owned machines and containers are used; remote-owned storage is skipped. Vanilla interactions that already consume a player-inventory item, including fire toggles, are preserved. CraftFromStorage's crafting/count/removal patches are not modified, and SupplyFromStorage does not patch those APIs.

The [fuel and processing API reference](reference/Processing.md) records the verified interaction callback signatures, inspected `Smelter.OnAddOre` feedback path and unresolved prefab/input mappings. The focused .NET 10 test executable links SupplyFromStorage's production access, transfer and Harmony patch code to the shared test helpers, with test-only station doubles. Its conversion-list cases model several item names on a `Smelter` double; they do not prove that the corresponding live prefabs use that component or accept those inputs. The suite also exercises one-item-per-action behavior, hold-repeat guards, capacity, no load/update automation, storage-aware smelter feedback, the inclusive player-centered range (default 20m, configurable storage radius clamped to 1–100m, fixed 20m station reach), inaccessible/busy and stale storage, retry throttling, independent toggles and the CraftFromStorage inventory-hook boundary. Stubs do not emulate live Unity, Valheim networking or persistence. The signature validator confirms Harmony bindings against the installed game assembly; it does not prove runtime patch resolution. Verify each supported station prefab, held-E behavior, concurrent multiplayer access and saved inventory state in game before release.

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