# Changelog

## 1.0.0

- Add one accepted cooking ingredient from nearby accessible storage on each empty food-switch interaction.
- Preserve cooked-item pickup, player-inventory ingredient priority, manual item selection, station capacity and required-fire checks.
- Use only locally owned stations and containers; recheck access, ownership, range and acceptance before mutation.
- Add a configurable storage radius (1-100m, default 20m) and detailed config descriptions; stations must be within 20m.
- Add focused Harmony integration tests for supported behavior and rejected-transfer restoration.