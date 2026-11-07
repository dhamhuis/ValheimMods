# ValheimMods

Valheim mods by dhamhuis, developed as focused BepInEx plugins.

## Mods

| Mod | Purpose |
| --- | --- |
| [AutoRepair](AutoRepair/README.md) | Repairs eligible inventory equipment when you interact with a crafting station. |
| [CraftFromStorage](CraftFromStorage/README.md) | Crafts and upgrades at stations using accessible storage within 20 metres of the player. |

## Download and install

Each mod's `dist/` folder is included in version control so its prepared DLL and versioned ZIP can be downloaded without rebuilding:

- [AutoRepair downloads](AutoRepair/dist/)
- [CraftFromStorage downloads](CraftFromStorage/dist/)

Install with BepInExPack_Valheim using the mod's installation instructions. Distribution files represent the last prepared build, not necessarily every subsequent source or artwork edit.

## Development

[ValheimMods.sln](ValheimMods.sln) contains the mod projects. Each mod keeps source in `src/`, command wrappers in `scripts/`, and its player-facing package files in `Thunderstore/`. Shared build and packaging tools live in the root `scripts/` folder.

See the [build, deployment, and packaging guide](docs/Development.md) for prerequisites, commands, and testing. Packaging creates a local ZIP; it does not publish a release.

Repository-wide [C# coding standards and AI instructions](AGENTS.md) apply to source and tests.

## Artwork

The [carved-wood frame](assets/icons/valheim-wood-frame.svg) is shared across mod icons. Each mod owns its composition, such as [AutoRepair's icon source](AutoRepair/assets/icon.svg), and its 256x256 package PNG.