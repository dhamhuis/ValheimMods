# ValheimMods

Valheim mods by Hoose, developed as focused BepInEx plugins.

## Mods

| Mod | Purpose |
| --- | --- |
| [AutoRepair](AutoRepair/README.md) | Repairs eligible inventory equipment when you interact with a crafting station. |
| [CraftFromStorage](CraftFromStorage/README.md) | Crafts and upgrades at non-food stations using accessible storage near the player (20 m by default, configurable). |
| [CookFromStorage](CookFromStorage/README.md) | Prepares food-station recipes and supplies cooking racks/ovens and Fermenters from nearby accessible storage. |
| [SupplyFromStorage](SupplyFromStorage/README.md) | Supplies one accepted fuel or processing input from nearby accessible storage per machine E interaction. |

CookFromStorage and SupplyFromStorage are separate plugins: cooking ingredients and Fermenter bases belong to CookFromStorage; machine fuel and Smelter processing inputs belong to SupplyFromStorage.

## Download and install

Each mod's `dist/` folder is included in version control so its prepared DLL and versioned ZIP can be downloaded without rebuilding:

- [AutoRepair downloads](AutoRepair/dist/)
- [CraftFromStorage downloads](CraftFromStorage/dist/)
- [CookFromStorage downloads](CookFromStorage/dist/)
- [SupplyFromStorage downloads](SupplyFromStorage/dist/)

Install with BepInExPack_Valheim using the mod's installation instructions. Mods that use shared runtime helpers include `ValheimMods.Common.dll` alongside their plugin. Distribution files represent the last prepared build, not necessarily every subsequent source or artwork edit. In-game and multiplayer validation is still required.

## Development

[ValheimMods.sln](ValheimMods.sln) contains the mod projects and the reusable [ValheimMods.Common](ValheimMods.Common/src/) runtime library. Each mod keeps source in `src/`, command wrappers in `scripts/`, and its player-facing package files in `Thunderstore/`. Shared build and packaging tools live in the root `scripts/` folder.

See the [build, deployment, and packaging guide](docs/Development.md) for prerequisites, commands, and testing. Packaging creates a local ZIP; it does not publish a release.

Repository-wide [C# coding standards and AI instructions](AGENTS.md) apply to source and tests.

The [mod API reference](docs/reference/README.md) maps supporting libraries and verified API usage. Agents consult its index before discovery and maintain relevant findings during normal work; see the [API discovery skill](.github/skills/valheim-api-discovery/SKILL.md) for investigating gaps.

## Artwork

The [carved-wood frame](assets/icons/valheim-wood-frame.svg) is shared across mod icons. Each mod owns its composition, such as [AutoRepair's icon source](AutoRepair/assets/icon.svg), and its 256x256 package PNG.