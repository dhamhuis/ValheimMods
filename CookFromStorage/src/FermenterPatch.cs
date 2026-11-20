using HarmonyLib;

namespace CookFromStorage;

[HarmonyPatch(typeof(Fermenter), "Interact", new[] { typeof(Humanoid), typeof(bool), typeof(bool) })]
internal static class FermenterPatch
{
    [HarmonyPrefix]
    private static bool BeforeInteracted(Fermenter __instance, Humanoid user, bool hold, ref bool __result)
    {
        if (!hold && CookingInputSelection.TryHandle(__instance, user, out bool selectedResult))
        {
            __result = selectedResult;

            return false;
        }

        if (!FermenterTransfer.TryHandleRequest(__instance, user, hold))
        {
            return true;
        }

        __result = true;

        return false;
    }
}