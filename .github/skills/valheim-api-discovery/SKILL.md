---
name: valheim-api-discovery
description: "Use when discovering, inspecting, or revalidating Valheim, Unity, BepInEx, or Harmony APIs, game commands, DLL signatures, ownership behavior, or missing/stale mod reference knowledge. Records evidence and maintains the repository API reference."
---

# Valheim API discovery

## Consult before inspecting

1. Read the [reference index](../../../docs/reference/README.md) and relevant topics. Root instructions require this even when this skill was not explicitly requested.
2. Define the bounded question: desired behavior, candidate type/member, exact overload, and whether metadata or method-body/runtime evidence is required.
3. Locate references through the affected project file and [library map](../../../docs/reference/Libraries.md), respecting build overrides and the selected profile. Compare the affected assembly's SHA-256 once per session before relying on its recorded metadata.
4. Reuse an existing entry/example when applicable and current. If the DLL is unavailable, state that current compatibility is unverified; do not guess a successful inspection.

## Targeted inspection

Use the [read-only inspector](../../../scripts/Get-ValheimApi.ps1), which uses the profile's Mono.Cecil without loading/executing game code:

```powershell
$game = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll'
.\scripts\Get-ValheimApi.ps1 -AssemblyPath $game
.\scripts\Get-ValheimApi.ps1 -AssemblyPath $game -TypeName Inventory -MemberName CountItems,RemoveItem
.\scripts\Get-ValheimApi.ps1 -AssemblyPath $game -TypeName Container -MemberName RPC_OpenResponse,Load,Save
```

`-BepInExCoreDir 'C:\path\to\BepInEx\core'` selects another inspection-tool source. The tool reports the target's name/version/MVID/SHA-256 and exact selected method/field/property metadata, including property getter/setter signatures and their individual visibility. Use the full declaring type name (Cecil nested names use `/`); matching is exact. Missing types/members and empty/blank selections fail explicitly. A missing result only bounds that specific search.

For a genuinely unknown type, first search relevant repository examples with a narrow C# file filter. If necessary use a bounded metadata type-name filter in Cecil or an available local decompiler's type search; do not dump all types or methods. Record the search scope. Use existing tools before considering installations.

Metadata answers existence, signature, original visibility, parameter names, and attributes, not effects or ordering. If the question concerns eligibility, mutation, ownership, callbacks, persistence, or lifecycle, inspect only the relevant method bodies and immediate callers/callees with an available local IL/decompilation tool. Stop expanding when the question has adequate evidence.

Do not copy decompiled bodies or third-party DLLs into the repository. Record concise original explanations, necessary API signatures, and assembly identity. Do not send local game/mod code or assemblies to external services.

## Verify the actual requirement

- For changed Harmony bindings, build the affected project and use the [signature validator](../../../scripts/Validate-ModPatches.ps1) against a freshly built matching plugin, not a stale distribution. Follow the [build skill](../valheim-mod-build/SKILL.md) and [development guide](../../../docs/Development.md).
- The signature validator supports a bounded subset of attribute-based patches. It does not fully validate result/state/exception bindings, patch ordering, method bodies, or runtime registration.
- Run relevant production-linked tests for code changes. Stub tests verify our logic under their modeled behavior, not game implementation or multiplayer persistence.
- Check registration in the live BepInEx log and perform the relevant in-game scenarios when available. If this requires the user to launch/play, report the remaining checklist honestly; never claim it was performed.
- For documentation-only findings, match signatures/fingerprints and inspect linked source. Do not rebuild/deploy/package merely to update documentation.

## Record reusable findings during the same task

Update the existing topic rather than creating duplicate entries. New topics must be linked from the index. Each topic uses this structure (replace every placeholder with bounded evidence; do not label guesses Verified):

```markdown
# Topic name

Status: Verified - assembly metadata only.
Last verified: YYYY-MM-DD.
Evidence: Relative source/test links; exact assembly SHA-256 or a link to its baseline; inspection/test method and actual result.
Limitations: What this evidence does not establish.

## Use when

Task routing, relevant types, and inappropriate alternatives.

## API and behavior

Exact signatures/visibility and relevant binding names; preconditions, side effects,
timing, ownership, failure handling, and examples, each qualified by evidence.

## Pitfalls

Known mistakes and why the verified approach avoids them.

## Unknowns

Uninvestigated behavior, hypotheses, bounded negative results, and next verification.
```

Dates must be real calendar dates. An entirely uninvestigated topic uses `Status: Unknown / not investigated.` and `Last verified: Never.`; do not invent verification to satisfy the template. For changed DLLs, mark affected entries `Needs revalidation` until their claims are checked, keeping their previous dates and evidence. Do not replace a shared baseline fingerprint and thereby imply all linked topics were reverified. Record per-topic fingerprints when topics now refer to different builds; update the shared baseline only when its affected entries have been reconciled.

Example: "No `SaveInventory` member found on `Container` in SHA-256 X using exact-name metadata inspection; other types/callbacks not investigated" is a bounded negative finding. "Valheim cannot save container inventories" is not justified.

When behavior is only modeled by our implementation, say "the mod checks ownership before mutation," not "the game guarantees exclusive mutation." Add tests/evidence and update the status/date only for what was actually checked.

Run `.\scripts\tests\Test-AgentReference.ps1` after maintenance. In the final task summary mention useful knowledge changes and outstanding runtime verification. Do not create a documentation update for every trivial lookup.

## Fresh-session acceptance

After installing/changing this workflow:

1. In VS Code's customization UI, confirm the root instructions and this skill are discovered for the selected harness. A listed file proves discovery, not compliance.
2. Start a fresh session in a worktree containing these changes. Ask: "Which hook should we use to repair equipment after a station interaction?" Do not mention the reference.
3. Inspect tool activity/references: it should read the index and Repair topic before any assembly discovery, use the existing hook, and distinguish metadata/source evidence from live testing.
4. For maintenance, ask that session to investigate one genuinely undocumented API on a disposable branch. It should inspect narrowly, update the appropriate topic/index with evidence and gaps, and run the reference validator without being reminded.
5. Review the diff and reported limits. If instructions were not loaded, check harness support/settings and worktree contents; do not compensate by duplicating the full knowledge base into multiple instruction files.

This interaction check requires a fresh agent session; the structural validator cannot certify that an agent complied. Existing worktrees do not receive these files until merged, and running sessions may need refreshed instructions.