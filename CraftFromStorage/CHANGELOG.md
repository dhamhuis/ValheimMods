# Changelog

## 1.0.1

- Show inventory and nearby storage counts below crafting and upgrade requirements, with shortage feedback accounting for both.
- Ship the updated shared `ValheimMods.Common.dll` used by all current storage mods. Update every storage mod together so each folder has the same copy.

## 1.0.0

- Add focused station crafting and equipment upgrades from accessible storage within a configurable player-centered sphere (1-100m, default 20m).
- Keep inventory-first consumption, nearest storage ordering, normal access rules and vanilla station/recipe checks.
- Include storage in crafting requirement checks and coloring.
- Request chest ownership when needed and handle cancellation or timeouts without spending ingredients.
- Cancel safely when storage is invalidated during an ownership wait; allow retries and validated manual opening after lost responses while suppressing unsolicited late replies.