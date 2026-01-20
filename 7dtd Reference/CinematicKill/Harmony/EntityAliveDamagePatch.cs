// ═══════════════════════════════════════════════════════════════════════════════
// EntityAliveDamagePatch.cs - Core Harmony patches for kill detection
// ═══════════════════════════════════════════════════════════════════════════════
//
// PATCHES IN THIS FILE:
//
//   EntityAliveDamagePatch (EntityAlive.DamageEntity)
//     - Prefix: Captures pre-damage state (wasAlive, sneakAttack detection)
//     - Postfix: Calls HandleDamageResponse() to evaluate cinematic triggers
//
//   DismembermentManagerPatch (DismembermentManager.DismemberPart)
//     - Postfix: Calls HandleDismember() for dismemberment cinematics
//
//   EntityAliveDeathPatch (EntityAlive.OnEntityDeath)
//     - Postfix: Calls HandleEntityDeath() as fallback death detection
//
//   EntityAliveSetDeadPatch (EntityAlive.SetDead)
//     - Postfix: Captures ragdoll transform via SetRagdollTarget() for camera tracking
//
// ═══════════════════════════════════════════════════════════════════════════════

using HarmonyLib;
using UnityEngine;

namespace CinematicKill
{
    [HarmonyPatch(typeof(EntityAlive), nameof(EntityAlive.DamageEntity), new[] { typeof(DamageSource), typeof(int), typeof(bool), typeof(float) })]
    internal static class EntityAliveDamagePatch
    {
        internal struct DamageState
        {
            public bool WasAlive;
            public bool WasSneakAttack; // Capture sneak state BEFORE damage alerts the target
        }

        [HarmonyPrefix]
        private static void Prefix(EntityAlive __instance, DamageSource _damageSource, int _strength, out DamageState __state)
        {
            __state = default;
            if (__instance != null)
            {
                __state.WasAlive = !__instance.IsDead();
                
                // Capture sneak state BEFORE damage - target becomes aware after taking damage
                // A sneak attack occurs when the target's revenge/attack targets are NOT the attacker
                if (_damageSource is DamageSourceEntity dse)
                {
                    var world = GameManager.Instance?.World;
                    var attacker = world?.GetEntity(dse.getEntityId());
                    if (attacker != null)
                    {
                        Entity revengeTarget = __instance.GetRevengeTarget();
                        Entity attackTarget = __instance.GetAttackTarget();
                        
                        bool noRevengeOnAttacker = (revengeTarget == null || revengeTarget.entityId != attacker.entityId);
                        bool noAttackOnAttacker = (attackTarget == null || attackTarget.entityId != attacker.entityId);
                        
                        __state.WasSneakAttack = noRevengeOnAttacker && noAttackOnAttacker;
                    }
                }
                
                // Check if this will be a killing blow - only capture projectile reference
                // The actual cinematic triggering happens in Postfix via HandleDamageResponse
                if (__state.WasAlive && _damageSource is DamageSourceEntity)
                {
                    bool willDie = __instance.Health <= _strength;
                    if (willDie)
                    {
                        // Queue projectile camera for trigger-based routing in HandleDamageResponse
                        if (CinematicKillManager.CurrentProjectile != null)
                        {
                            CinematicKillManager.QueueProjectileCamera(CinematicKillManager.CurrentProjectile, __instance, _damageSource);
                        }
                        // NOTE: Hitscan camera is now handled by HandleDamageResponse in Postfix
                        // This ensures proper trigger evaluation (BasicKill, Headshot, etc.)
                    }
                }
            }
        }

        [HarmonyPostfix]
        private static void Postfix(EntityAlive __instance, DamageSource _damageSource, int _strength, bool _criticalHit, float _impulseScale, DamageState __state)
        {
            // ═══════════════════════════════════════════════════════════════════════
            // EXPERIMENTAL: Last Stand - Check if PLAYER is about to die
            // ═══════════════════════════════════════════════════════════════════════
            if (__instance is EntityPlayerLocal player)
            {
                CinematicKillManager.HandlePlayerDamage(player, _strength);
            }
            
            CinematicKillManager.HandleDamageResponse(__instance, _damageSource, __state.WasAlive, _criticalHit, __state.WasSneakAttack);
        }
    }

    [HarmonyPatch(typeof(DismembermentManager), nameof(DismembermentManager.DismemberPart))]
    internal static class DismembermentManagerPatch
    {
        [HarmonyPostfix]
        private static void Postfix(EntityAlive _entity)
        {
            if (_entity != null)
            {
                CinematicKillManager.HandleDismember(_entity);
            }
        }
    }

    /// <summary>
    /// Hooks into EntityAlive.OnEntityDeath for more reliable death detection
    /// This fires after the entity's death state is fully established
    /// </summary>
    [HarmonyPatch(typeof(EntityAlive), nameof(EntityAlive.OnEntityDeath))]
    internal static class EntityAliveDeathPatch
    {
        [HarmonyPostfix]
        private static void Postfix(EntityAlive __instance)
        {
            if (__instance == null) return;
            
            // Notify the manager that an entity has definitively died
            // This allows for fallback cinematic triggering if damage-based detection missed
            CinematicKillManager.HandleEntityDeath(__instance);
        }
    }

    /// <summary>
    /// Hooks into EntityAlive.SetDead to capture ragdoll transform for camera tracking
    /// This fires when the entity transitions to dead state and ragdoll is created
    /// </summary>
    [HarmonyPatch(typeof(EntityAlive), nameof(EntityAlive.SetDead))]
    internal static class EntityAliveSetDeadPatch
    {
        [HarmonyPostfix]
        private static void Postfix(EntityAlive __instance)
        {
            if (__instance == null) return;
            
            // Capture entity model transform for projectile camera tracking
            // This allows the camera to follow the body as it ragdolls
            Transform ragdollRoot = null;
            
            try
            {
                // Use entity model transform - this follows the ragdoll physics
                if (__instance.emodel != null)
                {
                    ragdollRoot = __instance.emodel.transform;
                }
                else
                {
                    // Fallback to entity transform
                    ragdollRoot = __instance.transform;
                }
            }
            catch { }
            
            if (ragdollRoot != null)
            {
                CinematicKillManager.SetRagdollTarget(__instance.entityId, ragdollRoot);
            }
        }
    }
}
