---
name: valheim-mod-package
description: "Use when preparing or reviewing Thunderstore packages or publishing GitHub Releases for this repository's Valheim mods. Covers ZIP layout, metadata, versioning, per-mod tags, existing ZIP uploads and release verification."
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

## GitHub Release Workflow

Thunderstore remains the primary player distribution channel; GitHub Releases provide companion downloads. Follow the [development guide](../../../docs/Development.md#github-releases) for command examples.

1. Publish only on explicit request. Create one release per requested mod, with tag `<Mod>-v<version>` and title `<Mod> <version>`, for example `AutoRepair-v1.0.0` and `AutoRepair 1.0.0`.
2. Use the existing `<Mod>/dist/<name>-<version>.zip`, not the metadata directory `<Mod>/Thunderstore`. Do not rebuild or regenerate archives solely for GitHub. Inspect the ZIP using the package rules above and read its manifest and changelog as the packaged snapshot.
3. Choose the exact committed source snapshot associated with that prepared ZIP. Verify the local ZIP matches the blob at that commit and that the mod source, shared library source and packaged metadata correspond to the prepared build. Do not assume current `main` matches an older archive; a ZIP blob match alone does not prove which source produced its DLLs. Resolve uncertain provenance before publishing.
4. Check existing tags, published releases and drafts before creation. Reuse a matching draft when continuing an interrupted upload. Never move an existing tag, duplicate a release or replace a published asset without explicit approval.
5. Prefer authenticated GitHub CLI/API tools; if unavailable, use a signed-in shared browser. Check authentication each time. Do not extract browser credentials or display tokens; ask the user to authenticate when necessary.
6. Attach only that mod's validated ZIP. Include a player-facing feature/change summary, a Thunderstore link, and manager/manual installation guidance naming any required shared DLL. Do not claim testing or compatibility beyond recorded evidence. Select stable versus prerelease status according to the requested release, not merely the age of an archived changelog.
7. Verify the published release is not a draft, its tag resolves to the intended commit, and its asset name and SHA-256 match the local ZIP. Use GitHub's displayed asset digest or download and hash the uploaded asset. Report release links and any unresolved verification failure; do not claim success from an upload button alone.

Keep browser IDs, authentication state, current commit hashes and release-specific results out of this skill. GitHub release publication does not upload to Thunderstore or change tracked distributions.

## Thunderstore README

The package README is the player-facing page. Do not mention testing status. Keep separate `Installation` (starting with "Install with r2modman or the Thunderstore Mod Manager.") and `Configuration` sections, and include a `Stack with other mods` section linking the repository's companion mods.

## Install for Local Testing

Use `.\<Mod>\scripts\build.ps1 -Deploy` to copy the built plugin, and any shared library, into the chosen r2modman profile. This does not publish or create a Thunderstore package.