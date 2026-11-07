---
name: valheim-mod-build
description: "Use when building, debugging, validating, or deploying this repository's Valheim BepInEx plugins, including AutoRepair and CraftFromStorage. Covers Unity Mono, game references, profile deployment, and build validation."
---

# Valheim Mod Build

Read the [API reference index](../../../docs/reference/README.md) and relevant topics before discovering game APIs. Use the [discovery skill](../valheim-api-discovery/SKILL.md) for missing or stale knowledge, and maintain verified findings during the task.

## Runtime Target

Valheim runs Unity Mono under BepInEx 5. The installed Valheim managed directory includes `netstandard.dll` version 2.1, so this plugin targets `netstandard2.1`. Do not retarget it to `netcoreapp` or ship .NET runtime DLLs with the plugin.

## Build

1. Confirm Valheim is installed and the selected r2modman profile has BepInExPack installed.
2. Run `.\<Mod>\scripts\build.ps1`. The per-mod wrapper calls the shared `scripts/Build-Mod.ps1`, which builds the SDK-style project and copies only the plugin DLL into `<Mod>/dist`.
3. If paths differ from the defaults, pass `-ValheimInstallDir`, `-BepInExCoreDir`, or `-R2ModmanProfileDir`.
4. Use `-Deploy` only when intentionally copying the DLL to the selected profile's `BepInEx\plugins\<Mod>` folder.
5. Do not copy Valheim, Unity, BepInEx, Harmony, or publicized reference assemblies into the plugin folder.

## Validation

- Compile the affected project with `dotnet build .\<Mod>\src\<Mod>.csproj --configuration Release`; see the [development guide](../../../docs/Development.md) for test and signature-validation commands.
- Inspect the BepInEx log after a user launches the profile; do not launch or terminate Valheim from a build script.
- Valheim API changes must be checked against the installed game's assemblies and tested in game. A successful compile alone does not prove a Harmony patch resolves at runtime.