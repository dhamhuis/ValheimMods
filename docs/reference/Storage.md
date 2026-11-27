# Storage and ownership

Status: Verified - installed signatures, repository implementation, and targeted chest-opening method bodies.
Last verified: 2026-11-06.
Evidence: Targeted Mono.Cecil inspection at the [library baseline](Libraries.md); [NearbyStorage](../../ValheimMods.Common/src/Crafting/NearbyStorage.cs), [StorageCrafting](../../ValheimMods.Common/src/Crafting/StorageCrafting.cs), and lifecycle/response patches linked below.
Limitations: Mod access and protocol descriptions below are inferred from repository source. The targeted game-body inspection is limited to the opening flow noted below; network ordering, multiplayer behavior, and disk persistence remain unverified in game.

## Use when

Finding nearby storage, checking accessibility, waiting for ownership, handling container lifecycle, or deciding whether container contents may safely be read or mutated.

## API and behavior

### Installed signatures

| Member | Original visibility / return | Parameters |
| --- | --- | --- |
| `Container.Awake`, `Container.OnDestroyed` | Private instance / `void` | None |
| `Container.Interact` | Public instance / `bool` | `Humanoid character, bool hold, bool alt` |
| `Container.CheckAccess` | Private instance / `bool` | `long playerID` |
| `Container.IsOwner`, `Container.IsInUse` | Public instance / `bool` | None |
| `Container.Load` | Private instance / `bool` | None |
| `Container.Save` | Private instance / `void` | None |
| `Container.RPC_OpenResponse` | Private instance / `void` | `long uid, bool granted` |
| `ZNetView.InvokeRPC` | Public instance / `void` | `string method, object[] parameters` or `long targetID, string method, object[] parameters` |
| `PrivateArea.CheckAccess` | Public static / `bool` | `UnityEngine.Vector3 point, float radius, bool flash, bool wardCheck` |

Private members are accessed through the publicized reference. `Load()` returns a boolean; current repository calls do not inspect that result. The signature alone establishes neither when loading is necessary nor its failure/persistence semantics.

### Discovery and access policy

[NearbyStorage](../../ValheimMods.Common/src/Crafting/NearbyStorage.cs) seeds a registry from loaded containers; [creation](../../ValheimMods.Common/src/Crafting/ContainerCreatedPatch.cs) and [destruction](../../ValheimMods.Common/src/Crafting/ContainerDestroyedPatch.cs) patches maintain it. `Find` filters, calls `Load`, and sorts by squared distance with instance ID as a tie-breaker. It caches results for the same frame/player/position unless refreshed.

`IsAccessible` requires an active container, valid network view, inventory, and a player within the **configured player-centered storage radius**. Each storage mod binds its own `Storage/Storage radius` setting (default 20 m, clamped to 1–100 m by `AcceptableValueRange` and [`StorageRange.Clamp`](../../ValheimMods.Common/src/StorageRange.cs)); shared recipe discovery uses CookFromStorage's radius for food stations and CraftFromStorage's for other stations. Station reach checks in Cook and Supply stay fixed at `StorageRange.StationReach` (20 m) so a small storage radius does not reject a station the player is using. It checks local/network in-use state, wagon use, player access, and ward access when requested by the container. `allowReserved` bypasses one local busy test for our own reservation, not all access checks. Radius, filtering, and ordering are this mod's policies, not universal Valheim rules.

### Ownership and mutation policy

On 2026-11-24, targeted Mono.Cecil method-body inspection of `assembly_valheim.dll` SHA-256 `9F959C0396EC4240D47FCBE5D0D062C428CC627244506DB5EA567F9D77B2B769` established the following vanilla opening flow. `Container.Interact(Humanoid character, bool hold, bool alt)` checks applicable ward and container access, then sends `RPC_RequestOpen` with the player's ID. On the existing network owner, `Container.RPC_RequestOpen(long uid, long playerID)` checks busy state and access; an accepted request calls `ZDOMan.ForceSendZDO` and `ZDO.SetOwner(uid)` with the requesting network peer before sending a granted `RPC_OpenResponse`. The response opens the inventory UI. `Container.IsOwner()` delegates to `ZNetView.IsOwner()`.

This is network-object ownership, distinct from player access permissions. The inspected flow does not make ownership a permanent player claim or restrict transfer to the first open. It does not establish how long ownership lasts after closing, when it may change later, or live multiplayer behavior. BuildFromStorage filters for current local ownership and closed, accessible storage; opening and closing a chest is a possible way to make it eligible, not a guarantee or a prerequisite when it is already locally owned.

FarmFromStorage uses the same local-ownership-only policy through the [shared placement engine](Building.md#shared-hammercultivator-engine---2026-11-24). Its client-specific radius and access/reservation behavior were exercised in stub tests on 2026-11-24; no new ownership RPC or persistence guarantee is introduced.

[StorageCrafting](../../ValheimMods.Common/src/Crafting/StorageCrafting.cs) plans the needed sources, then for each non-owned source sends `InvokeRPC("RPC_RequestOpen", player.GetPlayerID())`. It waits for both a granted response and local `IsOwner()`, with a five-second real-time deadline and ongoing validity checks.

[StorageOwnershipResponsePatch](../../ValheimMods.Common/src/Crafting/StorageOwnershipResponsePatch.cs) routes matching responses into that pending state rather than treating them as ordinary manual chest opens. Late-response guards and [manual-interaction handling](../../ValheimMods.Common/src/Crafting/PendingContainerInteractionPatch.cs) distinguish pending craft responses from a valid later player open.

After ownership, the coroutine rechecks accessibility, calls `Load`, sets the owner's local `m_inUse` flag, and records the reservation. It validates UI/player/recipe/station state and source ownership again, then replans ingredients before executing the [crafting flow](Crafting.md). Finally it clears execution/pending state and releases local reservations. This is not a globally atomic network lock.

The cleanup does not establish an ownership-transfer-back protocol. `Container.Save` exists in metadata, but this reference has not inspected the game's inventory-change/save path; do not invent a direct `Save` requirement or claim persistence is proven.

## Pitfalls

- Nearby does not mean accessible, owned, loaded, writable, or persisted.
- A granted response alone is not the coroutine's completion condition; local ownership must also be observed.
- Access checks may become stale while yielding. Revalidate before mutation, and handle cancellation/late responses without opening unexpected UI.
- Reserving local `m_inUse` is not proof of a network-wide lock; do not describe the flow as race-free.
- Container destruction invalidates cached references and pending state. Respect Unity destroyed-object checks and the lifecycle hooks.

## Unknowns

- The full game method bodies for ownership transfer internals, inventory-change callbacks, `Load`, and `Save`, and how long ownership lasts after a chest closes.
- Live multiplayer ordering, competing players/mods, timeout/disconnect recovery, dedicated servers, and persisted contents after reload.
- Other container-like objects not covered by this registry. Follow the [discovery workflow](../../.github/skills/valheim-api-discovery/SKILL.md) rather than assuming they are unsupported.