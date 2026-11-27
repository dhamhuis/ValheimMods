# FarmFromStorage testing checklist

**In-game status: core single-player scenarios passing; unchecked scenarios, including multiplayer and controller, not performed.** The 2026-11-25 missing-material report was traced to seeds in nearby chests; see Results. Automated production-linked tests and installed-assembly signature/IL inspection do not replace in-game checks. Use a disposable character/world and back up saves. Only check a box after performing its step and observing the expected result; record failures and skipped scenarios in Results.

## 0. Preparation and startup

- [x] Back up the test world; use disposable seeds, crops and tree seeds.
- [x] Build FarmFromStorage with `.\FarmFromStorage\scripts\build.ps1`; build the updated Hammer companion with `.\BuildFromStorage\scripts\build.ps1` if it will be installed.
- [x] In a dedicated test profile with BepInExPack, install the two FarmFromStorage `dist/` DLLs together under `BepInEx/plugins/FarmFromStorage/`. Remove any duplicate plugin installation.
- [x] Initially disable other storage mods. If their folders remain installed, replace their `ValheimMods.Common.dll` copies with the current identical DLL, or remove those disabled folders from the profile.
- [x] Launch Valheim through the profile. Find `FarmFromStorage 1.0.0 loaded` in `BepInEx/LogOutput.log`; confirm there are no missing-library, shared-placement registration, or Harmony errors. Stop if startup fails.
- [x] Enter the test world with a usable Cultivator. Disable free-build/no-placement-cost modes, allow normal stamina and durability, and discover the materials for the plants being tested.
- [x] Set up appropriate cultivated ground and two accessible chests within 20 m of the player, at distinct distances. Label them **near** and **far**.
- [x] Open and close both chests yourself. They must remain closed and locally owned during planting. Opening can transfer ownership but is not a guarantee.
- [x] Pick an unlocked Cultivator plant and record its displayed resource name and cost **K**: plant __________; resource __________; K __________.
- [x] Before every test, remove that resource from other nearby storage, record all starting counts, and close inspected chests. Avoid picking up extra items during the test.

## 1. Inventory-only baseline

- [x] Put K planting materials in inventory and K+2 in the near chest; record counts and close it.
- [x] Select the plant and place once in a valid location.
- [x] Confirm exactly one plant appears, inventory loses K, and both chest counts remain unchanged.
- [x] Confirm normal stamina/durability costs apply and no mod errors appear.

## 2. Storage-only planting and menu availability

- [x] Put zero planting materials in inventory, K+1 in the near chest, and zero in the far chest. Close both.
- [x] Open the Cultivator menu and select the unlocked plant. Confirm its availability reflects sufficient eligible storage; record any incorrect menu indicators separately.
- [x] Place once in a valid location. Confirm one plant appears, inventory stays empty, and near storage has exactly 1 remaining.
- [x] Reopen and close the near chest. Confirm it is not stuck busy and no unexpected chest UI opened during planting.

### First-open availability regression

- [x] Restart the game with FarmFromStorage enabled and enter a world with several unlocked Cultivator plants. Disable free-build mode.
- [ ] Put the cost of one plant only in inventory, another only in an eligible nearby closed chest, and no materials for a third plant in inventory or eligible storage.
- [x] Open the Cultivator menu for the first time. Immediately confirm the inventory-backed and storage-backed plants are lit, and the missing-material plant is greyed out without first attempting to plant or waiting for the icons to update.
- [x] Try the missing-material plant and confirm vanilla's missing-material message appears and no resources are consumed.
- [x] Close the menu, change which materials are available, and reopen it. Immediately confirm all three indicators reflect the changed inventory and storage.
- [x] Repeat with FarmFromStorage disabled and no relevant materials in storage, and confirm the first-open indicators match vanilla inventory-only availability. Re-enable the mod afterward.
- [x] With the updated BuildFromStorage also enabled, open the Hammer menu and confirm its normal availability and placement still work.

## 3. Every visible plantable Cultivator entry

The actual Cultivator menu is the authoritative list for your game and installed content, not a fixed seed whitelist. Repeat all steps below **separately for each plantable entry**. Copy this block for additional entries and record the name/cost/biome/result. Examples to look for where available: crops from seeds, crops planted to produce seeds, Barley/Flax, mushroom crops, and every available tree sapling.

Entry __________; resource/cost __________; required growing conditions __________.

- [x] Discover/unlock the entry normally. Prepare its correct growing conditions and sufficient free space.
- [x] Put zero required materials in inventory and precisely the displayed cost plus one in an eligible chest. Close it.
- [x] Select and place the entry once. Confirm exactly one corresponding plant/sapling appears.
- [x] Confirm the chest lost exactly the displayed cost, one remains, and inventory did not receive temporary materials.
- [x] Confirm vanilla growth status is appropriate; record any unhealthy plant without assuming successful placement guarantees healthy growth.
- [x] Record this entry's result before resetting for the next entry: __________.

## 4. Inventory-first and nearest-first

- [x] For an entry with cost K greater than 1, put K-1 in inventory, 1 in near storage, and K in far storage. For cost-1 entries, use zero in inventory, 1 near and 1 far instead.
- [x] Place once. Confirm inventory is used before storage, near supplies only the shortfall, and far is unchanged.
- [x] If a cost greater than 1 exists, reset to zero inventory, K-1 near and K far; close both and place once.
- [x] Confirm near reaches zero and far loses exactly 1. Mark the split-source portion not applicable if every available entry costs only 1.

## 5. Repeated planting and depletion

- [x] Put zero materials in inventory and exactly 3*K in the near chest. Close it and choose three valid planting positions.
- [x] Plant three times using normal input. Confirm exactly three plants appear and the chest reaches zero.
- [x] Attempt a fourth placement. Confirm no fourth plant appears and nothing else is consumed.
- [x] Add K to the near chest, close it and retry. Confirm availability refreshes and one more plant consumes exactly K.

## 6. Insufficient and wrong materials

- [x] Put only K-1 of the needed material across inventory and both chests; for K=1, put zero. Record counts.
- [x] Attempt placement. Confirm it is rejected and all counts remain unchanged.
- [x] Put a different crop/seed type in the near chest, leaving the needed material absent. Attempt again.
- [x] Confirm the unrelated materials cannot satisfy the selected plant and are not removed.

## 7. Vanilla terrain, biome, spacing and tool restrictions

- [x] Put sufficient materials only in eligible storage and record counts.
- [x] Attempt planting on terrain/ground where vanilla rejects that entry. Confirm placement behavior and feedback match vanilla, with no consumption on a rejected placement.
- [x] Check crowded placement, unsuitable biome and overhead/space restrictions against a vanilla baseline. Some conditions allow placement but produce an unhealthy plant: do not count that as a rejected placement.
- [x] For every rejected attempt, confirm no plant and no consumed material. For an accepted but unhealthy plant, confirm exactly the normal cost is consumed and normal unhealthy feedback remains.
- [x] Attempt planting with insufficient stamina or a broken Cultivator. Confirm vanilla tool/input restrictions are not bypassed and a rejected action consumes no storage materials.
- [x] Restore valid growing conditions, stamina and durability; confirm successful planting from storage.

## 8. Range and configuration

- [x] Exit Valheim, set `Storage radius = 5` under `[Storage]` in `hoose.valheim.farmfromstorage.cfg`, and relaunch.
- [x] Put the only K materials in an eligible chest; stand clearly farther than 5 m from it and attempt placement.
- [x] Confirm storage does not satisfy the cost and no materials are consumed.
- [x] Move clearly within 5 m and place once. Confirm success and exactly K consumed.
- [x] Repeat with vertical separation if practical; confirm distance is spherical and player-centered, not measured from the target soil.
- [x] Exit, restore radius 20, and relaunch before other tests.

## 9. Busy storage, ownership and permissions

- [ ] Using a second player, put the only K materials in a chest and have that player keep it open. Attempt planting; confirm no consumption from the busy chest.
- [ ] Have the second player close it, then attempt planting without opening it yourself. Record availability and counts; a remote-owned chest may remain ineligible.
- [ ] Open and close the chest yourself, retry, and record whether it supplies the plant. Confirm no duplication or unexplained loss; ownership can change.
- [ ] Repeat with a personal chest denying your player access. Confirm protected items are not counted or consumed.
- [ ] Repeat with a ward denying access. Confirm no planting from the protected chest.
- [ ] Restore permissions and close the chest, retry, and confirm the eligible chest can supply planting when locally owned.

## 10. Container lifecycle

- [ ] Build a new chest while the mod is loaded, put K materials in it, close it, and remove all other usable materials.
- [ ] Plant once. Confirm the new chest supplies the cost without restarting the game.
- [ ] Empty and dismantle it; remove returned materials from inventory and the test area.
- [ ] Attempt planting again. Confirm no plant appears and no destroyed-container errors occur.

## 11. Free-build/no-placement-cost mode

- [ ] Record inventory and eligible chest contents, then enable a normal supported free-build/no-placement-cost mode in the disposable world.
- [ ] Place a plant with that mode active. Confirm the chest contents are unchanged and the chest remains accessible.
- [ ] Disable the mode, reset resources, and plant normally. Confirm normal resource consumption resumes.

## 12. Non-planting actions and discovery

- [ ] With FarmFromStorage alone, cultivate soil and restore grass. Confirm their vanilla behavior and no unrelated storage consumption.
- [ ] Harvest a mature plant. Confirm vanilla drops/growth behavior and no automatic transfer to or from storage.
- [ ] Attempt Hammer construction with its only materials in storage and BuildFromStorage disabled. Confirm FarmFromStorage does not supply it.
- [ ] Use an undiscovered plant material stored only in a chest with a fresh character. Confirm storage does not unlock the plant.
- [ ] Discover it normally, return to the closed chest, and confirm the now-unlocked plant can be placed using its stored requirements.

## 13. Updated BuildFromStorage compatibility

- [ ] Install the **current development** BuildFromStorage DLL, not its old prepared ZIP, and the same current common DLL beside both plugins. Restart and verify both startup messages without errors.
- [ ] Set FarmFromStorage radius to 20 and BuildFromStorage radius to 5; put planting/building materials in an eligible chest between 5 and 20 m from the player.
- [ ] Confirm Cultivator planting can use that chest, while Hammer construction cannot.
- [ ] Move within 5 m, perform one Hammer build and one planting action, and confirm each consumes exactly its own normal cost once.
- [ ] Switch tools repeatedly and repeat storage-only actions. Confirm no double counts, double spending or stuck chests.
- [ ] Disable FarmFromStorage and restart; confirm Hammer storage building still works. Then enable FarmFromStorage, disable BuildFromStorage and restart; confirm storage planting still works.
- [ ] Restore both radius settings and repeat the applicable Hammer tests in [BuildFromStorage's checklist](../BuildFromStorage/TESTING.md). Its earlier checked results predate this shared-engine migration.

## 14. Older BuildFromStorage failure message

Use an isolated test profile and preserve the updated DLLs separately.

- [x] Install an older standalone-placement BuildFromStorage DLL with the current common DLL and FarmFromStorage.
- [x] Restart and inspect the log. Confirm FarmFromStorage reports an explicit older-BuildFromStorage update error instead of loading competing farming patches.
- [ ] Do not farm in this intentionally incompatible setup. Restore the updated BuildFromStorage DLL and identical current common DLLs, restart, and confirm both load normally.

## 15. Other companion mods

- [x] Enable CraftFromStorage, CookFromStorage and SupplyFromStorage with identical current common DLLs beside every plugin, then restart.
- [x] Repeat storage-only planting, repeated depletion and one Hammer build. Confirm each consumes materials exactly once.
- [x] Craft a non-food recipe, prepare a food recipe and supply a machine through the corresponding mods. Confirm prior behavior and no competing storage consumption.
- [x] Check startup and action logs for errors; restore the normal profile configuration afterward.

## 16. Save/reload and multiplayer competition

- [x] Plant from storage in single-player, record plant locations and remaining counts, then save and exit normally.
- [x] Reload and confirm the plants remain and storage counts exactly match, without restored or duplicated materials.
- [ ] Repeat storage-backed planting on a disposable shared world/server with both players running current plugins.
- [ ] With exactly K materials in one shared chest and none elsewhere, have both players attempt to plant nearly simultaneously. Record successful plant count and all final inventory/chest counts.
- [ ] Confirm no negative counts, duplicated plants/materials, or unexplained loss; save/reload and check again. Local reservations are not a proven global network lock.
- [ ] Retain both client logs and the server log for any anomaly; mark multiplayer unsafe if duplication or loss occurs.

## 17. Controller

- [ ] If a controller is available, select an unlocked Cultivator plant and place it from storage using normal controller input.
- [ ] Repeat depletion and one rejected placement; confirm exact costs and normal input/feedback. Otherwise record controller testing as not performed.

## 18. Requirement counts (all storage mods)

Install the current build of every storage mod with identical common DLLs. The expected format is the required amount raised one line, with `inventory:storage` in its normal place below it; in tooltips long material names stay on one line while counts are shown. The upper line is the vanilla required amount and keeps vanilla colouring: white when inventory and accessible storage together have enough, flashing red and white when they don't. The number before the colon is the count in your inventory and the number after it the count in accessible nearby storage; the counts line is yellow. A side with none is left blank, for example `1:` or `:3`; with none anywhere, only the vanilla number is shown.

- [x] Put 1 Carrot Seeds in your inventory and 3 in a closed chest within range. Select Carrot Seeds in the Cultivator menu and confirm the tooltip shows `1` raised one line with `1:3` in yellow below, that the `1` is white, and that neither line overlaps the name.
- [x] Move all Carrot Seeds out of the chest. Confirm the tooltip shows `1` with `1:` below, with the `1` white.
- [x] Remove the carried seed so you have none anywhere. Confirm the tooltip shows only `1`, flashing red and white exactly as vanilla.
- [x] Put 3 seeds back in the chest only. Confirm `1` with `:3` below, with a white `1`, then plant once and confirm it updates to `1` with `:2` below.
- [x] Open the chest (so it is busy) or step out of range. Confirm the counts after the colon disappear and the `1` flashes red.
- [x] With BuildFromStorage installed, select a Hammer piece with less wood in inventory than required but enough in storage, for example a Wood wall with 1 carried and 10 stored. Confirm `2` raised with `1:10` below, with a white `2`. Select a piece using a long material name such as Black Metal (Pots and Pans) with counts shown, and confirm the name stays on one readable line without overlapping the amount; then confirm it wraps as in vanilla again when there are no counts. Remove the stored wood and confirm `2` with `1:` below flashing red.
- [x] At a Workbench, select a recipe whose materials are split between inventory and storage. Confirm each ingredient shows the required amount raised one line with the yellow counts in its original place, so the counts are not hidden under the control below. Check an equipment upgrade too, and that an ingredient with no counts shows the amount in its normal position. Then switch to the Upgrade tab with nothing to upgrade, and open another station with nothing to upgrade, and confirm no counts are left behind.
- [x] With the Hammer, hover a piece with two materials and counts on both, then a neighbouring piece with one material. Confirm the next slot (now the crafting station) shows no leftover counts. At the Workbench, select a recipe with more ingredients, then one with fewer, and confirm no leftover counts remain under the hidden slots.
- [x] At a Cauldron (CookFromStorage), select a food recipe with ingredients in storage. Confirm the same format.
- [x] Use large stacks, for example 100 carried and 500 stored, and confirm `100:500` fits on its own line without truncation and the next requirement row is not overlapped.
- [x] Unequip the tools and close the crafting panel. Confirm other vanilla UI (inventory tooltips, other tools) shows the normal amounts.
- [x] Confirm SupplyFromStorage still loads and supplies a machine with the shared common DLL.

## Results and cleanup

- **2026-11-25:** The tester reported that most Cultivator entries were enabled and could be planted without accessible materials, and that Barley and Flax could be placed in the Meadows. Resolved on 2026-11-26: see below.
- [ ] Record game version, plugin builds, profile, single-player/server, date, tester and tests skipped: __________.
- [ ] For each failure record test/step, plant/resource, initial/final counts, distances, permissions/ownership conditions and relevant log entries: __________.
- [ ] Restore radius settings, no-cost modes and profile changes. Remove deliberately incompatible/duplicate DLLs.
- [ ] Report results separately for planting entries, Hammer regression, multiplayer, persistence and controller.
- [ ] Package only after the applicable tests are satisfactory; do not mark unchecked scenarios as passed.
- **2026-11-26 vanilla baseline (FarmFromStorage removed, same world):** only entries with carried materials were enabled, so no world free-build key explains the modded result. Vanilla also allows Barley and Flax outside the Plains; they turn brown with a wrong-biome tooltip. Biome placement is therefore not a mod regression.
- **2026-11-26 resolved, not a mod bug:** nearby chests held carrots, flax, barley and turnips, which the mod correctly counted. After removing all seeds from inventory and nearby chests, every Cultivator entry was greyed out, and placement was rejected with the vanilla missing-resource message. Adding Flax to a chest enabled Flax in the menu and allowed planting it from storage. The temporary diagnostic logging has been removed.
- **2026-11-26 first open:** after a fresh game start the tester confirmed the Cultivator icons were correct on the very first menu open, without reopening. The exact inventory-only/storage-only/missing three-plant split in the first-open setup step was not recorded separately; the Hammer regression step remains unchecked.
- **2026-11-26 final single-player results:** the tester confirmed Hammer building with BuildFromStorage, rejection after storage depletion, unrelated seeds not satisfying a plant, and single-player save/reload. Multiplayer and controller scenarios were not performed and remain untested.
- **2026-11-26 companion mods:** the tester ran all storage mods together and reported they work fine.
- **2026-11-26 old BuildFromStorage:** with the committed older BuildFromStorage DLL and current common DLLs, FarmFromStorage logged the explicit older-BuildFromStorage InvalidOperationException and did not register; other plugins loaded. Hammer building and Cultivator planting from storage still worked because the old BuildFromStorage scopes every placement tool, which is the competing behavior the guard prevents. The current BuildFromStorage DLL was then restored.