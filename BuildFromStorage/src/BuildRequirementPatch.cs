using System;
using HarmonyLib;

namespace BuildFromStorage;

[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
internal static class BuildRequirementPatch
{
    [HarmonyPrefix]
    private static void BeforeRequirementCheck(
        Player __instance,
        Player.RequirementMode mode,
        out BuildStorageContext.ScopeToken __state)
    {
        __state = mode == Player.RequirementMode.CanBuild
            ? BuildStorageContext.BeginRequirements(
                __instance,
                BuildFromStoragePlugin.StorageRegistry,
                BuildFromStoragePlugin.StorageRadius)
            : null;
    }

    [HarmonyFinalizer]
    private static Exception AfterRequirementCheck(Exception __exception, BuildStorageContext.ScopeToken __state)
    {
        BuildStorageContext.End(__state);

        return __exception;
    }
}