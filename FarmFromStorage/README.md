# FarmFromStorage

Plant your fields with the Cultivator using materials straight from nearby storage. Leave the seeds in your chests, not your pockets.

Every plantable piece available in your Cultivator menu uses its normal resource requirements: crop seeds, crops planted for seed production, saplings, and other plantable entries are not restricted to a hardcoded item list. Your inventory supplies costs first, then the nearest eligible storage supplies the remainder. Materials are removed only after successful placement.

## Storage rules

- The default radius is **20 meters**, configurable from 1 to 100 meters, in a sphere centered on your character.
- Storage must be accessible, closed, and currently owned by your client's network peer. Access permission alone does not establish ownership.
- If an accessible chest is not used, try opening and closing it yourself. This can transfer ownership, but ownership may change later; the mod does not request it automatically.
- Personal-chest and ward permissions remain in effect. Busy, invalid, remote-owned, and out-of-range containers are skipped.
- Normal material discovery, biome/soil/spacing restrictions, stamina, durability, and no-cost modes remain vanilla. Storage does not unlock unknown plants or make unsuitable growing conditions valid.
- The Cultivator tooltip shows each planting material as the required amount with `inventory:storage` on the line below, for example `5` with `10:42` below (the `5` moves up a line so the counts fit where it normally sits, and long material names stay on one line): 5 required, 10 in your inventory, 42 in nearby storage, with the counts line in yellow. A side with none is left blank (`10:` or `:42`), and with none anywhere you see just the vanilla `5`. The required amount stays white, or flashes red when inventory and storage together are short, as in vanilla.
- Cultivating soil and restoring grass keep their normal behavior. Harvesting, crop growth, tool crafting/repair, and Hammer building are outside this mod's scope.

## Installation

Install with r2modman or the Thunderstore Mod Manager. Requires **BepInExPack_Valheim**.

For manual installation, copy `plugins/FarmFromStorage.dll` and `plugins/ValheimMods.Common.dll` from the archive into `BepInEx/plugins/FarmFromStorage/`. Keep both DLLs together and do not install a second copy of the plugin.

FarmFromStorage runs on each player's client; a server installation is not required.

### Companion compatibility

FarmFromStorage and the updated BuildFromStorage register disjoint Cultivator and Hammer clients in one shared placement engine. Each keeps its own radius. Install the current BuildFromStorage DLL if using both, and replace **every installed copy** of `ValheimMods.Common.dll` with the same current build, including copies beside CraftFromStorage, CookFromStorage and SupplyFromStorage.

FarmFromStorage loads after BuildFromStorage when it is present. Older BuildFromStorage builds with their own placement patches are explicitly rejected with an update error, rather than risking duplicate counts or consumption.

## Configuration

`BepInEx/config/hoose.valheim.farmfromstorage.cfg`:

- `Storage/Storage radius` (default `20`, range `1`-`100` meters): how far around the player storage is searched.

## Development and validation

```powershell
.\FarmFromStorage\scripts\build.ps1
dotnet run --project .\FarmFromStorage\tests\FarmFromStorage.Tests.csproj --configuration Release
dotnet run --project .\BuildFromStorage\tests\BuildFromStorage.Tests.csproj --configuration Release
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\FarmFromStorage
.\scripts\Render-ModIcon.ps1 -ModDirectory .\FarmFromStorage
```

See the [development guide](../docs/Development.md), [testing checklist](TESTING.md) and [placement API reference](../docs/reference/Building.md).

If you enjoy these mods and would ever like to support my work, [I'm on Ko-fi](https://ko-fi.com/hoose_builds). Thanks for using them!

[Project and support](https://github.com/dhamhuis/ValheimMods)