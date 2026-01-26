using System;
using System.Collections.Generic;
using CSM.Configuration;
using CSM.Core;
using ThunderRoad;
using UnityEngine;

namespace CSM.Hooks
{
    public class EventHooks
    {
        private static EventHooks _instance;
        private bool _subscribed = false;
        private bool _deflectSubscribed = false;
        private bool _parrySubscribed = false;
        private bool _spawnSubscribed = false;
        private float _lastPlayerHealthRatio = 1f;
        private bool _lastStandTriggered = false;
        
        private int _maxEnemiesSeenThisWave = 0;
        private float _lastWaveResetTime = 0f;
        private const float WAVE_RESET_TIMEOUT = 10f;
        private readonly Dictionary<int, float> _recentSlicedParts = new Dictionary<int, float>();
        private float _lastSliceCleanupTime = 0f;
        private const float SLICE_REARM_SECONDS = 30f;
        private const float SLICE_CLEANUP_INTERVAL = 10f;
        private readonly HashSet<Ragdoll> _hookedRagdolls = new HashSet<Ragdoll>();

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

        public static void SubscribeThrowTracking()
        {
            if (_instance == null)
            {
                _instance = new EventHooks();
            }
            _instance.SubscribeSpawnEvent();
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
                _instance._recentSlicedParts.Clear();
                _instance._lastSliceCleanupTime = 0f;
                _instance._hookedRagdolls.Clear();
                ThrowTracker.Reset();
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
                SubscribeSpawnEvent();
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

        private void SubscribeSpawnEvent()
        {
            if (_spawnSubscribed) return;

            try
            {
                EventManager.onCreatureSpawn += new EventManager.CreatureSpawnedEvent(this.OnCreatureSpawn);
                _spawnSubscribed = true;
                RegisterExistingCreatures();
                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Creature spawn hook subscribed");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] Failed to subscribe creature spawn hook: " + ex.Message);
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
                if (_spawnSubscribed)
                {
                    EventManager.onCreatureSpawn -= new EventManager.CreatureSpawnedEvent(this.OnCreatureSpawn);
                }
            }
            catch { }

            _subscribed = false;
            _deflectSubscribed = false;
            _parrySubscribed = false;
            _spawnSubscribed = false;
            UnregisterAllRagdollHooks();
        }

        private void RegisterExistingCreatures()
        {
            try
            {
                if (Creature.allActive == null) return;
                foreach (var creature in Creature.allActive)
                {
                    RegisterRagdollHooks(creature);
                }
            }
            catch { }
        }

        private void OnCreatureSpawn(Creature creature)
        {
            if (creature == null) return;
            RegisterRagdollHooks(creature);
        }

        private void RegisterRagdollHooks(Creature creature)
        {
            if (creature == null || creature.ragdoll == null) return;

            var ragdoll = creature.ragdoll;
            if (_hookedRagdolls.Contains(ragdoll)) return;

            ragdoll.OnUngrabEvent += OnRagdollUngrab;
            ragdoll.OnTelekinesisReleaseEvent += OnRagdollTelekinesisRelease;
            _hookedRagdolls.Add(ragdoll);
        }

        private void UnregisterAllRagdollHooks()
        {
            foreach (var ragdoll in _hookedRagdolls)
            {
                if (ragdoll == null) continue;
                ragdoll.OnUngrabEvent -= OnRagdollUngrab;
                ragdoll.OnTelekinesisReleaseEvent -= OnRagdollTelekinesisRelease;
            }
            _hookedRagdolls.Clear();
        }

        private void OnRagdollUngrab(RagdollHand ragdollHand, HandleRagdoll handleRagdoll, bool lastHandler)
        {
            if (!lastHandler) return;

            bool playerReleased = ragdollHand != null && (ragdollHand.playerHand != null || ragdollHand.creature?.isPlayer == true);
            if (!playerReleased) return;

            var creature = handleRagdoll?.ragdollPart?.ragdoll?.creature;
            ThrowTracker.RecordThrow(creature, "Grab");
        }

        private void OnRagdollTelekinesisRelease(ThunderRoad.Skill.SpellPower.SpellTelekinesis spellTelekinesis, HandleRagdoll handleRagdoll, bool lastHandler)
        {
            if (!lastHandler) return;

            bool playerReleased = spellTelekinesis?.spellCaster?.ragdollHand?.playerHand != null ||
                                  spellTelekinesis?.spellCaster?.ragdollHand?.creature?.isPlayer == true;
            if (!playerReleased) return;

            var creature = handleRagdoll?.ragdollPart?.ragdoll?.creature;
            ThrowTracker.RecordThrow(creature, "Telekinesis");
        }

        private void OnCreatureKill(Creature creature, Player player, CollisionInstance collisionInstance, EventTime eventTime)
        {
            try
            {
                if (eventTime == EventTime.OnStart) return;
                if (creature == null) return;

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] CreatureKill event: " + creature.name);

                if (creature.isPlayer)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Player died, cancelling slow motion");
                    CSMManager.Instance.CancelSlowMotion();
                    return;
                }

                bool killedByPlayer = WasKilledByPlayer(collisionInstance);
                bool thrownImpactKill = false;
                if (!killedByPlayer)
                {
                    thrownImpactKill = ThrowTracker.WasImpactThisFrame(creature);
                    if (!thrownImpactKill)
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Kill skipped - not player and not thrown impact");
                        return;
                    }
                }

                UpdateEnemyTracking();

                int aliveEnemies = CountAliveEnemies();
                
                bool isLastEnemy = IsSmartLastEnemy(aliveEnemies);
                
                if (isLastEnemy)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Last enemy of wave killed (wave had " + _maxEnemiesSeenThisWave + " enemies)");

                    bool triggered = CSMManager.Instance.TriggerSlow(TriggerType.LastEnemy, 0f, creature);
                    if (triggered)
                    {
                        _maxEnemiesSeenThisWave = 0;
                        _lastWaveResetTime = Time.unscaledTime;
                        return;
                    }
                }

                float damageDealt = collisionInstance?.damageStruct.damage ?? 0f;

                if (thrownImpactKill)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Thrown impact kill detected");
                    CSMManager.Instance.TriggerSlow(TriggerType.BasicKill, damageDealt, creature);
                    return;
                }

                if (collisionInstance != null && collisionInstance.damageStruct.hitRagdollPart != null)
                {
                    var part = collisionInstance.damageStruct.hitRagdollPart;
                    var partType = part.type;
                    bool isHeadOrNeck = (partType & (RagdollPart.Type.Head | RagdollPart.Type.Neck)) != 0;

                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Hit part: " + partType + " isHeadOrNeck=" + isHeadOrNeck + " isSliced=" + part.isSliced + " damage=" + damageDealt);

                    if (isHeadOrNeck && part.isSliced)
                    {
                        if (!IsNewSlice(part))
                        {
                            if (CSMModOptions.DebugLogging)
                                Debug.Log("[CSM] Decapitation ignored (already handled): " + partType);
                            return;
                        }
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Decapitation detected");
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Decapitation, damageDealt, creature))
                            return;
                    }

                    if (isHeadOrNeck)
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Critical kill detected");
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Critical, damageDealt, creature))
                            return;
                    }

                    if (part.isSliced)
                    {
                        if (!IsNewSlice(part))
                        {
                            if (CSMModOptions.DebugLogging)
                                Debug.Log("[CSM] Dismemberment ignored (already handled): " + partType);
                            return;
                        }
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Dismemberment detected");
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Dismemberment, damageDealt, creature))
                            return;
                    }
                }

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

                if (creature.isPlayer)
                {
                    HandlePlayerHit(creature);
                    return;
                }

                if (creature.isKilled)
                    return;

                if (collisionInstance != null && !WasKilledByPlayer(collisionInstance))
                {
                    ThrowTracker.RecordImpact(creature);
                }

                if (collisionInstance != null &&
                    collisionInstance.damageStruct.hitRagdollPart != null &&
                    collisionInstance.damageStruct.hitRagdollPart.isSliced)
                {
                    float damageDealt = collisionInstance.damageStruct.damage;
                    var part = collisionInstance.damageStruct.hitRagdollPart;
                    var partType = part.type;
                    if (!IsNewSlice(part))
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Slice ignored (already handled): " + partType);
                        return;
                    }
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

        private bool IsNewSlice(RagdollPart part)
        {
            if (part == null) return false;
            int id = part.GetInstanceID();
            float now = Time.unscaledTime;

            if (_recentSlicedParts.TryGetValue(id, out float lastTime) && now - lastTime < SLICE_REARM_SECONDS)
                return false;

            _recentSlicedParts[id] = now;
            CleanupSliceCache(now);
            return true;
        }

        private void CleanupSliceCache(float now)
        {
            if (now - _lastSliceCleanupTime < SLICE_CLEANUP_INTERVAL)
                return;

            _lastSliceCleanupTime = now;
            List<int> expired = null;
            foreach (var kvp in _recentSlicedParts)
            {
                if (now - kvp.Value > SLICE_REARM_SECONDS)
                {
                    if (expired == null) expired = new List<int>();
                    expired.Add(kvp.Key);
                }
            }

            if (expired == null) return;
            foreach (var key in expired)
                _recentSlicedParts.Remove(key);
        }

        private void HandlePlayerHit(Creature player)
        {
            try
            {
                float currentHealth = GetHealthRatio(player);
                float threshold = CSMModOptions.LastStandThreshold;

                if (_lastPlayerHealthRatio > threshold && currentHealth <= threshold && currentHealth > 0 && !_lastStandTriggered)
                {
                    _lastStandTriggered = true;
                    Debug.Log("[CSM] Last Stand triggered! Health: " + (currentHealth * 100f).ToString("F0") + "%");
                    CSMManager.Instance.TriggerSlow(TriggerType.LastStand);
                }

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

        private void UpdateEnemyTracking()
        {
            int currentEnemies = CountAliveEnemies();
            
            int totalInWave = currentEnemies + 1;
            
            if (currentEnemies == 0 && _maxEnemiesSeenThisWave > 0)
            {
                if (Time.unscaledTime - _lastWaveResetTime > WAVE_RESET_TIMEOUT)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Wave tracking reset after timeout");
                    _maxEnemiesSeenThisWave = 0;
                }
            }
            
            if (totalInWave > _maxEnemiesSeenThisWave)
            {
                _maxEnemiesSeenThisWave = totalInWave;
                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Wave tracking: max enemies = " + _maxEnemiesSeenThisWave);
            }
            
            if (currentEnemies > 0)
            {
                _lastWaveResetTime = Time.unscaledTime;
            }
        }

        private bool IsSmartLastEnemy(int currentAliveEnemies)
        {
            
            if (currentAliveEnemies > 0)
                return false;
            
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

        private bool WasKilledByPlayer(CollisionInstance collision)
        {
            try
            {
                if (collision == null) return false;

                if (collision.sourceColliderGroup?.collisionHandler?.item?.mainHandler?.creature?.isPlayer == true)
                    return true;

                if (collision.sourceColliderGroup?.collisionHandler?.item?.lastHandler?.creature?.isPlayer == true)
                    return true;

                if (collision.sourceColliderGroup?.collisionHandler?.ragdollPart?.ragdoll?.creature?.isPlayer == true)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void OnDeflect(Creature source, Item deflectingItem, Creature target)
        {
            try
            {
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
