# ValheimMods

Valheim mods by dhamhuis, developed as focused BepInEx plugins.

## Mods

| Mod | Purpose |
| --- | --- |
| [AutoRepair](AutoRepair/README.md) | Repairs eligible inventory equipment when you interact with a crafting station. |

## Development

[ValheimMods.sln](ValheimMods.sln) contains the mod projects. Each mod keeps source in `src/`, command wrappers in `scripts/`, and its player-facing package files in `Thunderstore/`. Shared build and packaging tools live in the root `scripts/` folder.

See the [build, deployment, and packaging guide](docs/Development.md) for prerequisites, commands, and testing. Packaging creates a local ZIP; it does not publish a release.

## Artwork

The [carved-wood frame](assets/icons/valheim-wood-frame.svg) is shared across mod icons. Each mod owns its composition, such as [AutoRepair's icon source](AutoRepair/assets/icon.svg), and its 256x256 package PNG.
