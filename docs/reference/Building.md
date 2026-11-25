# Building from storage

Status: Verified - installed signatures and targeted game-method IL inspection; implementation tests use game stubs.
Last verified: 2026-11-23.
Evidence: `assembly_valheim.dll` MVID `7ac23c58-e834-4776-b9c1-ae8a703440f1`, SHA-256 `9F959C0396EC4240D47FCBE5D0D062C428CC627244506DB5EA567F9D77B2B769`; targeted Mono.Cecil signature and method-body inspection; [BuildFromStorage patches](../../BuildFromStorage/src/); [placement tests](../../BuildFromStorage/tests/Integration/PiecePlacementTests.cs). The user reported single-player testing complete on 2026-11-24; see the checked [in-game checklist](../../BuildFromStorage/TESTING.md).
Limitations: Method-body evidence is for this installed assembly only. Automated tests exercise mod code against stubs. The in-game report is user-provided; multiplayer ownership, concurrency, and persistence have not been tested.

## Use when

Investigating Hammer piece requirements, placement input, or resource consumption from storage. Read [Storage](Storage.md) for shared container access policy and [Crafting and inventories](Crafting.md) for the name-based inventory-removal contract.

## API and behavior

### Installed signatures

| Member | Original visibility / return | Parameters |
| --- | --- | --- |
| `Player.HaveRequirements` | Public instance / `bool` | `Piece piece, Player.RequirementMode mode` |
| `Player.UpdatePlacement` | Private instance / `void` | `bool takeInput, float dt` |
| `Player.TryPlacePiece` | Public instance / `bool` | `Piece piece` |
| `Player.ConsumeResources` | Public instance / `void` | `Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier` |
| `Piece.Requirement.GetAmount` | Public instance / `int` | `int qualityLevel` |
| `Piece.Requirement.m_resItem`, `m_amount`, `m_amountPerLevel`, `m_upgraderResource` | Public fields | Item resource, base/per-level amounts, and upgrader-resource marker |

`Player.RequirementMode` values in this fingerprint are `CanBuild = 0`, `IsKnown = 1`, and `CanAlmostBuild = 2`. The method body preserves station, DLC, and build-key checks before its resource loop. For `CanBuild`, it checks each resource with the player's `Inventory.CountItems(name, -1, true)` against `Piece.Requirement.m_amount`; other modes use different logic.

Targeted IL inspection established that `Player.UpdatePlacement` calls `HaveRequirements(piece, CanBuild)`, then `TryPlacePiece`, then `ConsumeResources(piece.m_resources, 0, -1, 1)` when placement succeeds and the piece's free-build key is not active. `ConsumeResources` filters requirements by the current station's `m_upgrader` and each requirement's `m_upgraderResource`, calculates `GetAmount(qualityLevel) * multiplier`, and calls the name-based inventory removal overload with world-level matching enabled. `GetAmount(0)` returns the base `m_amount` in this fingerprint.

### BuildFromStorage implementation

[BuildFromStorage](../../BuildFromStorage/README.md) scopes storage counts to the local player's `HaveRequirements` checks for `CanBuild`, including Hammer UI and placement checks. It adds counts only to the player's inventory query, leaving vanilla station, DLC, and free-build checks intact. Before placement, it plans the complete cost using player inventory first and nearby storage ordered by distance; it reserves only storage needed for that placement and consumes those portions after vanilla consumes the player inventory.

The mod reuses [`ContainerRegistry.IsAccessible`](../../ValheimMods.Common/src/ContainerRegistry.cs) and [`StorageRange`](../../ValheimMods.Common/src/StorageRange.cs). Its storage candidates must be accessible, within the configured player-centered radius, closed, and currently owned by the local client. This avoids starting an asynchronous ownership request in the synchronous Hammer placement flow. The configured radius defaults to 20 m and is clamped to 1–100 m.

## Pitfalls

- Keep the storage count patch inside the `CanBuild` requirement scope. `HaveRequirements` is also used for material discovery and other build UI checks.
- Build availability and cost consumption are separate steps. Plan the full cost before placement to prevent duplicate requirements from allocating the same stack twice.
- Preserve the current-station/upgrader-resource filter and the free-build key. Do not globally inflate `Inventory.CountItems`.
- `Inventory.RemoveItem(string, int, int, bool)` returns `void`; validate planned storage counts before consuming and surface unexpected changes.

## Unknowns

- The user checked the single-player Hammer placement, UI, and save/reload scenarios in the checklist; their report does not establish multiplayer behavior.
- Multiplayer ownership changes, concurrent container access, and persistence after multiplayer save/reload need in-game validation.
- The current implementation does not request ownership of a chest it does not already own. Opening and closing it may make it eligible, but ownership can change and this is not guaranteed; see [Storage and ownership](Storage.md).