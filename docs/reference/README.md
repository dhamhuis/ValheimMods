# Mod API reference

Read this index before library discovery. It is an agent routing map, not an exhaustive game API catalog. Follow only the relevant links, reuse verified findings, and maintain them during the task under the [repository workflow](../../AGENTS.md).

## Choose a topic

| Task or symbols | Read first | Coverage and gaps |
| --- | --- | --- |
| Dependencies, DLL locations, publicizing, `BaseUnityPlugin`, `Harmony`, Unity lifecycle | [Libraries and runtime](Libraries.md) | Dependency map, local fingerprints, startup/cleanup patterns; configuration and custom UI APIs not investigated. |
| Station interaction, `CraftingStation.Interact`, `HaveRepairableItems`, `RepairOneItem` | [Station repair](Repair.md) | Installed signatures and AutoRepair guards; game-internal eligibility/effects need targeted inspection. |
| Recipes, `CountItems`, `RemoveItem`, `HaveRequirementItems`, `GetFirstRequiredItem`, `DoCrafting` | [Crafting and inventories](Crafting.md) | Existing scoped patches and ingredient plan; new recipe systems require discovery. |
| Containers, access, wards, `ZNetView`, ownership RPCs, persistence | [Storage and ownership](Storage.md) | Existing selection/ownership protocol; multiplayer and persistence not live-verified here. |
| Fuel and processing inputs, `Fireplace`, `CookingStation`, `Fermenter`, `Smelter`, `Windmill` | [Fuel and processing](Processing.md) | Installed component signatures, cooking/Fermenter input paths and SupplyFromStorage hooks; prefab mappings and machine behavior need in-game verification. |
| Missing API, changed signature, unfamiliar system, revalidation | [API discovery skill](../../.github/skills/valheim-api-discovery/SKILL.md) | Bounded inspection, evidence recording, validation, and maintenance procedure. |
| Build, deploy, test, package | [Development guide](../Development.md) | Commands and validation limits; not an API catalog. |

Console/debug commands, spawning, combat, world generation, custom UI, and other unlisted systems are **not investigated**, not unsupported. Add focused topics when a task establishes useful knowledge; do not prepopulate speculative catalogs.

## Evidence and freshness contract

Every topic records `Status`, `Last verified`, `Evidence`, and `Limitations`, followed by use cases, API/behavior, pitfalls, and unknowns. The date alone does not establish compatibility.

- **Verified** means only the explicitly stated evidence is established. Qualify it as repository inspection, assembly metadata, game-body inspection, automated test, or in-game observation. These levels are not interchangeable.
- **Needs revalidation** means a relevant fingerprint changed, evidence conflicts, or assumptions no longer match. Keep the old evidence and state what needs checking.
- **Unknown / not investigated** means discovery is required before relying on a claim. Hypotheses must stay in Unknowns, not become verified advice.

Use a real `YYYY-MM-DD` verification date. An entirely uninvestigated topic may use `Last verified: Never.` rather than invent a date; it must have `Status: Unknown / not investigated.`. A stale topic keeps its previous verification date and evidence.

Seeded on 2026-11-06 from repository revision `cdddc55` and the installed DLL identities in [Libraries](Libraries.md). Metadata was read with Mono.Cecil; no game code was executed and no in-game tests were performed for this reference. Repository behavior describes that revision, not a promise about another worktree or future changes.

Before depending on an assembly-backed finding in a new session, compare the affected DLL's SHA-256 using the [inspector](../../scripts/Get-ValheimApi.ps1). Compare once per relevant assembly per session, or again after a known update. A matching fingerprint avoids repeated signature discovery, but does not prove runtime behavior. If no DLL is available, retain the recorded evidence and explicitly report that current compatibility is unverified.

## Maintenance rules

1. Update the smallest relevant entry when useful knowledge is established or invalidated. Add a topic and index row only when an existing topic is unsuitable.
2. Include exact declaring type, overload, original parameter names when patch binding matters, return type, and visibility. Say whether a callable-looking member needs publicizing.
3. Separate library behavior from our mod's policy. Document preconditions, mutation, timing, ownership, failure handling, and alternatives only as far as evidence supports them.
4. Link evidence and working examples relative to the document. Identify relevant tests and whether they were actually run; stub behavior is not game-body evidence.
5. For a negative finding, record the assembly fingerprint, exact type/member/search scope, result, and limits. "Not found in these members" is not "impossible in Valheim."
6. Preserve unverified gaps rather than filling them with guesses. Do not update verification dates or fingerprints without checking the associated claims.
7. Run `.\scripts\tests\Test-AgentReference.ps1` after maintenance. Runtime changes still require normal validation. Do not rebuild distributions for reference-only changes.

## Automation boundaries

The entry points use the [AGENTS.md format](https://agents.md/) and [Agent Skills specification](https://agentskills.io/specification). Harness discovery and fresh-session verification follow [VS Code's custom instruction guidance](https://code.visualstudio.com/docs/agent-customization/custom-instructions). The topic evidence format is a repository convention, not a universal agent standard.

The root instructions require consult-before-discover and maintain-before-complete without a repeated user prompt. The skill is an investigation aid, not the sole trigger. Supporting agents must load the root instructions; linked topics are explicitly requested by those instructions, not automatically injected in full.

Instructions cannot guarantee compliance in every harness. The validator detects structural regressions, not reasoning quality or live behavior. Verify discovery and behavior in a fresh session using the acceptance procedure in the skill. Existing worktrees need these changes merged before they share this workflow; already-running sessions may need a restart or an explicit instruction refresh.