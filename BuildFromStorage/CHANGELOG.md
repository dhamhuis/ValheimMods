# Changelog

## 1.0.1

- Show inventory and nearby storage counts below each Hammer build requirement, with missing-material feedback accounting for both.
- Keep Hammer and farming radius settings separate; use matching current common DLLs beside companion plugins.
- Fix 1.0.0 also supplying Cultivator planting and other placement tools from storage; BuildFromStorage now handles only the Hammer, and farming from storage requires FarmFromStorage.

## 1.0.0

- Add Hammer building with materials from nearby, closed storage owned by the local client.
- Keep inventory-first consumption, nearby-storage ordering, access permissions and vanilla placement checks.
- Add a configurable player-centered storage radius (1-100 m, default 20 m).