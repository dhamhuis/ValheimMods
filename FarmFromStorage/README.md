# FarmFromStorage

Plant with the Cultivator using materials from nearby storage.

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

For this development build, copy `dist/FarmFromStorage.dll` and `dist/ValheimMods.Common.dll` together into `BepInEx/plugins/FarmFromStorage/` in a test profile. Do not install a second copy of the same plugin.

FarmFromStorage runs on each player's client; a server installation is not required.

### Companion compatibility

FarmFromStorage and the updated BuildFromStorage register disjoint Cultivator and Hammer clients in one shared placement engine. Each keeps its own radius. Install the current BuildFromStorage DLL if using both, and replace **every installed copy** of `ValheimMods.Common.dll` with the same current build, including copies beside CraftFromStorage, CookFromStorage and SupplyFromStorage.

FarmFromStorage loads after BuildFromStorage when it is present. Older BuildFromStorage builds with their own placement patches are explicitly rejected with an update error, rather than risking duplicate counts or consumption. The old prepared BuildFromStorage ZIP is not the updated development build.

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

The production-linked tests use game stubs; the installed assembly was inspected for tool selection and placement-cost ordering. Neither establishes live planting, menu behavior, multiplayer safety, or persistence. All in-game tests remain pending in [TESTING.md](TESTING.md). The shared-engine migration also needs a fresh Hammer regression pass.

Prepared test DLLs are under `dist/`. Packaging and publication are deferred until in-game validation is satisfactory; then run `.\FarmFromStorage\scripts\pack.ps1`. See the [development guide](../docs/Development.md) and [placement API reference](../docs/reference/Building.md).

[Project and support](https://github.com/dhamhuis/ValheimMods)