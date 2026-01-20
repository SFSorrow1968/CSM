// WeaponAttackBlockPatch.cs - Blocks weapon attacks during projectile camera to prevent accidental shots

using HarmonyLib;

namespace CinematicKill
{
    /// <summary>
    /// Blocks weapon attacks while the projectile camera is active.
    /// This prevents the "extra damage shot" bug where the camera repositioning
    /// causes the weapon to fire in a different direction while the player
    /// is still holding the fire button.
    /// </summary>
    [HarmonyPatch(typeof(ItemActionRanged), nameof(ItemActionRanged.ExecuteAction))]
    internal static class WeaponAttackBlockPatch
    {
        /// <summary>
        /// Prefix to block ranged weapon execution during projectile camera mode.
        /// </summary>
        [HarmonyPrefix]
        private static bool Prefix()
        {
            // Block attack if projectile camera is active
            if (CinematicKillManager.IsProjectileCameraActive)
            {
                return false; // Skip original method - don't fire
            }
            return true; // Allow normal execution
        }
    }
    
    /// <summary>
    /// Blocks melee attacks while the projectile camera is active.
    /// </summary>
    [HarmonyPatch(typeof(ItemActionMelee), nameof(ItemActionMelee.ExecuteAction))]
    internal static class MeleeAttackBlockPatch
    {
        /// <summary>
        /// Prefix to block melee weapon execution during projectile camera mode.
        /// </summary>
        [HarmonyPrefix]
        private static bool Prefix()
        {
            // Block attack if projectile camera is active
            if (CinematicKillManager.IsProjectileCameraActive)
            {
                return false; // Skip original method - don't attack
            }
            return true; // Allow normal execution
        }
    }
}
