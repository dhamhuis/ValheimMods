using System;
using HarmonyLib;

namespace BuildFromStorage;

[HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement), typeof(bool), typeof(float))]
internal static class BuildPlacementContextPatch
{
    [HarmonyPrefix]
    private static void BeforePlacement(Player __instance, out BuildStorageContext.ScopeToken __state)
    {
        __state = BuildStorageContext.BeginPlacement(
            __instance,
            BuildFromStoragePlugin.StorageRegistry,
            BuildFromStoragePlugin.StorageRadius);
    }

    [HarmonyFinalizer]
    private static Exception AfterPlacement(Exception __exception, BuildStorageContext.ScopeToken __state)
    {
        BuildStorageContext.End(__state);

        return __exception;
    }
}