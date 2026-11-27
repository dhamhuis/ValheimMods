# Processing input-selection checklist

Use this file for repeatable manual tests and results instead of chat-only instructions. For a fresh run, clear the checkboxes, record the build/profile/date, and check each step after confirming its expected result. Stop if items disappear unexpectedly or duplicate.

On 2026-11-20 the user reported, "In game testing is now complete." This is overall user-reported acceptance; individual D and H checklist results were not recorded here, so their unchecked boxes are retained.

On 2026-11-09 the user also passed the non-consuming preview's initial opening, icons/counts/highlighting and Escape/control-restoration checks. That preview result did not establish selected-type insertion, broader input, controller or UI-scale behavior.

## Recorded run

- Date: 2026-11-09.
- Profile: Valheim Mod Building.
- Feature source: `5054a09`, including the fireplace fix from main.
- Deployment: feature DLLs redeployed and hash-verified after the initial F8 failure was traced to an installed main-only build.
- SupplyFromStorage DLL SHA-256: `CAF6C465F0BDEA8B582E13DC9B95B44A0A6058A7348EE7DE1B8FF65B0C21BFC3`.
- Shared DLL SHA-256: `410EE428FC79E40D2A0AB41B0948016B6002B0E92C487271EBF9AD7CD0A312DE`.
- Result: the user reported all nine numbered scenarios passed, including same-character restart and separate defaults on another character.
- Evidence: checked steps below reflect that scenario-level user report, not independent observation or captured item-count logs.
- Not established: multiplayer/concurrent access, controller-specific behavior, other resolutions, malformed-config handling in game, or broader station regressions.

## Setup for a new run

- [ ] Record date, profile, source commit and installed DLL hashes.
- [ ] Use a backed-up world/character and small item stacks.
- [ ] Confirm the feature DLLs and matching common libraries are installed; restart after any replacement. A main-only build does not provide the picker even if old selection settings remain in the config.
- [ ] Enable `Input selection/Enabled` and `Automation/Automatic processing inputs`.
- [ ] Use a kiln with enough empty input space for the current test.
- [ ] Remove unintended wood from all eligible nearby chests. Open and close the test chest if necessary to obtain normal local ownership; do not leave it open/in use.
- [ ] Keep aiming at the kiln input between selection and insertion checks. Leaving the input intentionally clears explicit selection.

Setup checkboxes are deliberately unchecked: the previous report did not separately confirm backups or every setup action.

## Hint and controls

The translucent box near the bottom of the screen is the intended input hint, not the picker. For example, `Auto: Wood (inventory 5, storage 0) | F8: choose / preferences` means automatic E currently chooses Wood, with five in inventory and none in eligible storage. Counts are advisory, not reservations. While holding or stopped, the hint identifies that state and type.

F8 opens the picker. Highlighting alone changes nothing; **Use highlighted** arms a type, **Auto** clears it, and Escape/F8/Cancel dismisses without changing the armed type. Preference edits save immediately even if the picker is cancelled.

UX feedback from this run: the hint was unexpected. Its visibility, positioning and styling are unchanged; no request to remove it or make it optional has been assumed.

## 1. Explicit selection - passed

- [x] Carry 5 Wood and 5 Fine Wood.
- [x] Aim at the input, open F8, highlight Fine Wood and choose **Use highlighted**.
- [x] Verify choosing consumes nothing: still 5 of each.
- [x] Release controls and tap E once. Expected: 5 Wood, 4 Fine Wood; exactly one Fine Wood added.

## 2. Cancel versus Auto - passed

- [x] Keep Fine Wood selected. Open F8, highlight Wood, then press Escape.
- [x] Tap E. Expected: another Fine Wood consumed, not Wood.
- [x] Open F8 and choose **Auto**. Expected: hint changes to Auto without consuming an item.

## 3. Priority across inventory and storage - passed

- [x] Carry 5 Fine Wood, no Wood; put 3 Wood in the eligible test chest and close it.
- [x] Allow Wood/Fine Wood, click Wood's **Move to first priority**, then choose **Auto**.
- [x] Tap E. Expected: chest Wood 3 -> 2, inventory Fine Wood remains 5.

## 4. Inventory first within the chosen type - passed

- [x] Carry 2 Wood and leave 2 Wood in the chest. Keep Wood first in Auto.
- [x] Tap E. Expected: inventory Wood 2 -> 1; chest Wood remains 2.

## 5. Automatic exclusions and explicit override - passed

- [x] Carry 3 Fine Wood and 3 Core Wood; remove all eligible Wood.
- [x] Mark Fine Wood and Core Wood **Never auto-add**, then choose **Auto**.
- [x] Tap and then hold E. Expected: nothing consumed, no vanilla fallback, and no repeated stop-message spam during the hold.
- [x] Release E, explicitly select Fine Wood, and tap E. Expected: exactly one Fine Wood consumed despite its automatic exclusion.

Observed at step 5.3: `No allowed kiln input is available. Use F8 to choose a type or edit automatic preferences.` The hint showed `none` with inventory/storage counts of zero. Here `none` means no available allowed Auto candidate; it does not mean the excluded materials are absent. As of 2026-11-18 this message is shortened to `Items set to Never auto-add (F8)`.

## 6. Held-E type lock - passed

- [x] Allow all types, put Wood first, choose Auto.
- [x] Carry 1 Wood and 5 Fine Wood; put 2 Wood in the chest.
- [x] Hold E beyond three additions. Expected: exactly 3 Wood consumed across inventory/storage, Fine Wood remains 5, and insertion stops without changing type.
- [x] Verify the stopped/held hint refers to Wood rather than switching to Fine Wood.
- [x] Release and press E again. Expected: Auto may now add Fine Wood.

## 7. Exact selection must not substitute - passed

- [x] Carry 1 Fine Wood and 5 Wood, with no other eligible Fine Wood.
- [x] Explicitly select Fine Wood and hold E. Expected: only the one Fine Wood consumed; Wood remains 5 through subsequent repeats.
- [x] Release and tap E again without changing selection. Expected: still no Wood consumed.

## 8. Fullness and interruption - passed

- [x] With a full kiln, select an available type and hold E. Expected: no items consumed; newly available capacity does not restart this stopped hold without release.
- [x] While holding E, look away or change kiln. Expected: insertion stops and leaving the input clears temporary selection.
- [x] Open F8, hold E while closing it, then release. Expected: no leaked insertion; a fresh E press works.
- [x] Additional user test: start inserting with held E while F8 is closed, then open F8. Expected: insertion stops.
- [x] Continue holding E and close F8. Expected: insertion does not resume until controls are released and a new use press occurs.

## 9. Persistence and character separation - passed

- [x] Block Core Wood, put Fine Wood first, and explicitly select Wood.
- [x] Quit and restart the profile, then return to the kiln.
- [x] Verify Core Wood remains blocked and Fine Wood remains first, but temporary selection resets to Auto.
- [x] Load another character. Expected: that character has default preferences, not the first character's edits.

## Regression coverage R1-R4 - passed

On 2026-11-12 the user reported all four regression sections passed. All 188 individual R-step checkboxes were checked in this document. This establishes user-reported local success for disabled-selection behavior, disabled-processing behavior, manual item-use override and unchanged Smelter ore/fuel behavior. It does not complete the broader follow-up coverage below.

For a repeat run, clear the R-step checkboxes first. Each checkbox is one action or verification. Report a section and step ID, such as `R2.18 passed` or `R4.34 failed: chest coal stayed at 3`. These step IDs replace the earlier grouped R-step IDs. Do not repeat the nine passed scenarios unless investigating a regression.

### Configuration and counting rules for R1-R4

- Settings refer to the profile's `BepInEx/config/hoose.valheim.supplyfromstorage.cfg`. Names repeat between sections: change `Enabled` only under the named section.
- Quit Valheim fully before editing the file, then restart through Valheim Mod Building. Do not rely on live config reload or edit while the game can overwrite it. Keep the existing feature DLLs installed.
- Leave `Storage/Storage radius` unchanged. Use one accessible, locally owned chest well inside that radius, closed during transfers.
- Back up the config and record existing priorities/exclusions before the first section. Use small stacks and a backed-up world/character.
- Use a kiln with at least four free input spaces. Remove all unintended accepted materials from nearby eligible chests. Count item stacks before and after each tap, not charcoal output.
- Wait at least six seconds after an intentionally failed legacy storage attempt before retrying; failure checks can have a cooldown. Release E between taps.
- The preview flag is deliberately left enabled in R1/R2 to verify it cannot independently activate selection.

### R1. Selection disabled: legacy inventory-first and storage fallback

Goal: disabling only selection restores normal inventory handling, even for an excluded type, but keeps processing-from-storage enabled.

#### Establish preferences

Start with selection and automatic processing enabled.

- [x] **R1.1:** Aim at the kiln input.
- [x] **R1.2:** Press F8.
- [x] **R1.3:** Record the current priorities/exclusions for restoration later.
- [x] **R1.4:** Set Fine Wood to **Never auto-add** using its auto-status button.
- [x] **R1.5:** Click Wood's **Move to first priority**.
- [x] **R1.6:** Click **Auto**.

#### Disable selection and restart

- [x] **R1.7:** Quit Valheim fully.
- [x] **R1.8:** Open the profile's SupplyFromStorage config file.
- [x] **R1.9:** Under `[Input selection]`, set `Enabled = false`.
- [x] **R1.10:** Under `[Input selection preview]`, set `Enabled = true`.
- [x] **R1.11:** Under `[Automation]`, set `Automatic processing inputs = true`.
- [x] **R1.12:** Under `[Automation]`, set `Automatic fuel = true`.
- [x] **R1.13:** Save the config file.
- [x] **R1.14:** Launch Valheim through Valheim Mod Building.
- [x] **R1.15:** Load the same character/world.

#### Check preview and inventory priority

- [x] **R1.16:** Arrange your inventory to contain exactly 2 Fine Wood and no other kiln inputs.
- [x] **R1.17:** Arrange eligible nearby storage to contain exactly 3 Wood and no other kiln inputs.
- [x] **R1.18:** Close the chest.
- [x] **R1.19:** Verify the kiln has at least three free input spaces.
- [x] **R1.20:** Aim at the kiln input.
- [x] **R1.21:** Press F8.
- [x] **R1.22:** Verify the window describes preview-only behavior.
- [x] **R1.23:** Click the Wood tile.
- [x] **R1.24:** Press Escape.
- [x] **R1.25:** Verify no controlled Auto/Selected hint appears outside the preview.
- [x] **R1.26:** Verify inventory Fine Wood is still 2.
- [x] **R1.27:** Verify chest Wood is still 3.
- [x] **R1.28:** Close any inventory/chest UI.
- [x] **R1.29:** Aim at the kiln input.
- [x] **R1.30:** Tap E once.
- [x] **R1.31:** Verify inventory Fine Wood is now 1.
- [x] **R1.32:** Verify chest Wood is still 3.

These counts confirm exactly one inventory input was added despite its saved exclusion.

#### Check storage fallback and restore

- [x] **R1.33:** Move the remaining inventory Fine Wood outside all eligible nearby storage.
- [x] **R1.34:** Close any inventory/chest UI.
- [x] **R1.35:** Aim at the kiln input.
- [x] **R1.36:** Tap E once.
- [x] **R1.37:** Verify chest Wood is now 2.
- [x] **R1.38:** Quit Valheim fully.
- [x] **R1.39:** In the config, set `[Input selection]` / `Enabled = true`.
- [x] **R1.40:** Save the config.
- [x] **R1.41:** Launch the profile and load the same character/world.
- [x] **R1.42:** Aim at the kiln input.
- [x] **R1.43:** Press F8.
- [x] **R1.44:** Verify Fine Wood is still marked **Never auto-add**.
- [x] **R1.45:** Verify Wood is still first priority.
- [x] **R1.46:** Click **Auto**.

Result/notes:

### R2. Automatic processing disabled: no storage insertion, vanilla inventory allowed

Goal: the processing toggle disables both storage processing and controlled selection without disabling fuel automation.

#### Establish preferences

Start with selection and automatic processing enabled.

- [x] **R2.1:** Aim at the kiln input.
- [x] **R2.2:** Press F8.
- [x] **R2.3:** Set Fine Wood to **Never auto-add**.
- [x] **R2.4:** Click Wood's **Move to first priority**.
- [x] **R2.5:** Click **Auto**.

#### Disable processing and restart

- [x] **R2.6:** Quit Valheim fully.
- [x] **R2.7:** Open the profile's SupplyFromStorage config file.
- [x] **R2.8:** Under `[Input selection]`, set `Enabled = true`.
- [x] **R2.9:** Under `[Input selection preview]`, set `Enabled = true`.
- [x] **R2.10:** Under `[Automation]`, set `Automatic processing inputs = false`.
- [x] **R2.11:** Under `[Automation]`, set `Automatic fuel = true`.
- [x] **R2.12:** Save the config file.
- [x] **R2.13:** Launch the profile and load the same character/world.

#### Check storage is blocked and preview is non-consuming

- [x] **R2.14:** Remove all Wood, Fine Wood and Core Wood from your inventory.
- [x] **R2.15:** Arrange eligible storage to contain exactly 3 Wood and no other kiln inputs.
- [x] **R2.16:** Close the chest.
- [x] **R2.17:** Verify the kiln has free input capacity.
- [x] **R2.18:** Aim at the kiln input.
- [x] **R2.19:** Tap E once.
- [x] **R2.20:** Verify no input was added; a normal missing-processable-items message is acceptable.
- [x] **R2.21:** Verify chest Wood is still 3.
- [x] **R2.22:** Close the chest.
- [x] **R2.23:** Aim at the kiln input.
- [x] **R2.24:** Press F8.
- [x] **R2.25:** Verify the window is preview-only, without consuming selection/preference controls.
- [x] **R2.26:** Click the Wood tile.
- [x] **R2.27:** Press Escape.
- [x] **R2.28:** Verify no controlled Auto/Selected hint appears.
- [x] **R2.29:** Verify chest Wood is still 3.

#### Check vanilla inventory use and restore

- [x] **R2.30:** Arrange your inventory to contain exactly 2 Fine Wood and no other kiln inputs.
- [x] **R2.31:** Close any inventory/chest UI.
- [x] **R2.32:** Aim at the kiln input.
- [x] **R2.33:** Tap E once.
- [x] **R2.34:** Verify inventory Fine Wood is now 1.
- [x] **R2.35:** Verify chest Wood is still 3.
- [x] **R2.36:** Quit Valheim fully.
- [x] **R2.37:** In the config, set `[Automation]` / `Automatic processing inputs = true`.
- [x] **R2.38:** Save the config.
- [x] **R2.39:** Launch the profile and load the same character/world.
- [x] **R2.40:** Aim at the kiln input.
- [x] **R2.41:** Press F8.
- [x] **R2.42:** Verify consuming selection/preference controls have returned.
- [x] **R2.43:** Verify Fine Wood remains excluded and Wood remains first priority.
- [x] **R2.44:** Click **Auto**.

Result/notes:

### R3. Explicit manual item-use overrides automatic exclusion

Goal: a normal explicit item-use request remains independent of Auto exclusions and the picker's chosen type.

#### Prepare

Selection, automatic processing and automatic fuel must all be enabled, as restored by R1/R2.

- [x] **R3.1:** Verify these config settings are all `true`: `[Input selection]` / `Enabled`, `[Automation]` / `Automatic processing inputs`, and `[Automation]` / `Automatic fuel`. If you change them, save while Valheim is closed and restart the profile before continuing.
- [x] **R3.2:** Arrange your inventory to contain exactly 2 Wood and 3 Fine Wood, with no Core Wood.
- [x] **R3.3:** Put the Fine Wood stack in a hotbar slot.
- [x] **R3.4:** Record that slot's number key.
- [x] **R3.5:** Remove all kiln inputs from eligible nearby storage.
- [x] **R3.6:** Close any inventory/chest UI.
- [x] **R3.7:** Verify the kiln has at least four free input spaces.

#### Block Auto

- [x] **R3.8:** Aim at the kiln input.
- [x] **R3.9:** Press F8.
- [x] **R3.10:** Set Wood to **Never auto-add**.
- [x] **R3.11:** Set Fine Wood to **Never auto-add**.
- [x] **R3.12:** Set Core Wood to **Never auto-add**.
- [x] **R3.13:** Click **Auto**.
- [x] **R3.14:** Release all use controls.
- [x] **R3.15:** Tap E once.
- [x] **R3.16:** Verify the no-allowed-input message appears.
- [x] **R3.17:** Verify inventory Wood remains 2 and Fine Wood remains 3.
- [x] **R3.18:** Close the inventory.

#### Arm Wood, then manually use Fine Wood

- [x] **R3.19:** Aim at the kiln input.
- [x] **R3.20:** Press F8.
- [x] **R3.21:** Click the Wood tile.
- [x] **R3.22:** Click **Use highlighted**.
- [x] **R3.23:** Release all use controls.
- [x] **R3.24:** Verify the hint shows selected Wood.
- [x] **R3.25:** Press the Fine Wood hotbar number key once while still aiming at the input. Do not press E.
- [x] **R3.26:** Verify Fine Wood is now 2.
- [x] **R3.27:** Verify Wood remains 2.
- [x] **R3.28:** Close the inventory.

If R3.25 does not attempt manual insertion, stop this section and report `R3.25 blocked: manual item-use action not triggered`, including any message. Do not substitute an E/picker action.

#### Verify picker selection survived manual use

Opening inventory to check counts does not intentionally clear selection, but looking away from the kiln does. Keep the input targeted throughout this part.

- [x] **R3.29:** Release all use controls.
- [x] **R3.30:** Tap E once.
- [x] **R3.31:** Verify Wood is now 1.
- [x] **R3.32:** Verify Fine Wood remains 2.
- [x] **R3.33:** Close the inventory.

#### Return to Auto and restore

- [x] **R3.34:** Press F8 while aiming at the input.
- [x] **R3.35:** Click **Auto**.
- [x] **R3.36:** Release all use controls.
- [x] **R3.37:** Tap E once.
- [x] **R3.38:** Verify Wood remains 1 and Fine Wood remains 2.
- [x] **R3.39:** Close the inventory.
- [x] **R3.40:** Press F8 while aiming at the input.
- [x] **R3.41:** Restore Wood's original allowed/excluded state.
- [x] **R3.42:** Restore Fine Wood's original allowed/excluded state.
- [x] **R3.43:** Restore Core Wood's original allowed/excluded state.
- [x] **R3.44:** Restore your recorded priority order using **Move to first priority**.
- [x] **R3.45:** Click **Auto**.

Result/notes:

### R4. Other station and fuel slot remain unchanged

Goal: kiln preferences do not take over a Smelter's ore interaction or its separate coal input. This milestone does not add a Smelter picker.

Historical result: R4 passed against the kiln-only build. The new Smelter/Blast Furnace milestone intentionally adds an ore picker, so R4.13-R4.14 are no longer expected on that new build. Keep these recorded results; use S/B/X below for current-build testing instead.

#### Prepare the Smelter and kiln policy

Use an ordinary Smelter, not a Blast Furnace: this test uses Copper Ore. Selection, automatic processing and automatic fuel must be enabled.

- [x] **R4.1:** Verify those three settings are enabled.
- [x] **R4.2:** Verify the Smelter has at least three free ore spaces.
- [x] **R4.3:** Verify the Smelter contains zero Coal.
- [x] **R4.4:** Aim at the kiln input.
- [x] **R4.5:** Press F8.
- [x] **R4.6:** Set Wood to **Never auto-add**.
- [x] **R4.7:** Set Fine Wood to **Never auto-add**.
- [x] **R4.8:** Set Core Wood to **Never auto-add**.
- [x] **R4.9:** Click **Auto**.
- [x] **R4.10:** Walk to the Smelter's ore input.
- [x] **R4.11:** Aim at that ore input.
- [x] **R4.12:** Press F8.
- [x] **R4.13:** Verify no kiln picker opens.
- [x] **R4.14:** Verify no kiln Auto/Selected hint appears.

#### Check ore inventory priority

- [x] **R4.15:** Arrange your inventory to contain exactly 2 Copper Ore and no other accepted ores or Coal.
- [x] **R4.16:** Arrange eligible storage to contain exactly 3 Copper Ore and 3 Coal, with no other accepted ores.
- [x] **R4.17:** Close the chest.
- [x] **R4.18:** Aim at the Smelter's ore input.
- [x] **R4.19:** Tap E once.
- [x] **R4.20:** Verify inventory Copper Ore is now 1.
- [x] **R4.21:** Verify chest Copper Ore remains 3.
- [x] **R4.22:** Verify chest Coal remains 3.

#### Check ore storage fallback

- [x] **R4.23:** Move the remaining inventory Copper Ore outside all eligible nearby storage.
- [x] **R4.24:** Close any inventory/chest UI.
- [x] **R4.25:** Aim at the Smelter's ore input.
- [x] **R4.26:** Tap E once.
- [x] **R4.27:** Verify chest Copper Ore is now 2.
- [x] **R4.28:** Verify chest Coal remains 3.

#### Check separate fuel input from storage

Once Coal is supplied, normal processing can begin. Check source counts rather than expecting the displayed fuel/ore queue to stay constant.

- [x] **R4.29:** Verify your inventory contains no Coal or accepted ore.
- [x] **R4.30:** Close any inventory/chest UI.
- [x] **R4.31:** Aim at the Smelter's coal input.
- [x] **R4.32:** Tap E once.
- [x] **R4.33:** Verify an added-fuel message appears without a preceding missing-coal warning.
- [x] **R4.34:** Verify chest Coal is now 2.
- [x] **R4.35:** Verify chest Copper Ore remains 2.
- [x] **R4.36:** Verify your inventory still contains no accepted ore.

#### Check fuel inventory priority

- [x] **R4.37:** Obtain exactly 2 Coal in inventory without taking them from the test chest.
- [x] **R4.38:** Verify the test chest still contains 2 Coal.
- [x] **R4.39:** Close any inventory/chest UI.
- [x] **R4.40:** Verify the Smelter has room for another Coal.
- [x] **R4.41:** Aim at the Smelter's coal input.
- [x] **R4.42:** Tap E once.
- [x] **R4.43:** Verify inventory Coal is now 1.
- [x] **R4.44:** Verify chest Coal remains 2.
- [x] **R4.45:** Verify chest Copper Ore remains 2.

#### Restore kiln preferences

- [x] **R4.46:** Return to the kiln.
- [x] **R4.47:** Aim at its input.
- [x] **R4.48:** Press F8.
- [x] **R4.49:** Restore Wood's original allowed/excluded state.
- [x] **R4.50:** Restore Fine Wood's original allowed/excluded state.
- [x] **R4.51:** Restore Core Wood's original allowed/excluded state.
- [x] **R4.52:** Restore your recorded priority order.
- [x] **R4.53:** Click **Auto**.

Result/notes:

## Smelter and Blast Furnace milestone - local behavior checks passed

The following unchecked steps apply only after deploying the new processor-selection build, not the earlier kiln-only build. Historical checked tests above are not successes for this new build. Record failures using S/B/X step IDs. Setup actions and checks each have their own checkbox.

- Prepared/deployed: 2026-11-12, Valheim Mod Building, existing `Hoose-*` plugin folders.
- Source: `feature/input-selection`, working changes based on `5054a09` (not a new committed release).
- SupplyFromStorage DLL SHA-256: `85D3C9518B88C933DEE95EE548AF9BE6A161E9639C67733145F4E86CD1F13B13`.
- Shared DLL SHA-256: `8978C72BA782256E13F9D1A0035DBE8BDE9299F0D54C6AC9F9F1B107E0DAA4FA`.
- All three storage plugins were hash-verified against their build/dist outputs; all three installed common DLLs match.
- Existing config and kiln preferences were preserved byte-for-byte; selection/processing/fuel remain enabled and the key is F8.
- Automated validation: 189 tests / 462 assertions; three builds with zero errors and four existing assembly-conflict warnings each; Harmony signature checks (Supply 25, Cook 23, Craft 19).
- Live results reviewed 2026-11-12: S has 74/75 checked steps (only the S.4 empty-queue setup checkbox remains unchecked); B has 68/68, X has 57/57 and XB has 14/14. No failures recorded. All listed behavior checks passed by user checklist. No release ZIP was regenerated.

### Settings and test conditions

Use the approved Valheim Mod Building profile and a backed-up world/character. Close Valheim before config edits and restart after editing or DLL replacement. Keep these settings:

```ini
[Input selection]
Enabled = true

[Input selection preview]
Enabled = true
Open key = F8

[Automation]
Automatic processing inputs = true
Automatic fuel = true
```

The selection switch now covers the kiln, Smelter and Blast Furnace together. The old preview-only flag still covers only the kiln. Each processor type has separate local per-character preferences. Existing kiln preferences are retained; new processor priorities start unconfigured, using stable prefab-ID order until you rank them.

Use locally owned processors/chests with normal access, one closed test chest well within storage radius, and no competing automation. For S/B keep both processors unfueled so processing cannot change queue counts. Remove unintended accepted materials from other nearby eligible storage. The Smelter has seven distinct inputs on the inspected game data, including scrap; the Blast Furnace lists Black Metal Scrap, Flametal Ore and Petrified Tissue in F8. `GoldOre` is the internal prefab ID for Petrified Tissue, not an item displayed as Gold Ore. The normal current Flametal ore prefab ID is `FlametalOreNew`, not the legacy ore ID.

On 2026-11-12 the user confirmed these three Blast Furnace picker labels, then confirmed that Petrified Tissue spawns as `GoldOre` and can be added to the furnace. Exact Petrified Tissue inventory/storage selection and refinement output were not reported; there is no PT checklist in the current saved results. The B tests use Black Metal Scrap and Flametal Ore.

### S. Smelter ore selection

#### Setup and paging

- [x] **S.1:** Confirm the deployed build is the new Smelter/Blast Furnace milestone.
- [x] **S.2:** Launch Valheim Mod Building.
- [x] **S.3:** Load the test character/world.
- [ ] **S.4:** Choose an ordinary Smelter with an empty input queue.
- [x] **S.5:** Verify that Smelter contains zero Coal.
- [x] **S.6:** Remove all accepted Smelter ores/scrap from eligible nearby storage.
- [x] **S.7:** Arrange inventory to contain exactly 5 Copper Ore and 5 Tin Ore, with no other accepted Smelter materials.
- [x] **S.8:** Close inventory.
- [x] **S.9:** Aim at the Smelter's ore input.
- [x] **S.10:** Press F8.
- [x] **S.11:** Verify the picker title identifies the Smelter, not the kiln.
- [x] **S.12:** Verify Copper Ore and Tin Ore show inventory counts of 5 when their tiles are visible.
- [x] **S.13:** Click **Next page** until you have viewed all three pages.
- [x] **S.14:** Verify seven distinct input tiles appear across those pages, including unavailable types; no duplicate tiles.
- [x] **S.15:** Verify Coal is not among the input tiles.

#### Exact selection and Cancel

- [x] **S.16:** Navigate to the Tin Ore tile.
- [x] **S.17:** Click Tin Ore.
- [x] **S.18:** Click **Use highlighted**.
- [x] **S.19:** Release use controls.
- [x] **S.20:** Verify inventory is still 5 Copper Ore and 5 Tin Ore.
- [x] **S.21:** Close inventory without looking away from the ore input.
- [x] **S.22:** Tap E once.
- [x] **S.23:** Verify inventory is 5 Copper Ore and 4 Tin Ore.
- [x] **S.24:** Close inventory.
- [x] **S.25:** Press F8 while still aiming at the ore input.
- [x] **S.26:** Highlight Copper Ore, navigating pages if necessary.
- [x] **S.27:** Press Escape instead of confirming.
- [x] **S.28:** Tap E once.
- [x] **S.29:** Verify inventory is 5 Copper Ore and 3 Tin Ore.

#### Type priority across sources and within one type

- [x] **S.30:** Remove all accepted ore/scrap from inventory except 3 Tin Ore.
- [x] **S.31:** Place exactly 3 Copper Ore in the test chest without putting any other Smelter material there.
- [x] **S.32:** Close the chest.
- [x] **S.33:** Aim at the ore input.
- [x] **S.34:** Press F8.
- [x] **S.35:** Set Copper Ore to allowed for Auto.
- [x] **S.36:** Set Tin Ore to allowed for Auto.
- [x] **S.37:** Click Copper Ore's **Move to first priority**.
- [x] **S.38:** Click **Auto**.
- [x] **S.39:** Release use controls.
- [x] **S.40:** Tap E once.
- [x] **S.41:** Verify inventory Tin Ore remains 3.
- [x] **S.42:** Verify chest Copper Ore is now 2.
- [x] **S.43:** Obtain exactly 1 Copper Ore in inventory without taking it from the test chest.
- [x] **S.44:** Close any inventory/chest UI.
- [x] **S.45:** Aim at the ore input.
- [x] **S.46:** Tap E once.
- [x] **S.47:** Verify inventory Copper Ore is now zero.
- [x] **S.48:** Verify chest Copper Ore remains 2.

#### Held-type exhaustion

- [x] **S.49:** Verify the Smelter still has at least three free input spaces.
- [x] **S.50:** Verify inventory contains 3 Tin Ore and no Copper Ore or other accepted input.
- [x] **S.51:** Close any inventory/chest UI.
- [x] **S.52:** Aim at the ore input.
- [x] **S.53:** Hold E long enough for more than two additions.
- [x] **S.54:** Verify insertion stops after the two remaining chest Copper Ore are consumed.
- [x] **S.55:** Release E.
- [x] **S.56:** Verify inventory Tin Ore remains 3.
- [x] **S.57:** Verify chest Copper Ore is zero.
- [x] **S.58:** Close any inventory/chest UI.
- [x] **S.59:** Aim at the ore input.
- [x] **S.60:** Tap E once.
- [x] **S.61:** Verify inventory Tin Ore is now 2 (a fresh Auto press may choose another allowed type).

#### Exclusion and explicit override

- [x] **S.62:** Close inventory.
- [x] **S.63:** Press F8 while aiming at the ore input.
- [x] **S.64:** Set Tin Ore to **Never auto-add**.
- [x] **S.65:** Click **Auto**.
- [x] **S.66:** Release use controls.
- [x] **S.67:** Tap E once.
- [x] **S.68:** Verify inventory Tin Ore remains 2 and no input was added.
- [x] **S.69:** Close inventory.
- [x] **S.70:** Press F8 while aiming at the ore input.
- [x] **S.71:** Click Tin Ore.
- [x] **S.72:** Click **Use highlighted**.
- [x] **S.73:** Release use controls.
- [x] **S.74:** Tap E once.
- [x] **S.75:** Verify inventory Tin Ore is now 1.

Leave Copper Ore ranked first and Tin Ore excluded for the persistence test X. Do not clear preferences yet.

S result/notes:

### B. Blast Furnace input selection

#### Setup and accepted types

- [x] **B.1:** Choose a Blast Furnace with an empty input queue.
- [x] **B.2:** Verify it contains zero Coal.
- [x] **B.3:** Remove all accepted Blast Furnace inputs from eligible nearby storage.
- [x] **B.4:** Arrange inventory to contain exactly 5 Black Metal Scrap and 5 current Flametal ore, with no other accepted Blast Furnace inputs.
- [x] **B.5:** Close inventory.
- [x] **B.6:** Aim at the Blast Furnace material input.
- [x] **B.7:** Press F8.
- [x] **B.8:** Verify the picker title identifies the Blast Furnace.
- [x] **B.9:** Verify Black Metal Scrap, Flametal Ore and Petrified Tissue each appear once (Petrified Tissue remains editable even when unavailable).
- [x] **B.10:** Verify Coal, Copper Ore and Tin Ore do not appear in this picker.

#### Exact selection and cancellation

- [x] **B.11:** Click Flametal ore.
- [x] **B.12:** Click **Use highlighted**.
- [x] **B.13:** Release use controls.
- [x] **B.14:** Verify inventory is still 5 Black Metal Scrap and 5 Flametal ore.
- [x] **B.15:** Close inventory without looking away from the material input.
- [x] **B.16:** Tap E once.
- [x] **B.17:** Verify inventory is now 5 Black Metal Scrap and 4 Flametal ore.
- [x] **B.18:** Close inventory.
- [x] **B.19:** Press F8 while aiming at the input.
- [x] **B.20:** Highlight Black Metal Scrap.
- [x] **B.21:** Press Escape.
- [x] **B.22:** Tap E once.
- [x] **B.23:** Verify inventory is now 5 Black Metal Scrap and 3 Flametal ore.

#### Priority and source selection

- [x] **B.24:** Remove all accepted Blast Furnace inputs from inventory except 3 Flametal ore.
- [x] **B.25:** Place exactly 3 Black Metal Scrap in the test chest without adding any other Blast Furnace input.
- [x] **B.26:** Close the chest.
- [x] **B.27:** Aim at the material input.
- [x] **B.28:** Press F8.
- [x] **B.29:** Set Black Metal Scrap to allowed for Auto.
- [x] **B.30:** Set Flametal ore to allowed for Auto.
- [x] **B.31:** Click Black Metal Scrap's **Move to first priority**.
- [x] **B.32:** Click **Auto**.
- [x] **B.33:** Release use controls.
- [x] **B.34:** Tap E once.
- [x] **B.35:** Verify inventory Flametal ore remains 3.
- [x] **B.36:** Verify chest Black Metal Scrap is now 2.
- [x] **B.37:** Obtain exactly 1 Black Metal Scrap in inventory without taking it from the chest.
- [x] **B.38:** Close any inventory/chest UI.
- [x] **B.39:** Aim at the material input.
- [x] **B.40:** Tap E once.
- [x] **B.41:** Verify inventory Black Metal Scrap is now zero.
- [x] **B.42:** Verify chest Black Metal Scrap remains 2.

#### Held-type exhaustion and exclusions

- [x] **B.43:** Verify the furnace still has at least three free input spaces.
- [x] **B.44:** Close any inventory/chest UI.
- [x] **B.45:** Aim at the material input.
- [x] **B.46:** Hold E long enough for more than two additions.
- [x] **B.47:** Verify insertion stops after the two remaining chest Black Metal Scrap are consumed.
- [x] **B.48:** Release E.
- [x] **B.49:** Verify inventory Flametal ore remains 3.
- [x] **B.50:** Verify chest Black Metal Scrap is zero.
- [x] **B.51:** Close any inventory/chest UI.
- [x] **B.52:** Aim at the material input.
- [x] **B.53:** Tap E once.
- [x] **B.54:** Verify inventory Flametal ore is now 2.
- [x] **B.55:** Close inventory.
- [x] **B.56:** Press F8 while aiming at the input.
- [x] **B.57:** Set Flametal ore to **Never auto-add**.
- [x] **B.58:** Click **Auto**.
- [x] **B.59:** Release use controls.
- [x] **B.60:** Tap E once.
- [x] **B.61:** Verify inventory Flametal ore remains 2 and no input was added.
- [x] **B.62:** Close inventory.
- [x] **B.63:** Press F8 while aiming at the input.
- [x] **B.64:** Click Flametal ore.
- [x] **B.65:** Click **Use highlighted**.
- [x] **B.66:** Release use controls.
- [x] **B.67:** Tap E once.
- [x] **B.68:** Verify inventory Flametal ore is now 1.

Leave Black Metal Scrap first and Flametal ore excluded for X. Do not clear preferences yet.

B result/notes:

### X. Station isolation, coal, interruption and restart

Complete S and B first. Both processors should still be unfueled. Source-count checks below take precedence over queue/fuel readings once coal starts normal processing.

#### Separate coal inputs

- [x] **X.1:** Arrange inventory to contain no Coal.
- [x] **X.2:** Place exactly 3 Coal in the test chest.
- [x] **X.3:** Close the chest.
- [x] **X.4:** Aim at the Smelter's coal input.
- [x] **X.5:** Press F8.
- [x] **X.6:** Verify no material picker opens on the coal input.
- [x] **X.7:** Tap E once.
- [x] **X.8:** Verify added-fuel feedback appears without a missing-coal warning.
- [x] **X.9:** Verify chest Coal is now 2.
- [x] **X.10:** Close the chest.
- [x] **X.11:** Aim at the Blast Furnace's coal input.
- [x] **X.12:** Press F8.
- [x] **X.13:** Verify no material picker opens.
- [x] **X.14:** Tap E once.
- [x] **X.15:** Verify chest Coal is now 1.
- [x] **X.16:** Obtain exactly 2 Coal in inventory without taking it from the test chest.
- [x] **X.17:** Close any inventory/chest UI.
- [x] **X.18:** Aim at the Blast Furnace coal input.
- [x] **X.19:** Tap E once.
- [x] **X.20:** Verify inventory Coal is now 1.
- [x] **X.21:** Verify chest Coal remains 1.

#### Smelter fullness and held interruption

Use enough coal for normal processing to free a slot during the full-queue hold. Prepare extra coal before starting this subsection if necessary. F8 selection must be an available type for the insertion/interruption checks.

- [x] **X.22:** Fill the Smelter's input queue using explicit manual item-use of a known accepted stack.
- [x] **X.23:** Record that stack's remaining count.
- [x] **X.24:** Hold E on the full Smelter's ore input.
- [x] **X.25:** Verify the attempt consumes no input while full.
- [x] **X.26:** Keep holding E until normal processing frees a slot, without changing your aim away from the ore input.
- [x] **X.27:** Verify insertion does not restart before releasing E.
- [x] **X.28:** Release E.
- [x] **X.29:** At the Smelter with free space, arm an available type in F8.
- [x] **X.30:** Hold E to start adding that type.
- [x] **X.31:** Press F8 while still holding E.
- [x] **X.32:** Verify insertion stops while the picker is open.
- [x] **X.33:** Close F8 while still holding E.
- [x] **X.34:** Verify insertion stays stopped until release.
- [x] **X.35:** Release E.

#### Blast Furnace fullness and held interruption

- [x] **XB.1:** Fill the Blast Furnace's input queue using explicit manual item-use of a known accepted stack.
- [x] **XB.2:** Record that stack's remaining count.
- [x] **XB.3:** Hold E on the full Blast Furnace's material input.
- [x] **XB.4:** Verify the attempt consumes no input while full.
- [x] **XB.5:** Keep holding E until normal processing frees a slot.
- [x] **XB.6:** Verify insertion does not restart before releasing E.
- [x] **XB.7:** Release E.
- [x] **XB.8:** At the Blast Furnace with free space, arm an available type in F8.
- [x] **XB.9:** Hold E to start adding that type.
- [x] **XB.10:** Press F8 while still holding E.
- [x] **XB.11:** Verify insertion stops while the picker is open.
- [x] **XB.12:** Close F8 while still holding E.
- [x] **XB.13:** Verify insertion stays stopped until release.
- [x] **XB.14:** Release E.

#### Cross-target hold safety

- [x] **X.36:** While holding E at one processor, aim at the other processor's material input.
- [x] **X.37:** Verify the second processor does not start inserting during that same hold.
- [x] **X.38:** Release E.

#### Preference isolation and persistence

- [x] **X.39:** Open F8 at the kiln input.
- [x] **X.40:** Verify the kiln's pre-milestone priorities/exclusions are retained, without ore entries.
- [x] **X.41:** Cancel the picker.
- [x] **X.42:** Open F8 at the Smelter ore input.
- [x] **X.43:** Verify Copper Ore is still first and Tin Ore still excluded.
- [x] **X.44:** Cancel the picker.
- [x] **X.45:** Open F8 at the Blast Furnace material input.
- [x] **X.46:** Verify Black Metal Scrap is still first and Flametal ore still excluded.
- [x] **X.47:** Cancel the picker.
- [x] **X.48:** Quit Valheim fully.
- [x] **X.49:** Restart the profile and load the same character/world.
- [x] **X.50:** Open F8 at the Smelter input.
- [x] **X.51:** Verify Copper Ore/Tin Ore preferences persisted, with temporary selection reset to Auto.
- [x] **X.52:** Cancel the picker.
- [x] **X.53:** Open F8 at the Blast Furnace input.
- [x] **X.54:** Verify Black Metal Scrap/Flametal ore preferences persisted, with temporary selection reset to Auto.
- [x] **X.55:** Cancel the picker.
- [x] **X.56:** Load another character.
- [x] **X.57:** Verify that character has separate default preferences at both processors.

X result/notes:

### Broader follow-up coverage

- [ ] Test ownership/access changes and simultaneous users in multiplayer; verify inventories and station state after save/reload.
- [ ] Test controller navigation, confirmation, cancellation and release guarding. Controller opening has no dedicated binding yet.
- [ ] Check the hint and picker at other resolutions/UI scales and with other menus.
- [ ] Test death/teleport/disconnect cleanup and abnormal configuration/save failures in a disposable setup.

## Battering Ram milestone - live checklist passed

These BR steps apply to the new ram-selection build, not the earlier furnace-only build. The default at that checkpoint was unranked Auto with no exclusions; fresh entries now rank Wood, Finewood, Corewood, Ashwood (2026-11-13). Preferences are separate from the kiln and furnaces. Existing enabled selection also enables the ram engine.

- Initial ram build deployed: 2026-11-12, Valheim Mod Building, existing `Hoose-SupplyFromStorage` folder.
- Source: `feature/input-selection`, working changes based on `5054a09`.
- Ram selection build Supply DLL SHA-256: `EF5B7C1D2FB4D3A0EE155A8848C07D8BC2CFB8DE5A827DACF076F523A70AD05A`.
- Unchanged Common DLL SHA-256: `8978C72BA782256E13F9D1A0035DBE8BDE9299F0D54C6AC9F9F1B107E0DAA4FA`; all three installed common copies match.
- User-reported live results: BR.1-BR.82 all passed on 2026-11-13; no behavior-test failures.
- The user's only note was an unavailable-input message displaying the prefab ID `FineWood` instead of the game item name `Finewood`.
- Corrected build Supply DLL SHA-256: `D59C7B4724D1F718E88AE4EEED95C6EAF4B91845198E8733CB03BCD274D62C3C`.
- Supply tests after correction: 99 tests / 301 assertions passed; build zero errors with four existing assembly-conflict warnings; 25 Harmony signature checks passed.
- The message correction was deployed 2026-11-13 after backing up the previous DLL/config. Config was preserved byte-for-byte. The user subsequently confirmed the `Finewood` wording looks good.
- No release ZIP regenerated.

The engine accepts Wood (`Wood`), Fine Wood (`FineWood`), Core Wood (`RoundLog`) and Ashwood (`Blackwood`). It uses the processing-input path as engine fuel, with a nominal capacity of 25 and no produced item. The engine can consume loaded wood over time: use inventory/chest counts for exact insertion assertions, not a queue count that may decrease. Use an intact, locally owned ram, backed-up character/world, a closed test chest well within storage radius and no other automation/users. Keep the ram stationary unless a step explicitly says otherwise. Do not expect refined output.

### BR-A. Setup, targeting and four input types

- [x] **BR.1:** Confirm deployment of the new Battering Ram selection build to Valheim Mod Building.
- [x] **BR.2:** Launch that profile and load the backed-up test character/world.
- [x] **BR.3:** Verify `[Input selection] Enabled`, `[Input selection preview] Enabled`, `[Automation] Automatic processing inputs` and `[Automation] Automatic fuel` are all true.
- [x] **BR.4:** Verify the picker open key is F8.
- [x] **BR.5:** Choose an intact Battering Ram with an empty engine input.
- [x] **BR.6:** Remove all four accepted wood types from eligible nearby storage.
- [x] **BR.7:** Arrange inventory to contain exactly 5 Wood, 5 Fine Wood, 5 Core Wood and 5 Ashwood.
- [x] **BR.8:** Close inventory and aim at the engine-loading interaction at the back of the ram.
- [x] **BR.9:** Press F8.
- [x] **BR.10:** Verify the title identifies the Battering Ram, not the Charcoal Kiln.
- [x] **BR.11:** Verify all four wood types appear once, using Next page if necessary.
- [x] **BR.12:** Verify each wood type displays inventory 5 and storage 0.
- [x] **BR.13:** Verify no Coal or furnace ores appear.
- [x] **BR.14:** Verify all four types are initially allowed with unconfigured priorities.
- [x] **BR.15:** Close with Escape and verify no wood was consumed.
- [x] **BR.16:** Aim at the ram's driving/operating interaction rather than the engine-loading input and press F8.
- [x] **BR.17:** Verify no input picker opens there.

### BR-B. Exact selection, cancellation and manual item-use

- [x] **BR.18:** Aim at the engine-loading input and open F8.
- [x] **BR.19:** Click Ashwood and then **Use highlighted**.
- [x] **BR.20:** Release use controls and verify all four inventory counts remain 5.
- [x] **BR.21:** Close inventory, aim at the same input and tap E once.
- [x] **BR.22:** Verify Ashwood decreases to 4 and the other three wood counts remain 5.
- [x] **BR.23:** Reopen F8 at that input, highlight Fine Wood and cancel with Escape.
- [x] **BR.24:** Release use controls and tap E once at the same input.
- [x] **BR.25:** Verify Ashwood decreases to 3 and Fine Wood remains 5.
- [x] **BR.26:** Assign Core Wood to a free hotbar slot.
- [x] **BR.27:** Close inventory and press that hotbar key once while aiming at the loading input.
- [x] **BR.28:** Verify Core Wood decreases to 4 and Ashwood remains 3.

### BR-C. Priority across sources and inventory-first within a type

- [x] **BR.29:** Remove all accepted wood from inventory except exactly 3 Fine Wood.
- [x] **BR.30:** Place exactly 3 Wood in the test chest with no other accepted wood there.
- [x] **BR.31:** Close the chest and open F8 at the ram input.
- [x] **BR.32:** Set Wood and Fine Wood to allowed, then move Wood to first priority.
- [x] **BR.33:** Click **Auto**, release use controls and tap E once at the input.
- [x] **BR.34:** Verify chest Wood decreases to 2 while inventory Fine Wood remains 3.
- [x] **BR.35:** Obtain exactly 1 Wood in inventory without taking it from the test chest.
- [x] **BR.36:** Close inventory, aim at the input and tap E once.
- [x] **BR.37:** Verify inventory Wood is zero and chest Wood remains 2.

### BR-D. Held exhaustion, exclusions and explicit override

- [x] **BR.38:** Verify the engine has at least three free input spaces.
- [x] **BR.39:** Close all inventory/chest UI and hold E at the loading input long enough for more than two additions.
- [x] **BR.40:** Verify chest Wood becomes zero and insertion stops without consuming inventory Fine Wood.
- [x] **BR.41:** Keep holding E for two more seconds and verify Fine Wood remains 3.
- [x] **BR.42:** Release E and tap E once again.
- [x] **BR.43:** Verify inventory Fine Wood decreases to 2.
- [x] **BR.44:** Open F8 at the ram and set Fine Wood to **Never auto-add**.
- [x] **BR.45:** Click **Auto**, release use controls and tap E once.
- [x] **BR.46:** Verify Fine Wood remains 2 and no allowed-input availability is reported.
- [x] **BR.47:** Open F8, explicitly choose Fine Wood and click **Use highlighted**.
- [x] **BR.48:** Release use controls and tap E once.
- [x] **BR.49:** Verify Fine Wood decreases to 1 despite its Auto exclusion.
- [x] **BR.50:** Tap E once more, then record the now-zero Fine Wood count.
- [x] **BR.51:** Obtain exactly 2 Wood in inventory without leaving any Fine Wood in accessible storage.
- [x] **BR.52:** Close inventory and tap E at the same loading input without changing the explicit choice.
- [x] **BR.53:** Verify Wood remains 2; exhausted explicit Fine Wood must not silently substitute Wood.

### BR-E. Picker interruption, target changes and capacity

- [x] **BR.54:** Prepare at least 30 Wood in inventory and ensure the engine has free input space.
- [x] **BR.55:** Explicitly select Wood in F8 and release use controls.
- [x] **BR.56:** Hold E to begin loading, then press F8 while still holding E.
- [x] **BR.57:** Verify inventory Wood stops decreasing while the picker is open.
- [x] **BR.58:** Close F8 while still holding E and record inventory Wood.
- [x] **BR.59:** Keep holding E for two more seconds and verify that count does not decrease.
- [x] **BR.60:** Release E, then hold E again until the engine reports full.
- [x] **BR.61:** Record inventory Wood at the first full indication and keep holding E until the engine consumes fuel and frees space.
- [x] **BR.62:** Verify loading does not restart and the recorded inventory count remains unchanged until E is released.
- [x] **BR.63:** Release E and tap E after space is available; verify exactly one Wood is consumed.
- [x] **BR.64:** With space available, begin holding E at the ram and move aim to the nearby kiln's input without releasing E.
- [x] **BR.65:** Verify the kiln does not insert anything during that same hold.
- [x] **BR.66:** Release E, then open F8 at the ram and verify temporary selection reset to Auto.

### BR-F. Preference isolation, restart and disabled-mode regression

- [x] **BR.67:** Verify the ram still has Wood first and Fine Wood excluded.
- [x] **BR.68:** Open F8 at the kiln and verify its earlier preferences are unchanged, without Ashwood added by the ram.
- [x] **BR.69:** Open F8 at the Smelter and verify Copper Ore first and Tin Ore excluded are unchanged.
- [x] **BR.70:** Open F8 at the Blast Furnace and verify Black Metal Scrap first and Flametal Ore excluded are unchanged.
- [x] **BR.71:** Quit Valheim fully and restart the same profile with the same character.
- [x] **BR.72:** Open F8 at the ram and verify Wood first, Fine Wood excluded and temporary selection reset to Auto.
- [x] **BR.73:** Load another character, open the ram picker and verify separate default preferences.
- [x] **BR.74:** Quit fully, set `[Input selection] Enabled = false` and restart with the original character.
- [x] **BR.75:** Aim at the ram loading input and press F8; verify no ram picker opens even with preview enabled.
- [x] **BR.76:** Ensure free engine space, remove accepted wood from inventory and place exactly 3 Wood in the test chest.
- [x] **BR.77:** Close the chest, aim at the input and tap E once.
- [x] **BR.78:** Verify legacy storage supply still consumes exactly one chest Wood.
- [x] **BR.79:** Quit fully, set `[Input selection] Enabled = true`, set `[Automation] Automatic processing inputs = false`, and restart.
- [x] **BR.80:** With no accepted wood in inventory, tap E at the ram input and verify chest Wood remains 2.
- [x] **BR.81:** Quit fully, restore automatic processing enabled and restart.
- [x] **BR.82:** Open F8 at the ram and verify its saved Wood/Fine Wood preferences were retained.

BR result/notes:

- Completed: 2026-11-13; user reports all BR.1-BR.82 passed on the deployed Battering Ram selection build. No behavior-test failures reported.
- The only note was an on-screen unavailable-input message displaying the internal ID `FineWood` instead of the game item name `Finewood`.
- Fixed by localizing the conversion's item-name token before formatting unavailable-input feedback. Added a production-linked regression test; all 99 Supply tests / 301 assertions pass.
- The message correction was built and deployed after the live checklist run. Restart Valheim Mod Building before use; the corrected on-screen wording has not yet been rechecked in game.

- Date/build/profile:
- Passed steps:
- Failed step and exact actions:
- Inventory/chest counts and engine state:
- Unexpected messages or log errors: casing/spelling is showing up in on screen messages as `FineWood`, should be `Finewood`.

## Discovery-filter checkpoint - pending

Added 2026-11-16 after the passed Supply/Cook selection checkpoints. This extension has automated regression and build/signature evidence only, not a live pass. Use a backed-up world and characters with different discovery histories; do not reset existing preferences. Keep selection and the relevant automation enabled in both mods. Repeat applicable steps at the kiln, Smelter, Blast Furnace, ram engine, wooden/iron cooking racks, oven ingredient switch and Fermenter.

Partial report on 2026-11-16: a new character's cooking stations opened the empty-discovery dialog. Supply dialogs did not open because the profile's selection and preview settings were both disabled. Supply's fresh-config default was corrected to true and this profile's selection explicitly enabled, preserving other settings. Retest D.1 for Supply; the complete checkpoint remains pending.

- [ ] D.1: With a character that has not discovered all accepted inputs, open F8. Only personally discovered types appear, including zero-count types. No hidden tiles, missing rank numbers, placeholders or hidden-item count hint at undiscovered inputs. A station with one known input still exposes its picker; with none, it shows the generic no-discovered-inputs message.
- [ ] D.2: Have another character put an undiscovered accepted input in accessible, locally owned storage. It must not appear or affect visible counts. Put a lower-priority discovered input in another eligible source; Auto must choose the discovered type. With only undiscovered types available, E must consume nothing and must not fall through to vanilla.
- [ ] D.3: Pick up the previously unknown type to discover it normally. Reopen/refresh F8: it appears in the station's normal display order, with accurate counts and the existing saved rank/exclusion, not a reset policy. Auto can use it after release/repress, unless excluded. An explicit held selection still stops on exhaustion instead of substituting.
- [ ] D.4: Edit Up/Down and exclusions while some types are hidden. Visible ranks remain consecutive; restart and discover the hidden types. Their saved settings remain intact. Another character sees only that character's discoveries and preferences.
- [ ] D.5: Confirm selecting a known excluded type and ordinary explicit item use still work. Cooked output/finished-mead collection, running batches, fuel slots, food recipes, single-input stations and Foundry casts remain unchanged.
- [ ] D.6: Disable selection (or its automation) and verify the prior legacy/vanilla behavior remains. With Supply's non-consuming kiln preview enabled, the preview still hides undiscovered types without changing E behavior.

## Continuous held-input checkpoint - pending

Added 2026-11-18 after the user asked for vanilla-like held E; extended the same day to CookFromStorage at the user's request. Automated tests pass; not yet tested in game. Repeat H.1-H.6 at the Stone Oven ingredient switch (racks and Fermenters keep vanilla's single-press behavior).

- [ ] H.1: In Auto at the Smelter ore input, hold E and swing aim off the input (sky/ground or a chest) and back several times. Insertion pauses while away and resumes on return; no "Release use" message appears.
- [ ] H.2: With e.g. 2 Copper Ore and several Tin Ore available (default priority), keep holding E. After the copper is gone, insertion continues with the next available priority (Copper Scrap, else Tin Ore) without releasing. Excluded and undiscovered types are never used.
- [ ] H.3: Explicitly select a type in F8, hold E, swing away and back: the explicit type continues. When it runs out, insertion stops with the "No ... available" message and no other type is consumed.
- [ ] H.4: Release E while aiming away from the input after an explicit selection; aim back and verify the selection reset to Auto.
- [ ] H.5: While holding E at one processor, aim at another supported processor's input. It continues loading there using that station's Auto choice; the first station's explicit selection does not carry over.
- [ ] H.6: Fill a station while holding: the game's full message appears once and insertion stops until release. Opening inventory/F8 while holding silently stops it (no message); release and press E to resume. Opening a chest with E shows no station message.
- [ ] H.7: With no accepted item in inventory or nearby storage, press E at a supported station. Expected: vanilla's normal no-items message (Stone Oven/racks: the vanilla no-cookable message). With accepted items present but all set to **Never auto-add**, expected: `Items set to Never auto-add (F8)`.

## Future-run notes

- Date/build/profile:
- Passed steps:
- Failed step and exact actions:
- Inventory/chest counts before and after:
- Station state and relevant log errors: