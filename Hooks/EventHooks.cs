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
        private int _aliveEnemyCount = -1;
        private float _lastEnemyCountRefreshTime = 0f;
        private const float ENEMY_COUNT_REFRESH_INTERVAL = 15f; // Increased from 5s - event-driven tracking handles most updates
        private readonly Dictionary<int, float> _recentSlicedParts = new Dictionary<int, float>();
        private float _lastSliceCleanupTime = 0f;
        private const float SLICE_REARM_SECONDS = 30f;
        private const float SLICE_CLEANUP_INTERVAL = 10f;
        private readonly HashSet<Ragdoll> _hookedRagdolls = new HashSet<Ragdoll>();

        // Store delegate references to ensure proper unsubscription (prevents memory leaks)
        private EventManager.CreatureKillEvent _onCreatureKillHandler;
        private EventManager.CreatureHitEvent _onCreatureHitHandler;
        private EventManager.CreatureSpawnedEvent _onCreatureSpawnHandler;
        private EventManager.DeflectEvent _onDeflectHandler;
        private EventManager.CreatureParryEvent _onCreatureParryHandler;
        private EventManager.LevelLoadEvent _onLevelUnloadHandler;
        
        // Track creatures the player has recently damaged
        // This allows attributing DOT/status effect kills (bleeds, fire DOT, lightning) to the player
        private readonly Dictionary<int, PlayerDamageHit> _playerDamageHits = new Dictionary<int, PlayerDamageHit>();
        private const float DOT_ATTRIBUTION_WINDOW = 15f; // Seconds after last hit to attribute DOT kill
        private float _lastDamageHitCleanupTime = 0f;
        private const float DAMAGE_HIT_CLEANUP_INTERVAL = 5f;

        private class PlayerDamageHit
        {
            public float Timestamp;
            public DamageType DamageType;
            public float TotalDamage;
            public bool WasThrown; // Track if the hit was from a thrown weapon
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
                _instance._aliveEnemyCount = -1;
                _instance._lastEnemyCountRefreshTime = 0f;
                _instance._recentSlicedParts.Clear();
                _instance._lastSliceCleanupTime = 0f;
                _instance._hookedRagdolls.Clear();
                _instance._playerDamageHits.Clear();
                _instance._lastDamageHitCleanupTime = 0f;
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
                // Create and store delegate references for proper unsubscription
                _onCreatureKillHandler = new EventManager.CreatureKillEvent(this.OnCreatureKill);
                _onCreatureHitHandler = new EventManager.CreatureHitEvent(this.OnCreatureHit);
                _onDeflectHandler = new EventManager.DeflectEvent(this.OnDeflect);
                _onCreatureParryHandler = new EventManager.CreatureParryEvent(this.OnCreatureAttackParry);

                EventManager.onCreatureKill += _onCreatureKillHandler;
                EventManager.onCreatureHit += _onCreatureHitHandler;
                SubscribeSpawnEvent();
                EventManager.onDeflect += _onDeflectHandler;
                EventManager.onCreatureAttackParry += _onCreatureParryHandler;

                // Subscribe to level unload for cleanup on scene transitions
                _onLevelUnloadHandler = new EventManager.LevelLoadEvent(this.OnLevelUnload);
                EventManager.onLevelUnload += _onLevelUnloadHandler;

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
                if (_onDeflectHandler == null)
                    _onDeflectHandler = new EventManager.DeflectEvent(this.OnDeflect);
                EventManager.onDeflect += _onDeflectHandler;
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
                if (_onCreatureSpawnHandler == null)
                    _onCreatureSpawnHandler = new EventManager.CreatureSpawnedEvent(this.OnCreatureSpawn);
                EventManager.onCreatureSpawn += _onCreatureSpawnHandler;
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
                if (_onCreatureParryHandler == null)
                    _onCreatureParryHandler = new EventManager.CreatureParryEvent(this.OnCreatureAttackParry);
                EventManager.onCreatureAttackParry += _onCreatureParryHandler;
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
                // Use stored delegate references to ensure proper unsubscription
                if (_onCreatureKillHandler != null)
                    EventManager.onCreatureKill -= _onCreatureKillHandler;
                if (_onCreatureHitHandler != null)
                    EventManager.onCreatureHit -= _onCreatureHitHandler;
                if (_onDeflectHandler != null)
                    EventManager.onDeflect -= _onDeflectHandler;
                if (_onCreatureParryHandler != null)
                    EventManager.onCreatureAttackParry -= _onCreatureParryHandler;
                if (_spawnSubscribed && _onCreatureSpawnHandler != null)
                    EventManager.onCreatureSpawn -= _onCreatureSpawnHandler;
                if (_onLevelUnloadHandler != null)
                    EventManager.onLevelUnload -= _onLevelUnloadHandler;
            }
            catch (Exception ex)
            {
                if (CSMModOptions.DebugLogging)
                    Debug.LogWarning("[CSM] Error during event unsubscription: " + ex.Message);
            }

            // Clear delegate references
            _onCreatureKillHandler = null;
            _onCreatureHitHandler = null;
            _onDeflectHandler = null;
            _onCreatureParryHandler = null;
            _onCreatureSpawnHandler = null;
            _onLevelUnloadHandler = null;

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
                int aliveCount = 0;
                foreach (var creature in Creature.allActive)
                {
                    RegisterRagdollHooks(creature);
                    if (creature != null && !creature.isPlayer && !creature.isKilled)
                        aliveCount++;
                }
                _aliveEnemyCount = aliveCount;
                _lastEnemyCountRefreshTime = Time.unscaledTime;
            }
            catch (Exception ex)
            {
                if (CSMModOptions.DebugLogging)
                    Debug.LogWarning("[CSM] Error registering existing creatures: " + ex.Message);
            }
        }

        private void OnCreatureSpawn(Creature creature)
        {
            if (creature == null) return;
            RegisterRagdollHooks(creature);
            IncrementAliveEnemyCount(creature);
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
            // No longer tracking creature throws - thrown creatures die from Blunt damage
            // which is already handled by the Blunt Multiplier
        }

        private void OnRagdollTelekinesisRelease(ThunderRoad.Skill.SpellPower.SpellTelekinesis spellTelekinesis, HandleRagdoll handleRagdoll, bool lastHandler)
        {
            // No longer tracking creature throws - thrown creatures die from Blunt damage
            // which is already handled by the Blunt Multiplier
        }

        /// <summary>
        /// Handles level unload events to clean up state and prevent orphaned effects.
        /// </summary>
        private void OnLevelUnload(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
        {
            // Only handle OnEnd to avoid double-processing
            if (eventTime != EventTime.OnEnd) return;

            try
            {
                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Level unloading - cleaning up state");

                // Cancel any active slow motion
                CSMManager.Instance.CancelSlowMotion();

                // Reset all tracking state
                ResetState();

                // Unregister ragdoll hooks (creatures will be destroyed)
                UnregisterAllRagdollHooks();

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Level cleanup complete");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CSM] Error during level unload cleanup: {ex.Message}");
                // Force restore time scale as safety fallback
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
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

                NotifyEnemyKilled(creature);

                // Check for player attribution including recent elemental damage and thrown state
                DamageType elementalDamageType;
                float elementalDamage;
                bool wasThrown;
                bool killedByPlayer = WasKilledByPlayer(collisionInstance, creature, out elementalDamageType, out elementalDamage, out wasThrown);
                if (!killedByPlayer)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Kill skipped - not player kill");
                    return;
                }

                int aliveEnemies = UpdateEnemyTrackingAndGetCount();
                
                bool isLastEnemy = IsSmartLastEnemy(aliveEnemies);

                // Extract damage type and intensity from collision
                DamageType damageType = collisionInstance?.damageStruct.damageType ?? DamageType.Unknown;
                float impactIntensity = GetImpactIntensity(collisionInstance);
                float damageDealt = collisionInstance?.damageStruct.damage ?? 0f;
                bool isStatusDamage = collisionInstance?.damageStruct.isStatus ?? false;
                
                // Track if this is a status effect kill (DOT from fire, lightning, DOT bleeds, etc.)
                // DOT kills are identified by ANY of:
                // 1. damageStruct.isStatus == true (fire/lightning DOT ticks)
                // 2. Damage is 99999 (DOT's kill signature for bleeds)
                // 3. No valid collision but we have DOT attribution
                // Instant elemental kills (fireball impact, lightning bolt) have isStatus=false
                bool isStatusKill = false;
                const float DOT_KILL_DAMAGE = 99999f;
                
                // Check for DOT kill indicators
                bool isDOTKill = isStatusDamage || 
                                 damageDealt >= DOT_KILL_DAMAGE || 
                                 (collisionInstance == null && elementalDamageType != DamageType.Unknown);
                
                if (isDOTKill && elementalDamageType != DamageType.Unknown)
                {
                    // True DOT kill - use tracked damage type and apply DOT multiplier
                    damageType = elementalDamageType;
                    isStatusKill = true;
                    if (impactIntensity < 0.1f && elementalDamage > 0f)
                    {
                        impactIntensity = Mathf.Clamp01(elementalDamage / 50f);
                    }
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] DOT kill detected (isStatus=" + isStatusDamage + " damage=" + damageDealt + ")");
                }
                else if (elementalDamageType != DamageType.Unknown)
                {
                    // DOT attribution was used but this is an instant kill (valid collision, not status damage)
                    // Use the collision's damage type, elemental multiplier will apply
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] DOT attribution overridden - instant kill (isStatus=" + isStatusDamage + " damage=" + damageDealt + ")");
                }

                if (CSMModOptions.DebugLogging)
                {
                    bool isStatus = collisionInstance?.damageStruct.isStatus ?? false;
                    Debug.Log("[CSM] Kill damage: type=" + damageType + " intensity=" + impactIntensity.ToString("F2") + 
                              " isStatus=" + isStatus + " byPlayer=" + killedByPlayer + 
                              (elementalDamageType != DamageType.Unknown ? " (DOT attribution: " + elementalDamageType + ")" : ""));
                }
                
                // Determine if this kill is from a thrown weapon
                // Sources (in order of reliability):
                // 1. wasThrown from DOT attribution tracking (for DOT kills)
                // 2. WasRecentHitFromThrown - checks if recent hit was thrown (for instant kills where isThrowed may be cleared)
                // 3. IsFromThrownWeapon - checks collision data directly (penetrationFromThrow for Pierce)
                bool isThrownWeaponKill = wasThrown || WasRecentHitFromThrown(creature) || IsFromThrownWeapon(collisionInstance);
                
                if (CSMModOptions.DebugLogging && isThrownWeaponKill)
                    Debug.Log("[CSM] Thrown weapon kill detected");
                
                if (isLastEnemy)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Last enemy of wave killed (wave had " + _maxEnemiesSeenThisWave + " enemies)");

                    bool triggered = CSMManager.Instance.TriggerSlow(TriggerType.LastEnemy, 0f, creature, damageType, impactIntensity, false, isStatusKill, isThrownWeaponKill);
                    if (triggered)
                    {
                        _maxEnemiesSeenThisWave = 0;
                        _lastWaveResetTime = Time.unscaledTime;
                        return;
                    }
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
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Decapitation, damageDealt, creature, damageType, impactIntensity, false, isStatusKill, isThrownWeaponKill))
                            return;
                    }

                    if (isHeadOrNeck)
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Critical kill detected");
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Critical, damageDealt, creature, damageType, impactIntensity, false, isStatusKill, isThrownWeaponKill))
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
                        if (CSMManager.Instance.TriggerSlow(TriggerType.Dismemberment, damageDealt, creature, damageType, impactIntensity, false, isStatusKill, isThrownWeaponKill))
                            return;
                    }
                }

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Basic kill with damage=" + damageDealt);
                CSMManager.Instance.TriggerSlow(TriggerType.BasicKill, damageDealt, creature, damageType, impactIntensity, false, isStatusKill, isThrownWeaponKill);
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

                // Track player damage for DOT attribution and thrown weapon detection
                // IMPORTANT: Do this BEFORE the isKilled check because instant kills need this data!
                // OnCreatureHit fires even for killing blows, but isKilled may already be true
                if (collisionInstance != null)
                {
                    bool directPlayerHit = WasKilledByPlayer(collisionInstance);
                    
                    // Track all player hits - needed for:
                    // 1. DOT/status damage attribution (DOT bleeds, burns)
                    // 2. Thrown weapon detection (item.isThrowed may be cleared by kill time)
                    var damageType = collisionInstance.damageStruct.damageType;
                    
                    if (directPlayerHit)
                    {
                        // Track if this hit was from a thrown weapon (must check NOW before isThrowed is cleared)
                        bool wasThrown = IsFromThrownWeapon(collisionInstance);
                        TrackPlayerDamageHit(creature, damageType, collisionInstance.damageStruct.damage, wasThrown);
                    }
                }

                // Skip slice handling for already-dead creatures
                if (creature.isKilled)
                    return;

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

        private int UpdateEnemyTrackingAndGetCount()
        {
            int currentEnemies = GetAliveEnemyCount();
            
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
            
            return currentEnemies;
        }

        private bool IsSmartLastEnemy(int currentAliveEnemies)
        {
            
            if (currentAliveEnemies > 0)
                return false;
            
            int minEnemies = CSMModOptions.LastEnemyMinimumGroup;
            
            bool isLast = _maxEnemiesSeenThisWave >= minEnemies;
            
            if (CSMModOptions.DebugLogging)
                Debug.Log($"[CSM] IsSmartLastEnemy: alive={currentAliveEnemies} maxWave={_maxEnemiesSeenThisWave} minRequired={minEnemies} result={isLast}");
            
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

        private int GetAliveEnemyCount()
        {
            float now = Time.unscaledTime;
            if (_aliveEnemyCount < 0 || now - _lastEnemyCountRefreshTime > ENEMY_COUNT_REFRESH_INTERVAL)
            {
                _aliveEnemyCount = CountAliveEnemies();
                _lastEnemyCountRefreshTime = now;
            }
            return _aliveEnemyCount;
        }

        private void IncrementAliveEnemyCount(Creature creature)
        {
            if (creature == null || creature.isPlayer || creature.isKilled) return;
            if (_aliveEnemyCount < 0) return;
            _aliveEnemyCount++;
        }

        private void NotifyEnemyKilled(Creature creature)
        {
            if (creature == null || creature.isPlayer) return;
            if (_aliveEnemyCount < 0) return;
            _aliveEnemyCount = Mathf.Max(0, _aliveEnemyCount - 1);
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
            return WasKilledByPlayer(collision, null, out _, out _, out _);
        }

        private bool WasKilledByPlayer(CollisionInstance collision, Creature creature, out DamageType elementalDamageType, out float elementalDamage, out bool wasThrown)
        {
            elementalDamageType = DamageType.Unknown;
            elementalDamage = 0f;
            wasThrown = false;
            
            try
            {
                if (collision == null)
                {
                    // No collision but check if player recently hit this creature (for DOT kills)
                    if (creature != null && TryGetRecentPlayerDamageHit(creature, out elementalDamageType, out elementalDamage, out wasThrown))
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] DOT kill attributed to player - recent hit: " + elementalDamageType);
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

                // Check if player recently hit this creature (for DOT/bleed kills from CDoT, etc.)
                if (creature != null && TryGetRecentPlayerDamageHit(creature, out elementalDamageType, out elementalDamage, out wasThrown))
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] DOT kill attributed to player - recent hit: " + elementalDamageType);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the damage came from a thrown weapon (dagger, arrow, spear).
        /// Uses item.isThrowed flag which is set when an item is thrown/released.
        /// </summary>
        private bool IsFromThrownWeapon(CollisionInstance collision)
        {
            try
            {
                if (collision == null) return false;

                // Check if the source item is marked as thrown
                var item = collision.sourceColliderGroup?.collisionHandler?.item;
                if (item != null && item.isThrowed)
                    return true;

                // Also check penetrationFromThrow for penetrating thrown weapons
                if (collision.damageStruct.penetrationFromThrow)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void TrackPlayerDamageHit(Creature creature, DamageType damageType, float damage, bool wasThrown)
        {
            if (creature == null) return;
            int id = creature.GetInstanceID();
            
            if (_playerDamageHits.TryGetValue(id, out var existing))
            {
                existing.Timestamp = Time.unscaledTime;
                existing.DamageType = damageType;
                existing.TotalDamage += damage;
                // Once thrown, always mark as thrown for this creature
                if (wasThrown) existing.WasThrown = true;
            }
            else
            {
                _playerDamageHits[id] = new PlayerDamageHit
                {
                    Timestamp = Time.unscaledTime,
                    DamageType = damageType,
                    TotalDamage = damage,
                    WasThrown = wasThrown
                };
            }
            
            // Cleanup old entries periodically
            CleanupDamageHitCache();
        }

        private bool TryGetRecentPlayerDamageHit(Creature creature, out DamageType damageType, out float totalDamage, out bool wasThrown)
        {
            damageType = DamageType.Unknown;
            totalDamage = 0f;
            wasThrown = false;
            
            if (creature == null) return false;
            int id = creature.GetInstanceID();
            
            if (_playerDamageHits.TryGetValue(id, out var hit))
            {
                if (Time.unscaledTime - hit.Timestamp <= DOT_ATTRIBUTION_WINDOW)
                {
                    damageType = hit.DamageType;
                    totalDamage = hit.TotalDamage;
                    wasThrown = hit.WasThrown;
                    _playerDamageHits.Remove(id); // Consume the attribution
                    return true;
                }
                _playerDamageHits.Remove(id); // Expired
            }
            return false;
        }

        /// <summary>
        /// Check if a recent hit on this creature was from a thrown weapon.
        /// Does NOT consume the attribution (use for instant kills where we need thrown state
        /// but the hit tracking should remain for potential DOT attribution).
        /// </summary>
        private bool WasRecentHitFromThrown(Creature creature)
        {
            if (creature == null) return false;
            int id = creature.GetInstanceID();
            
            if (_playerDamageHits.TryGetValue(id, out var hit))
            {
                if (Time.unscaledTime - hit.Timestamp <= DOT_ATTRIBUTION_WINDOW)
                {
                    return hit.WasThrown;
                }
            }
            return false;
        }

        private void CleanupDamageHitCache()
        {
            float now = Time.unscaledTime;
            if (now - _lastDamageHitCleanupTime < DAMAGE_HIT_CLEANUP_INTERVAL)
                return;
            
            _lastDamageHitCleanupTime = now;
            List<int> expired = null;
            
            foreach (var kvp in _playerDamageHits)
            {
                if (now - kvp.Value.Timestamp > DOT_ATTRIBUTION_WINDOW)
                {
                    if (expired == null) expired = new List<int>();
                    expired.Add(kvp.Key);
                }
            }
            
            if (expired != null)
            {
                foreach (var key in expired)
                    _playerDamageHits.Remove(key);
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
