# BuildFromStorage in-game test checklist

Use a disposable world and character. Tests consume materials and may expose multiplayer synchronization issues; back up the world and do not use valuable items. Check each box only after completing the step and confirming its expected result. If a check fails, record the result and continue only if it is safe.

The plugin has passed automated stub tests and patch-signature checks. On 2026-11-24 the user reported testing complete except for multiplayer; checked items below record the user's test progress. Multiplayer scenarios remain untested.

## Preparation

- [x] Back up the test world and select a disposable character.
- [x] In r2modman, select the `Valheim Mod Building` profile. The development build has been deployed to its `BepInEx\plugins\BuildFromStorage` folder.
- [x] For the initial tests, disable other storage mods in the profile. Keep BepInEx enabled.
- [x] Start Valheim through the profile. If it was already running when the mod was deployed, exit and restart it.
- [x] Open the profile's `BepInEx\LogOutput.log` and find `BuildFromStorage 1.0.0 loaded`. Check for BuildFromStorage errors. If patches were not registered, stop testing and save the log.
- [x] Enter the test world. Disable free-build and no-placement-cost modes, and ensure the character has unlocked a Hammer and the pieces used below.
- [x] Set up a workbench, clear ground, and two chests within 20 m of your character at clearly different distances. Call them **near** and **far**.
- [x] Open and close both chests yourself. The mod only uses accessible, closed chests currently owned by your client. Opening a chest may transfer ownership, but this has not been verified in game.
- [x] Choose an unlocked Hammer piece requiring only wood, with a displayed cost of at least 2 wood. Write down its actual cost as **K**: __________.
- [x] Remove wood from other nearby storage, and keep the two test chests within range unless a step says otherwise. Avoid picking up extra wood during a test.
- [x] Before each test, reset inventory and chest contents as specified; record the starting and ending counts. After inspecting a chest, close it before attempting a build.

## 1. Inventory-only baseline

- [x] Put K wood in your inventory and extra wood in the near chest. Record both counts and close the chest.
- [x] Place the chosen piece in a valid location within the required workbench range.
- [x] Confirm exactly one piece was placed, your inventory lost K wood, and the chest was unchanged.

## 2. Storage-only construction

- [x] Put zero wood in inventory, K+1 wood in the near chest, and no wood in the far chest. Close both chests.
- [x] Select the piece and check that its availability reflects sufficient materials. Place it once in a valid location.
- [x] Confirm exactly one piece was placed, inventory remains at zero, and the near chest contains exactly 1 wood.
- [x] Reopen and close the near chest successfully; it should not remain stuck in use.

## 3. Inventory-first and nearest-first

- [x] Put K-1 wood in inventory, 1 in the near chest, and K in the far chest. Close both chests.
- [x] Place the piece once. Confirm inventory and near chest reach zero, and the far chest still contains K wood.
- [x] Reset to zero wood in inventory, K-1 in the near chest, and K in the far chest. Close both chests.
- [x] Place the piece once. Confirm the near chest reaches zero and the far chest contains K-1 wood.

## 4. Insufficient materials

- [x] Put K-1 wood total across inventory and the two chests. Record each count and close the chests.
- [x] Attempt to place the piece. Confirm no piece is placed and all counts are unchanged.
- [x] Add 1 wood to an eligible chest, close it, and try again. Confirm one piece is placed and exactly K wood is consumed in total.

## 5. Multiple material types

- [x] Choose an unlocked piece requiring at least two different materials. Record its displayed cost for each material.
- [x] Put part of each cost in inventory and the remainder in eligible chests. Record all counts and close the chests.
- [x] Place the piece once. Confirm exactly the displayed amount of each material was consumed, using inventory before storage.
- [x] Reset the materials, but remove one required material entirely from inventory and all nearby chests. Record counts and close the chests.
- [x] Attempt placement. Confirm no piece is placed and no materials are consumed.

## 6. Vanilla placement restrictions

- [x] Put enough materials for a piece that requires a workbench into eligible storage; keep the corresponding inventory amounts at zero.
- [x] Stand within storage range but outside the required workbench's range and attempt placement. Confirm it is rejected and no materials are consumed.
- [x] Return within workbench range, aim at a location where vanilla prevents placement, and attempt placement. Confirm no piece is placed and no materials are consumed.
- [x] Move to a valid location and place the piece. Confirm it succeeds and consumes the displayed cost.

## 7. Configured range

- [x] Exit Valheim. Open the profile's `BepInEx\config\hoose.valheim.buildfromstorage.cfg` and set `Storage radius = 5` under `[Storage]`.
- [x] Relaunch the profile and put K wood only in an eligible, closed chest. Stand clearly more than 5 m from the chest while remaining in workbench range.
- [x] Attempt to place the piece. Confirm the chest's wood does not satisfy the cost and nothing is consumed.
- [x] Move clearly within 5 m of the chest and attempt placement again. Confirm the piece is placed and K wood is consumed.
- [x] If practical, repeat with the chest vertically separated from the player: the range is a sphere centered on the player, not a horizontal circle.
- [x] Exit the game, restore `Storage radius = 20`, and relaunch before continuing.

## 8. Chest creation and destruction

- [x] Remove wood from inventory and existing nearby chests. Build a new chest, put K wood in it, then close it.
- [x] Place the chosen piece. Confirm the new chest supplies K wood and can be reopened afterward.
- [x] Empty and dismantle the new chest. Remove any returned wood from inventory and the test area, then try to place the piece again with no wood in inventory or other nearby chests.
- [x] Confirm placement is rejected and the log contains no errors about the destroyed chest.

## 9. Free-build mode

- [x] In the disposable world, put wood in an eligible chest and record the count. Close the chest and enable the game's free-build/no-placement-cost mode.
- [x] Place the piece. Confirm it is placed and the chest's wood count is unchanged.
- [x] Turn free-build/no-placement-cost mode off before continuing.

## 10. Save and reload

- [x] Place a piece using chest materials and record the remaining chest counts and the location of the piece.
- [x] Save and exit normally, then reload the same world.
- [x] Confirm the piece remains placed and the chest contents match the recorded counts without duplication or restoration.

## 11. Multiplayer ownership and busy storage

Use a second player and a disposable shared world. Both players need the plugin for their own storage-backed building.

- [ ] Put exactly K wood in one shared chest, with no other usable wood nearby or in your inventory.
- [ ] Have the second player open the chest and keep it open while you attempt to build. Confirm the busy chest is not used and no materials are consumed.
- [ ] Have the second player close the chest. Try building and record whether it succeeds and the resulting counts.
- [ ] Reset the chest to K wood. Open and close it yourself, then attempt the build. Record whether it succeeds and the resulting counts.
- [ ] Check that no attempt duplicated or unexpectedly lost materials. Opening a chest does not guarantee local ownership; record any unexpected inability to use it and retain the log.

## 12. Multiplayer permissions

- [ ] Set up a personal chest or ward that denies your character access. Put the only K wood available inside it.
- [ ] Attempt to build. Confirm the protected storage is not used and its contents remain unchanged.
- [ ] Restore access, open and close the chest yourself, and retry. Record whether it can now supply the piece; it must also be closed, in range, and locally owned.

## 13. Competing players

Local reservations are not a proven network-wide lock; perform this test only with disposable materials.

- [ ] Give two players no wood and put exactly K wood in one shared chest. Record the initial counts and ensure both players are positioned to build.
- [ ] Have both players attempt to place the chosen piece nearly simultaneously. Record how many pieces were placed and each player's and the chest's final counts.
- [ ] Save and reload the world. Check the piece and material counts again.
- [ ] Confirm there was no duplication, negative count, or unexplained material loss. Treat any anomaly as a failure and retain both players' logs.

## 14. Companion-mod compatibility

- [x] Enable the companion storage mods in the test profile and restart Valheim.
- [x] Repeat tests 2-5. Confirm Hammer costs are consumed exactly once, with the expected inventory-first and nearest-first behavior.
- [x] Craft a normal station recipe and use a processing machine with the relevant companion mods. Confirm they retain their expected behavior and do not consume materials twice.

## Results

- [ ] Record any failed test number, game and mod versions, single-player or server, chest distances, and initial/final counts.
- [ ] Save screenshots and relevant `BepInEx\LogOutput.log` entries for failures.
- [ ] Restore the radius and any profile/world settings changed for testing.
- [ ] Report the results, including tests not performed and any unexpected ownership, persistence, or multiplayer behavior.