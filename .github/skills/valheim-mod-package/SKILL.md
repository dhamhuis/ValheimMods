---
name: valheim-mod-package
description: "Use when preparing or reviewing an AutoRepair Thunderstore package, ZIP layout, manifest, deployment, or release. Ensures the archive contains only the plugin and required player-facing metadata."
---

# Valheim Mod Package

## Package Workflow

1. Keep the package manifest, README, and per-mod icon under `<Mod>/Thunderstore`; keep the changelog at the mod root.
2. Keep the package version in `manifest.json` aligned with `Main.VERSION`; `AutoRepair/pack.ps1` checks this before packaging.
3. Run `<Mod>/pack.ps1`. The per-mod wrapper validates its plugin version, then calls the shared `scripts/Package-Thunderstore.ps1` to build the plugin and create `dist/<name>-<version>.zip`.
4. Inspect the archive. Its root should contain `manifest.json`, `README.md`, `icon.png`, and `CHANGELOG.md`; `plugins/` should contain only `AutoRepair.dll`.
5. Never include game assemblies, BepInEx/Harmony DLLs, NuGet packages, debug output, or credentials in the archive.
6. Publishing is separate from packaging. Do not upload without an explicit release request; keep account tokens out of files, logs, and chat.

## Install for Local Testing

Use `AutoRepair/build.ps1 -Deploy` to copy the built plugin into the chosen r2modman profile. This does not publish or create a Thunderstore package.