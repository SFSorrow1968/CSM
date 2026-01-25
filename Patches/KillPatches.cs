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
        }
    }
}
