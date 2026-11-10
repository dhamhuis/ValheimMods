# Station input selection and picker UI

Status: Verified - installed metadata/bounded IL, compilation and production-linked logic tests; user-reported live success applies only to the initial non-consuming preview.
Last verified: 2026-11-09.
Evidence: [Picker and policy](../../ValheimMods.Common/src/InputSelection/); [kiln adapter](../../SupplyFromStorage/src/KilnInputPreview.cs); [controlled requests](../../SupplyFromStorage/src/KilnInputSelection.cs); [policy/transfer tests](../../SupplyFromStorage/tests/Behavior/InputSelectionTests.cs); [preview helper tests](../../SupplyFromStorage/tests/Behavior/InputPreviewTests.cs); [Harmony integration tests](../../SupplyFromStorage/tests/Integration/PatchIntegrationTests.cs). Targeted Mono.Cecil inspection used the fingerprints below.
Limitations: The three reported live checks cover only the non-consuming preview. The new opt-in consuming selection, preference persistence, controller-specific behavior, resolutions and multiplayer are not live-verified. Stub tests model our policy, not game networking or disk persistence.

## Use when

Implementing item-type preferences, an input picker, target discovery or held-interaction safety in CookFromStorage and SupplyFromStorage. Read [processing](Processing.md) before changing any insertion path and [storage](Storage.md) before counting or mutating containers.

## API and behavior

### Inspected assemblies

| Assembly | SHA-256 |
| --- | --- |
| `assembly_valheim.dll` | `9F959C0396EC4240D47FCBE5D0D062C428CC627244506DB5EA567F9D77B2B769` |
| `assembly_utils.dll` | `843946B3DF81FB0A3060A6EBEDD34BCACF71CF62B78B74F0DA4653872BAF25F9` |
| `assembly_guiutils.dll` | `08A5AEA10B3E8BF20B60B3AB545B6CB8550DB8B12CFF05E32C90BDD9F45DC244` |
| `UnityEngine.CoreModule.dll` | `FBA3821AFB6867FE4D471DC50BF04BA21E6982B33AB6B5336B08A5C55119D990` |
| `UnityEngine.IMGUIModule.dll` | `6D79759136956F7E01F07E850905F9A8869CFD6987672CB85310AF35E510E109` |

### Targeting and input gates

- Public `GameObject Player.GetHoverObject()` returns `m_hovering`. Bounded inspection of private `Player.Interact(GameObject go, bool hold, bool alt)` shows it resolves `go.GetComponentInParent<Interactable>()`; held calls have a 0.2-second player-side interval before the station's own handling.
- Protected `bool Player.TakeInput()` checks major game UIs, death, cutscene and teleportation. `Player.Update` obtains this result before its normal interaction branch, but still calls `UpdateHover`. `UpdateHover` refreshes the hover ray unless dead, in placement mode or controlling a doodad. The prototype only opens when the original input result is true and also excludes unified popups, radial UI and piece selection.
- Private `bool PlayerController.TakeInput(bool look = false)` is a separate movement/look gate. Blocking `Player.TakeInput` alone is not sufficient. The prototype postfixes both gates, scoped to the local player.
- Public `void GameCamera.UpdateMouseCapture()` controls cursor state through `ZCursor`. Its inspected body checks native UI surfaces but does not know about custom pickers. The prototype prefixes it while open, and calls the vanilla method after closing to restore the game's desired capture.
- Private `void Menu.Update()` independently handles Escape, `JoyMenu` and `JoyButtonB`. The prototype suppresses that method while its input guard is active so cancellation does not also open the pause menu. This requires live ordering checks.

`ZInput` and `ZCursor` are in `assembly_utils.dll`, not `assembly_valheim.dll`. Public static APIs used here are `bool GetKeyDown(KeyCode key, bool logWarning = true)`, `bool GetKey(KeyCode key, bool logWarning = true)`, `bool GetButton(string name)`, `bool GetButtonDown(string name)`, `CursorLockMode ZCursor.LockState { get; set; }`, `ZCursor.Show()` and `ZCursor.Hide()`. Key reads route through the game's Input System controls rather than Unity's legacy `Input` API.

Bounded inspection of keyboard/gamepad button registration verified `Use`, `Attack`, `SecondaryAttack`, `JoyUse`, `JoyAttack`, `JoySecondaryAttack`, `JoyButtonB`, `JoyMenu` and horizontal `JoyDPad` bindings used by the prototype. F8 is only a configurable prototype default, not a verified conflict-free binding across the game and other mods. A search of `ZInput` alone cannot establish that.

### Display and current prototype

Public `Sprite ItemDrop.ItemData.GetIcon()` is in the game assembly. `Localization.instance` and `Localize(string text)` are in `assembly_guiutils.dll`; exact top-level searches in the game and utility assemblies did not find `Localization`. Publicized game references are still required for private station fields.

The preview uses Unity IMGUI with item sprite texture rectangles, localized item/station names, screen-relative scaling and a three-column kiln grid. Its explanatory text is English-only at this prototype stage. IMGUI and TextRendering are explicit non-distributed compile references of the common library.

SupplyFromStorage lazily registers the shared preview only when its new preview setting is enabled. Disabling/unloading disposes its four patches and restores cursor handling. The adapter verifies the hovered switch equals the kiln's `m_addOreSwitch`, checks local ownership/access, reads current accepted conversions and counts exact prefab identities in inventory and eligible storage. The snapshot refresh interval is 0.25 seconds; it does not reserve anything.

The release guard blocks gameplay while open and through the closing frame, then waits for use/attack/cancel controls to be released. The opening frame cannot also process the opener as a close request. These are tested mod-side state-machine rules, not a live input-system guarantee.

### User-reported local preview check

On 2026-11-09, after deployment to the approved mod-manager profile and enabling the preview, the user reported, "Tests 1,2,3 work as expected". The supplied checklist was: (1) aim at the Charcoal Kiln input and press F8; (2) check icons, inventory/chest counts and highlighting; (3) close with Escape and verify controls resume without accidental insertion or attacks. This confirms those local preview scenarios by user report and clears the initial UI checkpoint for subsequent implementation. No controller-specific steps, resolution, multiplayer scenarios, actual selected-type insertion or preference persistence were reported.

### Consuming kiln milestone: inspected game behavior

On 2026-11-09, bounded IL inspection reconfirmed `Smelter.OnAddOre` on the game fingerprint above. With a non-null item, it checks `item.m_dropPrefab.name` against `IsItemAllowed(string)`, checks capacity, removes that exact inventory item, sends `RPC_AddOre` with the prefab ID and `m_cheated`, then updates private `m_addedOreTime` and conditionally calls private `SetAnimation(bool active)` when public `m_addOreAnimationDuration` is positive. Calling the whole vanilla method is not a transaction-confirmation mechanism: it ignores `RemoveItem`'s result and returns after sending the RPC.

`RPC_AddOre(long sender, string name, bool cheated)` checks local ownership and accepted type, queues the input and then runs effects. The inspected body does not itself enforce capacity. A callback can therefore fail after committing; the mod checks for queue growth before deciding whether to restore a removed item. It performs its own capacity/access checks immediately before mutation and preserves the cheated flag and insertion-animation fields. This is bounded body evidence, not proof of multiplayer atomicity or effect ordering with other mods.

`Switch.Interact(Humanoid user, bool hold, bool alt)` rejects held requests when `m_holdRepeatInterval <= 0`, otherwise checks its own repeat deadline before invoking `m_onUse(this, user, null)`. The selection feature does not schedule interactions or bypass that gate.

`Smelter.FindCookableItem(Inventory inventory)` iterates conversions and calls `Inventory.GetItem(string name, int quality = -1, bool isPrefabName = false)` with the conversion's shared name, `-1`, `false`. That boolean is a prefab-name selector, not a world-level flag; do not infer `HaveItem`/`CountItems`'s different boolean semantics here.

Public `long Player.GetPlayerID()` returns zero for an invalid player network view; otherwise it reads `ZDOVars.s_playerID` from the player's ZDO. The mod refuses to initialize usable preferences for zero and keys local BepInEx entries by this ID and station type. Stability across character/profile migration and actual save/reload behavior still require live checks.

The installed profile's `BepInEx.dll` SHA-256 is `18CAFFD2C09DC4E6A51FC13B4AF7FD2389251F2EC8ECDF8BF076B84A14FB991E`. Bounded inspection of public `ConfigFile.Save()` showed direct filesystem/stream writes with finally handlers and no catch handlers; the mod catches I/O/access errors around binding and saving. This does not prove atomic disk replacement, crash recovery or live persistence.

### Implemented mod policy, not live-certified behavior

The separate `Input selection/Enabled` opt-in is effective only while automatic processing is enabled. The old preview flag alone remains non-consuming. Controlled kiln requests run before the legacy inventory shortcut and suppress vanilla even when blocked; explicit non-null item-use, other stations, fuel and recipe crafting remain untouched.

`InputPreferences` uses exact, case-sensitive prefab IDs, ranked types first and ordinal unranked ordering. Unknown IDs survive editing/serialization. An exclusion blocks automatic inventory and storage selection, but an explicit picker choice overrides it. Type priority applies across both sources, with inventory preferred within the chosen type. Malformed preferences have no permissive fallback. Configuration binding/save I/O failures log/notify and block controlled insertion.

The local per-character/per-station-type entry encodes `priority|exclusions`; defaults are `Wood,FineWood,RoundLog|`. Preference edits save immediately. Cancel leaves the armed selection unchanged, not undoing preference edits. Empty types remain editable; modded extra conversions get pages and an unconfigured label. The picker only arms a type; native E callbacks perform transfers.

`InputTypeSession` latches the type across repeated native callbacks and inventory-to-storage transitions. Exhaustion/fullness/failure stops that hold without substitution; release/repress permits a new Auto choice. Target/access changes or input interruption stop held use; target changes/player lifecycle clear the ephemeral explicit selection. Observation uses physical Use/JoyUse state once per frame in the local player's input gate. Because `Player.UpdateHover` follows `TakeInput`, crossing targets on the press frame may conservatively require releasing/repressing; live input ordering is not yet certified.

The same removal/confirmation/restoration helper handles exact inventory and storage items. An uncertain confirmation is logged and leaves the item consumed rather than risking duplication. The mod uses locally owned sources only and does not request ownership. Availability/hint counts refresh at 0.25 seconds and are not reservations.

## Pitfalls

- Legacy transfers defer accepted inventory items to vanilla. Only the opt-in kiln path currently controls inventory selection; reordering storage alone cannot enforce exclusions.
- Never let an intentionally blocked automatic choice fall through to vanilla and consume an excluded inventory type.
- A highlighted tile is not an armed selection until confirmed. The preview-only setting still does not change E behavior; do not silently migrate it into consuming mode.
- Do not add a blanket repeated-input scheduler: direct cooking interactions and Fermenters currently reject held requests.
- Availability counts can become stale. Actual insertion revalidates exact item, ownership, access and readiness at commit time.
- Keep a shared-library copy compatible across all installed storage mods; the game may resolve one of several beside-plugin copies.

## Unknowns

- Controller-specific navigation/cancellation, other UI scales/resolutions, opening alongside other menus, multiplayer behavior and broader input-order edge cases beyond the three user-reported local checks.
- Controller opening binding and final native-game UI integration/localization.
- Selection currently targets only the ordinary Charcoal Kiln's input switch. Other multi-type processors and CookFromStorage integration remain later milestones. Preference persistence, consumption, native hold timing and interruption behavior need the next user checkpoint in the [Supply README](../../SupplyFromStorage/README.md).