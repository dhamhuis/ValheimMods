# CraftFromStorage

CraftFromStorage by dhamhuis lets the local player craft and upgrade equipment at crafting stations using accessible storage within a **20 metre sphere centered on the player**.

The [Thunderstore README](Thunderstore/README.md) is the player-facing package page.

## Scope

- Station recipes, including cauldron recipes and equipment upgrades.
- Inventory first, then the nearest eligible storage, splitting requirements across stacks and containers.
- All loaded Valheim `Container` storage: ordinary, reinforced, black metal and accessible personal chests, carts, ships, and compatible modded containers.
- Personal chest and ward permissions remain in force. Open/busy containers and carts in use are excluded.
- Vanilla station levels, shelter/fire checks, recipe discovery, DLC checks, output capacity, item quality, world-level matching and no-cost modes remain in force.
- Requirement coloring and craftability checks include eligible storage. Requirements are not automatically learned from chest contents.
- Output goes to the player's inventory, never automatically back to storage.

No hand crafting, hammer building, repairs, fueling, automatic machine inputs, stashing, sorting, or new hotkeys. The radius is fixed, not configurable.

## Implementation

This is an original implementation. [Teflon Ted's CraftFromChests](https://github.com/trak3r/valheim/tree/main/mods/CraftFromChests) and [SmartCraft-Storage](https://github.com/Zellds/SmartCraft-Storage) were consulted for behavior and hook ideas; no third-party source was copied or bundled.

- [NearbyStorage](src/NearbyStorage.cs) tracks containers and applies spherical distance and access checks. Discovery is cached only for the current frame and player position; final consumption uses fresh validation.
- [CraftingContext](src/CraftingContext.cs) confines additional counts to station requirement checks, recipe ingredient selection, requirement UI and the actual crafting transaction. Finalizers restore nested scopes even on exceptions.
- [IngredientPlan](src/IngredientPlan.cs) allocates exact stacks before vanilla creates output, accounting for upgrades, multi-craft, alternative ingredients, quality and world level. Only planned string-based ingredient removals are redirected; unrelated inventory operations remain vanilla.
- [StorageCrafting](src/StorageCrafting.cs) uses vanilla `RPC_RequestOpen` / `RPC_OpenResponse` to obtain ownership of needed chests without opening their UI. Once ownership arrives, it reloads and reserves closed chests using the owner's busy flag, then revalidates everything before invoking vanilla crafting. Inventory changes use normal removal methods and the container's normal save callback.
- The [individual Harmony patch classes](src/) wire only these surfaces, one class per file. Startup checks patch registration; teardown removes patches and releases reservations.

Ownership can introduce a short delay on remote-owned storage. Denial, a five-second timeout, missing resources or lost access cancels the craft without spending ingredients. Closing the crafting UI, changing recipes/stations, dying or pressing Cancel also aborts pending work. A timed-out or cancelled request releases the chest for crafting retries and explicit manual opening without requiring a response or plugin reload. Unsolicited late/duplicate responses remain suppressed until that container is destroyed or the plugin is unloaded.

Vanilla replies have no request ID. A late reply can therefore satisfy a new pending action, but crafting rechecks ownership, inventory and access before consuming anything. Explicit manual opening of a previously requested chest authorizes one reply for at most five seconds; its player identity, life state, interaction distance, permissions, busy state and ownership are rechecked before allowing vanilla to open the UI. Repeated manual requests can be retried after that window expires.

Install on each client that wants this behavior. The ownership protocol uses vanilla RPCs, so it does not require a server plugin. **Multiplayer behavior still needs in-game validation**, including simultaneous access; do not treat compilation or stub tests as proof of network correctness. Other craft-from-chest mods should be disabled to avoid double counting or consumption hooks.

## Build and validation

```powershell
.\CraftFromStorage\scripts\build.ps1
.\scripts\Validate-ModPatches.ps1 -ModDirectory .\CraftFromStorage
dotnet run --project .\CraftFromStorage\tests\CraftFromStorage.Tests.csproj --configuration Release
.\CraftFromStorage\scripts\build.ps1 -Deploy
.\CraftFromStorage\scripts\pack.ps1
```

The test executable links the production storage/context/planning/transaction code and every CraftFromStorage patch against small game stubs. It also applies those patches with test-only HarmonyX to exercise actual prefixes, postfixes and finalizers, including simulated output creation and ingredient-removal callbacks. HarmonyX 2.16.1 is used only by the .NET 10 test executable because the profile's older Mono-targeted Harmony build cannot initialize there; production plugins continue referencing the profile's installed Harmony.

Tests are organized into `Behavior/`, `Integration/` and `TestDoubles/`. Shared game/Unity stubs and neutral logging live in the independent [ValheimMods.Testing](../tests/ValheimMods.Testing/) support project. CraftFromStorage's tests contain no AutoRepair source or plugin doubles; AutoRepair has its own test project.

[Behavior tests](tests/Behavior/StorageBehaviorTests.cs) and [patch integration tests](tests/Integration/PatchIntegrationTests.cs) use independently arranged, named scenario methods with short XML summaries. The entry point only runs these suites and prints test/assertion totals; failures identify the scenario method.

These tests do not emulate Unity, actual container persistence or networking. The signature validator separately checks all Harmony target methods and argument bindings against the installed game DLL without launching it. Passing either check does not complete the in-game review checklist below.

See the [development guide](../docs/Development.md) for shared tooling and prerequisites.

## In-game review checklist

Local in-game testing was reported successful for both CraftFromStorage and AutoRepair. The checklist below remains useful for regression testing; dedicated multiplayer, simultaneous-access and persistence coverage has not been confirmed.

- Check the verified station-crafting startup message and absence of Harmony errors in `BepInEx/LogOutput.log`.
- Craft with an empty ingredient inventory; split resources across inventory, two chests, carts and ships. Verify exact debits and inventory output.
- Test just inside, exactly at and just outside 20m, including vertically separated storage and diagonal corners outside the sphere.
- Test workbench, forge, cauldron, quality upgrades, multi-craft and any available one-of/variable-yield recipes.
- Test unknown recipes, low-level/wrong/unusable stations, full inventory and no-cost modes. Storage must not bypass vanilla restrictions.
- Test locked personal chests, ward denial, open chests, moving carts and containers destroyed while a request is pending.
- Test no hand-crafting, hammer-building, machine fueling/input or repair behavior changes. Keep AutoRepair enabled to verify coexistence.
- With two clients, test remote-owned and simultaneously accessed chests, permission changes, disconnect/timeouts, cancellation and walking out of range while awaiting ownership. After a lost reply, retry crafting and manual opening without reloading; deliver delayed/duplicate replies and verify they do not unexpectedly open a chest or spend ingredients twice.
- Reopen chests and relog/restart to confirm saved quantities persist; watch both clients' logs for failures or desynchronization.

Release notes: [CHANGELOG.md](CHANGELOG.md). Icon source: [assets/icon.svg](assets/icon.svg), using the repository's shared frame.