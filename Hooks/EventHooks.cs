using System.Linq;
using CSM.Configuration;
using CSM.Core;
using ThunderRoad;
using UnityEngine;

namespace CSM.Hooks
{
    /// <summary>
    /// Event-based hooks using ThunderRoad's EventManager.
    /// Compatible with both PCVR and Nomad (IL2CPP).
    /// Note: Some triggers (parry) may have reduced functionality compared to PCVR Harmony version.
    /// </summary>
    public static class EventHooks
    {
        private static float _lastPlayerHealthRatio = 1f;
        private static bool _lastStandTriggered = false;

        public static void Subscribe()
        {
            EventManager.onCreatureKill += OnCreatureKill;
            EventManager.onCreatureHit += OnCreatureHit;
            Debug.Log("[CSM] Event hooks subscribed.");
        }

        public static void Unsubscribe()
        {
            EventManager.onCreatureKill -= OnCreatureKill;
            EventManager.onCreatureHit -= OnCreatureHit;
            Debug.Log("[CSM] Event hooks unsubscribed.");
        }

        private static void OnCreatureKill(Creature creature, Player player, CollisionInstance collisionInstance, EventTime eventTime)
        {
            // Only process on end of kill event
            if (eventTime == EventTime.OnStart) return;

            if (creature.isPlayer)
            {
                CSMManager.Instance.CancelSlowMotion();
                return;
            }

            // Check if this was the last enemy
            if (IsLastEnemy())
            {
                if (CSMManager.Instance.TriggerSlow(TriggerType.LastEnemy)) return;
            }

            // Check for decapitation/critical (head/neck hit)
            if (collisionInstance?.damageStruct.hitRagdollPart != null)
            {
                var partType = collisionInstance.damageStruct.hitRagdollPart.type;
                if ((partType & (RagdollPart.Type.Head | RagdollPart.Type.Neck)) != 0)
                {
                    // Check if the part was sliced (dismembered)
                    if (collisionInstance.damageStruct.hitRagdollPart.isSliced)
                    {
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Decapitation)) return;
                    }
                    if (CSMManager.Instance.TriggerSlow(TriggerType.Critical)) return;
                }

                // Check for any dismemberment
                if (collisionInstance.damageStruct.hitRagdollPart.isSliced)
                {
                    if (CSMManager.Instance.TriggerSlow(TriggerType.Dismemberment)) return;
                }
            }

            CSMManager.Instance.TriggerSlow(TriggerType.BasicKill);
        }

        private static void OnCreatureHit(Creature creature, CollisionInstance collisionInstance, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart) return;
            if (creature == null) return;

            // Handle player damage for Last Stand
            if (creature.isPlayer)
            {
                float currentHealth = GetHealthRatio(creature);
                float threshold = CSMSettings.Instance.LastStandHealthThreshold;

                if (_lastPlayerHealthRatio > threshold && currentHealth <= threshold && currentHealth > 0 && !_lastStandTriggered)
                {
                    _lastStandTriggered = true;
                    CSMManager.Instance.TriggerSlow(TriggerType.LastStand);
                }

                if (currentHealth > threshold) _lastStandTriggered = false;
                _lastPlayerHealthRatio = currentHealth;
                return;
            }

            // Handle parry detection (enemy was pushed back by player block/parry)
            // This is a simplified detection - checks if enemy was hit during a defensive action
            if (collisionInstance?.damageStruct.damage == 0 &&
                collisionInstance?.sourceColliderGroup?.collisionHandler?.item?.mainHandler?.creature?.isPlayer == true)
            {
                // No damage dealt but player weapon was involved - likely a parry/block
                CSMManager.Instance.TriggerSlow(TriggerType.Parry);
                return;
            }

            // Check for dismemberment on non-lethal hits
            if (collisionInstance?.damageStruct.hitRagdollPart?.isSliced == true)
            {
                var partType = collisionInstance.damageStruct.hitRagdollPart.type;
                if ((partType & (RagdollPart.Type.Head | RagdollPart.Type.Neck)) != 0)
                {
                    CSMManager.Instance.TriggerSlow(TriggerType.Decapitation);
                }
                else
                {
                    CSMManager.Instance.TriggerSlow(TriggerType.Dismemberment);
                }
            }
        }

        private static bool IsLastEnemy()
        {
            return Creature.allActive.Count(c => !c.isPlayer && !c.isKilled) == 0;
        }

        private static float GetHealthRatio(Creature creature)
        {
            if (creature?.currentHealth == null || creature.maxHealth <= 0) return 1f;
            return creature.currentHealth / creature.maxHealth;
        }

        public static void ResetState()
        {
            _lastPlayerHealthRatio = 1f;
            _lastStandTriggered = false;
        }
    }
}
