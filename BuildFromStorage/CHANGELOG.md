# Changelog

## 1.0.1

- Show what you carry and what nearby storage holds in the Hammer build tooltip, for example `5` with `10:42` below (the `5` moves up a line so the counts fit where it normally sits, and long material names stay on one line): 5 required, 10 in your inventory, 42 in nearby storage, with the counts line in yellow. A side with none is left blank (`10:` or `:42`), and with none anywhere you see just the vanilla `5`. The required amount stays white, or flashes red when inventory and storage together are short, as in vanilla. Missing-material flashing also counts nearby storage.
- Share the placement engine with FarmFromStorage, routing Hammer and Cultivator costs independently and preserving inventory-first, nearest-storage-first consumption.
- Keep Hammer and farming radius settings separate; use matching current common DLLs beside companion plugins.
- Fix 1.0.0 also supplying Cultivator planting and other placement tools from storage; BuildFromStorage now handles only the Hammer, and farming from storage requires FarmFromStorage.

## 1.0.0

- Add Hammer building with materials from nearby, closed storage owned by the local client.
- Keep inventory-first consumption, nearby-storage ordering, access permissions and vanilla placement checks.
- Add a configurable player-centered storage radius (1-100 m, default 20 m).
- Integrate with the shared mod build, deployment, icon and Thunderstore package structure.