# Changelog

## 1.0.1

- Show what you carry and what nearby storage holds in crafting and upgrade requirements, for example `5` with `10:42` below (the `5` moves up a line so the counts fit where it normally sits): 5 required, 10 in your inventory, 42 in nearby storage, with the counts line in yellow. A side with none is left blank (`10:` or `:42`), and with none anywhere you see just the vanilla `5`. The required amount stays white, or flashes red when inventory and storage together are short, as in vanilla.
- Ship the updated shared `ValheimMods.Common.dll` used by all current storage mods. Update every storage mod together so each folder has the same copy.

## 1.0.0

- Add focused station crafting and equipment upgrades from accessible storage within a configurable player-centered sphere (1-100m, default 20m).
- Keep inventory-first consumption, nearest storage ordering, normal access rules and vanilla station/recipe checks.
- Include storage in crafting requirement checks and coloring.
- Add exact ingredient planning, vanilla chest ownership requests, cancellation/timeout handling and signature/behavior validation.
- Cancel safely when storage is invalidated during an ownership wait; allow retries and validated manual opening after lost responses while suppressing unsolicited late replies.
- Exercise production Harmony patches and exception cleanup in independent, documented behavior and integration tests.
- Follow the shared mod build, deployment, icon and Thunderstore package structure.
- Complete local in-game testing; dedicated multiplayer and persistence validation remains outstanding.