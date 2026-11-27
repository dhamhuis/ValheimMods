# CraftFromStorage testing

The test executable links the production storage/context/planning/transaction code and every CraftFromStorage patch against small game stubs. It also applies those patches with test-only HarmonyX to exercise actual prefixes, postfixes and finalizers, including simulated output creation and ingredient-removal callbacks. HarmonyX 2.16.1 is used only by the .NET 10 test executable because the profile's older Mono-targeted Harmony build cannot initialize there; production plugins continue referencing the profile's installed Harmony.

Tests are organized into `Behavior/`, `Integration/` and `TestDoubles/`. Shared game/Unity stubs and neutral logging live in the independent [ValheimMods.Testing](../tests/ValheimMods.Testing/) support project. CraftFromStorage's tests contain no AutoRepair source or plugin doubles; AutoRepair has its own test project.

[Behavior tests](tests/Behavior/StorageBehaviorTests.cs) and [patch integration tests](tests/Integration/PatchIntegrationTests.cs) use independently arranged, named scenario methods with short XML summaries. The entry point only runs these suites and prints test/assertion totals; failures identify the scenario method.

These tests do not emulate Unity, actual container persistence or networking. The signature validator separately checks all Harmony target methods and argument bindings against the installed game DLL without launching it. Passing either check does not complete the in-game review checklist below.

## In-game review checklist

Local in-game testing was reported successful for both CraftFromStorage and AutoRepair. The checklist below remains useful for regression testing; dedicated multiplayer, simultaneous-access and persistence coverage has not been confirmed.

- Check the verified station-crafting startup message and absence of Harmony errors in `BepInEx/LogOutput.log`.
- Craft with an empty ingredient inventory; split resources across inventory, two chests, carts and ships. Verify exact debits and inventory output.
- Test just inside, exactly at and just outside the configured radius (default 20m), including vertically separated storage and diagonal corners outside the sphere.
- Test workbench, forge, quality upgrades, multi-craft and any available one-of/variable-yield recipes. Food stations must stay vanilla unless CookFromStorage is installed.
- Test unknown recipes, low-level/wrong/unusable stations, full inventory and no-cost modes. Storage must not bypass vanilla restrictions.
- Check that requirement counts show the required amount with `inventory:storage` on a yellow line below, leaving a side with none blank and showing only the vanilla amount when neither has any, for normal recipes and upgrades.
- Test locked personal chests, ward denial, open chests, moving carts and containers destroyed while a request is pending.
- Test no hand-crafting, hammer-building, machine fueling/input or repair behavior changes. Keep AutoRepair enabled to verify coexistence.
- With two clients, test remote-owned and simultaneously accessed chests, permission changes, disconnect/timeouts, cancellation and walking out of range while awaiting ownership. After a lost reply, retry crafting and manual opening without reloading; deliver delayed/duplicate replies and verify they do not unexpectedly open a chest or spend ingredients twice.
- Reopen chests and relog/restart to confirm saved quantities persist; watch both clients' logs for failures or desynchronization.