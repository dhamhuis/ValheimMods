# CraftFromStorage

Craft and upgrade without the chest-to-workbench shuffle. CraftFromStorage by Hoose uses accessible storage within a sphere centered on the player. The radius is configurable from 1 to 100 meters (default **20 meters**).

The [Thunderstore README](Thunderstore/README.md) is the player-facing package page.

## Scope

- Non-food station recipes and equipment upgrades. Cauldron, Food Preparation Table and Mead Ketill recipes belong exclusively to CookFromStorage.
- Inventory first, then the nearest eligible storage, splitting requirements across stacks and containers.
- All loaded Valheim `Container` storage: ordinary, reinforced, black metal and accessible personal chests, carts, ships, and compatible modded containers.
- Personal chest and ward permissions remain in force. Open/busy containers and carts in use are excluded.
- Vanilla station levels, shelter/fire checks, recipe discovery, DLC checks, output capacity, item quality, world-level matching and no-cost modes remain in force.
- Requirement coloring and craftability checks include eligible storage. Requirements are not automatically learned from chest contents.
- Requirement counts are shown as the required amount with `inventory:storage` on the line below, for example `5` with `10:42` below (the `5` moves up a line so the counts fit): 5 required, 10 in your inventory, 42 in nearby storage, with the counts line in yellow. A side with none is left blank (`10:` or `:42`), and with none anywhere you see just the vanilla `5`. The required amount stays white, or flashes red when inventory and storage together are short, as in vanilla.
- Output goes to the player's inventory, never automatically back to storage.

No hand crafting, hammer building, repairs, fueling, automatic machine inputs, stashing, sorting, or new hotkeys.

Configuration is in `BepInEx/config/hoose.valheim.craftfromstorage.cfg`:

- `Storage/Storage radius` (default `20`, range `1`-`100` meters): how far around the player storage is searched for non-food recipes and upgrades. Food-station recipes use CookFromStorage's own setting.

## Implementation

This is an original implementation. [Teflon Ted's CraftFromChests](https://github.com/trak3r/valheim/tree/main/mods/CraftFromChests) and [SmartCraft-Storage](https://github.com/Zellds/SmartCraft-Storage) were consulted for behavior and hook ideas; no third-party source was copied or bundled.

- The shared [NearbyStorage](../ValheimMods.Common/src/Crafting/NearbyStorage.cs) tracks containers and applies spherical distance and access checks. Discovery is cached only for the current frame and player position; final consumption uses fresh validation.
- [CraftingContext](../ValheimMods.Common/src/Crafting/CraftingContext.cs) confines additional counts to station requirement checks, recipe ingredient selection, requirement UI and the actual crafting transaction. Finalizers restore nested scopes even on exceptions.
- [IngredientPlan](../ValheimMods.Common/src/Crafting/IngredientPlan.cs) allocates exact stacks before vanilla creates output, accounting for upgrades, multi-craft, alternative ingredients, quality and world level. Only planned string-based ingredient removals are redirected; unrelated inventory operations remain vanilla.
- [StorageCrafting](../ValheimMods.Common/src/Crafting/StorageCrafting.cs) uses vanilla `RPC_RequestOpen` / `RPC_OpenResponse` to obtain ownership of needed chests without opening their UI. Once ownership arrives, it reloads and reserves closed chests using the owner's busy flag, then revalidates everything before invoking vanilla crafting. Inventory changes use normal removal methods and the container's normal save callback.
- [RecipeStorage](../ValheimMods.Common/src/Crafting/RecipeStorage.cs) registers this plugin for non-food stations. CookFromStorage registers the food category independently. They share one patch set and transaction engine, preventing duplicate counts/consumption; either plugin works alone.

Ownership can introduce a short delay on remote-owned storage. Denial, a five-second timeout, missing resources or lost access cancels the craft without spending ingredients. Closing the crafting UI, changing recipes/stations, dying or pressing Cancel also aborts pending work. A timed-out or cancelled request releases the chest for crafting retries and explicit manual opening without requiring a response or plugin reload. Unsolicited late/duplicate responses remain suppressed until that container is destroyed or the plugin is unloaded.

Vanilla replies have no request ID. A late reply can therefore satisfy a new pending action, but crafting rechecks ownership, inventory and access before consuming anything. Explicit manual opening of a previously requested chest authorizes one reply for at most five seconds; its player identity, life state, interaction distance, permissions, busy state and ownership are rechecked before allowing vanilla to open the UI. Repeated manual requests can be retried after that window expires.

Install on each client that wants this behavior. The distribution includes `ValheimMods.Common.dll`, which must remain beside `CraftFromStorage.dll` in the same plugin folder. The ownership protocol uses vanilla RPCs, so it does not require a server plugin. Other craft-from-chest mods should be disabled to avoid double counting or consumption hooks.

## Build and validation

```powershell
.\CraftFromStorage\scripts\build.ps1
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\CraftFromStorage
dotnet run --project .\CraftFromStorage\tests\CraftFromStorage.Tests.csproj --configuration Release
.\CraftFromStorage\scripts\build.ps1 -Deploy
.\CraftFromStorage\scripts\pack.ps1
```

See the [development guide](../docs/Development.md) for shared tooling and prerequisites and the [testing checklist](TESTING.md) for verification records.

Release notes: [CHANGELOG.md](CHANGELOG.md). Icon source: [assets/icon.svg](assets/icon.svg), using the repository's shared frame.

If you enjoy these mods and would ever like to support my work, [I'm on Ko-fi](https://ko-fi.com/hoose_builds). Thanks for using them!

[Project and support](https://github.com/dhamhuis/ValheimMods)