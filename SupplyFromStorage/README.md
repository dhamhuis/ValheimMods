# SupplyFromStorage

SupplyFromStorage by Hoose adds one accepted fuel or processing input from nearby accessible storage when you use a machine's default E interaction. It is a separate BepInEx plugin from CraftFromStorage.

## Supported machines

- **Fire and light fuel:** campfires, bonfires, hearths, fireplaces, braziers and torches using Valheim's `Fireplace` component.
- **Cooking-station fuel:** cooking stations, hanging and iron cooking stations, and the stone oven when its `CookingStation` fuel slot is enabled. Cooking ingredients are never transferred.
- **Smelter fuel:** smelters and blast furnaces using Valheim's `Smelter` fuel slot.
- **Processing inputs:** charcoal kiln, smelters, blast furnaces, windmill, spinning wheel and eitr refinery when their prefab uses the vanilla `Smelter` conversion list. Each prefab's current accepted conversion entries determine which items can be supplied.

The mod does not load cooking ingredients, fermenter bases, outputs or unrelated crafting ingredients. It does not read or consume items from the player's inventory. Fueling and processing-input automation have independent configuration switches, both enabled by default.

## Access and multiplayer behavior

Storage and machines must be within an inclusive **20 metre sphere centered on the local player**. Storage access, ward checks, busy state and cart use follow CraftFromStorage's container rules. Transfers recheck range, access, ownership, station capacity and item quantity immediately before removing a storage item. A station's vanilla acceptance/conversion data is checked before insertion. Each valid E action transfers at most one item; holding E repeats according to the game's interaction repeat interval. No items are supplied during station updates or when the world loads. Failed, full, unavailable, out-of-range and no-eligible-item attempts are briefly rate-limited; successful transfers are not throttled.

For Smelter inputs, the game normally reports “You don't have any processable items” before a postfix can supply from storage. SupplyFromStorage checks storage before vanilla takes that branch, suppresses the message when it successfully adds an item, and preserves vanilla inventory handling and the full-station message.

For safety, SupplyFromStorage transfers only when the local client owns both the source container and the machine. It does not request ownership, open containers, or modify remote-owned inventories. As a result, storage owned by another player is skipped until normal gameplay gives the local client ownership. Multiplayer behavior, including ownership changes and simultaneous access, still requires in-game testing.

CraftFromStorage and SupplyFromStorage have separate plugins and do not share transfer hooks. CraftFromStorage patches recipe checks and string-based inventory consumption; SupplyFromStorage patches only the machine's E-action callbacks, and supplies from storage only if vanilla did not already handle that action from player inventory. It removes a specific item from its source container and invokes the machine's vanilla fuel/input RPC only when the local client owns it. These distinct hooks avoid double insertion through CraftFromStorage's crafting hooks. Disable other mods that automate the same machine inputs.

## Installation

Requires BepInExPack_Valheim. Install on the client; a server-only install does not enable the behavior.

For manual installation, copy `plugins/SupplyFromStorage.dll` into `BepInEx/plugins/SupplyFromStorage/`. Keep only one installed copy.

Configuration is in `BepInEx/config/dhamhuis.valheim.supplyfromstorage.cfg`:

- `Automation/Automatic fuel` (default `true`): enable one fuel item per valid machine E action.
- `Automation/Automatic processing inputs` (default `true`): enable one processing input per valid machine E action.

This is a local preview, not a published or in-game-certified release. Stub tests and a successful build do not prove live-game networking or persistence.

## Development

```powershell
.\SupplyFromStorage\scripts\build.ps1
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\SupplyFromStorage
dotnet run --project .\SupplyFromStorage\tests\SupplyFromStorage.Tests.csproj --configuration Release
```

See the [repository development guide](../docs/Development.md) for build prerequisites, package layout and validation limits.

[Source and support](https://github.com/dhamhuis/ValheimMods)
