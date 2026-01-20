// ProjectileTrackingPatch.cs - Captures current projectile during collision check
// Sets CinematicKillManager.CurrentProjectile for projectile camera routing

using HarmonyLib;
using UnityEngine;

namespace CinematicKill
{
    /// <summary>
    /// Patches ProjectileMoveScript to track projectile collision events
    /// </summary>
    [HarmonyPatch(typeof(ProjectileMoveScript))]
    [HarmonyPatch("checkCollision")]
    public class ProjectileTrackingPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ProjectileMoveScript __instance)
        {
            // Set the current projectile context before collision check
            CinematicKillManager.CurrentProjectile = __instance;
        }

        [HarmonyPostfix]
        public static void Postfix(ProjectileMoveScript __instance)
        {
            // Clear the context after collision check is done
            CinematicKillManager.CurrentProjectile = null;
        }
    }
}
