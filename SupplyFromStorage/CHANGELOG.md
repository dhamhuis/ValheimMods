# Changelog

## 1.0.3

- Ship the updated shared `ValheimMods.Common.dll` used by all current storage mods. Update every storage mod together so each folder has the same copy. No SupplyFromStorage behavior changes.

## 1.0.2

- Take control of what goes into your stations with the F8 input menu: choose an exact item or set automatic priorities and exclusions. For example, turn Wood into charcoal while keeping Finewood and Corewood out of automatic kiln input.
- Expand the menu to the Smelter, Blast Furnace and Battering Ram engine alongside the Charcoal Kiln. Selection is enabled by default for new configurations; existing saved settings are preserved.
- Save automatic preferences per character and station type. Priorities span inventory and accessible storage, preferring inventory within the chosen type; explicit choices can override exclusions.
- Show and automatically select only inputs discovered by the current character (no spoilers), while retaining preferences for hidden inputs.
- Continue held input when aiming away and back; Auto advances to the next available priority when a type runs out. Explicit selections do not substitute another type.

## 1.0.1

- Fix the misleading "You are out of wood" message when adding fuel from storage to empty or fueled fires. Successful transfers now show the normal added-fuel message.
- Show the normal cannot-add-more message for full fires when accessible storage has fuel, without consuming wood.
- Preserve inventory-first fueling, fire toggles, held-interaction timing and vanilla failure feedback.

## 1.0.0

- Add independent fuel and processing-input settings, enabled by default; each valid machine E interaction transfers at most one item.
- Repeat one-item transfers while E is held, following vanilla interaction timing; do not fill stations during world load or periodic updates.
- Suppress the Smelter no-processable-items message when an accepted storage input is supplied; preserve vanilla inventory and full-station feedback.
- Supply accepted items from locally owned, accessible storage in a player-centered sphere with a configurable radius (1-100m, default 20m); machines must be within 20m.
- Use vanilla fire/cooking/smelter fuel slots and current Smelter conversion entries; vanilla uses accepted player-inventory items first, then the mod falls back to accessible storage.
- Preserve vanilla player-inventory interactions and fire toggles; recheck container permission, ownership, station state and capacity before removing each item, and rate-limit unsuccessful checks.