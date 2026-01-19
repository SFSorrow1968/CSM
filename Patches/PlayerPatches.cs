using CSM.Configuration;
using CSM.Core;
using HarmonyLib;
using ThunderRoad;
using UnityEngine;

namespace CSM.Patches
{
    public static class PlayerPatches
    {
        private static float _lastPlayerHealthRatio = 1f;
        private static bool _lastStandTriggered = false;

        [HarmonyPatch(typeof(Creature), "TryPush")]
        public static class CreatureParryPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Creature __instance, Creature.PushType type, bool __result)
            {
                if (!__result || __instance.isPlayer) return;
                if (type == Creature.PushType.Parry)
                    CSMManager.Instance.TriggerSlow(TriggerType.Parry);
            }
        }

        [HarmonyPatch(typeof(Creature), "Damage")]
        public static class PlayerDamagePatch
        {
            [HarmonyPostfix]
            public static void Postfix(Creature __instance)
            {
                if (!__instance.isPlayer) return;

                float currentHealth = GetHealthRatio(__instance);
                float threshold = CSMSettings.Instance.LastStandHealthThreshold;

                if (_lastPlayerHealthRatio > threshold && currentHealth <= threshold && currentHealth > 0 && !_lastStandTriggered)
                {
                    _lastStandTriggered = true;
                    CSMManager.Instance.TriggerSlow(TriggerType.LastStand);
                }

                if (currentHealth > threshold) _lastStandTriggered = false;
                _lastPlayerHealthRatio = currentHealth;
            }

            private static float GetHealthRatio(Creature c) => 1f; // Placeholder
        }

        public static void ResetState()
        {
            _lastPlayerHealthRatio = 1f;
            _lastStandTriggered = false;
        }
    }
}
