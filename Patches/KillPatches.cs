using System.Linq;
using CSM.Configuration;
using CSM.Core;
using HarmonyLib;
using ThunderRoad;
using UnityEngine;

namespace CSM.Patches
{
    public static class KillPatches
    {
        [HarmonyPatch(typeof(Creature), "Kill")]
        public static class CreatureKillPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Creature __instance)
            {
                if (__instance.isPlayer)
                {
                    CSMManager.Instance.CancelSlowMotion();
                    return;
                }

                if (IsLastEnemy())
                {
                    if (CSMManager.Instance.TriggerSlow(TriggerType.LastEnemy, 0f, __instance)) return;
                }

                if (!WasKilledByPlayer(__instance) && ThrowTracker.WasImpactThisFrame(__instance))
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Thrown impact kill detected");
                    CSMManager.Instance.TriggerSlow(TriggerType.BasicKill, 0f, __instance);
                    return;
                }

                if (!WasKilledByPlayer(__instance))
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Kill skipped - not killed by player");
                    return;
                }

                if (WasCriticalKill(__instance))
                {
                    if (CSMManager.Instance.TriggerSlow(TriggerType.Critical, 0f, __instance)) return;
                }

                CSMManager.Instance.TriggerSlow(TriggerType.BasicKill, 0f, __instance);
            }

            private static bool IsLastEnemy()
            {
                return Creature.allActive.Count(c => !c.isPlayer && !c.isKilled) == 0;
            }

            private static bool WasCriticalKill(Creature creature)
            {
                if (creature.ragdoll == null) return false;
                foreach (var part in creature.ragdoll.parts)
                {
                    if (part != null && (part.type & (RagdollPart.Type.Head | RagdollPart.Type.Neck)) != 0)
                        return true;
                }
                return false;
            }

            private static bool WasKilledByPlayer(Creature creature)
            {
                try
                {
                    if (creature == null) return false;
                    CollisionInstance lastDamage = null;
                    try
                    {
                        lastDamage = creature.lastDamage;
                    }
                    catch { }

                    if (lastDamage == null) return false;

                    var sourceColliderGroup = lastDamage.sourceColliderGroup;
                    if (sourceColliderGroup?.collisionHandler?.item?.mainHandler?.creature?.isPlayer == true)
                        return true;
                    if (sourceColliderGroup?.collisionHandler?.item?.lastHandler?.creature?.isPlayer == true)
                        return true;
                    if (sourceColliderGroup?.collisionHandler?.ragdollPart?.ragdoll?.creature?.isPlayer == true)
                        return true;
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
