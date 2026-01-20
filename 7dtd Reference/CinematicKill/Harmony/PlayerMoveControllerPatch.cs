// PlayerMoveControllerPatch.cs - Hooks PlayerMoveController.Update for timer management
// Calls: CinematicKillManager.HandleUpdate() every frame (cooldowns, duration, ragdoll detection)

using HarmonyLib;

namespace CinematicKill
{
    [HarmonyPatch(typeof(PlayerMoveController), nameof(PlayerMoveController.Update))]
    internal static class PlayerMoveControllerPatch
    {
        [HarmonyPostfix]
        private static void Postfix(PlayerMoveController __instance)
        {
            CinematicKillManager.HandleUpdate(__instance);
        }
    }
}
