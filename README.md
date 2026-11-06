# AutoRepair

Standalone development workspace for AutoRepair, a BepInEx plugin that invokes Valheim's normal repair checks when a usable crafting station is opened.

## Build and test

Valheim runs Unity Mono under BepInEx 5. The plugin targets `netstandard2.1` to match the installed game's Unity assemblies; it is not a .NET Core application.

Prerequisites: .NET SDK, Steam Valheim, and a r2modman Valheim profile with BepInExPack installed.

```powershell
.\AutoRepair\scripts\build.ps1
.\AutoRepair\scripts\build.ps1 -Deploy
.\AutoRepair\scripts\pack.ps1
```

Override paths when needed with `-ValheimInstallDir`, `-BepInExCoreDir`, and `-R2ModmanProfileDir`. Build output is in `AutoRepair\dist`; package metadata and the per-mod icon are in `AutoRepair\Thunderstore`. The archive contains only the plugin DLL and package files; packaging does not upload to Thunderstore.

Author: dhamhuis

## Mod icons

The reusable dark carved-wood frame source is `assets/icons/valheim-wood-frame.svg`. `AutoRepair/assets/icon.svg` shows the AutoRepair composition; each mod keeps its own 256×256 PNG in its `Thunderstore` folder.
