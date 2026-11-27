# Changelog

## 1.0.0

- Plant anything available in the Cultivator menu using materials from nearby accessible, closed, locally owned storage.
- Use player inventory first, then storage in nearest-first order, with a configurable 1-100 meter radius (20 meters by default).
- Share scoped placement checks and resource planning with BuildFromStorage without duplicate consumption.
- Show what you carry and what nearby storage holds in the Cultivator tooltip, for example `5` with `10:42` below (the `5` moves up a line so the counts fit where it normally sits, and long material names stay on one line): 5 required, 10 in your inventory, 42 in nearby storage, with the counts line in yellow. A side with none is left blank (`10:` or `:42`), and with none anywhere you see just the vanilla `5`. The required amount stays white, or flashes red when inventory and storage together are short, as in vanilla. Missing-seed flashing also counts nearby storage.
- Preserve vanilla discovery, placement restrictions, tool costs and no-cost modes.