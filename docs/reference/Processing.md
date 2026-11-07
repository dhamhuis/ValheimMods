# Fuel and processing

Status: Verified - installed assembly metadata, targeted IL inspection, repository implementation and stub tests.
Last verified: 2026-11-06.
Evidence: [Installed API baseline](Libraries.md); targeted `Get-ValheimApi.ps1` inspection of `Fireplace`, `CookingStation`, `Smelter`, `Windmill`, `ZDOVars`, and `ZDO`; targeted Mono.Cecil IL inspection of `Smelter.OnAddOre` and `Smelter.FindCookableItem`; [SupplyFromStorage patches and transfer logic](../../SupplyFromStorage/src/); [focused behavior tests](../../SupplyFromStorage/tests/Behavior/SupplyBehaviorTests.cs) and [patch integration tests](../../SupplyFromStorage/tests/Integration/PatchIntegrationTests.cs), passing 20 tests and 65 assertions.
Limitations: IL inspection does not establish prefab component assignments, accepted inputs in live prefab data, Harmony startup, multiplayer behavior, or persistence. Stub tests model the observed no-item feedback branch but do not execute the game method. In particular, the processing-family test uses only a `Smelter` test double; its barley/flax/soft-tissue cases do not prove windmill, spinning-wheel, or refinery support.

## Use when

Adding or reviewing E-triggered fuel/processing-input supply, choosing an interaction hook, or checking which installed component APIs SupplyFromStorage currently uses. Read [Storage and ownership](Storage.md) for the repository's container access and mutation policies.

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
| `CookingStation.RPC_AddFuel` | Private instance / `void` | `long sender` |
| `CookingStation.GetFuel` | Private instance / `float` | None |
| `CookingStation.IsItemAllowed` | Private instance / `bool` | `ItemDrop/ItemData item` or `string itemName` |
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

[SupplyFromStorage](../../SupplyFromStorage/src/) installs a guarded postfix on `Fireplace.Interact`, postfixes on `CookingStation.OnAddFuelSwitch` and `Smelter.OnAddFuel`, and a prefix on `Smelter.OnAddOre`. It does not patch station updates. Fuel/processing settings remain separate, and each eligible callback can transfer at most one storage item. Successful transfers have no cooldown so vanilla held-interaction repeats can continue; failed/full/unavailable checks are briefly rate-limited. The fireplace postfix skips alternate actions, vanilla-handled actions (including toggles and inventory fuel), and held callbacks rejected by the fireplace's repeat guard. Fuel switch callbacks only supply when vanilla did not report success and the callback's item argument is null, preventing storage transfers on manual item-use paths.

The inspected `Smelter.OnAddOre` body calls `FindCookableItem` on the user's inventory when its item argument is null. If no item is found, it calls `Character.Message` with `$msg_noprocessableitems` and returns before a postfix can run. SupplyFromStorage therefore uses a prefix for this callback: it leaves accepted inventory items to vanilla, transfers from nearby storage only when no accepted inventory item exists, and skips the vanilla no-item branch only when the storage transfer is confirmed. If accessible storage has an accepted item but the station is full, it displays vanilla's `$msg_itsfull` message and does not consume storage. When neither source has a valid item, vanilla runs and displays its no-processable-items message. The prefix uses current conversion entries and rechecks storage through the normal transfer path; runtime message timing and callback behavior still need in-game validation.

The mod's [storage discovery](../../SupplyFromStorage/src/NearbyStorage.cs) uses a player-centred 20-metre sphere and checks current access, busy state, ward policy, network validity and local container ownership. The transfer path rechecks access, ownership, item identity/quantity, station range and capacity before removing one item. It invokes the machine's vanilla RPC and checks the resulting station state; a rejected transfer attempts to restore the storage item and logs the outcome. A local `m_inUse` reservation and ownership check are not a network-wide atomic lock.

### Bounded type-name search

For the same `assembly_valheim.dll` fingerprint, exact top-level metadata lookups found `Fireplace`, `CookingStation`, `Smelter`, and `Windmill`. They did not find top-level types named `CharcoalKiln`, `StoneOven`, `Oven`, `BlastFurnace`, `SpinningWheel`, `EitrRefinery`, or `Torch`. This is only an exact-name search of top-level types in this assembly; it does not establish that the prefabs or functionality are absent, or that similarly named components do not exist in another assembly.

The `Windmill` metadata exposes a private `Smelter m_smelter` field, but metadata alone does not establish whether a prefab's nested smelter receives the patched update, has a barley conversion, or processes items as assumed. The actual component and input mapping for every supported prefab must be checked in game data and then validated in game. Do not treat item-name scenarios on the generic `Smelter` test double as that evidence.

## Pitfalls

- Do not infer supported prefabs from a component's class name, a private `Windmill.m_smelter` field, or a test-double conversion list.
- Keep fuel and processing-input config paths separate; do not extend a fuel-only hook to cooking ingredients or fermenter bases.
- Recheck actual capacity and accepted inputs before each mutation. A matching shared item name alone does not prove machine acceptance.
- Do not call a transfer successful solely because an RPC was issued; verify a changed machine state and handle failed restoration explicitly.
- SupplyFromStorage does not patch CraftFromStorage's inventory removal overload. The integration test checks this boundary in stubs only; it does not prove coexistence under live patch ordering or multiplayer access.

## Unknowns

- Which current Valheim prefabs attach these components, their configured conversion lists, and the inputs accepted by each prefab (including charcoal kilns, blast furnaces, windmills, spinning wheels, eitr refineries, stone ovens, fires and lights).
- Method-body behavior for the station RPCs and fuel/queue accessors, including whether observed post-RPC state changes are sufficient confirmation for each machine.
- Runtime Harmony registration, server/client ownership transitions, concurrent access from other players or mods, and saved inventory state.
- Whether a different assembly contains relevant component types or whether game updates renamed/moved them. Repeat a bounded search with the [API discovery workflow](../../.github/skills/valheim-api-discovery/SKILL.md) before extending coverage.