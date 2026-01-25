using CSM.Configuration;
using CSM.Core;
using HarmonyLib;
using ThunderRoad;
using UnityEngine;

namespace CSM.Patches
{
    public static class DismemberPatches
    {
        [HarmonyPatch(typeof(RagdollPart), "TrySlice")]
        public static class RagdollPartSlicePatch
        {
            [HarmonyPostfix]
            public static void Postfix(RagdollPart __instance, bool __result)
            {
                if (!__result) return;
                if (__instance.ragdoll?.creature?.isPlayer == true) return;

                if (((__instance.type & (RagdollPart.Type.Head | RagdollPart.Type.Neck)) != 0))
                {
                    if (CSMManager.Instance.TriggerSlow(TriggerType.Decapitation, 0f, __instance.ragdoll?.creature)) return;
                }

                CSMManager.Instance.TriggerSlow(TriggerType.Dismemberment, 0f, __instance.ragdoll?.creature);
            }
        }
    }
}
