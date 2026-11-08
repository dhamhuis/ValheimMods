# Storage and ownership

Status: Verified - installed signatures and repository implementation only.
Last verified: 2026-11-06.
Evidence: Targeted Mono.Cecil inspection at the [library baseline](Libraries.md); [NearbyStorage](../../ValheimMods.Common/src/Crafting/NearbyStorage.cs), [StorageCrafting](../../ValheimMods.Common/src/Crafting/StorageCrafting.cs), and lifecycle/response patches linked below.
Limitations: Access and protocol descriptions below are mod policy inferred directly from source, not an independent verification of game RPC internals, authorization, network ordering, or disk persistence.

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

- The full game method bodies for `RPC_RequestOpen`, `RPC_OpenResponse`, ownership transfer, inventory-change callbacks, `Load`, and `Save`.
- Live multiplayer ordering, competing players/mods, timeout/disconnect recovery, dedicated servers, and persisted contents after reload.
- Other container-like objects not covered by this registry. Follow the [discovery workflow](../../.github/skills/valheim-api-discovery/SKILL.md) rather than assuming they are unsupported.