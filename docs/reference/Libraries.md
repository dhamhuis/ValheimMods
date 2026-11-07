# Libraries and runtime

Status: Verified - repository wiring and installed assembly metadata only.
Last verified: 2026-11-06.
Evidence: Both [AutoRepair](../../AutoRepair/src/AutoRepair.csproj) and [CraftFromStorage](../../CraftFromStorage/src/CraftFromStorage.csproj) project references; plugin startup code linked below; targeted Mono.Cecil inspection of local DLLs.
Limitations: No library implementation bodies, external API manuals, or live game behavior verified in this seed.

## Use when

Choosing a dependency, locating a DLL, checking version drift, creating a plugin, installing a Harmony patch, or deciding whether a Unity API belongs here.

## API and behavior

### Dependency map

| Dependency | Use and current source | Important boundary |
| --- | --- | --- |
| `assembly_valheim.dll` | Game classes including `Player`, `Inventory`, `InventoryGui`, `CraftingStation`, `Container`, `ZNetView`, and `PrivateArea`; `$(ValheimManagedDir)`. | No exhaustive public API contract. Verify exact overloads and behavior for the installed build. |
| `UnityEngine.dll`, `UnityEngine.CoreModule.dll` | Unity facade/core types, objects, components, `Vector3`, `Time`, and coroutines; `$(ValheimManagedDir)`. | Do not assume APIs from a different Unity version are available. |
| `UnityEngine.UIModule.dll`, `UnityEngine.UI.dll` | UI dependencies referenced by both projects; `$(ValheimManagedDir)`. | Current references do not establish a custom UI implementation pattern. |
| `BepInEx.dll` | `BaseUnityPlugin`, plugin attributes, and `ManualLogSource`; `$(BepInExCoreDir)`. | Local BepInEx 5 reference, not a BepInEx 6 migration guide. Configuration APIs not yet investigated. |
| `0Harmony.dll` | `HarmonyLib` prefixes, postfixes, finalizers, target lookup, registration inspection, and unpatching; `$(BepInExCoreDir)`. | Use installed production version, not assumptions from the test-only HarmonyX package. |
| `BepInEx.AssemblyPublicizer.MSBuild` | NuGet build dependency; both projects specify `0.4.1`, `PrivateAssets=all`, and `Publicize=true` on the game reference. | Compile-time access to private game members does not make those members stable or distributable. |
| `Mono.Cecil.dll` | Read-only assembly metadata inspection by the [patch validator](../../scripts/Validate-ModPatches.ps1) and [API inspector](../../scripts/Get-ValheimApi.ps1); BepInEx core. | Inspection tooling, not a plugin dependency to ship. Metadata alone does not explain method behavior. |

The project files define default Steam/profile paths and override properties `ValheimInstallDir`, `ValheimManagedDir`, `R2ModmanProfileDir`, and `BepInExCoreDir`. Use those properties and the [development guide](../Development.md); do not hardcode a contributor's absolute profile path in documentation.

Both plugins target `netstandard2.1` for the repository's Unity Mono/BepInEx setup. Game references have `Private=false`. Build, deploy, and packaging must not redistribute game, Unity, BepInEx, Harmony, Cecil, or publicized reference DLLs.

### Installed baseline

These are local reference identities, not latest upstream version claims. Assembly version `0.0.0.0` is insufficient to identify a game or Unity build; compare SHA-256. The player-facing Valheim/Unity version strings were not established here.

| DLL | Assembly version | SHA-256 |
| --- | --- | --- |
| `assembly_valheim.dll` | `0.0.0.0` | `9F959C0396EC4240D47FCBE5D0D062C428CC627244506DB5EA567F9D77B2B769` |
| `UnityEngine.CoreModule.dll` | `0.0.0.0` | `FBA3821AFB6867FE4D471DC50BF04BA21E6982B33AB6B5336B08A5C55119D990` |
| `UnityEngine.UIModule.dll` | `0.0.0.0` | `89E31F812501D5DAE1235545D442284C603141924ABCF387E1F4B447F43C3C27` |
| `UnityEngine.UI.dll` | `1.0.0.0` | `61703D54EBDAEBB8AE0070257213888AFF7E4BFA5E61599DE7C0049427C07A0E` |
| `BepInEx.dll` | `5.4.23.6` | `18CAFFD2C09DC4E6A51FC13B4AF7FD2389251F2EC8ECDF8BF076B84A14FB991E` |
| `0Harmony.dll` | `2.9.0.0` | `1A21CC03424FC82C3DD1346905D16494536B9595AE4162228D99FB7C285C1031` |
| `Mono.Cecil.dll` | `0.10.4.0` | `7AE470288FFF4A402899C254D0A76CEFEF55877F5C54F96E83C797CC5BB6E2F6` |

The `UnityEngine.dll` facade was identified from project references but not fingerprinted in this seed; inspect it if a task depends on its metadata. MVIDs are also printed by the inspector for diagnostics; SHA-256 is the comparison key recorded here.

### BepInEx and Harmony patterns in this repository

The [AutoRepair plugin](../../AutoRepair/src/AutoRepairPlugin.cs) and [CraftFromStorage plugin](../../CraftFromStorage/src/CraftFromStoragePlugin.cs) derive from `BaseUnityPlugin`, declare `BepInPlugin` metadata and `BepInProcess("valheim.exe")`, and install patches in `Awake()`. Each uses its stable plugin GUID as the Harmony owner, verifies expected patch registration before logging success, and unpatches itself in `OnDestroy()`. This is repository implementation evidence, not proof every game lifecycle path has been exercised.

- Use `Harmony.PatchAll(assembly)` for the existing attribute-based pattern and `Harmony.GetPatchInfo(target)` to check registration. Do not treat compilation or a successful `PatchAll` call as proof the intended patch was registered.
- Overloaded targets such as `Inventory.RemoveItem` use explicit argument types in `HarmonyPatch`. Original argument names and Harmony names such as `__instance`, `__result`, `__state`, and positional `__0` are binding contracts, not style choices.
- The [requirement-check patch](../../CraftFromStorage/src/RequirementCheckPatch.cs) creates a disposable context in a prefix and restores it in a finalizer. The [transaction finalizer](../../CraftFromStorage/src/CraftTransactionPatch.cs) logs exceptions without converting failure into success.
- Use the plugin's `ManualLogSource` for actionable warnings/errors. Do not catch and silently ignore API or signature failures.

### Unity usage already present

[NearbyStorage](../../CraftFromStorage/src/NearbyStorage.cs) initially finds loaded containers with `Object.FindObjectsByType<Container>(FindObjectsSortMode.None)`, then maintains a registry through container lifecycle patches. Its query cache is scoped to frame, player, and position using `Time.frameCount` and `Vector3`. It uses Unity-style `container != null` checks when removing destroyed objects; do not assume ordinary managed-reference null checks are interchangeable.

[StorageCrafting](../../CraftFromStorage/src/StorageCrafting.cs) runs as a coroutine started by the plugin, uses `yield return null` while awaiting ownership, and measures a five-second timeout with `Time.realtimeSinceStartup`. These are mod policies. Threading, object destruction semantics, and callback ordering beyond this usage need library/body or live verification.

## Pitfalls

- The test executables use stubs and a test-only HarmonyX package. They do not establish compatibility with the production Harmony DLL or a live Unity instance.
- Publicized compile references can hide that a game member was originally private. Record original visibility from the unmodified installed DLL.
- A new fingerprint invalidates compatibility assumptions, not necessarily every documented behavior. Recheck affected members and preserve old evidence until replaced.
- Do not add a dependency merely because it is common in other Valheim mods. No Jotunn dependency is currently declared in these two projects.

## Unknowns

- BepInEx configuration APIs, dependency ordering, headless/dedicated-server loading, and new custom UI construction.
- The exact player-facing game/Unity build labels and compatibility outside this fingerprint baseline.
- Behavior of patched APIs under other mods' prefixes/transpilers and patch ordering; verify per integration task.