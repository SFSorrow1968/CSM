using System.Collections.Generic;
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
        private static float ThrownKillWindowSeconds => CSMModOptions.ThrownImpactWindowSeconds;
        private static readonly Dictionary<int, float> RecentThrownCreatures = new Dictionary<int, float>();
        private static readonly HashSet<Ragdoll> HookedRagdolls = new HashSet<Ragdoll>();
        private static float _lastCleanupTime = 0f;
        private const float CleanupInterval = 5f;
        private static bool _hooksInitialized = false;

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

                if (!WasKilledByPlayer(__instance) && WasRecentlyThrownByPlayer(__instance))
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Thrown impact kill detected");
                    CSMManager.Instance.TriggerSlow(TriggerType.BasicKill, 0f, __instance);
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

        private static void HookExistingCreatures()
        {
            try
            {
                foreach (var creature in Creature.allActive)
                {
                    HookRagdoll(creature);
                }
            }
            catch { }
        }

        public static void EnsureHooks()
        {
            if (_hooksInitialized) return;
            _hooksInitialized = true;
            HookExistingCreatures();
        }

        private static void HookRagdoll(Creature creature)
        {
            if (creature == null || creature.ragdoll == null) return;
            var ragdoll = creature.ragdoll;
            if (HookedRagdolls.Contains(ragdoll)) return;

            ragdoll.OnUngrabEvent += OnRagdollUngrab;
            ragdoll.OnTelekinesisReleaseEvent += OnRagdollTelekinesisRelease;
            HookedRagdolls.Add(ragdoll);
        }

        private static void OnRagdollUngrab(RagdollHand ragdollHand, HandleRagdoll handleRagdoll, bool lastHandler)
        {
            if (!lastHandler) return;
            bool playerReleased = ragdollHand != null && (ragdollHand.playerHand != null || ragdollHand.creature?.isPlayer == true);
            if (!playerReleased) return;
            var creature = handleRagdoll?.ragdollPart?.ragdoll?.creature;
            MarkCreatureThrown(creature, "Grab");
        }

        private static void OnRagdollTelekinesisRelease(ThunderRoad.Skill.SpellPower.SpellTelekinesis spellTelekinesis, HandleRagdoll handleRagdoll, bool lastHandler)
        {
            if (!lastHandler) return;
            bool playerReleased = spellTelekinesis?.spellCaster?.ragdollHand?.playerHand != null ||
                                  spellTelekinesis?.spellCaster?.ragdollHand?.creature?.isPlayer == true;
            if (!playerReleased) return;
            var creature = handleRagdoll?.ragdollPart?.ragdoll?.creature;
            MarkCreatureThrown(creature, "Telekinesis");
        }

        private static void MarkCreatureThrown(Creature creature, string source)
        {
            if (creature == null || creature.isPlayer) return;
            RecentThrownCreatures[creature.GetInstanceID()] = Time.unscaledTime;
            CleanupThrownCache(Time.unscaledTime);
            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Thrown release recorded (" + source + ", window " + ThrownKillWindowSeconds.ToString("0.###") + "s): " + creature.name);
        }

        private static bool WasRecentlyThrownByPlayer(Creature creature)
        {
            if (creature == null) return false;
            if (!CSMModOptions.EnableBasicKill || !CSMModOptions.EnableThrownImpactKill)
                return false;

            int id = creature.GetInstanceID();
            if (!RecentThrownCreatures.TryGetValue(id, out float releaseTime))
                return false;

            float now = Time.unscaledTime;
            if (now - releaseTime > ThrownKillWindowSeconds)
                return false;

            RecentThrownCreatures.Remove(id);
            return true;
        }

        private static void CleanupThrownCache(float now)
        {
            if (now - _lastCleanupTime < CleanupInterval)
                return;

            _lastCleanupTime = now;
            List<int> expired = null;
            foreach (var kvp in RecentThrownCreatures)
            {
                if (now - kvp.Value > ThrownKillWindowSeconds * 2f)
                {
                    if (expired == null) expired = new List<int>();
                    expired.Add(kvp.Key);
                }
            }

            if (expired == null) return;
            foreach (var key in expired)
                RecentThrownCreatures.Remove(key);
        }
    }
}
