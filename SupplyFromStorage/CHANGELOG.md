# Changelog

## 1.0.1

- Fix the misleading "You are out of wood" message when adding fuel from storage to empty or fueled fires. Successful transfers now show the normal added-fuel message.
- Show the normal cannot-add-more message for full fires when accessible storage has fuel, without consuming wood.
- Preserve inventory-first fueling, fire toggles, held-interaction timing and vanilla failure feedback.

## 1.0.0

- Add independent fuel and processing-input settings, enabled by default; each valid machine E interaction transfers at most one item.
- Repeat one-item transfers while E is held, following vanilla interaction timing; do not fill stations during world load or periodic updates.
- Suppress the Smelter no-processable-items message when an accepted storage input is supplied; preserve vanilla inventory and full-station feedback.
- Supply accepted items from locally owned, accessible storage in a player-centered sphere with a configurable radius (1-100m, default 20m); machines must be within 20m.
- Describe each setting's machines and items in the config file.
- Use vanilla fire/cooking/smelter fuel slots and current Smelter conversion entries; vanilla uses accepted player-inventory items first, then the mod falls back to accessible storage.
- Preserve vanilla player-inventory interactions and fire toggles; recheck container permission, ownership, station state and capacity before removing each item, and rate-limit unsuccessful checks.
- Add focused behavior and Harmony integration tests, including the CraftFromStorage inventory-hook boundary.