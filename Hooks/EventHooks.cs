using System;
using CSM.Configuration;
using CSM.Core;
using ThunderRoad;
using UnityEngine;

namespace CSM.Hooks
{
    /// <summary>
    /// Event-based hooks for Nomad (IL2CPP compatible).
    /// Uses instance methods like working reference mods (KillOnPress, CarnageReborn).
    /// </summary>
    public class EventHooks
    {
        private static EventHooks _instance;
        private bool _subscribed = false;
        private bool _deflectSubscribed = false;
        private bool _parrySubscribed = false;
        private float _lastPlayerHealthRatio = 1f;
        private bool _lastStandTriggered = false;
        
        // Track enemies for smarter "Last Enemy" detection
        private int _maxEnemiesSeenThisWave = 0;
        private float _lastWaveResetTime = 0f;
        private const float WAVE_RESET_TIMEOUT = 10f; // Reset wave tracking after 10s with no enemies

        public static void Subscribe()
        {
            if (_instance == null)
            {
                _instance = new EventHooks();
            }
            _instance.SubscribeEvents();
        }

        public static void SubscribeDeflect()
        {
            if (_instance == null)
            {
                _instance = new EventHooks();
            }
            _instance.SubscribeDeflectEvent();
            _instance.SubscribeParryEvent();
        }

        public static void Unsubscribe()
        {
            _instance?.UnsubscribeEvents();
        }

        public static void ResetState()
        {
            if (_instance != null)
            {
                _instance._lastPlayerHealthRatio = 1f;
                _instance._lastStandTriggered = false;
                _instance._maxEnemiesSeenThisWave = 0;
                _instance._lastWaveResetTime = 0f;
            }
        }

        private void SubscribeEvents()
        {
            if (_subscribed)
            {
                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Already subscribed to events");
                return;
            }

            Debug.Log("[CSM] Subscribing to EventManager events...");

            try
            {
                EventManager.onCreatureKill += new EventManager.CreatureKillEvent(this.OnCreatureKill);
                EventManager.onCreatureHit += new EventManager.CreatureHitEvent(this.OnCreatureHit);
                EventManager.onDeflect += new EventManager.DeflectEvent(this.OnDeflect);
                EventManager.onCreatureAttackParry += new EventManager.CreatureParryEvent(this.OnCreatureAttackParry);
                _subscribed = true;
                _deflectSubscribed = true;
                _parrySubscribed = true;
                Debug.Log("[CSM] Event hooks subscribed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] Failed to subscribe to events: " + ex.Message);
                _subscribed = false;
            }
        }

        private void SubscribeDeflectEvent()
        {
            if (_deflectSubscribed) return;

            try
            {
                EventManager.onDeflect += new EventManager.DeflectEvent(this.OnDeflect);
                _deflectSubscribed = true;
                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Deflect hook subscribed");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] Failed to subscribe deflect hook: " + ex.Message);
            }
        }

        private void SubscribeParryEvent()
        {
            if (_parrySubscribed) return;

            try
            {
                EventManager.onCreatureAttackParry += new EventManager.CreatureParryEvent(this.OnCreatureAttackParry);
                _parrySubscribed = true;
                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Parry hook subscribed");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] Failed to subscribe parry hook: " + ex.Message);
            }
        }

        private void UnsubscribeEvents()
        {
            Debug.Log("[CSM] Unsubscribing from events...");

            try
            {
                EventManager.onCreatureKill -= new EventManager.CreatureKillEvent(this.OnCreatureKill);
                EventManager.onCreatureHit -= new EventManager.CreatureHitEvent(this.OnCreatureHit);
                EventManager.onDeflect -= new EventManager.DeflectEvent(this.OnDeflect);
                EventManager.onCreatureAttackParry -= new EventManager.CreatureParryEvent(this.OnCreatureAttackParry);
            }
            catch { }

            _subscribed = false;
            _deflectSubscribed = false;
            _parrySubscribed = false;
        }

        private void OnCreatureKill(Creature creature, Player player, CollisionInstance collisionInstance, EventTime eventTime)
        {
            try
            {
                // Only process on end of kill event
                if (eventTime == EventTime.OnStart) return;
                if (creature == null) return;

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] CreatureKill event: " + creature.name);

                // Cancel slow motion if player dies
                if (creature.isPlayer)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Player died, cancelling slow motion");
                    CSMManager.Instance.CancelSlowMotion();
                    return;
                }

                // Player-Only filter: skip NPC vs NPC kills
                if (!WasKilledByPlayer(collisionInstance))
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Kill skipped - NPC vs NPC");
                    return;
                }

                // Update enemy tracking for "Last Enemy" detection
                UpdateEnemyTracking();

                // Get current alive enemies BEFORE processing
                int aliveEnemies = CountAliveEnemies();
                
                // Check if this was the last enemy of a meaningful group
                bool isLastEnemy = IsSmartLastEnemy(aliveEnemies);
                
                if (isLastEnemy)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Last enemy of wave killed (wave had " + _maxEnemiesSeenThisWave + " enemies)");

                    bool triggered = CSMManager.Instance.TriggerSlow(TriggerType.LastEnemy, 0f, creature);
                    if (triggered)
                    {
                        // Reset wave tracking after successful last enemy trigger
                        _maxEnemiesSeenThisWave = 0;
                        _lastWaveResetTime = Time.unscaledTime;
                        return;
                    }
                }

                // Get damage dealt for dynamic intensity
                float damageDealt = collisionInstance?.damageStruct.damage ?? 0f;

                // Check for decapitation/critical hit
                if (collisionInstance != null && collisionInstance.damageStruct.hitRagdollPart != null)
                {
                    var part = collisionInstance.damageStruct.hitRagdollPart;
                    var partType = part.type;
                    bool isHeadOrNeck = (partType & (RagdollPart.Type.Head | RagdollPart.Type.Neck)) != 0;

                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Hit part: " + partType + " isHeadOrNeck=" + isHeadOrNeck + " isSliced=" + part.isSliced + " damage=" + damageDealt);

                    // Decapitation takes priority over critical
                    if (isHeadOrNeck && part.isSliced)
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Decapitation detected");
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Decapitation, damageDealt, creature))
                            return;
                    }

                    // Critical kill (head/neck hit without slicing)
                    if (isHeadOrNeck)
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Critical kill detected");
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Critical, damageDealt, creature))
                            return;
                    }

                    // Non-head dismemberment
                    if (part.isSliced)
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Dismemberment detected");
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Dismemberment, damageDealt, creature))
                            return;
                    }
                }

                // Basic kill - lowest priority
                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Basic kill with damage=" + damageDealt);
                CSMManager.Instance.TriggerSlow(TriggerType.BasicKill, damageDealt, creature);
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] OnCreatureKill error: " + ex.Message);
            }
        }

        private void OnCreatureHit(Creature creature, CollisionInstance collisionInstance, EventTime eventTime)
        {
            try
            {
                if (eventTime == EventTime.OnStart) return;
                if (creature == null) return;

                // Handle player damage for Last Stand
                if (creature.isPlayer)
                {
                    HandlePlayerHit(creature);
                    return;
                }

                // Check for dismemberment on non-lethal hits
                if (collisionInstance != null &&
                    collisionInstance.damageStruct.hitRagdollPart != null &&
                    collisionInstance.damageStruct.hitRagdollPart.isSliced)
                {
                    float damageDealt = collisionInstance.damageStruct.damage;
                    var partType = collisionInstance.damageStruct.hitRagdollPart.type;
                    if ((partType & (RagdollPart.Type.Head | RagdollPart.Type.Neck)) != 0)
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Non-lethal decapitation detected, damage=" + damageDealt);
                        CSMManager.Instance.TriggerSlow(TriggerType.Decapitation, damageDealt, creature);
                    }
                    else
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Non-lethal dismemberment detected, damage=" + damageDealt);
                        CSMManager.Instance.TriggerSlow(TriggerType.Dismemberment, damageDealt, creature);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] OnCreatureHit error: " + ex.Message);
            }
        }

        private void HandlePlayerHit(Creature player)
        {
            try
            {
                float currentHealth = GetHealthRatio(player);
                float threshold = CSMModOptions.LastStandThreshold;

                // Trigger Last Stand when health drops below threshold
                if (_lastPlayerHealthRatio > threshold && currentHealth <= threshold && currentHealth > 0 && !_lastStandTriggered)
                {
                    _lastStandTriggered = true;
                    Debug.Log("[CSM] Last Stand triggered! Health: " + (currentHealth * 100f).ToString("F0") + "%");
                    CSMManager.Instance.TriggerSlow(TriggerType.LastStand);
                }

                // Reset Last Stand trigger when health goes back above threshold
                if (currentHealth > threshold)
                {
                    _lastStandTriggered = false;
                }

                _lastPlayerHealthRatio = currentHealth;
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] HandlePlayerHit error: " + ex.Message);
            }
        }

        /// <summary>
        /// Update tracking of how many enemies we've seen in the current "wave"
        /// </summary>
        private void UpdateEnemyTracking()
        {
            int currentEnemies = CountAliveEnemies();
            
            // We're inside a kill event, so add 1 for the enemy that just died
            // This ensures we count ALL enemies in the wave, including the one being killed
            int totalInWave = currentEnemies + 1;
            
            // Check if we should reset wave tracking (been at 0 enemies for a while)
            if (currentEnemies == 0 && _maxEnemiesSeenThisWave > 0)
            {
                // Don't reset immediately - the IsSmartLastEnemy check happens after this
                // Only reset after the timeout
                if (Time.unscaledTime - _lastWaveResetTime > WAVE_RESET_TIMEOUT)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Wave tracking reset after timeout");
                    _maxEnemiesSeenThisWave = 0;
                }
            }
            
            // Always track max enemies (including the one just killed)
            if (totalInWave > _maxEnemiesSeenThisWave)
            {
                _maxEnemiesSeenThisWave = totalInWave;
                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Wave tracking: max enemies = " + _maxEnemiesSeenThisWave);
            }
            
            // Reset the timer when there are still enemies alive
            if (currentEnemies > 0)
            {
                _lastWaveResetTime = Time.unscaledTime;
            }
        }

        /// <summary>
        /// Smart "Last Enemy" detection - only triggers if there was actually a group of enemies
        /// </summary>
        private bool IsSmartLastEnemy(int currentAliveEnemies)
        {
            // Only trigger if:
            // 1. No enemies are currently alive (counting the one that just died)
            // 2. We saw at least 2 enemies in this wave (configurable threshold)
            
            if (currentAliveEnemies > 0)
                return false;
            
            // Require at least 2 enemies for "Last Enemy" to trigger
            // This prevents it from firing on every kill in endless 1v1 mode
            int minEnemies = CSMModOptions.LastEnemyMinimumGroup;
            
            bool isLast = _maxEnemiesSeenThisWave >= minEnemies;
            
            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] IsSmartLastEnemy: alive=" + currentAliveEnemies + " maxWave=" + _maxEnemiesSeenThisWave + " minRequired=" + minEnemies + " result=" + isLast);
            
            return isLast;
        }

        private int CountAliveEnemies()
        {
            try
            {
                if (Creature.allActive == null) return 0;

                int aliveEnemies = 0;
                foreach (var c in Creature.allActive)
                {
                    if (c != null && !c.isPlayer && !c.isKilled)
                    {
                        aliveEnemies++;
                    }
                }
                return aliveEnemies;
            }
            catch
            {
                return 0;
            }
        }

        private float GetHealthRatio(Creature creature)
        {
            try
            {
                if (creature == null) return 1f;
                if (creature.maxHealth <= 0) return 1f;
                return Mathf.Clamp01(creature.currentHealth / creature.maxHealth);
            }
            catch
            {
                return 1f;
            }
        }

        /// <summary>
        /// Check if a kill was caused by the player (directly or via held item).
        /// </summary>
        private bool WasKilledByPlayer(CollisionInstance collision)
        {
            try
            {
                if (collision == null) return false;

                // Check if the source creature is the player
                if (collision.sourceColliderGroup?.collisionHandler?.item?.mainHandler?.creature?.isPlayer == true)
                    return true;

                // Check if the source item is held by the player
                if (collision.sourceColliderGroup?.collisionHandler?.item?.lastHandler?.creature?.isPlayer == true)
                    return true;

                // Check if damage came from player body part
                if (collision.sourceColliderGroup?.collisionHandler?.ragdollPart?.ragdoll?.creature?.isPlayer == true)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Handle deflect events for parry detection.
        /// Fires when a weapon successfully deflects/parries an attack.
        /// </summary>
        private void OnDeflect(Creature source, Item deflectingItem, Creature target)
        {
            try
            {
                // Only trigger parry for player deflects
                bool playerDeflected = deflectingItem?.mainHandler?.creature?.isPlayer == true ||
                                       deflectingItem?.lastHandler?.creature?.isPlayer == true;

                if (!playerDeflected)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Deflect skipped - not player weapon (item=" + (deflectingItem ? deflectingItem.name : "null") + ")");
                    return;
                }

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Parry detected via deflect event (item=" + (deflectingItem ? deflectingItem.name : "null") + ")");

                CSMManager.Instance.TriggerSlow(TriggerType.Parry);
            }
            catch (Exception ex)
            {
                if (CSMModOptions.DebugLogging)
                    Debug.LogError("[CSM] OnDeflect error: " + ex.Message);
            }
        }

        private void OnCreatureAttackParry(Creature attacker, Item attackerItem, Creature defender, Item defenderItem, CollisionInstance collisionInstance)
        {
            try
            {
                bool playerParried = defender?.isPlayer == true ||
                                     defenderItem?.mainHandler?.creature?.isPlayer == true ||
                                     defenderItem?.lastHandler?.creature?.isPlayer == true;

                if (!playerParried)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Parry skipped - defender not player (attacker=" + (attacker ? attacker.name : "null") + ")");
                    return;
                }

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Parry detected via attack parry event (attacker=" + (attacker ? attacker.name : "null") + ", defender=" + (defender ? defender.name : "null") + ")");

                CSMManager.Instance.TriggerSlow(TriggerType.Parry);
            }
            catch (Exception ex)
            {
                if (CSMModOptions.DebugLogging)
                    Debug.LogError("[CSM] OnCreatureAttackParry error: " + ex.Message);
            }
        }
    }
}
