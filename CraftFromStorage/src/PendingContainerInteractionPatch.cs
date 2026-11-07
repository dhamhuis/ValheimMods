using HarmonyLib;

namespace CraftFromStorage;

[HarmonyPatch(typeof(Container), nameof(Container.Interact))]
internal static class PendingContainerInteractionPatch
{
    [HarmonyPrefix]
    private static bool BeforeInteract(Container __instance, Humanoid character, bool hold, ref bool __result)
    {
        if (!StorageCrafting.IsBusy(__instance))
        {
            StorageCrafting.PrepareManualOpen(__instance, character, hold);

            return true;
        }

        character.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null, false);
        __result = true;

        return false;
    }
}