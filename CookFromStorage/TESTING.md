# CookFromStorage testing

## Input-selection checkpoint

Status: Passed by user report on 2026-11-16. All 17 steps below were checked when the user confirmed, "Testing passed." This records the local checkpoint, not independent observation or captured per-step logs. Multiplayer, controller-specific behavior, other resolutions and abnormal failures remain unverified. The tested build was deployed on 2026-11-13; no new assembly inspection, rebuild or deployment was performed to record these results.

Use a backed-up world/character and small stacks. Keep both CookFromStorage and SupplyFromStorage enabled for the shared-provider checks.

- [x] Restart through the test profile; confirm both mods load and Cook reports its shared cooking/Fermenter picker registration without errors.
- [x] At a wooden Cooking Station, open F8 with multiple accepted raw foods split between inventory and an accessible chest. Check localized names, exact counts, all accepted tiles and no consumption while editing.
- [x] Select one available type, close, and press E. Verify exactly one of that type is consumed, using inventory before storage for that same type.
- [x] In Auto, move a stored type above a different carried type. Verify E uses the preferred stored type, not the carried alternative.
- [x] Exclude every available type. Verify Auto consumes neither source. Explicitly select an excluded type and verify E can add exactly that type.
- [x] Exhaust a selected type while another type is available. Verify no substitution and a localized unavailable-type message; release before retrying.
- [x] Check missing-fire/full messages consume nothing. Restore readiness, release/repress and verify insertion works.
- [x] Collect cooked food with ingredients available and exclusions set. Verify collection takes precedence and does not also consume an ingredient.
- [x] Repeat targeting and exact insertion at the Iron Cooking Station and Stone Oven. Oven fuel must not open the picker or change fuel behavior.
- [x] At an empty sheltered Fermenter, verify the accepted base list, counts, paging and priority editing. Select a base and verify one exact inventory/storage base starts a batch.
- [x] Verify missing-roof/exposure checks consume nothing, a running batch accepts no new base, and finished-batch collection does not reload.
- [x] Verify ordinary explicit item use remains vanilla and does not replace the picker's manual choice.
- [x] Alternate F8 between cooking/Fermenter inputs and Supply's kiln/furnaces/ram. Confirm the correct list/settings open each time, only one dialog appears, and closing restores controls without accidental insertion.
- [x] Move between station types while holding use; verify the old type/hold does not carry over. Check Escape/F8 close and release/repress behavior.
- [x] Restart with the same character and verify priorities/exclusions persist separately by station type; another character should receive fresh conversion-order defaults.
- [x] Disable Cook's input selection and verify legacy inventory-first cooking/brewing. Disable Automatic ingredients and verify selection/storage crafting stop while vanilla inventory actions remain available.
- [x] Check the Frost Foundry still has no ingredient picker and no stored casts are automatically loaded; food recipes and Supply fuel handling remain unchanged.

The discovery filter and 2026-11-18 continuous held-input change postdate this checkpoint (step 6's "no substitution" now applies to explicit selections only, and step 14 now expects a hold to restart on the new station). On 2026-11-20 the user reported that in-game testing was complete; individual results for the later [discovery](../SupplyFromStorage/TESTING.md#discovery-filter-checkpoint---pending) and [held-input](../SupplyFromStorage/TESTING.md#continuous-held-input-checkpoint---pending) checklists were not recorded.

Stub tests and a successful build do not prove live prefab setup, Harmony startup, multiplayer behavior or persistence. Test the supported station prefabs, input order, multiplayer ownership and saved item state in game before release. For Fermenters, check an accepted stored base with empty inventory, inventory-first input, missing roof/exposure, a running batch, finished-mead collection without reloading, and save/reload persistence.