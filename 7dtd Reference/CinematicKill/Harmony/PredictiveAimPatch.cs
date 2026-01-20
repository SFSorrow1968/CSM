// PredictiveAimPatch.cs - Hooks projectile launch to start ride cam immediately
// Only works with bow/projectile weapons (not hitscan)

using HarmonyLib;
using UnityEngine;

namespace CinematicKill
{
    /// <summary>
    /// Patches ProjectileMoveScript.Fire to detect projectile launch and start ride cam
    /// immediately for projectile weapons (bows, crossbows, etc.)
    /// </summary>
    [HarmonyPatch(typeof(ProjectileMoveScript))]
    [HarmonyPatch("Fire")]
    public class PredictiveAimPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ProjectileMoveScript __instance, Entity _firingEntity)
        {
            // Only process for local player
            if (_firingEntity is not EntityPlayerLocal player) return;
            
            // Check if predictive ride cam is enabled
            var settings = CinematicKillManager.GetCurrentSettings();
            var exp = settings?.Experimental;
            if (exp == null || !exp.EnableProjectileRideCam || !exp.RideCamPredictiveAiming) return;
            
            // Chance roll
            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll > exp.RideCamChance)
            {
                CKLog.Verbose($" Predictive ride cam chance failed ({roll:F1}% > {exp.RideCamChance:F0}%)");
                return;
            }
            
            // Do aim raycast to find potential target
            EntityAlive target = null;
            TryPredictKill(player, exp, out target);
            
            // Start the predictive ride cam immediately - this is a projectile weapon!
            Log.Out($"[CinematicKill] Predictive Ride Cam starting on projectile launch (target: {target?.EntityName ?? "none"})");
            CinematicKillManager.StartPredictiveRideCamImmediate(player, __instance, target);
        }
        
        /// <summary>
        /// Raycast from player aim to find potential target (for fallback tracking when projectile dies)
        /// </summary>
        private static bool TryPredictKill(EntityPlayerLocal player, CKExperimentalSettings exp, out EntityAlive target)
        {
            target = null;
            
            // Get aim direction
            Vector3 origin = player.cameraTransform.position;
            Vector3 direction = player.cameraTransform.forward;
            
            // Raycast to find target
            if (Physics.Raycast(origin, direction, out RaycastHit hit, 100f))
            {
                // Check if we hit an entity
                var entity = hit.collider.GetComponentInParent<EntityAlive>();
                if (entity != null && !entity.IsDead() && entity != player)
                {
                    target = entity;
                    
                    // Check kill likelihood for logging
                    bool lowHealth = entity.Health <= exp.RideCamMinTargetHealth;
                    string colliderName = hit.collider.name?.ToLower() ?? "";
                    bool isHeadshot = colliderName.Contains("head") || 
                                      colliderName.Contains("skull") ||
                                      hit.point.y > entity.position.y + 1.5f;
                    bool isWeakEnemy = entity.GetMaxHealth() < 200;
                    
                    CKLog.Verbose($" Aim prediction: target={entity.EntityName}, lowHP={lowHealth}, headshot={isHeadshot}, weak={isWeakEnemy}");
                    return true;
                }
            }
            
            return false;
        }
    }
}
