// CameraUpdatePatch.cs - Hooks EntityPlayerLocal.LateUpdate for projectile camera positioning
// Calls: CinematicKillManager.HandleCameraUpdate() every frame

using HarmonyLib;
using UnityEngine;

namespace CinematicKill
{
    [HarmonyPatch(typeof(EntityPlayerLocal), "LateUpdate")]
    public class CameraUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
        {
            if (__instance != null && __instance.playerCamera != null)
            {
                CinematicKillManager.HandleCameraUpdate();
            }
        }
    }
}
