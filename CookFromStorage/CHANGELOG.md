# Changelog

## 1.0.2

- Show inventory and nearby storage counts below cooking recipe requirements, with shortage feedback accounting for both.
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