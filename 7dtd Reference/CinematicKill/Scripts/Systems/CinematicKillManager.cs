// ═══════════════════════════════════════════════════════════════════════════════
// CinematicKillManager.cs - Core cinematic kill camera system for 7 Days to Die
// ═══════════════════════════════════════════════════════════════════════════════
//
// ARCHITECTURE OVERVIEW:
//   This is the main manager for the CinematicKill mod. It handles:
//   - Kill detection via Harmony patches (EntityAliveDamagePatch)
//   - Camera systems (First-Person slowmo, Projectile follow cam)
//   - Time scale manipulation (slow motion effects)
//   - Ragdoll tracking for dynamic duration
//   - Screen effects coordination
//
// KEY ENTRY POINTS:
//   HandleDamageResponse() - Called by Harmony when entity takes damage
//   HandleCameraUpdate()   - Called every frame to position projectile camera
//   HandleUpdate()         - Called every frame for timer management
//   StartSequence()        - Begins a cinematic sequence
//   CancelSequence()       - Ends cinematic and restores state
//
// SECTIONS:
//   Lines 30-190   : Fields & Constants
//   Lines 196-353  : Weapon Detection
//   Lines 359-470  : Initialization & Update Loops
//   Lines 473-959  : Damage Response & Kill Detection
//   Lines 961-1183 : Ragdoll Tracking & Audio
//   Lines 1186-1532: Timers & Freeze Frame
//   Lines 1576-2106: Cinematic Sequence Control
//   Lines 2108-2969: Camera Selection & Time Scale
//   Lines 2980-3858: HUD, Projectile Camera, View Restore
//   Lines 3860-6499: Config Loading (XML)
//   Lines 6501-end : Settings Access, Menu, Saving
//
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace CinematicKill
{
    /// <summary>
    /// Weapon-based kill mode for cinematic triggers
    /// </summary>
    public enum WeaponMode
    {
        Unknown,
        Melee,
        Ranged,
        Bow,
        Explosion,
        Thrown,
        Trap
    }

    public static class CinematicKillManager
    {
        #region Fields

        #region Core State
        // Shorthand alias for s_cinematicSettings to reduce code noise
        private static CinematicKillSettings CKSettings => s_cinematicSettings;
        
        // Current weapon mode for the active kill
        private static WeaponMode s_currentWeaponMode = WeaponMode.Unknown;
        
        private const float MinScale = 0.05f;
        private const float MaxScale = 2f;

        private static float s_baseFixedDelta = 0.02f;
        private static float s_slowScale = 0.2f;
        private static float s_duration = 1.25f;
        private static bool s_enabled = true;
        private static float s_originalAudioVolume = 1f;
        private static bool s_audioAdjusted;
        private static float s_killcamChance = 1f;
        private static bool s_ignoreCorpseHits = true;
        private static string s_configPath = string.Empty;
        private static CinematicKillSettings s_cinematicSettings = CinematicKillSettings.Default;
        private static float s_cameraDebugTimer = 0f;
        #endregion
        
        #region Public Properties
        /// <summary>
        /// Public read-only access to the current settings for HUD and other systems.
        /// </summary>
        public static CinematicKillSettings Settings => s_cinematicSettings;
        
        /// <summary>
        /// Whether a cinematic sequence is currently active.
        /// </summary>
        public static bool IsActive => s_isActive;
        
        /// <summary>
        /// The reason for the last triggered cinematic.
        /// </summary>
        public static string LastTriggerReason { get; private set; } = "None";
        
        /// <summary>
        /// Current killstreak count.
        /// </summary>
        public static int CurrentStreak => s_currentStreak;
        #endregion

        #region Sequence State
        private static bool s_isActive;
        private static float s_timer;
        
        // Hitstop - deprecated, use Freeze Frame instead
        // Kept for backward compatibility with existing code paths
        private static bool s_isHitstopActive;
        private static float s_hitstopTimer;
        private static float s_resumeScale;
        #endregion

        #region Cooldowns
        private static float s_lastRuntimeCooldown; // Cooldown chosen for the active sequence (basic vs special)
        private static float s_returnDuration; // Active return duration for time restore
        private static float s_returnStartTime; // When to start restoring during slowmo
        private static bool s_isRestoringTime;
        private static float s_cooldownCrit;
        private static float s_cooldownDismember;
        private static float s_cooldownLongRange;
        private static float s_cooldownLowHealth;
        private static float s_cooldownLastEnemy;
        
        // Unified trigger cooldown dictionary
        private static Dictionary<string, float> s_triggerCooldowns = new Dictionary<string, float>();
        #endregion

        #region Killstreaks
        private static int s_currentStreak;
        private static float s_lastKillTime;
        #endregion

        #region Menu
        private static GameObject s_menuObject;
        private static CinematicKillMenu s_menuComponent;
        #endregion

        #region Effects
        private static CinematicFOVController s_fovController;
        private static Coroutine s_timeRestoreCoroutine;
        #endregion
        
        #region Ragdoll Tracking
        // Maps entity IDs to their ragdoll transforms for camera follow
        private static Dictionary<int, Transform> s_ragdollTargets = new Dictionary<int, Transform>();
        // Current ragdoll being tracked by projectile camera
        private static Transform s_currentRagdollTarget;
        private static int s_currentRagdollEntityId = -1;
        // Ragdoll rigidbodies for velocity monitoring
        private static Rigidbody[] s_ragdollRigidbodies;
        // Ragdoll ground detection
        private static bool s_ragdollGroundDetectionActive = false;
        private static float s_ragdollSettledTime = 0f;  // Time ragdoll has been settled
        private static float s_ragdollSettledThreshold = 0.3f;  // Velocity threshold to consider "settled"
        private static float s_ragdollSettledDuration = 0.5f;  // How long it must stay settled
        private static float s_ragdollPostLandDelay = 0.3f;  // Extra time after landing before ending
        // Audio pitch for slow-motion effect
        private static float s_originalAudioPitch = 1f;
        private static bool s_audioSlowMoActive = false;
        #endregion
        
        #region Freeze Frame
        private static bool s_isFreezeFrameActive;
        private static bool s_isFreezeDelayActive;      // True during delay before freeze
        private static float s_freezeDelayTimer;        // Countdown for delay before freeze
        private static float s_freezeFrameTimer;        // Countdown for freeze duration
        private static bool s_freezeIsFirstPerson;      // True if using FP freeze settings, false for Projectile
        
        // Enhanced Freeze Frame - Camera Movement
        private static int s_freezeCameraPresetIndex = -1;       // Active projectile preset index during freeze (-1 = none)
        private static Vector3 s_freezeOriginalCameraPos;        // Camera position before freeze started
        private static Quaternion s_freezeOriginalCameraRot;     // Camera rotation before freeze started
        private static bool s_freezeWasFirstPerson;              // Was in first person view before freeze
        private static Transform s_freezeCameraTarget;           // Target to look at during freeze (usually victim)
        private static float s_freezeCameraDriftAngle;           // Current drift angle for subtle camera movement
        private static bool s_postFreezeContinueCinematic;       // Whether to continue cinematic after freeze ends
        private static string s_freezeTriggerReason;             // The trigger that caused the freeze (for post-freeze handling)
        #endregion
        
        #region Experimental Features
        // Projectile Ride Cam - camera attached directly to projectile
        private static bool s_isRideCamActive;
        private static Transform s_rideCamProjectileTransform;
        // Dismemberment Focus Cam - camera follows severed limb
        private static bool s_isDismemberFocusCamActive;
        private static Transform s_dismemberedLimbTarget;
        
        // Last Stand / Second Wind - player near-death cinematic
        private static bool s_isLastStandActive;
        private static float s_lastStandTimer;
        private static float s_lastStandCooldownTimer;
        private static bool s_lastStandPreviousGodMode;
        
        // Chain Reaction - multi-kill camera daisy chain
        private static bool s_isChainReactionActive;
        private static int s_chainReactionKillCount;
        private static List<EntityAlive> s_chainReactionList = new List<EntityAlive>();
        
        // Slow-Mo Toggle - manual slow motion control via keybind
        private static bool s_isSlowMoToggleActive;
        private static float s_slowMoToggleOriginalTimeScale = 1f;
        #endregion

        #region Helper Types
        private struct EffectOverride
        {
            // Hitstop fields - deprecated, use Freeze Frame instead
            // Kept for backward compatibility with existing trigger configurations
            public bool HasHitstopOverride;
            public bool HitstopEnabled;
            public float HitstopDuration;
            public bool HitstopCritOnly;
            // Sound override
            public bool HasSoundOverride;
            public bool SoundEnabled;
            public float SoundVolume;
            public string VisualSource;
            public string FOVSource;
            public string FXSource;
        }
        private static EffectOverride? s_effectOverride;

        private struct ProjectileRuntime
        {
            public float Duration;
            public float SlowScale;
            public float ReturnDuration;
            public float Chance;
            public bool LastEnemyOnly;
            public float HeightOffset;
            public float DistanceOffset;
            public float XOffset;
            public float LookYaw;
            public float LookPitch;
            public float Tilt;  // Roll/Dutch angle in degrees
            public float RandomYawRange;
            public float RandomPitchRange;
            public float FOV;
            public float FOVIn;
            public float FOVHold;
            public float FOVOut;
            public bool EnableVignette;
            public float VignetteIntensity;
        }

        private static ProjectileRuntime s_projectileRuntime;
        #endregion

        #endregion Fields

        #region Weapon Detection

        /// <summary>
        /// Detects the weapon mode (Melee/Ranged/Explosion/Thrown/Trap) from a damage source
        /// </summary>
        private static WeaponMode DetectWeaponMode(DamageSource source)
        {
            if (source == null) return WeaponMode.Unknown;

            // PRIORITY: Check for auto-turret damage first (CreatorEntityId == -2)
            // This is the definitive way 7 Days to Die marks auto-turret kills
            // and must be checked before any other weapon detection
            if (source.CreatorEntityId == -2)
            {
                CKLog.Verbose(" Detected auto-turret kill via CreatorEntityId=-2");
                return WeaponMode.Trap;
            }

            // Check damage type first for explosions
            var damageType = source.damageType;
            if (damageType == EnumDamageTypes.Heat || 
                source.DamageTypeTag.Test_AnySet(FastTags<TagGroup.Global>.Parse("explosive,explosion")))
            {
                return WeaponMode.Explosion;
            }
            
            // Check for trap damage (trap tags in damage type)
            if (source.DamageTypeTag.Test_AnySet(FastTags<TagGroup.Global>.Parse("trap,spikes,blade,electric,turret")))
            {
                return WeaponMode.Trap;
            }
            
            // External damage with no item is typically a trap
            if (source.damageSource == EnumDamageSource.External && source.ItemClass == null)
            {
                return WeaponMode.Trap;
            }

            // Check the attacking item
            var itemClass = source.ItemClass;
            if (itemClass == null) return WeaponMode.Unknown;
            
            // Check item name for turret/trap patterns
            string itemName = itemClass.Name?.ToLower() ?? "";
            if (itemName.Contains("turret") || itemName.Contains("trap") || 
                itemName.Contains("autoshotgun") || itemName.Contains("autoturret") ||
                itemName.Contains("sledge") || itemName.Contains("junkturret"))
            {
                CKLog.Verbose($" Detected turret/trap by item name: {itemClass.Name}");
                return WeaponMode.Trap;
            }

            // Check item actions to determine weapon type
            var actions = itemClass.Actions;
            if (actions != null && actions.Length > 0 && actions[0] != null)
            {
                var primaryAction = actions[0];
                
                // Thrown weapons (throwing knives, rocks, molotovs before explosion)
                if (primaryAction is ItemActionThrownWeapon)
                {
                    // Check if it's an explosive thrown item
                    var itemTags = itemClass.ItemTags;
                    if (itemTags.Test_AnySet(FastTags<TagGroup.Global>.Parse("explosive,grenade,dynamite,molotov")))
                    {
                        return WeaponMode.Explosion;
                    }
                    return WeaponMode.Thrown;
                }
                
                // Melee weapons use ItemActionMelee or ItemActionDynamicMelee
                if (primaryAction is ItemActionMelee || primaryAction is ItemActionDynamicMelee)
                {
                    return WeaponMode.Melee;
                }
                
                // Launcher fires explosives
                if (primaryAction is ItemActionLauncher)
                {
                    return WeaponMode.Explosion;
                }
                
                // Bows and crossbows - check for bow/crossbow tags before generic ranged
                var itemTagsAction = itemClass.ItemTags;
                if (itemTagsAction.Test_AnySet(FastTags<TagGroup.Global>.Parse("bow,crossbow,perkArchery")))
                {
                    return WeaponMode.Bow;
                }
                
                // Ranged weapons use ItemActionRanged, ItemActionProjectile (guns)
                if (primaryAction is ItemActionRanged || primaryAction is ItemActionProjectile)
                {
                    return WeaponMode.Ranged;
                }
            }

            // Fallback: check item tags
            var itemTags2 = itemClass.ItemTags;
            if (itemTags2.Test_AnySet(FastTags<TagGroup.Global>.Parse("thrown,throwingKnife,rock")))
            {
                return WeaponMode.Thrown;
            }
            if (itemTags2.Test_AnySet(FastTags<TagGroup.Global>.Parse("melee,perkBrawler,perkClubmaster,perkDeepCuts,perkSkullCrusher")))
            {
                return WeaponMode.Melee;
            }
            if (itemTags2.Test_AnySet(FastTags<TagGroup.Global>.Parse("bow,crossbow,perkArchery")))
            {
                return WeaponMode.Bow;
            }
            if (itemTags2.Test_AnySet(FastTags<TagGroup.Global>.Parse("ranged,gun,perkGunslinger,perkMachineGunner,perkShotgunMessiah,perkDeadEye")))
            {
                return WeaponMode.Ranged;
            }
            if (itemTags2.Test_AnySet(FastTags<TagGroup.Global>.Parse("explosive,launcher,rocket,grenade,dynamite,perkDemolitionsExpert")))
            {
                return WeaponMode.Explosion;
            }

            // Default based on damage type
            switch (damageType)
            {
                case EnumDamageTypes.Bashing:
                case EnumDamageTypes.Slashing:
                case EnumDamageTypes.Crushing:
                    return WeaponMode.Melee;
                case EnumDamageTypes.Piercing:
                    // Piercing could be arrows or bullets - check distance or default to ranged
                    return WeaponMode.Ranged;
                default:
                    return WeaponMode.Unknown;
            }
        }

        /// <summary>
        /// Gets the mode settings for the current weapon mode
        /// </summary>
        private static CKModeSettings GetModeSettings(WeaponMode mode)
        {
            var menuV2 = s_cinematicSettings.MenuV2;
            if (menuV2 == null) return null;
            
            switch (mode)
            {
                case WeaponMode.Melee:
                    return menuV2.Melee;
                case WeaponMode.Ranged:
                    return menuV2.Ranged;
                case WeaponMode.Bow:
                    return menuV2.Bow;
                case WeaponMode.Explosion:
                    return menuV2.Dismember; // Dismember in UI = Explosion mode
                case WeaponMode.Thrown:
                    return menuV2.Thrown;
                case WeaponMode.Trap:
                    return menuV2.Trap;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets trigger settings FRESH from the live settings, bypassing MenuV2 cache.
        /// This ensures UI changes to trigger enable states are immediately reflected at runtime.
        /// </summary>
        private static CKContextTriggerSettings GetLiveTriggerSettings(string triggerName)
        {
            var s = s_cinematicSettings;
            var defaults = s.TriggerDefaults;
            
            CKTriggerSettings trigger = triggerName switch
            {
                "LastEnemy" => s.LastEnemy,
                "Killstreak" => s.Killstreak,
                "Dismember" => s.Dismember,
                "Headshot" => s.Headshot,
                "Crit" => s.Critical,
                "LongRange" => s.LongRange,
                "LowHealth" => s.LowHealth,
                "Sneak" => s.Sneak,
                _ => null
            };
            
            if (trigger == null) return null;
            
            // Create CKContextTriggerSettings on-demand from live settings
            return CKContextTriggerSettings.FromTrigger(trigger, defaults);
        }
        
        /// <summary>
        /// Gets BasicKill settings FRESH from the live settings, bypassing MenuV2 cache.
        /// </summary>
        private static CKContextTriggerSettings GetLiveBasicKillSettings()
        {
            return CKContextTriggerSettings.FromSimple(s_cinematicSettings.BasicKill);
        }
        
        /// <summary>
        /// Gets the master trigger chance directly from live settings
        /// </summary>
        private static float GetLiveMasterTriggerChance()
        {
            return s_cinematicSettings.MasterTriggerChance;
        }
        
        /// <summary>
        /// Checks if special triggers are enabled directly from live settings
        /// </summary>
        private static bool AreLiveTriggersEnabled()
        {
            return s_cinematicSettings.TriggerDefaults.EnableTriggers;
        }

        #endregion Weapon Detection

        #region Initialization

        public static void Initialize(string modPath)
        {
            if (!GameManager.IsDedicatedServer)
            {
                s_baseFixedDelta = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;
                
                // Initialize FOV controller
                s_fovController = new CinematicFOVController();
            }

            // Use ModManager for reliable path resolution
            Mod mod = ModManager.GetMod("CinematicKill");
            if (mod != null)
            {
                s_configPath = Path.Combine(mod.Path, "Config", "CinematicKillSettings.xml");
            }
            else
            {
                Log.Error("[CinematicKill] Could not find mod via ModManager, using fallback path.");
                s_configPath = Path.Combine(modPath ?? string.Empty, "Config", "CinematicKillSettings.xml");
            }
            
            LoadConfig();
            ResetTimeScale();

            // Initialize Menu
            if (s_menuObject == null)
            {
                // Fix for duplicate menu: Check if object already exists in scene
                s_menuObject = GameObject.Find("CinematicKillMenu");
                if (s_menuObject != null)
                {
                    s_menuComponent = s_menuObject.GetComponent<CinematicKillMenu>();
                }
                else
                {
                    s_menuObject = new GameObject("CinematicKillMenu");
                    GameObject.DontDestroyOnLoad(s_menuObject);
                    s_menuComponent = s_menuObject.AddComponent<CinematicKillMenu>();
                }
            }
            
            // Initialize HUD
            var hudObject = GameObject.Find("CinematicKillHUD");
            if (hudObject == null)
            {
                hudObject = new GameObject("CinematicKillHUD");
                GameObject.DontDestroyOnLoad(hudObject);
                hudObject.AddComponent<CinematicKillHUD>();
            }
        }

        /// <summary>
        /// Public wrapper to reload configuration from disk.
        /// Used by import functionality to refresh settings after copying backup file.
        /// </summary>
        public static void ReloadConfig()
        {
            LoadConfig();
            CKLog.Verbose(" Configuration reloaded from disk.");
        }

        public static void OnGameStartDone(ref ModEvents.SGameStartDoneData _)
        {
            ResetTimeScale();
            
            // Reset all cinematic state on player spawn/respawn to fix camera issues after death
            s_isActive = false;
            s_isHitstopActive = false;
            s_timer = 0f;
            
            // Clear stale ragdoll targets
            s_ragdollTargets.Clear();
            s_currentRagdollTarget = null;
            s_currentRagdollEntityId = -1;
            s_ragdollGroundDetectionActive = false;
            
            // Reset experimental features
            s_isRideCamActive = false;
            s_rideCamProjectileTransform = null;
            s_isDismemberFocusCamActive = false;
            s_dismemberedLimbTarget = null;
            s_isLastStandActive = false;
            s_isChainReactionActive = false;
            
            // Clear cooldowns on spawn so player gets fresh start
            s_triggerCooldowns.Clear();
            
            CKLog.Verbose(" Game start - reset all cinematic state for clean spawn");
        }

        public static void HandleUpdate(PlayerMoveController controller)
        {
            if (GameManager.IsDedicatedServer || controller?.entityPlayerLocal == null)
            {
                return;
            }

            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.World == null)
            {
                ResetTimeScale();
                return;
            }

            // Menu key handling is done in CinematicKillMenu.Update() which runs every frame
            // regardless of game state (pause, loading, etc.)

            if (gameManager.IsPaused())
            {
                // If our menu is open, we might be the one pausing it, so don't cancel sequence just yet?
                // Actually, if we are paused, we shouldn't be running a cinematic sequence anyway.
                // But we definitely want to allow the menu to toggle.
                
                // If the menu is NOT open, and we are paused, we should probably cancel any active sequence
                // to prevent weirdness when unpausing.
                if (s_menuComponent == null || !s_menuComponent.IsVisible)
                {
                    CancelSequence();
                }
                return;
            }

            var delta = Time.unscaledDeltaTime;
            if (delta <= 0f)
            {
                delta = Time.deltaTime; // fallback if unscaled delta is unavailable
            }

            UpdateTimers(delta);
            
            // Update FOV effect if active (and not in hitstop)
            // Use Time.deltaTime (game time) so FOV zoom syncs with slow-motion cinematics
            if (s_fovController != null && s_fovController.IsActive && !s_isHitstopActive)
            {
                s_fovController.Update(Time.deltaTime);
            }
        }
        
        // ═══════════════════════════════════════════════════════════════════════
        // EXPERIMENTAL: Last Stand - Player near-death handler
        // ═══════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Called when player takes damage. Checks if they're about to die and triggers Last Stand.
        /// </summary>
        public static void HandlePlayerDamage(EntityPlayerLocal player, int damageAmount)
        {
            if (!s_enabled || GameManager.IsDedicatedServer) return;
            
            // Last Stand settings are in main CKSettings, not Experimental
            if (!CKSettings.EnableLastStand) return;
            if (s_isLastStandActive) return; // Already in Last Stand
            if (s_lastStandCooldownTimer > 0f) return; // Still on cooldown
            
            // Check if this damage would kill the player
            int healthAfterDamage = player.Health - damageAmount;
            if (healthAfterDamage <= 0)
            {
                StartLastStand(player);
            }
        }
        
        /// <summary>
        /// Start Last Stand mode - give player a chance to survive.
        /// </summary>
        private static void StartLastStand(EntityPlayerLocal player)
        {
            // Last Stand settings are in main CKSettings, not Experimental
            var settings = CKSettings;
            
            s_isLastStandActive = true;
            s_lastStandTimer = settings.LastStandDuration;
            s_lastStandPreviousGodMode = player.IsGodMode.Value;
            
            // Prevent death
            player.IsGodMode.Value = true;
            player.Health = 1; // Keep at 1 HP
            
            // Apply extreme time dilation
            ApplyTimeScale(settings.LastStandTimeScale);
            
            // Visual feedback
            try
            {
                player.ScreenEffectManager?.SetScreenEffect("Dying", 0.8f, settings.LastStandDuration);
            }
            catch { }
            
            CKLog.Verbose($" LAST STAND activated! Duration: {settings.LastStandDuration}s, TimeScale: {settings.LastStandTimeScale}");
        }
        
        /// <summary>
        /// End Last Stand mode - either revive or die.
        /// </summary>
        private static void EndLastStand(bool gotKill)
        {
            if (!s_isLastStandActive) return;
            
            var player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player == null) return;
            
            // Last Stand settings are in main CKSettings, not Experimental
            var settings = CKSettings;
            s_isLastStandActive = false;
            
            // Restore god mode state
            player.IsGodMode.Value = s_lastStandPreviousGodMode;
            
            // Start cooldown
            s_lastStandCooldownTimer = settings.LastStandCooldown;
            
            if (gotKill)
            {
                // SECOND WIND - Revive player
                player.Health = (int)settings.LastStandReviveHealth;
                
                // Clear dying effect
                try
                {
                    player.ScreenEffectManager?.SetScreenEffect("Dying", 0f, 0.1f);
                }
                catch { }
                
                CKLog.Verbose($" SECOND WIND! Player revived with {settings.LastStandReviveHealth} HP");
            }
            else
            {
                // Player failed - they die
                // Note: SetDead doesn't exist on player, so we just set health to 0
                // The game's next damage tick will kill them
                player.Health = -1;
                
                CKLog.Verbose(" LAST STAND failed - player died");
            }
            
            // Restore time scale
            ResetTimeScale();
        }
        
        /// <summary>
        /// Called when player gets a kill during Last Stand - triggers Second Wind.
        /// </summary>
        public static void CheckLastStandKill()
        {
            if (s_isLastStandActive)
            {
                EndLastStand(true);
            }
        }
        
        /// <summary>
        /// EXPERIMENTAL: Try to find and activate camera tracking for a dismembered limb (GoreBlock).
        /// Searches for nearby GoreBlock entities that spawned from the victim.
        /// </summary>
        private static void TryActivateDismemberFocusCam(EntityAlive victim)
        {
            // Reset state
            s_isDismemberFocusCamActive = false;
            s_dismemberedLimbTarget = null;
            
            if (victim == null) return;
            
            // Search for nearby GoreBlock entities spawned by this kill
            var world = GameManager.Instance?.World;
            if (world == null) return;
            
            Vector3 victimPos = victim.position;
            float searchRadius = 5f; // GoreBlocks spawn near the victim
            Transform closestLimb = null;
            float closestDist = float.MaxValue;
            
            // Look for GoreBlock entities in the world
            foreach (var entity in world.Entities.list)
            {
                if (entity == null) continue;
                
                // Check if this is a GoreBlock (contains "Gore" in name or is EntityGoreBlock type)
                string entityName = entity.GetType().Name;
                if (entityName.Contains("Gore") || entity.name?.Contains("Gore") == true)
                {
                    float dist = Vector3.Distance(entity.position, victimPos);
                    if (dist < searchRadius && dist < closestDist)
                    {
                        closestDist = dist;
                        closestLimb = entity.transform;
                    }
                }
            }
            
            // If we found a limb, track it
            if (closestLimb != null)
            {
                s_isDismemberFocusCamActive = true;
                s_dismemberedLimbTarget = closestLimb;
                CKLog.Verbose($" Dismemberment Focus Cam activated - tracking limb at distance {closestDist:F1}m");
            }
            else
            {
                CKLog.Verbose(" Dismemberment Focus Cam - no limb found to track");
            }
        }
        
        // ═══════════════════════════════════════════════════════════════════════
        // PREDICTIVE RIDE CAM - Called from PredictiveAimPatch before projectile exists
        // ═══════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Flag set when predictive aim detected a likely kill - projectile tracking will use this
        /// </summary>
        public static bool s_predictiveRideCamQueued = false;
        public static EntityAlive s_predictiveTarget = null;
        
        /// <summary>
        /// Called from PredictiveAimPatch when aim prediction detects a likely kill.
        /// Sets up state for ride cam to activate when projectile is created.
        /// </summary>
        public static void StartPredictiveRideCam(EntityPlayerLocal player, EntityAlive target)
        {
            if (player == null) return;
            
            s_predictiveRideCamQueued = true;
            s_predictiveTarget = target;
            
            Log.Out($" Predictive Ride Cam QUEUED for target: {target?.EntityName ?? "NULL"}");
        }


        /// <summary>
        /// Called from PredictiveAimPatch when a projectile is launched.
        /// Immediately starts the ride cam cinematic (does not wait for kill).
        /// </summary>
        public static void StartPredictiveRideCamImmediate(EntityPlayerLocal player, ProjectileMoveScript projectile, EntityAlive target)
        {
            if (player == null || projectile == null) return;
            
            // Don't start if already in a cinematic
            if (s_isActive || s_isHitstopActive) return;
            
            var exp = CKSettings.Experimental;
            if (exp == null || !exp.EnableProjectileRideCam) return;
            
            // Start the cinematic mode
            s_isActive = true;
            s_isProjectileCameraActive = true;
            s_isRideCamActive = true;
            s_rideCamProjectileTransform = projectile.transform;
            s_predictiveTarget = target;  // Save for fallback when projectile dies
            
            // Save original camera state
            if (Camera.main != null)
            {
                s_originalCameraPos = Camera.main.transform.position;
                s_originalCameraRot = Camera.main.transform.rotation;
            }
            
            // Save player view state
            s_wasFirstPersonView = player.bFirstPersonView;
            
            // Switch to 3rd person for the projectile view
            s_savedPlayerRotation = player.transform.rotation;
            player.SetFirstPersonView(false, false);
            player.transform.rotation = s_savedPlayerRotation;
            
            // Set timer for max duration
            float duration = 5f; // Default max duration for predictive mode
            s_timer = duration;
            
            // Apply slow motion if configured
            float slowScale = s_slowScale > 0 ? s_slowScale : 0.3f;
            ApplyTimeScale(slowScale);
            
            Log.Out("[CinematicKill] Predictive Ride Cam STARTED immediately - following projectile");
        }

        public static void HandleDamageResponse(EntityAlive victim, DamageSource source, bool wasAlive, bool isCrit = false, bool wasUnaware = false)
        {
            if (!s_enabled || GameManager.IsDedicatedServer)
            {
                return;
            }

            if (victim == null || source is not DamageSourceEntity damageSource)
            {
                return;
            }

            // Prevent overlapping cinematics regardless of cooldown
            // EXCEPT: Chain Reaction mode queues additional kills
            if (s_isActive || s_isHitstopActive)
            {
                // ═══════════════════════════════════════════════════════════════════════
                // EXPERIMENTAL: Chain Reaction - Queue kills during active cinematic
                // ═══════════════════════════════════════════════════════════════════════
                var expSettings = CKSettings.Experimental;
                if (expSettings.EnableChainReaction)
                {
                    bool chainIsKill = wasAlive && (victim.IsDead() || victim.Health <= 0f);
                    CKLog.Verbose($" [EXPERIMENTAL] Chain Reaction check: Active={s_isActive}, NotHitstop={!s_isHitstopActive}, IsKill={chainIsKill}, Count={s_chainReactionKillCount}/{expSettings.ChainReactionMaxKills}");
                    
                    if (s_isActive && !s_isHitstopActive && chainIsKill && s_chainReactionKillCount < expSettings.ChainReactionMaxKills)
                    {
                        s_chainReactionList.Add(victim);
                        s_chainReactionKillCount++;
                        s_isChainReactionActive = true;
                        CKLog.Verbose($" [EXPERIMENTAL] Chain Reaction: QUEUED victim #{s_chainReactionKillCount} ({victim.EntityName}, queue size: {s_chainReactionList.Count})");
                    }
                }
                return;
            }

            bool isKill = wasAlive && (victim.IsDead() || victim.Health <= 0f);
            
            if (!isKill)
            {
                return;
            }
            
            // ═══════════════════════════════════════════════════════════════════════
            // EXPERIMENTAL: Last Stand - Check if player got a kill during Last Stand
            // ═══════════════════════════════════════════════════════════════════════
            CheckLastStandKill();

            if (s_ignoreCorpseHits && !wasAlive)
            {
                return;
            }

            var world = GameManager.Instance?.World;
            var localPlayer = world?.GetPrimaryPlayer();
            if (localPlayer == null || damageSource.getEntityId() != localPlayer.entityId)
            {
                return;
            }

            if (!CKSettings.EnableCinematics)
            {
                CKLog.Verbose(" Cinematics disabled");
                return;
            }

            // NOTE: Master chance removed - per-trigger/basic kill chance rolls handle gating
            // Any kill can trigger a cinematic unless blocked by chance, per-trigger cooldown, or RequireTriggerForCinematic

            // ═══════════════════════════════════════════════════════════════
            // WEAPON MODE DETECTION - Determine Melee/Ranged/Explosion
            // ═══════════════════════════════════════════════════════════════
            s_currentWeaponMode = DetectWeaponMode(source);
            var modeSettings = GetModeSettings(s_currentWeaponMode);
            
            CKLog.Verbose($" Detected weapon mode: {s_currentWeaponMode}");
            
            // Check if this weapon mode is enabled
            if (modeSettings != null && !modeSettings.Enabled)
            {
                CKLog.Verbose($" {s_currentWeaponMode} mode is disabled, aborting.");
                return;
            }

            // HARDCODED: Trap/turret kills NEVER trigger cinematics
            // This blocks all cinematics from trap/turret kills regardless of any settings
            if (s_currentWeaponMode == WeaponMode.Trap)
            {
                CKLog.Verbose(" Trap/Turret kills hardcoded as disabled, blocking all cinematics.");
                return;
            }

            var triggerSystem = s_cinematicSettings.MenuV2?.TriggerSystem;
            var coreSettings = s_cinematicSettings.MenuV2?.Core;
            s_lastRuntimeCooldown = 0f;
            
            // ═══════════════════════════════════════════════════════════════
            // TRIGGER SYSTEM - Triggers refine behavior, they don't gate cinematics
            // Any kill can trigger a cinematic. Triggers provide bonuses/modifications.
            // Use RequireTriggerForCinematic if you want triggers to be required.
            // ═══════════════════════════════════════════════════════════════
            CKLog.Verbose(" Kill detected. Evaluating triggers for bonuses...");

            // ═══════════════════════════════════════════════════════════════
            // HEADSHOT DETECTION - Check if hit was to the head
            // ═══════════════════════════════════════════════════════════════
            bool isHeadshot = false;
            try
            {
                var bodyPart = source.GetEntityDamageBodyPart(victim);
                isHeadshot = (bodyPart & EnumBodyPartHit.Head) != EnumBodyPartHit.None;
                if (isHeadshot)
                {
                    CKLog.Verbose(" Headshot detected!");
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"CinematicKill: Failed to detect headshot: {ex.Message}");
            }

            // Get context modifiers with extended info (headshot + killstreak + sneak)
            var mods = CinematicContextManager.Instance.GetContextModifiers(localPlayer, victim, source, isCrit, isHeadshot, s_currentStreak, wasUnaware);
            
            bool isLastEnemy = IsLastEnemy(victim, localPlayer);
            bool contextTriggered = false;
            string triggerReason = "";
            CKContextTriggerSettings activeTrigger = null;
            bool triggerWasBlockedByCooldown = false;  // Track if any valid trigger was on cooldown

            // ═══════════════════════════════════════════════════════════════════════
            // UNIFIED TRIGGER EVALUATION - Uses LIVE settings, not cached MenuV2
            // Triggers are checked in priority order (highest first)
            // ═══════════════════════════════════════════════════════════════════════
            if (CKSettings.EnableTriggers)
            {
                CKLog.Verbose(" Triggers enabled, evaluating contexts.");
                
                // Use live settings check instead of cached triggerSystem
                if (!AreLiveTriggersEnabled())
                {
                    CKLog.Verbose(" Special triggers master toggle is OFF; skipping special triggers.");
                }
                else
                {
                    // Build list of potential triggers - read LIVE from settings
                    var potentialTriggers = new System.Collections.Generic.List<(string name, CKContextTriggerSettings settings, bool contextMatches)>();
                    
                    // Check each trigger using LIVE settings (not cached)
                    // LastEnemy trigger - special: checks isLastEnemy instead of context flag
                    if (s_cinematicSettings.LastEnemy.Enabled && isLastEnemy)
                    {
                        var liveSettings = GetLiveTriggerSettings("LastEnemy");
                        if (liveSettings != null) potentialTriggers.Add(("LastEnemy", liveSettings, true));
                    }
                    
                    // Killstreak trigger
                    if (s_cinematicSettings.Killstreak.Enabled && mods.TriggeredContexts.HasFlag(CinematicContextManager.KillContext.Killstreak))
                    {
                        var liveSettings = GetLiveTriggerSettings("Killstreak");
                        if (liveSettings != null) potentialTriggers.Add(("Killstreak", liveSettings, true));
                    }
                    
                    // Dismember trigger
                    if (s_cinematicSettings.Dismember.Enabled && mods.TriggeredContexts.HasFlag(CinematicContextManager.KillContext.Dismember))
                    {
                        var liveSettings = GetLiveTriggerSettings("Dismember");
                        if (liveSettings != null) potentialTriggers.Add(("Dismember", liveSettings, true));
                    }
                    
                    // Headshot trigger
                    if (s_cinematicSettings.Headshot.Enabled && mods.TriggeredContexts.HasFlag(CinematicContextManager.KillContext.Headshot))
                    {
                        var liveSettings = GetLiveTriggerSettings("Headshot");
                        if (liveSettings != null) potentialTriggers.Add(("Headshot", liveSettings, true));
                    }
                    
                    // Critical trigger
                    if (s_cinematicSettings.Critical.Enabled && mods.TriggeredContexts.HasFlag(CinematicContextManager.KillContext.Crit))
                    {
                        var liveSettings = GetLiveTriggerSettings("Crit");
                        if (liveSettings != null) potentialTriggers.Add(("Crit", liveSettings, true));
                    }
                    
                    // LongRange trigger
                    if (s_cinematicSettings.LongRange.Enabled && mods.TriggeredContexts.HasFlag(CinematicContextManager.KillContext.LongRange))
                    {
                        var liveSettings = GetLiveTriggerSettings("LongRange");
                        if (liveSettings != null) potentialTriggers.Add(("LongRange", liveSettings, true));
                    }
                    
                    // LowHealth trigger
                    if (s_cinematicSettings.LowHealth.Enabled && mods.TriggeredContexts.HasFlag(CinematicContextManager.KillContext.LowHealth))
                    {
                        var liveSettings = GetLiveTriggerSettings("LowHealth");
                        if (liveSettings != null) potentialTriggers.Add(("LowHealth", liveSettings, true));
                    }
                    
                    // Sneak trigger - log for debugging
                    bool sneakEnabled = s_cinematicSettings.Sneak.Enabled;
                    bool sneakContext = mods.TriggeredContexts.HasFlag(CinematicContextManager.KillContext.Sneak);
                    CKLog.Verbose($" Sneak check - TriggerEnabled:{sneakEnabled}, ContextSneak:{sneakContext}");
                    if (sneakEnabled && sneakContext)
                    {
                        var liveSettings = GetLiveTriggerSettings("Sneak");
                        if (liveSettings != null) potentialTriggers.Add(("Sneak", liveSettings, true));
                    }
                    
                    // Triggers are evaluated in list order - first matching trigger wins
                    // No stacking, no bonuses - each trigger uses its own settings only
                    float masterChance = GetLiveMasterTriggerChance();
                    
                    foreach (var (name, settings, _) in potentialTriggers)
                    {
                        // Check cooldown - only remaining gate for triggers
                        float cooldownRemaining = GetTriggerCooldownRemaining(name);
                        if (settings.CooldownSeconds > 0 && cooldownRemaining > 0)
                        {
                            CKLog.Verbose($" {name} trigger on cooldown ({cooldownRemaining:F1}s remaining).");
                            triggerWasBlockedByCooldown = true;  // A valid trigger exists but is on cooldown
                            continue;
                        }
                        
                        // Chance roll - check if trigger should fire based on configured probability
                        // Use per-trigger chance if OverrideChance=true, otherwise use master trigger chance
                        float effectiveChance = settings.OverrideChance 
                            ? settings.ChancePercent 
                            : masterChance;
                        float chanceRoll = UnityEngine.Random.Range(0f, 100f);
                        if (chanceRoll > effectiveChance)
                        {
                            string chanceSource = settings.OverrideChance ? "override" : "master";
                            CKLog.Verbose($" {name} chance roll {chanceRoll:F1}% > {effectiveChance:F1}% ({chanceSource}) => FAIL");
                            continue;
                        }
                        
                        // First matching trigger wins - use its settings
                        contextTriggered = true;
                        triggerReason = name;
                        activeTrigger = settings;
                        
                        // Start cooldown for this trigger
                        StartTriggerCooldown(name, settings.CooldownSeconds);
                        
                        CKLog.Verbose($" {name} trigger activated (chance: {effectiveChance:F0}% {(settings.OverrideChance ? "[override]" : "[master]")}, roll: {chanceRoll:F1}%)");
                        break; // First match wins - no stacking
                    }
                    
                    if (!contextTriggered)
                    {
                        CKLog.Verbose(" No context trigger matched (all on cooldown or none enabled).");
                    }
                } // end AreLiveTriggersEnabled check
            } // end EnableTriggers

            // ═══════════════════════════════════════════════════════════════════════
            // BASIC KILL: If no special/contextual trigger fired, fall back to Basic Kill settings
            // UNLESS a special trigger was detected but blocked by cooldown - then skip entirely
            // ═══════════════════════════════════════════════════════════════════════
            if (!contextTriggered)
            {
                // If a special trigger was detected but blocked by cooldown, skip BasicKill fallback
                if (triggerWasBlockedByCooldown)
                {
                    ClearQueuedProjectile(victim);
                    CKLog.Verbose(" Special trigger was on cooldown; skipping BasicKill fallback.");
                    return;
                }

                // Use LIVE BasicKill settings instead of cached
                var basicTrigger = GetLiveBasicKillSettings();
                if (basicTrigger?.Enabled == true)
                {
                    float cooldownRemaining = GetTriggerCooldownRemaining("BasicKill");
                    if (cooldownRemaining > 0f)
                    {
                        CKLog.Verbose($" Basic Kill on cooldown ({cooldownRemaining:F1}s remaining).");
                    }
                    else
                    {
                        float roll = UnityEngine.Random.Range(0f, 100f);
                        if (roll <= basicTrigger.ChancePercent)
                        {
                            CKLog.Verbose($" Basic Kill chance roll {roll:F2}% <= {basicTrigger.ChancePercent:F2}% => PASS");
                            contextTriggered = true;
                            triggerReason = "BasicKill";
                            activeTrigger = basicTrigger;
                            StartTriggerCooldown("BasicKill", basicTrigger.CooldownSeconds);
                        }
                        else
                        {
                            CKLog.Verbose($" Basic Kill chance roll {roll:F2}% > {basicTrigger.ChancePercent:F2}% => FAIL");
                        }
                    }
                }

                if (!contextTriggered)
                {
                    // Check live settings for RequireTriggerForCinematic
                    if (triggerSystem?.RequireTriggerForCinematic == true)
                    {
                        ClearQueuedProjectile(victim);
                        CKLog.Verbose(" No trigger matched and RequireTriggerForCinematic is ON - skipping cinematic.");
                        return;
                    }

                    ClearQueuedProjectile(victim);
                    CKLog.Verbose(" No trigger matched; skipping cinematic.");
                    return;
                }
            }

            // Determine if we should start sequence
            bool shouldTrigger = contextTriggered;

            if (shouldTrigger)
            {
                CKLog.Verbose($" Triggering cinematic (reason: {triggerReason})");
                
                // ═══════════════════════════════════════════════════════════════════════
                // EXPERIMENTAL: Trigger special visual effects based on kill CONTEXT
                // These trigger when the context occurred, regardless of which trigger won
                // ═══════════════════════════════════════════════════════════════════════
                var exp = CKSettings.Experimental;
                var fx = s_cinematicSettings.ScreenEffects;
                
                // Log all active contexts for debugging
                CKLog.Verbose($" [EFFECTS] Contexts: {mods.TriggeredContexts} | KillFlash:{fx.EnableKillFlash} BloodSplatter:{fx.EnableBloodSplatter} DismemberCam:{exp.EnableDismemberFocusCam} Chain:{exp.EnableChainReaction}");
                
                // Check context for DismemberFocusCam (still context-dependent)
                bool hasDismemberContext = mods.TriggeredContexts.HasFlag(CinematicContextManager.KillContext.Dismember);
                
                // Determine if this is a Basic Kill or Special Trigger
                // Basic Kill = triggered by BasicKill system, Special = triggered by trigger system
                bool isBasicKill = (triggerReason == "Basic Kill");
                bool isSpecialTrigger = !isBasicKill;
                
                // Get cinematic duration to use for effects (they last as long as the cinematic)
                float cinematicDuration = isBasicKill 
                    ? s_cinematicSettings.BasicKill.Duration 
                    : s_cinematicSettings.TriggerDefaults.Duration;
                
                // Kill Flash - bright flash effect
                if (fx.EnableKillFlash)
                {
                    bool shouldTriggerFlash = (isBasicKill && fx.EnableKillFlash_BasicKill) || 
                                              (isSpecialTrigger && fx.EnableKillFlash_SpecialTrigger);
                    if (shouldTriggerFlash)
                    {
                        CKLog.Verbose($" Kill Flash TRIGGERED (duration={cinematicDuration}s, intensity={fx.KillFlashIntensity})");
                        CinematicScreenEffects.Instance.TriggerKillFlash(cinematicDuration, fx.KillFlashIntensity);
                    }
                }
                
                // Blood Splatter - screen blood overlay
                if (fx.EnableBloodSplatter)
                {
                    bool shouldTriggerBlood = (isBasicKill && fx.EnableBloodSplatter_BasicKill) || 
                                              (isSpecialTrigger && fx.EnableBloodSplatter_SpecialTrigger);
                    if (shouldTriggerBlood)
                    {
                        CKLog.Verbose($" Blood Splatter TRIGGERED (duration={cinematicDuration}s, intensity={fx.BloodSplatterIntensity})");
                        CinematicScreenEffects.Instance.TriggerBloodSplatter(cinematicDuration, fx.BloodSplatterIntensity);
                    }
                }
                
                // X-Ray Vision - high-contrast flash effect
                if (fx.EnableXRayVision)
                {
                    bool shouldTriggerXRay = (isBasicKill && fx.EnableXRayVision_BasicKill) || 
                                             (isSpecialTrigger && fx.EnableXRayVision_SpecialTrigger);
                    if (shouldTriggerXRay)
                    {
                        CKLog.Verbose($" X-Ray Vision TRIGGERED (duration={cinematicDuration}s, intensity={fx.XRayVisionIntensity})");
                        CinematicScreenEffects.Instance.TriggerKillFlash(cinematicDuration, fx.XRayVisionIntensity);
                    }
                }
                
                // Dismemberment Focus Cam - try to find and track the severed limb
                if (exp.EnableDismemberFocusCam)
                {
                    if (hasDismemberContext)
                    {
                        CKLog.Verbose($" [EXPERIMENTAL] Dismember Focus Cam ACTIVATING");
                        TryActivateDismemberFocusCam(victim);
                    }
                    else
                    {
                        CKLog.Verbose($" [EXPERIMENTAL] Dismember Focus Cam enabled but no dismember context");
                    }
                }
            }
            else
            {
                CKLog.Verbose(" No trigger conditions met, skipping cinematic.");
                return;
            }

            if (activeTrigger != null)
            {
                bool isBasicKillTrigger = triggerSystem?.BasicKill != null && ReferenceEquals(activeTrigger, triggerSystem.BasicKill);

                // Clear context modifiers - each trigger uses only its own settings
                mods.BonusDuration = 0f;
                mods.BonusSlowScale = 0f;
                
                // Set effect overrides based on trigger (special triggers only)
                if (!isBasicKillTrigger && (activeTrigger.OverrideDuration || activeTrigger.OverrideSlowScale || activeTrigger.OverrideHitstop))
                {
                    SetTriggerEffectOverride(triggerReason);
                }
                
                float runtimeSlow;
                float runtimeDuration;
                float runtimeCooldown;

                if (isBasicKillTrigger)
                {
                    runtimeSlow = activeTrigger.RandomizeSlowScale
                        ? UnityEngine.Random.Range(activeTrigger.SlowScaleMin, activeTrigger.SlowScaleMax)
                        : activeTrigger.SlowScale;
                    runtimeDuration = activeTrigger.RandomizeDuration
                        ? UnityEngine.Random.Range(activeTrigger.DurationMin, activeTrigger.DurationMax)
                        : activeTrigger.DurationSeconds;
                    runtimeCooldown = activeTrigger.CooldownSeconds;
                    CKLog.Verbose($" Basic Kill settings - Scale: {runtimeSlow:F2}x, Duration: {runtimeDuration:F1}s, Cooldown: {runtimeCooldown:F1}s");
                }
                else
                {
                    // Use the values from the trigger - they were already resolved
                    // by FromTrigger/FromSimple to use override or default values
                    runtimeSlow = activeTrigger.RandomizeSlowScale
                        ? UnityEngine.Random.Range(activeTrigger.SlowScaleMin, activeTrigger.SlowScaleMax)
                        : activeTrigger.SlowScale;

                    runtimeDuration = activeTrigger.RandomizeDuration
                        ? UnityEngine.Random.Range(activeTrigger.DurationMin, activeTrigger.DurationMax)
                        : activeTrigger.DurationSeconds;

                    runtimeCooldown = activeTrigger.CooldownSeconds;

                    CKLog.Verbose($" {(activeTrigger.OverrideSlowScale ? "Trigger override" : "Trigger defaults")} - Scale: {runtimeSlow:F2}x, Duration: {runtimeDuration:F1}s, Cooldown: {runtimeCooldown:F1}s");
                }

                // No bonuses - each trigger uses its own settings only
                // Duration and TimeScale already set from trigger's override or defaults

                s_slowScale = runtimeSlow;
                s_duration = runtimeDuration;
                s_lastRuntimeCooldown = runtimeCooldown;

                // Use trigger-based camera routing
                bool useProjectile;
                if (ChooseModeFromTrigger(activeTrigger, isLastEnemy, out useProjectile))
                {
                    if (useProjectile)
                    {
                        // Build projectile runtime with the already-computed duration/scale
                        bool isSpecialTrigger = !isBasicKillTrigger;
                        BuildProjectileRuntime(
                            isLastEnemy,
                            forceUse: true,
                            triggerOverride: activeTrigger,
                            modeOverride: modeSettings,
                            runtimeDurationOverride: runtimeDuration,
                            runtimeSlowOverride: runtimeSlow,
                            isSpecialTrigger: isSpecialTrigger);
                        
                        // Check for freeze frame (uses per-camera settings)
                        StartFreezeFrame(triggerReason, false); // false = projectile camera
                        
                        // Start projectile/hitscan camera (runs after freeze if enabled)
                        if (TryConsumeQueuedProjectile(victim, out var queuedProjectile) && queuedProjectile != null)
                        {
                            TriggerProjectileCamera(queuedProjectile, victim, triggerReason, skipChecks: true);
                        }
                        else
                        {
                            TriggerHitscanCamera(victim, victim.GetPosition(), triggerReason, skipChecks: true);
                        }
                        return;
                    }
                    // First-person was chosen by ChooseModeFromTrigger - proceed to StartSequence
                }
                else
                {
                    ClearQueuedProjectile(victim);
                    CKLog.Verbose(" No allowed mode for this trigger.");
                    return;
                }

                // ChooseModeFromTrigger already validated camera selection for triggers with Override
                // For non-override special triggers, the global settings were applied in ChooseModeFromTrigger
                // No additional check needed - proceed to start first-person cinematic
                ClearQueuedProjectile(victim);
                if (!s_cinematicSettings.EnableFirstPersonSlowMo)
                {
                    mods.SlowScaleMultiplier = 1f / Mathf.Max(0.01f, s_slowScale);
                }
                CKLog.Verbose($" Starting first-person cinematic. Trigger={triggerReason}");
                
                // Check for freeze frame (uses per-camera settings)
                StartFreezeFrame(triggerReason, true); // true = first person camera
                
                StartSequence(localPlayer, mods, triggerReason, isLastEnemy);
            }
            else
            {
                CKLog.Verbose(" No trigger succeeded; no cinematic started.");
            }
        }

        public static void HandleDismember(EntityAlive victim)
        {
            if (!s_enabled || !CKSettings.EnableDismember || GameManager.IsDedicatedServer)
            {
                return;
            }

            if (victim == null || victim.IsDead())
            {
                return;
            }

            // Dismember Cooldown Check
            if (s_cinematicSettings.EnableCooldownDismember && s_cooldownDismember > 0f)
            {
                return;
            }

            // Chance Check
            if (UnityEngine.Random.value > CKSettings.ChanceDismember)
            {
                return;
            }

            if (!CKSettings.DismemberAllowFirstPerson)
            {
                CKLog.Verbose(" Dismember blocked (FP not allowed).");
                return;
            }

            var world = GameManager.Instance?.World;
            var localPlayer = world?.GetPrimaryPlayer();
            if (localPlayer == null)
            {
                return;
            }
            bool isLastEnemy = IsLastEnemy(victim, localPlayer);

            EntityAlive attacker = null;
            if (victim.GetRevengeTarget() is EntityPlayerLocal player && player == localPlayer)
            {
                attacker = player;
            }
            else
            {
                return;
            }

            // Dismember is treated as a special context
            var mods = new CinematicContextManager.ContextModifiers
            {
                DurationMultiplier = 1f,
                SlowScaleMultiplier = 1f,
                ZoomMultiplier = CKSettings.CritZoomMultiplier,
                ZoomSpeedMultiplier = CKSettings.CritZoomSpeed,
                ScreenTint = new Color(0f, 0f, 0.2f, 0.2f),
                TriggerFlash = true,
                DebugInfo = "Dismember",
                TriggeredContexts = CinematicContextManager.KillContext.Dismember,
                TargetDistance = float.MaxValue // Default to far so blood splatter doesn't trigger incorrectly
            };

            if (CKSettings.DismemberCustomEffects)
            {
                SetTriggerEffectOverride("Dismember");
            }

            // ═══════════════════════════════════════════════════════════════════════
            // EXPERIMENTAL: X-Ray Vision - Screen effect on dismemberment kills
            // ═══════════════════════════════════════════════════════════════════════
            var exp = CKSettings.Experimental;
            if (exp.EnableXRayVision)
            {
                try
                {
                    var screenEffects = localPlayer?.ScreenEffectManager;
                    if (screenEffects != null)
                    {
                        // VibrantDeSat creates a high-contrast desaturated look
                        screenEffects.SetScreenEffect("VibrantDeSat", exp.XRayIntensity, exp.XRayDuration);
                        CKLog.Verbose($" X-Ray Vision triggered (intensity: {exp.XRayIntensity}, duration: {exp.XRayDuration}s)");
                    }
                }
                catch (System.Exception ex)
                {
                    CKLog.Verbose($" X-Ray Vision failed: {ex.Message}");
                }
            }

            StartSequence(localPlayer, mods, "Dismember", isLastEnemy);
        }

        /// <summary>
        /// Called by OnEntityDeath Harmony patch - fallback death detection
        /// Used when damage-based detection might have missed the kill
        /// </summary>
        public static void HandleEntityDeath(EntityAlive victim)
        {
            if (!s_enabled || GameManager.IsDedicatedServer || victim == null) return;
            
            // Only log for debugging - the primary cinematic is triggered by HandleDamageResponse
            // This serves as a hook point for future improvements
            CKLog.Verbose($" OnEntityDeath fired for entity {victim.entityId} ({victim.EntityName})");
            
            // Clean up ragdoll tracking for this entity after a delay
            // (keep the ragdoll reference for a bit in case camera is still tracking it)
        }

        /// <summary>
        /// Called by SetDead Harmony patch - captures ragdoll transform for camera tracking
        /// </summary>
        public static void SetRagdollTarget(int entityId, Transform ragdollRoot)
        {
            if (ragdollRoot == null) return;
            
            s_ragdollTargets[entityId] = ragdollRoot;
            CKLog.Verbose($" Captured ragdoll transform for entity {entityId}");
            
            // If this is the entity we're currently tracking with projectile camera, update reference
            if (s_isProjectileCameraActive && s_currentRagdollEntityId == entityId)
            {
                s_currentRagdollTarget = ragdollRoot;
            }
        }

        /// <summary>
        /// Gets the ragdoll transform for an entity if available
        /// </summary>
        public static Transform GetRagdollTransform(int entityId)
        {
            if (s_ragdollTargets.TryGetValue(entityId, out Transform transform))
            {
                return transform;
            }
            return null;
        }

        /// <summary>
        /// Starts ragdoll tracking for projectile camera
        /// </summary>
        private static void StartRagdollTracking(EntityAlive victim)
        {
            if (victim == null) return;
            
            s_currentRagdollEntityId = victim.entityId;
            s_ragdollGroundDetectionActive = false;
            s_ragdollSettledTime = 0f;
            s_ragdollRigidbodies = null;
            
            // Try to get existing ragdoll transform
            if (s_ragdollTargets.TryGetValue(victim.entityId, out Transform existing))
            {
                s_currentRagdollTarget = existing;
                // Capture rigidbodies for velocity monitoring
                CaptureRagdollRigidbodies(existing);
            }
            else
            {
                // Fallback to entity transform
                s_currentRagdollTarget = victim.transform;
                CaptureRagdollRigidbodies(victim.transform);
            }
            
            // Enable ground detection if the correct section-specific AND camera-specific ragdoll setting is on
            // Basic Kill uses BK settings, Special Triggers use TD settings
            bool isBasicKill = LastTriggerReason == "BasicKill";
            bool shouldUseRagdoll = false;
            float postLandDelay = 0.3f;
            
            if (s_isProjectileCameraActive)
            {
                shouldUseRagdoll = isBasicKill 
                    ? s_cinematicSettings?.EnableDynamicRagdollDuration_BK_Proj == true
                    : s_cinematicSettings?.EnableDynamicRagdollDuration_TD_Proj == true;
                postLandDelay = isBasicKill 
                    ? s_cinematicSettings.RagdollPostLandDelay_BK_Proj 
                    : s_cinematicSettings.RagdollPostLandDelay_TD_Proj;
            }
            else
            {
                shouldUseRagdoll = isBasicKill 
                    ? s_cinematicSettings?.EnableDynamicRagdollDuration_BK_FP == true
                    : s_cinematicSettings?.EnableDynamicRagdollDuration_TD_FP == true;
                postLandDelay = isBasicKill 
                    ? s_cinematicSettings.RagdollPostLandDelay_BK_FP 
                    : s_cinematicSettings.RagdollPostLandDelay_TD_FP;
            }
            
            if (shouldUseRagdoll)
            {
                s_ragdollGroundDetectionActive = true;
                s_ragdollPostLandDelay = postLandDelay;
                
                // Extend duration to fallback timeout so ragdoll has time to settle
                // The cinematic will end EITHER when ragdoll settles OR when fallback expires
                float fallbackDuration = s_cinematicSettings.RagdollFallbackDuration;
                if (s_timer < fallbackDuration)
                {
                    CKLog.Verbose($" Extending duration from {s_timer:F2}s to fallback {fallbackDuration:F2}s for ragdoll detection");
                    s_timer = fallbackDuration;
                }
                
                string cameraMode = s_isProjectileCameraActive ? "Projectile" : "FP";
                string triggerMode = isBasicKill ? "BasicKill" : "SpecialTrigger";
                CKLog.Verbose($" Dynamic ragdoll duration enabled ({triggerMode} {cameraMode}) - waiting for ragdoll to settle (fallback: {fallbackDuration:F2}s)");
            }
        }

        /// <summary>
        /// Captures all Rigidbody components from ragdoll for velocity monitoring
        /// </summary>
        private static void CaptureRagdollRigidbodies(Transform root)
        {
            if (root == null) return;
            
            try
            {
                s_ragdollRigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
                if (s_ragdollRigidbodies != null && s_ragdollRigidbodies.Length > 0)
                {
                    CKLog.Verbose($" Captured {s_ragdollRigidbodies.Length} rigidbodies for velocity monitoring");
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"CinematicKill: Failed to capture ragdoll rigidbodies: {ex.Message}");
                s_ragdollRigidbodies = null;
            }
        }

        /// <summary>
        /// Checks if ragdoll has settled (low velocity across all rigidbodies)
        /// </summary>
        private static bool IsRagdollSettled()
        {
            if (s_ragdollRigidbodies == null || s_ragdollRigidbodies.Length == 0)
            {
                return false;
            }
            
            float maxVelocity = 0f;
            int sleepingCount = 0;
            
            foreach (var rb in s_ragdollRigidbodies)
            {
                if (rb == null) continue;
                
                // Check velocity magnitude
                float velocity = rb.velocity.magnitude;
                if (velocity > maxVelocity)
                {
                    maxVelocity = velocity;
                }
                
                // Count sleeping rigidbodies
                if (rb.IsSleeping())
                {
                    sleepingCount++;
                }
            }
            
            // Consider settled if:
            // 1. Max velocity is below threshold, OR
            // 2. Most rigidbodies are sleeping
            bool velocitySettled = maxVelocity < s_ragdollSettledThreshold;
            bool mostSleeping = sleepingCount >= s_ragdollRigidbodies.Length * 0.7f;
            
            return velocitySettled || mostSleeping;
        }

        /// <summary>
        /// Updates ragdoll ground detection and returns true if cinematic should end
        /// </summary>
        private static bool UpdateRagdollGroundDetection(float deltaTime)
        {
            if (!s_ragdollGroundDetectionActive) return false;
            
            if (IsRagdollSettled())
            {
                s_ragdollSettledTime += deltaTime;
                
                if (s_ragdollSettledTime >= s_ragdollSettledDuration + s_ragdollPostLandDelay)
                {
                    CKLog.Verbose($" Ragdoll settled for {s_ragdollSettledTime:F2}s - ending cinematic");
                    s_ragdollGroundDetectionActive = false;
                    return true; // Signal to end cinematic
                }
            }
            else
            {
                // Reset if ragdoll starts moving again
                s_ragdollSettledTime = 0f;
            }
            
            return false;
        }

        /// <summary>
        /// Stops ragdoll tracking
        /// </summary>
        private static void StopRagdollTracking()
        {
            if (s_ragdollGroundDetectionActive)
            {
                CKLog.Verbose($" StopRagdollTracking: Deactivating ragdoll ground detection (was tracking entity {s_currentRagdollEntityId})");
            }
            s_currentRagdollTarget = null;
            s_currentRagdollEntityId = -1;
            s_ragdollRigidbodies = null;
            s_ragdollGroundDetectionActive = false;
            s_ragdollSettledTime = 0f;
        }

        #region Audio Slow-Motion
        /// <summary>
        /// Applies slow-motion pitch shift to audio
        /// </summary>
        private static void ApplyAudioSlowMotion(float timeScale)
        {
            if (s_audioSlowMoActive) return;
            
            try
            {
                // Get the audio listener or main audio source
                var listener = AudioListener.volume;
                s_originalAudioPitch = Time.timeScale > 0 ? Time.timeScale : 1f;
                
                // Apply pitch shift based on time scale for dramatic effect
                // This creates the classic slow-motion audio effect
                AudioListener.volume = Mathf.Max(0.3f, timeScale); // Reduce volume slightly
                s_audioSlowMoActive = true;
                
                CKLog.Verbose($" Audio slow-mo applied (timeScale: {timeScale})");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"CinematicKill: Failed to apply audio slow-mo: {ex.Message}");
            }
        }

        /// <summary>
        /// Restores normal audio pitch
        /// </summary>
        private static void RestoreAudioPitch()
        {
            if (!s_audioSlowMoActive) return;
            
            try
            {
                AudioListener.volume = 1f;
                s_audioSlowMoActive = false;
                CKLog.Verbose(" Audio restored to normal");
            }
            catch { }
        }
        #endregion

        private static bool IsLastEnemy(EntityAlive victim, EntityPlayerLocal player)
        {
            if (victim == null || player == null)
            {
                return true;
            }

            var world = GameManager.Instance?.World;
            if (world == null)
            {
                return true;
            }

            var entities = world.Entities.list;
            var radiusSqr = CKSettings.EnemyScanRadius * CKSettings.EnemyScanRadius;
            var playerPos = player.GetPosition();

            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i] as EntityEnemy;
                if (entity == null || entity == victim || entity.IsDead() || entity.Health <= 0)
                {
                    continue;
                }

                if (entity.GetDistanceSq(playerPos) > radiusSqr)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static void UpdateTimers(float delta)
        {
            if (s_cooldownCrit > 0f) s_cooldownCrit = Mathf.Max(0f, s_cooldownCrit - delta);
            if (s_cooldownDismember > 0f) s_cooldownDismember = Mathf.Max(0f, s_cooldownDismember - delta);
            if (s_cooldownLongRange > 0f) s_cooldownLongRange = Mathf.Max(0f, s_cooldownLongRange - delta);
            if (s_cooldownLowHealth > 0f) s_cooldownLowHealth = Mathf.Max(0f, s_cooldownLowHealth - delta);
            if (s_cooldownLastEnemy > 0f) s_cooldownLastEnemy = Mathf.Max(0f, s_cooldownLastEnemy - delta);

            // Update Killstreak - Use MenuV2 settings (UI must initialize)
            var ksSettings = s_cinematicSettings.MenuV2.Killstreak;
            float streakTimeout = ksSettings.StreakTimeout;
            bool killstreaksEnabled = ksSettings.Enabled;
            
            if (killstreaksEnabled && s_currentStreak > 0)
            {
                if (Time.time - s_lastKillTime > streakTimeout)
                {
                    s_currentStreak = 0;
                    CKLog.Verbose($" Streak reset (timeout: {streakTimeout}s)");
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // SLOW-MO TOGGLE - Manual slow motion control via keybind
            // ═══════════════════════════════════════════════════════════════
            var exp = CKSettings.Experimental;
            if (exp.EnableSlowMoToggle && Input.GetKeyDown(exp.SlowMoToggleKey))
            {
                if (s_isSlowMoToggleActive)
                {
                    // Turn off slow-mo
                    Time.timeScale = s_slowMoToggleOriginalTimeScale;
                    s_isSlowMoToggleActive = false;
                    CKLog.Verbose($" SlowMo Toggle OFF - restored time scale to {s_slowMoToggleOriginalTimeScale:F2}");
                }
                else
                {
                    // Turn on slow-mo
                    s_slowMoToggleOriginalTimeScale = Time.timeScale;
                    Time.timeScale = exp.SlowMoToggleTimeScale;
                    s_isSlowMoToggleActive = true;
                    CKLog.Verbose($" SlowMo Toggle ON - time scale set to {exp.SlowMoToggleTimeScale:F2}");
                }
            }

            // Handle Hitstop
            if (s_isHitstopActive)
            {
                s_hitstopTimer -= Time.unscaledDeltaTime; // Use unscaled time since timeScale is 0
                if (s_hitstopTimer <= 0f)
                {
                    s_isHitstopActive = false;
                    ApplyTimeScale(s_resumeScale); // Resume to slow motion
                    CKLog.Verbose(" Hitstop ended, resuming slow motion");
                }
                return; // Don't update slowmo timer while in hitstop
            }
            
            // ═══════════════════════════════════════════════════════════════
            // FREEZE FRAME DELAY - Wait before freezing
            // ═══════════════════════════════════════════════════════════════
            if (s_isFreezeDelayActive)
            {
                s_freezeDelayTimer -= Time.unscaledDeltaTime;
                if (s_freezeDelayTimer <= 0f)
                {
                    ApplyFreeze();
                }
                return; // Don't update anything else during delay
            }
            
            // ═══════════════════════════════════════════════════════════════
            // FREEZE FRAME ACTIVE - Complete pause
            // ═══════════════════════════════════════════════════════════════
            if (s_isFreezeFrameActive)
            {
                s_freezeFrameTimer -= Time.unscaledDeltaTime;
                
                if (s_freezeFrameTimer <= 0f)
                {
                    EndFreezeFrame();
                }
                return; // Don't update anything else while frozen
            }

            // ═══════════════════════════════════════════════════════════════════════
            // EXPERIMENTAL: Last Stand - Update timer and check for failure
            // NOTE: This must run BEFORE the !s_isActive check since Last Stand
            //       operates independently of regular cinematic sequences
            // ═══════════════════════════════════════════════════════════════════════
            if (s_isLastStandActive)
            {
                // Use deltaTime (scaled) so duration is in slow-mo game time
                // e.g., 5s duration at 0.1x timescale = 5s of slow-mo experience
                s_lastStandTimer -= Time.deltaTime;
                if (s_lastStandTimer <= 0f)
                {
                    // Player failed to get a kill - they die
                    EndLastStand(false);
                }
            }
            
            // Update Last Stand cooldown (always runs, independent of cinematics)
            if (s_lastStandCooldownTimer > 0f)
            {
                s_lastStandCooldownTimer -= Time.unscaledDeltaTime;
            }

            if (!s_isActive)
            {
                return;
            }

            // Start timed restore during slowmo if configured
            if (!s_isRestoringTime && s_timer <= s_returnStartTime)
            {
                s_isRestoringTime = true;
                SmoothRestoreTimeScale(s_returnDuration);
            }

            s_timer -= Mathf.Max(0f, delta);
            
            // Check for dynamic ragdoll duration - end when ragdoll settles
            bool ragdollSettled = UpdateRagdollGroundDetection(delta);
            
            if (s_timer <= 0f || ragdollSettled)
            {
                if (ragdollSettled)
                {
                    CKLog.Verbose("Ragdoll settled - ending cinematic early");
                }
                
                // ═══════════════════════════════════════════════════════════════════════
                // EXPERIMENTAL: Chain Reaction - Check for queued victims before ending
                // ═══════════════════════════════════════════════════════════════════════
                if (s_isChainReactionActive && s_chainReactionList.Count > 0)
                {
                    var nextVictim = s_chainReactionList[0];
                    s_chainReactionList.RemoveAt(0);
                    if (nextVictim != null && nextVictim.transform != null)
                    {
                        // Transition camera to next victim
                        s_projectileCameraTarget = nextVictim.transform;
                        s_timer = CKSettings.Experimental.ChainReactionWindow;
                        
                        // Apply slow-mo ramp if enabled
                        if (CKSettings.Experimental.ChainReactionSlowMoRamp)
                        {
                            float newScale = s_slowScale * CKSettings.Experimental.ChainSlowMoMultiplier;
                            newScale = Mathf.Max(MinScale, newScale);
                            ApplyTimeScale(newScale);
                            s_slowScale = newScale;
                        }
                        
                        CKLog.Verbose($" Chain Reaction: Transitioning to victim #{s_chainReactionKillCount - s_chainReactionList.Count}");
                        return; // Continue the cinematic with new victim
                    }
                }
                
                // Reset Chain Reaction state
                s_isChainReactionActive = false;
                s_chainReactionKillCount = 0;
                s_chainReactionList.Clear();
                
                CancelSequence();
            }
        }
        
        // ═══════════════════════════════════════════════════════════════
        //  FREEZE FRAME (Simplified)
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Start the freeze frame effect (legacy overload)
        /// </summary>
        private static void StartFreezeFrame()
        {
            StartFreezeFrame("Unknown", false);
        }
        
        /// <summary>
        /// Start the freeze frame effect (legacy overload with trigger reason)
        /// </summary>
        private static void StartFreezeFrame(string triggerReason)
        {
            // Determine if this is a first-person or projectile camera based on current mode
            bool isFirstPerson = !s_isProjectileCameraActive;
            StartFreezeFrame(triggerReason, isFirstPerson);
        }
        
        /// <summary>
        /// Start the freeze frame effect with camera type specification
        /// </summary>
        private static void StartFreezeFrame(string triggerReason, bool isFirstPerson)
        {
            // Get the appropriate freeze settings based on camera type
            var settings = isFirstPerson ? CKSettings.FPFreezeFrame : CKSettings.ProjectileFreezeFrame;
            
            // Check if freeze is enabled for this camera type
            if (!settings.Enabled)
            {
                CKLog.Verbose($" Freeze frame disabled for {(isFirstPerson ? "FP" : "Projectile")} camera");
                return;
            }
            
            // Chance roll
            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll > settings.Chance)
            {
                CKLog.Verbose($" Freeze frame chance failed: {roll:F1}% > {settings.Chance:F1}%");
                return;
            }
            
            s_freezeIsFirstPerson = isFirstPerson;
            s_freezeTriggerReason = triggerReason;
            
            // Determine if we should continue cinematic after freeze based on settings
            s_postFreezeContinueCinematic = settings.PostAction == PostFreezeAction.ContinueCinematic || 
                                            settings.PostAction == PostFreezeAction.SwitchCamera;
            
            if (settings.Delay > 0f)
            {
                // Start delay before freeze
                s_isFreezeDelayActive = true;
                s_freezeDelayTimer = settings.Delay;
                CKLog.Verbose($" Freeze frame ({(isFirstPerson ? "FP" : "Proj")}) delay started: {settings.Delay:F2}s before {settings.Duration:F2}s freeze");
            }
            else
            {
                // No delay, freeze immediately
                ApplyFreeze();
            }
        }
        
        /// <summary>
        /// Actually apply the freeze (called after delay or immediately)
        /// </summary>
        private static void ApplyFreeze()
        {
            var settings = s_freezeIsFirstPerson ? CKSettings.FPFreezeFrame : CKSettings.ProjectileFreezeFrame;
            
            s_isFreezeDelayActive = false;
            s_isFreezeFrameActive = true;
            s_freezeFrameTimer = settings.Duration;
            s_freezeCameraDriftAngle = 0f;
            
            var player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player == null) return;
            
            // Store original camera state for potential restoration
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                s_freezeOriginalCameraPos = mainCam.transform.position;
                s_freezeOriginalCameraRot = mainCam.transform.rotation;
            }
            s_freezeWasFirstPerson = player.bFirstPersonView;
            
            // Apply super slow motion (not complete freeze) for cinematic feel
            float timeScale = settings.EnableCameraMovement ? settings.TimeScale : 0f;
            ApplyTimeScale(timeScale);
            CKLog.Verbose($" Freeze frame ({(s_freezeIsFirstPerson ? "FP" : "Proj")}) ACTIVE: {settings.Duration:F2}s at {timeScale:F3}x speed");
            
            // FIRST PERSON FREEZE: Use FOV zoom only
            if (s_freezeIsFirstPerson)
            {
                s_freezeCameraPresetIndex = -1;
                
                // Dramatic zoom during freeze
                float zoomDuration = settings.Duration * 0.7f;
                float holdDuration = settings.Duration * 0.3f;
                StartFOVEffect(player, 15f, zoomDuration, holdDuration, 0.1f, FOVMode.ZoomIn);
                CKLog.Verbose($" FP Freeze: dramatic zoom started (15% over {zoomDuration:F2}s)");
            }
            // PROJECTILE FREEZE: Use preset camera positioning
            else if (settings.EnableCameraMovement && s_projectileCameraTarget != null)
            {
                // Select a preset (random or first enabled)
                var presets = StandardCameraPreset.All;
                if (presets != null && presets.Length > 0)
                {
                    if (settings.RandomizePreset)
                    {
                        s_freezeCameraPresetIndex = UnityEngine.Random.Range(0, presets.Length);
                    }
                    else
                    {
                        s_freezeCameraPresetIndex = 0;
                    }
                    
                    var preset = presets[s_freezeCameraPresetIndex];
                    s_freezeCameraTarget = s_projectileCameraTarget;
                    
                    CKLog.Verbose($" Proj Freeze: camera using preset: {preset.Name}");
                    
                    // Position camera according to preset relative to victim
                    PositionFreezeCamera(player, preset);
                }
            }
            else
            {
                s_freezeCameraPresetIndex = -1;
            }
        }
        
        /// <summary>
        /// Position the camera for freeze frame using a projectile preset
        /// </summary>
        private static void PositionFreezeCamera(EntityPlayerLocal player, StandardCameraPreset preset)
        {
            if (s_freezeCameraTarget == null) return;
            
            var targetPos = s_freezeCameraTarget.position;
            
            // Calculate camera position based on preset (similar to projectile camera logic)
            // Distance behind/from target, height above target, side offset
            Vector3 directionToPlayer = (player.GetPosition() - targetPos).normalized;
            directionToPlayer.y = 0; // Keep horizontal
            if (directionToPlayer.sqrMagnitude < 0.01f)
            {
                directionToPlayer = Vector3.forward;
            }
            
            // Random side offset for variety
            float sideOffset = UnityEngine.Random.Range(-1.5f, 1.5f);
            Vector3 sideDirection = Vector3.Cross(Vector3.up, directionToPlayer);
            
            Vector3 cameraPos = targetPos 
                + directionToPlayer * preset.Distance 
                + Vector3.up * preset.Height
                + sideDirection * sideOffset;
            
            // Switch to third-person for better view (if in first-person)
            if (player.bFirstPersonView)
            {
                player.SetFirstPersonView(false, false);
            }
            
            // Position and aim camera at target
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = cameraPos;
                mainCam.transform.LookAt(targetPos + Vector3.up * 0.8f); // Look at chest height
                
                // Apply pitch and tilt from preset
                var euler = mainCam.transform.eulerAngles;
                euler.x += preset.Pitch;
                euler.z = preset.Tilt;
                mainCam.transform.eulerAngles = euler;
            }
        }
        
        
        /// <summary>
        /// End the freeze frame and handle post-freeze action
        /// </summary>
        private static void EndFreezeFrame()
        {
            if (!s_isFreezeFrameActive) return;
            
            var settings = s_freezeIsFirstPerson ? CKSettings.FPFreezeFrame : CKSettings.ProjectileFreezeFrame;
            s_isFreezeFrameActive = false;
            s_freezeCameraPresetIndex = -1;
            
            CKLog.Verbose($" Freeze frame ENDED, post-freeze action: {settings.PostAction}");
            
            switch (settings.PostAction)
            {
                case PostFreezeAction.End:
                    // End immediately - restore normal time and cancel cinematic
                    SmoothRestoreTimeScale(0.1f);
                    CancelSequence();
                    break;
                    
                case PostFreezeAction.ContinueCinematic:
                    // Resume slow-mo cinematic with same camera
                    SmoothRestoreTimeScale(0.3f);
                    // Cinematic continues naturally via s_timer
                    break;
                    
                case PostFreezeAction.SwitchCamera:
                    // Switch to a new camera angle and continue
                    SmoothRestoreTimeScale(0.3f);
                    if (!s_freezeIsFirstPerson && settings.RandomizePostCamera && s_freezeCameraTarget != null)
                    {
                        var player = GameManager.Instance?.World?.GetPrimaryPlayer();
                        var presets = StandardCameraPreset.All;
                        if (player != null && presets != null && presets.Length > 1)
                        {
                            // Pick a different preset than the freeze camera used
                            int newIndex;
                            do
                            {
                                newIndex = UnityEngine.Random.Range(0, presets.Length);
                            } while (newIndex == s_freezeCameraPresetIndex && presets.Length > 1);
                            
                            PositionFreezeCamera(player, presets[newIndex]);
                            CKLog.Verbose($" Post-freeze camera switched to preset: {presets[newIndex].Name}");
                        }
                    }
                    // Cinematic continues naturally via s_timer
                    break;
                    
                case PostFreezeAction.Skip:
                    // End freeze, skip remaining cinematic effects
                    SmoothRestoreTimeScale(0.1f);
                    CancelSequence();
                    break;
            }
            
            // Clean up freeze camera state
            s_freezeCameraTarget = null;
        }
        
        
        /// <summary>
        /// Simple modifiers container for freeze frame pending cinematics
        /// </summary>
        private class CinematicSequenceModifiers
        {
            public float DurationMultiplier = 1f;
            public float SlowScaleMultiplier = 1f;
            public float ZoomMultiplier = 1f;
            public float ZoomSpeedMultiplier = 1f;
            public Color ScreenTint = new Color(0f, 0f, 0.2f, 0.2f);
            public float TargetDistance = float.MaxValue;
            public CinematicContextManager.KillContext TriggeredContexts;
            
            public CinematicContextManager.ContextModifiers ToContextMods()
            {
                return new CinematicContextManager.ContextModifiers
                {
                    DurationMultiplier = this.DurationMultiplier,
                    SlowScaleMultiplier = this.SlowScaleMultiplier,
                    ZoomMultiplier = this.ZoomMultiplier,
                    ZoomSpeedMultiplier = this.ZoomSpeedMultiplier,
                    ScreenTint = this.ScreenTint,
                    TargetDistance = this.TargetDistance,
                    TriggeredContexts = this.TriggeredContexts
                };
            }
            
            public static CinematicSequenceModifiers FromContextMods(CinematicContextManager.ContextModifiers mods)
            {
                return new CinematicSequenceModifiers
                {
                    DurationMultiplier = mods.DurationMultiplier,
                    SlowScaleMultiplier = mods.SlowScaleMultiplier,
                    ZoomMultiplier = mods.ZoomMultiplier,
                    ZoomSpeedMultiplier = mods.ZoomSpeedMultiplier,
                    ScreenTint = mods.ScreenTint,
                    TargetDistance = mods.TargetDistance,
                    TriggeredContexts = mods.TriggeredContexts
                };
            }
        }

        private static void StartSequence(EntityPlayerLocal player, CinematicContextManager.ContextModifiers mods, string reason, bool isLastEnemyContext = false)
        {
            if (player == null) return;

            // Apply default modifiers if none provided
            if (mods.Equals(default(CinematicContextManager.ContextModifiers)))
            {
                mods.DurationMultiplier = 1f;
                mods.SlowScaleMultiplier = 1f;
                mods.ZoomMultiplier = 1f;
                mods.ZoomSpeedMultiplier = 1f;
                mods.ScreenTint = new Color(0f, 0f, 0.2f, 0.2f);
                mods.TargetDistance = float.MaxValue; // Prevent blood splatter from triggering incorrectly
            }

            bool cameraIsProjectile = s_isProjectileCameraActive || reason == "Projectile" || reason == "Hitscan";
            bool visualsUseProjectile = cameraIsProjectile;
            EffectOverride? overridePack = s_effectOverride;
            s_effectOverride = null;
            
            // ═══════════════════════════════════════════════════════════════════════
            // EXPERIMENTAL: Predator Vision - Subtle visual effect on sneak kills
            // ═══════════════════════════════════════════════════════════════════════
            var expSettings = CKSettings.Experimental;
            if (expSettings.EnablePredatorVision && reason == "Sneak")
            {
                try
                {
                    var screenEffects = player?.ScreenEffectManager;
                    if (screenEffects != null)
                    {
                        // Use DrunkVFX for a subtle predator-like distortion instead of full NightVision
                        // Scale intensity down for a subtle effect (0.1-0.3 looks good)
                        float subtleIntensity = Mathf.Clamp(expSettings.PredatorVisionIntensity * 0.3f, 0.05f, 0.4f);
                        screenEffects.SetScreenEffect("DrunkVFX", subtleIntensity, expSettings.PredatorVisionDuration);
                        CKLog.Verbose($" Predator Vision triggered (subtle distortion: {subtleIntensity:F2}, duration: {expSettings.PredatorVisionDuration}s)");
                    }
                }
                catch (System.Exception ex)
                {
                    CKLog.Verbose($" Predator Vision failed: {ex.Message}");
                }
            }
            
            // Apply randomization to duration and timescale if enabled
            float baseDuration = s_duration;
            float baseSlowScale = s_slowScale;
            
            var bk = s_cinematicSettings.BasicKill;
            if (bk.RandomizeDuration)
            {
                baseDuration = UnityEngine.Random.Range(bk.DurationMin, bk.DurationMax);
            }
            if (bk.RandomizeTimeScale)
            {
                baseSlowScale = UnityEngine.Random.Range(bk.TimeScaleMin, bk.TimeScaleMax);
            }
            
            float duration = Mathf.Max(0.1f, baseDuration * mods.DurationMultiplier);
            float slowScale = Mathf.Clamp(baseSlowScale * mods.SlowScaleMultiplier, MinScale, MaxScale);
            // Prepare return timing
            s_isRestoringTime = false;
            float baseReturnPercent = cameraIsProjectile ? CKSettings.ProjectileReturnPercent : CKSettings.FirstPersonReturnPercent;
            float baseReturnStart = cameraIsProjectile ? CKSettings.ProjectileReturnStart : CKSettings.FirstPersonReturnStart;
            s_returnDuration = Mathf.Clamp(duration * baseReturnPercent, 0.01f, duration);
            s_returnStartTime = Mathf.Clamp(baseReturnStart, 0f, duration);

            // Mode effect packs
            bool enableHitstop = cameraIsProjectile ? CKSettings.EnableProjectileHitstop : CKSettings.EnableFirstPersonHitstop;
            bool hitstopCritOnly = cameraIsProjectile ? CKSettings.ProjectileHitstopOnCritOnly : CKSettings.FirstPersonHitstopOnCritOnly;
            float hitstopDuration = cameraIsProjectile
                ? RandomizeValue(CKSettings.ProjectileHitstopDuration, s_cinematicSettings.ProjectileHitstopDurationMin, s_cinematicSettings.ProjectileHitstopDurationMax, s_cinematicSettings.RandomizeProjectileHitstopDuration)
                : RandomizeValue(CKSettings.FirstPersonHitstopDuration, s_cinematicSettings.FirstPersonHitstopDurationMin, s_cinematicSettings.FirstPersonHitstopDurationMax, s_cinematicSettings.RandomizeFirstPersonHitstopDuration);

            if (overridePack.HasValue)
            {
                var ov = overridePack.Value;
                if (ov.HasHitstopOverride)
                {
                    enableHitstop = ov.HitstopEnabled;
                    hitstopDuration = ov.HitstopDuration;
                    hitstopCritOnly = ov.HitstopCritOnly;
                }
                if (!string.IsNullOrEmpty(ov.VisualSource))
                {
                    if (ov.VisualSource == "FirstPerson") visualsUseProjectile = false;
                    else if (ov.VisualSource == "Projectile") visualsUseProjectile = true;
                }
            }

            float tmpFovAmt = CKSettings.FOVZoomAmount;
            float tmpFovIn = CKSettings.FOVZoomInDuration;
            float tmpFovHold = CKSettings.FOVZoomHoldDuration;
            float tmpFovOut = CKSettings.FOVZoomOutDuration;
            float tmpVignette = CKSettings.VignetteIntensity;
            float tmpColor = s_cinematicSettings.ColorGradingIntensity;
            float tmpFlash = CKSettings.FlashIntensity;
            float speedMultiplier = Mathf.Max(0.1f, mods.ZoomSpeedMultiplier);
            if (!cameraIsProjectile)
            {
                s_cinematicSettings.FOVZoomAmount = RandomizeValue(CKSettings.FOVZoomAmount, s_cinematicSettings.FOVZoomAmountMin, s_cinematicSettings.FOVZoomAmountMax, s_cinematicSettings.RandomizeFOVAmount);
                s_cinematicSettings.FOVZoomInDuration = RandomizeValue(CKSettings.FOVZoomInDuration, s_cinematicSettings.FOVZoomInDurationMin, s_cinematicSettings.FOVZoomInDurationMax, s_cinematicSettings.RandomizeFOVIn) / speedMultiplier;
                s_cinematicSettings.FOVZoomHoldDuration = RandomizeValue(CKSettings.FOVZoomHoldDuration, s_cinematicSettings.FOVZoomHoldDurationMin, s_cinematicSettings.FOVZoomHoldDurationMax, s_cinematicSettings.RandomizeFOVHold);
                s_cinematicSettings.FOVZoomOutDuration = RandomizeValue(CKSettings.FOVZoomOutDuration, s_cinematicSettings.FOVZoomOutDurationMin, s_cinematicSettings.FOVZoomOutDurationMax, s_cinematicSettings.RandomizeFOVOut) / speedMultiplier;
                s_cinematicSettings.VignetteIntensity = RandomizeValue(CKSettings.VignetteIntensity, CKSettings.VignetteIntensityMin, CKSettings.VignetteIntensityMax, CKSettings.RandomizeVignetteIntensity);
                s_cinematicSettings.ColorGradingIntensity = RandomizeValue(s_cinematicSettings.ColorGradingIntensity, s_cinematicSettings.ColorGradingIntensityMin, s_cinematicSettings.ColorGradingIntensityMax, s_cinematicSettings.RandomizeColorGradingIntensity);
                s_cinematicSettings.FlashIntensity = RandomizeValue(CKSettings.FlashIntensity, CKSettings.FlashIntensityMin, CKSettings.FlashIntensityMax, CKSettings.RandomizeFlashIntensity);
            }
            else
            {
                // Projectile path uses runtime struct; keep settings as-is
            }

            if (s_isActive || s_isHitstopActive)
            {
                // Extend duration if already active? For now just reset timer
                s_timer = duration;
                return;
            }

            // Reset queued cooldown holder (global cooldown unused)
            s_lastRuntimeCooldown = 0f;

            // Set Sub-Cooldowns based on reason
            switch (reason)
            {
                case "Crit": s_cooldownCrit = CKSettings.CooldownCrit; break;
                case "Dismember": s_cooldownDismember = CKSettings.CooldownDismember; break;
                case "LongRange": s_cooldownLongRange = CKSettings.CooldownLongRange; break;
                case "LowHealth": s_cooldownLowHealth = CKSettings.CooldownLowHealth; break;
                case "LastEnemy": s_cooldownLastEnemy = s_cinematicSettings.CooldownLastEnemy; break;
            }

            // ═══════════════════════════════════════════════════════════════
            // ENHANCED KILLSTREAK SYSTEM - 5 Tiers, Per-Mode, Bonus Intensity
            // ═══════════════════════════════════════════════════════════════
            float streakBonus = 0f;
            float streakIntensity = 1f;
            var ksSettings = s_cinematicSettings.MenuV2?.Killstreak;
            
            if (ksSettings != null && ksSettings.Enabled && CKSettings.EnableTriggers)
            {
                s_currentStreak++;
                s_lastKillTime = Time.time;

                // Check 3 tiers (highest to lowest) with null safety
                if (ksSettings.Tier3 != null && s_currentStreak >= ksSettings.Tier3.KillsRequired)
                {
                    streakBonus = ksSettings.Tier3.BonusDuration;
                    streakIntensity = ksSettings.Tier3.BonusIntensity;
                }
                else if (ksSettings.Tier2 != null && s_currentStreak >= ksSettings.Tier2.KillsRequired)
                {
                    streakBonus = ksSettings.Tier2.BonusDuration;
                    streakIntensity = ksSettings.Tier2.BonusIntensity;
                }
                else if (ksSettings.Tier1 != null && s_currentStreak >= ksSettings.Tier1.KillsRequired)
                {
                    streakBonus = ksSettings.Tier1.BonusDuration;
                    streakIntensity = ksSettings.Tier1.BonusIntensity;
                }

                if (streakBonus > 0f)
                {
                    CKLog.Verbose($" Streak x{s_currentStreak} (+{streakBonus:F2}s, {streakIntensity:F1}x intensity)");
                }
            }
            // Apply streak intensity to slow scale (more dramatic slow-mo on higher streaks)
            if (streakIntensity > 1f)
            {
                slowScale = Mathf.Max(0.05f, slowScale / streakIntensity);
                CKLog.Verbose($" Streak intensity applied, new slow scale: {slowScale:F3}");
            }

            s_isActive = true;
            s_timer = duration + streakBonus;
            
            // If ragdoll detection is active, extend timer to fallback duration
            // This ensures ragdoll has time to settle before timeout
            if (s_ragdollGroundDetectionActive && s_cinematicSettings?.RagdollFallbackDuration > 0)
            {
                float fallbackDuration = s_cinematicSettings.RagdollFallbackDuration;
                if (s_timer < fallbackDuration)
                {
                    CKLog.Verbose($" Extending timer from {s_timer:F2}s to fallback {fallbackDuration:F2}s for ragdoll");
                    s_timer = fallbackDuration;
                }
            }
            
            // ═══════════════════════════════════════════════════════════════
            // HUD HIDING - Hide game HUD during cinematic
            // ═══════════════════════════════════════════════════════════════
            if (s_cinematicSettings.HUDElements?.HideAllHUDDuringCinematic == true)
            {
                SetGameHUDVisible(player, false);
            }
            
            // ═══════════════════════════════════════════════════════════════
            // HUD NOTIFICATIONS - Display cinematic info on screen
            // ═══════════════════════════════════════════════════════════════
            var hud = CinematicKillHUD.Instance;
            if (hud != null)
            {
                // Format trigger name nicely
                string triggerDisplay = FormatTriggerName(reason);
                
                // Build active effects list
                var globalVisuals = s_cinematicSettings.MenuV2?.GlobalVisuals;
                var activeEffects = new System.Collections.Generic.List<string>();
                if (globalVisuals != null && globalVisuals.EnableScreenEffects)
                {
                    if (globalVisuals.EnableFOVEffect) activeEffects.Add("FOV");
                    if (globalVisuals.EnableVignette) activeEffects.Add("Vignette");
                    if (globalVisuals.EnableDesaturation) activeEffects.Add("Desat");
                    if (globalVisuals.EnableBloodSplatter) activeEffects.Add("Blood");
                }
                if (s_cinematicSettings.MenuV2?.Hitstop?.Enabled == true) activeEffects.Add("Hitstop");
                
                // Show trigger
                hud.ShowTrigger(triggerDisplay);
                
                // Show camera type
                string cameraType = cameraIsProjectile ? "Projectile" : "First Person";
                hud.ShowCamera(cameraType, -1f);
                
                // Show timing
                hud.ShowTiming(duration + streakBonus, slowScale);
                
                // Show effects
                if (activeEffects.Count > 0)
                {
                    hud.ShowEffects(activeEffects);
                }
                
                // Show killstreak bonus if applicable
                if (streakBonus > 0f)
                {
                    int tier = 0;
                    if (ksSettings?.Tier3 != null && s_currentStreak >= ksSettings.Tier3.KillsRequired) tier = 3;
                    else if (ksSettings?.Tier2 != null && s_currentStreak >= ksSettings.Tier2.KillsRequired) tier = 2;
                    else if (ksSettings?.Tier1 != null && s_currentStreak >= ksSettings.Tier1.KillsRequired) tier = 1;
                    hud.ShowKillstreak(s_currentStreak, tier, streakBonus);
                }
            }
            
            // Hitstop Logic
            bool triggerHitstop = enableHitstop;
            if (hitstopCritOnly && !mods.TriggeredContexts.HasFlag(CinematicContextManager.KillContext.Crit))
            {
                triggerHitstop = false;
            }

            StopTimeRestoreCoroutine();

            if (triggerHitstop)
            {
                s_isHitstopActive = true;
                s_hitstopTimer = hitstopDuration;
                s_resumeScale = slowScale;
                ApplyTimeScale(0f); // Freeze!
                CKLog.Verbose($" Hitstop triggered ({hitstopDuration:F2}s)");
            }
            else
            {
                ApplyTimeScale(slowScale);
            }
            
            bool useProjFOV = visualsUseProjectile;
            bool useProjFX = visualsUseProjectile;
            bool isProjectilePath = cameraIsProjectile;
            if (!string.IsNullOrEmpty(s_cinematicSettings.ProjectileFOVSource) && isProjectilePath)
            {
                if (s_cinematicSettings.ProjectileFOVSource == "FirstPerson") useProjFOV = false;
                else if (s_cinematicSettings.ProjectileFOVSource == "Projectile") useProjFOV = true;
            }
            if (!string.IsNullOrEmpty(s_cinematicSettings.ProjectileFXSource) && isProjectilePath)
            {
                if (s_cinematicSettings.ProjectileFXSource == "FirstPerson") useProjFX = false;
                else if (s_cinematicSettings.ProjectileFXSource == "Projectile") useProjFX = true;
            }
            // Force first-person cameras to use first-person visual settings so FX always fire
            if (!cameraIsProjectile)
            {
                useProjFX = false;
            }

            // Start FOV effect if enabled
            // zoomPercent: how much to zoom in/out as a percentage
            // isZoomOut: if true, increase FOV (zoom out); if false, reduce FOV (zoom in)
            FOVMode fovMode;
            float zoomPercent;
            // Use the actual camera mode chosen (not visual overrides) to determine FOV settings source
            // This ensures BasicKill with FP-only uses FP FOV settings, not projectile settings
            bool useProjectileFOVSettings = cameraIsProjectile;
            
            if (reason == "BasicKill")
            {
                if (useProjectileFOVSettings)
                {
                    fovMode = s_cinematicSettings.BasicKill.ProjectileFOVMode;
                    zoomPercent = s_cinematicSettings.BasicKill.ProjectileFOVPercent;
                }
                else
                {
                    fovMode = s_cinematicSettings.BasicKill.FOVMode;
                    zoomPercent = s_cinematicSettings.BasicKill.FOVPercent;
                }
                // Apply context modifiers (e.g., crits zoom more)
                zoomPercent *= mods.ZoomMultiplier;
            }
            else
            {
                // For special triggers, check for per-trigger FOV overrides, otherwise use defaults
                CKTriggerSettings triggerSettings = GetTriggerSettingsByReason(reason);
                if (triggerSettings != null && triggerSettings.Override)
                {
                    if (useProjectileFOVSettings)
                    {
                        fovMode = triggerSettings.ProjectileFOVMode;
                        zoomPercent = triggerSettings.ProjectileFOVPercent;
                    }
                    else
                    {
                        fovMode = triggerSettings.FOVMode;
                        zoomPercent = triggerSettings.FOVPercent;
                    }
                }
                else
                {
                    if (useProjectileFOVSettings)
                    {
                        fovMode = s_cinematicSettings.TriggerDefaults.ProjectileFOVMode;
                        zoomPercent = s_cinematicSettings.TriggerDefaults.ProjectileFOVPercent;
                    }
                    else
                    {
                        fovMode = s_cinematicSettings.TriggerDefaults.FOVMode;
                        zoomPercent = s_cinematicSettings.TriggerDefaults.FOVPercent;
                    }
                }
                zoomPercent *= mods.ZoomMultiplier;
            }
            
            if (fovMode != FOVMode.Off)
            {
                string modeStr = fovMode == FOVMode.ZoomIn ? "ZoomIn" : "ZoomOut";
                
                // Get FOV timing based on camera type
                float fovIn, fovHold, fovOut;
                if (useProjectileFOVSettings)
                {
                    // Use projectile camera FOV timing
                    fovIn = s_cinematicSettings.ProjectileCamera.FOVZoomInDuration;
                    fovHold = s_cinematicSettings.ProjectileCamera.FOVHoldDuration;
                    fovOut = s_cinematicSettings.ProjectileCamera.FOVZoomOutDuration;
                }
                else
                {
                    // Use FP camera FOV timing from BasicKill or TriggerDefaults
                    if (reason == "BasicKill")
                    {
                        fovIn = s_cinematicSettings.BasicKill.FOVZoomInDuration;
                        fovHold = s_cinematicSettings.BasicKill.FOVHoldDuration;
                        fovOut = s_cinematicSettings.BasicKill.FOVZoomOutDuration;
                    }
                    else
                    {
                        fovIn = s_cinematicSettings.TriggerDefaults.FOVZoomInDuration;
                        fovHold = s_cinematicSettings.TriggerDefaults.FOVHoldDuration;
                        fovOut = s_cinematicSettings.TriggerDefaults.FOVZoomOutDuration;
                    }
                }

                ResolveFovPhaseDurations(fovMode, fovIn, fovHold, fovOut, out float enterDuration, out float holdDuration, out float exitDuration);

                // Handle duration when ragdoll detection is active
                // The cinematic will last until ragdoll settles (up to fallback duration)
                float effectiveDuration = duration;
                if (s_ragdollGroundDetectionActive && s_cinematicSettings?.RagdollFallbackDuration > duration)
                {
                    effectiveDuration = s_cinematicSettings.RagdollFallbackDuration;
                    
                    // Scale up hold phase to fill the extended ragdoll duration
                    // Keep enter/exit as configured for crisp transitions, extend hold to fill remaining time
                    float enterExit = enterDuration + exitDuration;
                    float remainingTime = effectiveDuration - enterExit;
                    if (remainingTime > holdDuration)
                    {
                        CKLog.Verbose($" Extended FOV hold from {holdDuration:F2}s to {remainingTime:F2}s for ragdoll duration");
                        holdDuration = remainingTime;
                    }
                }
                
                // Scale DOWN if total exceeds effective duration (safety clamp)
                float totalFOV = enterDuration + holdDuration + exitDuration;
                if (totalFOV > effectiveDuration)
                {
                    float scale = effectiveDuration / totalFOV;
                    enterDuration *= scale;
                    holdDuration *= scale;
                    exitDuration *= scale;
                    CKLog.Verbose($" FOV timing scaled down: {totalFOV:F2}s -> {effectiveDuration:F2}s");
                }

                string enterLabel = fovMode == FOVMode.ZoomOut ? "Out" : "In";
                string exitLabel = fovMode == FOVMode.ZoomOut ? "In" : "Out";
                CKLog.Verbose($" FOV effect enabled for {reason}, mode={modeStr}, percent={zoomPercent:F1}%, timing={enterDuration:F2}+{holdDuration:F2}+{exitDuration:F2}s (enter={enterLabel}, return={exitLabel}, cam={(useProjectileFOVSettings ? "Proj" : "FP")})");
                StartFOVEffect(player, zoomPercent, enterDuration, holdDuration, exitDuration, fovMode);
            }
            else
            {
                CKLog.Verbose($" FOV effect disabled for {reason}");
            }

            // Start Screen Effects
            var effects = CinematicScreenEffects.Instance;
            if (effects != null)
            {
                // Read vignette from ScreenEffects (where Effects tab writes)
                bool enableVignette = s_cinematicSettings.ScreenEffects.EnableVignette;
                float vignetteInt = s_cinematicSettings.ScreenEffects.VignetteIntensity;
                
                bool enableColor = !useProjFX && s_cinematicSettings.EnableColorGrading;
                int colorMode = s_cinematicSettings.ColorGradingMode;
                float colorInt = s_cinematicSettings.ColorGradingIntensity;

                // Calculate tint color
                Color tintColor = mods.ScreenTint;
                
                // Get globalVisuals for post-processing settings
                var globalVisuals = s_cinematicSettings.MenuV2.GlobalVisuals;
                
                // Get desaturation settings from ScreenEffects (where menu writes them)
                bool enableDesaturation = s_cinematicSettings.ScreenEffects.EnableDesaturation;
                float desatAmount = s_cinematicSettings.ScreenEffects.DesaturationAmount;

                // Use extended enable method with GUI-based effects only
                effects.EnableEffectsExtended(
                    enableVignette, vignetteInt,
                    enableColor, tintColor,
                    enableDesaturation, desatAmount
                );
                
                // Trigger 7DTD native effects - blood splatter only on close kills (within 5m)
                const float BLOOD_SPLATTER_MAX_DISTANCE = 5f;
                bool enableBloodSplatter = globalVisuals.EnableBloodSplatter && mods.TargetDistance <= BLOOD_SPLATTER_MAX_DISTANCE;
                if (enableBloodSplatter)
                {
                    int splatterDir = globalVisuals.BloodSplatterDirection;
                    float splatterIntensity = globalVisuals.BloodSplatterIntensity;
                    effects.TriggerBloodSplatter(splatterDir, splatterIntensity);
                }
                
                // Camera shake removed - causes issues with gameplay
                // Concussion removed - causes persistent black/white screen issue
                
                // Post-Processing Effects
                bool enableMotionBlur = globalVisuals.EnableMotionBlur;
                if (enableMotionBlur)
                {
                    float mbIntensity = globalVisuals.MotionBlurIntensity;
                    effects.EnableMotionBlur(mbIntensity);
                }
                
                bool enableChromatic = globalVisuals.EnableChromaticAberration;
                if (enableChromatic)
                {
                    float caIntensity = globalVisuals.ChromaticAberrationIntensity;
                    effects.EnableChromaticAberration(caIntensity);
                }
                
                bool enableDOF = globalVisuals.EnableDepthOfField;
                if (enableDOF)
                {
                    float dofFocus = globalVisuals.DepthOfFieldFocusDistance;
                    float dofAperture = globalVisuals.DepthOfFieldAperture;
                    float dofFocal = globalVisuals.DepthOfFieldFocalLength;
                    effects.EnableDepthOfField(dofFocus, dofAperture, dofFocal);
                }
                
                bool enableRadialBlur = globalVisuals.EnableRadialBlur;
                if (enableRadialBlur)
                {
                    float rbIntensity = globalVisuals.RadialBlurIntensity;
                    float rbDuration = globalVisuals.RadialBlurDuration;
                    effects.TriggerRadialBlur(rbIntensity, rbDuration);
                }
                
                // Mark post-processing as active if any were enabled
                if (enableMotionBlur || enableChromatic || enableDOF)
                {
                    effects.MarkPostProcessingActive();
                }

                bool enableFlash = !cameraIsProjectile && CKSettings.EnableFlash;
                float flashInt = CKSettings.FlashIntensity;

                if (enableFlash && mods.TriggerFlash)
                {
                    effects.TriggerFlash(new Color(1f, 1f, 1f, flashInt), 0.15f);
                }
            }
            // Restore user-visible settings so randomization does not persist
            s_cinematicSettings.FOVZoomAmount = tmpFovAmt;
            s_cinematicSettings.FOVZoomInDuration = tmpFovIn;
            s_cinematicSettings.FOVZoomHoldDuration = tmpFovHold;
            s_cinematicSettings.FOVZoomOutDuration = tmpFovOut;
            s_cinematicSettings.VignetteIntensity = tmpVignette;
            s_cinematicSettings.ColorGradingIntensity = tmpColor;
            s_cinematicSettings.FlashIntensity = tmpFlash;
            
            CKLog.Verbose($" Enabled (Reason: {reason}, Scale: {slowScale:F2}, Duration: {duration:F2}s)");
        }

        private static void CancelSequence()
        {
            if (!s_isActive && !s_isHitstopActive)
            {
                return;
            }

            // ═══════════════════════════════════════════════════════════════════════
            // EXPERIMENTAL: Chain Reaction - Switch to next victim instead of ending
            // ═══════════════════════════════════════════════════════════════════════
            var expSettings = CKSettings?.Experimental;
            if (expSettings != null && s_isChainReactionActive && s_chainReactionList != null && s_chainReactionList.Count > 0)
            {
                // Get next victim from queue
                var nextVictim = s_chainReactionList[0];
                s_chainReactionList.RemoveAt(0);
                
                // Clean up invalid entries
                while (s_chainReactionList.Count > 0 && (nextVictim == null || nextVictim.transform == null))
                {
                    nextVictim = s_chainReactionList[0];
                    s_chainReactionList.RemoveAt(0);
                }
                
                if (nextVictim != null && nextVictim.transform != null)
                {
                    // Switch camera target to next victim's transform
                    s_currentRagdollTarget = nextVictim.transform;
                    s_currentRagdollEntityId = nextVictim.entityId;
                    
                    // Reset timer for next victim (shorter duration per chain)
                    float chainDuration = expSettings.ChainCameraTransitionTime * 2f; // 1s per victim by default
                    s_timer = chainDuration;
                    
                    // Apply slow-mo ramp if enabled
                    if (expSettings.ChainReactionSlowMoRamp)
                    {
                        float newScale = s_slowScale * expSettings.ChainSlowMoMultiplier;
                        s_slowScale = Mathf.Max(MinScale, newScale);
                        ApplyTimeScale(s_slowScale);
                    }
                    
                    CKLog.Verbose($" Chain Reaction: Switching to victim #{s_chainReactionKillCount - s_chainReactionList.Count} ({nextVictim.EntityName}), remaining: {s_chainReactionList.Count}");
                    
                    // Don't cancel - continue with new target
                    return;
                }
                else
                {
                    // No valid victims left, end chain reaction
                    s_isChainReactionActive = false;
                    s_chainReactionList.Clear();
                    s_chainReactionKillCount = 0;
                    CKLog.Verbose(" Chain Reaction: No more valid victims, ending chain");
                }
            }

            // Reset chain reaction state on full cancel
            s_isChainReactionActive = false;
            if (s_chainReactionList != null)
            {
                s_chainReactionList.Clear();
            }
            s_chainReactionKillCount = 0;

            s_isActive = false;
            s_isHitstopActive = false;
            s_timer = 0f;
            
            // Reset ragdoll tracking to prevent state leaking to next cinematic
            StopRagdollTracking();
            
            // Get player reference for cleanup
            var player = GameManager.Instance?.World?.GetPrimaryPlayer();
            
            // Restore Camera if active
            RestoreCameraState();

            // Stop FOV effect if active
            StopFOVEffect();

            // Stop Screen Effects
            var effects = CinematicScreenEffects.Instance;
            if (effects != null)
            {
                effects.DisableEffects();
            }
            
            // Restore Game HUD if it was hidden
            if (s_cinematicSettings.HUDElements?.HideAllHUDDuringCinematic == true)
            {
                if (player != null)
                {
                    SetGameHUDVisible(player, true);
                }
            }
            
            // NOTE: The double toggle fix for invisible hands is handled in RestoreCameraState()
            // via RestoreViewForHandsFix() → ApplyProjectileCameraViewRestore()
            // This only runs for PROJECTILE camera cinematics where hands can become invisible
            
            float restore = Mathf.Max(0.01f, s_returnDuration);
            
            // Only start smooth restore if not already restoring - avoids restarting the ease-out
            if (!s_isRestoringTime)
            {
                SmoothRestoreTimeScale(restore); // Reset to normal speed with ease-out
            }
            else
            {
                // Ensure we reach 1x if the restore coroutine hasn't finished
                ApplyTimeScale(1f);
                StopTimeRestoreCoroutine();
            }
            RestoreAudio();
            CKLog.Verbose(" Disabled");
        }

        private static void RestoreCameraState()
        {
            if (!s_isProjectileCameraActive) return;

            s_isProjectileCameraActive = false;
            
            // Reset target tracking to prevent stale data affecting next cinematic
            s_lastTargetPos = Vector3.zero;
            s_projectileCameraTarget = null;
            s_cameraFallbackTarget = null;
            
            // Reset experimental camera modes
            s_isRideCamActive = false;
            s_rideCamProjectileTransform = null;
            s_isDismemberFocusCamActive = false;
            s_dismemberedLimbTarget = null;
            
            CKLog.Verbose($" Restoring Camera State. Original Pos: {s_originalCameraPos}");

            var player = GameManager.Instance.World.GetPrimaryPlayer();
            if (player != null)
            {
                // Force restore camera immediately (redundancy)
                if (Camera.main != null && s_originalCameraPos != Vector3.zero)
                {
                    Camera.main.transform.position = s_originalCameraPos;
                    Camera.main.transform.rotation = s_originalCameraRot;
                }
                else
                {
                    Log.Warning($"CinematicKill: Cannot restore camera. Main: {Camera.main != null}, Pos: {s_originalCameraPos}");
                }

                // Use coroutine to restore view cleanly
                if (s_menuComponent != null)
                {
                    s_menuComponent.StartCoroutine(RestoreViewForHandsFix(player));
                }
                else
                {
                    // Fallback if menu component missing - restore with hands fix
                    ApplyProjectileCameraViewRestore(player, s_wasFirstPersonView);
                }
            }
        }

        private static void ResolveFovPhaseDurations(FOVMode mode, float inDuration, float holdDuration, float outDuration, out float enterDuration, out float hold, out float exitDuration)
        {
            if (mode == FOVMode.ZoomOut)
            {
                enterDuration = outDuration;
                exitDuration = inDuration;
            }
            else
            {
                enterDuration = inDuration;
                exitDuration = outDuration;
            }

            hold = holdDuration;
        }

        private static void StartFOVEffect(EntityPlayerLocal player, float zoomPercent, float enterDuration, float holdDuration, float exitDuration, FOVMode fovMode, bool isAbsoluteZoom = false)
        {
            if (player == null || s_fovController == null)
            {
                Log.Warning("CinematicKill: Cannot start FOV effect - invalid state");
                return;
            }

            // zoomPercent is a percentage (5-50) representing how much to change FOV
            // ZoomIn: reduce FOV by percentage (15% = multiply by 0.85)
            // ZoomOut: increase FOV by percentage (15% = multiply by 1.15)
            float zoomMultiplier;
            if (isAbsoluteZoom)
            {
                zoomMultiplier = zoomPercent; // Absolute FOV for projectile camera
            }
            else if (fovMode == FOVMode.ZoomOut)
            {
                // Zoom out = increase FOV = multiply by > 1.0
                zoomMultiplier = 1f + (zoomPercent / 100f);
            }
            else
            {
                // Zoom in = reduce FOV = multiply by < 1.0
                zoomMultiplier = Mathf.Clamp01(1f - (zoomPercent / 100f));
            }
            
            float totalDuration = enterDuration + holdDuration + exitDuration;

            // Start the FOV zoom effect with specified phases
            s_fovController.StartFOVEffect(
                player,
                zoomMultiplier,
                enterDuration,
                holdDuration,
                exitDuration,
                totalDuration,
                isAbsoluteZoom
            );
        }

        private static void StopFOVEffect()
        {
            // Stop FOV effect if it's active
            if (s_fovController != null && s_fovController.IsActive)
            {
                s_fovController.StopFOVEffect();
            }
        }

        private static void ApplyTimeScale(float scale)
        {
            if (GameManager.IsDedicatedServer)
            {
                return;
            }

            Time.timeScale = scale;
            
            // Audio slow-motion effect - reduce volume during slow-mo for dramatic effect
            if (scale < 0.5f && scale > 0f && s_cinematicSettings?.EnableAudioSlowMo == true)
            {
                ApplyAudioSlowMotion(scale);
            }
            else if (scale >= 1f)
            {
                RestoreAudioPitch();
            }
            
            // Ragdoll stabilization: Use higher fixedDeltaTime during slow-mo
            // This causes physics to update less frequently in game-time, which
            // reduces the "floating" effect on ragdolls during slow motion.
            // The base value is multiplied by a stabilization factor when slowed.
            if (scale < 1f && scale > 0f)
            {
                // Use a higher physics timestep to stabilize ragdolls
                // The lower the scale, the higher we boost physics stability
                float stabilizationFactor = Mathf.Lerp(1f, 0.5f, 1f - scale);
                Time.fixedDeltaTime = s_baseFixedDelta * stabilizationFactor;
            }
            else
            {
                Time.fixedDeltaTime = s_baseFixedDelta;
            }
        }

        private static void SmoothRestoreTimeScale(float duration)
        {
            if (GameManager.IsDedicatedServer)
            {
                return;
            }

            if (duration <= 0f || s_menuComponent == null)
            {
                ApplyTimeScale(1f);
                return;
            }

            StopTimeRestoreCoroutine();
            s_timeRestoreCoroutine = s_menuComponent.StartCoroutine(RestoreTimeScaleRoutine(duration));
        }

        private static float RandomizeValue(float value, float rangeMin, float rangeMax, bool enabled)
        {
            // When randomization is disabled, return the exact value - no clamping
            if (!enabled) return value;
            
            // When randomization is enabled, pick a random value within the range
            float minVal = Mathf.Min(rangeMin, rangeMax);
            float maxVal = Mathf.Max(rangeMin, rangeMax);
            return UnityEngine.Random.Range(minVal, maxVal);
        }

        /// <summary>
        /// Format trigger name for HUD display
        /// </summary>
        private static string FormatTriggerName(string reason)
        {
            return reason switch
            {
                "BasicKill" => CKLocalization.L("ck_trigger_basic_kill", "Basic Kill"),
                "LastEnemy" => CKLocalization.L("ck_trigger_last_enemy", "Last Enemy"),
                "LongRange" => CKLocalization.L("ck_trigger_long_range", "Long Range"),
                "LowHealth" => CKLocalization.L("ck_trigger_low_health", "Low Health"),
                "Projectile" => CKLocalization.L("ck_trigger_projectile_kill", "Projectile Kill"),
                "Hitscan" => CKLocalization.L("ck_trigger_ranged_kill", "Ranged Kill"),
                "Crit" => CKLocalization.L("ck_trigger_critical", "Critical Hit"),
                "Dismember" => CKLocalization.L("ck_trigger_dismember", "Dismember"),
                "Killstreak" => CKLocalization.L("ck_trigger_killstreak", "Killstreak"),
                "Headshot" => CKLocalization.L("ck_trigger_headshot", "Headshot"),
                "Sneak" => CKLocalization.L("ck_trigger_sneak_kill", "Sneak Kill"),
                "None" => CKLocalization.L("ck_ui_none", "None"),
                _ => reason
            };
        }

        /// <summary>
        /// Get remaining cooldown time for a specific trigger
        /// </summary>
        private static float GetTriggerCooldownRemaining(string triggerName)
        {
            if (s_triggerCooldowns.TryGetValue(triggerName, out float readyTime))
            {
                float remaining = readyTime - Time.realtimeSinceStartup;
                return remaining > 0 ? remaining : 0f;
            }
            return 0f;
        }
        
        /// <summary>
        /// Start cooldown for a specific trigger
        /// Cooldown starts AFTER the cinematic ends (accounts for cinematic duration)
        /// </summary>
        private static void StartTriggerCooldown(string triggerName, float cooldownSeconds, float cinematicDuration = 0f)
        {
            if (cooldownSeconds <= 0) return;
            // Add cinematic duration so cooldown effectively starts AFTER the cinematic ends
            float totalDelay = cooldownSeconds + cinematicDuration;
            s_triggerCooldowns[triggerName] = Time.realtimeSinceStartup + totalDelay;
            CKLog.Verbose($" Started {triggerName} cooldown ({cooldownSeconds:F1}s + {cinematicDuration:F1}s cinematic = {totalDelay:F1}s total).");
        }

        private static void SetTriggerEffectOverride(string trigger)
        {
            EffectOverride ov = new EffectOverride
            {
                HasHitstopOverride = false,
                HasSoundOverride = false,
                VisualSource = string.Empty
            };

            switch (trigger)
            {
                case "Crit":
                    if (s_cinematicSettings.CritOverrideHitstop)
                    {
                        ov.HasHitstopOverride = true;
                        ov.HitstopEnabled = true;
                        ov.HitstopDuration = RandomizeValue(s_cinematicSettings.CritHitstopDuration, s_cinematicSettings.CritHitstopDurationMin, s_cinematicSettings.CritHitstopDurationMax, s_cinematicSettings.RandomizeCritHitstopDuration);
                        ov.HitstopCritOnly = s_cinematicSettings.CritHitstopOnCritOnly;
                    }
                    if (s_cinematicSettings.CritOverrideSlowMoSound)
                    {
                        ov.HasSoundOverride = true;
                        ov.SoundEnabled = true;
                        ov.SoundVolume = RandomizeValue(s_cinematicSettings.CritSlowMoSoundVolume, s_cinematicSettings.CritSlowMoSoundVolumeMin, s_cinematicSettings.CritSlowMoSoundVolumeMax, s_cinematicSettings.RandomizeCritSlowMoSoundVolume);
                    }
                    ov.VisualSource = s_cinematicSettings.CritVisualSource;
                    ov.FOVSource = s_cinematicSettings.CritFOVSource;
                    ov.FXSource = s_cinematicSettings.CritFXSource;
                    break;
                case "Dismember":
                    if (s_cinematicSettings.DismemberOverrideHitstop)
                    {
                        ov.HasHitstopOverride = true;
                        ov.HitstopEnabled = true;
                        ov.HitstopDuration = RandomizeValue(s_cinematicSettings.DismemberHitstopDuration, s_cinematicSettings.DismemberHitstopDurationMin, s_cinematicSettings.DismemberHitstopDurationMax, s_cinematicSettings.RandomizeDismemberHitstopDuration);
                        ov.HitstopCritOnly = s_cinematicSettings.DismemberHitstopOnCritOnly;
                    }
                    if (s_cinematicSettings.DismemberOverrideSlowMoSound)
                    {
                        ov.HasSoundOverride = true;
                        ov.SoundEnabled = true;
                        ov.SoundVolume = RandomizeValue(s_cinematicSettings.DismemberSlowMoSoundVolume, s_cinematicSettings.DismemberSlowMoSoundVolumeMin, s_cinematicSettings.DismemberSlowMoSoundVolumeMax, s_cinematicSettings.RandomizeDismemberSlowMoSoundVolume);
                    }
                    ov.VisualSource = s_cinematicSettings.DismemberVisualSource;
                    ov.FOVSource = s_cinematicSettings.DismemberFOVSource;
                    ov.FXSource = s_cinematicSettings.DismemberFXSource;
                    break;
                case "LongRange":
                    if (s_cinematicSettings.LongRangeOverrideHitstop)
                    {
                        ov.HasHitstopOverride = true;
                        ov.HitstopEnabled = true;
                        ov.HitstopDuration = RandomizeValue(s_cinematicSettings.LongRangeHitstopDuration, s_cinematicSettings.LongRangeHitstopDurationMin, s_cinematicSettings.LongRangeHitstopDurationMax, s_cinematicSettings.RandomizeLongRangeHitstopDuration);
                        ov.HitstopCritOnly = s_cinematicSettings.LongRangeHitstopOnCritOnly;
                    }
                    if (s_cinematicSettings.LongRangeOverrideSlowMoSound)
                    {
                        ov.HasSoundOverride = true;
                        ov.SoundEnabled = true;
                        ov.SoundVolume = RandomizeValue(s_cinematicSettings.LongRangeSlowMoSoundVolume, s_cinematicSettings.LongRangeSlowMoSoundVolumeMin, s_cinematicSettings.LongRangeSlowMoSoundVolumeMax, s_cinematicSettings.RandomizeLongRangeSlowMoSoundVolume);
                    }
                    ov.VisualSource = s_cinematicSettings.LongRangeVisualSource;
                    ov.FOVSource = s_cinematicSettings.LongRangeFOVSource;
                    ov.FXSource = s_cinematicSettings.LongRangeFXSource;
                    break;
                case "LowHealth":
                    if (s_cinematicSettings.LowHealthOverrideHitstop)
                    {
                        ov.HasHitstopOverride = true;
                        ov.HitstopEnabled = true;
                        ov.HitstopDuration = RandomizeValue(s_cinematicSettings.LowHealthHitstopDuration, s_cinematicSettings.LowHealthHitstopDurationMin, s_cinematicSettings.LowHealthHitstopDurationMax, s_cinematicSettings.RandomizeLowHealthHitstopDuration);
                        ov.HitstopCritOnly = s_cinematicSettings.LowHealthHitstopOnCritOnly;
                    }
                    if (s_cinematicSettings.LowHealthOverrideSlowMoSound)
                    {
                        ov.HasSoundOverride = true;
                        ov.SoundEnabled = true;
                        ov.SoundVolume = RandomizeValue(s_cinematicSettings.LowHealthSlowMoSoundVolume, s_cinematicSettings.LowHealthSlowMoSoundVolumeMin, s_cinematicSettings.LowHealthSlowMoSoundVolumeMax, s_cinematicSettings.RandomizeLowHealthSlowMoSoundVolume);
                    }
                    ov.VisualSource = s_cinematicSettings.LowHealthVisualSource;
                    ov.FOVSource = s_cinematicSettings.LowHealthFOVSource;
                    ov.FXSource = s_cinematicSettings.LowHealthFXSource;
                    break;
            }

            if (ov.HasHitstopOverride || ov.HasSoundOverride || !string.IsNullOrEmpty(ov.VisualSource))
            {
                s_effectOverride = ov;
            }
        }

        /// <summary>
        /// Gets the camera override for the current weapon mode
        /// Returns Auto if no override is set
        /// </summary>
        private static CameraOverride GetWeaponModeCameraOverride()
        {
            switch (s_currentWeaponMode)
            {
                case WeaponMode.Melee:
                    return s_cinematicSettings.MeleeCameraOverride;
                case WeaponMode.Ranged:
                    return s_cinematicSettings.RangedCameraOverride;
                case WeaponMode.Explosion:
                    return s_cinematicSettings.ExplosiveCameraOverride;
                case WeaponMode.Trap:
                    return s_cinematicSettings.TrapCameraOverride;
                case WeaponMode.Bow:
                case WeaponMode.Thrown:
                    return s_cinematicSettings.BowCameraOverride; // Bows/thrown use same override
                default:
                    return CameraOverride.Auto;
            }
        }

        /// <summary>
        /// Detects if player is indoors by checking for blocks above the player
        /// Uses 7DTD's block system instead of Unity Physics
        /// </summary>
        private static bool IsPlayerIndoors()
        {
            var player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player == null) return false;

            try
            {
                var world = GameManager.Instance.World;
                if (world == null) return false;
                
                // Check for blocks above player position
                float checkHeight = s_cinematicSettings?.IndoorDetectionHeight ?? 10f;
                Vector3i playerBlockPos = new Vector3i(player.position);
                
                // Sample a few points above the player
                for (int y = 1; y <= (int)checkHeight; y++)
                {
                    Vector3i checkPos = new Vector3i(playerBlockPos.x, playerBlockPos.y + y, playerBlockPos.z);
                    BlockValue block = world.GetBlock(checkPos);
                    
                    // If we find a solid block above, player is likely indoors
                    if (!block.isair && !block.isWater)
                    {
                        CKLog.Verbose($" Indoor detection - found block at height +{y}");
                        return true;
                    }
                }
                
                // No ceiling found - outdoors
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning($"CinematicKill: Indoor detection failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Applies weapon mode camera override to the selection
        /// Returns true if an override was applied
        /// </summary>
        private static bool ApplyWeaponCameraOverride(ref bool useProjectile)
        {
            CameraOverride camOverride = GetWeaponModeCameraOverride();
            
            if (camOverride == CameraOverride.FirstPersonOnly)
            {
                useProjectile = false;
                CKLog.Verbose($" Weapon mode {s_currentWeaponMode} forces First Person camera");
                return true;
            }
            else if (camOverride == CameraOverride.ProjectileOnly)
            {
                useProjectile = true;
                CKLog.Verbose($" Weapon mode {s_currentWeaponMode} forces Projectile camera");
                return true;
            }
            
            return false;
        }

        private static bool ApplySmartIndoorOutdoorSelection(bool allowFirstPerson, bool allowProjectile, ref bool useProjectile)
        {
            if (s_cinematicSettings == null || !s_cinematicSettings.SmartIndoorOutdoorDetection)
            {
                return false;  // Smart mode disabled, fall through to normal selection
            }

            bool isIndoors = IsPlayerIndoors();
            CKLog.Verbose($"Smart mode active - IsIndoors={isIndoors}");
            
            if (isIndoors)
            {
                // INDOOR: ALWAYS use First Person camera, regardless of trigger settings
                // This provides the best indoor experience - projectile cam doesn't work well indoors
                useProjectile = false;
                CKLog.Verbose("Smart mode - FORCING First Person camera (indoor kill)");
                return true;
            }
            else
            {
                // OUTDOOR: Use projectile if allowed, otherwise FP
                if (allowProjectile)
                {
                    useProjectile = true;
                    CKLog.Verbose("Smart mode - Using Projectile camera (outdoor kill)");
                    return true;
                }
                else if (allowFirstPerson)
                {
                    useProjectile = false;
                    CKLog.Verbose("Smart mode - Using First Person camera (outdoor, no projectile allowed)");
                    return true;
                }
                // No cameras allowed - fall through
                return false;
            }
        }

        private static bool CanRunFirstPersonCamera()
        {
            if (!CKSettings.EnableFirstPersonCamera) return false;
            float chance = RandomizeValue(CKSettings.FirstPersonCameraChance, s_cinematicSettings.FirstPersonCameraChanceMin, s_cinematicSettings.FirstPersonCameraChanceMax, s_cinematicSettings.RandomizeFirstPersonCameraChance);
            float roll = UnityEngine.Random.value;
            if (roll > chance)
            {
                CKLog.Verbose($" First-person roll failed. Roll={roll:F2}, Chance={chance:F2}");
                return false;
            }
            CKLog.Verbose($" First-person roll passed. Roll={roll:F2}, Chance={chance:F2}");
            return true;
        }

        /// <summary>
        /// Choose camera mode using trigger settings directly
        /// When a trigger has Override enabled, it bypasses global camera settings entirely
        /// </summary>
        private static bool ChooseModeFromTrigger(CKContextTriggerSettings trigger, bool isLastEnemy, out bool useProjectile)
        {
            useProjectile = false;
            if (trigger == null) return false;
            
            // Check weapon mode camera override first - this takes highest priority
            if (ApplyWeaponCameraOverride(ref useProjectile))
            {
                return true;
            }
            
            // Check if this trigger has override enabled (OverrideSlowScale means it's using its own settings)
            bool hasOverride = trigger.OverrideSlowScale || trigger.OverrideDuration;
            
            // Check if this is a BasicKill trigger (Priority 10) vs special trigger (Priority 50+)
            bool isBasicKill = trigger.Priority <= 10;
            
            bool allowFP;
            bool allowProj;
            
            if (isBasicKill || hasOverride)
            {
                // BasicKill OR Override triggers: Use the trigger's own camera settings directly
                // When Override is enabled, the trigger's camera settings bypass global settings
                allowFP = trigger.AllowFirstPerson;
                allowProj = trigger.AllowProjectile;
                CKLog.Verbose($" {(isBasicKill ? "BasicKill" : "Override")} camera - FP:{allowFP}, Proj:{allowProj}");
            }
            else
            {
                // Non-override special triggers: AND with global trigger defaults as master toggles
                allowFP = CKSettings.EnableFirstPersonCamera && trigger.AllowFirstPerson;
                allowProj = CKSettings.EnableProjectileCamera && trigger.AllowProjectile;
                CKLog.Verbose($" Default trigger camera - GlobalFP:{CKSettings.EnableFirstPersonCamera}, GlobalProj:{CKSettings.EnableProjectileCamera}, TriggerFP:{trigger.AllowFirstPerson}, TriggerProj:{trigger.AllowProjectile}");
            }
            
            CKLog.Verbose($" Camera routing - FP:{allowFP}, Proj:{allowProj}");

            if (ApplySmartIndoorOutdoorSelection(allowFP, allowProj, ref useProjectile))
            {
                return true;
            }
            
            if (!allowFP && !allowProj)
            {
                CKLog.Verbose(" No camera mode allowed by trigger.");
                return false;
            }
            
            if (allowFP && !allowProj)
            {
                useProjectile = false;
                return true;
            }
            
            if (!allowFP && allowProj)
            {
                useProjectile = true;
                return true;
            }
            
            // Both allowed - use trigger's first-person camera chance to decide
            float triggerFpChance = trigger.FirstPersonChance / 100f; // Convert from 0-100 to 0-1
            float roll = UnityEngine.Random.value;
            useProjectile = roll > triggerFpChance;
            CKLog.Verbose($" Camera split - roll={roll:F2}, triggerFpChance={triggerFpChance:F2}, useProjectile={useProjectile}");
            return true;
        }

        private static bool ChooseMode(bool isLastEnemy, string triggerReason, out bool useProjectile)
        {
            // Check weapon mode camera override first - this takes highest priority
            useProjectile = false;
            if (ApplyWeaponCameraOverride(ref useProjectile))
            {
                return true;
            }
            
            bool allowFP = CKSettings.EnableFirstPersonCamera;
            bool allowProj = CKSettings.EnableProjectileCamera;

            // ═══════════════════════════════════════════════════════════════
            // WEAPON MODE CAMERA SELECTION - Apply per-mode camera preferences
            // ═══════════════════════════════════════════════════════════════
            var modeSettings = GetModeSettings(s_currentWeaponMode);
            if (modeSettings != null && s_currentWeaponMode != WeaponMode.Unknown)
            {
                // Mode-specific camera routing takes priority
                bool modeAllowsFP = modeSettings.UseFirstPersonCamera;
                bool modeAllowsProj = modeSettings.UseProjectileCamera;
                
                // If mode has specific preferences, apply them
                if (modeAllowsFP || modeAllowsProj)
                {
                    allowFP = allowFP && modeAllowsFP;
                    allowProj = allowProj && modeAllowsProj;
                    CKLog.Verbose($" {s_currentWeaponMode} mode camera prefs - FP:{modeAllowsFP}, Proj:{modeAllowsProj}");
                }
            }

            // Per-trigger routing overrides (context triggers can further restrict)
            switch (triggerReason)
            {
                case "Crit":
                    allowFP = allowFP && CKSettings.CritAllowFirstPerson;
                    allowProj = allowProj && CKSettings.CritAllowProjectile;
                    break;
                case "Dismember":
                    allowFP = allowFP && CKSettings.DismemberAllowFirstPerson;
                    allowProj = allowProj && CKSettings.DismemberAllowProjectile;
                    break;
                case "LongRange":
                    allowFP = allowFP && CKSettings.LongRangeAllowFirstPerson;
                    allowProj = allowProj && CKSettings.LongRangeAllowProjectile;
                    break;
                case "LowHealth":
                    allowFP = allowFP && CKSettings.LowHealthAllowFirstPerson;
                    allowProj = allowProj && CKSettings.LowHealthAllowProjectile;
                    break;
            }

            // Last enemy routing gates (mutually exclusive preference)
            if (isLastEnemy)
            {
                bool forceFP = CKSettings.AllowLastEnemyFirstPerson;
                bool forceProj = CKSettings.AllowLastEnemyProjectile;

                // Guard against both being true; prefer the one set last by UI (but default to FP if both true)
                if (forceFP && forceProj)
                {
                    forceProj = false;
                }

                if (forceFP)
                {
                    allowFP = CKSettings.EnableFirstPersonCamera;
                    allowProj = false;
                }
                else if (forceProj)
                {
                    allowProj = CKSettings.EnableProjectileCamera;
                    allowFP = false;
                }
                // else: fall through to normal split
            }

            if (ApplySmartIndoorOutdoorSelection(allowFP, allowProj, ref useProjectile))
            {
                return true;
            }

            if (!allowFP && !allowProj)
            {
                useProjectile = false;
                return false;
            }

            if (allowFP && !allowProj)
            {
                useProjectile = false;
                return true;
            }

            if (!allowFP && allowProj)
            {
                useProjectile = true;
                return true;
            }

            // Both allowed: use split
            float fpChance = RandomizeValue(CKSettings.FirstPersonCameraChance, s_cinematicSettings.FirstPersonCameraChanceMin, s_cinematicSettings.FirstPersonCameraChanceMax, s_cinematicSettings.RandomizeFirstPersonCameraChance);
            float roll = UnityEngine.Random.value;
            useProjectile = roll > fpChance;
            return true;
        }

        private static void ClampFovDurationsRuntime(ref float zoomIn, ref float hold, ref float zoomOut, float cap)
        {
            zoomIn = Mathf.Clamp(zoomIn, 0.05f, cap);
            hold = Mathf.Clamp(hold, 0f, cap);
            zoomOut = Mathf.Clamp(zoomOut, 0.05f, cap);
            float total = zoomIn + hold + zoomOut;
            if (total > cap)
            {
                float excess = total - cap;
                if (hold > 0f)
                {
                    float reduceHold = Mathf.Min(hold, excess);
                    hold -= reduceHold;
                    excess -= reduceHold;
                }
                if (excess > 0f)
                {
                    zoomOut = Mathf.Max(0.05f, zoomOut - excess);
                }
            }
        }

        private static void ApplyDurationCaps(CinematicKillSettings s)
        {
            // NOTE: MenuV2 handles validation at UI level, this is now a no-op
            // Legacy properties forward to MenuV2 which validates its own ranges
            // The ref parameters cannot be used with properties, but the clamping
            // is no longer needed since MenuV2 sliders enforce valid ranges
        }

        private static void BuildProjectileRuntime(bool isLastEnemy, bool forceUse = false)
        {
            BuildProjectileRuntime(isLastEnemy, forceUse, null, null);
        }
        
        private static void BuildProjectileRuntime(bool isLastEnemy, bool forceUse, CKContextTriggerSettings triggerOverride, CKModeSettings modeOverride, float runtimeDurationOverride = -1f, float runtimeSlowOverride = -1f, bool isSpecialTrigger = false)
        {
            var s = s_cinematicSettings;
            
            // ═══════════════════════════════════════════════════════════════════════
            // APPLY RANDOM PRESET - Select and apply a random enabled preset
            // ═══════════════════════════════════════════════════════════════════════
            var projCam = s.ProjectileCamera;
            if (projCam != null && projCam.UseStandardPresets)
            {
                int presetIndex = projCam.GetRandomEnabledPreset(isSpecialTrigger);
                
                if (presetIndex == -1)
                {
                    // Near Player preset selected - use player-relative positioning
                    projCam.Distance = projCam.NearPlayerDistance;
                    projCam.Height = projCam.NearPlayerHeight;
                    projCam.XOffset = UnityEngine.Random.Range(-projCam.NearPlayerXOffset, projCam.NearPlayerXOffset);
                    projCam.Pitch = -15f; // Default pitch looking slightly down
                    projCam.Yaw = 0f;
                    projCam.Tilt = 0f;
                    CKLog.Verbose($" Projectile camera using preset: ✦ Near Player (D:{projCam.Distance:F1}, H:{projCam.Height:F1}, X:{projCam.XOffset:+0.0;-0.0})");
                }
                else
                {
                    projCam.ApplyStandardPreset(presetIndex);
                    CKLog.Verbose($" Projectile camera using preset: {StandardCameraPreset.All[presetIndex].Name}");
                }
                
                // ═══════════════════════════════════════════════════════════════════════
                // APPLY GLOBAL RANDOMIZATION - Tilt and Side Offset
                // ═══════════════════════════════════════════════════════════════════════
                
                // Apply tilt randomization if enabled
                if (projCam.RandomizeTilt)
                {
                    float randomTilt = UnityEngine.Random.Range(-projCam.RandomTiltRange, projCam.RandomTiltRange);
                    projCam.Tilt = randomTilt;
                    CKLog.Verbose($" Tilt randomized: {randomTilt:F1}° (range: ±{projCam.RandomTiltRange:F0}°)");
                }
                
                // Apply side offset randomization based on enabled levels
                // Each level has asymmetric left/right ranges that avoid the center for wider shots
                if (projCam.RandomizeSideOffset)
                {
                    var selectedLevel = projCam.GetRandomEnabledSideOffsetLevel();
                    if (selectedLevel.HasValue)
                    {
                        float randomOffset;
                        string rangeDesc;
                        
                        // Randomly choose left or right side
                        bool useRightSide = UnityEngine.Random.value > 0.5f;
                        
                        switch (selectedLevel.Value)
                        {
                            case SideOffsetLevel.Wide:
                                // Wide: -4 to -2 (left) OR 2 to 4 (right) - avoids center
                                randomOffset = useRightSide 
                                    ? UnityEngine.Random.Range(2f, 4f) 
                                    : UnityEngine.Random.Range(-4f, -2f);
                                rangeDesc = "-4 to -2 | 2 to 4";
                                break;
                                
                            case SideOffsetLevel.Medium:
                                // Standard: -2 to 0 (left) OR 0 to 2 (right)
                                randomOffset = useRightSide 
                                    ? UnityEngine.Random.Range(0f, 2f) 
                                    : UnityEngine.Random.Range(-2f, 0f);
                                rangeDesc = "-2 to 0 | 0 to 2";
                                break;
                                
                            default: // Tight
                                // Tight: -1 to 0 (left) OR 0 to 1 (right)
                                randomOffset = useRightSide 
                                    ? UnityEngine.Random.Range(0f, 1f) 
                                    : UnityEngine.Random.Range(-1f, 0f);
                                rangeDesc = "-1 to 0 | 0 to 1";
                                break;
                        }
                        
                        projCam.XOffset = randomOffset;
                        CKLog.Verbose($" Side offset randomized: {randomOffset:F1}m (level: {selectedLevel.Value}, range: {rangeDesc})");
                    }
                }
            }
            
            var coreSettings = s.MenuV2?.Core;
            float baseDuration = runtimeDurationOverride > 0f
                ? runtimeDurationOverride
                : (coreSettings?.SlowMoDuration ?? 1.2f);

            float baseSlowScale = runtimeSlowOverride > 0f
                ? runtimeSlowOverride
                : (coreSettings?.GlobalTimeScale ?? 0.2f);

            s_projectileRuntime = new ProjectileRuntime
            {
                Duration = baseDuration,
                SlowScale = baseSlowScale,
                ReturnDuration = Mathf.Clamp(baseDuration * 0.5f, 0.01f, baseDuration),
                Chance = 1f,
                LastEnemyOnly = false,
                HeightOffset = projCam?.Height ?? (s.MenuV2?.ProjectileCamera?.FollowHeight ?? 0f),
                DistanceOffset = projCam?.Distance ?? (s.MenuV2?.ProjectileCamera?.FollowDistance ?? 2f),
                XOffset = projCam?.XOffset ?? 0f,
                LookYaw = projCam?.Yaw ?? (s.MenuV2?.ProjectileCamera?.CameraTilt ?? 0f),
                LookPitch = projCam?.Pitch ?? 0f,
                Tilt = projCam?.Tilt ?? 0f,  // Roll/Dutch angle
                RandomYawRange = 0f,
                RandomPitchRange = 0f,
                FOV = CKSettings.FOVZoomAmount,
                // Fixed ratio: 20% in, 70% hold, 10% out
                FOVIn = Mathf.Max(0.05f, baseDuration * 0.2f),
                FOVHold = Mathf.Max(0.05f, baseDuration * 0.7f),
                FOVOut = Mathf.Max(0.05f, baseDuration * 0.1f),
                EnableVignette = false,
                VignetteIntensity = 0.4f
            };
            
            // ═══════════════════════════════════════════════════════════════════════
            // APPLY TRIGGER SIMPLE OVERRIDE - When OverrideSlowScale is true,
            // use trigger's SlowScale, DurationSeconds, and CooldownSeconds
            // ═══════════════════════════════════════════════════════════════════════
            if (triggerOverride != null && triggerOverride.OverrideSlowScale)
            {
                s_projectileRuntime.Duration = triggerOverride.RandomizeDuration
                    ? UnityEngine.Random.Range(triggerOverride.DurationMin, triggerOverride.DurationMax)
                    : triggerOverride.DurationSeconds;
                    
                s_projectileRuntime.SlowScale = triggerOverride.RandomizeSlowScale
                    ? UnityEngine.Random.Range(triggerOverride.SlowScaleMin, triggerOverride.SlowScaleMax)
                    : triggerOverride.SlowScale;
                    
                CKLog.Verbose($" Projectile using trigger simple override - Duration: {s_projectileRuntime.Duration:F2}s, Scale: {s_projectileRuntime.SlowScale:F2}x");
            }
            // ═══════════════════════════════════════════════════════════════════════
            // APPLY TRIGGER ADVANCED OVERRIDES - Individual setting overrides
            // Only apply if simple override is NOT enabled
            // ═══════════════════════════════════════════════════════════════════════
            else if (triggerOverride != null)
            {
                if (triggerOverride.OverrideDuration)
                {
                    s_projectileRuntime.Duration = triggerOverride.RandomizeDuration
                        ? UnityEngine.Random.Range(triggerOverride.DurationMin, triggerOverride.DurationMax)
                        : triggerOverride.DurationSeconds;
                    CKLog.Verbose($" Projectile using trigger advanced duration override: {s_projectileRuntime.Duration:F2}s");
                }
                // Check mode override if trigger doesn't override
                else if (modeOverride != null && modeOverride.OverrideGlobalSlowMo)
                {
                    s_projectileRuntime.Duration = modeOverride.OverrideDurationSeconds;
                    CKLog.Verbose($" Projectile using mode duration override: {s_projectileRuntime.Duration:F2}s");
                }
            }
            // If no trigger, check mode override
            else if (modeOverride != null && modeOverride.OverrideGlobalSlowMo)
            {
                s_projectileRuntime.Duration = modeOverride.OverrideDurationSeconds;
                CKLog.Verbose($" Projectile using mode duration override: {s_projectileRuntime.Duration:F2}s");
            }
            
            // Always clamp FOV to duration - FOV should never last longer than the cinematic
            float fovCap = s_projectileRuntime.Duration;
            ClampFovDurationsRuntime(ref s_projectileRuntime.FOVIn, ref s_projectileRuntime.FOVHold, ref s_projectileRuntime.FOVOut, fovCap);
        }

        private static IEnumerator RestoreTimeScaleRoutine(float duration)
        {
            float start = Time.timeScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                ApplyTimeScale(Mathf.Lerp(start, 1f, t));
                yield return null;
            }

            ApplyTimeScale(1f);
            s_timeRestoreCoroutine = null;
        }

        private static void StopTimeRestoreCoroutine()
        {
            if (s_timeRestoreCoroutine != null && s_menuComponent != null)
            {
                s_menuComponent.StopCoroutine(s_timeRestoreCoroutine);
                s_timeRestoreCoroutine = null;
            }
        }

        private static void ApplySlowMoAudio(bool enable, float volume)
        {
            if (!enable) return;
            var listenerType = Type.GetType("UnityEngine.AudioListener, UnityEngine.AudioModule");
            if (listenerType == null) return;
            var volumeProp = listenerType.GetProperty("volume", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (volumeProp == null || !volumeProp.CanRead || !volumeProp.CanWrite) return;
            s_originalAudioVolume = Convert.ToSingle(volumeProp.GetValue(null, null));
            volumeProp.SetValue(null, Mathf.Clamp01(volume), null);
            s_audioAdjusted = true;
        }

        private static void RestoreAudio()
        {
            if (!s_audioAdjusted) return;
            var listenerType = Type.GetType("UnityEngine.AudioListener, UnityEngine.AudioModule");
            if (listenerType != null)
            {
                var volumeProp = listenerType.GetProperty("volume", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (volumeProp != null && volumeProp.CanWrite)
                {
                    volumeProp.SetValue(null, s_originalAudioVolume, null);
                }
            }
            s_audioAdjusted = false;
        }

        /// <summary>
        /// Shows or hides the game's HUD during cinematics.
        /// Uses multiple approaches to find and toggle HUD visibility.
        /// </summary>
        private static bool s_hudWasHidden = false;
        private static List<GameObject> s_hudElements = null;
        private static List<GameObject> s_scopeElements = null;  // Separate list for scope elements
        private static Dictionary<GameObject, bool> s_originalHudStates = new Dictionary<GameObject, bool>();
        
        private static void SetGameHUDVisible(EntityPlayerLocal player, bool visible)
        {
            try
            {
                if (player == null) return;
                
                // Scope visibility: hide in projectile mode, preserve in first-person mode
                bool hideScope = s_isProjectileCameraActive;
                
                // Initialize HUD element list on first call - search for all HUD elements
                if (s_hudElements == null)
                {
                    s_hudElements = new List<GameObject>();
                    s_scopeElements = new List<GameObject>();
                    
                    // Comprehensive list of 7DTD HUD element names
                    string[] hudNames = new string[]
                    {
                        // XUi window names (most common in 7DTD)
                        "windowToolbelt",
                        "windowCompass",
                        // "windowCrosshair" - moved to scopeNames to preserve sniper scope in FP mode 
                        "windowHealthBar",
                        "windowStatBars",
                        "windowBuffs",
                        "windowAmmoCount",
                        "windowQuickSlots",
                        "windowMiniMap",
                        "windowQuestTracker",
                        "windowHUD",
                        "windowPlayerInfo",
                        "windowFoodBar",
                        "windowWaterBar",
                        "windowStaminaBar",
                        "windowHealthHUD",
                        "windowPlayerStats",
                        // Legacy/alternate names
                        "HUDLeft",
                        "HUDRight", 
                        "HUDTop",
                        "HUDBottom",
                        "HUDCenter",
                        "IngameHUD",
                        "InGameHUD",
                        "GameHUD",
                        "HUD",
                        "HUDRoot",
                        "UIRoot"
                    };
                    
                    // Scope-related element names (will be preserved in first-person mode, hidden in projectile mode)
                    string[] scopeNames = new string[]
                    {
                        "windowScope",
                        "windowCrosshair",  // Moved here - preserve sniper crosshair in FP mode
                        "SniperScope",
                        "ScopeOverlay",
                        "Scope",
                        "windowScopeOverlay"
                    };
                    
                    // Find scope elements first
                    foreach (string name in scopeNames)
                    {
                        GameObject go = GameObject.Find(name);
                        if (go != null && !s_scopeElements.Contains(go))
                        {
                            s_scopeElements.Add(go);
                            CKLog.Verbose($" Found scope element '{name}'");
                        }
                    }
                    
                    foreach (string name in hudNames)
                    {
                        GameObject go = GameObject.Find(name);
                        if (go != null && !s_hudElements.Contains(go) && !s_scopeElements.Contains(go))
                        {
                            s_hudElements.Add(go);
                            CKLog.Verbose($" Found HUD element '{name}' for hiding");
                        }
                    }
                    
                    // Also try to find all children of main NGUI/XUi root that contain "window" in name
                    try
                    {
                        GameObject nguiRoot = GameObject.Find("NGUI Root (2D)");
                        if (nguiRoot != null)
                        {
                            foreach (Transform child in nguiRoot.transform)
                            {
                                string childName = child.name.ToLower();
                                
                                // Check for scope or crosshair elements (preserve in FP mode)
                                if (childName.Contains("scope") || childName.Contains("crosshair"))
                                {
                                    if (!s_scopeElements.Contains(child.gameObject))
                                    {
                                        s_scopeElements.Add(child.gameObject);
                                        CKLog.Verbose($" Found NGUI scope/crosshair child '{child.name}'");
                                    }
                                    continue;
                                }
                                
                                if (childName.Contains("hud") || childName.Contains("toolbelt") || 
                                    childName.Contains("compass") ||
                                    childName.Contains("stat") || childName.Contains("buff") ||
                                    childName.Contains("health") || childName.Contains("ammo"))
                                {
                                    if (!s_hudElements.Contains(child.gameObject) && !s_scopeElements.Contains(child.gameObject))
                                    {
                                        s_hudElements.Add(child.gameObject);
                                        CKLog.Verbose($" Found NGUI HUD child '{child.name}' for hiding");
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                    
                    if (s_hudElements.Count == 0)
                    {
                        Log.Warning("CinematicKill: Could not find any HUD elements to hide");
                    }
                    else
                    {
                        CKLog.Verbose($" Found {s_hudElements.Count} HUD elements for hiding, {s_scopeElements.Count} scope elements");
                    }
                }
                
                // Toggle visibility of found elements
                if (s_hudElements.Count > 0)
                {
                    foreach (var go in s_hudElements)
                    {
                        if (go == null) continue;
                        
                        if (!visible)
                        {
                            // Save original state before hiding
                            if (!s_originalHudStates.ContainsKey(go))
                            {
                                s_originalHudStates[go] = go.activeSelf;
                            }
                            go.SetActive(false);
                        }
                        else
                        {
                            // Restore original state
                            if (s_originalHudStates.TryGetValue(go, out bool wasActive))
                            {
                                go.SetActive(wasActive);
                            }
                            else
                            {
                                go.SetActive(true);
                            }
                        }
                    }
                    
                    // Handle scope elements separately (hide in projectile mode, preserve in first-person mode)
                    if (s_scopeElements != null && s_scopeElements.Count > 0 && hideScope)
                    {
                        foreach (var go in s_scopeElements)
                        {
                            if (go == null) continue;
                            
                            if (!visible)
                            {
                                if (!s_originalHudStates.ContainsKey(go))
                                {
                                    s_originalHudStates[go] = go.activeSelf;
                                }
                                go.SetActive(false);
                            }
                            else
                            {
                                if (s_originalHudStates.TryGetValue(go, out bool wasActive))
                                {
                                    go.SetActive(wasActive);
                                }
                                else
                                {
                                    go.SetActive(true);
                                }
                            }
                        }
                    }
                    
                    if (!visible)
                    {
                        s_hudWasHidden = true;
                        CKLog.Verbose($" Hidden {s_hudElements.Count} HUD elements" + 
                            (!hideScope ? " (scope preserved)" : $", {s_scopeElements?.Count ?? 0} scope elements"));
                    }
                    else if (s_hudWasHidden)
                    {
                        s_hudWasHidden = false;
                        s_originalHudStates.Clear();
                        CKLog.Verbose($" Restored {s_hudElements.Count} HUD elements");
                    }
                    return;
                }
                
                // Fallback: Try to access XUi directly
                try
                {
                    var lpUI = LocalPlayerUI.GetUIForPlayer(player);
                    if (lpUI?.xui != null)
                    {
                        // Try to toggle HUD via XUi
                        var xui = lpUI.xui;
                        var playerUI = xui.playerUI;
                        if (playerUI != null)
                        {
                            var windowManager = playerUI.windowManager;
                            if (windowManager != null)
                            {
                                // Hide/show all HUD windows
                                if (!visible)
                                {
                                    windowManager.CloseAllOpenWindows();
                                    s_hudWasHidden = true;
                                    CKLog.Verbose(" Closed all HUD windows via windowManager");
                                }
                                // Note: For restoring, windows will auto-open as needed
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    CKLog.Verbose($" XUi approach failed: {ex.Message}");
                }
                
                Log.Warning("CinematicKill: Could not find method to toggle HUD visibility");
            }
            catch (Exception ex)
            {
                Log.Warning($"CinematicKill: Failed to toggle HUD visibility: {ex.Message}");
            }
        }

        private static void StartFirstPersonFallback(EntityPlayerLocal player)
        {
            if (player == null) return;
            if (!CanRunFirstPersonCamera()) return;

            var defaultMods = new CinematicContextManager.ContextModifiers
            {
                DurationMultiplier = 1f,
                SlowScaleMultiplier = s_cinematicSettings.EnableFirstPersonSlowMo ? 1f : (1f / Mathf.Max(0.01f, s_slowScale)),
                ZoomMultiplier = 1f,
                ZoomSpeedMultiplier = 1f,
                ScreenTint = new Color(0.1f, 0.1f, 0.1f, 0.1f),
                TriggerFlash = false,
                DebugInfo = "ProjectileCameraSkipped",
                TargetDistance = float.MaxValue
            };
            CKLog.Verbose(" Triggering first-person fallback cinematic.");
            StartSequence(player, defaultMods, "ProjectileSkipped");
        }

        // Projectile Camera
        public static ProjectileMoveScript CurrentProjectile { get; set; }
        private static Transform s_projectileCameraTarget;
        private static Transform s_cameraFallbackTarget;
        private static Vector3 s_originalCameraPos;
        private static Quaternion s_originalCameraRot;
        private static bool s_isProjectileCameraActive;
        private static bool s_wasFirstPersonView; // v2.5: Save original view state
        private static Quaternion s_savedPlayerRotation; // v2.5: Lock player rotation during projectile camera
        private static Vector3 s_lastTargetPos;
        private static Quaternion s_cameraRot;
        private static ProjectileMoveScript s_pendingProjectileCamera;
        private static int s_pendingProjectileVictimId = -1;
        
        /// <summary>
        /// Public accessor for projectile camera state. Used by WeaponAttackBlockPatch
        /// to prevent extra damage shots while camera is repositioned.
        /// </summary>
        public static bool IsProjectileCameraActive => s_isProjectileCameraActive;

        public static void QueueProjectileCamera(ProjectileMoveScript projectile, EntityAlive victim, DamageSource source)
        {
            if (projectile == null || victim == null) return;
            if (source is not DamageSourceEntity damageSource) return;
            if (!CKSettings.EnableCinematics) return;

            var player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player == null || damageSource.getEntityId() != player.entityId) return;

            s_pendingProjectileCamera = projectile;
            s_pendingProjectileVictimId = victim.entityId;
        }

        private static bool TryConsumeQueuedProjectile(EntityAlive victim, out ProjectileMoveScript projectile)
        {
            projectile = null;
            if (victim == null || s_pendingProjectileCamera == null) return false;
            if (victim.entityId != s_pendingProjectileVictimId) return false;

            projectile = s_pendingProjectileCamera;
            s_pendingProjectileCamera = null;
            s_pendingProjectileVictimId = -1;
            return projectile != null;
        }

        private static void ClearQueuedProjectile(EntityAlive victim)
        {
            if (victim == null) return;
            if (victim.entityId != s_pendingProjectileVictimId) return;

            s_pendingProjectileCamera = null;
            s_pendingProjectileVictimId = -1;
        }

        public static void TriggerProjectileCamera(ProjectileMoveScript projectile, EntityAlive victim, string triggerReason = "Projectile", bool skipChecks = false)
        {
            if (projectile == null || victim == null) return;

            var player = GameManager.Instance.World.GetPrimaryPlayer();
            if (player == null) return;
            bool isLastEnemy = IsLastEnemy(victim, player);

            if (!skipChecks)
            {
                if (!CKSettings.EnableCinematics || !CKSettings.EnableProjectileCamera)
                {
                    CKLog.Verbose(" Projectile camera disabled.");
                    return;
                }

                // Weapon mode camera override check - if weapon forces first-person, don't use projectile
                bool useProjectile = true;
                if (ApplyWeaponCameraOverride(ref useProjectile))
                {
                    if (!useProjectile)
                    {
                        CKLog.Verbose($" Weapon mode {s_currentWeaponMode} forces First Person - blocking projectile camera");
                        StartFirstPersonFallback(player);
                        return;
                    }
                }

                // Smart indoor detection - block projectile camera when indoors
                if (s_cinematicSettings != null && s_cinematicSettings.SmartIndoorOutdoorDetection)
                {
                    bool isIndoors = IsPlayerIndoors();
                    if (isIndoors)
                    {
                        CKLog.Verbose(" Smart mode: Player is INDOORS - blocking projectile camera, using First Person");
                        StartFirstPersonFallback(player);
                        return;
                    }
                    else
                    {
                        CKLog.Verbose(" Smart mode: Player is OUTDOORS - allowing projectile camera");
                    }
                }

                // Last enemy routing
                if (isLastEnemy && !CKSettings.AllowLastEnemyProjectile)
                {
                    CKLog.Verbose(" Last enemy routing blocks projectile mode.");
                    StartFirstPersonFallback(player);
                    return;
                }

                BuildProjectileRuntime(isLastEnemy);
            }
            else
            {
                CKLog.Verbose(" Projectile camera using pre-validated trigger routing.");
            }

            // Reset state
            s_originalCameraPos = Vector3.zero;
            s_originalCameraRot = Quaternion.identity;

            // LastEnemyOnly setting - require this to be the last enemy
            if (s_projectileRuntime.LastEnemyOnly)
            {
                int nearbyEnemies = ScanForEnemies(victim.position, CKSettings.EnemyScanRadius, victim.entityId);
                if (nearbyEnemies > 0)
                {
                    CKLog.Verbose(" Projectile blocked by LastEnemyOnly requirement.");
                    StartFirstPersonFallback(player);
                    return;
                }
            }

            // Save original camera state
            if (Camera.main != null)
            {
                s_originalCameraPos = Camera.main.transform.position;
                s_originalCameraRot = Camera.main.transform.rotation;
                CKLog.Verbose($" Saved Camera Pos {s_originalCameraPos}");
            }
            else
            {
                Log.Warning("CinematicKill: Camera.main is null in TriggerProjectileCamera!");
            }

            // v2.5: Save original view state before switching
            s_wasFirstPersonView = player.bFirstPersonView;
            CKLog.Verbose($" Saved view state: wasFirstPerson={s_wasFirstPersonView}");
            
            // ═══════════════════════════════════════════════════════════════════════
            // EXPERIMENTAL: Projectile Ride Cam - Check if we should use ride cam
            // ═══════════════════════════════════════════════════════════════════════
            s_isRideCamActive = false;
            s_rideCamProjectileTransform = null;
            
            var expSettings = CKSettings.Experimental;
            if (expSettings.EnableProjectileRideCam && projectile != null && projectile.transform != null)
            {
                // Check if predictive aim already queued this (set before shot by PredictiveAimPatch)
                if (s_predictiveRideCamQueued)
                {
                    s_isRideCamActive = true;
                    s_rideCamProjectileTransform = projectile.transform;
                    s_predictiveRideCamQueued = false; // Clear the flag
                    CKLog.Verbose($" RIDE CAM activated (PREDICTIVE - target: {s_predictiveTarget?.EntityName ?? "NULL"})");
                }
                else
                {
                    // Non-predictive fallback: Chance roll (this is the original behavior)
                    float rideRoll = UnityEngine.Random.Range(0f, 100f);
                    if (rideRoll <= expSettings.RideCamChance)
                    {
                        s_isRideCamActive = true;
                        s_rideCamProjectileTransform = projectile.transform;
                        CKLog.Verbose($" RIDE CAM activated (chance roll: {rideRoll:F1}% <= {expSettings.RideCamChance:F0}%)");
                    }
                    else
                    {
                        CKLog.Verbose($" Ride cam chance failed (roll: {rideRoll:F1}% > {expSettings.RideCamChance:F0}%)");
                    }
                }
            }

            // Fix invisible hands bug: Quick weapon slot switch to refresh hand model
            // Switching to another slot and back forces the hand renderer to refresh
            try 
            { 
                int currentSlot = player.inventory.holdingItemIdx;
                int switchToSlot = (currentSlot == 0) ? 1 : 0;
                
                // Quick switch to another slot and back - this refreshes the hand model
                player.inventory.SetHoldingItemIdxNoHolsterTime(switchToSlot);
                player.inventory.SetHoldingItemIdxNoHolsterTime(currentSlot);
                
                // Save player rotation to STATIC field BEFORE switching to 3rd person
                // The game rotates the player model to face the camera in 3rd person mode
                // We save it to a static field so we can continuously enforce it during Update
                s_savedPlayerRotation = player.transform.rotation;
                
                // Set to 3rd person for the projectile view
                player.SetFirstPersonView(false, false);
                
                // Restore player rotation IMMEDIATELY after switching to prevent facing camera
                player.transform.rotation = s_savedPlayerRotation;
                
                // Set camera blend to ensure proper state
                if (player.vp_FPCamera != null)
                {
                    player.vp_FPCamera.m_Current3rdPersonBlend = 1f;
                }
                
                CKLog.Verbose($"Applied weapon slot switch fix for hands visibility, locked player rotation to {s_savedPlayerRotation.eulerAngles}");
            } 
            catch (Exception ex)
            {
                Log.Warning($"CinematicKill: Hands fix failed: {ex.Message}");
            }

            // --- VICTIM LOCK MODE ---
            // Instead of following the projectile (which flies away) or rotating with the victim (which spins),
            // we attach to the victim's position but keep a FIXED rotation based on the impact angle.
            
            s_projectileCameraTarget = victim.transform; // Target the victim immediately
            s_cameraFallbackTarget = victim.transform;
            s_isProjectileCameraActive = true;
            s_lastTargetPos = victim.transform.position;
            
            // Start ragdoll tracking for dynamic duration and camera follow
            StartRagdollTracking(victim);

            // Calculate the rotation based on projectile direction
            Vector3 shotDirection = projectile.transform.forward;
            if (shotDirection == Vector3.zero) 
            {
                shotDirection = (victim.position - player.position).normalized;
            }

            s_cameraRot = BuildCameraRotation(shotDirection);

            // Set duration from projectile runtime
            s_duration = s_projectileRuntime.Duration;
            s_slowScale = s_projectileRuntime.SlowScale;

            // Calculate Slow Scale Multiplier
            float globalScale = Mathf.Max(0.001f, s_slowScale);
            float desiredScale = s_projectileRuntime.SlowScale;
            float scaleMult = s_cinematicSettings.EnableProjectileSlowMo ? desiredScale / globalScale : 1f;

            // Start the sequence
            var mods = new CinematicContextManager.ContextModifiers
            {
                DurationMultiplier = 1f,
                SlowScaleMultiplier = scaleMult,
                ZoomMultiplier = 1f,
                ZoomSpeedMultiplier = 1f,
                ScreenTint = new Color(0.1f, 0.1f, 0.1f, 0.1f),
                TriggerFlash = false,
                DebugInfo = "ProjectileCamera",
                TargetDistance = float.MaxValue
            };

            StartSequence(player, mods, triggerReason, isLastEnemy);
            
            // Using fast double toggle workaround above to fix invisible hands bug
            // Just stay in 3rd person for duration, will restore in RestoreCameraState
            
            CKLog.Verbose($" Projectile Camera Triggered on {victim.EntityName} (Mode: Follow, Reason: {triggerReason})");
        }


        public static void TriggerHitscanCamera(EntityAlive victim, Vector3 hitPosition, string triggerReason = "Hitscan", bool skipChecks = false)
        {
            if (victim == null) return;

            var player = GameManager.Instance.World.GetPrimaryPlayer();
            if (player == null) return;
            bool isLastEnemy = IsLastEnemy(victim, player);
            
            // Skip all checks if called from trigger routing (already validated)
            if (!skipChecks)
            {
                if (!CKSettings.EnableCinematics || !CKSettings.EnableProjectileCamera)
                {
                    CKLog.Verbose(" Projectile cam disabled for hitscan.");
                    return;
                }

                // Last enemy routing
                if (isLastEnemy && !CKSettings.AllowLastEnemyProjectile)
                {
                    CKLog.Verbose(" Last enemy routing blocks projectile mode (hitscan).");
                    StartFirstPersonFallback(player);
                    return;
                }

                BuildProjectileRuntime(isLastEnemy);

            }
            else
            {
                CKLog.Verbose(" Hitscan camera using pre-validated trigger routing.");
            }

            // Save original camera state
            if (Camera.main != null)
            {
                s_originalCameraPos = Camera.main.transform.position;
                s_originalCameraRot = Camera.main.transform.rotation;
                CKLog.Verbose($" Saved Camera Pos {s_originalCameraPos}");
            }

            // v2.5: Save original view state before switching
            s_wasFirstPersonView = player.bFirstPersonView;
            CKLog.Verbose($" Saved view state: wasFirstPerson={s_wasFirstPersonView}");

            // Fix invisible hands bug: Quick weapon slot switch to refresh hand model
            // Switching to another slot and back forces the hand renderer to refresh
            try 
            { 
                int currentSlot = player.inventory.holdingItemIdx;
                int switchToSlot = (currentSlot == 0) ? 1 : 0;
                
                // Quick switch to another slot and back - this refreshes the hand model
                player.inventory.SetHoldingItemIdxNoHolsterTime(switchToSlot);
                player.inventory.SetHoldingItemIdxNoHolsterTime(currentSlot);
                
                // Save player rotation to STATIC field BEFORE switching to 3rd person
                // The game rotates the player model to face the camera in 3rd person mode
                // We save it to a static field so we can continuously enforce it during Update
                s_savedPlayerRotation = player.transform.rotation;
                
                // Set to 3rd person for the projectile view
                player.SetFirstPersonView(false, false);
                
                // Restore player rotation IMMEDIATELY after switching to prevent facing camera
                player.transform.rotation = s_savedPlayerRotation;
                
                // Set camera blend to ensure proper state
                if (player.vp_FPCamera != null)
                {
                    player.vp_FPCamera.m_Current3rdPersonBlend = 1f;
                }
                
                CKLog.Verbose($"Applied weapon slot switch fix for hands visibility, locked player rotation to {s_savedPlayerRotation.eulerAngles}");
            } 
            catch (Exception ex)
            {
                Log.Warning($"CinematicKill: Hands fix failed: {ex.Message}");
            }

            // For hitscan, we use the same logic: Victim Lock with Fixed Rotation
            
            // Calculate a good camera angle
            // Look from the shooter (player) towards the victim
            Vector3 direction = (hitPosition - player.GetPosition()).normalized;
            
            // Set target to victim
            s_projectileCameraTarget = victim.transform;
            s_cameraFallbackTarget = victim.transform;
            s_isProjectileCameraActive = true;
            s_lastTargetPos = victim.transform.position;
            
            // Start ragdoll tracking for dynamic duration and camera follow
            StartRagdollTracking(victim);

            s_cameraRot = BuildCameraRotation(direction);

            // Set duration from projectile runtime
            s_duration = s_projectileRuntime.Duration;
            s_slowScale = s_projectileRuntime.SlowScale;

            // Calculate Slow Scale Multiplier
            float globalScale = Mathf.Max(0.001f, s_slowScale);
            float desiredScale = s_projectileRuntime.SlowScale;
            float scaleMult = s_cinematicSettings.EnableProjectileSlowMo ? desiredScale / globalScale : 1f;

            var mods = new CinematicContextManager.ContextModifiers
            {
                DurationMultiplier = 1f,
                SlowScaleMultiplier = scaleMult,
                ZoomMultiplier = 1f,
                ScreenTint = new Color(0.1f, 0.1f, 0.1f, 0.1f),
                TriggerFlash = false,
                DebugInfo = "HitscanCamera",
                TargetDistance = float.MaxValue
            };

            StartSequence(player, mods, triggerReason, isLastEnemy);
            
            // Using fast double toggle workaround above to fix invisible hands bug
            // Just stay in 3rd person for duration, will restore in RestoreCameraState
            
            CKLog.Verbose($" Hitscan Camera Triggered on {victim.EntityName}");
        }

        private static Quaternion BuildCameraRotation(Vector3 forward)
        {
            if (forward == Vector3.zero)
            {
                forward = Vector3.forward;
            }

            Quaternion rotation = Quaternion.LookRotation(forward.normalized);

            float randomYaw = UnityEngine.Random.Range(-s_projectileRuntime.RandomYawRange, s_projectileRuntime.RandomYawRange);
            float randomPitch = UnityEngine.Random.Range(-s_projectileRuntime.RandomPitchRange, s_projectileRuntime.RandomPitchRange);

            float yaw = s_projectileRuntime.LookYaw + randomYaw;
            float pitch = s_projectileRuntime.LookPitch + randomPitch;

            rotation = Quaternion.Euler(pitch, yaw, s_projectileRuntime.Tilt) * rotation;
            return rotation;
        }

        public static void HandleCameraUpdate()
        {
            if (!s_isProjectileCameraActive || Camera.main == null)
            {
                return;
            }
            
            // Debug: Log target state once per second to avoid spam
            if (s_cameraDebugTimer <= 0f)
            {
                s_cameraDebugTimer = 1f;
                CKLog.Verbose($" CameraUpdate: target={s_projectileCameraTarget?.name ?? "NULL"}, fallback={s_cameraFallbackTarget?.name ?? "NULL"}, camPos={Camera.main.transform.position}");
            }
            s_cameraDebugTimer -= Time.unscaledDeltaTime;
            
            // ═══════════════════════════════════════════════════════════════════════
            // EXPERIMENTAL: Projectile Ride Cam - Camera follows behind projectile
            // ═══════════════════════════════════════════════════════════════════════
            if (s_isRideCamActive && s_rideCamProjectileTransform != null)
            {
                try
                {
                    // Check if projectile still exists
                    if (s_rideCamProjectileTransform.gameObject == null || !s_rideCamProjectileTransform.gameObject.activeInHierarchy)
                    {
                        throw new System.Exception("Projectile destroyed");
                    }
                    
                    var exp = CKSettings.Experimental;
                    // Position camera BEHIND projectile to prevent clipping into target
                    Vector3 camPos = s_rideCamProjectileTransform.position - s_rideCamProjectileTransform.forward * exp.RideCamOffset;
                    Camera.main.transform.position = camPos;
                    Camera.main.transform.rotation = s_rideCamProjectileTransform.rotation;
                    Camera.main.fieldOfView = exp.RideCamFOV;
                    return; // Skip normal camera positioning
                }
                catch
                {
                    // Projectile destroyed - fall back to tracking predictive target
                    Log.Out("[CinematicKill] Projectile destroyed - transitioning to target tracking");
                    s_isRideCamActive = false;
                    s_rideCamProjectileTransform = null;
                    
                    // Set the predictive target as the camera target
                    if (s_predictiveTarget != null && !s_predictiveTarget.IsDead())
                    {
                        s_projectileCameraTarget = s_predictiveTarget.transform;
                        s_cameraFallbackTarget = s_predictiveTarget.transform;
                    }
                }
            }
            
            // ═══════════════════════════════════════════════════════════════════════
            // EXPERIMENTAL: Dismemberment Focus Cam - Camera follows severed limb
            // ═══════════════════════════════════════════════════════════════════════
            Transform target = s_projectileCameraTarget;
            if (s_isDismemberFocusCamActive && s_dismemberedLimbTarget != null)
            {
                try
                {
                    if (s_dismemberedLimbTarget.gameObject.activeInHierarchy)
                    {
                        target = s_dismemberedLimbTarget;
                    }
                    else
                    {
                        // Limb destroyed/deactivated, fall back to normal target
                        s_isDismemberFocusCamActive = false;
                        s_dismemberedLimbTarget = null;
                    }
                }
                catch
                {
                    s_isDismemberFocusCamActive = false;
                    s_dismemberedLimbTarget = null;
                }
            }

            // Victim Lock Mode (Follow)
            if (target == null) target = s_cameraFallbackTarget;
            if (target == null) return;

            // Robustness Check: Ensure target is valid and hasn't been pooled/reset
            Vector3 currentPos = target.position;
            bool isValid = true;
            string invalidReason = "";

            // Check 1: Object inactive (likely pooled)
            if (!target.gameObject.activeInHierarchy)
            {
                isValid = false;
                invalidReason = "Target inactive";
            }
            
            // Check 2: Position is exactly zero (suspicious for pooled entities)
            if (isValid && currentPos == Vector3.zero)
            {
                isValid = false;
                invalidReason = "Position is zero";
            }

            // Check 3: Large jump detection (teleportation to pool)
            // NOTE: Only apply this check for SAME-CINEMATIC jumps, not cross-cinematic
            // The s_lastTargetPos is reset at the start of each new cinematic in TriggerHitscanCamera
            // so this check is safe within a single cinematic session
            if (isValid && s_lastTargetPos != Vector3.zero && Vector3.Distance(currentPos, s_lastTargetPos) > 10f)
            {
                isValid = false;
                invalidReason = $"Large jump: {currentPos} -> {s_lastTargetPos} ({Vector3.Distance(currentPos, s_lastTargetPos):F1}m)";
            }

            if (isValid)
            {
                s_lastTargetPos = currentPos;
            }
            else
            {
                // Use last known valid position
                if (s_lastTargetPos != Vector3.zero)
                {
                    CKLog.Verbose($" Camera target invalid ({invalidReason}), using fallback at {s_lastTargetPos}");
                    currentPos = s_lastTargetPos;
                }
            }
            
            // Fix: Calculate camera position and rotation based on player-to-target direction
            // This ensures the camera is positioned between the player and target, looking at the target
            Vector3 playerPos = GameManager.Instance?.World?.GetPrimaryPlayer()?.GetPosition() ?? Vector3.zero;
            Vector3 toTarget = currentPos - playerPos;
            float horizontalDistance = new Vector3(toTarget.x, 0, toTarget.z).magnitude;
            
            // Calculate look direction from a point behind target (toward player) to the target
            Vector3 lookDir;
            if (horizontalDistance > 0.5f)
            {
                // Normal case: camera looks from player direction toward target
                lookDir = toTarget.normalized;
            }
            else
            {
                // Fallback for very close targets - use the stored rotation
                lookDir = s_cameraRot * Vector3.forward;
            }
            
            // Safety: ensure we have a valid look direction
            if (lookDir.sqrMagnitude < 0.1f) lookDir = Vector3.forward;
            
            Vector3 viewRight = Vector3.Cross(Vector3.up, lookDir).normalized;
            if (viewRight.sqrMagnitude < 0.1f) viewRight = Vector3.right; // Fallback for straight up/down
            
            // Calculate camera offset based on dimension settings
            Vector3 cameraOffset = GetCameraOffset(lookDir, viewRight);
            Vector3 finalCamPos = currentPos + cameraOffset;

            // Note: Ground level clamping removed to support caves and basements
            // The camera will now follow the target regardless of terrain height

            // --- Fix Super High Camera Bug ---
            // If the camera is insanely high relative to the target, clamp it.
            // This happens if the "Fixed" position was calculated weirdly or if offsets are huge.
            // Let's clamp to max 10m above target.
            float targetY = s_lastTargetPos.y;
            if (finalCamPos.y > targetY + 15f)
            {
                finalCamPos.y = targetY + 15f;
            }

            finalCamPos = AdjustProjectileCameraForCollision(currentPos, finalCamPos, target);

            // Calculate rotation to look at the target from camera position
            Vector3 lookAtDir = currentPos - finalCamPos;
            Quaternion finalCamRot;
            if (lookAtDir.sqrMagnitude > 0.01f)
            {
                // Base rotation: look at target
                finalCamRot = Quaternion.LookRotation(lookAtDir.normalized, Vector3.up);
                
                // Apply rotation settings (Pitch, Yaw, Tilt) as offsets
                var cam = CKSettings.ProjectileCamera;
                Quaternion rotationOffset = Quaternion.Euler(cam.Pitch, cam.Yaw, cam.Tilt);
                finalCamRot = finalCamRot * rotationOffset;
            }
            else
            {
                finalCamRot = s_cameraRot; // Fallback
            }

            Camera.main.transform.position = finalCamPos;
            Camera.main.transform.rotation = finalCamRot;
            
            // CRITICAL: Continuously enforce player rotation to prevent model from facing camera
            // The game's third-person mode tries to rotate the player to face the camera direction
            var player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player != null && s_savedPlayerRotation != Quaternion.identity)
            {
                player.transform.rotation = s_savedPlayerRotation;
            }
            
            // CRITICAL: Re-apply FOV from controller to override game's native FOV management
            // The game's EntityPlayerLocal.LateUpdate continuously sets playerCamera.fieldOfView,
            // overriding whatever CinematicFOVController sets. We run after that, so re-apply here.
            if (s_fovController != null && s_fovController.IsActive)
            {
                s_fovController.ApplyCurrentFOV();
            }
        }
        
        /// <summary>
        /// Calculates camera offset based on current dimension settings
        /// </summary>
        private static Vector3 GetCameraOffset(Vector3 lookDir, Vector3 right)
        {
            var cam = CKSettings.ProjectileCamera;
            
            // Distance: negative value means behind target, positive means in front
            // Height: vertical offset from target
            // XOffset: horizontal offset (negative = left, positive = right)
            return (-lookDir * cam.Distance) + (Vector3.up * cam.Height) + (right * cam.XOffset);
        }

        private const float ProjectileCameraCollisionRadius = 0.2f;
        private const float ProjectileCameraCollisionBuffer = 0.1f;
        private const float ProjectileCameraCollisionStartOffset = 0.1f;
        private const float ProjectileCameraMinHeightAboveGround = 0.5f;

        private static Vector3 AdjustProjectileCameraForCollision(Vector3 targetPos, Vector3 desiredPos, Transform target)
        {
            // Note: Removed per-frame verbose log here - was too spammy
            
            Vector3 toCamera = desiredPos - targetPos;
            float distance = toCamera.magnitude;
            if (distance <= ProjectileCameraCollisionStartOffset)
            {
                return desiredPos;
            }

            Vector3 direction = toCamera / distance;
            Vector3 adjustedPos = desiredPos;
            
            // ═══════════════════════════════════════════════════════════════
            // BLOCK-BASED COLLISION DETECTION (7DTD voxel world)
            // Check if camera ends up inside a wall/floor/ceiling
            // ═══════════════════════════════════════════════════════════════
            var world = GameManager.Instance?.World;
            bool blockCollisionFound = false;
            
            if (world != null)
            {
                // First, check if the desired camera position is INSIDE a solid block
                Vector3i cameraBlockPos = new Vector3i(desiredPos);
                try
                {
                    BlockValue cameraBlock = world.GetBlock(cameraBlockPos);
                    if (!cameraBlock.isair && !cameraBlock.isWater)
                    {
                        // Camera is inside a solid block! Pull it back toward target
                        CKLog.Verbose($"Camera inside block at {cameraBlockPos}, pulling back to target");
                        adjustedPos = targetPos + direction * 1.5f; // Just 1.5m from target
                        blockCollisionFound = true;
                    }
                }
                catch { }
                
                // Second, ensure camera Y is above the floor the target is standing on
                if (!blockCollisionFound)
                {
                    // Get the floor level at target position
                    float targetFloorY = targetPos.y;
                    for (int yCheck = 0; yCheck < 5; yCheck++)
                    {
                        Vector3i floorCheck = new Vector3i((int)targetPos.x, (int)targetPos.y - yCheck, (int)targetPos.z);
                        try
                        {
                            BlockValue floorBlock = world.GetBlock(floorCheck);
                            if (!floorBlock.isair && !floorBlock.isWater)
                            {
                                targetFloorY = floorCheck.y + 1; // Floor top is block Y + 1
                                break;
                            }
                        }
                        catch { break; }
                    }
                    
                    // Camera must be at least at floor level + 0.5m
                    float minCameraY = targetFloorY + ProjectileCameraMinHeightAboveGround;
                    if (adjustedPos.y < minCameraY)
                    {
                        adjustedPos.y = minCameraY;
                    }
                    
                    // Get ceiling level at target position
                    float targetCeilingY = targetPos.y + 20; // Default high ceiling
                    for (int yCheck = 1; yCheck <= 10; yCheck++)
                    {
                        Vector3i ceilingCheck = new Vector3i((int)targetPos.x, (int)targetPos.y + yCheck, (int)targetPos.z);
                        try
                        {
                            BlockValue ceilingBlock = world.GetBlock(ceilingCheck);
                            if (!ceilingBlock.isair && !ceilingBlock.isWater)
                            {
                                targetCeilingY = ceilingCheck.y - 0.5f; // Stay below ceiling
                                break;
                            }
                        }
                        catch { break; }
                    }
                    
                    // Camera must be below ceiling
                    if (adjustedPos.y > targetCeilingY)
                    {
                        adjustedPos.y = targetCeilingY;
                        CKLog.Verbose($"Camera above ceiling, clamped to Y={targetCeilingY:F1}");
                    }
                }
                
                // Third, raycast from target to desired camera to find walls
                if (!blockCollisionFound)
                {
                    float stepSize = 0.3f; // Check every 0.3 meters for better accuracy
                    float wallBuffer = 1.0f; // Stop 1 meter before wall
                    int maxSteps = (int)(distance / stepSize) + 1;
                    Vector3 lastValidPos = targetPos + direction * ProjectileCameraCollisionStartOffset;
                    
                    for (int step = 1; step <= maxSteps; step++)
                    {
                        float checkDist = Mathf.Min(ProjectileCameraCollisionStartOffset + step * stepSize, distance);
                        Vector3 checkPos = targetPos + direction * checkDist;
                        Vector3i blockPos = new Vector3i(checkPos);
                        
                        try
                        {
                            BlockValue block = world.GetBlock(blockPos);
                            
                            // Check if this block is solid (not air, not water)
                            if (!block.isair && !block.isWater)
                            {
                                // Found a solid block - position camera with buffer before it
                                float stopDist = Mathf.Max(ProjectileCameraCollisionStartOffset, checkDist - wallBuffer);
                                adjustedPos = targetPos + direction * stopDist;
                                blockCollisionFound = true;
                                CKLog.Verbose($"Block collision at {blockPos}, camera adjusted from dist {distance:F1} to {stopDist:F1}");
                                break;
                            }
                            
                            lastValidPos = checkPos;
                        }
                        catch
                        {
                            // Block access failed (out of bounds), use last valid position
                            adjustedPos = lastValidPos;
                            blockCollisionFound = true;
                            break;
                        }
                    }
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // PHYSICS-BASED COLLISION (Unity colliders - terrain, entities)
            // ═══════════════════════════════════════════════════════════════
            float castDistance = distance - ProjectileCameraCollisionStartOffset;
            Vector3 rayStart = targetPos + direction * ProjectileCameraCollisionStartOffset;

            RaycastHit[] hits = Physics.SphereCastAll(rayStart, ProjectileCameraCollisionRadius, direction, castDistance);
            
            if (hits != null && hits.Length > 0)
            {
                bool found = false;
                float bestDistance = float.MaxValue;
                RaycastHit bestHit = default;

                for (int i = 0; i < hits.Length; i++)
                {
                    var hit = hits[i];
                    if (hit.collider == null)
                    {
                        continue;
                    }

                    if (target != null && hit.collider.transform.IsChildOf(target))
                    {
                        continue;
                    }

                    if (hit.distance < bestDistance)
                    {
                        bestDistance = hit.distance;
                        bestHit = hit;
                        found = true;
                    }
                }

                if (found)
                {
                    // If physics collision is closer than block collision, use it
                    Vector3 physicsPos = bestHit.point - direction * ProjectileCameraCollisionBuffer;
                    float physicsDistFromTarget = (physicsPos - targetPos).magnitude;
                    float blockDistFromTarget = (adjustedPos - targetPos).magnitude;
                    
                    if (physicsDistFromTarget < blockDistFromTarget)
                    {
                        adjustedPos = physicsPos;
                    }
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // GROUND HEIGHT CHECK - DISABLED FOR INDOOR SUPPORT
            // The block-based floor/ceiling detection above handles indoor constraints.
            // The raycast below hits terrain BELOW buildings, causing camera to be
            // pushed UP through ceilings on upper floors.
            // ═══════════════════════════════════════════════════════════════

            return adjustedPos;
        }

        private static void ResetTimeScale()
        {
            s_isActive = false;
            s_isHitstopActive = false;
            s_isRestoringTime = false;
            
            CKLog.Verbose($" ResetTimeScale called. Active: {s_isProjectileCameraActive}");

            RestoreCameraState();

            s_timer = 0f;
            // Fallback restore if no timed restore was kicked off
            float fallbackRestore = Mathf.Max(0.01f, s_returnDuration);
            SmoothRestoreTimeScale(fallbackRestore);
            CKLog.Verbose(" Disabled");
        }

        private static void ApplyProjectileCameraViewRestore(EntityPlayerLocal player, bool targetFirstPerson)
        {
            if (player == null) return;

            // Quick weapon slot switch to refresh hand renderer, then return to target view
            try
            {
                int currentSlot = player.inventory.holdingItemIdx;
                int switchToSlot = (currentSlot == 0) ? 1 : 0;
                
                // Quick switch to another slot and back - refreshes hand model
                player.inventory.SetHoldingItemIdxNoHolsterTime(switchToSlot);
                player.inventory.SetHoldingItemIdxNoHolsterTime(currentSlot);
                
                // Set the target view
                player.SetFirstPersonView(targetFirstPerson, true);

                if (player.vp_FPCamera != null)
                {
                    player.vp_FPCamera.m_Current3rdPersonBlend = targetFirstPerson ? 0f : 1f;
                }

                CKLog.Verbose("Applied weapon slot switch on restore for hands visibility");
            }
            catch (Exception ex)
            {
                Log.Warning($"CinematicKill: Error restoring view: {ex.Message}");
            }
        }

        private static System.Collections.IEnumerator RestoreViewForHandsFix(EntityPlayerLocal player)
        {
            if (player == null) yield break;

            // Restore Camera Position immediately to prevent jump
            if (Camera.main != null)
            {
                Camera.main.transform.position = s_originalCameraPos;
                Camera.main.transform.rotation = s_originalCameraRot;
            }

            // Restore to the user's original view state
            bool targetFirstPerson = s_wasFirstPersonView;
            
            // Fix invisible hands: toolbelt switch + view toggle to refresh hand renderer
            try
            {
                // STEP 1: Toolbelt slot switch - this forces the hand model to refresh
                int currentSlot = player.inventory.holdingItemIdx;
                int switchToSlot = (currentSlot == 0) ? 1 : 0;
                player.inventory.SetHoldingItemIdxNoHolsterTime(switchToSlot);
                player.inventory.SetHoldingItemIdxNoHolsterTime(currentSlot);
                
                // STEP 2: View toggle - additional refresh for hand renderer
                player.SetFirstPersonView(false, false);  // Brief 3rd person
                player.SetFirstPersonView(true, false);   // Back to 1st person (refreshes hands)
                
                // STEP 3: Set the actual target view
                player.SetFirstPersonView(targetFirstPerson, true);
                
                if (player.vp_FPCamera != null)
                {
                    player.vp_FPCamera.m_Current3rdPersonBlend = targetFirstPerson ? 0f : 1f;
                }
                
                CKLog.Verbose("Applied toolbelt + view toggle fix for hands visibility on restore");
            }
            catch (Exception ex)
            {
                Log.Warning($"CinematicKill: Error in view restore: {ex.Message}");
            }
            
            yield return new WaitForSecondsRealtime(0.05f);
            
            // Final camera position check
            if (Camera.main != null)
            {
                Camera.main.transform.position = s_originalCameraPos;
                Camera.main.transform.rotation = s_originalCameraRot;
            }
        }

        private static void LoadConfig()
        {
            s_enabled = true;
            s_slowScale = 0.2f;
            s_killcamChance = 1f;
            s_cinematicSettings = CinematicKillSettings.Default;
            s_ignoreCorpseHits = true;

            if (string.IsNullOrEmpty(s_configPath) || !File.Exists(s_configPath))
            {
                s_cinematicSettings.Clamp();
                // Refresh menu with defaults if no config file
                if (s_menuComponent != null)
                {
                    s_menuComponent.RefreshSettings();
                }
                return;
            }

            try
            {
                var document = XDocument.Load(s_configPath);
                var root = document.Root;
                if (root == null)
                {
                    return;
                }

                foreach (var property in root.Elements("Property"))
                {
                    var name = property.Attribute("name")?.Value;
                    var value = property.Attribute("value")?.Value;
                    if (string.IsNullOrEmpty(name) || value == null)
                    {
                        continue;
                    }

                    switch (name)
                    {
                        case "Enabled":
                            s_enabled = ParseBool(value, true);
                            s_cinematicSettings.EnableCinematics = s_enabled;
                            break;
                        case "EnableCinematics":
                            s_cinematicSettings.EnableCinematics = ParseBool(value, true);
                            s_enabled = s_cinematicSettings.EnableCinematics;
                            break;
                        case "EnableTriggers":
                            s_cinematicSettings.EnableTriggers = ParseBool(value, true);
                            if (s_cinematicSettings.MenuV2?.TriggerSystem != null)
                                s_cinematicSettings.MenuV2.TriggerSystem.EnableTriggers = s_cinematicSettings.EnableTriggers;
                            break;
                        case "AdvancedMode":
                            s_cinematicSettings.AdvancedMode = ParseBool(value, false);
                            break;
                        case "EnableVerboseLogging":
                            s_cinematicSettings.EnableVerboseLogging = ParseBool(value, false);
                            break;
                            
                        // ═══════════════════════════════════════════════════════════════════════
                        // NEW SIMPLIFIED CONFIG PROPERTIES - Maps to MenuV2 for runtime
                        // ═══════════════════════════════════════════════════════════════════════
                        
                        // Basic Kill Settings
                        case "BasicKillEnabled":
                            s_cinematicSettings.EnableCinematics = ParseBool(value, true);
                            s_enabled = s_cinematicSettings.EnableCinematics;
                            if (s_cinematicSettings.MenuV2?.Core != null)
                                s_cinematicSettings.MenuV2.Core.Enabled = s_enabled;
                            break;
                        case "BasicKillChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkChance))
                            {
                                float clampedChance = Mathf.Clamp(bkChance, 0f, 100f);
                                s_killcamChance = clampedChance / 100f;
                                s_cinematicSettings.BasicKill.Chance = clampedChance;
                                if (s_cinematicSettings.MenuV2?.Core != null)
                                    s_cinematicSettings.MenuV2.Core.GlobalTriggerChance = clampedChance;
                            }
                            break;
                        case "BasicKillDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkDur))
                            {
                                s_duration = Mathf.Clamp(bkDur, 0.1f, 10f);
                                s_cinematicSettings.BasicKill.Duration = s_duration;
                                if (s_cinematicSettings.MenuV2?.Core != null)
                                    s_cinematicSettings.MenuV2.Core.SlowMoDuration = s_duration;
                            }
                            break;
                        case "BasicKillTimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkScale))
                            {
                                s_slowScale = Mathf.Clamp(bkScale, MinScale, MaxScale);
                                s_cinematicSettings.BasicKill.TimeScale = s_slowScale;
                                if (s_cinematicSettings.MenuV2?.Core != null)
                                    s_cinematicSettings.MenuV2.Core.GlobalTimeScale = s_slowScale;
                            }
                            break;
                        case "BasicKillCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkCool))
                            {
                                float clampedCooldown = Mathf.Clamp(bkCool, 0f, 60f);
                                s_cinematicSettings.BasicKill.Cooldown = clampedCooldown;
                            }
                            break;
                        case "BasicKillFirstPersonCamera":
                            {
                                bool bkFP = ParseBool(value, true);
                                s_cinematicSettings.BasicKill.FirstPersonCamera = bkFP;
                                if (s_cinematicSettings.MenuV2?.Core != null)
                                    s_cinematicSettings.MenuV2.Core.EnableFirstPersonCamera = bkFP;
                            }
                            break;
                        case "BasicKillProjectileCamera":
                            {
                                bool bkProj = ParseBool(value, true);
                                s_cinematicSettings.BasicKill.ProjectileCamera = bkProj;
                                if (s_cinematicSettings.MenuV2?.Core != null)
                                    s_cinematicSettings.MenuV2.Core.EnableProjectileCamera = bkProj;
                            }
                            break;
                        case "BasicKillFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkFPChance))
                            {
                                float clampedFPChance = Mathf.Clamp(bkFPChance, 0f, 100f);
                                s_cinematicSettings.BasicKill.FirstPersonChance = clampedFPChance;
                                if (s_cinematicSettings.MenuV2?.Core != null)
                                    s_cinematicSettings.MenuV2.Core.FirstPersonCameraChance = clampedFPChance;
                            }
                            break;
                        case "BasicKillFOVEnabled":
                            {
                                bool bkFovEnabled = ParseBool(value, true);
                                s_cinematicSettings.BasicKill.FOVEnabled = bkFovEnabled;
                                if (s_cinematicSettings.MenuV2?.GlobalVisuals != null)
                                    s_cinematicSettings.MenuV2.GlobalVisuals.EnableFOVZoom = bkFovEnabled;
                            }
                            break;
                        case "BasicKillFOVMultiplier":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkFOV))
                            {
                                float clampedFOV = Mathf.Clamp(bkFOV, 0.3f, 1.5f);
                                s_cinematicSettings.BasicKill.FOVMultiplier = clampedFOV;
                                CKSettings.FOVZoomAmount = clampedFOV;
                                if (s_cinematicSettings.MenuV2?.GlobalVisuals != null)
                                    s_cinematicSettings.MenuV2.GlobalVisuals.FOVZoomMultiplier = clampedFOV;
                            }
                            break;
                            
                        // Trigger Default Settings
                        case "TriggerDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tDur))
                            {
                                float clampedTDur = Mathf.Clamp(tDur, 0.1f, 10f);
                                s_cinematicSettings.TriggerDefaults.Duration = clampedTDur;
                            }
                            break;
                        case "TriggerTimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tScale))
                            {
                                float clampedTScale = Mathf.Clamp(tScale, MinScale, MaxScale);
                                s_cinematicSettings.TriggerDefaults.TimeScale = clampedTScale;
                            }
                            break;
                        case "TriggerFirstPersonCamera":
                            {
                                bool tFP = ParseBool(value, true);
                                s_cinematicSettings.TriggerDefaults.FirstPersonCamera = tFP;
                            }
                            break;
                        case "TriggerProjectileCamera":
                            {
                                bool tProj = ParseBool(value, true);
                                s_cinematicSettings.TriggerDefaults.ProjectileCamera = tProj;
                            }
                            break;
                        case "TriggerFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tFPChance))
                            {
                                float clampedTFPChance = Mathf.Clamp(tFPChance, 0f, 100f);
                                s_cinematicSettings.TriggerDefaults.FirstPersonChance = clampedTFPChance;
                            }
                            break;
                        case "TriggerFOVEnabled":
                            {
                                bool tFovEnabled = ParseBool(value, true);
                                s_cinematicSettings.TriggerDefaults.FOVEnabled = tFovEnabled;
                            }
                            break;
                        case "TriggerFOVMultiplier":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tFovMult))
                            {
                                s_cinematicSettings.TriggerDefaults.FOVMultiplier = Mathf.Clamp(tFovMult, 0.3f, 1.5f);
                            }
                            break;
                        
                        // ═══════════════════════════════════════════════════════════════
                        // Ragdoll Floor Hit Settings - Per camera type
                        // ═══════════════════════════════════════════════════════════════
                        case "EnableDynamicRagdollDuration_BK_FP":
                            s_cinematicSettings.EnableDynamicRagdollDuration_BK_FP = ParseBool(value, false);
                            break;
                        case "EnableDynamicRagdollDuration_BK_Proj":
                            s_cinematicSettings.EnableDynamicRagdollDuration_BK_Proj = ParseBool(value, false);
                            break;
                        case "EnableDynamicRagdollDuration_TD_FP":
                            s_cinematicSettings.EnableDynamicRagdollDuration_TD_FP = ParseBool(value, false);
                            break;
                        case "EnableDynamicRagdollDuration_TD_Proj":
                            s_cinematicSettings.EnableDynamicRagdollDuration_TD_Proj = ParseBool(value, false);
                            break;
                        case "RagdollPostLandDelay_BK_FP":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ragBkFp))
                                s_cinematicSettings.RagdollPostLandDelay_BK_FP = Mathf.Clamp(ragBkFp, 0f, 3f);
                            break;
                        case "RagdollPostLandDelay_BK_Proj":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ragBkProj))
                                s_cinematicSettings.RagdollPostLandDelay_BK_Proj = Mathf.Clamp(ragBkProj, 0f, 3f);
                            break;
                        case "RagdollPostLandDelay_TD_FP":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ragTdFp))
                                s_cinematicSettings.RagdollPostLandDelay_TD_FP = Mathf.Clamp(ragTdFp, 0f, 3f);
                            break;
                        case "RagdollPostLandDelay_TD_Proj":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ragTdProj))
                                s_cinematicSettings.RagdollPostLandDelay_TD_Proj = Mathf.Clamp(ragTdProj, 0f, 3f);
                            break;
                        case "RagdollFallbackDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ragFallback))
                                s_cinematicSettings.RagdollFallbackDuration = Mathf.Clamp(ragFallback, 1f, 30f);
                            break;
                            
                        // Trigger Enable Toggles - Set directly on simple struct
                        case "HeadshotEnabled":
                            s_cinematicSettings.Headshot.Enabled = ParseBool(value, false);
                            break;
                        case "CriticalEnabled":
                            s_cinematicSettings.Critical.Enabled = ParseBool(value, false);
                            break;
                        case "LastEnemyEnabled":
                            s_cinematicSettings.LastEnemy.Enabled = ParseBool(value, false);
                            break;
                        case "LongRangeEnabled":
                            s_cinematicSettings.LongRange.Enabled = ParseBool(value, false);
                            break;
                        case "LowHealthEnabled":
                            s_cinematicSettings.LowHealth.Enabled = ParseBool(value, false);
                            break;
                        case "DismemberEnabled":
                            s_cinematicSettings.Dismember.Enabled = ParseBool(value, false);
                            break;
                        case "KillstreakEnabled":
                            s_cinematicSettings.Killstreak.Enabled = ParseBool(value, false);
                            break;
                        case "SneakEnabled":
                            s_cinematicSettings.Sneak.Enabled = ParseBool(value, false);
                            break;
                            
                        // Trigger Thresholds
                        case "LongRangeDistance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrDist))
                            {
                                s_cinematicSettings.LongRangeDistance = Mathf.Clamp(lrDist, 5f, 100f);
                            }
                            break;
                        case "LowHealthPercent":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhPct))
                            {
                                s_cinematicSettings.LowHealthPercent = Mathf.Clamp(lhPct, 5f, 50f);
                            }
                            break;
                        case "EnemyScanRadius":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var scanRad))
                            {
                                s_cinematicSettings.EnemyScanRadius = Mathf.Clamp(scanRad, 5f, 100f);
                                if (s_cinematicSettings.MenuV2?.Core != null)
                                    s_cinematicSettings.MenuV2.Core.EnemyScanRadius = s_cinematicSettings.EnemyScanRadius;
                            }
                            break;
                            
                        // ═══════════════════════════════════════════════════════════════════════
                        // TRIGGER OVERRIDES - Per-trigger settings on simple struct
                        // ═══════════════════════════════════════════════════════════════════════
                        
                        // Headshot Override
                        case "HeadshotOverride":
                            s_cinematicSettings.Headshot.Override = ParseBool(value, false);
                            break;
                        case "HeadshotDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hsDur))
                                s_cinematicSettings.Headshot.Duration = Mathf.Clamp(hsDur, 0.1f, 10f);
                            break;
                        case "HeadshotTimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hsScale))
                                s_cinematicSettings.Headshot.TimeScale = Mathf.Clamp(hsScale, MinScale, MaxScale);
                            break;
                        case "HeadshotCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hsCool))
                                s_cinematicSettings.Headshot.Cooldown = Mathf.Clamp(hsCool, 0f, 60f);
                            break;
                        case "HeadshotFirstPersonCamera":
                            s_cinematicSettings.Headshot.FirstPersonCamera = ParseBool(value, true);
                            break;
                        case "HeadshotProjectileCamera":
                            s_cinematicSettings.Headshot.ProjectileCamera = ParseBool(value, true);
                            break;
                        case "HeadshotFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hsFPChance))
                                s_cinematicSettings.Headshot.FirstPersonChance = Mathf.Clamp(hsFPChance, 0f, 100f);
                            break;
                            
                        // Critical Override
                        case "CriticalOverride":
                            s_cinematicSettings.Critical.Override = ParseBool(value, false);
                            break;
                        case "CriticalDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var critDur))
                                s_cinematicSettings.Critical.Duration = Mathf.Clamp(critDur, 0.1f, 10f);
                            break;
                        case "CriticalTimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var critScale))
                                s_cinematicSettings.Critical.TimeScale = Mathf.Clamp(critScale, MinScale, MaxScale);
                            break;
                        case "CriticalCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var critCool))
                                s_cinematicSettings.Critical.Cooldown = Mathf.Clamp(critCool, 0f, 60f);
                            break;
                        case "CriticalFirstPersonCamera":
                            s_cinematicSettings.Critical.FirstPersonCamera = ParseBool(value, true);
                            break;
                        case "CriticalProjectileCamera":
                            s_cinematicSettings.Critical.ProjectileCamera = ParseBool(value, true);
                            break;
                        case "CriticalFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var critFPChance))
                                s_cinematicSettings.Critical.FirstPersonChance = Mathf.Clamp(critFPChance, 0f, 100f);
                            break;
                            
                        // LastEnemy Override  
                        case "LastEnemyOverride":
                            s_cinematicSettings.LastEnemy.Override = ParseBool(value, false);
                            break;
                        case "LastEnemyDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leDur))
                                s_cinematicSettings.LastEnemy.Duration = Mathf.Clamp(leDur, 0.1f, 10f);
                            break;
                        case "LastEnemyTimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leScale))
                                s_cinematicSettings.LastEnemy.TimeScale = Mathf.Clamp(leScale, MinScale, MaxScale);
                            break;
                        case "LastEnemyCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leCool))
                                s_cinematicSettings.LastEnemy.Cooldown = Mathf.Clamp(leCool, 0f, 60f);
                            break;
                        case "LastEnemyFirstPersonCamera":
                            s_cinematicSettings.LastEnemy.FirstPersonCamera = ParseBool(value, true);
                            break;
                        case "LastEnemyProjectileCamera":
                            s_cinematicSettings.LastEnemy.ProjectileCamera = ParseBool(value, true);
                            break;
                        case "LastEnemyFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leFPChance))
                                s_cinematicSettings.LastEnemy.FirstPersonChance = Mathf.Clamp(leFPChance, 0f, 100f);
                            break;
                            
                        // LongRange Override
                        case "LongRangeOverride":
                            s_cinematicSettings.LongRange.Override = ParseBool(value, false);
                            break;
                        case "LongRangeDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrDur))
                                s_cinematicSettings.LongRange.Duration = Mathf.Clamp(lrDur, 0.1f, 10f);
                            break;
                        case "LongRangeTimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrScale))
                                s_cinematicSettings.LongRange.TimeScale = Mathf.Clamp(lrScale, MinScale, MaxScale);
                            break;
                        case "LongRangeCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrCool))
                                s_cinematicSettings.LongRange.Cooldown = Mathf.Clamp(lrCool, 0f, 60f);
                            break;
                        case "LongRangeFirstPersonCamera":
                            s_cinematicSettings.LongRange.FirstPersonCamera = ParseBool(value, true);
                            break;
                        case "LongRangeProjectileCamera":
                            s_cinematicSettings.LongRange.ProjectileCamera = ParseBool(value, true);
                            break;
                        case "LongRangeFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrFPChance))
                                s_cinematicSettings.LongRange.FirstPersonChance = Mathf.Clamp(lrFPChance, 0f, 100f);
                            break;
                            
                        // LowHealth Override
                        case "LowHealthOverride":
                            s_cinematicSettings.LowHealth.Override = ParseBool(value, false);
                            break;
                        case "LowHealthDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhDur))
                                s_cinematicSettings.LowHealth.Duration = Mathf.Clamp(lhDur, 0.1f, 10f);
                            break;
                        case "LowHealthTimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhScale))
                                s_cinematicSettings.LowHealth.TimeScale = Mathf.Clamp(lhScale, MinScale, MaxScale);
                            break;
                        case "LowHealthCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhCool))
                                s_cinematicSettings.LowHealth.Cooldown = Mathf.Clamp(lhCool, 0f, 60f);
                            break;
                        case "LowHealthFirstPersonCamera":
                            s_cinematicSettings.LowHealth.FirstPersonCamera = ParseBool(value, true);
                            break;
                        case "LowHealthProjectileCamera":
                            s_cinematicSettings.LowHealth.ProjectileCamera = ParseBool(value, true);
                            break;
                        case "LowHealthFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhFPChance))
                                s_cinematicSettings.LowHealth.FirstPersonChance = Mathf.Clamp(lhFPChance, 0f, 100f);
                            break;
                            
                        // Dismember Override
                        case "DismemberOverride":
                            s_cinematicSettings.Dismember.Override = ParseBool(value, false);
                            break;
                        case "DismemberDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dmDur))
                                s_cinematicSettings.Dismember.Duration = Mathf.Clamp(dmDur, 0.1f, 10f);
                            break;
                        case "DismemberTimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dmScale))
                                s_cinematicSettings.Dismember.TimeScale = Mathf.Clamp(dmScale, MinScale, MaxScale);
                            break;
                        case "DismemberCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dmCool))
                                s_cinematicSettings.Dismember.Cooldown = Mathf.Clamp(dmCool, 0f, 60f);
                            break;
                        case "DismemberFirstPersonCamera":
                            s_cinematicSettings.Dismember.FirstPersonCamera = ParseBool(value, true);
                            break;
                        case "DismemberProjectileCamera":
                            s_cinematicSettings.Dismember.ProjectileCamera = ParseBool(value, true);
                            break;
                        case "DismemberFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dmFPChance))
                                s_cinematicSettings.Dismember.FirstPersonChance = Mathf.Clamp(dmFPChance, 0f, 100f);
                            break;
                            
                        // Killstreak Override
                        case "KillstreakOverride":
                            s_cinematicSettings.Killstreak.Override = ParseBool(value, false);
                            break;
                        case "KillstreakDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ksDur))
                                s_cinematicSettings.Killstreak.Duration = Mathf.Clamp(ksDur, 0.1f, 10f);
                            break;
                        case "KillstreakTimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ksScale))
                                s_cinematicSettings.Killstreak.TimeScale = Mathf.Clamp(ksScale, MinScale, MaxScale);
                            break;
                        case "KillstreakCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ksCool))
                                s_cinematicSettings.Killstreak.Cooldown = Mathf.Clamp(ksCool, 0f, 60f);
                            break;
                        case "KillstreakFirstPersonCamera":
                            s_cinematicSettings.Killstreak.FirstPersonCamera = ParseBool(value, true);
                            break;
                        case "KillstreakProjectileCamera":
                            s_cinematicSettings.Killstreak.ProjectileCamera = ParseBool(value, true);
                            break;
                        case "KillstreakFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ksFPChance))
                                s_cinematicSettings.Killstreak.FirstPersonChance = Mathf.Clamp(ksFPChance, 0f, 100f);
                            break;
                        case "KillstreakWindow":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ksWindow))
                            {
                                s_cinematicSettings.KillstreakWindow = Mathf.Clamp(ksWindow, 1f, 30f);
                            }
                            break;
                        case "KillstreakKillsRequired":
                            if (int.TryParse(value, out var ksKills))
                            {
                                s_cinematicSettings.KillstreakThreshold = Mathf.Clamp(ksKills, 2, 20);
                            }
                            break;
                            
                        // Sneak Override
                        case "SneakOverride":
                            s_cinematicSettings.Sneak.Override = ParseBool(value, false);
                            break;
                        case "SneakDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var snkDur))
                                s_cinematicSettings.Sneak.Duration = Mathf.Clamp(snkDur, 0.1f, 10f);
                            break;
                        case "SneakTimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var snkScale))
                                s_cinematicSettings.Sneak.TimeScale = Mathf.Clamp(snkScale, MinScale, MaxScale);
                            break;
                        case "SneakCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var snkCool))
                                s_cinematicSettings.Sneak.Cooldown = Mathf.Clamp(snkCool, 0f, 60f);
                            break;
                        case "SneakFirstPersonCamera":
                            s_cinematicSettings.Sneak.FirstPersonCamera = ParseBool(value, true);
                            break;
                        case "SneakProjectileCamera":
                            s_cinematicSettings.Sneak.ProjectileCamera = ParseBool(value, true);
                            break;
                        case "SneakFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var snkFPChance))
                                s_cinematicSettings.Sneak.FirstPersonChance = Mathf.Clamp(snkFPChance, 0f, 100f);
                            break;
                            
                        // Projectile Camera Settings
                        case "ProjectileFollowDistance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projDist))
                                s_cinematicSettings.ProjectileCamera.Distance = Mathf.Clamp(projDist, 0.5f, 10f);
                            break;
                        case "ProjectileFollowHeight":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projHeight))
                                s_cinematicSettings.ProjectileCamera.Height = Mathf.Clamp(projHeight, -5f, 5f);
                            break;
                        case "ProjectileCameraTilt":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projTilt))
                                s_cinematicSettings.ProjectileCamera.Tilt = Mathf.Clamp(projTilt, -45f, 45f);
                            break;
                            
                        // Screen Effects
                        case "EnableScreenEffects":
                            s_cinematicSettings.ScreenEffects.Enabled = ParseBool(value, true);
                            break;
                        case "EnableVignette":
                            s_cinematicSettings.ScreenEffects.EnableVignette = ParseBool(value, true);
                            break;
                        case "EnableVignetteFX":
                            s_cinematicSettings.ScreenEffects.EnableVignette = ParseBool(value, true);
                            break;
                        case "VignetteIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var vigInt))
                                s_cinematicSettings.ScreenEffects.VignetteIntensity = Mathf.Clamp(vigInt, 0f, 1f);
                            break;
                        case "VignetteIntensityFX":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var vigIntFx))
                                s_cinematicSettings.ScreenEffects.VignetteIntensity = Mathf.Clamp(vigIntFx, 0f, 1f);
                            break;
                        case "EnableDesaturation":
                            s_cinematicSettings.ScreenEffects.EnableDesaturation = ParseBool(value, true);
                            break;
                        case "DesaturationAmount":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var desatAmt))
                                s_cinematicSettings.ScreenEffects.DesaturationAmount = Mathf.Clamp(desatAmt, 0f, 1f);
                            break;
                        case "DesaturationAmountFX":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var desatAmtFx))
                                s_cinematicSettings.ScreenEffects.DesaturationAmount = Mathf.Clamp(desatAmtFx, 0f, 1f);
                            break;
                        case "EnableBloodSplatter":
                            s_cinematicSettings.ScreenEffects.EnableBloodSplatter = ParseBool(value, true);
                            break;
                        case "BloodSplatterIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bloodInt))
                                s_cinematicSettings.ScreenEffects.BloodSplatterIntensity = Mathf.Clamp(bloodInt, 0f, 3f);
                            break;
                        case "BloodSplatterIntensityFX":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bloodIntFx))
                                s_cinematicSettings.ScreenEffects.BloodSplatterIntensity = Mathf.Clamp(bloodIntFx, 0f, 3f);
                            break;
                        case "EnableHitstop":
                            s_cinematicSettings.ScreenEffects.EnableHitstop = ParseBool(value, true);
                            break;
                        case "EnableHitstopFX":
                            s_cinematicSettings.ScreenEffects.EnableHitstop = ParseBool(value, true);
                            break;
                        case "HitstopDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hitDur))
                                s_cinematicSettings.ScreenEffects.HitstopDuration = Mathf.Clamp(hitDur, 0f, 0.5f);
                            break;
                        case "HitstopDurationFX":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hitDurFx))
                                s_cinematicSettings.ScreenEffects.HitstopDuration = Mathf.Clamp(hitDurFx, 0f, 0.5f);
                            break;
                        
                        // Screen Effects Randomization
                        case "RandomizeVignetteIntensity_FX":
                            s_cinematicSettings.ScreenEffects.RandomizeVignetteIntensity = ParseBool(value, false);
                            break;
                        case "VignetteIntensityMin_FX":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var vigMinFx))
                                s_cinematicSettings.ScreenEffects.VignetteIntensityMin = Mathf.Clamp(vigMinFx, 0f, 1f);
                            break;
                        case "VignetteIntensityMax_FX":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var vigMaxFx))
                                s_cinematicSettings.ScreenEffects.VignetteIntensityMax = Mathf.Clamp(vigMaxFx, 0f, 1f);
                            break;
                        case "RandomizeDesaturationAmount_FX":
                            s_cinematicSettings.ScreenEffects.RandomizeDesaturationAmount = ParseBool(value, false);
                            break;
                        case "DesaturationAmountMin_FX":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var desMinFx))
                                s_cinematicSettings.ScreenEffects.DesaturationAmountMin = Mathf.Clamp(desMinFx, 0f, 1f);
                            break;
                        case "DesaturationAmountMax_FX":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var desMaxFx))
                                s_cinematicSettings.ScreenEffects.DesaturationAmountMax = Mathf.Clamp(desMaxFx, 0f, 1f);
                            break;
                        case "RandomizeBloodSplatterIntensity_FX":
                            s_cinematicSettings.ScreenEffects.RandomizeBloodSplatterIntensity = ParseBool(value, false);
                            break;
                        case "BloodSplatterIntensityMin_FX":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bloodMinFx))
                                s_cinematicSettings.ScreenEffects.BloodSplatterIntensityMin = Mathf.Clamp(bloodMinFx, 0.5f, 2f);
                            break;
                        case "BloodSplatterIntensityMax_FX":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bloodMaxFx))
                                s_cinematicSettings.ScreenEffects.BloodSplatterIntensityMax = Mathf.Clamp(bloodMaxFx, 0.5f, 2f);
                            break;
                        case "RandomizeHitstopDuration_FX":
                            s_cinematicSettings.ScreenEffects.RandomizeHitstopDuration = ParseBool(value, false);
                            break;
                        case "HitstopDurationMin_FX":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hitMinFx))
                                s_cinematicSettings.ScreenEffects.HitstopDurationMin = Mathf.Clamp(hitMinFx, 0.01f, 0.3f);
                            break;
                        case "HitstopDurationMax_FX":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hitMaxFx))
                                s_cinematicSettings.ScreenEffects.HitstopDurationMax = Mathf.Clamp(hitMaxFx, 0.01f, 0.3f);
                            break;
                        
                        // Projectile Camera (simplified - now uses Distance, Height, XOffset, Tilt directly)
                        case "Distance_PC":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var distPc))
                                s_cinematicSettings.ProjectileCamera.Distance = Mathf.Clamp(distPc, 0.5f, 8f);
                            break;
                        case "Height_PC":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var heightPc))
                                s_cinematicSettings.ProjectileCamera.Height = Mathf.Clamp(heightPc, -2f, 4f);
                            break;
                        case "XOffset_PC":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var xoffPc))
                                s_cinematicSettings.ProjectileCamera.XOffset = Mathf.Clamp(xoffPc, -4f, 4f);
                            break;
                        case "Pitch_PC":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pitchPc))
                                s_cinematicSettings.ProjectileCamera.Pitch = Mathf.Clamp(pitchPc, -45f, 45f);
                            break;
                        case "Yaw_PC":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var yawPc))
                                s_cinematicSettings.ProjectileCamera.Yaw = Mathf.Clamp(yawPc, -180f, 180f);
                            break;
                        case "Tilt_PC":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tiltPc))
                                s_cinematicSettings.ProjectileCamera.Tilt = Mathf.Clamp(tiltPc, -45f, 45f);
                            break;
                            
                        // Weapon Mode Toggles
                        case "MeleeEnabled":
                            s_cinematicSettings.MeleeEnabled = ParseBool(value, true);
                            break;
                        case "RangedEnabled":
                            s_cinematicSettings.RangedEnabled = ParseBool(value, true);
                            break;
                        case "BowEnabled":
                            s_cinematicSettings.BowEnabled = ParseBool(value, true);
                            break;
                        case "ExplosiveEnabled":
                            s_cinematicSettings.ExplosiveEnabled = ParseBool(value, true);
                            break;
                        case "TrapEnabled":
                            s_cinematicSettings.TrapEnabled = ParseBool(value, true);
                            break;
                        
                        // ═══════════════════════════════════════════════════════════════════════
                        // LEGACY PROPERTY NAMES - Continue to support old config format
                        // ═══════════════════════════════════════════════════════════════════════
                        case "EnableFirstPersonCamera":
                            s_cinematicSettings.EnableFirstPersonCamera = ParseBool(value, true);
                            break;
                        case "EnableFirstPersonSlowMo":
                            s_cinematicSettings.EnableFirstPersonSlowMo = ParseBool(value, true);
                            break;
                        case "SlowScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var slow))
                            {
                                s_slowScale = Mathf.Clamp(slow, MinScale, MaxScale);
                                s_cinematicSettings.FirstPersonSlowScale = s_slowScale;
                            }
                            break;
                        case "FirstPersonSlowScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpSlow))
                            {
                                s_cinematicSettings.FirstPersonSlowScale = Mathf.Clamp(fpSlow, MinScale, MaxScale);
                                s_slowScale = s_cinematicSettings.FirstPersonSlowScale;
                            }
                            break;
                        case "FirstPersonDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpDur))
                            {
                                s_cinematicSettings.FirstPersonDuration = Mathf.Clamp(fpDur, 0.1f, 10f);
                                s_duration = s_cinematicSettings.FirstPersonDuration;
                            }
                            break;
                        case "FirstPersonCameraChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpChance))
                            {
                                s_cinematicSettings.FirstPersonCameraChance = Mathf.Clamp01(fpChance);
                            }
                            break;
                        case "FirstPersonCameraChanceMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpChanceMin))
                            {
                                s_cinematicSettings.FirstPersonCameraChanceMin = Mathf.Clamp01(fpChanceMin);
                            }
                            break;
                        case "FirstPersonCameraChanceMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpChanceMax))
                            {
                                s_cinematicSettings.FirstPersonCameraChanceMax = Mathf.Clamp01(fpChanceMax);
                            }
                            break;
                        case "RandomizeFirstPersonCameraChance":
                            s_cinematicSettings.RandomizeFirstPersonCameraChance = ParseBool(value, false);
                            break;
                    case "AllowLastEnemyFirstPerson":
                        s_cinematicSettings.AllowLastEnemyFirstPerson = ParseBool(value, true);
                        break;
                        case "AllowLastEnemyProjectile":
                            s_cinematicSettings.AllowLastEnemyProjectile = ParseBool(value, true);
                            break;
                        case "OverrideLastEnemyFirstPerson":
                            s_cinematicSettings.OverrideLastEnemyFirstPerson = ParseBool(value, false);
                            break;
                        case "LastEnemyFirstPersonSlowScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leFpSlow))
                                s_cinematicSettings.LastEnemyFirstPersonSlowScale = leFpSlow;
                            break;
                        case "LastEnemyFirstPersonSlowScaleMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leFpSlowMin))
                                s_cinematicSettings.LastEnemyFirstPersonSlowScaleMin = leFpSlowMin;
                            break;
                        case "LastEnemyFirstPersonSlowScaleMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leFpSlowMax))
                                s_cinematicSettings.LastEnemyFirstPersonSlowScaleMax = leFpSlowMax;
                            break;
                        case "RandomizeLastEnemyFirstPersonSlowScale":
                            s_cinematicSettings.RandomizeLastEnemyFirstPersonSlowScale = ParseBool(value, false);
                            break;
                        case "LastEnemyFirstPersonDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leFpDur))
                                s_cinematicSettings.LastEnemyFirstPersonDuration = leFpDur;
                            break;
                        case "LastEnemyFirstPersonDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leFpDurMin))
                                s_cinematicSettings.LastEnemyFirstPersonDurationMin = leFpDurMin;
                            break;
                        case "LastEnemyFirstPersonDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leFpDurMax))
                                s_cinematicSettings.LastEnemyFirstPersonDurationMax = leFpDurMax;
                            break;
                        case "RandomizeLastEnemyFirstPersonDuration":
                            s_cinematicSettings.RandomizeLastEnemyFirstPersonDuration = ParseBool(value, false);
                            break;
                        case "OverrideLastEnemyProjectile":
                            s_cinematicSettings.OverrideLastEnemyProjectile = ParseBool(value, false);
                            break;
                        case "LastEnemyProjectileSlowScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leProjSlow))
                                s_cinematicSettings.LastEnemyProjectileSlowScale = leProjSlow;
                            break;
                        case "LastEnemyProjectileSlowScaleMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leProjSlowMin))
                                s_cinematicSettings.LastEnemyProjectileSlowScaleMin = leProjSlowMin;
                            break;
                        case "LastEnemyProjectileSlowScaleMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leProjSlowMax))
                                s_cinematicSettings.LastEnemyProjectileSlowScaleMax = leProjSlowMax;
                            break;
                        case "RandomizeLastEnemyProjectileSlowScale":
                            s_cinematicSettings.RandomizeLastEnemyProjectileSlowScale = ParseBool(value, false);
                            break;
                        case "LastEnemyProjectileDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leProjDur))
                                s_cinematicSettings.LastEnemyProjectileDuration = leProjDur;
                            break;
                        case "LastEnemyProjectileDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leProjDurMin))
                                s_cinematicSettings.LastEnemyProjectileDurationMin = leProjDurMin;
                            break;
                        case "LastEnemyProjectileDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leProjDurMax))
                                s_cinematicSettings.LastEnemyProjectileDurationMax = leProjDurMax;
                            break;
                        case "RandomizeLastEnemyProjectileDuration":
                            s_cinematicSettings.RandomizeLastEnemyProjectileDuration = ParseBool(value, false);
                            break;
                        case "LastEnemyProjectileReturnDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leProjRet))
                                s_cinematicSettings.LastEnemyProjectileReturnDuration = leProjRet;
                            break;
                        case "LastEnemyProjectileReturnDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leProjRetMin))
                                s_cinematicSettings.LastEnemyProjectileReturnDurationMin = leProjRetMin;
                            break;
                        case "LastEnemyProjectileReturnDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leProjRetMax))
                                s_cinematicSettings.LastEnemyProjectileReturnDurationMax = leProjRetMax;
                            break;
                        case "RandomizeLastEnemyProjectileReturnDuration":
                            s_cinematicSettings.RandomizeLastEnemyProjectileReturnDuration = ParseBool(value, false);
                            break;
                        
                        // Weapon Mode Settings - Load directly into MenuV2
                        case "MenuV2_MeleeTriggerChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var meleeChance))
                                s_cinematicSettings.MenuV2.Melee.TriggerChancePercent = Mathf.Clamp(meleeChance, 0f, 100f);
                            break;
                        case "MeleeTriggerChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var meleeChanceLegacy))
                                s_cinematicSettings.MenuV2.Melee.TriggerChancePercent = Mathf.Clamp(meleeChanceLegacy, 0f, 100f);
                            break;
                        case "MenuV2_MeleeEnabled":
                            s_cinematicSettings.MenuV2.Melee.Enabled = ParseBool(value, true);
                            break;
                        case "MeleeOverrideDuration":
                            s_cinematicSettings.MenuV2.Melee.OverrideGlobalSlowMo = ParseBool(value, false);
                            break;
                        case "MeleeDurationSeconds":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var meleeDur))
                                s_cinematicSettings.MenuV2.Melee.OverrideDurationSeconds = Mathf.Clamp(meleeDur, 0.1f, 10f);
                            break;
                        case "MeleeUseFirstPerson":
                            s_cinematicSettings.MenuV2.Melee.UseFirstPersonCamera = ParseBool(value, true);
                            break;
                        case "MeleeUseProjectile":
                            s_cinematicSettings.MenuV2.Melee.UseProjectileCamera = ParseBool(value, true);
                            break;
                        case "RangedTriggerChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rangedChance))
                                s_cinematicSettings.MenuV2.Ranged.TriggerChancePercent = Mathf.Clamp(rangedChance, 0f, 100f);
                            break;
                        case "MenuV2_RangedEnabled":
                            s_cinematicSettings.MenuV2.Ranged.Enabled = ParseBool(value, true);
                            break;
                        case "RangedOverrideDuration":
                            s_cinematicSettings.MenuV2.Ranged.OverrideGlobalSlowMo = ParseBool(value, false);
                            break;
                        case "RangedDurationSeconds":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rangedDur))
                                s_cinematicSettings.MenuV2.Ranged.OverrideDurationSeconds = Mathf.Clamp(rangedDur, 0.1f, 10f);
                            break;
                        case "RangedUseFirstPerson":
                            s_cinematicSettings.MenuV2.Ranged.UseFirstPersonCamera = ParseBool(value, true);
                            break;
                        case "RangedUseProjectile":
                            s_cinematicSettings.MenuV2.Ranged.UseProjectileCamera = ParseBool(value, true);
                            break;
                        case "ExplosiveTriggerChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var expChance))
                                s_cinematicSettings.MenuV2.Dismember.TriggerChancePercent = Mathf.Clamp(expChance, 0f, 100f);
                            break;
                        case "MenuV2_ExplosiveEnabled":
                            s_cinematicSettings.MenuV2.Dismember.Enabled = ParseBool(value, true);
                            break;
                        case "ExplosiveOverrideDuration":
                            s_cinematicSettings.MenuV2.Dismember.OverrideGlobalSlowMo = ParseBool(value, false);
                            break;
                        case "ExplosiveDurationSeconds":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var expDur))
                                s_cinematicSettings.MenuV2.Dismember.OverrideDurationSeconds = Mathf.Clamp(expDur, 0.1f, 10f);
                            break;
                        case "ExplosiveUseFirstPerson":
                            s_cinematicSettings.MenuV2.Dismember.UseFirstPersonCamera = ParseBool(value, true);
                            break;
                        case "ExplosiveUseProjectile":
                            s_cinematicSettings.MenuV2.Dismember.UseProjectileCamera = ParseBool(value, true);
                            break;
                        
                        // Contextual Trigger Settings - Load into TriggerSystem
                        case "TriggerDistanceThreshold":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var distThresh))
                                s_cinematicSettings.MenuV2.TriggerSystem.DistanceThreshold = Mathf.Max(1f, distThresh);
                            break;
                        
                        // Trigger bonus settings
                        case "TriggerBonusDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bonusDur))
                                s_cinematicSettings.MenuV2.TriggerSystem.TriggerBonusDuration = Mathf.Max(0f, bonusDur);
                            break;
                        case "TriggerSlowReduction":
                        case "TriggerBonusSlowScale": // Legacy support
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var slowRed))
                                s_cinematicSettings.MenuV2.TriggerSystem.TriggerSlowReduction = Mathf.Clamp(slowRed, 0f, 0.5f);
                            break;
                        case "RequireTriggerForCinematic":
                            s_cinematicSettings.MenuV2.TriggerSystem.RequireTriggerForCinematic = ParseBool(value, false);
                            break;
                        case "StackTriggerBonuses":
                            s_cinematicSettings.MenuV2.TriggerSystem.StackTriggerBonuses = ParseBool(value, false);
                            break;
                        
                        // LastEnemy trigger
                        case "MenuV2_LastEnemyEnabled":
                            s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.Enabled = ParseBool(value, true);
                            break;
                        case "LastEnemyPriority":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lePri))
                                s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.Priority = lePri;
                            break;
                        case "MenuV2_LastEnemyCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leCd))
                                s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.CooldownSeconds = Mathf.Max(0f, leCd);
                            break;
                        case "LastEnemyAllowFirstPerson":
                            s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.AllowFirstPerson = ParseBool(value, true);
                            break;
                        case "LastEnemyAllowProjectile":
                            s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.AllowProjectile = ParseBool(value, true);
                            break;
                        case "MenuV2_LastEnemyFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leFpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.FirstPersonChance = Mathf.Clamp(leFpc, 0f, 100f);
                            break;
                        case "LastEnemyProjectileChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lePpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.ProjectileChance = Mathf.Clamp(lePpc, 0f, 100f);
                            break;
                        case "LastEnemyOverrideScreenEffects":
                            s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.OverrideScreenEffects = ParseBool(value, false);
                            break;
                        case "LastEnemyEnableVignette":
                            s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.EnableVignette = ParseBool(value, true);
                            break;
                        case "LastEnemyVignetteIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leVig))
                                s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.VignetteIntensity = Mathf.Clamp01(leVig);
                            break;
                        case "LastEnemyEnableDesaturation":
                            s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.EnableDesaturation = ParseBool(value, true);
                            break;
                        case "LastEnemyDesaturationAmount":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leDesat))
                                s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.DesaturationAmount = Mathf.Clamp01(leDesat);
                            break;
                        case "LastEnemyEnableBloodSplatter":
                            s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.EnableBloodSplatter = ParseBool(value, true);
                            break;
                        case "LastEnemyEnableRadialBlur":
                            s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.EnableRadialBlur = ParseBool(value, true);
                            break;
                        case "LastEnemyRadialBlurIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var leRadial))
                                s_cinematicSettings.MenuV2.TriggerSystem.LastEnemy.RadialBlurIntensity = Mathf.Clamp01(leRadial);
                            break;
                        
                        // Killstreak trigger
                        case "KillstreakTriggerEnabled":
                            s_cinematicSettings.MenuV2.TriggerSystem.Killstreak.Enabled = ParseBool(value, true);
                            break;
                        case "KillstreakPriority":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ksPri))
                                s_cinematicSettings.MenuV2.TriggerSystem.Killstreak.Priority = ksPri;
                            break;
                        case "MenuV2_KillstreakCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ksCd))
                                s_cinematicSettings.MenuV2.TriggerSystem.Killstreak.CooldownSeconds = Mathf.Max(0f, ksCd);
                            break;
                        case "KillstreakAllowFirstPerson":
                            s_cinematicSettings.MenuV2.TriggerSystem.Killstreak.AllowFirstPerson = ParseBool(value, true);
                            break;
                        case "KillstreakAllowProjectile":
                            s_cinematicSettings.MenuV2.TriggerSystem.Killstreak.AllowProjectile = ParseBool(value, true);
                            break;
                        case "MenuV2_KillstreakFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ksFpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.Killstreak.FirstPersonChance = Mathf.Clamp(ksFpc, 0f, 100f);
                            break;
                        case "KillstreakProjectileChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ksPpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.Killstreak.ProjectileChance = Mathf.Clamp(ksPpc, 0f, 100f);
                            break;
                        case "KillstreakOverrideScreenEffects":
                            s_cinematicSettings.MenuV2.TriggerSystem.Killstreak.OverrideScreenEffects = ParseBool(value, false);
                            break;
                        
                        // DismemberKill trigger
                        case "DismemberTriggerEnabled":
                            s_cinematicSettings.MenuV2.TriggerSystem.DismemberKill.Enabled = ParseBool(value, true);
                            break;
                        case "DismemberTriggerPriority":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dmPri))
                                s_cinematicSettings.MenuV2.TriggerSystem.DismemberKill.Priority = dmPri;
                            break;
                        case "DismemberTriggerCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dmCd))
                                s_cinematicSettings.MenuV2.TriggerSystem.DismemberKill.CooldownSeconds = Mathf.Max(0f, dmCd);
                            break;
                        case "DismemberTriggerAllowFirstPerson":
                            s_cinematicSettings.MenuV2.TriggerSystem.DismemberKill.AllowFirstPerson = ParseBool(value, true);
                            break;
                        case "DismemberTriggerAllowProjectile":
                            s_cinematicSettings.MenuV2.TriggerSystem.DismemberKill.AllowProjectile = ParseBool(value, true);
                            break;
                        case "MenuV2_DismemberFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dmFpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.DismemberKill.FirstPersonChance = Mathf.Clamp(dmFpc, 0f, 100f);
                            break;
                        case "DismemberProjectileChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dmPpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.DismemberKill.ProjectileChance = Mathf.Clamp(dmPpc, 0f, 100f);
                            break;
                        case "DismemberOverrideScreenEffects":
                            s_cinematicSettings.MenuV2.TriggerSystem.DismemberKill.OverrideScreenEffects = ParseBool(value, false);
                            break;
                        
                        // Headshot trigger
                        case "MenuV2_HeadshotEnabled":
                            s_cinematicSettings.MenuV2.TriggerSystem.Headshot.Enabled = ParseBool(value, true);
                            break;
                        case "HeadshotPriority":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hsPri))
                                s_cinematicSettings.MenuV2.TriggerSystem.Headshot.Priority = hsPri;
                            break;
                        case "MenuV2_HeadshotCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hsCd))
                                s_cinematicSettings.MenuV2.TriggerSystem.Headshot.CooldownSeconds = Mathf.Max(0f, hsCd);
                            break;
                        case "HeadshotAllowFirstPerson":
                            s_cinematicSettings.MenuV2.TriggerSystem.Headshot.AllowFirstPerson = ParseBool(value, true);
                            break;
                        case "HeadshotAllowProjectile":
                            s_cinematicSettings.MenuV2.TriggerSystem.Headshot.AllowProjectile = ParseBool(value, true);
                            break;
                        case "MenuV2_HeadshotFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hsFpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.Headshot.FirstPersonChance = Mathf.Clamp(hsFpc, 0f, 100f);
                            break;
                        case "HeadshotProjectileChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hsPpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.Headshot.ProjectileChance = Mathf.Clamp(hsPpc, 0f, 100f);
                            break;
                        case "HeadshotOverrideScreenEffects":
                            s_cinematicSettings.MenuV2.TriggerSystem.Headshot.OverrideScreenEffects = ParseBool(value, false);
                            break;
                        
                        // Critical trigger
                        case "CriticalTriggerEnabled":
                            s_cinematicSettings.MenuV2.TriggerSystem.Critical.Enabled = ParseBool(value, true);
                            break;
                        case "CriticalPriority":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var crPri))
                                s_cinematicSettings.MenuV2.TriggerSystem.Critical.Priority = crPri;
                            break;
                        case "MenuV2_CriticalCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var crCd))
                                s_cinematicSettings.MenuV2.TriggerSystem.Critical.CooldownSeconds = Mathf.Max(0f, crCd);
                            break;
                        case "CriticalAllowFirstPerson":
                            s_cinematicSettings.MenuV2.TriggerSystem.Critical.AllowFirstPerson = ParseBool(value, true);
                            break;
                        case "CriticalAllowProjectile":
                            s_cinematicSettings.MenuV2.TriggerSystem.Critical.AllowProjectile = ParseBool(value, true);
                            break;
                        case "MenuV2_CriticalFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var crFpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.Critical.FirstPersonChance = Mathf.Clamp(crFpc, 0f, 100f);
                            break;
                        case "CriticalProjectileChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var crPpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.Critical.ProjectileChance = Mathf.Clamp(crPpc, 0f, 100f);
                            break;
                        case "CriticalOverrideScreenEffects":
                            s_cinematicSettings.MenuV2.TriggerSystem.Critical.OverrideScreenEffects = ParseBool(value, false);
                            break;
                        
                        // LongRangeKill trigger
                        case "LongRangeTriggerEnabled":
                            s_cinematicSettings.MenuV2.TriggerSystem.LongRangeKill.Enabled = ParseBool(value, true);
                            break;
                        case "LongRangeTriggerPriority":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lrPri))
                                s_cinematicSettings.MenuV2.TriggerSystem.LongRangeKill.Priority = lrPri;
                            break;
                        case "LongRangeTriggerCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrCd))
                                s_cinematicSettings.MenuV2.TriggerSystem.LongRangeKill.CooldownSeconds = Mathf.Max(0f, lrCd);
                            break;
                        case "LongRangeTriggerAllowFirstPerson":
                            s_cinematicSettings.MenuV2.TriggerSystem.LongRangeKill.AllowFirstPerson = ParseBool(value, true);
                            break;
                        case "LongRangeTriggerAllowProjectile":
                            s_cinematicSettings.MenuV2.TriggerSystem.LongRangeKill.AllowProjectile = ParseBool(value, true);
                            break;
                        case "MenuV2_LongRangeFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrFpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.LongRangeKill.FirstPersonChance = Mathf.Clamp(lrFpc, 0f, 100f);
                            break;
                        case "LongRangeProjectileChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrPpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.LongRangeKill.ProjectileChance = Mathf.Clamp(lrPpc, 0f, 100f);
                            break;
                        case "LongRangeOverrideScreenEffects":
                            s_cinematicSettings.MenuV2.TriggerSystem.LongRangeKill.OverrideScreenEffects = ParseBool(value, false);
                            break;
                        
                        // LowHealthKill trigger
                        case "LowHealthTriggerEnabled":
                            s_cinematicSettings.MenuV2.TriggerSystem.LowHealthKill.Enabled = ParseBool(value, true);
                            break;
                        case "LowHealthTriggerPriority":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lhPri))
                                s_cinematicSettings.MenuV2.TriggerSystem.LowHealthKill.Priority = lhPri;
                            break;
                        case "LowHealthTriggerCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhCd))
                                s_cinematicSettings.MenuV2.TriggerSystem.LowHealthKill.CooldownSeconds = Mathf.Max(0f, lhCd);
                            break;
                        case "LowHealthTriggerAllowFirstPerson":
                            s_cinematicSettings.MenuV2.TriggerSystem.LowHealthKill.AllowFirstPerson = ParseBool(value, true);
                            break;
                        case "LowHealthTriggerAllowProjectile":
                            s_cinematicSettings.MenuV2.TriggerSystem.LowHealthKill.AllowProjectile = ParseBool(value, true);
                            break;
                        case "MenuV2_LowHealthFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhFpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.LowHealthKill.FirstPersonChance = Mathf.Clamp(lhFpc, 0f, 100f);
                            break;
                        case "LowHealthProjectileChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhPpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.LowHealthKill.ProjectileChance = Mathf.Clamp(lhPpc, 0f, 100f);
                            break;
                        case "LowHealthOverrideScreenEffects":
                            s_cinematicSettings.MenuV2.TriggerSystem.LowHealthKill.OverrideScreenEffects = ParseBool(value, false);
                            break;
                        
                        // SneakKill trigger
                        case "SneakTriggerEnabled":
                            s_cinematicSettings.MenuV2.TriggerSystem.SneakKill.Enabled = ParseBool(value, true);
                            break;
                        case "SneakTriggerPriority":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var snkPri))
                                s_cinematicSettings.MenuV2.TriggerSystem.SneakKill.Priority = snkPri;
                            break;
                        case "SneakTriggerCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var snkCd))
                                s_cinematicSettings.MenuV2.TriggerSystem.SneakKill.CooldownSeconds = Mathf.Max(0f, snkCd);
                            break;
                        case "SneakTriggerAllowFirstPerson":
                            s_cinematicSettings.MenuV2.TriggerSystem.SneakKill.AllowFirstPerson = ParseBool(value, true);
                            break;
                        case "SneakTriggerAllowProjectile":
                            s_cinematicSettings.MenuV2.TriggerSystem.SneakKill.AllowProjectile = ParseBool(value, true);
                            break;
                        case "MenuV2_SneakFirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var snkFpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.SneakKill.FirstPersonChance = Mathf.Clamp(snkFpc, 0f, 100f);
                            break;
                        case "SneakProjectileChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var snkPpc))
                                s_cinematicSettings.MenuV2.TriggerSystem.SneakKill.ProjectileChance = Mathf.Clamp(snkPpc, 0f, 100f);
                            break;
                        case "SneakOverrideScreenEffects":
                            s_cinematicSettings.MenuV2.TriggerSystem.SneakKill.OverrideScreenEffects = ParseBool(value, false);
                            break;
                            
                        case "KillcamChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var kcChance))
                            {
                                s_cinematicSettings.KillcamChance = Mathf.Clamp01(kcChance);
                                s_killcamChance = s_cinematicSettings.KillcamChance;
                            }
                            break;
                        case "Duration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ksWindow2))
                                s_cinematicSettings.KillstreakWindow = Mathf.Max(1f, ksWindow2);
                            break;
                        case "Tier1Kills":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var t1k))
                                s_cinematicSettings.Tier1Kills = Mathf.Max(1, t1k);
                            break;
                        case "Tier1BonusDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var t1b))
                                s_cinematicSettings.Tier1BonusDuration = Mathf.Max(0f, t1b);
                            break;
                        case "Tier2Kills":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var t2k))
                                s_cinematicSettings.Tier2Kills = Mathf.Max(1, t2k);
                            break;
                        case "Tier2BonusDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var t2b))
                                s_cinematicSettings.Tier2BonusDuration = Mathf.Max(0f, t2b);
                            break;
                        case "Tier3Kills":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var t3k))
                                s_cinematicSettings.Tier3Kills = Mathf.Max(1, t3k);
                            break;
                        case "Tier3BonusDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var t3b))
                                s_cinematicSettings.Tier3BonusDuration = Mathf.Max(0f, t3b);
                            break;

                        // GlobalVisuals
                        case "GV_EnableScreenEffects":
                            s_cinematicSettings.MenuV2.GlobalVisuals.EnableScreenEffects = ParseBool(value, true);
                            break;
                        case "GV_EnableFOVEffect":
                            s_cinematicSettings.MenuV2.GlobalVisuals.EnableFOVEffect = ParseBool(value, true);
                            break;
                        case "GV_FOVMultiplier":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvFovMult))
                                s_cinematicSettings.MenuV2.GlobalVisuals.FOVMultiplier = gvFovMult;
                            break;
                        case "GV_FOVInDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvFovIn))
                                s_cinematicSettings.MenuV2.GlobalVisuals.FOVInDuration = gvFovIn;
                            break;
                        case "GV_FOVHoldDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvFovHold))
                                s_cinematicSettings.MenuV2.GlobalVisuals.FOVHoldDuration = gvFovHold;
                            break;
                        case "GV_FOVOutDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvFovOut))
                                s_cinematicSettings.MenuV2.GlobalVisuals.FOVOutDuration = gvFovOut;
                            break;
                        case "GV_EnableVignette":
                            s_cinematicSettings.MenuV2.GlobalVisuals.EnableVignette = ParseBool(value, true);
                            break;
                        case "GV_VignetteIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvVignette))
                                s_cinematicSettings.MenuV2.GlobalVisuals.VignetteIntensity = gvVignette;
                            break;
                        case "GV_EnableDesaturation":
                            s_cinematicSettings.MenuV2.GlobalVisuals.EnableDesaturation = ParseBool(value, false);
                            break;
                        case "GV_DesaturationAmount":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvDesat))
                                s_cinematicSettings.MenuV2.GlobalVisuals.DesaturationAmount = gvDesat;
                            break;
                        case "GV_EnableBloodSplatter":
                            s_cinematicSettings.MenuV2.GlobalVisuals.EnableBloodSplatter = ParseBool(value, true);
                            break;
                        case "GV_BloodSplatterDirection":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var gvSplatterDir))
                                s_cinematicSettings.MenuV2.GlobalVisuals.BloodSplatterDirection = gvSplatterDir;
                            break;
                        case "GV_BloodSplatterIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvSplatterInt))
                                s_cinematicSettings.MenuV2.GlobalVisuals.BloodSplatterIntensity = gvSplatterInt;
                            break;
                        case "GV_EnableConcussion":
                            s_cinematicSettings.MenuV2.GlobalVisuals.EnableConcussion = ParseBool(value, false);
                            break;
                        case "GV_ConcussionIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvConcInt))
                                s_cinematicSettings.MenuV2.GlobalVisuals.ConcussionIntensity = gvConcInt;
                            break;
                        case "GV_ConcussionDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvConcDur))
                                s_cinematicSettings.MenuV2.GlobalVisuals.ConcussionDuration = gvConcDur;
                            break;
                        case "GV_ConcussionAudioMuffle":
                            s_cinematicSettings.MenuV2.GlobalVisuals.ConcussionAudioMuffle = ParseBool(value, false);
                            break;
                        // Post-processing effects
                        case "GV_EnableMotionBlur":
                            s_cinematicSettings.MenuV2.GlobalVisuals.EnableMotionBlur = ParseBool(value, true);
                            break;
                        case "GV_MotionBlurIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvMbInt))
                                s_cinematicSettings.MenuV2.GlobalVisuals.MotionBlurIntensity = gvMbInt;
                            break;
                        case "GV_EnableChromaticAberration":
                            s_cinematicSettings.MenuV2.GlobalVisuals.EnableChromaticAberration = ParseBool(value, true);
                            break;
                        case "GV_ChromaticAberrationIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvCaInt))
                                s_cinematicSettings.MenuV2.GlobalVisuals.ChromaticAberrationIntensity = gvCaInt;
                            break;
                        case "GV_EnableDepthOfField":
                            s_cinematicSettings.MenuV2.GlobalVisuals.EnableDepthOfField = ParseBool(value, false);
                            break;
                        case "GV_DepthOfFieldFocusDistance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvDofFocus))
                                s_cinematicSettings.MenuV2.GlobalVisuals.DepthOfFieldFocusDistance = gvDofFocus;
                            break;
                        case "GV_DepthOfFieldAperture":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvDofAperture))
                                s_cinematicSettings.MenuV2.GlobalVisuals.DepthOfFieldAperture = gvDofAperture;
                            break;
                        case "GV_DepthOfFieldFocalLength":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvDofFocal))
                                s_cinematicSettings.MenuV2.GlobalVisuals.DepthOfFieldFocalLength = gvDofFocal;
                            break;
                        case "GV_EnableRadialBlur":
                            s_cinematicSettings.MenuV2.GlobalVisuals.EnableRadialBlur = ParseBool(value, false);
                            break;
                        case "GV_RadialBlurIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvRbInt))
                                s_cinematicSettings.MenuV2.GlobalVisuals.RadialBlurIntensity = gvRbInt;
                            break;
                        case "GV_RadialBlurDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gvRbDur))
                                s_cinematicSettings.MenuV2.GlobalVisuals.RadialBlurDuration = gvRbDur;
                            break;

                        // Core HUD Settings
                        case "Core_EnableHUD":
                            s_cinematicSettings.MenuV2.Core.EnableHUD = ParseBool(value, true);
                            break;
                        case "Core_HUDOpacity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hudOpacity))
                                s_cinematicSettings.MenuV2.Core.HUDOpacity = Mathf.Clamp(hudOpacity, 0.2f, 1f);
                            break;
                        case "Core_HUDMessageDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hudDuration))
                                s_cinematicSettings.MenuV2.Core.HUDMessageDuration = Mathf.Clamp(hudDuration, 1f, 8f);
                            break;

                        // Smart Options
                        case "SmartIndoorOutdoorDetection":
                            s_cinematicSettings.SmartIndoorOutdoorDetection = ParseBool(value, false);
                            break;
                        case "IndoorDetectionHeight":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var detectHeight))
                                s_cinematicSettings.IndoorDetectionHeight = Mathf.Clamp(detectHeight, 3f, 20f);
                            break;
                        case "EnableAdvancedFOVTiming":
                            s_cinematicSettings.EnableAdvancedFOVTiming = ParseBool(value, false);
                            break;
                        
                        // BasicKill Randomization
                        case "BK_RandomizeChance":
                            s_cinematicSettings.BasicKill.RandomizeChance = ParseBool(value, false);
                            break;
                        case "BK_ChanceMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkChMin))
                                s_cinematicSettings.BasicKill.ChanceMin = bkChMin;
                            break;
                        case "BK_ChanceMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkChMax))
                                s_cinematicSettings.BasicKill.ChanceMax = bkChMax;
                            break;
                        case "BK_RandomizeDuration":
                            s_cinematicSettings.BasicKill.RandomizeDuration = ParseBool(value, false);
                            break;
                        case "BK_DurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkDurMin))
                                s_cinematicSettings.BasicKill.DurationMin = bkDurMin;
                            break;
                        case "BK_DurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkDurMax))
                                s_cinematicSettings.BasicKill.DurationMax = bkDurMax;
                            break;
                        case "BK_RandomizeTimeScale":
                            s_cinematicSettings.BasicKill.RandomizeTimeScale = ParseBool(value, false);
                            break;
                        case "BK_TimeScaleMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkTsMin))
                                s_cinematicSettings.BasicKill.TimeScaleMin = bkTsMin;
                            break;
                        case "BK_TimeScaleMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkTsMax))
                                s_cinematicSettings.BasicKill.TimeScaleMax = bkTsMax;
                            break;
                        
                        // BasicKill Core Settings
                        case "BK_Enabled":
                            s_cinematicSettings.BasicKill.Enabled = ParseBool(value, true);
                            break;
                        case "BK_Chance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkChanceCore))
                                s_cinematicSettings.BasicKill.Chance = bkChanceCore;
                            break;
                        case "BK_Duration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkDurCore))
                                s_cinematicSettings.BasicKill.Duration = bkDurCore;
                            break;
                        case "BK_TimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkTsCore))
                                s_cinematicSettings.BasicKill.TimeScale = bkTsCore;
                            break;
                        case "BK_Cooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkCdCore))
                                s_cinematicSettings.BasicKill.Cooldown = bkCdCore;
                            break;
                        case "BK_FirstPersonCamera":
                            s_cinematicSettings.BasicKill.FirstPersonCamera = ParseBool(value, true);
                            break;
                        case "BK_ProjectileCamera":
                            s_cinematicSettings.BasicKill.ProjectileCamera = ParseBool(value, true);
                            break;
                        case "BK_FirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkFpChance))
                                s_cinematicSettings.BasicKill.FirstPersonChance = bkFpChance;
                            break;
                        case "BK_FOVMode":
                            if (int.TryParse(value, out var bkFovMode))
                                s_cinematicSettings.BasicKill.FOVMode = (FOVMode)bkFovMode;
                            break;
                        case "BK_FOVPercent":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkFovPct))
                                s_cinematicSettings.BasicKill.FOVPercent = bkFovPct;
                            break;
                        case "BK_FOVZoomInDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkFovIn))
                                s_cinematicSettings.BasicKill.FOVZoomInDuration = bkFovIn;
                            break;
                        case "BK_FOVHoldDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkFovHold))
                                s_cinematicSettings.BasicKill.FOVHoldDuration = bkFovHold;
                            break;
                        case "BK_FOVZoomOutDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkFovOut))
                                s_cinematicSettings.BasicKill.FOVZoomOutDuration = bkFovOut;
                            break;
                        case "BK_ProjectileFOVMode":
                            if (int.TryParse(value, out var bkProjFovMode))
                                s_cinematicSettings.BasicKill.ProjectileFOVMode = (FOVMode)bkProjFovMode;
                            break;
                        case "BK_ProjectileFOVPercent":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkProjFovPct))
                                s_cinematicSettings.BasicKill.ProjectileFOVPercent = bkProjFovPct;
                            break;
                        case "BK_ProjectileFOVZoomInDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkProjFovIn))
                                s_cinematicSettings.BasicKill.ProjectileFOVZoomInDuration = bkProjFovIn;
                            break;
                        case "BK_ProjectileFOVHoldDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkProjFovHold))
                                s_cinematicSettings.BasicKill.ProjectileFOVHoldDuration = bkProjFovHold;
                            break;
                        case "BK_ProjectileFOVZoomOutDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bkProjFovOut))
                                s_cinematicSettings.BasicKill.ProjectileFOVZoomOutDuration = bkProjFovOut;
                            break;
                        
                        // TriggerDefaults Core Settings
                        case "TD_EnableTriggers":
                            s_cinematicSettings.TriggerDefaults.EnableTriggers = ParseBool(value, true);
                            break;
                        case "TD_Duration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdDurCore))
                                s_cinematicSettings.TriggerDefaults.Duration = tdDurCore;
                            break;
                        case "TD_TimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdTsCore))
                                s_cinematicSettings.TriggerDefaults.TimeScale = tdTsCore;
                            break;
                        case "MasterTriggerChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var masterChance))
                            {
                                s_cinematicSettings.MasterTriggerChance = Mathf.Clamp(masterChance, 0f, 100f);
                                if (s_cinematicSettings.MenuV2?.TriggerSystem != null)
                                    s_cinematicSettings.MenuV2.TriggerSystem.MasterTriggerChance = s_cinematicSettings.MasterTriggerChance;
                            }
                            break;
                        case "TD_FirstPersonCamera":
                            s_cinematicSettings.TriggerDefaults.FirstPersonCamera = ParseBool(value, true);
                            break;
                        case "TD_ProjectileCamera":
                            s_cinematicSettings.TriggerDefaults.ProjectileCamera = ParseBool(value, true);
                            break;
                        case "TD_FirstPersonChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdFpChance))
                                s_cinematicSettings.TriggerDefaults.FirstPersonChance = tdFpChance;
                            break;
                        case "TD_FOVMode":
                            if (int.TryParse(value, out var tdFovMode))
                                s_cinematicSettings.TriggerDefaults.FOVMode = (FOVMode)tdFovMode;
                            break;
                        case "TD_FOVPercent":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdFovPct))
                                s_cinematicSettings.TriggerDefaults.FOVPercent = tdFovPct;
                            break;
                        case "TD_FOVZoomInDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdFovIn))
                                s_cinematicSettings.TriggerDefaults.FOVZoomInDuration = tdFovIn;
                            break;
                        case "TD_FOVHoldDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdFovHold))
                                s_cinematicSettings.TriggerDefaults.FOVHoldDuration = tdFovHold;
                            break;
                        case "TD_FOVZoomOutDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdFovOut))
                                s_cinematicSettings.TriggerDefaults.FOVZoomOutDuration = tdFovOut;
                            break;
                        case "TD_ProjectileFOVMode":
                            if (int.TryParse(value, out var tdProjFovMode))
                                s_cinematicSettings.TriggerDefaults.ProjectileFOVMode = (FOVMode)tdProjFovMode;
                            break;
                        case "TD_ProjectileFOVPercent":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdProjFovPct))
                                s_cinematicSettings.TriggerDefaults.ProjectileFOVPercent = tdProjFovPct;
                            break;
                        case "TD_ProjectileFOVZoomInDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdProjFovIn))
                                s_cinematicSettings.TriggerDefaults.ProjectileFOVZoomInDuration = tdProjFovIn;
                            break;
                        case "TD_ProjectileFOVHoldDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdProjFovHold))
                                s_cinematicSettings.TriggerDefaults.ProjectileFOVHoldDuration = tdProjFovHold;
                            break;
                        case "TD_ProjectileFOVZoomOutDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdProjFovOut))
                                s_cinematicSettings.TriggerDefaults.ProjectileFOVZoomOutDuration = tdProjFovOut;
                            break;
                        
                        // TriggerDefaults Randomization
                        case "TD_RandomizeDuration":
                            s_cinematicSettings.TriggerDefaults.RandomizeDuration = ParseBool(value, false);
                            break;
                        case "TD_DurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdDurMin))
                                s_cinematicSettings.TriggerDefaults.DurationMin = tdDurMin;
                            break;
                        case "TD_DurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdDurMax))
                                s_cinematicSettings.TriggerDefaults.DurationMax = tdDurMax;
                            break;
                        case "TD_RandomizeTimeScale":
                            s_cinematicSettings.TriggerDefaults.RandomizeTimeScale = ParseBool(value, false);
                            break;
                        case "TD_TimeScaleMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdTsMin))
                                s_cinematicSettings.TriggerDefaults.TimeScaleMin = tdTsMin;
                            break;
                        case "TD_TimeScaleMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdTsMax))
                                s_cinematicSettings.TriggerDefaults.TimeScaleMax = tdTsMax;
                            break;
                        
                        // Weapon Mode Camera Overrides
                        case "MeleeCameraOverride":
                            if (int.TryParse(value, out var meleeOvr))
                                s_cinematicSettings.MeleeCameraOverride = (CameraOverride)meleeOvr;
                            break;
                        case "RangedCameraOverride":
                            if (int.TryParse(value, out var rangedOvr))
                                s_cinematicSettings.RangedCameraOverride = (CameraOverride)rangedOvr;
                            break;
                        case "BowCameraOverride":
                            if (int.TryParse(value, out var bowOvr))
                                s_cinematicSettings.BowCameraOverride = (CameraOverride)bowOvr;
                            break;
                        case "ExplosiveCameraOverride":
                            if (int.TryParse(value, out var expOvr))
                                s_cinematicSettings.ExplosiveCameraOverride = (CameraOverride)expOvr;
                            break;
                        case "TrapCameraOverride":
                            if (int.TryParse(value, out var trapOvr))
                                s_cinematicSettings.TrapCameraOverride = (CameraOverride)trapOvr;
                            break;

                        // Menu
                        case "MenuKey":
                            if (Enum.TryParse(value, true, out KeyCode key))
                                s_cinematicSettings.MenuKey = key;
                            break;
						case "EnableGlobalCooldown":
								s_cinematicSettings.EnableGlobalCooldown = ParseBool(value, true);
							break;
                        case "CooldownGlobal":
                            if (float.TryParse(value, out float cdGlobal))
                            {
                                s_cinematicSettings.CooldownGlobal = Mathf.Max(0f, cdGlobal);
                            }
                            break;
						case "IgnoreGlobalCooldownOnLastEnemy":
							s_cinematicSettings.IgnoreGlobalCooldownOnLastEnemy = ParseBool(value, false);
							break;

						case "EnableCooldownCrit":
							s_cinematicSettings.EnableCooldownCrit = ParseBool(value, true);
							break;
						case "CooldownCrit":
							if (float.TryParse(value, out float cdCrit))
							{
								s_cinematicSettings.CooldownCrit = Mathf.Max(0f, cdCrit);
							}
							break;

						case "EnableCooldownDismember":
							s_cinematicSettings.EnableCooldownDismember = ParseBool(value, true);
							break;
						case "CooldownDismember":
							if (float.TryParse(value, out float cdDismember))
							{
								s_cinematicSettings.CooldownDismember = Mathf.Max(0f, cdDismember);
							}
							break;

						case "EnableCooldownLongRange":
							s_cinematicSettings.EnableCooldownLongRange = ParseBool(value, true);
							break;
						case "CooldownLongRange":
							if (float.TryParse(value, out float cdLongRange))
							{
								s_cinematicSettings.CooldownLongRange = Mathf.Max(0f, cdLongRange);
							}
							break;

						case "EnableCooldownLowHealth":
							s_cinematicSettings.EnableCooldownLowHealth = ParseBool(value, true);
							break;
						case "CooldownLowHealth":
							if (float.TryParse(value, out float cdLowHealth))
							{
								s_cinematicSettings.CooldownLowHealth = Mathf.Max(0f, cdLowHealth);
							}
							break;

						case "EnableCooldownLastEnemy":
							s_cinematicSettings.EnableCooldownLastEnemy = ParseBool(value, true);
							break;
						case "CooldownLastEnemy":
							if (float.TryParse(value, out float cdLastEnemy))
							{
								s_cinematicSettings.CooldownLastEnemy = Mathf.Max(0f, cdLastEnemy);
							}
							break;

						case "IgnoreCorpseHits":
							s_ignoreCorpseHits = ParseBool(value, true);
                            s_cinematicSettings.IgnoreCorpseHits = s_ignoreCorpseHits;
							break;
                        case "EnableProjectileCamera":
                            s_cinematicSettings.EnableProjectileCamera = ParseBool(value, true);
                            break;
                        case "EnableProjectileSlowMo":
                            s_cinematicSettings.EnableProjectileSlowMo = ParseBool(value, true);
                            break;
                        case "Legacy_EnemyScanRadius":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var scanRadius))
                            {
                                s_cinematicSettings.EnemyScanRadius = Mathf.Max(5f, scanRadius);
                            }
                            break;
                        case "TriggerOnDismember": // Alias
                        case "EnableDismember":
                            s_cinematicSettings.EnableDismember = ParseBool(value, true);
                            break;
                        case "DismemberChance": // Alias
                        case "ChanceDismember":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var chanceDis))
                            {
                                s_cinematicSettings.ChanceDismember = Mathf.Clamp01(chanceDis);
                            }
                            break;
                        case "DismemberAllowFirstPerson":
                            s_cinematicSettings.DismemberAllowFirstPerson = ParseBool(value, true);
                            break;
                        case "DismemberAllowProjectile":
                            s_cinematicSettings.DismemberAllowProjectile = ParseBool(value, true);
                            break;
                        case "DismemberCustomEffects":
                            s_cinematicSettings.DismemberCustomEffects = ParseBool(value, false);
                            break;
                        case "DismemberOverrideHitstop":
                            s_cinematicSettings.DismemberOverrideHitstop = ParseBool(value, false);
                            break;
                        case "DismemberHitstopDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var disHs))
                                s_cinematicSettings.DismemberHitstopDuration = disHs;
                            break;
                        case "DismemberHitstopDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var disHsMin))
                                s_cinematicSettings.DismemberHitstopDurationMin = disHsMin;
                            break;
                        case "DismemberHitstopDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var disHsMax))
                                s_cinematicSettings.DismemberHitstopDurationMax = disHsMax;
                            break;
                        case "RandomizeDismemberHitstopDuration":
                            s_cinematicSettings.RandomizeDismemberHitstopDuration = ParseBool(value, false);
                            break;
                        case "DismemberHitstopOnCritOnly":
                            s_cinematicSettings.DismemberHitstopOnCritOnly = ParseBool(value, false);
                            break;
                        case "DismemberOverrideSlowMoSound":
                            s_cinematicSettings.DismemberOverrideSlowMoSound = ParseBool(value, false);
                            break;
                        case "DismemberSlowMoSoundVolume":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var disVol))
                                s_cinematicSettings.DismemberSlowMoSoundVolume = Mathf.Clamp01(disVol);
                            break;
                        case "DismemberSlowMoSoundVolumeMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var disVolMin))
                                s_cinematicSettings.DismemberSlowMoSoundVolumeMin = disVolMin;
                            break;
                        case "DismemberSlowMoSoundVolumeMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var disVolMax))
                                s_cinematicSettings.DismemberSlowMoSoundVolumeMax = disVolMax;
                            break;
                        case "RandomizeDismemberSlowMoSoundVolume":
                            s_cinematicSettings.RandomizeDismemberSlowMoSoundVolume = ParseBool(value, false);
                            break;
                        case "DismemberVisualSource":
                            s_cinematicSettings.DismemberVisualSource = value;
                            break;
                        case "DismemberFOVSource":
                            s_cinematicSettings.DismemberFOVSource = value;
                            break;
                        case "DismemberFXSource":
                            s_cinematicSettings.DismemberFXSource = value;
                            break;
                        // Context Toggles & Chances
                        case "EnableCrit":
                            s_cinematicSettings.EnableCrit = ParseBool(value, true);
                            break;
                        case "ChanceCrit":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var chanceCrit))
                                s_cinematicSettings.ChanceCrit = Mathf.Clamp01(chanceCrit);
                            break;
                        case "CritAllowFirstPerson":
                            s_cinematicSettings.CritAllowFirstPerson = ParseBool(value, true);
                            break;
                        case "CritAllowProjectile":
                            s_cinematicSettings.CritAllowProjectile = ParseBool(value, true);
                            break;
                        case "CritCustomEffects":
                            s_cinematicSettings.CritCustomEffects = ParseBool(value, false);
                            break;
                        case "CritOverrideHitstop":
                            s_cinematicSettings.CritOverrideHitstop = ParseBool(value, false);
                            break;
                        case "CritHitstopDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var critHs))
                                s_cinematicSettings.CritHitstopDuration = critHs;
                            break;
                        case "CritHitstopDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var critHsMin))
                                s_cinematicSettings.CritHitstopDurationMin = critHsMin;
                            break;
                        case "CritHitstopDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var critHsMax))
                                s_cinematicSettings.CritHitstopDurationMax = critHsMax;
                            break;
                        case "RandomizeCritHitstopDuration":
                            s_cinematicSettings.RandomizeCritHitstopDuration = ParseBool(value, false);
                            break;
                        case "CritHitstopOnCritOnly":
                            s_cinematicSettings.CritHitstopOnCritOnly = ParseBool(value, true);
                            break;
                        case "CritOverrideSlowMoSound":
                            s_cinematicSettings.CritOverrideSlowMoSound = ParseBool(value, false);
                            break;
                        case "CritSlowMoSoundVolume":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var critVol))
                                s_cinematicSettings.CritSlowMoSoundVolume = Mathf.Clamp01(critVol);
                            break;
                        case "CritSlowMoSoundVolumeMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var critVolMin))
                                s_cinematicSettings.CritSlowMoSoundVolumeMin = critVolMin;
                            break;
                        case "CritSlowMoSoundVolumeMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var critVolMax))
                                s_cinematicSettings.CritSlowMoSoundVolumeMax = critVolMax;
                            break;
                        case "RandomizeCritSlowMoSoundVolume":
                            s_cinematicSettings.RandomizeCritSlowMoSoundVolume = ParseBool(value, false);
                            break;
                        case "CritVisualSource":
                            s_cinematicSettings.CritVisualSource = value;
                            break;
                        case "CritFOVSource":
                            s_cinematicSettings.CritFOVSource = value;
                            break;
                        case "CritFXSource":
                            s_cinematicSettings.CritFXSource = value;
                            break;
                        case "EnableLongRange":
                            s_cinematicSettings.EnableLongRange = ParseBool(value, true);
                            break;
                        case "ChanceLongRange":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var chanceLR))
                                s_cinematicSettings.ChanceLongRange = Mathf.Clamp01(chanceLR);
                            break;
                        case "LongRangeAllowFirstPerson":
                            s_cinematicSettings.LongRangeAllowFirstPerson = ParseBool(value, true);
                            break;
                        case "LongRangeAllowProjectile":
                            s_cinematicSettings.LongRangeAllowProjectile = ParseBool(value, true);
                            break;
                        case "LongRangeCustomEffects":
                            s_cinematicSettings.LongRangeCustomEffects = ParseBool(value, false);
                            break;
                        case "LongRangeOverrideHitstop":
                            s_cinematicSettings.LongRangeOverrideHitstop = ParseBool(value, false);
                            break;
                        case "LongRangeHitstopDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrHs))
                                s_cinematicSettings.LongRangeHitstopDuration = lrHs;
                            break;
                        case "LongRangeHitstopDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrHsMin))
                                s_cinematicSettings.LongRangeHitstopDurationMin = lrHsMin;
                            break;
                        case "LongRangeHitstopDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrHsMax))
                                s_cinematicSettings.LongRangeHitstopDurationMax = lrHsMax;
                            break;
                        case "RandomizeLongRangeHitstopDuration":
                            s_cinematicSettings.RandomizeLongRangeHitstopDuration = ParseBool(value, false);
                            break;
                        case "LongRangeHitstopOnCritOnly":
                            s_cinematicSettings.LongRangeHitstopOnCritOnly = ParseBool(value, false);
                            break;
                        case "LongRangeOverrideSlowMoSound":
                            s_cinematicSettings.LongRangeOverrideSlowMoSound = ParseBool(value, false);
                            break;
                        case "LongRangeSlowMoSoundVolume":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrVol))
                                s_cinematicSettings.LongRangeSlowMoSoundVolume = Mathf.Clamp01(lrVol);
                            break;
                        case "LongRangeSlowMoSoundVolumeMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrVolMin))
                                s_cinematicSettings.LongRangeSlowMoSoundVolumeMin = lrVolMin;
                            break;
                        case "LongRangeSlowMoSoundVolumeMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrVolMax))
                                s_cinematicSettings.LongRangeSlowMoSoundVolumeMax = lrVolMax;
                            break;
                        case "RandomizeLongRangeSlowMoSoundVolume":
                            s_cinematicSettings.RandomizeLongRangeSlowMoSoundVolume = ParseBool(value, false);
                            break;
                        case "LongRangeVisualSource":
                            s_cinematicSettings.LongRangeVisualSource = value;
                            break;
                        case "LongRangeFOVSource":
                            s_cinematicSettings.LongRangeFOVSource = value;
                            break;
                        case "LongRangeFXSource":
                            s_cinematicSettings.LongRangeFXSource = value;
                            break;
                        case "EnableLowHealth":
                            s_cinematicSettings.EnableLowHealth = ParseBool(value, true);
                            break;
                        case "ChanceLowHealth":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var chanceLH))
                                s_cinematicSettings.ChanceLowHealth = Mathf.Clamp01(chanceLH);
                            break;
                        case "LowHealthAllowFirstPerson":
                            s_cinematicSettings.LowHealthAllowFirstPerson = ParseBool(value, true);
                            break;
                        case "LowHealthAllowProjectile":
                            s_cinematicSettings.LowHealthAllowProjectile = ParseBool(value, true);
                            break;
                        case "LowHealthCustomEffects":
                            s_cinematicSettings.LowHealthCustomEffects = ParseBool(value, false);
                            break;
                        case "LowHealthOverrideHitstop":
                            s_cinematicSettings.LowHealthOverrideHitstop = ParseBool(value, false);
                            break;
                        case "LowHealthHitstopDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhHs))
                                s_cinematicSettings.LowHealthHitstopDuration = lhHs;
                            break;
                        case "LowHealthHitstopDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhHsMin))
                                s_cinematicSettings.LowHealthHitstopDurationMin = lhHsMin;
                            break;
                        case "LowHealthHitstopDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhHsMax))
                                s_cinematicSettings.LowHealthHitstopDurationMax = lhHsMax;
                            break;
                        case "RandomizeLowHealthHitstopDuration":
                            s_cinematicSettings.RandomizeLowHealthHitstopDuration = ParseBool(value, false);
                            break;
                        case "LowHealthHitstopOnCritOnly":
                            s_cinematicSettings.LowHealthHitstopOnCritOnly = ParseBool(value, false);
                            break;
                        case "LowHealthOverrideSlowMoSound":
                            s_cinematicSettings.LowHealthOverrideSlowMoSound = ParseBool(value, false);
                            break;
                        case "LowHealthSlowMoSoundVolume":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhVol))
                                s_cinematicSettings.LowHealthSlowMoSoundVolume = Mathf.Clamp01(lhVol);
                            break;
                        case "LowHealthSlowMoSoundVolumeMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhVolMin))
                                s_cinematicSettings.LowHealthSlowMoSoundVolumeMin = lhVolMin;
                            break;
                        case "LowHealthSlowMoSoundVolumeMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhVolMax))
                                s_cinematicSettings.LowHealthSlowMoSoundVolumeMax = lhVolMax;
                            break;
                        case "RandomizeLowHealthSlowMoSoundVolume":
                            s_cinematicSettings.RandomizeLowHealthSlowMoSoundVolume = ParseBool(value, false);
                            break;
                        case "LowHealthVisualSource":
                            s_cinematicSettings.LowHealthVisualSource = value;
                            break;
                        case "LowHealthFOVSource":
                            s_cinematicSettings.LowHealthFOVSource = value;
                            break;
                        case "LowHealthFXSource":
                            s_cinematicSettings.LowHealthFXSource = value;
                            break;
                        // MenuV2Json loading removed - using only legacy XML properties
                        // Hitstop
                        case "Legacy_EnableHitstop":
                            s_cinematicSettings.EnableHitstop = ParseBool(value, true);
                            break;
                        case "Legacy_HitstopDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hitstopDur))
                            {
                                s_cinematicSettings.HitstopDuration = Mathf.Clamp(hitstopDur, 0.01f, 0.5f);
                            }
                            break;
                        case "HitstopOnCritOnly":
                            s_cinematicSettings.HitstopOnCritOnly = ParseBool(value, false);
                            break;
                        // FOV
                        case "EnableFOVEffect":
                            s_cinematicSettings.EnableFOVEffect = ParseBool(value, true);
                            break;
                        case "FOVZoomAmount":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var zoomAmount))
                            {
                                s_cinematicSettings.FOVZoomAmount = Mathf.Clamp(zoomAmount, 0f, 50f);
                            }
                            break;
                        case "FOVZoomInDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var zoomInDuration))
                            {
                                s_cinematicSettings.FOVZoomInDuration = Mathf.Max(0.1f, zoomInDuration);
                            }
                            break;
                        case "FOVZoomHoldDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var holdDuration))
                            {
                                s_cinematicSettings.FOVZoomHoldDuration = Mathf.Max(0f, holdDuration);
                            }
                            break;
                        case "FOVZoomOutDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var zoomOutDuration))
                            {
                                s_cinematicSettings.FOVZoomOutDuration = Mathf.Max(0.1f, zoomOutDuration);
                            }
                            break;
                        // Screen Effects
                        case "Legacy_EnableVignette":
                            s_cinematicSettings.EnableVignette = ParseBool(value, true);
                            break;
                        case "Legacy_VignetteIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var vignette))
                            {
                                s_cinematicSettings.VignetteIntensity = Mathf.Clamp01(vignette);
                            }
                            break;
                        case "EnableColorGrading":
                            s_cinematicSettings.EnableColorGrading = ParseBool(value, true);
                            break;
                        case "EnableFlash":
                            s_cinematicSettings.EnableFlash = ParseBool(value, true);
                            break;
                        case "FlashIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var flashInt))
                            {
                                s_cinematicSettings.FlashIntensity = Mathf.Clamp01(flashInt);
                            }
                            break;
                        case "ColorGradingIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gradeInt))
                            {
                                s_cinematicSettings.ColorGradingIntensity = Mathf.Clamp01(gradeInt);
                            }
                            break;
                        case "ColorGradingMode":
                            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var gradeMode))
                                s_cinematicSettings.ColorGradingMode = gradeMode;
                            break;
                        // Sound
                        case "EnableSlowMoSound":
                            s_cinematicSettings.EnableSlowMoSound = ParseBool(value, true);
                            break;
                        case "SlowMoSoundVolume":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var vol))
                            {
                                s_cinematicSettings.SlowMoSoundVolume = Mathf.Clamp01(vol);
                            }
                            break;
                        case "AllowFOVBeyondDuration":
                            s_cinematicSettings.AllowFOVBeyondDuration = ParseBool(value, false);
                            break;
                        // Killstreaks
                        case "EnableKillstreaks":
                            s_cinematicSettings.EnableKillstreaks = ParseBool(value, true);
                            break;
                        case "Legacy_KillstreakWindow":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var kWindow))
                                s_cinematicSettings.KillstreakWindow = Mathf.Max(1f, kWindow);
                            break;
                        // Context Modifiers
                        case "DistanceThreshold":
                            {
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dist))
                                {
                                    s_cinematicSettings.DistanceThreshold = Mathf.Max(1f, dist);
                                }
                                break;
                            }
                        case "LowHealthThreshold":
                            {
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var health))
                                {
                                    s_cinematicSettings.LowHealthThreshold = Mathf.Clamp01(health);
                                }
                                break;
                            }
                        // Multipliers
                        case "LongRangeZoomMultiplier":
                            {
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrZoom))
                                {
                                    s_cinematicSettings.LongRangeZoomMultiplier = Mathf.Max(1f, lrZoom);
                                }
                                break;
                            }
                        case "LongRangeZoomSpeed":
                            {
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lrSpeed))
                                {
                                    s_cinematicSettings.LongRangeZoomSpeed = Mathf.Clamp(lrSpeed, 0.1f, 5f);
                                }
                                break;
                            }
                        case "LowHealthSlowScale":
                            {
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lhSlow))
                                {
                                    s_cinematicSettings.LowHealthSlowScale = Mathf.Clamp(lhSlow, 0.1f, 1f);
                                }
                                break;
                            }
                        case "CritZoomMultiplier":
                            {
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var critZoom))
                                {
                                    s_cinematicSettings.CritZoomMultiplier = Mathf.Max(1f, critZoom);
                                }
                                break;

                            }
                        case "CritZoomSpeed":
                            {
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var critSpeed))
                                {
                                    s_cinematicSettings.CritZoomSpeed = Mathf.Clamp(critSpeed, 0.1f, 5f);
                                }
                                break;
                            }
                        case "ProjectileCameraDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcDur))
                                s_cinematicSettings.ProjectileCameraDuration = Mathf.Clamp(pcDur, 0.5f, 10f);
                            break;
                        case "ProjectileCameraDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcDurMin))
                                s_cinematicSettings.ProjectileCameraDurationMin = pcDurMin;
                            break;
                        case "ProjectileCameraDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcDurMax))
                                s_cinematicSettings.ProjectileCameraDurationMax = pcDurMax;
                            break;
                        case "ProjectileCameraHeightOffset":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcHeight))
                                s_cinematicSettings.ProjectileCameraHeightOffset = Mathf.Clamp(pcHeight, -5f, 10f);
                            break;
                        case "ProjectileCameraHeightOffsetMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcHeightMin))
                                s_cinematicSettings.ProjectileCameraHeightOffsetMin = pcHeightMin;
                            break;
                        case "ProjectileCameraHeightOffsetMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcHeightMax))
                                s_cinematicSettings.ProjectileCameraHeightOffsetMax = pcHeightMax;
                            break;
                        case "ProjectileCameraDistanceOffset":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcDist))
                                s_cinematicSettings.ProjectileCameraDistanceOffset = Mathf.Clamp(pcDist, 0.5f, 20f);
                            break;
                        case "ProjectileCameraDistanceOffsetMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcDistMin))
                                s_cinematicSettings.ProjectileCameraDistanceOffsetMin = pcDistMin;
                            break;
                        case "ProjectileCameraDistanceOffsetMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcDistMax))
                                s_cinematicSettings.ProjectileCameraDistanceOffsetMax = pcDistMax;
                            break;
                        case "ProjectileCameraChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcChance))
                                s_cinematicSettings.ProjectileCameraChance = Mathf.Clamp01(pcChance);
                            break;
                        case "ProjectileCameraChanceMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcChanceMin))
                                s_cinematicSettings.ProjectileCameraChanceMin = pcChanceMin;
                            break;
                        case "ProjectileCameraChanceMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcChanceMax))
                                s_cinematicSettings.ProjectileCameraChanceMax = pcChanceMax;
                            break;
                        case "ProjectileCameraLastEnemyOnly":
                            if (bool.TryParse(value, out var pcLast))
                                s_cinematicSettings.ProjectileCameraLastEnemyOnly = pcLast;
                            break;
                        case "ProjectileCameraSlowScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcSlow))
                                s_cinematicSettings.ProjectileCameraSlowScale = Mathf.Clamp(pcSlow, 0.01f, 1f);
                            break;
                        case "ProjectileCameraSlowScaleMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcSlowMin))
                                s_cinematicSettings.ProjectileCameraSlowScaleMin = pcSlowMin;
                            break;
                        case "ProjectileCameraSlowScaleMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcSlowMax))
                                s_cinematicSettings.ProjectileCameraSlowScaleMax = pcSlowMax;
                            break;
                        case "ProjectileCameraXOffset":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcX))
                                s_cinematicSettings.ProjectileCameraXOffset = Mathf.Clamp(pcX, -5f, 5f);
                            break;
                        case "ProjectileCameraXOffsetMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcXMin))
                                s_cinematicSettings.ProjectileCameraXOffsetMin = pcXMin;
                            break;
                        case "ProjectileCameraXOffsetMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcXMax))
                                s_cinematicSettings.ProjectileCameraXOffsetMax = pcXMax;
                            break;
                        case "ProjectileCameraLookYaw":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcYaw))
                                s_cinematicSettings.ProjectileCameraLookYaw = Mathf.Clamp(pcYaw, -90f, 90f);
                            break;
                        case "ProjectileCameraLookYawMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcYawMin))
                                s_cinematicSettings.ProjectileCameraLookYawMin = pcYawMin;
                            break;
                        case "ProjectileCameraLookYawMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcYawMax))
                                s_cinematicSettings.ProjectileCameraLookYawMax = pcYawMax;
                            break;
                        case "ProjectileCameraLookPitch":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcPitch))
                                s_cinematicSettings.ProjectileCameraLookPitch = Mathf.Clamp(pcPitch, -60f, 60f);
                            break;
                        case "ProjectileCameraLookPitchMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcPitchMin))
                                s_cinematicSettings.ProjectileCameraLookPitchMin = pcPitchMin;
                            break;
                        case "ProjectileCameraLookPitchMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcPitchMax))
                                s_cinematicSettings.ProjectileCameraLookPitchMax = pcPitchMax;
                            break;
                        case "ProjectileCameraRandomYawRange":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcRYaw))
                                s_cinematicSettings.ProjectileCameraRandomYawRange = Mathf.Clamp(pcRYaw, 0f, 90f);
                            break;
                        case "ProjectileCameraRandomYawRangeMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcRYawMin))
                                s_cinematicSettings.ProjectileCameraRandomYawRangeMin = pcRYawMin;
                            break;
                        case "ProjectileCameraRandomYawRangeMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcRYawMax))
                                s_cinematicSettings.ProjectileCameraRandomYawRangeMax = pcRYawMax;
                            break;
                        case "ProjectileCameraRandomPitchRange":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcRPitch))
                                s_cinematicSettings.ProjectileCameraRandomPitchRange = Mathf.Clamp(pcRPitch, 0f, 60f);
                            break;
                        case "ProjectileCameraRandomPitchRangeMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcRPitchMin))
                                s_cinematicSettings.ProjectileCameraRandomPitchRangeMin = pcRPitchMin;
                            break;
                        case "ProjectileCameraRandomPitchRangeMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcRPitchMax))
                                s_cinematicSettings.ProjectileCameraRandomPitchRangeMax = pcRPitchMax;
                            break;
                        case "ProjectileCameraReturnDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcReturn))
                                s_cinematicSettings.ProjectileCameraReturnDuration = Mathf.Clamp(pcReturn, 0f, 5f);
                            break;
                        case "ProjectileCameraReturnDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcReturnMin))
                                s_cinematicSettings.ProjectileCameraReturnDurationMin = pcReturnMin;
                            break;
                        case "ProjectileCameraReturnDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcReturnMax))
                                s_cinematicSettings.ProjectileCameraReturnDurationMax = pcReturnMax;
                            break;
                        case "ProjectileReturnPercent":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projReturnPct))
                                s_cinematicSettings.ProjectileReturnPercent = Mathf.Clamp01(projReturnPct);
                            break;
                        case "ProjectileReturnStart":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projReturnStart))
                                s_cinematicSettings.ProjectileReturnStart = Mathf.Max(0f, projReturnStart);
                            break;
                        case "AllowProjectileFOVBeyondDuration":
                            s_cinematicSettings.AllowProjectileFOVBeyondDuration = ParseBool(value, false);
                            break;
                        case "AllowProjectileSlowBeyondDuration":
                            s_cinematicSettings.AllowProjectileSlowBeyondDuration = ParseBool(value, false);
                            break;
                        case "ProjectileCameraFOV":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcFOV))
                                s_cinematicSettings.ProjectileCameraFOV = Mathf.Clamp(pcFOV, 0f, 60f);
                            break;
                        case "ProjectileCameraFOVMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcFOVMin))
                                s_cinematicSettings.ProjectileCameraFOVMin = pcFOVMin;
                            break;
                        case "ProjectileCameraFOVMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcFOVMax))
                                s_cinematicSettings.ProjectileCameraFOVMax = pcFOVMax;
                            break;
                        case "ProjectileCameraFOVZoomIn":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcFovIn))
                                s_cinematicSettings.ProjectileCameraFOVZoomInDuration = Mathf.Clamp(pcFovIn, 0.05f, 5f);
                            break;
                        case "ProjectileCameraFOVZoomInMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcFovInMin))
                                s_cinematicSettings.ProjectileCameraFOVZoomInDurationMin = pcFovInMin;
                            break;
                        case "ProjectileCameraFOVZoomInMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcFovInMax))
                                s_cinematicSettings.ProjectileCameraFOVZoomInDurationMax = pcFovInMax;
                            break;
                        case "ProjectileCameraFOVHold":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcFovHold))
                                s_cinematicSettings.ProjectileCameraFOVHoldDuration = Mathf.Clamp(pcFovHold, 0f, 5f);
                            break;
                        case "ProjectileCameraFOVHoldMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcFovHoldMin))
                                s_cinematicSettings.ProjectileCameraFOVHoldDurationMin = pcFovHoldMin;
                            break;
                        case "ProjectileCameraFOVHoldMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcFovHoldMax))
                                s_cinematicSettings.ProjectileCameraFOVHoldDurationMax = pcFovHoldMax;
                            break;
                        case "ProjectileCameraFOVZoomOut":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcFovOut))
                                s_cinematicSettings.ProjectileCameraFOVZoomOutDuration = Mathf.Clamp(pcFovOut, 0.05f, 5f);
                            break;
                        case "ProjectileCameraFOVZoomOutMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcFovOutMin))
                                s_cinematicSettings.ProjectileCameraFOVZoomOutDurationMin = pcFovOutMin;
                            break;
                        case "ProjectileCameraFOVZoomOutMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcFovOutMax))
                                s_cinematicSettings.ProjectileCameraFOVZoomOutDurationMax = pcFovOutMax;
                            break;
                        case "ProjectileCameraEnableVignette":
                            s_cinematicSettings.ProjectileCameraEnableVignette = ParseBool(value, true);
                            break;
                        case "ProjectileCameraVignetteIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcVignette))
                                s_cinematicSettings.ProjectileCameraVignetteIntensity = Mathf.Clamp01(pcVignette);
                            break;
                        case "ProjectileCameraVignetteIntensityMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcVignetteMin))
                                s_cinematicSettings.ProjectileCameraVignetteIntensityMin = pcVignetteMin;
                            break;
                        case "ProjectileCameraVignetteIntensityMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var pcVignetteMax))
                                s_cinematicSettings.ProjectileCameraVignetteIntensityMax = pcVignetteMax;
                            break;
                        case "RandomizeProjectileDuration":
                            s_cinematicSettings.RandomizeProjectileDuration = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileSlowScale":
                            s_cinematicSettings.RandomizeProjectileSlowScale = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileReturnDuration":
                            s_cinematicSettings.RandomizeProjectileReturnDuration = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileChance":
                            s_cinematicSettings.RandomizeProjectileChance = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileHeightOffset":
                            s_cinematicSettings.RandomizeProjectileHeightOffset = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileDistanceOffset":
                            s_cinematicSettings.RandomizeProjectileDistanceOffset = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileXOffset":
                            s_cinematicSettings.RandomizeProjectileXOffset = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileLookYaw":
                            s_cinematicSettings.RandomizeProjectileLookYaw = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileLookPitch":
                            s_cinematicSettings.RandomizeProjectileLookPitch = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileRandomYawRange":
                            s_cinematicSettings.RandomizeProjectileRandomYawRange = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileRandomPitchRange":
                            s_cinematicSettings.RandomizeProjectileRandomPitchRange = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileFOV":
                            s_cinematicSettings.RandomizeProjectileFOV = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileFOVIn":
                            s_cinematicSettings.RandomizeProjectileFOVIn = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileFOVHold":
                            s_cinematicSettings.RandomizeProjectileFOVHold = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileFOVOut":
                            s_cinematicSettings.RandomizeProjectileFOVOut = ParseBool(value, false);
                            break;
                        case "RandomizeProjectileVignetteIntensity":
                            s_cinematicSettings.RandomizeProjectileVignetteIntensity = ParseBool(value, false);
                            break;
                        case "ProjectileVisualSource":
                            s_cinematicSettings.ProjectileVisualSource = value;
                            break;
                        case "ProjectileFOVSource":
                            s_cinematicSettings.ProjectileFOVSource = value;
                            break;
                        case "ProjectileFXSource":
                            s_cinematicSettings.ProjectileFXSource = value;
                            break;
                        case "EnableProjectileHitstop":
                            s_cinematicSettings.EnableProjectileHitstop = ParseBool(value, true);
                            break;
                        case "ProjectileHitstopDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projHs))
                                s_cinematicSettings.ProjectileHitstopDuration = projHs;
                            break;
                        case "ProjectileHitstopDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projHsMin))
                                s_cinematicSettings.ProjectileHitstopDurationMin = projHsMin;
                            break;
                        case "ProjectileHitstopDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projHsMax))
                                s_cinematicSettings.ProjectileHitstopDurationMax = projHsMax;
                            break;
                        case "RandomizeProjectileHitstopDuration":
                            s_cinematicSettings.RandomizeProjectileHitstopDuration = ParseBool(value, false);
                            break;
                        case "ProjectileHitstopOnCritOnly":
                            s_cinematicSettings.ProjectileHitstopOnCritOnly = ParseBool(value, false);
                            break;
                        case "EnableProjectileSlowMoSound":
                            s_cinematicSettings.EnableProjectileSlowMoSound = ParseBool(value, true);
                            break;
                        case "ProjectileSlowMoSoundVolume":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projVol))
                                s_cinematicSettings.ProjectileSlowMoSoundVolume = Mathf.Clamp01(projVol);
                            break;
                        case "ProjectileSlowMoSoundVolumeMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projVolMin))
                                s_cinematicSettings.ProjectileSlowMoSoundVolumeMin = projVolMin;
                            break;
                        case "ProjectileSlowMoSoundVolumeMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projVolMax))
                                s_cinematicSettings.ProjectileSlowMoSoundVolumeMax = projVolMax;
                            break;
                        case "RandomizeProjectileSlowMoSoundVolume":
                            s_cinematicSettings.RandomizeProjectileSlowMoSoundVolume = ParseBool(value, false);
                            break;

                        // First person extended fields
                        case "RandomizeFirstPersonSlowScale":
                            s_cinematicSettings.RandomizeFirstPersonSlowScale = ParseBool(value, false);
                            break;
                        case "FirstPersonSlowScaleMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpSlowMin2))
                                s_cinematicSettings.FirstPersonSlowScaleMin = fpSlowMin2;
                            break;
                        case "FirstPersonSlowScaleMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpSlowMax2))
                                s_cinematicSettings.FirstPersonSlowScaleMax = fpSlowMax2;
                            break;
                        case "RandomizeFirstPersonDuration":
                            s_cinematicSettings.RandomizeFirstPersonDuration = ParseBool(value, false);
                            break;
                        case "FirstPersonReturnPercent":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpReturnPct))
                                s_cinematicSettings.FirstPersonReturnPercent = Mathf.Clamp01(fpReturnPct);
                            break;
                        case "FirstPersonReturnStart":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpReturnStart))
                                s_cinematicSettings.FirstPersonReturnStart = Mathf.Max(0f, fpReturnStart);
                            break;
                        case "FirstPersonDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpDurMin2))
                                s_cinematicSettings.FirstPersonDurationMin = fpDurMin2;
                            break;
                        case "FirstPersonDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpDurMax2))
                                s_cinematicSettings.FirstPersonDurationMax = fpDurMax2;
                            break;
                        case "EnableFirstPersonHitstop":
                            s_cinematicSettings.EnableFirstPersonHitstop = ParseBool(value, true);
                            break;
                        case "FirstPersonHitstopDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpHs))
                                s_cinematicSettings.FirstPersonHitstopDuration = fpHs;
                            break;
                        case "FirstPersonHitstopDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpHsMin))
                                s_cinematicSettings.FirstPersonHitstopDurationMin = fpHsMin;
                            break;
                        case "FirstPersonHitstopDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpHsMax))
                                s_cinematicSettings.FirstPersonHitstopDurationMax = fpHsMax;
                            break;
                        case "RandomizeFirstPersonHitstopDuration":
                            s_cinematicSettings.RandomizeFirstPersonHitstopDuration = ParseBool(value, false);
                            break;
                        case "FirstPersonHitstopOnCritOnly":
                            s_cinematicSettings.FirstPersonHitstopOnCritOnly = ParseBool(value, false);
                            break;
                        case "EnableFirstPersonSlowMoSound":
                            s_cinematicSettings.EnableFirstPersonSlowMoSound = ParseBool(value, true);
                            break;
                        case "FirstPersonSlowMoSoundVolume":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpVol))
                                s_cinematicSettings.FirstPersonSlowMoSoundVolume = Mathf.Clamp01(fpVol);
                            break;
                        case "FirstPersonSlowMoSoundVolumeMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpVolMin))
                                s_cinematicSettings.FirstPersonSlowMoSoundVolumeMin = fpVolMin;
                            break;
                        case "FirstPersonSlowMoSoundVolumeMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpVolMax))
                                s_cinematicSettings.FirstPersonSlowMoSoundVolumeMax = fpVolMax;
                            break;
                        case "RandomizeFirstPersonSlowMoSoundVolume":
                            s_cinematicSettings.RandomizeFirstPersonSlowMoSoundVolume = ParseBool(value, false);
                            break;
                        case "RandomizeFOVAmount":
                            s_cinematicSettings.RandomizeFOVAmount = ParseBool(value, false);
                            break;
                        case "FOVZoomAmountMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fovAmtMin))
                                s_cinematicSettings.FOVZoomAmountMin = fovAmtMin;
                            break;
                        case "FOVZoomAmountMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fovAmtMax))
                                s_cinematicSettings.FOVZoomAmountMax = fovAmtMax;
                            break;
                        case "RandomizeFOVIn":
                            s_cinematicSettings.RandomizeFOVIn = ParseBool(value, false);
                            break;
                        case "FOVZoomInDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fovInMin))
                                s_cinematicSettings.FOVZoomInDurationMin = fovInMin;
                            break;
                        case "FOVZoomInDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fovInMax))
                                s_cinematicSettings.FOVZoomInDurationMax = fovInMax;
                            break;
                        case "RandomizeFOVHold":
                            s_cinematicSettings.RandomizeFOVHold = ParseBool(value, false);
                            break;
                        case "FOVZoomHoldDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fovHoldMin))
                                s_cinematicSettings.FOVZoomHoldDurationMin = fovHoldMin;
                            break;
                        case "FOVZoomHoldDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fovHoldMax))
                                s_cinematicSettings.FOVZoomHoldDurationMax = fovHoldMax;
                            break;
                        case "RandomizeFOVOut":
                            s_cinematicSettings.RandomizeFOVOut = ParseBool(value, false);
                            break;
                        case "FOVZoomOutDurationMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fovOutMin))
                                s_cinematicSettings.FOVZoomOutDurationMin = fovOutMin;
                            break;
                        case "FOVZoomOutDurationMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fovOutMax))
                                s_cinematicSettings.FOVZoomOutDurationMax = fovOutMax;
                            break;
                        case "RandomizeVignetteIntensity":
                            s_cinematicSettings.RandomizeVignetteIntensity = ParseBool(value, false);
                            break;
                        case "VignetteIntensityMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var vigMin))
                                s_cinematicSettings.VignetteIntensityMin = vigMin;
                            break;
                        case "VignetteIntensityMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var vigMax))
                                s_cinematicSettings.VignetteIntensityMax = vigMax;
                            break;
                        case "RandomizeColorGradingIntensity":
                            s_cinematicSettings.RandomizeColorGradingIntensity = ParseBool(value, false);
                            break;
                        case "ColorGradingIntensityMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var cgMin))
                                s_cinematicSettings.ColorGradingIntensityMin = cgMin;
                            break;
                        case "ColorGradingIntensityMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var cgMax))
                                s_cinematicSettings.ColorGradingIntensityMax = cgMax;
                            break;
                        case "RandomizeFlashIntensity":
                            s_cinematicSettings.RandomizeFlashIntensity = ParseBool(value, false);
                            break;
                        case "FlashIntensityMin":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var flMin))
                                s_cinematicSettings.FlashIntensityMin = flMin;
                            break;
                        case "FlashIntensityMax":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var flMax))
                                s_cinematicSettings.FlashIntensityMax = flMax;
                            break;
                            
                        // ═══════════════════════════════════════════════════════════════════════
                        // HUD & NOTIFICATION SETTINGS
                        // ═══════════════════════════════════════════════════════════════════════
                        case "HUDEnabled":
                            s_cinematicSettings.HUD.Enabled = ParseBool(value, true);
                            break;
                        case "HUDOpacity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hudOp))
                                s_cinematicSettings.HUD.Opacity = Mathf.Clamp(hudOp, 0.1f, 1f);
                            break;
                        case "HUDMessageDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hudDur))
                                s_cinematicSettings.HUD.MessageDuration = Mathf.Clamp(hudDur, 1f, 10f);
                            break;
                            
                        // Toast Settings
                        case "ToastEnabled":
                            s_cinematicSettings.Toast.Enabled = ParseBool(value, true);
                            break;
                        case "ToastDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var toastDur))
                                s_cinematicSettings.Toast.Duration = Mathf.Clamp(toastDur, 0.5f, 5f);
                            break;
                        case "ToastOpacity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var toastOp))
                                s_cinematicSettings.Toast.Opacity = Mathf.Clamp(toastOp, 0.3f, 1f);
                            break;
                        case "ToastShowCinematicStart":
                            s_cinematicSettings.Toast.ShowCinematicStart = ParseBool(value, true);
                            break;
                        case "ToastShowCameraType":
                            s_cinematicSettings.Toast.ShowCameraType = ParseBool(value, true);
                            break;
                        case "ToastShowWeaponMode":
                            s_cinematicSettings.Toast.ShowWeaponMode = ParseBool(value, true);
                            break;
                        case "ToastShowSpecialTriggers":
                            s_cinematicSettings.Toast.ShowSpecialTriggers = ParseBool(value, true);
                            break;
                        case "ToastShowKillstreak":
                            s_cinematicSettings.Toast.ShowKillstreak = ParseBool(value, true);
                            break;
                            
                        // HUD Element Hiding
                        case "HideAllHUDDuringCinematic":
                            s_cinematicSettings.HUDElements.HideAllHUDDuringCinematic = ParseBool(value, false);
                            break;
                        case "HideCrosshair":
                            s_cinematicSettings.HUDElements.HideCrosshair = ParseBool(value, false);
                            break;
                        case "HideHealthBar":
                            s_cinematicSettings.HUDElements.HideHealthBar = ParseBool(value, false);
                            break;
                        case "HideCompass":
                            s_cinematicSettings.HUDElements.HideCompass = ParseBool(value, false);
                            break;
                        case "HideToolbelt":
                            s_cinematicSettings.HUDElements.HideToolbelt = ParseBool(value, false);
                            break;
                        case "HideAmmoCount":
                            s_cinematicSettings.HUDElements.HideAmmoCount = ParseBool(value, false);
                            break;
                        case "HideBuffIcons":
                            s_cinematicSettings.HUDElements.HideBuffIcons = ParseBool(value, false);
                            break;
                            
                        // ═══════════════════════════════════════════════════════════════════════
                        // PROJECTILE CAMERA SETTINGS - New settings that need loading
                        // ═══════════════════════════════════════════════════════════════════════
                        case "EnabledPresets_PC":
                            // Parse comma-separated 0/1 values for preset toggles
                            var parts = value.Split(',');
                            for (int i = 0; i < parts.Length && i < s_cinematicSettings.ProjectileCamera.EnabledPresets.Length; i++)
                            {
                                s_cinematicSettings.ProjectileCamera.EnabledPresets[i] = parts[i].Trim() == "1";
                            }
                            break;
                        case "EnabledPresetsBasicKill_PC":
                            var basicPresetParts = value.Split(',');
                            for (int i = 0; i < basicPresetParts.Length && i < s_cinematicSettings.ProjectileCamera.EnabledPresetsBasicKill.Length; i++)
                            {
                                s_cinematicSettings.ProjectileCamera.EnabledPresetsBasicKill[i] = basicPresetParts[i].Trim() == "1";
                            }
                            break;
                        case "EnabledPresetsTriggers_PC":
                            var triggerPresetParts = value.Split(',');
                            for (int i = 0; i < triggerPresetParts.Length && i < s_cinematicSettings.ProjectileCamera.EnabledPresetsTriggers.Length; i++)
                            {
                                s_cinematicSettings.ProjectileCamera.EnabledPresetsTriggers[i] = triggerPresetParts[i].Trim() == "1";
                            }
                            break;
                        // Dynamic Zoom (ADS simulation)
                        case "PC_EnableDynamicZoomIn":
                            s_cinematicSettings.ProjectileCamera.EnableDynamicZoomIn = ParseBool(value, false);
                            break;
                        case "PC_EnableDynamicZoomOut":
                            s_cinematicSettings.ProjectileCamera.EnableDynamicZoomOut = ParseBool(value, false);
                            break;
                        case "PC_DynamicZoomBalance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dzBalance))
                                s_cinematicSettings.ProjectileCamera.DynamicZoomBalance = Mathf.Clamp(dzBalance, 10f, 90f);
                            break;
                        case "UseStandardPresets_PC":
                            s_cinematicSettings.ProjectileCamera.UseStandardPresets = ParseBool(value, true);
                            break;
                        case "RandomizeTilt_PC":
                            s_cinematicSettings.ProjectileCamera.RandomizeTilt = ParseBool(value, false);
                            break;
                        case "RandomTiltRange_PC":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tiltRange))
                                s_cinematicSettings.ProjectileCamera.RandomTiltRange = Mathf.Clamp(tiltRange, 1f, 45f);
                            break;
                        case "RandomizeSideOffset_PC":
                            s_cinematicSettings.ProjectileCamera.RandomizeSideOffset = ParseBool(value, false);
                            break;
                        case "SideOffsetWide_PC":
                            s_cinematicSettings.ProjectileCamera.SideOffsetWide = ParseBool(value, false);
                            break;
                        case "SideOffsetStandard_PC":
                            s_cinematicSettings.ProjectileCamera.SideOffsetStandard = ParseBool(value, true);
                            break;
                        case "SideOffsetTight_PC":
                            s_cinematicSettings.ProjectileCamera.SideOffsetTight = ParseBool(value, false);
                            break;
                        case "TD_Chance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdChance))
                                s_cinematicSettings.TriggerDefaults.Chance = Mathf.Clamp(tdChance, 0f, 100f);
                            break;
                        case "TD_Cooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var tdCooldown))
                                s_cinematicSettings.TriggerDefaults.Cooldown = Mathf.Clamp(tdCooldown, 0f, 60f);
                            break;
                            
                        // ═══════════════════════════════════════════════════════════════════════
                        // FREEZE FRAME SETTINGS (Per-camera: FP and Projectile)
                        // ═══════════════════════════════════════════════════════════════════════
                        
                        // FP Freeze Frame
                        case "FPFreeze_Enabled":
                            s_cinematicSettings.FPFreezeFrame.Enabled = ParseBool(value, false);
                            break;
                        case "FPFreeze_Chance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpFreezeChance))
                                s_cinematicSettings.FPFreezeFrame.Chance = Mathf.Clamp(fpFreezeChance, 0f, 100f);
                            break;
                        case "FPFreeze_Duration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpFreezeDur))
                                s_cinematicSettings.FPFreezeFrame.Duration = Mathf.Clamp(fpFreezeDur, 0.1f, 5f);
                            break;
                        case "FPFreeze_Delay":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpFreezeDelay))
                                s_cinematicSettings.FPFreezeFrame.Delay = Mathf.Clamp(fpFreezeDelay, 0f, 2f);
                            break;
                        case "FPFreeze_TriggerOnBasicKill":
                            s_cinematicSettings.FPFreezeFrame.TriggerOnBasicKill = ParseBool(value, true);
                            break;
                        case "FPFreeze_TriggerOnSpecialTrigger":
                            s_cinematicSettings.FPFreezeFrame.TriggerOnSpecialTrigger = ParseBool(value, true);
                            break;
                        case "FPFreeze_EnableCameraMovement":
                            s_cinematicSettings.FPFreezeFrame.EnableCameraMovement = ParseBool(value, true);
                            break;
                        case "FPFreeze_TimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpFreezeScale))
                                s_cinematicSettings.FPFreezeFrame.TimeScale = Mathf.Clamp(fpFreezeScale, 0.01f, 0.1f);
                            break;
                        case "FPFreeze_PostAction":
                            if (int.TryParse(value, out var fpPostAction))
                                s_cinematicSettings.FPFreezeFrame.PostAction = (PostFreezeAction)Mathf.Clamp(fpPostAction, 0, 3);
                            break;
                        case "FPFreeze_EnableContrastEffect":
                            s_cinematicSettings.FPFreezeFrame.EnableContrastEffect = ParseBool(value, true);
                            break;
                        case "FPFreeze_ContrastAmount":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fpContrast))
                                s_cinematicSettings.FPFreezeFrame.ContrastAmount = Mathf.Clamp(fpContrast, 1f, 1.8f);
                            break;
                        
                        // Projectile Freeze Frame
                        case "ProjFreeze_Enabled":
                            s_cinematicSettings.ProjectileFreezeFrame.Enabled = ParseBool(value, false);
                            break;
                        case "ProjFreeze_Chance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projFreezeChance))
                                s_cinematicSettings.ProjectileFreezeFrame.Chance = Mathf.Clamp(projFreezeChance, 0f, 100f);
                            break;
                        case "ProjFreeze_Duration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projFreezeDur))
                                s_cinematicSettings.ProjectileFreezeFrame.Duration = Mathf.Clamp(projFreezeDur, 0.1f, 5f);
                            break;
                        case "ProjFreeze_Delay":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projFreezeDelay))
                                s_cinematicSettings.ProjectileFreezeFrame.Delay = Mathf.Clamp(projFreezeDelay, 0f, 2f);
                            break;
                        case "ProjFreeze_TriggerOnBasicKill":
                            s_cinematicSettings.ProjectileFreezeFrame.TriggerOnBasicKill = ParseBool(value, true);
                            break;
                        case "ProjFreeze_TriggerOnSpecialTrigger":
                            s_cinematicSettings.ProjectileFreezeFrame.TriggerOnSpecialTrigger = ParseBool(value, true);
                            break;
                        case "ProjFreeze_EnableCameraMovement":
                            s_cinematicSettings.ProjectileFreezeFrame.EnableCameraMovement = ParseBool(value, true);
                            break;
                        case "ProjFreeze_TimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projFreezeScale))
                                s_cinematicSettings.ProjectileFreezeFrame.TimeScale = Mathf.Clamp(projFreezeScale, 0.01f, 0.1f);
                            break;
                        case "ProjFreeze_RandomizePreset":
                            s_cinematicSettings.ProjectileFreezeFrame.RandomizePreset = ParseBool(value, true);
                            break;
                        case "ProjFreeze_PostAction":
                            if (int.TryParse(value, out var projPostAction))
                                s_cinematicSettings.ProjectileFreezeFrame.PostAction = (PostFreezeAction)Mathf.Clamp(projPostAction, 0, 3);
                            break;
                        case "ProjFreeze_RandomizePostCamera":
                            s_cinematicSettings.ProjectileFreezeFrame.RandomizePostCamera = ParseBool(value, true);
                            break;
                        case "ProjFreeze_EnableContrastEffect":
                            s_cinematicSettings.ProjectileFreezeFrame.EnableContrastEffect = ParseBool(value, true);
                            break;
                        case "ProjFreeze_ContrastAmount":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var projContrast))
                                s_cinematicSettings.ProjectileFreezeFrame.ContrastAmount = Mathf.Clamp(projContrast, 1f, 1.8f);
                            break;
                            
                        // Kill Flash & Kill Vignette (Screen Effects)
                        case "Effects_EnableKillFlash":
                            s_cinematicSettings.ScreenEffects.EnableKillFlash = ParseBool(value, false);
                            break;
                        case "Effects_KillFlashDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var flashDur))
                                s_cinematicSettings.ScreenEffects.KillFlashDuration = Mathf.Clamp(flashDur, 0.1f, 2f);
                            break;
                        case "Effects_KillFlashIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var killFlashInt))
                                s_cinematicSettings.ScreenEffects.KillFlashIntensity = Mathf.Clamp(killFlashInt, 0.5f, 3f);
                            break;
                        case "Effects_EnableKillVignette":
                            s_cinematicSettings.ScreenEffects.EnableKillVignette = ParseBool(value, false);
                            break;
                        case "Effects_KillVignetteDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var vigDur))
                                s_cinematicSettings.ScreenEffects.KillVignetteDuration = Mathf.Clamp(vigDur, 0.5f, 5f);
                            break;
                        case "Effects_KillVignetteIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var killVigInt))
                                s_cinematicSettings.ScreenEffects.KillVignetteIntensity = Mathf.Clamp(killVigInt, 0.5f, 3f);
                            break;
                            
                        // X-Ray Vision (legacy - keep for backward compatibility)
                        case "Exp_EnableXRayVision":
                            s_cinematicSettings.Experimental.EnableXRayVision = ParseBool(value, false);
                            break;
                        case "Exp_XRayDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var xrayDur))
                                s_cinematicSettings.Experimental.XRayDuration = Mathf.Clamp(xrayDur, 0.1f, 2f);
                            break;
                        case "Exp_XRayIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var xrayInt))
                                s_cinematicSettings.Experimental.XRayIntensity = Mathf.Clamp(xrayInt, 0.5f, 2f);
                            break;
                            
                        // Predator Vision
                        case "Exp_EnablePredatorVision":
                            s_cinematicSettings.Experimental.EnablePredatorVision = ParseBool(value, false);
                            break;
                        case "Exp_PredatorVisionDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var predDur))
                                s_cinematicSettings.Experimental.PredatorVisionDuration = Mathf.Clamp(predDur, 0.5f, 3f);
                            break;
                        case "Exp_PredatorVisionIntensity":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var predInt))
                                s_cinematicSettings.Experimental.PredatorVisionIntensity = Mathf.Clamp(predInt, 0.3f, 1f);
                            break;
                            
                        // Projectile Ride Cam
                        case "Exp_EnableProjectileRideCam":
                            s_cinematicSettings.Experimental.EnableProjectileRideCam = ParseBool(value, false);
                            break;
                        case "Exp_RideCamFOV":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rideFov))
                                s_cinematicSettings.Experimental.RideCamFOV = Mathf.Clamp(rideFov, 60f, 120f);
                            break;
                        case "Exp_RideCamChance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rideChance))
                                s_cinematicSettings.Experimental.RideCamChance = Mathf.Clamp(rideChance, 0f, 100f);
                            break;
                        case "Exp_RideCamOffset":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rideOffset))
                                s_cinematicSettings.Experimental.RideCamOffset = Mathf.Clamp(rideOffset, 0.5f, 5f);
                            break;
                        case "Exp_RideCamPredictiveAiming":
                            s_cinematicSettings.Experimental.RideCamPredictiveAiming = ParseBool(value, true);
                            break;
                        case "Exp_RideCamMinTargetHealth":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rideMinHP))
                                s_cinematicSettings.Experimental.RideCamMinTargetHealth = Mathf.Clamp(rideMinHP, 0f, 200f);
                            break;
                            
                        // Dismemberment Focus Cam
                        case "Exp_EnableDismemberFocusCam":
                            s_cinematicSettings.Experimental.EnableDismemberFocusCam = ParseBool(value, false);
                            break;
                        case "Exp_FocusCamDistance":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var focusDist))
                                s_cinematicSettings.Experimental.FocusCamDistance = Mathf.Clamp(focusDist, 0.5f, 4f);
                            break;
                        case "Exp_FocusCamDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var focusDur))
                                s_cinematicSettings.Experimental.FocusCamDuration = Mathf.Clamp(focusDur, 0.5f, 3f);
                            break;
                            
                        // Last Stand / Second Wind
                        case "Exp_EnableLastStand":
                            s_cinematicSettings.Experimental.EnableLastStand = ParseBool(value, false);
                            break;
                        case "Exp_LastStandDuration":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lsDur))
                                s_cinematicSettings.Experimental.LastStandDuration = Mathf.Clamp(lsDur, 1f, 10f);
                            break;
                        case "Exp_LastStandTimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lsScale))
                                s_cinematicSettings.Experimental.LastStandTimeScale = Mathf.Clamp(lsScale, 0.05f, 0.3f);
                            break;
                        case "Exp_LastStandReviveHealth":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lsHealth))
                                s_cinematicSettings.Experimental.LastStandReviveHealth = Mathf.Clamp(lsHealth, 10f, 50f);
                            break;
                        case "Exp_LastStandCooldown":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lsCd))
                                s_cinematicSettings.Experimental.LastStandCooldown = Mathf.Clamp(lsCd, 30f, 120f);
                            break;
                        case "Exp_LastStandInfiniteAmmo":
                            s_cinematicSettings.Experimental.LastStandInfiniteAmmo = ParseBool(value, true);
                            break;
                            
                        // Chain Reaction
                        case "Exp_EnableChainReaction":
                            s_cinematicSettings.Experimental.EnableChainReaction = ParseBool(value, false);
                            break;
                        case "Exp_ChainReactionWindow":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var chainWin))
                                s_cinematicSettings.Experimental.ChainReactionWindow = Mathf.Clamp(chainWin, 0.5f, 5f);
                            break;
                        case "Exp_ChainReactionMaxKills":
                            if (int.TryParse(value, out var chainMax))
                                s_cinematicSettings.Experimental.ChainReactionMaxKills = Mathf.Clamp(chainMax, 2, 10);
                            break;
                        case "Exp_ChainCameraTransitionTime":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var chainTrans))
                                s_cinematicSettings.Experimental.ChainCameraTransitionTime = Mathf.Clamp(chainTrans, 0.2f, 1f);
                            break;
                        case "Exp_ChainReactionSlowMoRamp":
                            s_cinematicSettings.Experimental.ChainReactionSlowMoRamp = ParseBool(value, true);
                            break;
                        case "Exp_ChainSlowMoMultiplier":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var chainMult))
                                s_cinematicSettings.Experimental.ChainSlowMoMultiplier = Mathf.Clamp(chainMult, 0.5f, 0.9f);
                            break;
                            
                        // Slow-Mo Toggle
                        case "Exp_EnableSlowMoToggle":
                            s_cinematicSettings.Experimental.EnableSlowMoToggle = ParseBool(value, false);
                            break;
                        case "Exp_SlowMoToggleKey":
                            if (Enum.TryParse<KeyCode>(value, out var slowMoKey))
                                s_cinematicSettings.Experimental.SlowMoToggleKey = slowMoKey;
                            break;
                        case "Exp_SlowMoToggleTimeScale":
                            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var slowMoScale))
                                s_cinematicSettings.Experimental.SlowMoToggleTimeScale = Mathf.Clamp(slowMoScale, 0.1f, 0.5f);
                            break;
                    }
                }
                
                // Update context manager settings (only if instance exists)
                var ctx = CinematicContextManager.Instance;
                if (ctx != null)
                {
                    // Use TriggerSystem.DistanceThreshold if available, else fall back to root setting
                    ctx.DistanceThreshold = s_cinematicSettings.MenuV2?.TriggerSystem?.DistanceThreshold > 0 
                        ? s_cinematicSettings.MenuV2.TriggerSystem.DistanceThreshold 
                        : s_cinematicSettings.DistanceThreshold;
                    ctx.LowHealthThreshold = s_cinematicSettings.LowHealthThreshold;
                    ctx.LongRangeZoomMultiplier = s_cinematicSettings.LongRangeZoomMultiplier;
                    ctx.LowHealthSlowScale = s_cinematicSettings.LowHealthSlowScale;
                    ctx.CritZoomMultiplier = s_cinematicSettings.CritZoomMultiplier;
                    ctx.CritZoomSpeed = s_cinematicSettings.CritZoomSpeed;
                    ctx.LongRangeZoomSpeed = s_cinematicSettings.LongRangeZoomSpeed;
                    ctx.ColorGradingMode = s_cinematicSettings.ColorGradingMode.ToString();
                    ctx.ColorGradingIntensity = s_cinematicSettings.ColorGradingIntensity;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"CinematicKill: Failed to read settings: {ex.Message}");
            }

            // NOTE: MenuV2 is no longer cached - settings changes are immediately reflected
            
            s_cinematicSettings.Clamp();
            ApplyDurationCaps(s_cinematicSettings);
            
            // Use BasicKill values as the primary source for runtime
            s_slowScale = s_cinematicSettings.BasicKill.TimeScale;
            s_duration = s_cinematicSettings.BasicKill.Duration;
            s_killcamChance = s_cinematicSettings.BasicKill.Chance / 100f;
            s_enabled = s_cinematicSettings.EnableCinematics;
            s_ignoreCorpseHits = s_cinematicSettings.IgnoreCorpseHits;
            CKLog.Verbose($" Loaded settings - BasicKill: Duration={s_duration:F2}s, TimeScale={s_slowScale:F2}x, Chance={s_killcamChance*100:F0}%, FP={s_cinematicSettings.BasicKill.FirstPersonCamera}, Proj={s_cinematicSettings.BasicKill.ProjectileCamera}");
            
            // Refresh menu with loaded settings
            if (s_menuComponent != null)
            {
                s_menuComponent.RefreshSettings();
            }
        }

        public static CinematicKillSettings GetSettings()
        {
            // Return live settings directly - changes take effect immediately
            // The menu and runtime now use the same settings object
            return s_cinematicSettings;
        }
        
        /// <summary>
        /// Returns the live settings reference for direct modification.
        /// Use this for Gears bindings and other external systems that need to modify settings directly.
        /// For UI/menu usage, use GetSettings() which returns a clone.
        /// </summary>
        public static CinematicKillSettings GetLiveSettings()
        {
            return s_cinematicSettings;
        }
        
        /// <summary>
        /// Returns the current settings reference for screen effects restoration.
        /// </summary>
        public static CinematicKillSettings GetCurrentSettings()
        {
            return s_cinematicSettings;
        }

        /// <summary>
        /// Saves the current live settings to the config file.
        /// Called automatically when menu closes.
        /// </summary>
        public static void SaveSettingsToFile()
        {
            // Just call SaveSettingsFromMenu with the current live settings
            // This ensures all the sync and clamping logic runs
            SaveSettingsFromMenu(s_cinematicSettings);
        }

        /// <summary>
        /// Resets settings to defaults and saves to file.
        /// </summary>
        public static void ResetSettingsToDefaults()
        {
            s_cinematicSettings = new CinematicKillSettings();
            SaveSettingsToFile();
        }

        public static void SaveSettingsFromMenu(CinematicKillSettings newSettings)
        {
            if (newSettings == null) return;

            // NOTE: MenuV2 is no longer cached, so no need to invalidate
            // Settings changes are immediately reflected via fresh MenuV2 instances

            s_cinematicSettings = newSettings;
            
            s_cinematicSettings.Clamp();
            ApplyDurationCaps(s_cinematicSettings);

            // Sync context manager with new settings
            var ctx = CinematicContextManager.Instance;
            if (ctx != null)
            {
                // Use TriggerSystem.DistanceThreshold if available, else fall back to root setting
                ctx.DistanceThreshold = s_cinematicSettings.MenuV2?.TriggerSystem?.DistanceThreshold > 0 
                    ? s_cinematicSettings.MenuV2.TriggerSystem.DistanceThreshold 
                    : s_cinematicSettings.DistanceThreshold;
                ctx.LowHealthThreshold = s_cinematicSettings.LowHealthThreshold;
                ctx.LongRangeZoomMultiplier = s_cinematicSettings.LongRangeZoomMultiplier;
                ctx.LowHealthSlowScale = s_cinematicSettings.LowHealthSlowScale;
                ctx.CritZoomMultiplier = s_cinematicSettings.CritZoomMultiplier;
                ctx.CritZoomSpeed = s_cinematicSettings.CritZoomSpeed;
                ctx.LongRangeZoomSpeed = s_cinematicSettings.LongRangeZoomSpeed;
                ctx.ColorGradingMode = s_cinematicSettings.ColorGradingMode.ToString();
                ctx.ColorGradingIntensity = s_cinematicSettings.ColorGradingIntensity;
            }

            // Update local fields that mirror settings
            s_enabled = s_cinematicSettings.EnableCinematics;
            s_slowScale = s_cinematicSettings.FirstPersonSlowScale;
            s_duration = s_cinematicSettings.FirstPersonDuration;
            s_cinematicSettings.FirstPersonCameraChance = Mathf.Clamp01(s_cinematicSettings.FirstPersonCameraChance);
            s_killcamChance = s_cinematicSettings.KillcamChance;
            s_ignoreCorpseHits = s_cinematicSettings.IgnoreCorpseHits;
            // The XML has "Enabled" but Settings class doesn't have it explicitly as a field, it's static in Manager.
            // For now, let's assume Enabled is handled separately or we should add it to Settings class.
            // Actually, let's just save the fields we have in Settings.

            if (string.IsNullOrEmpty(s_configPath)) return;

            try
            {
                XDocument doc = new XDocument(new XElement("CinematicKillSettings"));
                XElement root = doc.Root;

                root.Add(new XComment(" General "));
                root.Add(CreateProperty("EnableCinematics", s_cinematicSettings.EnableCinematics));
                root.Add(CreateProperty("EnableTriggers", s_cinematicSettings.EnableTriggers));
                root.Add(CreateProperty("KillcamChance", s_cinematicSettings.KillcamChance));
                root.Add(CreateProperty("EnableGlobalCooldown", s_cinematicSettings.EnableGlobalCooldown));
                root.Add(CreateProperty("CooldownGlobal", s_cinematicSettings.CooldownGlobal));
                root.Add(CreateProperty("IgnoreGlobalCooldownOnLastEnemy", s_cinematicSettings.IgnoreGlobalCooldownOnLastEnemy));
                root.Add(CreateProperty("AdvancedMode", s_cinematicSettings.AdvancedMode));
                root.Add(CreateProperty("IgnoreCorpseHits", s_cinematicSettings.IgnoreCorpseHits));
                root.Add(CreateProperty("EnemyScanRadius", s_cinematicSettings.EnemyScanRadius));
                root.Add(CreateProperty("EnableFirstPersonCamera", s_cinematicSettings.EnableFirstPersonCamera));
                root.Add(CreateProperty("FirstPersonCameraChance", s_cinematicSettings.FirstPersonCameraChance));
                root.Add(CreateProperty("FirstPersonCameraChanceMin", s_cinematicSettings.FirstPersonCameraChanceMin));
                root.Add(CreateProperty("FirstPersonCameraChanceMax", s_cinematicSettings.FirstPersonCameraChanceMax));
                root.Add(CreateProperty("RandomizeFirstPersonCameraChance", s_cinematicSettings.RandomizeFirstPersonCameraChance));
                root.Add(CreateProperty("AllowLastEnemyFirstPerson", s_cinematicSettings.AllowLastEnemyFirstPerson));
                root.Add(CreateProperty("AllowLastEnemyProjectile", s_cinematicSettings.AllowLastEnemyProjectile));
                root.Add(CreateProperty("OverrideLastEnemyFirstPerson", s_cinematicSettings.OverrideLastEnemyFirstPerson));
                root.Add(CreateProperty("LastEnemyFirstPersonSlowScale", s_cinematicSettings.LastEnemyFirstPersonSlowScale));
                root.Add(CreateProperty("LastEnemyFirstPersonSlowScaleMin", s_cinematicSettings.LastEnemyFirstPersonSlowScaleMin));
                root.Add(CreateProperty("LastEnemyFirstPersonSlowScaleMax", s_cinematicSettings.LastEnemyFirstPersonSlowScaleMax));
                root.Add(CreateProperty("RandomizeLastEnemyFirstPersonSlowScale", s_cinematicSettings.RandomizeLastEnemyFirstPersonSlowScale));
                root.Add(CreateProperty("LastEnemyFirstPersonDuration", s_cinematicSettings.LastEnemyFirstPersonDuration));
                root.Add(CreateProperty("LastEnemyFirstPersonDurationMin", s_cinematicSettings.LastEnemyFirstPersonDurationMin));
                root.Add(CreateProperty("LastEnemyFirstPersonDurationMax", s_cinematicSettings.LastEnemyFirstPersonDurationMax));
                root.Add(CreateProperty("RandomizeLastEnemyFirstPersonDuration", s_cinematicSettings.RandomizeLastEnemyFirstPersonDuration));
                root.Add(CreateProperty("OverrideLastEnemyProjectile", s_cinematicSettings.OverrideLastEnemyProjectile));
                root.Add(CreateProperty("LastEnemyProjectileSlowScale", s_cinematicSettings.LastEnemyProjectileSlowScale));
                root.Add(CreateProperty("LastEnemyProjectileSlowScaleMin", s_cinematicSettings.LastEnemyProjectileSlowScaleMin));
                root.Add(CreateProperty("LastEnemyProjectileSlowScaleMax", s_cinematicSettings.LastEnemyProjectileSlowScaleMax));
                root.Add(CreateProperty("RandomizeLastEnemyProjectileSlowScale", s_cinematicSettings.RandomizeLastEnemyProjectileSlowScale));
                root.Add(CreateProperty("LastEnemyProjectileDuration", s_cinematicSettings.LastEnemyProjectileDuration));
                root.Add(CreateProperty("LastEnemyProjectileDurationMin", s_cinematicSettings.LastEnemyProjectileDurationMin));
                root.Add(CreateProperty("LastEnemyProjectileDurationMax", s_cinematicSettings.LastEnemyProjectileDurationMax));
                root.Add(CreateProperty("RandomizeLastEnemyProjectileDuration", s_cinematicSettings.RandomizeLastEnemyProjectileDuration));
                root.Add(CreateProperty("LastEnemyProjectileReturnDuration", s_cinematicSettings.LastEnemyProjectileReturnDuration));
                root.Add(CreateProperty("LastEnemyProjectileReturnDurationMin", s_cinematicSettings.LastEnemyProjectileReturnDurationMin));
                root.Add(CreateProperty("LastEnemyProjectileReturnDurationMax", s_cinematicSettings.LastEnemyProjectileReturnDurationMax));
                root.Add(CreateProperty("RandomizeLastEnemyProjectileReturnDuration", s_cinematicSettings.RandomizeLastEnemyProjectileReturnDuration));
                root.Add(CreateProperty("EnableFirstPersonSlowMo", s_cinematicSettings.EnableFirstPersonSlowMo));
                root.Add(CreateProperty("FirstPersonSlowScale", s_cinematicSettings.FirstPersonSlowScale));
                root.Add(CreateProperty("RandomizeFirstPersonSlowScale", s_cinematicSettings.RandomizeFirstPersonSlowScale));
                root.Add(CreateProperty("FirstPersonSlowScaleMin", s_cinematicSettings.FirstPersonSlowScaleMin));
                root.Add(CreateProperty("FirstPersonSlowScaleMax", s_cinematicSettings.FirstPersonSlowScaleMax));
                root.Add(CreateProperty("FirstPersonDuration", s_cinematicSettings.FirstPersonDuration));
                root.Add(CreateProperty("RandomizeFirstPersonDuration", s_cinematicSettings.RandomizeFirstPersonDuration));
                root.Add(CreateProperty("FirstPersonDurationMin", s_cinematicSettings.FirstPersonDurationMin));
                root.Add(CreateProperty("FirstPersonDurationMax", s_cinematicSettings.FirstPersonDurationMax));
                root.Add(CreateProperty("FirstPersonReturnPercent", s_cinematicSettings.FirstPersonReturnPercent));
                root.Add(CreateProperty("FirstPersonReturnStart", s_cinematicSettings.FirstPersonReturnStart));
                root.Add(CreateProperty("EnableProjectileCamera", s_cinematicSettings.EnableProjectileCamera));
                root.Add(CreateProperty("EnableProjectileSlowMo", s_cinematicSettings.EnableProjectileSlowMo));
                root.Add(CreateProperty("AllowFOVBeyondDuration", s_cinematicSettings.AllowFOVBeyondDuration));
                root.Add(CreateProperty("AllowProjectileFOVBeyondDuration", s_cinematicSettings.AllowProjectileFOVBeyondDuration));
                root.Add(CreateProperty("AllowProjectileSlowBeyondDuration", s_cinematicSettings.AllowProjectileSlowBeyondDuration));

                // Ragdoll Floor Hit Settings - Per camera type
                root.Add(new XComment(" Ragdoll Floor Hit Settings "));
                root.Add(CreateProperty("EnableDynamicRagdollDuration_BK_FP", s_cinematicSettings.EnableDynamicRagdollDuration_BK_FP));
                root.Add(CreateProperty("EnableDynamicRagdollDuration_BK_Proj", s_cinematicSettings.EnableDynamicRagdollDuration_BK_Proj));
                root.Add(CreateProperty("EnableDynamicRagdollDuration_TD_FP", s_cinematicSettings.EnableDynamicRagdollDuration_TD_FP));
                root.Add(CreateProperty("EnableDynamicRagdollDuration_TD_Proj", s_cinematicSettings.EnableDynamicRagdollDuration_TD_Proj));
                root.Add(CreateProperty("RagdollPostLandDelay_BK_FP", s_cinematicSettings.RagdollPostLandDelay_BK_FP));
                root.Add(CreateProperty("RagdollPostLandDelay_BK_Proj", s_cinematicSettings.RagdollPostLandDelay_BK_Proj));
                root.Add(CreateProperty("RagdollPostLandDelay_TD_FP", s_cinematicSettings.RagdollPostLandDelay_TD_FP));
                root.Add(CreateProperty("RagdollPostLandDelay_TD_Proj", s_cinematicSettings.RagdollPostLandDelay_TD_Proj));
                root.Add(CreateProperty("RagdollFallbackDuration", s_cinematicSettings.RagdollFallbackDuration));

                // Save Mode Settings from MenuV2 (UI must initialize)
                root.Add(new XComment(" Weapon Mode Settings "));
                var menuV2 = s_cinematicSettings.MenuV2;
                
                // Melee Mode
                root.Add(CreateProperty("MeleeTriggerChance", menuV2.Melee.TriggerChancePercent));
                root.Add(CreateProperty("MeleeEnabled", menuV2.Melee.Enabled));
                root.Add(CreateProperty("MeleeOverrideDuration", menuV2.Melee.OverrideGlobalSlowMo));
                root.Add(CreateProperty("MeleeDurationSeconds", menuV2.Melee.OverrideDurationSeconds));
                root.Add(CreateProperty("MeleeUseFirstPerson", menuV2.Melee.UseFirstPersonCamera));
                root.Add(CreateProperty("MeleeUseProjectile", menuV2.Melee.UseProjectileCamera));
                
                // Ranged Mode
                root.Add(CreateProperty("RangedTriggerChance", menuV2.Ranged.TriggerChancePercent));
                root.Add(CreateProperty("RangedEnabled", menuV2.Ranged.Enabled));
                root.Add(CreateProperty("RangedOverrideDuration", menuV2.Ranged.OverrideGlobalSlowMo));
                root.Add(CreateProperty("RangedDurationSeconds", menuV2.Ranged.OverrideDurationSeconds));
                root.Add(CreateProperty("RangedUseFirstPerson", menuV2.Ranged.UseFirstPersonCamera));
                root.Add(CreateProperty("RangedUseProjectile", menuV2.Ranged.UseProjectileCamera));
                
                // Explosive/Dismember Mode
                root.Add(CreateProperty("ExplosiveTriggerChance", menuV2.Dismember.TriggerChancePercent));
                root.Add(CreateProperty("ExplosiveEnabled", menuV2.Dismember.Enabled));
                root.Add(CreateProperty("ExplosiveOverrideDuration", menuV2.Dismember.OverrideGlobalSlowMo));
                root.Add(CreateProperty("ExplosiveDurationSeconds", menuV2.Dismember.OverrideDurationSeconds));
                root.Add(CreateProperty("ExplosiveUseFirstPerson", menuV2.Dismember.UseFirstPersonCamera));
                root.Add(CreateProperty("ExplosiveUseProjectile", menuV2.Dismember.UseProjectileCamera));
                
                // Contextual Trigger Settings (UI must initialize)
                root.Add(new XComment(" Contextual Trigger Settings "));
                var ts = menuV2.TriggerSystem;
                
                root.Add(CreateProperty("TriggerDistanceThreshold", ts.DistanceThreshold));
                
                // Trigger bonus settings
                root.Add(CreateProperty("TriggerBonusDuration", ts.TriggerBonusDuration));
                root.Add(CreateProperty("TriggerSlowReduction", ts.TriggerSlowReduction));
                root.Add(CreateProperty("RequireTriggerForCinematic", ts.RequireTriggerForCinematic));
                root.Add(CreateProperty("StackTriggerBonuses", ts.StackTriggerBonuses));
                
                // LastEnemy trigger
                root.Add(CreateProperty("LastEnemyEnabled", ts.LastEnemy.Enabled));
                root.Add(CreateProperty("LastEnemyPriority", ts.LastEnemy.Priority));
                root.Add(CreateProperty("LastEnemyCooldown", ts.LastEnemy.CooldownSeconds));
                root.Add(CreateProperty("LastEnemyAllowFirstPerson", ts.LastEnemy.AllowFirstPerson));
                root.Add(CreateProperty("LastEnemyAllowProjectile", ts.LastEnemy.AllowProjectile));
                root.Add(CreateProperty("LastEnemyFirstPersonChance", ts.LastEnemy.FirstPersonChance));
                root.Add(CreateProperty("LastEnemyProjectileChance", ts.LastEnemy.ProjectileChance));
                root.Add(CreateProperty("LastEnemyOverrideScreenEffects", ts.LastEnemy.OverrideScreenEffects));
                root.Add(CreateProperty("LastEnemyEnableVignette", ts.LastEnemy.EnableVignette));
                root.Add(CreateProperty("LastEnemyVignetteIntensity", ts.LastEnemy.VignetteIntensity));
                root.Add(CreateProperty("LastEnemyEnableDesaturation", ts.LastEnemy.EnableDesaturation));
                root.Add(CreateProperty("LastEnemyDesaturationAmount", ts.LastEnemy.DesaturationAmount));
                root.Add(CreateProperty("LastEnemyEnableBloodSplatter", ts.LastEnemy.EnableBloodSplatter));
                root.Add(CreateProperty("LastEnemyEnableRadialBlur", ts.LastEnemy.EnableRadialBlur));
                root.Add(CreateProperty("LastEnemyRadialBlurIntensity", ts.LastEnemy.RadialBlurIntensity));
                
                // Killstreak trigger
                root.Add(CreateProperty("KillstreakTriggerEnabled", ts.Killstreak.Enabled));
                root.Add(CreateProperty("KillstreakPriority", ts.Killstreak.Priority));
                root.Add(CreateProperty("KillstreakCooldown", ts.Killstreak.CooldownSeconds));
                root.Add(CreateProperty("KillstreakAllowFirstPerson", ts.Killstreak.AllowFirstPerson));
                root.Add(CreateProperty("KillstreakAllowProjectile", ts.Killstreak.AllowProjectile));
                root.Add(CreateProperty("KillstreakFirstPersonChance", ts.Killstreak.FirstPersonChance));
                root.Add(CreateProperty("KillstreakProjectileChance", ts.Killstreak.ProjectileChance));
                root.Add(CreateProperty("KillstreakOverrideScreenEffects", ts.Killstreak.OverrideScreenEffects));
                
                // DismemberKill trigger
                root.Add(CreateProperty("DismemberTriggerEnabled", ts.DismemberKill.Enabled));
                root.Add(CreateProperty("DismemberTriggerPriority", ts.DismemberKill.Priority));
                root.Add(CreateProperty("DismemberTriggerCooldown", ts.DismemberKill.CooldownSeconds));
                root.Add(CreateProperty("DismemberTriggerAllowFirstPerson", ts.DismemberKill.AllowFirstPerson));
                root.Add(CreateProperty("DismemberTriggerAllowProjectile", ts.DismemberKill.AllowProjectile));
                root.Add(CreateProperty("DismemberFirstPersonChance", ts.DismemberKill.FirstPersonChance));
                root.Add(CreateProperty("DismemberProjectileChance", ts.DismemberKill.ProjectileChance));
                root.Add(CreateProperty("DismemberOverrideScreenEffects", ts.DismemberKill.OverrideScreenEffects));
                
                // Headshot trigger
                root.Add(CreateProperty("HeadshotEnabled", ts.Headshot.Enabled));
                root.Add(CreateProperty("HeadshotPriority", ts.Headshot.Priority));
                root.Add(CreateProperty("HeadshotCooldown", ts.Headshot.CooldownSeconds));
                root.Add(CreateProperty("HeadshotAllowFirstPerson", ts.Headshot.AllowFirstPerson));
                root.Add(CreateProperty("HeadshotAllowProjectile", ts.Headshot.AllowProjectile));
                root.Add(CreateProperty("HeadshotFirstPersonChance", ts.Headshot.FirstPersonChance));
                root.Add(CreateProperty("HeadshotProjectileChance", ts.Headshot.ProjectileChance));
                root.Add(CreateProperty("HeadshotOverrideScreenEffects", ts.Headshot.OverrideScreenEffects));
                
                // Critical trigger
                root.Add(CreateProperty("CriticalTriggerEnabled", ts.Critical.Enabled));
                root.Add(CreateProperty("CriticalPriority", ts.Critical.Priority));
                root.Add(CreateProperty("CriticalCooldown", ts.Critical.CooldownSeconds));
                root.Add(CreateProperty("CriticalAllowFirstPerson", ts.Critical.AllowFirstPerson));
                root.Add(CreateProperty("CriticalAllowProjectile", ts.Critical.AllowProjectile));
                root.Add(CreateProperty("CriticalFirstPersonChance", ts.Critical.FirstPersonChance));
                root.Add(CreateProperty("CriticalProjectileChance", ts.Critical.ProjectileChance));
                root.Add(CreateProperty("CriticalOverrideScreenEffects", ts.Critical.OverrideScreenEffects));
                
                // LongRangeKill trigger
                root.Add(CreateProperty("LongRangeTriggerEnabled", ts.LongRangeKill.Enabled));
                root.Add(CreateProperty("LongRangeTriggerPriority", ts.LongRangeKill.Priority));
                root.Add(CreateProperty("LongRangeTriggerCooldown", ts.LongRangeKill.CooldownSeconds));
                root.Add(CreateProperty("LongRangeTriggerAllowFirstPerson", ts.LongRangeKill.AllowFirstPerson));
                root.Add(CreateProperty("LongRangeTriggerAllowProjectile", ts.LongRangeKill.AllowProjectile));
                root.Add(CreateProperty("LongRangeFirstPersonChance", ts.LongRangeKill.FirstPersonChance));
                root.Add(CreateProperty("LongRangeProjectileChance", ts.LongRangeKill.ProjectileChance));
                root.Add(CreateProperty("LongRangeOverrideScreenEffects", ts.LongRangeKill.OverrideScreenEffects));
                
                // LowHealthKill trigger
                root.Add(CreateProperty("LowHealthTriggerEnabled", ts.LowHealthKill.Enabled));
                root.Add(CreateProperty("LowHealthTriggerPriority", ts.LowHealthKill.Priority));
                root.Add(CreateProperty("LowHealthTriggerCooldown", ts.LowHealthKill.CooldownSeconds));
                root.Add(CreateProperty("LowHealthTriggerAllowFirstPerson", ts.LowHealthKill.AllowFirstPerson));
                root.Add(CreateProperty("LowHealthTriggerAllowProjectile", ts.LowHealthKill.AllowProjectile));
                root.Add(CreateProperty("LowHealthFirstPersonChance", ts.LowHealthKill.FirstPersonChance));
                root.Add(CreateProperty("LowHealthProjectileChance", ts.LowHealthKill.ProjectileChance));
                root.Add(CreateProperty("LowHealthOverrideScreenEffects", ts.LowHealthKill.OverrideScreenEffects));
                
                // SneakKill trigger
                root.Add(CreateProperty("SneakTriggerEnabled", ts.SneakKill.Enabled));
                root.Add(CreateProperty("SneakTriggerPriority", ts.SneakKill.Priority));
                root.Add(CreateProperty("SneakTriggerCooldown", ts.SneakKill.CooldownSeconds));
                root.Add(CreateProperty("SneakTriggerAllowFirstPerson", ts.SneakKill.AllowFirstPerson));
                root.Add(CreateProperty("SneakTriggerAllowProjectile", ts.SneakKill.AllowProjectile));
                root.Add(CreateProperty("SneakFirstPersonChance", ts.SneakKill.FirstPersonChance));
                root.Add(CreateProperty("SneakProjectileChance", ts.SneakKill.ProjectileChance));
                root.Add(CreateProperty("SneakOverrideScreenEffects", ts.SneakKill.OverrideScreenEffects));

                root.Add(new XComment(" Sub-Cooldowns "));
                root.Add(CreateProperty("EnableCooldownCrit", s_cinematicSettings.EnableCooldownCrit));
                root.Add(CreateProperty("CooldownCrit", s_cinematicSettings.CooldownCrit));
                root.Add(CreateProperty("EnableCooldownDismember", s_cinematicSettings.EnableCooldownDismember));
                root.Add(CreateProperty("CooldownDismember", s_cinematicSettings.CooldownDismember));
                root.Add(CreateProperty("EnableCooldownLongRange", s_cinematicSettings.EnableCooldownLongRange));
                root.Add(CreateProperty("CooldownLongRange", s_cinematicSettings.CooldownLongRange));
                root.Add(CreateProperty("EnableCooldownLowHealth", s_cinematicSettings.EnableCooldownLowHealth));
                root.Add(CreateProperty("CooldownLowHealth", s_cinematicSettings.CooldownLowHealth));
                root.Add(CreateProperty("EnableCooldownLastEnemy", s_cinematicSettings.EnableCooldownLastEnemy));
                root.Add(CreateProperty("CooldownLastEnemy", s_cinematicSettings.CooldownLastEnemy));

                root.Add(new XComment(" Killstreaks "));
                root.Add(CreateProperty("EnableKillstreaks", s_cinematicSettings.EnableKillstreaks));
                root.Add(CreateProperty("KillstreakWindow", s_cinematicSettings.KillstreakWindow));
                root.Add(CreateProperty("Tier1Kills", s_cinematicSettings.Tier1Kills));
                root.Add(CreateProperty("Tier1BonusDuration", s_cinematicSettings.Tier1BonusDuration));
                root.Add(CreateProperty("Tier2Kills", s_cinematicSettings.Tier2Kills));
                root.Add(CreateProperty("Tier2BonusDuration", s_cinematicSettings.Tier2BonusDuration));
                root.Add(CreateProperty("Tier3Kills", s_cinematicSettings.Tier3Kills));
                root.Add(CreateProperty("Tier3BonusDuration", s_cinematicSettings.Tier3BonusDuration));

                root.Add(new XComment(" Context Toggles & Chances "));
                root.Add(CreateProperty("EnableDismember", s_cinematicSettings.EnableDismember));
                root.Add(CreateProperty("ChanceDismember", s_cinematicSettings.ChanceDismember));
                root.Add(CreateProperty("DismemberAllowFirstPerson", s_cinematicSettings.DismemberAllowFirstPerson));
                root.Add(CreateProperty("DismemberAllowProjectile", s_cinematicSettings.DismemberAllowProjectile));
                root.Add(CreateProperty("DismemberCustomEffects", s_cinematicSettings.DismemberCustomEffects));
                root.Add(CreateProperty("DismemberOverrideHitstop", s_cinematicSettings.DismemberOverrideHitstop));
                root.Add(CreateProperty("DismemberHitstopDuration", s_cinematicSettings.DismemberHitstopDuration));
                root.Add(CreateProperty("DismemberHitstopDurationMin", s_cinematicSettings.DismemberHitstopDurationMin));
                root.Add(CreateProperty("DismemberHitstopDurationMax", s_cinematicSettings.DismemberHitstopDurationMax));
                root.Add(CreateProperty("RandomizeDismemberHitstopDuration", s_cinematicSettings.RandomizeDismemberHitstopDuration));
                root.Add(CreateProperty("DismemberHitstopOnCritOnly", s_cinematicSettings.DismemberHitstopOnCritOnly));
                root.Add(CreateProperty("DismemberOverrideSlowMoSound", s_cinematicSettings.DismemberOverrideSlowMoSound));
                root.Add(CreateProperty("DismemberSlowMoSoundVolume", s_cinematicSettings.DismemberSlowMoSoundVolume));
                root.Add(CreateProperty("DismemberSlowMoSoundVolumeMin", s_cinematicSettings.DismemberSlowMoSoundVolumeMin));
                root.Add(CreateProperty("DismemberSlowMoSoundVolumeMax", s_cinematicSettings.DismemberSlowMoSoundVolumeMax));
                root.Add(CreateProperty("RandomizeDismemberSlowMoSoundVolume", s_cinematicSettings.RandomizeDismemberSlowMoSoundVolume));
                root.Add(CreateProperty("DismemberVisualSource", s_cinematicSettings.DismemberVisualSource));
                root.Add(CreateProperty("DismemberFOVSource", s_cinematicSettings.DismemberFOVSource));
                root.Add(CreateProperty("DismemberFXSource", s_cinematicSettings.DismemberFXSource));
                root.Add(CreateProperty("EnableCrit", s_cinematicSettings.EnableCrit));
                root.Add(CreateProperty("ChanceCrit", s_cinematicSettings.ChanceCrit));
                root.Add(CreateProperty("CritAllowFirstPerson", s_cinematicSettings.CritAllowFirstPerson));
                root.Add(CreateProperty("CritAllowProjectile", s_cinematicSettings.CritAllowProjectile));
                root.Add(CreateProperty("CritCustomEffects", s_cinematicSettings.CritCustomEffects));
                root.Add(CreateProperty("CritOverrideHitstop", s_cinematicSettings.CritOverrideHitstop));
                root.Add(CreateProperty("CritHitstopDuration", s_cinematicSettings.CritHitstopDuration));
                root.Add(CreateProperty("CritHitstopDurationMin", s_cinematicSettings.CritHitstopDurationMin));
                root.Add(CreateProperty("CritHitstopDurationMax", s_cinematicSettings.CritHitstopDurationMax));
                root.Add(CreateProperty("RandomizeCritHitstopDuration", s_cinematicSettings.RandomizeCritHitstopDuration));
                root.Add(CreateProperty("CritHitstopOnCritOnly", s_cinematicSettings.CritHitstopOnCritOnly));
                root.Add(CreateProperty("CritOverrideSlowMoSound", s_cinematicSettings.CritOverrideSlowMoSound));
                root.Add(CreateProperty("CritSlowMoSoundVolume", s_cinematicSettings.CritSlowMoSoundVolume));
                root.Add(CreateProperty("CritSlowMoSoundVolumeMin", s_cinematicSettings.CritSlowMoSoundVolumeMin));
                root.Add(CreateProperty("CritSlowMoSoundVolumeMax", s_cinematicSettings.CritSlowMoSoundVolumeMax));
                root.Add(CreateProperty("RandomizeCritSlowMoSoundVolume", s_cinematicSettings.RandomizeCritSlowMoSoundVolume));
                root.Add(CreateProperty("CritVisualSource", s_cinematicSettings.CritVisualSource));
                root.Add(CreateProperty("CritFOVSource", s_cinematicSettings.CritFOVSource));
                root.Add(CreateProperty("CritFXSource", s_cinematicSettings.CritFXSource));
                root.Add(CreateProperty("EnableLongRange", s_cinematicSettings.EnableLongRange));
                root.Add(CreateProperty("ChanceLongRange", s_cinematicSettings.ChanceLongRange));
                root.Add(CreateProperty("LongRangeAllowFirstPerson", s_cinematicSettings.LongRangeAllowFirstPerson));
                root.Add(CreateProperty("LongRangeAllowProjectile", s_cinematicSettings.LongRangeAllowProjectile));
                root.Add(CreateProperty("LongRangeCustomEffects", s_cinematicSettings.LongRangeCustomEffects));
                root.Add(CreateProperty("LongRangeOverrideHitstop", s_cinematicSettings.LongRangeOverrideHitstop));
                root.Add(CreateProperty("LongRangeHitstopDuration", s_cinematicSettings.LongRangeHitstopDuration));
                root.Add(CreateProperty("LongRangeHitstopDurationMin", s_cinematicSettings.LongRangeHitstopDurationMin));
                root.Add(CreateProperty("LongRangeHitstopDurationMax", s_cinematicSettings.LongRangeHitstopDurationMax));
                root.Add(CreateProperty("RandomizeLongRangeHitstopDuration", s_cinematicSettings.RandomizeLongRangeHitstopDuration));
                root.Add(CreateProperty("LongRangeHitstopOnCritOnly", s_cinematicSettings.LongRangeHitstopOnCritOnly));
                root.Add(CreateProperty("LongRangeOverrideSlowMoSound", s_cinematicSettings.LongRangeOverrideSlowMoSound));
                root.Add(CreateProperty("LongRangeSlowMoSoundVolume", s_cinematicSettings.LongRangeSlowMoSoundVolume));
                root.Add(CreateProperty("LongRangeSlowMoSoundVolumeMin", s_cinematicSettings.LongRangeSlowMoSoundVolumeMin));
                root.Add(CreateProperty("LongRangeSlowMoSoundVolumeMax", s_cinematicSettings.LongRangeSlowMoSoundVolumeMax));
                root.Add(CreateProperty("RandomizeLongRangeSlowMoSoundVolume", s_cinematicSettings.RandomizeLongRangeSlowMoSoundVolume));
                root.Add(CreateProperty("LongRangeVisualSource", s_cinematicSettings.LongRangeVisualSource));
                root.Add(CreateProperty("LongRangeFOVSource", s_cinematicSettings.LongRangeFOVSource));
                root.Add(CreateProperty("LongRangeFXSource", s_cinematicSettings.LongRangeFXSource));
                root.Add(CreateProperty("EnableLowHealth", s_cinematicSettings.EnableLowHealth));
                root.Add(CreateProperty("ChanceLowHealth", s_cinematicSettings.ChanceLowHealth));
                root.Add(CreateProperty("LowHealthAllowFirstPerson", s_cinematicSettings.LowHealthAllowFirstPerson));
                root.Add(CreateProperty("LowHealthAllowProjectile", s_cinematicSettings.LowHealthAllowProjectile));
                root.Add(CreateProperty("LowHealthCustomEffects", s_cinematicSettings.LowHealthCustomEffects));
                root.Add(CreateProperty("LowHealthOverrideHitstop", s_cinematicSettings.LowHealthOverrideHitstop));
                root.Add(CreateProperty("LowHealthHitstopDuration", s_cinematicSettings.LowHealthHitstopDuration));
                root.Add(CreateProperty("LowHealthHitstopDurationMin", s_cinematicSettings.LowHealthHitstopDurationMin));
                root.Add(CreateProperty("LowHealthHitstopDurationMax", s_cinematicSettings.LowHealthHitstopDurationMax));
                root.Add(CreateProperty("RandomizeLowHealthHitstopDuration", s_cinematicSettings.RandomizeLowHealthHitstopDuration));
                root.Add(CreateProperty("LowHealthHitstopOnCritOnly", s_cinematicSettings.LowHealthHitstopOnCritOnly));
                root.Add(CreateProperty("LowHealthOverrideSlowMoSound", s_cinematicSettings.LowHealthOverrideSlowMoSound));
                root.Add(CreateProperty("LowHealthSlowMoSoundVolume", s_cinematicSettings.LowHealthSlowMoSoundVolume));
                root.Add(CreateProperty("LowHealthSlowMoSoundVolumeMin", s_cinematicSettings.LowHealthSlowMoSoundVolumeMin));
                root.Add(CreateProperty("LowHealthSlowMoSoundVolumeMax", s_cinematicSettings.LowHealthSlowMoSoundVolumeMax));
                root.Add(CreateProperty("RandomizeLowHealthSlowMoSoundVolume", s_cinematicSettings.RandomizeLowHealthSlowMoSoundVolume));
                root.Add(CreateProperty("LowHealthVisualSource", s_cinematicSettings.LowHealthVisualSource));
                root.Add(CreateProperty("LowHealthFOVSource", s_cinematicSettings.LowHealthFOVSource));
                root.Add(CreateProperty("LowHealthFXSource", s_cinematicSettings.LowHealthFXSource));

                root.Add(new XComment(" Context Modifiers "));
                root.Add(CreateProperty("DistanceThreshold", s_cinematicSettings.DistanceThreshold));
                root.Add(CreateProperty("LowHealthThreshold", s_cinematicSettings.LowHealthThreshold));
                root.Add(CreateProperty("LongRangeZoomMultiplier", s_cinematicSettings.LongRangeZoomMultiplier));
                root.Add(CreateProperty("LongRangeZoomSpeed", s_cinematicSettings.LongRangeZoomSpeed));
                root.Add(CreateProperty("LowHealthSlowScale", s_cinematicSettings.LowHealthSlowScale));
                root.Add(CreateProperty("CritZoomMultiplier", s_cinematicSettings.CritZoomMultiplier));
                root.Add(CreateProperty("CritZoomSpeed", s_cinematicSettings.CritZoomSpeed));

                root.Add(new XComment(" Hitstop "));
                root.Add(CreateProperty("EnableHitstop", s_cinematicSettings.EnableHitstop));
                root.Add(CreateProperty("HitstopDuration", s_cinematicSettings.HitstopDuration));
                root.Add(CreateProperty("HitstopOnCritOnly", s_cinematicSettings.HitstopOnCritOnly));

                root.Add(new XComment(" Screen Effects "));
                root.Add(CreateProperty("EnableScreenEffects", s_cinematicSettings.ScreenEffects.Enabled));
                root.Add(CreateProperty("EnableVignetteFX", s_cinematicSettings.ScreenEffects.EnableVignette));
                root.Add(CreateProperty("VignetteIntensityFX", s_cinematicSettings.ScreenEffects.VignetteIntensity));
                root.Add(CreateProperty("RandomizeVignetteIntensity_FX", s_cinematicSettings.ScreenEffects.RandomizeVignetteIntensity));
                root.Add(CreateProperty("VignetteIntensityMin_FX", s_cinematicSettings.ScreenEffects.VignetteIntensityMin));
                root.Add(CreateProperty("VignetteIntensityMax_FX", s_cinematicSettings.ScreenEffects.VignetteIntensityMax));
                root.Add(CreateProperty("EnableDesaturation", s_cinematicSettings.ScreenEffects.EnableDesaturation));
                root.Add(CreateProperty("DesaturationAmountFX", s_cinematicSettings.ScreenEffects.DesaturationAmount));
                root.Add(CreateProperty("RandomizeDesaturationAmount_FX", s_cinematicSettings.ScreenEffects.RandomizeDesaturationAmount));
                root.Add(CreateProperty("DesaturationAmountMin_FX", s_cinematicSettings.ScreenEffects.DesaturationAmountMin));
                root.Add(CreateProperty("DesaturationAmountMax_FX", s_cinematicSettings.ScreenEffects.DesaturationAmountMax));
                root.Add(CreateProperty("EnableBloodSplatter", s_cinematicSettings.ScreenEffects.EnableBloodSplatter));
                root.Add(CreateProperty("BloodSplatterIntensityFX", s_cinematicSettings.ScreenEffects.BloodSplatterIntensity));
                root.Add(CreateProperty("RandomizeBloodSplatterIntensity_FX", s_cinematicSettings.ScreenEffects.RandomizeBloodSplatterIntensity));
                root.Add(CreateProperty("BloodSplatterIntensityMin_FX", s_cinematicSettings.ScreenEffects.BloodSplatterIntensityMin));
                root.Add(CreateProperty("BloodSplatterIntensityMax_FX", s_cinematicSettings.ScreenEffects.BloodSplatterIntensityMax));
                root.Add(CreateProperty("EnableHitstopFX", s_cinematicSettings.ScreenEffects.EnableHitstop));
                root.Add(CreateProperty("HitstopDurationFX", s_cinematicSettings.ScreenEffects.HitstopDuration));
                root.Add(CreateProperty("RandomizeHitstopDuration_FX", s_cinematicSettings.ScreenEffects.RandomizeHitstopDuration));
                root.Add(CreateProperty("HitstopDurationMin_FX", s_cinematicSettings.ScreenEffects.HitstopDurationMin));
                root.Add(CreateProperty("HitstopDurationMax_FX", s_cinematicSettings.ScreenEffects.HitstopDurationMax));

                root.Add(new XComment(" Projectile Camera Settings "));
                root.Add(CreateProperty("Distance_PC", s_cinematicSettings.ProjectileCamera.Distance));
                root.Add(CreateProperty("Height_PC", s_cinematicSettings.ProjectileCamera.Height));
                root.Add(CreateProperty("XOffset_PC", s_cinematicSettings.ProjectileCamera.XOffset));
                root.Add(CreateProperty("Pitch_PC", s_cinematicSettings.ProjectileCamera.Pitch));
                root.Add(CreateProperty("Yaw_PC", s_cinematicSettings.ProjectileCamera.Yaw));
                root.Add(CreateProperty("Tilt_PC", s_cinematicSettings.ProjectileCamera.Tilt));
                root.Add(CreateProperty("UseStandardPresets_PC", s_cinematicSettings.ProjectileCamera.UseStandardPresets));
                root.Add(CreateProperty("RandomizeTilt_PC", s_cinematicSettings.ProjectileCamera.RandomizeTilt));
                root.Add(CreateProperty("RandomTiltRange_PC", s_cinematicSettings.ProjectileCamera.RandomTiltRange));
                root.Add(CreateProperty("RandomizeSideOffset_PC", s_cinematicSettings.ProjectileCamera.RandomizeSideOffset));
                root.Add(CreateProperty("SideOffsetWide_PC", s_cinematicSettings.ProjectileCamera.SideOffsetWide));
                root.Add(CreateProperty("SideOffsetStandard_PC", s_cinematicSettings.ProjectileCamera.SideOffsetStandard));
                root.Add(CreateProperty("SideOffsetTight_PC", s_cinematicSettings.ProjectileCamera.SideOffsetTight));

                root.Add(new XComment(" Sound "));
                root.Add(CreateProperty("EnableSlowMoSound", s_cinematicSettings.EnableSlowMoSound));
                root.Add(CreateProperty("SlowMoSoundVolume", s_cinematicSettings.SlowMoSoundVolume));

                root.Add(new XComment(" Visuals "));
                root.Add(CreateProperty("EnableFOVEffect", s_cinematicSettings.EnableFOVEffect));
                root.Add(CreateProperty("FOVZoomAmount", s_cinematicSettings.FOVZoomAmount));
                root.Add(CreateProperty("FOVZoomInDuration", s_cinematicSettings.FOVZoomInDuration));
                root.Add(CreateProperty("FOVZoomHoldDuration", s_cinematicSettings.FOVZoomHoldDuration));
                root.Add(CreateProperty("FOVZoomOutDuration", s_cinematicSettings.FOVZoomOutDuration));
                
                root.Add(new XComment(" Projectile Camera "));
                root.Add(CreateProperty("ProjectileCameraDuration", s_cinematicSettings.ProjectileCameraDuration));
                root.Add(CreateProperty("ProjectileCameraDurationMin", s_cinematicSettings.ProjectileCameraDurationMin));
                root.Add(CreateProperty("ProjectileCameraDurationMax", s_cinematicSettings.ProjectileCameraDurationMax));
                root.Add(CreateProperty("ProjectileCameraHeightOffset", s_cinematicSettings.ProjectileCameraHeightOffset));
                root.Add(CreateProperty("ProjectileCameraHeightOffsetMin", s_cinematicSettings.ProjectileCameraHeightOffsetMin));
                root.Add(CreateProperty("ProjectileCameraHeightOffsetMax", s_cinematicSettings.ProjectileCameraHeightOffsetMax));
                root.Add(CreateProperty("ProjectileCameraDistanceOffset", s_cinematicSettings.ProjectileCameraDistanceOffset));
                root.Add(CreateProperty("ProjectileCameraDistanceOffsetMin", s_cinematicSettings.ProjectileCameraDistanceOffsetMin));
                root.Add(CreateProperty("ProjectileCameraDistanceOffsetMax", s_cinematicSettings.ProjectileCameraDistanceOffsetMax));
                root.Add(CreateProperty("ProjectileCameraChance", s_cinematicSettings.ProjectileCameraChance));
                root.Add(CreateProperty("ProjectileCameraChanceMin", s_cinematicSettings.ProjectileCameraChanceMin));
                root.Add(CreateProperty("ProjectileCameraChanceMax", s_cinematicSettings.ProjectileCameraChanceMax));
                root.Add(CreateProperty("ProjectileCameraLastEnemyOnly", s_cinematicSettings.ProjectileCameraLastEnemyOnly));
                root.Add(CreateProperty("ProjectileCameraSlowScale", s_cinematicSettings.ProjectileCameraSlowScale));
                root.Add(CreateProperty("ProjectileCameraSlowScaleMin", s_cinematicSettings.ProjectileCameraSlowScaleMin));
                root.Add(CreateProperty("ProjectileCameraSlowScaleMax", s_cinematicSettings.ProjectileCameraSlowScaleMax));
                root.Add(CreateProperty("ProjectileCameraXOffset", s_cinematicSettings.ProjectileCameraXOffset));
                root.Add(CreateProperty("ProjectileCameraXOffsetMin", s_cinematicSettings.ProjectileCameraXOffsetMin));
                root.Add(CreateProperty("ProjectileCameraXOffsetMax", s_cinematicSettings.ProjectileCameraXOffsetMax));
                root.Add(CreateProperty("ProjectileCameraLookYaw", s_cinematicSettings.ProjectileCameraLookYaw));
                root.Add(CreateProperty("ProjectileCameraLookPitch", s_cinematicSettings.ProjectileCameraLookPitch));
                root.Add(CreateProperty("ProjectileCameraLookYawMin", s_cinematicSettings.ProjectileCameraLookYawMin));
                root.Add(CreateProperty("ProjectileCameraLookYawMax", s_cinematicSettings.ProjectileCameraLookYawMax));
                root.Add(CreateProperty("ProjectileCameraLookPitchMin", s_cinematicSettings.ProjectileCameraLookPitchMin));
                root.Add(CreateProperty("ProjectileCameraLookPitchMax", s_cinematicSettings.ProjectileCameraLookPitchMax));
                root.Add(CreateProperty("ProjectileCameraRandomYawRange", s_cinematicSettings.ProjectileCameraRandomYawRange));
                root.Add(CreateProperty("ProjectileCameraRandomPitchRange", s_cinematicSettings.ProjectileCameraRandomPitchRange));
                root.Add(CreateProperty("ProjectileCameraRandomYawRangeMin", s_cinematicSettings.ProjectileCameraRandomYawRangeMin));
                root.Add(CreateProperty("ProjectileCameraRandomYawRangeMax", s_cinematicSettings.ProjectileCameraRandomYawRangeMax));
                root.Add(CreateProperty("ProjectileCameraRandomPitchRangeMin", s_cinematicSettings.ProjectileCameraRandomPitchRangeMin));
                root.Add(CreateProperty("ProjectileCameraRandomPitchRangeMax", s_cinematicSettings.ProjectileCameraRandomPitchRangeMax));
                root.Add(CreateProperty("ProjectileCameraReturnDuration", s_cinematicSettings.ProjectileCameraReturnDuration));
                root.Add(CreateProperty("ProjectileCameraReturnDurationMin", s_cinematicSettings.ProjectileCameraReturnDurationMin));
                root.Add(CreateProperty("ProjectileCameraReturnDurationMax", s_cinematicSettings.ProjectileCameraReturnDurationMax));
                root.Add(CreateProperty("ProjectileReturnPercent", s_cinematicSettings.ProjectileReturnPercent));
                root.Add(CreateProperty("ProjectileReturnStart", s_cinematicSettings.ProjectileReturnStart));
                
                root.Add(CreateProperty("ProjectileCameraFOV", s_cinematicSettings.ProjectileCameraFOV));
                root.Add(CreateProperty("ProjectileCameraFOVMin", s_cinematicSettings.ProjectileCameraFOVMin));
                root.Add(CreateProperty("ProjectileCameraFOVMax", s_cinematicSettings.ProjectileCameraFOVMax));
                root.Add(CreateProperty("ProjectileCameraFOVZoomIn", s_cinematicSettings.ProjectileCameraFOVZoomInDuration));
                root.Add(CreateProperty("ProjectileCameraFOVZoomInMin", s_cinematicSettings.ProjectileCameraFOVZoomInDurationMin));
                root.Add(CreateProperty("ProjectileCameraFOVZoomInMax", s_cinematicSettings.ProjectileCameraFOVZoomInDurationMax));
                root.Add(CreateProperty("ProjectileCameraFOVHold", s_cinematicSettings.ProjectileCameraFOVHoldDuration));
                root.Add(CreateProperty("ProjectileCameraFOVHoldMin", s_cinematicSettings.ProjectileCameraFOVHoldDurationMin));
                root.Add(CreateProperty("ProjectileCameraFOVHoldMax", s_cinematicSettings.ProjectileCameraFOVHoldDurationMax));
                root.Add(CreateProperty("ProjectileCameraFOVZoomOut", s_cinematicSettings.ProjectileCameraFOVZoomOutDuration));
                root.Add(CreateProperty("ProjectileCameraFOVZoomOutMin", s_cinematicSettings.ProjectileCameraFOVZoomOutDurationMin));
                root.Add(CreateProperty("ProjectileCameraFOVZoomOutMax", s_cinematicSettings.ProjectileCameraFOVZoomOutDurationMax));
                root.Add(CreateProperty("ProjectileCameraEnableVignette", s_cinematicSettings.ProjectileCameraEnableVignette));
                root.Add(CreateProperty("ProjectileCameraVignetteIntensity", s_cinematicSettings.ProjectileCameraVignetteIntensity));
                root.Add(CreateProperty("ProjectileCameraVignetteIntensityMin", s_cinematicSettings.ProjectileCameraVignetteIntensityMin));
                root.Add(CreateProperty("ProjectileCameraVignetteIntensityMax", s_cinematicSettings.ProjectileCameraVignetteIntensityMax));
                root.Add(CreateProperty("RandomizeProjectileDuration", s_cinematicSettings.RandomizeProjectileDuration));
                root.Add(CreateProperty("RandomizeProjectileSlowScale", s_cinematicSettings.RandomizeProjectileSlowScale));
                root.Add(CreateProperty("RandomizeProjectileReturnDuration", s_cinematicSettings.RandomizeProjectileReturnDuration));
                root.Add(CreateProperty("RandomizeProjectileChance", s_cinematicSettings.RandomizeProjectileChance));
                root.Add(CreateProperty("RandomizeProjectileHeightOffset", s_cinematicSettings.RandomizeProjectileHeightOffset));
                root.Add(CreateProperty("RandomizeProjectileDistanceOffset", s_cinematicSettings.RandomizeProjectileDistanceOffset));
                root.Add(CreateProperty("RandomizeProjectileXOffset", s_cinematicSettings.RandomizeProjectileXOffset));
                root.Add(CreateProperty("RandomizeProjectileLookYaw", s_cinematicSettings.RandomizeProjectileLookYaw));
                root.Add(CreateProperty("RandomizeProjectileLookPitch", s_cinematicSettings.RandomizeProjectileLookPitch));
                root.Add(CreateProperty("RandomizeProjectileRandomYawRange", s_cinematicSettings.RandomizeProjectileRandomYawRange));
                root.Add(CreateProperty("RandomizeProjectileRandomPitchRange", s_cinematicSettings.RandomizeProjectileRandomPitchRange));
                root.Add(CreateProperty("RandomizeProjectileFOV", s_cinematicSettings.RandomizeProjectileFOV));
                root.Add(CreateProperty("RandomizeProjectileFOVIn", s_cinematicSettings.RandomizeProjectileFOVIn));
                root.Add(CreateProperty("RandomizeProjectileFOVHold", s_cinematicSettings.RandomizeProjectileFOVHold));
                root.Add(CreateProperty("RandomizeProjectileFOVOut", s_cinematicSettings.RandomizeProjectileFOVOut));
                root.Add(CreateProperty("RandomizeProjectileVignetteIntensity", s_cinematicSettings.RandomizeProjectileVignetteIntensity));
                root.Add(CreateProperty("ProjectileVisualSource", s_cinematicSettings.ProjectileVisualSource));
                root.Add(CreateProperty("ProjectileFOVSource", s_cinematicSettings.ProjectileFOVSource));
                root.Add(CreateProperty("ProjectileFXSource", s_cinematicSettings.ProjectileFXSource));
                root.Add(CreateProperty("EnableProjectileHitstop", s_cinematicSettings.EnableProjectileHitstop));
                root.Add(CreateProperty("ProjectileHitstopDuration", s_cinematicSettings.ProjectileHitstopDuration));
                root.Add(CreateProperty("ProjectileHitstopDurationMin", s_cinematicSettings.ProjectileHitstopDurationMin));
                root.Add(CreateProperty("ProjectileHitstopDurationMax", s_cinematicSettings.ProjectileHitstopDurationMax));
                root.Add(CreateProperty("RandomizeProjectileHitstopDuration", s_cinematicSettings.RandomizeProjectileHitstopDuration));
                root.Add(CreateProperty("ProjectileHitstopOnCritOnly", s_cinematicSettings.ProjectileHitstopOnCritOnly));
                root.Add(CreateProperty("EnableProjectileSlowMoSound", s_cinematicSettings.EnableProjectileSlowMoSound));
                root.Add(CreateProperty("ProjectileSlowMoSoundVolume", s_cinematicSettings.ProjectileSlowMoSoundVolume));
                root.Add(CreateProperty("ProjectileSlowMoSoundVolumeMin", s_cinematicSettings.ProjectileSlowMoSoundVolumeMin));
                root.Add(CreateProperty("ProjectileSlowMoSoundVolumeMax", s_cinematicSettings.ProjectileSlowMoSoundVolumeMax));
                root.Add(CreateProperty("RandomizeProjectileSlowMoSoundVolume", s_cinematicSettings.RandomizeProjectileSlowMoSoundVolume));
                
                root.Add(CreateProperty("EnableVignette", s_cinematicSettings.EnableVignette));
                root.Add(CreateProperty("VignetteIntensity", s_cinematicSettings.VignetteIntensity));
                root.Add(CreateProperty("RandomizeVignetteIntensity", s_cinematicSettings.RandomizeVignetteIntensity));
                root.Add(CreateProperty("VignetteIntensityMin", s_cinematicSettings.VignetteIntensityMin));
                root.Add(CreateProperty("VignetteIntensityMax", s_cinematicSettings.VignetteIntensityMax));
                root.Add(CreateProperty("EnableColorGrading", s_cinematicSettings.EnableColorGrading));
                root.Add(CreateProperty("ColorGradingIntensity", s_cinematicSettings.ColorGradingIntensity));
                root.Add(CreateProperty("RandomizeColorGradingIntensity", s_cinematicSettings.RandomizeColorGradingIntensity));
                root.Add(CreateProperty("ColorGradingIntensityMin", s_cinematicSettings.ColorGradingIntensityMin));
                root.Add(CreateProperty("ColorGradingIntensityMax", s_cinematicSettings.ColorGradingIntensityMax));
                root.Add(CreateProperty("ColorGradingMode", s_cinematicSettings.ColorGradingMode));
                root.Add(CreateProperty("EnableFlash", s_cinematicSettings.EnableFlash));
                root.Add(CreateProperty("FlashIntensity", s_cinematicSettings.FlashIntensity));
                root.Add(CreateProperty("RandomizeFlashIntensity", s_cinematicSettings.RandomizeFlashIntensity));
                root.Add(CreateProperty("FlashIntensityMin", s_cinematicSettings.FlashIntensityMin));
                root.Add(CreateProperty("FlashIntensityMax", s_cinematicSettings.FlashIntensityMax));
                root.Add(CreateProperty("EnableFirstPersonHitstop", s_cinematicSettings.EnableFirstPersonHitstop));
                root.Add(CreateProperty("FirstPersonHitstopDuration", s_cinematicSettings.FirstPersonHitstopDuration));
                root.Add(CreateProperty("FirstPersonHitstopDurationMin", s_cinematicSettings.FirstPersonHitstopDurationMin));
                root.Add(CreateProperty("FirstPersonHitstopDurationMax", s_cinematicSettings.FirstPersonHitstopDurationMax));
                root.Add(CreateProperty("RandomizeFirstPersonHitstopDuration", s_cinematicSettings.RandomizeFirstPersonHitstopDuration));
                root.Add(CreateProperty("FirstPersonHitstopOnCritOnly", s_cinematicSettings.FirstPersonHitstopOnCritOnly));
                root.Add(CreateProperty("EnableFirstPersonSlowMoSound", s_cinematicSettings.EnableFirstPersonSlowMoSound));
                root.Add(CreateProperty("FirstPersonSlowMoSoundVolume", s_cinematicSettings.FirstPersonSlowMoSoundVolume));
                root.Add(CreateProperty("FirstPersonSlowMoSoundVolumeMin", s_cinematicSettings.FirstPersonSlowMoSoundVolumeMin));
                root.Add(CreateProperty("FirstPersonSlowMoSoundVolumeMax", s_cinematicSettings.FirstPersonSlowMoSoundVolumeMax));
                root.Add(CreateProperty("RandomizeFirstPersonSlowMoSoundVolume", s_cinematicSettings.RandomizeFirstPersonSlowMoSoundVolume));
                root.Add(CreateProperty("RandomizeFOVAmount", s_cinematicSettings.RandomizeFOVAmount));
                root.Add(CreateProperty("FOVZoomAmountMin", s_cinematicSettings.FOVZoomAmountMin));
                root.Add(CreateProperty("FOVZoomAmountMax", s_cinematicSettings.FOVZoomAmountMax));
                root.Add(CreateProperty("RandomizeFOVIn", s_cinematicSettings.RandomizeFOVIn));
                root.Add(CreateProperty("FOVZoomInDurationMin", s_cinematicSettings.FOVZoomInDurationMin));
                root.Add(CreateProperty("FOVZoomInDurationMax", s_cinematicSettings.FOVZoomInDurationMax));
                root.Add(CreateProperty("RandomizeFOVHold", s_cinematicSettings.RandomizeFOVHold));
                root.Add(CreateProperty("FOVZoomHoldDurationMin", s_cinematicSettings.FOVZoomHoldDurationMin));
                root.Add(CreateProperty("FOVZoomHoldDurationMax", s_cinematicSettings.FOVZoomHoldDurationMax));
                root.Add(CreateProperty("RandomizeFOVOut", s_cinematicSettings.RandomizeFOVOut));
                root.Add(CreateProperty("FOVZoomOutDurationMin", s_cinematicSettings.FOVZoomOutDurationMin));
                root.Add(CreateProperty("FOVZoomOutDurationMax", s_cinematicSettings.FOVZoomOutDurationMax));

                // GlobalVisuals Settings
                root.Add(new XComment(" Global Visual Settings "));
                var gvSave = s_cinematicSettings.MenuV2?.GlobalVisuals;
                if (gvSave != null)
                {
                    root.Add(CreateProperty("GV_EnableScreenEffects", gvSave.EnableScreenEffects));
                    root.Add(CreateProperty("GV_EnableFOVEffect", gvSave.EnableFOVEffect));
                    root.Add(CreateProperty("GV_FOVMultiplier", gvSave.FOVMultiplier));
                    root.Add(CreateProperty("GV_FOVInDuration", gvSave.FOVInDuration));
                    root.Add(CreateProperty("GV_FOVHoldDuration", gvSave.FOVHoldDuration));
                    root.Add(CreateProperty("GV_FOVOutDuration", gvSave.FOVOutDuration));
                    root.Add(CreateProperty("GV_EnableVignette", gvSave.EnableVignette));
                    root.Add(CreateProperty("GV_VignetteIntensity", gvSave.VignetteIntensity));
                    root.Add(CreateProperty("GV_EnableDesaturation", gvSave.EnableDesaturation));
                    root.Add(CreateProperty("GV_DesaturationAmount", gvSave.DesaturationAmount));
                    root.Add(CreateProperty("GV_EnableBloodSplatter", gvSave.EnableBloodSplatter));
                    root.Add(CreateProperty("GV_BloodSplatterDirection", gvSave.BloodSplatterDirection));
                    root.Add(CreateProperty("GV_BloodSplatterIntensity", gvSave.BloodSplatterIntensity));
                    root.Add(CreateProperty("GV_EnableConcussion", gvSave.EnableConcussion));
                    root.Add(CreateProperty("GV_ConcussionIntensity", gvSave.ConcussionIntensity));
                    root.Add(CreateProperty("GV_ConcussionDuration", gvSave.ConcussionDuration));
                    root.Add(CreateProperty("GV_ConcussionAudioMuffle", gvSave.ConcussionAudioMuffle));
                    // Post-processing effects
                    root.Add(CreateProperty("GV_EnableMotionBlur", gvSave.EnableMotionBlur));
                    root.Add(CreateProperty("GV_MotionBlurIntensity", gvSave.MotionBlurIntensity));
                    root.Add(CreateProperty("GV_EnableChromaticAberration", gvSave.EnableChromaticAberration));
                    root.Add(CreateProperty("GV_ChromaticAberrationIntensity", gvSave.ChromaticAberrationIntensity));
                    root.Add(CreateProperty("GV_EnableDepthOfField", gvSave.EnableDepthOfField));
                    root.Add(CreateProperty("GV_DepthOfFieldFocusDistance", gvSave.DepthOfFieldFocusDistance));
                    root.Add(CreateProperty("GV_DepthOfFieldAperture", gvSave.DepthOfFieldAperture));
                    root.Add(CreateProperty("GV_DepthOfFieldFocalLength", gvSave.DepthOfFieldFocalLength));
                    root.Add(CreateProperty("GV_EnableRadialBlur", gvSave.EnableRadialBlur));
                    root.Add(CreateProperty("GV_RadialBlurIntensity", gvSave.RadialBlurIntensity));
                    root.Add(CreateProperty("GV_RadialBlurDuration", gvSave.RadialBlurDuration));
                }
                
                // Kill Flash and Kill Vignette (Screen Effects)
                root.Add(new XComment(" Kill Flash / Kill Vignette Effects "));
                var screenFx = s_cinematicSettings.ScreenEffects;
                root.Add(CreateProperty("Effects_EnableKillFlash", screenFx.EnableKillFlash));
                root.Add(CreateProperty("Effects_KillFlashDuration", screenFx.KillFlashDuration));
                root.Add(CreateProperty("Effects_KillFlashIntensity", screenFx.KillFlashIntensity));
                root.Add(CreateProperty("Effects_EnableKillVignette", screenFx.EnableKillVignette));
                root.Add(CreateProperty("Effects_KillVignetteDuration", screenFx.KillVignetteDuration));
                root.Add(CreateProperty("Effects_KillVignetteIntensity", screenFx.KillVignetteIntensity));

                // Core HUD Settings
                root.Add(new XComment(" HUD Settings "));
                var coreSave = s_cinematicSettings.MenuV2?.Core;
                if (coreSave != null)
                {
                    root.Add(CreateProperty("Core_EnableHUD", coreSave.EnableHUD));
                    root.Add(CreateProperty("Core_HUDOpacity", coreSave.HUDOpacity));
                    root.Add(CreateProperty("Core_HUDMessageDuration", coreSave.HUDMessageDuration));
                }

                // Smart Options & New Settings
                root.Add(new XComment(" Smart Options "));
                root.Add(CreateProperty("SmartIndoorOutdoorDetection", s_cinematicSettings.SmartIndoorOutdoorDetection));
                root.Add(CreateProperty("IndoorDetectionHeight", s_cinematicSettings.IndoorDetectionHeight));
                root.Add(CreateProperty("EnableAdvancedFOVTiming", s_cinematicSettings.EnableAdvancedFOVTiming));
                
                // BasicKill Randomization
                root.Add(new XComment(" BasicKill Randomization "));
                root.Add(CreateProperty("BK_RandomizeChance", s_cinematicSettings.BasicKill.RandomizeChance));
                root.Add(CreateProperty("BK_ChanceMin", s_cinematicSettings.BasicKill.ChanceMin));
                root.Add(CreateProperty("BK_ChanceMax", s_cinematicSettings.BasicKill.ChanceMax));
                root.Add(CreateProperty("BK_RandomizeDuration", s_cinematicSettings.BasicKill.RandomizeDuration));
                root.Add(CreateProperty("BK_DurationMin", s_cinematicSettings.BasicKill.DurationMin));
                root.Add(CreateProperty("BK_DurationMax", s_cinematicSettings.BasicKill.DurationMax));
                root.Add(CreateProperty("BK_RandomizeTimeScale", s_cinematicSettings.BasicKill.RandomizeTimeScale));
                root.Add(CreateProperty("BK_TimeScaleMin", s_cinematicSettings.BasicKill.TimeScaleMin));
                root.Add(CreateProperty("BK_TimeScaleMax", s_cinematicSettings.BasicKill.TimeScaleMax));
                
                // BasicKill Core Settings (user requested these be saved)
                root.Add(CreateProperty("BK_Enabled", s_cinematicSettings.BasicKill.Enabled));
                root.Add(CreateProperty("BK_Chance", s_cinematicSettings.BasicKill.Chance));
                root.Add(CreateProperty("BK_Duration", s_cinematicSettings.BasicKill.Duration));
                root.Add(CreateProperty("BK_TimeScale", s_cinematicSettings.BasicKill.TimeScale));
                root.Add(CreateProperty("BK_Cooldown", s_cinematicSettings.BasicKill.Cooldown));
                root.Add(CreateProperty("BK_FirstPersonCamera", s_cinematicSettings.BasicKill.FirstPersonCamera));
                root.Add(CreateProperty("BK_ProjectileCamera", s_cinematicSettings.BasicKill.ProjectileCamera));
                root.Add(CreateProperty("BK_FirstPersonChance", s_cinematicSettings.BasicKill.FirstPersonChance));
                root.Add(CreateProperty("BK_FOVMode", (int)s_cinematicSettings.BasicKill.FOVMode));
                root.Add(CreateProperty("BK_FOVPercent", s_cinematicSettings.BasicKill.FOVPercent));
                // BasicKill FOV Timing
                root.Add(CreateProperty("BK_FOVZoomInDuration", s_cinematicSettings.BasicKill.FOVZoomInDuration));
                root.Add(CreateProperty("BK_FOVHoldDuration", s_cinematicSettings.BasicKill.FOVHoldDuration));
                root.Add(CreateProperty("BK_FOVZoomOutDuration", s_cinematicSettings.BasicKill.FOVZoomOutDuration));
                // BasicKill Projectile FOV
                root.Add(CreateProperty("BK_ProjectileFOVMode", (int)s_cinematicSettings.BasicKill.ProjectileFOVMode));
                root.Add(CreateProperty("BK_ProjectileFOVPercent", s_cinematicSettings.BasicKill.ProjectileFOVPercent));
                root.Add(CreateProperty("BK_ProjectileFOVZoomInDuration", s_cinematicSettings.BasicKill.ProjectileFOVZoomInDuration));
                root.Add(CreateProperty("BK_ProjectileFOVHoldDuration", s_cinematicSettings.BasicKill.ProjectileFOVHoldDuration));
                root.Add(CreateProperty("BK_ProjectileFOVZoomOutDuration", s_cinematicSettings.BasicKill.ProjectileFOVZoomOutDuration));
                
                // Camera Preset Toggles (user requested)
                root.Add(new XComment(" Camera Preset Selections "));
                var presets = s_cinematicSettings.ProjectileCamera.EnabledPresets;
                string presetValues = string.Join(",", presets.Select(b => b ? "1" : "0"));
                root.Add(CreateProperty("EnabledPresets_PC", presetValues));
                // Separate preset arrays for BasicKill vs Triggers
                var presetsBK = s_cinematicSettings.ProjectileCamera.EnabledPresetsBasicKill;
                string presetBKValues = string.Join(",", presetsBK.Select(b => b ? "1" : "0"));
                root.Add(CreateProperty("EnabledPresetsBasicKill_PC", presetBKValues));
                var presetsTD = s_cinematicSettings.ProjectileCamera.EnabledPresetsTriggers;
                string presetTDValues = string.Join(",", presetsTD.Select(b => b ? "1" : "0"));
                root.Add(CreateProperty("EnabledPresetsTriggers_PC", presetTDValues));
                
                // Dynamic Zoom (ADS simulation)
                root.Add(new XComment(" Dynamic Zoom "));
                root.Add(CreateProperty("PC_EnableDynamicZoomIn", s_cinematicSettings.ProjectileCamera.EnableDynamicZoomIn));
                root.Add(CreateProperty("PC_EnableDynamicZoomOut", s_cinematicSettings.ProjectileCamera.EnableDynamicZoomOut));
                root.Add(CreateProperty("PC_DynamicZoomBalance", s_cinematicSettings.ProjectileCamera.DynamicZoomBalance));
                
                // HUD Elements (user requested HideAllHUD toggle)
                root.Add(new XComment(" HUD Element Hiding "));
                root.Add(CreateProperty("HideAllHUDDuringCinematic", s_cinematicSettings.HUDElements.HideAllHUDDuringCinematic));
                
                // Developer Options (ensure log setting is saved)
                root.Add(new XComment(" Developer Options "));
                root.Add(CreateProperty("EnableVerboseLogging", s_cinematicSettings.EnableVerboseLogging));

                // TriggerDefaults Settings
                root.Add(new XComment(" TriggerDefaults Settings "));
                root.Add(CreateProperty("TD_EnableTriggers", s_cinematicSettings.TriggerDefaults.EnableTriggers));
                root.Add(CreateProperty("TD_Duration", s_cinematicSettings.TriggerDefaults.Duration));
                root.Add(CreateProperty("TD_TimeScale", s_cinematicSettings.TriggerDefaults.TimeScale));
                root.Add(CreateProperty("TD_Chance", s_cinematicSettings.TriggerDefaults.Chance));
                root.Add(CreateProperty("TD_Cooldown", s_cinematicSettings.TriggerDefaults.Cooldown));
                root.Add(CreateProperty("TD_FirstPersonCamera", s_cinematicSettings.TriggerDefaults.FirstPersonCamera));
                root.Add(CreateProperty("TD_ProjectileCamera", s_cinematicSettings.TriggerDefaults.ProjectileCamera));
                root.Add(CreateProperty("TD_FirstPersonChance", s_cinematicSettings.TriggerDefaults.FirstPersonChance));
                // TriggerDefaults FP FOV
                root.Add(CreateProperty("TD_FOVMode", (int)s_cinematicSettings.TriggerDefaults.FOVMode));
                root.Add(CreateProperty("TD_FOVPercent", s_cinematicSettings.TriggerDefaults.FOVPercent));
                root.Add(CreateProperty("TD_FOVZoomInDuration", s_cinematicSettings.TriggerDefaults.FOVZoomInDuration));
                root.Add(CreateProperty("TD_FOVHoldDuration", s_cinematicSettings.TriggerDefaults.FOVHoldDuration));
                root.Add(CreateProperty("TD_FOVZoomOutDuration", s_cinematicSettings.TriggerDefaults.FOVZoomOutDuration));
                // TriggerDefaults Projectile FOV
                root.Add(CreateProperty("TD_ProjectileFOVMode", (int)s_cinematicSettings.TriggerDefaults.ProjectileFOVMode));
                root.Add(CreateProperty("TD_ProjectileFOVPercent", s_cinematicSettings.TriggerDefaults.ProjectileFOVPercent));
                root.Add(CreateProperty("TD_ProjectileFOVZoomInDuration", s_cinematicSettings.TriggerDefaults.ProjectileFOVZoomInDuration));
                root.Add(CreateProperty("TD_ProjectileFOVHoldDuration", s_cinematicSettings.TriggerDefaults.ProjectileFOVHoldDuration));
                root.Add(CreateProperty("TD_ProjectileFOVZoomOutDuration", s_cinematicSettings.TriggerDefaults.ProjectileFOVZoomOutDuration));
                // TriggerDefaults Randomization
                root.Add(CreateProperty("TD_RandomizeDuration", s_cinematicSettings.TriggerDefaults.RandomizeDuration));
                root.Add(CreateProperty("TD_DurationMin", s_cinematicSettings.TriggerDefaults.DurationMin));
                root.Add(CreateProperty("TD_DurationMax", s_cinematicSettings.TriggerDefaults.DurationMax));
                root.Add(CreateProperty("TD_RandomizeTimeScale", s_cinematicSettings.TriggerDefaults.RandomizeTimeScale));
                root.Add(CreateProperty("TD_TimeScaleMin", s_cinematicSettings.TriggerDefaults.TimeScaleMin));
                root.Add(CreateProperty("TD_TimeScaleMax", s_cinematicSettings.TriggerDefaults.TimeScaleMax));
                
                // Master Trigger Chance
                root.Add(CreateProperty("MasterTriggerChance", s_cinematicSettings.MasterTriggerChance));
                
                // Camera Overrides per Weapon Type
                root.Add(new XComment(" Weapon Mode Camera Overrides "));
                root.Add(CreateProperty("MeleeCameraOverride", (int)s_cinematicSettings.MeleeCameraOverride));
                root.Add(CreateProperty("RangedCameraOverride", (int)s_cinematicSettings.RangedCameraOverride));
                root.Add(CreateProperty("BowCameraOverride", (int)s_cinematicSettings.BowCameraOverride));
                root.Add(CreateProperty("ExplosiveCameraOverride", (int)s_cinematicSettings.ExplosiveCameraOverride));
                root.Add(CreateProperty("TrapCameraOverride", (int)s_cinematicSettings.TrapCameraOverride));

                // Freeze Frame Settings (Per-camera: FP and Projectile)
                root.Add(new XComment(" Freeze Frame - First Person "));
                var fpFreeze = s_cinematicSettings.FPFreezeFrame;
                root.Add(CreateProperty("FPFreeze_Enabled", fpFreeze.Enabled));
                root.Add(CreateProperty("FPFreeze_Chance", fpFreeze.Chance));
                root.Add(CreateProperty("FPFreeze_Duration", fpFreeze.Duration));
                root.Add(CreateProperty("FPFreeze_Delay", fpFreeze.Delay));
                root.Add(CreateProperty("FPFreeze_TriggerOnBasicKill", fpFreeze.TriggerOnBasicKill));
                root.Add(CreateProperty("FPFreeze_TriggerOnSpecialTrigger", fpFreeze.TriggerOnSpecialTrigger));
                root.Add(CreateProperty("FPFreeze_EnableCameraMovement", fpFreeze.EnableCameraMovement));
                root.Add(CreateProperty("FPFreeze_TimeScale", fpFreeze.TimeScale));
                root.Add(CreateProperty("FPFreeze_PostAction", (int)fpFreeze.PostAction));
                root.Add(CreateProperty("FPFreeze_EnableContrastEffect", fpFreeze.EnableContrastEffect));
                root.Add(CreateProperty("FPFreeze_ContrastAmount", fpFreeze.ContrastAmount));
                
                root.Add(new XComment(" Freeze Frame - Projectile "));
                var projFreeze = s_cinematicSettings.ProjectileFreezeFrame;
                root.Add(CreateProperty("ProjFreeze_Enabled", projFreeze.Enabled));
                root.Add(CreateProperty("ProjFreeze_Chance", projFreeze.Chance));
                root.Add(CreateProperty("ProjFreeze_Duration", projFreeze.Duration));
                root.Add(CreateProperty("ProjFreeze_Delay", projFreeze.Delay));
                root.Add(CreateProperty("ProjFreeze_TriggerOnBasicKill", projFreeze.TriggerOnBasicKill));
                root.Add(CreateProperty("ProjFreeze_TriggerOnSpecialTrigger", projFreeze.TriggerOnSpecialTrigger));
                root.Add(CreateProperty("ProjFreeze_EnableCameraMovement", projFreeze.EnableCameraMovement));
                root.Add(CreateProperty("ProjFreeze_TimeScale", projFreeze.TimeScale));
                root.Add(CreateProperty("ProjFreeze_RandomizePreset", projFreeze.RandomizePreset));
                root.Add(CreateProperty("ProjFreeze_PostAction", (int)projFreeze.PostAction));
                root.Add(CreateProperty("ProjFreeze_RandomizePostCamera", projFreeze.RandomizePostCamera));
                root.Add(CreateProperty("ProjFreeze_EnableContrastEffect", projFreeze.EnableContrastEffect));
                root.Add(CreateProperty("ProjFreeze_ContrastAmount", projFreeze.ContrastAmount));

                // Experimental Settings  
                root.Add(new XComment(" Experimental Features "));
                var exp = s_cinematicSettings.Experimental;
                
                // X-Ray Vision
                root.Add(CreateProperty("Exp_EnableXRayVision", exp.EnableXRayVision));
                root.Add(CreateProperty("Exp_XRayDuration", exp.XRayDuration));
                root.Add(CreateProperty("Exp_XRayIntensity", exp.XRayIntensity));
                
                // Predator Vision
                root.Add(CreateProperty("Exp_EnablePredatorVision", exp.EnablePredatorVision));
                root.Add(CreateProperty("Exp_PredatorVisionDuration", exp.PredatorVisionDuration));
                root.Add(CreateProperty("Exp_PredatorVisionIntensity", exp.PredatorVisionIntensity));
                
                // Projectile Ride Cam
                root.Add(CreateProperty("Exp_EnableProjectileRideCam", exp.EnableProjectileRideCam));
                root.Add(CreateProperty("Exp_RideCamFOV", exp.RideCamFOV));
                root.Add(CreateProperty("Exp_RideCamChance", exp.RideCamChance));
                root.Add(CreateProperty("Exp_RideCamOffset", exp.RideCamOffset));
                root.Add(CreateProperty("Exp_RideCamPredictiveAiming", exp.RideCamPredictiveAiming));
                root.Add(CreateProperty("Exp_RideCamMinTargetHealth", exp.RideCamMinTargetHealth));
                
                // Dismemberment Focus Cam
                root.Add(CreateProperty("Exp_EnableDismemberFocusCam", exp.EnableDismemberFocusCam));
                root.Add(CreateProperty("Exp_FocusCamDistance", exp.FocusCamDistance));
                root.Add(CreateProperty("Exp_FocusCamDuration", exp.FocusCamDuration));
                
                // Last Stand / Second Wind
                root.Add(CreateProperty("Exp_EnableLastStand", exp.EnableLastStand));
                root.Add(CreateProperty("Exp_LastStandDuration", exp.LastStandDuration));
                root.Add(CreateProperty("Exp_LastStandTimeScale", exp.LastStandTimeScale));
                root.Add(CreateProperty("Exp_LastStandReviveHealth", exp.LastStandReviveHealth));
                root.Add(CreateProperty("Exp_LastStandCooldown", exp.LastStandCooldown));
                root.Add(CreateProperty("Exp_LastStandInfiniteAmmo", exp.LastStandInfiniteAmmo));
                
                // Chain Reaction
                root.Add(CreateProperty("Exp_EnableChainReaction", exp.EnableChainReaction));
                root.Add(CreateProperty("Exp_ChainReactionWindow", exp.ChainReactionWindow));
                root.Add(CreateProperty("Exp_ChainReactionMaxKills", exp.ChainReactionMaxKills));
                root.Add(CreateProperty("Exp_ChainCameraTransitionTime", exp.ChainCameraTransitionTime));
                root.Add(CreateProperty("Exp_ChainReactionSlowMoRamp", exp.ChainReactionSlowMoRamp));
                root.Add(CreateProperty("Exp_ChainSlowMoMultiplier", exp.ChainSlowMoMultiplier));
                
                // Slow-Mo Toggle
                root.Add(CreateProperty("Exp_EnableSlowMoToggle", exp.EnableSlowMoToggle));
                root.Add(CreateProperty("Exp_SlowMoToggleKey", exp.SlowMoToggleKey.ToString()));
                root.Add(CreateProperty("Exp_SlowMoToggleTimeScale", exp.SlowMoToggleTimeScale));

                root.Add(new XComment(" Menu "));
                root.Add(CreateProperty("MenuKey", s_cinematicSettings.MenuKey.ToString()));

                doc.Save(s_configPath);
                CKLog.Verbose($" Settings saved to {s_configPath} (GlobalCD={s_cinematicSettings.CooldownGlobal:F2}, CritCD={s_cinematicSettings.CooldownCrit:F2}, IgnoreCorpseHits={s_cinematicSettings.IgnoreCorpseHits})");
                
                // Settings are already applied in memory - no need to reload.
            }
            catch (Exception ex)
            {
                Log.Error($"CinematicKill: Failed to save settings: {ex.Message}");
            }
        }

        private static XElement CreateProperty(string name, object value)
        {
            string serialized = value switch
            {
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value?.ToString() ?? string.Empty
            };

            return new XElement("Property", new XAttribute("name", name), new XAttribute("value", serialized));
        }

        private static bool ParseBool(string value, bool defaultValue)
        {
            if (bool.TryParse(value, out var parsed))
            {
                return parsed;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
            {
                return numeric != 0;
            }

            return defaultValue;
        }
        private static int ScanForEnemies(Vector3 center, float radius, int excludeEntityId)
        {
            int count = 0;
            var entities = GameManager.Instance.World.Entities.list;
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.entityId != excludeEntityId && entity is EntityEnemy && entity.IsAlive() && Vector3.Distance(center, entity.position) <= radius)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Get trigger settings by reason name
        /// </summary>
        private static CKTriggerSettings GetTriggerSettingsByReason(string reason)
        {
            if (s_cinematicSettings == null) return null;
            
            switch (reason)
            {
                case "Headshot": return s_cinematicSettings.Headshot;
                case "Critical": return s_cinematicSettings.Critical;
                case "LastEnemy": return s_cinematicSettings.LastEnemy;
                case "LongRange": return s_cinematicSettings.LongRange;
                case "LowHealth": return s_cinematicSettings.LowHealth;
                case "Dismember": return s_cinematicSettings.Dismember;
                case "Killstreak": return s_cinematicSettings.Killstreak;
                case "Sneak": return s_cinematicSettings.Sneak;
                default: return null;
            }
        }

        #endregion Initialization
    }
}
