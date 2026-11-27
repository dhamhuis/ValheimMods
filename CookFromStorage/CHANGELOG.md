# Changelog

## 1.0.2

- Show what you carry and what nearby storage holds in cooking recipe requirements, for example `5` with `10:42` below (the `5` moves up a line so the counts fit where it normally sits): 5 required, 10 in your inventory, 42 in nearby storage, with the counts line in yellow. A side with none is left blank (`10:` or `:42`), and with none anywhere you see just the vanilla `5`. The required amount stays white, or flashes red when inventory and storage together are short, as in vanilla.
- Ship the updated shared `ValheimMods.Common.dll` used by all current storage mods. Update every storage mod together so each folder has the same copy.

## 1.0.1

- Add the F8 input menu for multi-input cooking stations and Fermenters, enabled by default. Pick the ingredient you want or set automatic priorities and exclusions, so the mod doesn't choose for you when you'd rather save something for later.
- Save automatic preferences per character and station type.
- Choose automatic priorities across inventory and accessible storage, preferring inventory within the selected type. Explicit selection and normal item use can override automatic exclusions.
- Show and automatically select only inputs discovered by the current character, while retaining preferences for hidden inputs.
- Where vanilla supports held input, continue when aiming away and back and advance to the next available Auto priority. Explicit selections do not substitute another type.

## 1.0.0

- Add one accepted cooking ingredient from nearby accessible storage on each empty food-switch interaction.
- Preserve cooked-item pickup, player-inventory ingredient priority, manual item selection, station capacity and required-fire checks.
- Use only locally owned stations and containers; recheck access, ownership, range and acceptance before mutation.
- Add a configurable storage radius (1-100m, default 20m) and detailed config descriptions; stations must be within 20m.
- Add focused Harmony integration tests for supported behavior and rejected-transfer restoration.