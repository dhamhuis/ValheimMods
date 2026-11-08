# Fuel and processing

Status: Verified - installed assembly metadata, targeted IL inspection, repository implementation, stub tests and user-reported local cooking success.
Last verified: 2026-11-07.
Evidence: [Installed API baseline](Libraries.md); targeted `Get-ValheimApi.ps1` inspection of `Fireplace`, `CookingStation`, `Fermenter`, `Smelter`, `Windmill`, `ZDOVars`, and `ZDO`; targeted Mono.Cecil IL inspection of Smelter, cooking-station and Fermenter item paths; [SupplyFromStorage patches and transfer logic](../../SupplyFromStorage/src/); [CookFromStorage patch and transfer logic](../../CookFromStorage/src/); focused behavior and Harmony integration tests.
Limitations: IL inspection and stub tests do not establish live prefab assignments or game execution. On 2026-11-07, after the Fermenter deployment, the user reported "Tested. Cooking fully works." This is user-reported local cooking success, not a detailed per-scenario record, multiplayer test or persistence verification. Processing-family test cases use a generic `Smelter` double; they do not prove windmill, spinning-wheel, or refinery support.

## Use when

Adding or reviewing E-triggered fuel/processing-input supply, choosing an interaction hook, or checking which installed component APIs SupplyFromStorage currently uses. Read [Storage and ownership](Storage.md) for the repository's container access and mutation policies.

Food-recipe stations are also part of CookFromStorage but use the shared recipe engine, not these slot-cooking hooks. See [food recipe identities and category split](Crafting.md#food-station-troubleshooting) for verified Cauldron, Food Preparation Table and Mead Ketill keys. CraftFromStorage excludes those stations.

## API and behavior

### Installed signatures

The following signatures are from `assembly_valheim.dll` SHA-256 `9F959C0396EC4240D47FCBE5D0D062C428CC627244506DB5EA567F9D77B2B769`. Publicized compile references expose some members that are private in the original assembly.

| Member | Original visibility / return | Parameters or field type |
| --- | --- | --- |
| `Fireplace.Interact` | Public instance / `bool` | `Humanoid user, bool hold, bool alt` |
| `Fireplace.m_holdRepeatInterval`, `m_lastUseTime`, `m_infiniteFuel` | Public, private, public fields | `float`, `float`, `bool` |
| `Fireplace.RPC_AddFuel` | Private instance / `void` | `long sender` |
| `Fireplace.SetFuel` | Public instance / `void` | `float fuel` |
| `Fireplace.m_canRefill`, `m_fuelItem`, `m_maxFuel` | Public fields | `bool`, `ItemDrop`, `float` |
| `ZDOVars.s_fuel` | Public static readonly field | `int` |
| `ZDO.GetFloat` | Public instance / `float` | `int hash, float defaultValue` |
| `CookingStation.OnAddFuelSwitch` | Private instance / `bool` | `Switch sw, Humanoid user, ItemDrop/ItemData item` |
| `CookingStation.OnAddFoodSwitch` | Private instance / `bool` | `Switch caller, Humanoid user, ItemDrop/ItemData item` |
| `CookingStation.Interact` | Public instance / `bool` | `Humanoid user, bool hold, bool alt` |
| `CookingStation.OnInteract` | Private instance / `bool` | `Humanoid user` |
| `CookingStation.RPC_AddItem` | Private instance / `void` | `long sender, string itemName, bool cheated` |
| `CookingStation.FindCookableItem` | Private instance / `ItemDrop/ItemData` | `Inventory inventory` |
| `CookingStation.HaveDoneItem`, `GetFreeSlot`, `IsFireLit` | Private instance / `bool`, `int`, `bool` | None |
| `CookingStation.RPC_AddFuel` | Private instance / `void` | `long sender` |
| `CookingStation.GetFuel` | Private instance / `float` | None |
| `CookingStation.IsItemAllowed` | Private instance / `bool` | `ItemDrop/ItemData item` or `string itemName` |
| `CookingStation.m_slots`, `m_conversion`, `m_requireFire` | Public fields | `Transform[]`, `List<CookingStation/ItemConversion>`, `bool` |
| `CookingStation.m_useFuel`, `m_fuelItem`, `m_maxFuel` | Public fields | `bool`, `ItemDrop`, `int` |
| `Smelter.OnAddFuel`, `OnAddOre` | Private instance / `bool` | `Switch sw, Humanoid user, ItemDrop/ItemData item` |
| `Smelter.FindCookableItem` | Private instance / `ItemDrop/ItemData` | `Inventory inventory` |
| `Smelter.RPC_AddFuel` | Private instance / `void` | `long sender` |
| `Smelter.RPC_AddOre` | Private instance / `void` | `long sender, string name, bool cheated` |
| `Smelter.GetFuel`, `GetQueueSize` | Private instance / `float`, `int` | None |
| `Smelter.IsItemAllowed` | Private instance / `bool` | `ItemDrop/ItemData item` or `string itemName` |
| `Smelter.m_conversion`, `m_maxOre`, `m_fuelItem`, `m_maxFuel` | Public fields | `List<Smelter/ItemConversion>`, `int`, `ItemDrop`, `int` |
| `Windmill.m_smelter` | Private field | `Smelter` |

`CompilerControlled` members above are private in the original metadata; access from the mod's production project depends on the configured assembly publicizer. A successful compile does not prove a Harmony patch resolves at runtime.

An exact lookup for `Fireplace.GetFuel` returned no member on this assembly fingerprint. The mod reads `ZDOVars.s_fuel` with `ZDO.GetFloat(int, float)` instead; this records the access path used by the mod, not the game's fuel update semantics.

### Repository hooks and policy

[SupplyFromStorage](../../SupplyFromStorage/src/) installs a guarded postfix on `Fireplace.Interact` and prefixes on `CookingStation.OnAddFuelSwitch`, `Smelter.OnAddFuel` and `Smelter.OnAddOre`. It does not patch station updates. Fuel/processing settings remain separate, and each eligible callback can transfer at most one storage item. Successful transfers have no cooldown so vanilla held-interaction repeats can continue; failed/full/unavailable checks are briefly rate-limited. The fireplace postfix skips alternate actions, vanilla-handled actions (including toggles and inventory fuel), and held callbacks rejected by the fireplace's repeat guard. Fuel-switch prefixes supply only when the callback's item argument is null and vanilla's `Inventory.HaveItem(string name, bool matchWorldLevel)` with `true` finds no player-inventory fuel, preserving manual item use and inventory-first behavior.

On 2026-11-07, targeted IL inspection of both fuel-switch bodies on the baseline DLL confirmed they validate an explicit item's shared name, reject fuel above `m_maxFuel - 1` with `$msg_itsfull`, then check player inventory and show `$msg_donthaveany` before returning on missing fuel. The former postfix therefore added storage fuel only after a misleading failure message had already appeared. The prefixes now skip that branch only after a confirmed storage transfer and show `$msg_added` with the station's fuel name. Full, wrong-item, no-storage-fuel and disabled-automation paths retain vanilla handling. Regression tests record every emitted message, not just the last message, to detect a transient missing-fuel message before success.

The inspected `Smelter.OnAddOre` body calls `FindCookableItem` on the user's inventory when its item argument is null. If no item is found, it calls `Character.Message` with `$msg_noprocessableitems` and returns before a postfix can run. SupplyFromStorage therefore uses a prefix for this callback: it leaves accepted inventory items to vanilla, transfers from nearby storage only when no accepted inventory item exists, and skips the vanilla no-item branch only when the storage transfer is confirmed. If accessible storage has an accepted item but the station is full, it displays vanilla's `$msg_itsfull` message and does not consume storage. When neither source has a valid item, vanilla runs and displays its no-processable-items message. The prefix uses current conversion entries and rechecks storage through the normal transfer path; runtime message timing and callback behavior still need in-game validation.

The mod's [storage discovery](../../SupplyFromStorage/src/NearbyStorage.cs) uses a player-centered sphere with a configurable radius (1–100 m, default 20 m) and checks current access, busy state, ward policy, network validity and local container ownership. The transfer path rechecks access, ownership, item identity/quantity, station range and capacity before removing one item. It invokes the machine's vanilla RPC and checks the resulting station state; a rejected transfer attempts to restore the storage item and logs the outcome. A local `m_inUse` reservation and ownership check are not a network-wide atomic lock.

### Cooking-station ingredients

The following body behavior was inspected in `assembly_valheim.dll` SHA-256 `9F959C0396EC4240D47FCBE5D0D062C428CC627244506DB5EA567F9D77B2B769`:

- `OnAddFoodSwitch` sends a non-null selected item to `OnUseItem`; with a null item, it calls `OnInteract`.
- Targeted IL inspection on 2026-11-07 confirmed `Interact(Humanoid user, bool hold, bool alt)` rejects held calls and stations with a non-null `m_addFoodSwitch`; otherwise it calls `OnInteract(user)` directly, bypassing `OnAddFoodSwitch`. `UseItem` also bypasses the food switch and calls `OnUseItem` on stations without a separate switch. Patching only `OnAddFoodSwitch` therefore misses direct station interactions.
- `OnInteract` checks for a finished item before looking for a cookable item in the user's inventory. If none is found, it displays an incompatible/no-cookable message and returns `false`; a postfix would run too late to suppress that empty-inventory path.
- `FindCookableItem` iterates the station's current `m_conversion` list and asks the inventory for the corresponding shared item name. `IsItemAllowed(string)` compares the supplied prefab name with current conversion `m_from` prefab names.
- `OnUseItem` refuses insertion when required fire is unlit or no slot is free. `CookItem` removes the selected player item and sends `InvokeRPC("RPC_AddItem", prefabName, cheated)`; the station RPC rechecks item acceptance and free slot before writing a slot.
- Additional targeted body inspection on 2026-11-07 confirmed `OnUseItem` reports `$msg_needfire` before capacity checks and `$msg_nocookroom` for a full station. `IsFireLit` queries burning effect areas at the configured fire-check points (or station position when no points exist); a nearby-looking flame is not proof this check passes. CookFromStorage now finds an accepted storage ingredient before reporting these readiness failures, preserving vanilla's no-cookable message when neither inventory nor eligible storage has one. It logs rejected requests with station eligibility or accessible-container/accepted-item details. Regression tests reproduce the formerly misleading no-cookable message for unlit racks and verify no consumption until fire is detected.

[CookFromStorage](../../CookFromStorage/src/CookingStationPatch.cs) therefore prefixes the shared `OnInteract(Humanoid user)` handler, reached from both direct interactions and null-item food-switch callbacks. It does not patch both entry points, preventing double transfers; vanilla's direct-interaction guards and explicit-item `OnUseItem` paths remain untouched. It leaves cooked-item pickup and accepted player-inventory items to vanilla. Only on the empty-input path does it search accessible storage for a current conversion item; it requires local station/container ownership, station and storage range/access, a free slot, and any required fire. It removes one item, calls `RPC_AddItem`, confirms a free slot was consumed, and attempts to restore the source stack on rejection. [Regression tests](../../CookFromStorage/tests/Integration/PatchIntegrationTests.cs) model both entry paths, held/body rejection, inventory priority, and pickup; they do not verify prefab assignments or live multiplayer behavior. The user's 2026-11-07 retest reported stone-oven transfers working but wooden/iron racks still reporting no cookable items after the shared-handler correction. The live log confirmed CookFromStorage loaded, but did not establish why individual rack requests were rejected. Missing-fire feedback and request diagnostics were added afterward; rack success remains unverified. These are mod-side safeguards, not proof of network-wide atomicity or persisted inventory.

### Fermenter bases

On 2026-11-07, targeted metadata and IL inspection used the same `assembly_valheim.dll` fingerprint above. CookFromStorage now owns this brewing input path; SupplyFromStorage does not insert bases.

| Member | Original visibility / return | Parameters or field type |
| --- | --- | --- |
| `Fermenter.Interact` | Public instance / `bool` | `Humanoid user, bool hold, bool alt` |
| `Fermenter.UseItem` | Public instance / `bool` | `Humanoid user, ItemDrop/ItemData item` |
| `Fermenter.AddItem` | Private instance / `bool` | `Humanoid user, ItemDrop/ItemData item` |
| `Fermenter.FindCookableItem` | Private instance / `ItemDrop/ItemData` | `Inventory inventory` |
| `Fermenter.IsItemAllowed` | Private instance / `bool` | `ItemDrop/ItemData item` or `int nameHash` |
| `Fermenter.GetStatus`, `GetContent` | Private instance / `Fermenter/Status`, `int` | None |
| `Fermenter.UpdateCover` | Private instance / `void` | `float dt, bool forceUpdate` (optional second parameter) |
| `Fermenter.RPC_AddItem` | Private instance / `void` | `long sender, int nameHash, bool cheated` |
| `Fermenter.m_nview`, `m_hasRoof`, `m_exposed` | Private fields | `ZNetView`, `bool`, `bool` |
| `Fermenter.m_conversion` | Public field | `List<Fermenter/ItemConversion>` with `m_from`, `m_to`, `m_producedItems` |

The inspected bodies establish:

- `Interact` rejects held calls, forces `UpdateCover(0f, true)`, and checks station ward access. Empty stations require roof and sufficient cover before looking for an inventory input. Missing roof/exposure report `$piece_fermenter_needroof` / `$piece_fermenter_exposed`; no accepted inventory input reports `$msg_noprocessableitems`. The inspected method does not use `alt`.
- `GetStatus` returns Empty (0) without content, Ready (3) after the configured fermentation duration, otherwise Fermenting (1). The enum also defines Exposed (2), but this inspected getter does not return it. Ready interactions invoke `RPC_Tap`; they do not attempt input insertion.
- `FindCookableItem` uses shared names from the current conversion list. Actual acceptance compares the selected item's drop-prefab name hash with conversion input prefab hashes; a matching shared name alone is insufficient.
- `UseItem` checks ward access and calls `AddItem` directly. `AddItem` requires empty status and acceptance, removes one player-inventory item, and sends `RPC_AddItem` with the prefab hash and the item's `m_cheated` flag. The storage path must not pass a container item to `AddItem`, which removes from the user's inventory.
- `RPC_AddItem` requires local ownership, empty status and accepted hash. It creates effects, records `ZDOVars.s_content`, sets the fermentation start time and records the cheated flag. `GetContent` reads that integer content field. Effects can fail before the content write, so issuing the RPC alone is not proof of insertion.
- `UpdateCover` refreshes roof and exposure from the configured roof-check point; insufficient cover or missing roof resets the owner's fermentation timer. The mod leaves running and finished batches to vanilla rather than adding its own fermentation update.

Prefab identities use public static `StringExtensionMethods.GetStableHashCode(string str)` from `assembly_utils.dll`, SHA-256 `843946B3DF81FB0A3060A6EBEDD34BCACF71CF62B78B74F0DA4653872BAF25F9`. This helper is not defined in `assembly_valheim.dll`; CookFromStorage explicitly references the utility DLL without copying it to the distribution.

The [Fermenter prefix](../../CookFromStorage/src/FermenterPatch.cs) and [transfer path](../../CookFromStorage/src/FermenterTransfer.cs) preserve inventory-first input and explicit item use. Only an empty, active, locally owned station within the player-centered inclusive 20m station reach can consume a stored accepted base from storage within the configured radius (1–100 m, default 20 m). Shelter is refreshed before storage selection; ward, storage access/ownership, item presence and station readiness are rechecked before removal. The [local transaction helper](../../CookFromStorage/src/StorageInputTransfer.cs), also used by racks/ovens, reserves the source, removes one base, calls the RPC and confirms the exact selected hash through `GetContent`. Rejection attempts restoration and logs failures. A confirmed insertion is not restored if its callback throws afterward; an unreadable confirmation leaves the base consumed and logs the uncertainty rather than risking duplication.

[Fermenter integration tests](../../CookFromStorage/tests/Integration/FermenterIntegrationTests.cs) cover input priority, exact acceptance, modded conversions, one-item loading, running/finished batches, shelter feedback, wards, range, ownership rechecks, held/disabled behavior, reservation release and callback/confirmation failures. Their hash double provides consistent test identities, not the game's hash algorithm. Neither these tests nor inspected game bodies establish live Harmony startup, current prefab conversions, multiplayer atomicity or save/reload persistence. These still require in-game testing.

### Deep North compatibility findings

On 2026-11-07, the [official 1.0.21 patch notes](https://www.valheimgame.com/news/patch-1-0-21/) identified Deep North content and the Frost Foundry, and moved Baked Poteitr, Kale Chips and Oven Pancake recipes to the Food Preparation Table. Local inspection used the installed game baseline and bundle `c4210710` fingerprint recorded above, not assumptions about an unreleased biome.

- `piece_FrostFoundry` (`$piece_frostfoundry`) shares the exact serialized MonoScript pointer with `piece_oven` in the same asset file and has the CookingStation field layout, one slot, food/input and fuel switches, `m_useFuel=1`, `m_requireFire=0`, and fuel `$item_frozenfuel`. Its 30 conversion entries turn unfinished northern gear into finished gear; it is not a food oven despite using CookingStation internally.
- `piece_FrostKiln` (`$piece_frostkiln`) shares the exact serialized MonoScript pointer with `smelter` and has the Smelter field layout. Its fuel item is `$item_ice`, capacity 25, with a fuel switch but no ore switch and `m_maxOre=0`. Its conversion has a null input and output `$item_frozenfuel`; do not describe this as an ordinary ore-loading machine.
- SupplyFromStorage covers the Foundry's frozen-fuel switch and the Kiln's ice-as-fuel switch. The Foundry's cast input uses `CookingStation.OnInteract`, exposing an unintended overlap with storage cooking. After the user confirmed on 2026-11-07, "Tested. The cast was applied.", CookFromStorage explicitly excludes station key `$piece_frostfoundry` from both interaction interception and the transfer eligibility guard. Manual inventory input, selected items and output collection remain vanilla. SupplyFromStorage does not load casts.
- After deploying the exclusion, the user confirmed on 2026-11-07, "The Cooking mod no longer loads the molds." This verifies the excluded storage-input behavior in local play; it does not independently verify every manual-input or collection scenario.
- Northern food recipes at the existing Cauldron and Food Preparation Table use the documented food category. This inspection did not establish a new Deep North-specific food-station prefab.

Script-pointer equality and field layouts establish shared component identity relative to the known oven/smelter prefabs; external script class names were not resolved directly. On 2026-11-07 the user tested the SupplyFromStorage-only profile and confirmed both northern machines add their needed fuel from storage, but report missing ice/liquid frost immediately before insertion. After the fuel-prefix fix was tested and deployed, the user confirmed, "The frost messages are now fixed." This is local user-reported confirmation for both fuel paths, not multiplayer or save/reload verification. Foundry cast inputs are now excluded from CookFromStorage; the gameplay policy is manual cast selection, not automatic processing-input supply.

For a minimal Foundry input test, local prefab inspection resolved its first conversion's input `ItemDrop.m_GameObject` to `SwordGoldUncooked`. The installed `Terminal.InitTerminal` spawn-command registration documents amount/level/radius arguments; `spawn SwordGoldUncooked 1` requests one accepted unfinished input. This is prefab/command-registration evidence, not an executed spawn or successful storage input transfer. SupplyFromStorage currently supplies Foundry fuel only; storing this cast will not make its gear-input path run under SupplyFromStorage alone.

### Standalone station-boundary audit

On 2026-11-07, a read-only scan of all MonoBehaviour records in bundle `c4210710` (fingerprint above) selected serialized layouts for the five component families used by these mods. It found 45 matching components: 20 Fireplace, 11 CraftingStation, four CookingStation, nine Smelter and one Fermenter. This includes nested objects missed by enumerating only exposed prefab roots. It is not a search of every bundle or of third-party mod data.

The four CookingStation records were the wooden rack, iron rack, oven and Foundry. After the explicit Foundry exclusion, no additional non-food CookingStation record appeared in this scan. The single Fermenter layout belongs to the expected brewing station.

Two additional SupplyFromStorage scope surfaces were verified:

- `piece_bathtub` has the exact same serialized MonoScript pointer as `smelter`, a wood fuel item, an exposed fuel switch, zero ore capacity and no conversions. The fuel prefix can supply stored wood to this non-production interaction. It cannot receive ordinary ore inputs through the mod. On 2026-11-07, with SupplyFromStorage deployed alone, the user reported, "Hot tub works from storage." Multiplayer and persistence were not tested.
- A nested object named `kiln engine`, under `BatteringRam`, shares the smelter script pointer, has no fuel item, an exposed ore/input switch, capacity 25 and four conversion entries. SupplyFromStorage's processing prefix can supply its accepted stored inputs. Calling the plugin "station supply" does not exclude vehicle-mounted processing components. A follow-up 2026-11-07 UnityPy read of the same bundle found `m_name` `$tool_batteringram`, `m_secPerProduct` 20, and conversions from `Wood`, `FineWood`, `RoundLog` and `Blackwood` (player-facing Wood, Fine Wood, Core Wood and Ashwood) with null `m_to` pointers, so wood is consumed as engine fuel rather than converted into an item. SupplyTransfer's input selection reads only `m_from` and `IsItemAllowed`, so null outputs do not block it. On 2026-11-07 the user identified it as the Ashlands battering ram's engine, fed from the back by players during normal play, and reported after a live test with all four plugins: "It does load them from chests, just tested it." It remains in scope as routine supply. Multiplayer was not tested.

Targeted IL inspection of `Smelter.Awake` on the unchanged game baseline confirmed it attaches `OnAddOre` and `OnAddFuel` to configured input/fuel switches. Together with prefab wiring and the generic SupplyTransfer guards, this establishes reachable mod hooks, not a live transfer result. These two consumers were not in the player-facing supported-machine list or dedicated integration scenarios at audit time. Following the user's live tests, both are now listed in the SupplyFromStorage READMEs and kept as routine supply; no exclusion was added. They still have no dedicated integration scenarios.

Other bounded risks: compatible modded CookingStation conversions are still accepted unless they reuse the excluded Foundry key; a modded non-food processor using that component can repeat the original overlap. Unknown recipe-station keys belong to CraftFromStorage, not CookFromStorage, so a uniquely named modded food station is not automatically recognized by cooking alone. The vanilla special upgrade-station path and AutoRepair's no-cost/lower-world-level exceptions remain separate verification surfaces rather than evidence of another ordinary repair destination.

### Bounded type-name search

For the same `assembly_valheim.dll` fingerprint, exact top-level metadata lookups found `Fireplace`, `CookingStation`, `Smelter`, and `Windmill`. They did not find top-level types named `CharcoalKiln`, `StoneOven`, `Oven`, `BlastFurnace`, `SpinningWheel`, `EitrRefinery`, or `Torch`. This is only an exact-name search of top-level types in this assembly; it does not establish that the prefabs or functionality are absent, or that similarly named components do not exist in another assembly.

The `Windmill` metadata exposes a private `Smelter m_smelter` field, but metadata alone does not establish whether a prefab's nested smelter receives the patched update, has a barley conversion, or processes items as assumed. The actual component and input mapping for every supported prefab must be checked in game data and then validated in game. Do not treat item-name scenarios on the generic `Smelter` test double as that evidence.

## Pitfalls

- Do not infer supported prefabs from a component's class name, a private `Windmill.m_smelter` field, or a test-double conversion list.
- Keep fuel and processing-input config paths separate; do not extend a fuel-only hook to cooking ingredients or fermenter bases.
- Recheck actual capacity and accepted inputs before each mutation. A matching shared item name alone does not prove machine acceptance.
- Do not call a transfer successful solely because an RPC was issued; verify a changed machine state and handle failed restoration explicitly.
- SupplyFromStorage does not patch CraftFromStorage's inventory removal overload. The integration test checks this boundary in stubs only; it does not prove coexistence under live patch ordering or multiplayer access.

## Latest local cooking test

The user's subsequent 2026-11-07 report, "Tested. Cooking fully works.", supersedes the earlier unresolved cooking-rack failure as the latest local test outcome and includes the deployed Fermenter-enabled mod. Earlier unverified statements describe the evidence available before this retest. Exact station-by-station steps, input order, ownership conditions and save/reload scenarios were not supplied; dedicated multiplayer and persistence verification remain outstanding.

## Combined local deployment test

On 2026-11-07 all four plugins (AutoRepair, CraftFromStorage, CookFromStorage and SupplyFromStorage) were deployed together, each with an identical `ValheimMods.Common.dll`. The checklist covered: single storage consumption for food recipes, racks, oven and Fermenter; non-food crafting from storage; fuel and processing inputs without false missing-fuel messages; manual Foundry casts; repairs at the four ordinary repair stations; and clean startup. The user reported, "Test 1 complete. All passed." This is a summary local single-player result, not a per-scenario record, and does not cover multiplayer or concurrent access.

On the same date, with the same four-plugin deployment, the user ran a save/reload checklist: storage-sourced cooking-station and oven inputs left uncollected, an empty Fermenter loaded, smelter/kiln fuel and ore, one non-food and one food recipe craft, and a repair. Chest counts and station contents were checked after logging out to the menu and reloading, and again after fully quitting the game. Outputs were then collected. The user reported, "All tests passed, including optional 7." This is local single-player persistence evidence only; dedicated-server, multiplayer ownership handoff and concurrent-access scenarios remain unverified.
## Unknowns

- Which current Valheim prefabs attach these components, their configured conversion lists, and the inputs accepted by each prefab (including charcoal kilns, blast furnaces, windmills, spinning wheels, eitr refineries, stone ovens, fires and lights).
- Method-body behavior for the station RPCs and fuel/queue accessors, including whether observed post-RPC state changes are sufficient confirmation for each machine.
- Runtime Harmony registration, server/client ownership transitions, concurrent access from other players or mods, and saved inventory state.
- Whether a different assembly contains relevant component types or whether game updates renamed/moved them. Repeat a bounded search with the [API discovery workflow](../../.github/skills/valheim-api-discovery/SKILL.md) before extending coverage.