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
        
        // Track creatures the player has recently damaged with elemental attacks
        // This allows attributing status effect kills (fire DOT, lightning) to the player
        private readonly Dictionary<int, PlayerElementalHit> _playerElementalHits = new Dictionary<int, PlayerElementalHit>();
        private const float ELEMENTAL_ATTRIBUTION_WINDOW = 15f; // Seconds after last elemental hit to attribute kill

        private class PlayerElementalHit
        {
            public float Timestamp;
            public DamageType DamageType;
            public float TotalDamage;
        }

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
                _instance._playerElementalHits.Clear();
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

                // Check for player attribution including recent elemental damage
                DamageType elementalDamageType;
                float elementalDamage;
                bool killedByPlayer = WasKilledByPlayer(collisionInstance, creature, out elementalDamageType, out elementalDamage);
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

                // Extract damage type and intensity from collision
                // Use tracked elemental damage type if the kill was attributed via recent elemental hit
                DamageType damageType = collisionInstance?.damageStruct.damageType ?? DamageType.Unknown;
                float impactIntensity = GetImpactIntensity(collisionInstance);
                
                // Override with tracked elemental damage type for status effect kills
                if (elementalDamageType != DamageType.Unknown && damageType == DamageType.Unknown)
                {
                    damageType = elementalDamageType;
                    // Use elemental damage for intensity if collision intensity is missing
                    if (impactIntensity < 0.1f && elementalDamage > 0f)
                    {
                        impactIntensity = Mathf.Clamp01(elementalDamage / 50f);
                    }
                }

                if (CSMModOptions.DebugLogging)
                {
                    bool isStatus = collisionInstance?.damageStruct.isStatus ?? false;
                    Debug.Log("[CSM] Kill damage: type=" + damageType + " intensity=" + impactIntensity.ToString("F2") + 
                              " isStatus=" + isStatus + " byPlayer=" + killedByPlayer + 
                              (elementalDamageType != DamageType.Unknown ? " (elemental attribution: " + elementalDamageType + ")" : ""));
                }
                
                if (isLastEnemy)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Last enemy of wave killed (wave had " + _maxEnemiesSeenThisWave + " enemies)");

                    bool triggered = CSMManager.Instance.TriggerSlow(TriggerType.LastEnemy, 0f, creature, damageType, impactIntensity, false);
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
                    CSMManager.Instance.TriggerSlow(TriggerType.BasicKill, damageDealt, creature, damageType, impactIntensity, false);
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
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Decapitation, damageDealt, creature, damageType, impactIntensity, false))
                            return;
                    }

                    if (isHeadOrNeck)
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Critical kill detected");
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Critical, damageDealt, creature, damageType, impactIntensity, false))
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
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Dismemberment, damageDealt, creature, damageType, impactIntensity, false))
                            return;
                    }
                }

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Basic kill with damage=" + damageDealt);
                CSMManager.Instance.TriggerSlow(TriggerType.BasicKill, damageDealt, creature, damageType, impactIntensity, false);
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

                // Track elemental damage from player for status effect kill attribution
                if (collisionInstance != null)
                {
                    bool directPlayerHit = WasKilledByPlayer(collisionInstance);
                    
                    if (!directPlayerHit)
                    {
                        ThrowTracker.RecordImpact(creature);
                    }
                    
                    // Track elemental hits from player for status damage attribution
                    var damageType = collisionInstance.damageStruct.damageType;
                    bool isElemental = damageType == DamageType.Energy || 
                                       damageType == DamageType.Fire || 
                                       damageType == DamageType.Lightning;
                    
                    if (isElemental && directPlayerHit)
                    {
                        TrackPlayerElementalHit(creature, damageType, collisionInstance.damageStruct.damage);
                    }
                }

                if (collisionInstance != null &&
                    collisionInstance.damageStruct.hitRagdollPart != null &&
                    collisionInstance.damageStruct.hitRagdollPart.isSliced)
                {
                    float damageDealt = collisionInstance.damageStruct.damage;
                    var part = collisionInstance.damageStruct.hitRagdollPart;
                    var partType = part.type;
                    DamageType damageType = collisionInstance.damageStruct.damageType;
                    float impactIntensity = GetImpactIntensity(collisionInstance);
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
                        CSMManager.Instance.TriggerSlow(TriggerType.Decapitation, damageDealt, creature, damageType, impactIntensity, false);
                    }
                    else
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Non-lethal dismemberment detected, damage=" + damageDealt);
                        CSMManager.Instance.TriggerSlow(TriggerType.Dismemberment, damageDealt, creature, damageType, impactIntensity, false);
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

        private float GetImpactIntensity(CollisionInstance collisionInstance)
        {
            if (collisionInstance == null) return 0f;

            // First check if the CollisionInstance already has computed intensity
            if (collisionInstance.intensity > 0f)
            {
                return Mathf.Clamp01(collisionInstance.intensity);
            }

            // For elemental/status damage (fire, lightning), impactVelocity is typically zero
            // Use damage amount as a proxy for intensity
            float velocity = collisionInstance.impactVelocity.magnitude;
            if (velocity < 0.1f)
            {
                // Status/elemental damage - use damage amount for intensity
                // Typical damage range: 0-100+, normalize to 0-1
                float damage = collisionInstance.damageStruct.damage;
                if (damage > 0f)
                {
                    // Scale damage: 10 damage = 0.2 intensity, 50 damage = 1.0 intensity
                    return Mathf.Clamp01(damage / 50f);
                }
                return 0f;
            }

            // Standard impact velocity-based intensity
            // Typical combat velocity range: 2-15 m/s
            return Mathf.Clamp01((velocity - 2f) / 13f);
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
            return WasKilledByPlayer(collision, null, out _, out _);
        }

        private bool WasKilledByPlayer(CollisionInstance collision, Creature creature, out DamageType elementalDamageType, out float elementalDamage)
        {
            elementalDamageType = DamageType.Unknown;
            elementalDamage = 0f;
            
            try
            {
                if (collision == null)
                {
                    // No collision but check if player recently hit this creature with elemental damage
                    if (creature != null && TryGetRecentPlayerElementalHit(creature, out elementalDamageType, out elementalDamage))
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Status kill attributed to player - recent elemental hit: " + elementalDamageType);
                        return true;
                    }
                    return false;
                }

                // Use ThunderRoad's built-in method which checks:
                // - Items held by player
                // - Ragdoll parts with player interaction
                // - Spell caster (for elemental/magic damage)
                if (collision.IsDoneByPlayer())
                    return true;

                // Additional fallback checks for edge cases
                if (collision.sourceColliderGroup?.collisionHandler?.item?.mainHandler?.creature?.isPlayer == true)
                    return true;

                if (collision.sourceColliderGroup?.collisionHandler?.item?.lastHandler?.creature?.isPlayer == true)
                    return true;

                if (collision.sourceColliderGroup?.collisionHandler?.ragdollPart?.ragdoll?.creature?.isPlayer == true)
                    return true;

                // Check casterHand for spell damage (fire, lightning, etc.)
                if (collision.casterHand?.mana?.creature?.isPlayer == true)
                    return true;

                // Check if player recently hit this creature with elemental damage
                // This handles status effect kills (fire DOT, lightning electrocution) from other mods like BDOT
                if (creature != null && TryGetRecentPlayerElementalHit(creature, out elementalDamageType, out elementalDamage))
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Status kill attributed to player - recent elemental hit: " + elementalDamageType);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void TrackPlayerElementalHit(Creature creature, DamageType damageType, float damage)
        {
            if (creature == null) return;
            int id = creature.GetInstanceID();
            
            if (_playerElementalHits.TryGetValue(id, out var existing))
            {
                existing.Timestamp = Time.unscaledTime;
                existing.DamageType = damageType;
                existing.TotalDamage += damage;
            }
            else
            {
                _playerElementalHits[id] = new PlayerElementalHit
                {
                    Timestamp = Time.unscaledTime,
                    DamageType = damageType,
                    TotalDamage = damage
                };
            }
            
            // Cleanup old entries periodically
            CleanupElementalHitCache();
        }

        private bool TryGetRecentPlayerElementalHit(Creature creature, out DamageType damageType, out float totalDamage)
        {
            damageType = DamageType.Unknown;
            totalDamage = 0f;
            
            if (creature == null) return false;
            int id = creature.GetInstanceID();
            
            if (_playerElementalHits.TryGetValue(id, out var hit))
            {
                if (Time.unscaledTime - hit.Timestamp <= ELEMENTAL_ATTRIBUTION_WINDOW)
                {
                    damageType = hit.DamageType;
                    totalDamage = hit.TotalDamage;
                    _playerElementalHits.Remove(id); // Consume the attribution
                    return true;
                }
                _playerElementalHits.Remove(id); // Expired
            }
            return false;
        }

        private void CleanupElementalHitCache()
        {
            float now = Time.unscaledTime;
            List<int> expired = null;
            
            foreach (var kvp in _playerElementalHits)
            {
                if (now - kvp.Value.Timestamp > ELEMENTAL_ATTRIBUTION_WINDOW)
                {
                    if (expired == null) expired = new List<int>();
                    expired.Add(kvp.Key);
                }
            }
            
            if (expired != null)
            {
                foreach (var key in expired)
                    _playerElementalHits.Remove(key);
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
