---
name: valheim-mod-package
description: "Use when preparing or reviewing a Thunderstore package for any of this repository's Valheim mods (ZIP layout, manifest, README, icon, changelog, versioning or release). Ensures each archive contains only its plugin, any required shared library and player-facing metadata."
---

# Valheim Mod Package

Applies to every mod folder with a `<Mod>/Thunderstore` directory, such as AutoRepair, CraftFromStorage, CookFromStorage and SupplyFromStorage.

## Package Workflow

1. Keep the package `manifest.json`, `README.md` and `icon.png` under `<Mod>/Thunderstore`; keep `CHANGELOG.md` at the mod root.
2. Keep three versions aligned: `version_number` in `manifest.json`, `<Version>` in `<Mod>/src/<Mod>.csproj` and the `PluginVersion` constant in `<Mod>/src/<Mod>Plugin.cs`. The per-mod `pack.ps1` checks `PluginVersion`; the shared packager checks the project version.
3. Package only after changes are satisfactory and tested. Run `.\<Mod>\scripts\pack.ps1` (pass `-R2ModmanProfileDir` when the profile is not the default). It calls the shared [Package-Thunderstore](../../../scripts/Package-Thunderstore.ps1), which builds the mod and creates `<Mod>/dist/<name>-<version>.zip`.
4. Inspect the archive. Its root must contain `manifest.json`, `README.md`, `icon.png` and `CHANGELOG.md`. `plugins/` contains `<Mod>.dll` and, when the project references `ValheimMods.Common`, `ValheimMods.Common.dll`. All mods sharing the library should ship the same current build of it.
5. Never include game assemblies, BepInEx/Harmony DLLs, NuGet packages, debug output or credentials in the archive.
6. Keep the prepared `dist` DLLs and ZIPs in version control. Do not regenerate ZIPs for every edit.
7. Publishing is separate from packaging. Do not upload without an explicit release request; keep account tokens out of files, logs and chat.

## Thunderstore README

The package README is the player-facing page. Do not mention testing status. Keep separate `Installation` (starting with "Install with r2modman or the Thunderstore Mod Manager.") and `Configuration` sections, and include a `Stack with other mods` section linking the repository's companion mods.

## Install for Local Testing

Use `.\<Mod>\scripts\build.ps1 -Deploy` to copy the built plugin, and any shared library, into the chosen r2modman profile. This does not publish or create a Thunderstore package.