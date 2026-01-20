// ═══════════════════════════════════════════════════════════════════════════════
// CinematicKillSettings.cs - Configuration and settings for CinematicKill mod
// ═══════════════════════════════════════════════════════════════════════════════
//
// STRUCTURE:
//   CinematicKillSettings     - Main settings container with convenience properties
//   TriggerSettings           - Per-trigger configuration (Headshot, Critical, etc.)
//   ModeSettings              - Per-weapon-mode configuration (Melee, Ranged, etc.)
//   ScreenEffectsSettings     - Visual effect configuration
//   KillstreakSettings        - Killstreak bonus configuration
//   StandardCameraPreset      - Camera position presets
//
// SERIALIZATION:
//   Loaded/saved to XML in CinematicKillManager.LoadConfig() and SaveSettingsToFile()
//
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using UnityEngine;

namespace CinematicKill
{
    /// <summary>
    /// FOV zoom mode for cinematics
    /// </summary>
    public enum FOVMode
    {
        Off = 0,      // No FOV effect
        ZoomIn = 1,   // Reduce FOV (zoom in) by percentage
        ZoomOut = 2   // Increase FOV (zoom out) by percentage
    }

    /// <summary>
    /// Camera override mode - forces a specific camera type
    /// </summary>
    public enum CameraOverride
    {
        Auto = 0,           // Use normal selection logic
        FirstPersonOnly = 1, // Force first person camera
        ProjectileOnly = 2   // Force projectile/hitscan camera
    }

    /// <summary>
    /// Camera position presets for projectile/hitscan camera
    /// </summary>
    public enum CameraPosition
    {
        Behind = 0,    // Behind zombie, slight elevation
        Front = 1,     // Facing zombie toward player
        LeftSide = 2,  // Side view from zombie's left
        RightSide = 3, // Side view from zombie's right
        Above = 4,     // Bird's eye looking down
        LowAngle = 5   // Dramatic upward shot
    }

    /// <summary>
    /// Simplified settings container for Cinematic Kill mod.
    /// Three-tier hierarchy: Basic Kill → Special Triggers (defaults) → Per-Trigger Overrides
    /// </summary>
    public sealed class CinematicKillSettings
    {
        public static CinematicKillSettings Default => new();

        // Menu key binding
        public KeyCode MenuKey = KeyCode.Backslash;

        // Master toggle
        public bool EnableCinematics = true;
        
        // Developer settings
        public bool EnableVerboseLogging = false;
        
        // ═══════════════════════════════════════════════════════════════════════
        // LAST STAND / SECOND WIND - Trigger cinematic on player near-death
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableLastStand = false;             // Master toggle
        public float LastStandDuration = 5.0f;           // How long player has to get a kill (1-10s)
        public float LastStandTimeScale = 0.1f;          // Extreme time dilation (0.05-0.3)
        public float LastStandReviveHealth = 25f;        // Health restored on successful kill (10-50)
        public float LastStandCooldown = 60f;            // Cooldown before can trigger again (30-120s)
        public bool LastStandInfiniteAmmo = true;        // Grant infinite ammo during last stand
        
        // Experimental features
        public CKExperimentalSettings Experimental = new CKExperimentalSettings();
        
        // ═══════════════════════════════════════════════════════════════════════
        // FREEZE FRAME - Independent settings for each camera type
        // ═══════════════════════════════════════════════════════════════════════
        public CKFreezeFrameSettings FPFreezeFrame = new CKFreezeFrameSettings();
        public CKFreezeFrameSettings ProjectileFreezeFrame = new CKFreezeFrameSettings();
        
        // ═══════════════════════════════════════════════════════════════════════
        // LEGACY COMPATIBILITY - MenuV2 provides old structure for Manager
        // NOTE: No caching! Always returns fresh instance to reflect current settings
        // ═══════════════════════════════════════════════════════════════════════
        public CKMenuV2Settings MenuV2 
        { 
            get => CKMenuV2Settings.CreateFromSettings(this);
            set { } // No-op setter for backward compatibility
        }
        
        /// <summary>
        /// Legacy method - no longer needed since MenuV2 is not cached.
        /// Kept for API compatibility.
        /// </summary>
        [Obsolete("MenuV2 is no longer cached - this method is a no-op")]
        public void InvalidateMenuV2Cache() { }
        
        // Legacy property forwards
        public bool EnableTriggers { get => TriggerDefaults.EnableTriggers; set => TriggerDefaults.EnableTriggers = value; }
        public bool EnableFirstPersonCamera { get => TriggerDefaults.FirstPersonCamera; set => TriggerDefaults.FirstPersonCamera = value; }
        public bool EnableProjectileCamera { get => TriggerDefaults.ProjectileCamera; set => TriggerDefaults.ProjectileCamera = value; }
        public bool EnableFirstPersonSlowMo { get => TriggerDefaults.EnableTriggers; set { } }
        public float FirstPersonSlowScale { get => TriggerDefaults.TimeScale; set => TriggerDefaults.TimeScale = value; }
        public float FirstPersonDuration { get => TriggerDefaults.Duration; set => TriggerDefaults.Duration = value; }
        public float FirstPersonCameraChance { get => TriggerDefaults.FirstPersonChance / 100f; set => TriggerDefaults.FirstPersonChance = value * 100f; }
        public float KillcamChance { get => BasicKill.Chance / 100f; set => BasicKill.Chance = value * 100f; }
        public float CooldownGlobal { get => TriggerDefaults.Cooldown; set => TriggerDefaults.Cooldown = value; }
        public bool EnableGlobalCooldown { get => true; set { } }
        public float DistanceThreshold { get => LongRangeDistance; set => LongRangeDistance = value; }
        public float LowHealthThreshold { get => LowHealthPercent / 100f; set => LowHealthPercent = value * 100f; }
        public float EnemyScanRadius_Legacy { get => EnemyScanRadius; set => EnemyScanRadius = value; }
        public bool IgnoreCorpseHits { get; set; } = true;
        
        // Legacy trigger access
        public bool EnableCrit { get => Critical.Enabled; set => Critical.Enabled = value; }
        public bool EnableDismember { get => Dismember.Enabled; set => Dismember.Enabled = value; }
        public bool EnableLongRange { get => LongRange.Enabled; set => LongRange.Enabled = value; }
        public bool EnableLowHealth { get => LowHealth.Enabled; set => LowHealth.Enabled = value; }
        public bool EnableLastEnemy { get => LastEnemy.Enabled; set => LastEnemy.Enabled = value; }
        public float ChanceCrit { get => 1f; set { } }
        public float ChanceDismember { get => 1f; set { } }
        public float ChanceLongRange { get => 1f; set { } }
        public float ChanceLowHealth { get => 1f; set { } }
        public float CooldownCrit { get => Critical.Cooldown; set => Critical.Cooldown = value; }
        public float CooldownDismember { get => Dismember.Cooldown; set => Dismember.Cooldown = value; }
        public float CooldownLongRange { get => LongRange.Cooldown; set => LongRange.Cooldown = value; }
        public float CooldownLowHealth { get => LowHealth.Cooldown; set => LowHealth.Cooldown = value; }
        public float CooldownLastEnemy { get => LastEnemy.Cooldown; set => LastEnemy.Cooldown = value; }
        public bool EnableCooldownCrit { get => Critical.Cooldown > 0; set { } }
        public bool EnableCooldownDismember { get => Dismember.Cooldown > 0; set { } }
        public bool EnableCooldownLongRange { get => LongRange.Cooldown > 0; set { } }
        public bool EnableCooldownLowHealth { get => LowHealth.Cooldown > 0; set { } }
        public bool EnableCooldownLastEnemy { get => LastEnemy.Cooldown > 0; set { } }
        public bool AllowLastEnemyFirstPerson { get => LastEnemy.FirstPersonCamera; set => LastEnemy.FirstPersonCamera = value; }
        public bool AllowLastEnemyProjectile { get => LastEnemy.ProjectileCamera; set => LastEnemy.ProjectileCamera = value; }
        
        // Hitstop
        public bool EnableHitstop { get => ScreenEffects.EnableHitstop; set => ScreenEffects.EnableHitstop = value; }
        public float HitstopDuration { get => ScreenEffects.HitstopDuration; set => ScreenEffects.HitstopDuration = value; }
        
        // Vignette
        public bool EnableVignette { get => ScreenEffects.EnableVignette; set => ScreenEffects.EnableVignette = value; }
        public float VignetteIntensity { get => ScreenEffects.VignetteIntensity; set => ScreenEffects.VignetteIntensity = value; }
        
        // FOV
        public bool EnableFOVEffect { get => TriggerDefaults.FOVEnabled; set => TriggerDefaults.FOVEnabled = value; }
        public float FOVZoomAmount { get => (1f - TriggerDefaults.FOVMultiplier) * 100f; set => TriggerDefaults.FOVMultiplier = 1f - (value / 100f); }
        
        // Advanced FOV timing - when enabled, shows Return slider in UI
        public bool EnableAdvancedFOVTiming = false;
        
        // Audio slow-mo - reduces volume during slow motion for dramatic effect
        public bool EnableAudioSlowMo = true;
        
        // Dynamic ragdoll duration - when enabled, cinematic ends when ragdoll hits ground instead of fixed time
        // SEPARATE settings for Basic Kill vs Special Triggers AND per camera type
        // Basic Kill ragdoll settings
        public bool EnableDynamicRagdollDuration_BK_FP = false;    // Basic Kill - First Person camera
        public bool EnableDynamicRagdollDuration_BK_Proj = false;  // Basic Kill - Projectile camera
        public float RagdollPostLandDelay_BK_FP = 0.3f;            // Post-land delay for BK FP
        public float RagdollPostLandDelay_BK_Proj = 0.3f;          // Post-land delay for BK Proj
        
        // Special Triggers ragdoll settings  
        public bool EnableDynamicRagdollDuration_TD_FP = false;    // Trigger Defaults - First Person camera
        public bool EnableDynamicRagdollDuration_TD_Proj = false;  // Trigger Defaults - Projectile camera
        public float RagdollPostLandDelay_TD_FP = 0.3f;            // Post-land delay for TD FP
        public float RagdollPostLandDelay_TD_Proj = 0.3f;          // Post-land delay for TD Proj
        
        public float RagdollFallbackDuration = 5.0f;               // Maximum duration if ragdoll never hits floor
        
        // Legacy properties for backward compatibility
        public bool EnableDynamicRagdollDuration_FP
        {
            get => EnableDynamicRagdollDuration_BK_FP || EnableDynamicRagdollDuration_TD_FP;
            set { EnableDynamicRagdollDuration_BK_FP = value; EnableDynamicRagdollDuration_TD_FP = value; }
        }
        public bool EnableDynamicRagdollDuration_Proj
        {
            get => EnableDynamicRagdollDuration_BK_Proj || EnableDynamicRagdollDuration_TD_Proj;
            set { EnableDynamicRagdollDuration_BK_Proj = value; EnableDynamicRagdollDuration_TD_Proj = value; }
        }
        public float RagdollPostLandDelay_FP
        {
            get => RagdollPostLandDelay_BK_FP;
            set { RagdollPostLandDelay_BK_FP = value; RagdollPostLandDelay_TD_FP = value; }
        }
        public float RagdollPostLandDelay_Proj
        {
            get => RagdollPostLandDelay_BK_Proj;
            set { RagdollPostLandDelay_BK_Proj = value; RagdollPostLandDelay_TD_Proj = value; }
        }
        public float RagdollPostLandDelay
        {
            get => RagdollPostLandDelay_BK_Proj;
            set { RagdollPostLandDelay_BK_FP = value; RagdollPostLandDelay_BK_Proj = value; RagdollPostLandDelay_TD_FP = value; RagdollPostLandDelay_TD_Proj = value; }
        }
        public bool EnableDynamicRagdollDuration
        {
            get => EnableDynamicRagdollDuration_BK_FP || EnableDynamicRagdollDuration_BK_Proj || EnableDynamicRagdollDuration_TD_FP || EnableDynamicRagdollDuration_TD_Proj;
            set { EnableDynamicRagdollDuration_BK_FP = value; EnableDynamicRagdollDuration_BK_Proj = value; EnableDynamicRagdollDuration_TD_FP = value; EnableDynamicRagdollDuration_TD_Proj = value; }
        }
        
        // No-op legacy properties
        public bool AdvancedMode { get; set; } = false;
        public void Clamp() { }

        // ═══════════════════════════════════════════════════════════════════════
        // ADDITIONAL LEGACY PROPERTY FORWARDS
        // These properties existed in the old 400+ setting system but are now
        // simplified. They return sensible defaults or forward to new structs.
        // ═══════════════════════════════════════════════════════════════════════

        // First-person camera legacy
        public float FirstPersonDurationMin { get => TriggerDefaults.Duration * 0.5f; set { } }
        public float FirstPersonDurationMax { get => TriggerDefaults.Duration * 1.5f; set { } }
        public bool RandomizeFirstPersonDuration { get; set; } = false;
        public float FirstPersonSlowScaleMin { get => TriggerDefaults.TimeScale * 0.5f; set { } }
        public float FirstPersonSlowScaleMax { get => TriggerDefaults.TimeScale * 1.5f; set { } }
        public bool RandomizeFirstPersonSlowScale { get; set; } = false;
        public float FirstPersonCameraChanceMin { get => 30f; set { } }
        public float FirstPersonCameraChanceMax { get => 70f; set { } }
        public bool RandomizeFirstPersonCameraChance { get; set; } = false;
        public float FirstPersonReturnPercent { get => 0.3f; set { } }
        public float FirstPersonReturnStart { get => 0.5f; set { } }
        
        // Hitstop legacy
        public bool EnableFirstPersonHitstop { get => ScreenEffects.EnableHitstop; set => ScreenEffects.EnableHitstop = value; }
        public bool EnableProjectileHitstop { get => ScreenEffects.EnableHitstop; set => ScreenEffects.EnableHitstop = value; }
        public float FirstPersonHitstopDuration { get => ScreenEffects.HitstopDuration; set => ScreenEffects.HitstopDuration = value; }
        public float FirstPersonHitstopDurationMin { get => ScreenEffects.HitstopDuration * 0.5f; set { } }
        public float FirstPersonHitstopDurationMax { get => ScreenEffects.HitstopDuration * 1.5f; set { } }
        public bool RandomizeFirstPersonHitstopDuration { get; set; } = false;
        public bool FirstPersonHitstopOnCritOnly { get; set; } = false;
        public float ProjectileHitstopDuration { get => ScreenEffects.HitstopDuration; set => ScreenEffects.HitstopDuration = value; }
        public float ProjectileHitstopDurationMin { get => ScreenEffects.HitstopDuration * 0.5f; set { } }
        public float ProjectileHitstopDurationMax { get => ScreenEffects.HitstopDuration * 1.5f; set { } }
        public bool RandomizeProjectileHitstopDuration { get; set; } = false;
        public bool ProjectileHitstopOnCritOnly { get; set; } = false;
        public bool HitstopOnCritOnly { get; set; } = false;
        
        // FOV legacy
        public bool EnableFOVZoom { get => TriggerDefaults.FOVEnabled; set => TriggerDefaults.FOVEnabled = value; }
        public float FOVZoomMultiplier { get => TriggerDefaults.FOVMultiplier; set => TriggerDefaults.FOVMultiplier = value; }
        public float FOVZoomAmountMin { get => 5f; set { } }
        public float FOVZoomAmountMax { get => 25f; set { } }
        public bool RandomizeFOVAmount { get; set; } = false;
        public float FOVZoomInDuration { get => 0.15f; set { } }
        public float FOVZoomInDurationMin { get => 0.1f; set { } }
        public float FOVZoomInDurationMax { get => 0.3f; set { } }
        public bool RandomizeFOVIn { get; set; } = false;
        public float FOVZoomHoldDuration { get => 0.5f; set { } }
        public float FOVZoomHoldDurationMin { get => 0.3f; set { } }
        public float FOVZoomHoldDurationMax { get => 1f; set { } }
        public bool RandomizeFOVHold { get; set; } = false;
        public float FOVZoomOutDuration { get => 0.2f; set { } }
        public float FOVZoomOutDurationMin { get => 0.1f; set { } }
        public float FOVZoomOutDurationMax { get => 0.4f; set { } }
        public bool RandomizeFOVOut { get; set; } = false;
        public bool AllowFOVBeyondDuration { get; set; } = false;
        public bool AllowProjectileFOVBeyondDuration { get; set; } = false;
        public bool AllowProjectileSlowBeyondDuration { get; set; } = false;
        
        // Vignette legacy
        public float VignetteIntensityMin { get => ScreenEffects.VignetteIntensity * 0.5f; set { } }
        public float VignetteIntensityMax { get => ScreenEffects.VignetteIntensity * 1.5f; set { } }
        public bool RandomizeVignetteIntensity { get; set; } = false;
        
        // Color grading legacy
        public bool EnableColorGrading { get; set; } = false;
        public float ColorGradingIntensity { get; set; } = 0f;
        public float ColorGradingIntensityMin { get => 0f; set { } }
        public float ColorGradingIntensityMax { get => 1f; set { } }
        public bool RandomizeColorGradingIntensity { get; set; } = false;
        public int ColorGradingMode { get; set; } = 0;
        
        // Flash legacy
        public bool EnableFlash { get; set; } = false;
        public float FlashIntensity { get; set; } = 0.3f;
        public float FlashIntensityMin { get => 0.1f; set { } }
        public float FlashIntensityMax { get => 0.5f; set { } }
        public bool RandomizeFlashIntensity { get; set; } = false;
        
        // SlowMo sound legacy
        public bool EnableSlowMoSound { get; set; } = false;
        public bool EnableFirstPersonSlowMoSound { get; set; } = false;
        public bool EnableProjectileSlowMoSound { get; set; } = false;
        public float SlowMoSoundVolume { get; set; } = 0.5f;
        public float FirstPersonSlowMoSoundVolume { get; set; } = 0.5f;
        public float FirstPersonSlowMoSoundVolumeMin { get => 0.3f; set { } }
        public float FirstPersonSlowMoSoundVolumeMax { get => 0.7f; set { } }
        public bool RandomizeFirstPersonSlowMoSoundVolume { get; set; } = false;
        public float ProjectileSlowMoSoundVolume { get; set; } = 0.5f;
        public float ProjectileSlowMoSoundVolumeMin { get => 0.3f; set { } }
        public float ProjectileSlowMoSoundVolumeMax { get => 0.7f; set { } }
        public bool RandomizeProjectileSlowMoSoundVolume { get; set; } = false;
        
        // Projectile camera legacy
        public bool EnableProjectileSlowMo { get => TriggerDefaults.EnableTriggers; set { } }
        public float ProjectileCameraChance { get => 100f - TriggerDefaults.FirstPersonChance; set { } }
        public float ProjectileCameraChanceMin { get => 30f; set { } }
        public float ProjectileCameraChanceMax { get => 70f; set { } }
        public bool RandomizeProjectileChance { get; set; } = false;
        public float ProjectileCameraDuration { get => TriggerDefaults.Duration; set => TriggerDefaults.Duration = value; }
        public float ProjectileCameraDurationMin { get => TriggerDefaults.Duration * 0.5f; set { } }
        public float ProjectileCameraDurationMax { get => TriggerDefaults.Duration * 1.5f; set { } }
        public bool RandomizeProjectileDuration { get; set; } = false;
        public float ProjectileCameraSlowScale { get => TriggerDefaults.TimeScale; set => TriggerDefaults.TimeScale = value; }
        public float ProjectileCameraSlowScaleMin { get => TriggerDefaults.TimeScale * 0.5f; set { } }
        public float ProjectileCameraSlowScaleMax { get => TriggerDefaults.TimeScale * 1.5f; set { } }
        public bool RandomizeProjectileSlowScale { get; set; } = false;
        public float ProjectileCameraReturnDuration { get => 0.3f; set { } }
        public float ProjectileCameraReturnDurationMin { get => 0.2f; set { } }
        public float ProjectileCameraReturnDurationMax { get => 0.5f; set { } }
        public bool RandomizeProjectileReturnDuration { get; set; } = false;
        public float ProjectileReturnPercent { get => 0.3f; set { } }
        public float ProjectileReturnStart { get => 0.5f; set { } }
        public float ProjectileCameraFOV { get => 60f; set { } }
        public float ProjectileCameraFOVMin { get => 50f; set { } }
        public float ProjectileCameraFOVMax { get => 70f; set { } }
        public bool RandomizeProjectileFOV { get; set; } = false;
        public float ProjectileCameraFOVZoomInDuration { get => 0.15f; set { } }
        public float ProjectileCameraFOVZoomInDurationMin { get => 0.1f; set { } }
        public float ProjectileCameraFOVZoomInDurationMax { get => 0.3f; set { } }
        public bool RandomizeProjectileFOVIn { get; set; } = false;
        public float ProjectileCameraFOVHoldDuration { get => 0.5f; set { } }
        public float ProjectileCameraFOVHoldDurationMin { get => 0.3f; set { } }
        public float ProjectileCameraFOVHoldDurationMax { get => 1f; set { } }
        public bool RandomizeProjectileFOVHold { get; set; } = false;
        public float ProjectileCameraFOVZoomOutDuration { get => 0.2f; set { } }
        public float ProjectileCameraFOVZoomOutDurationMin { get => 0.1f; set { } }
        public float ProjectileCameraFOVZoomOutDurationMax { get => 0.4f; set { } }
        public bool RandomizeProjectileFOVOut { get; set; } = false;
        public float ProjectileCameraHeightOffset { get => ProjectileCamera.Height; set => ProjectileCamera.Height = value; }
        public float ProjectileCameraHeightOffsetMin { get => -0.5f; set { } }
        public float ProjectileCameraHeightOffsetMax { get => 0.5f; set { } }
        public bool RandomizeProjectileHeightOffset { get; set; } = false;
        public float ProjectileCameraXOffset { get => 0f; set { } }
        public float ProjectileCameraXOffsetMin { get => -0.5f; set { } }
        public float ProjectileCameraXOffsetMax { get => 0.5f; set { } }
        public bool RandomizeProjectileXOffset { get; set; } = false;
        public float ProjectileCameraDistanceOffset { get => ProjectileCamera.Distance; set => ProjectileCamera.Distance = value; }
        public float ProjectileCameraDistanceOffsetMin { get => -1f; set { } }
        public float ProjectileCameraDistanceOffsetMax { get => 1f; set { } }
        public bool RandomizeProjectileDistanceOffset { get; set; } = false;
        public float ProjectileCameraLookPitch { get => ProjectileCamera.Tilt; set => ProjectileCamera.Tilt = value; }
        public float ProjectileCameraLookPitchMin { get => -30f; set { } }
        public float ProjectileCameraLookPitchMax { get => 30f; set { } }
        public bool RandomizeProjectileLookPitch { get; set; } = false;
        public float ProjectileCameraLookYaw { get => 0f; set { } }
        public float ProjectileCameraLookYawMin { get => -30f; set { } }
        public float ProjectileCameraLookYawMax { get => 30f; set { } }
        public bool RandomizeProjectileLookYaw { get; set; } = false;
        public float ProjectileCameraRandomPitchRange { get => 15f; set { } }
        public float ProjectileCameraRandomPitchRangeMin { get => 5f; set { } }
        public float ProjectileCameraRandomPitchRangeMax { get => 30f; set { } }
        public bool RandomizeProjectileRandomPitchRange { get; set; } = false;
        public float ProjectileCameraRandomYawRange { get => 15f; set { } }
        public float ProjectileCameraRandomYawRangeMin { get => 5f; set { } }
        public float ProjectileCameraRandomYawRangeMax { get => 30f; set { } }
        public bool RandomizeProjectileRandomYawRange { get; set; } = false;
        public bool ProjectileCameraEnableVignette { get => ScreenEffects.EnableVignette; set { } }
        public float ProjectileCameraVignetteIntensity { get => ScreenEffects.VignetteIntensity; set { } }
        public float ProjectileCameraVignetteIntensityMin { get => 0.1f; set { } }
        public float ProjectileCameraVignetteIntensityMax { get => 0.5f; set { } }
        public bool RandomizeProjectileVignetteIntensity { get; set; } = false;
        public bool ProjectileCameraLastEnemyOnly { get; set; } = false;
        
        // Last enemy overrides
        public bool OverrideLastEnemyFirstPerson { get => LastEnemy.Override; set => LastEnemy.Override = value; }
        public bool OverrideLastEnemyProjectile { get => LastEnemy.Override; set => LastEnemy.Override = value; }
        public float LastEnemyFirstPersonDuration { get => LastEnemy.Duration; set => LastEnemy.Duration = value; }
        public float LastEnemyFirstPersonDurationMin { get => 1f; set { } }
        public float LastEnemyFirstPersonDurationMax { get => 3f; set { } }
        public bool RandomizeLastEnemyFirstPersonDuration { get; set; } = false;
        public float LastEnemyFirstPersonSlowScale { get => LastEnemy.TimeScale; set => LastEnemy.TimeScale = value; }
        public float LastEnemyFirstPersonSlowScaleMin { get => 0.05f; set { } }
        public float LastEnemyFirstPersonSlowScaleMax { get => 0.3f; set { } }
        public bool RandomizeLastEnemyFirstPersonSlowScale { get; set; } = false;
        public float LastEnemyProjectileDuration { get => LastEnemy.Duration; set => LastEnemy.Duration = value; }
        public float LastEnemyProjectileDurationMin { get => 1f; set { } }
        public float LastEnemyProjectileDurationMax { get => 3f; set { } }
        public bool RandomizeLastEnemyProjectileDuration { get; set; } = false;
        public float LastEnemyProjectileSlowScale { get => LastEnemy.TimeScale; set => LastEnemy.TimeScale = value; }
        public float LastEnemyProjectileSlowScaleMin { get => 0.05f; set { } }
        public float LastEnemyProjectileSlowScaleMax { get => 0.3f; set { } }
        public bool RandomizeLastEnemyProjectileSlowScale { get; set; } = false;
        public float LastEnemyProjectileReturnDuration { get => 0.3f; set { } }
        public float LastEnemyProjectileReturnDurationMin { get => 0.2f; set { } }
        public float LastEnemyProjectileReturnDurationMax { get => 0.5f; set { } }
        public bool RandomizeLastEnemyProjectileReturnDuration { get; set; } = false;
        public bool IgnoreGlobalCooldownOnLastEnemy { get; set; } = true;
        
        // Trigger-specific allow camera modes
        public bool DismemberAllowFirstPerson { get => Dismember.FirstPersonCamera; set => Dismember.FirstPersonCamera = value; }
        public bool DismemberAllowProjectile { get => Dismember.ProjectileCamera; set => Dismember.ProjectileCamera = value; }
        public bool DismemberCustomEffects { get; set; } = false;
        public bool CritAllowFirstPerson { get => Critical.FirstPersonCamera; set => Critical.FirstPersonCamera = value; }
        public bool CritAllowProjectile { get => Critical.ProjectileCamera; set => Critical.ProjectileCamera = value; }
        public bool CritCustomEffects { get; set; } = false;
        public bool LongRangeAllowFirstPerson { get => LongRange.FirstPersonCamera; set => LongRange.FirstPersonCamera = value; }
        public bool LongRangeAllowProjectile { get => LongRange.ProjectileCamera; set => LongRange.ProjectileCamera = value; }
        public bool LongRangeCustomEffects { get; set; } = false;
        public bool LowHealthAllowFirstPerson { get => LowHealth.FirstPersonCamera; set => LowHealth.FirstPersonCamera = value; }
        public bool LowHealthAllowProjectile { get => LowHealth.ProjectileCamera; set => LowHealth.ProjectileCamera = value; }
        public bool LowHealthCustomEffects { get; set; } = false;
        public float LowHealthSlowScale { get => LowHealth.TimeScale; set => LowHealth.TimeScale = value; }
        
        // Trigger-specific zoom settings
        public float CritZoomMultiplier { get; set; } = 1f;
        public float CritZoomSpeed { get; set; } = 1f;
        public float LongRangeZoomMultiplier { get; set; } = 1f;
        public float LongRangeZoomSpeed { get; set; } = 1f;
        
        // Trigger-specific hitstop
        public bool CritOverrideHitstop { get; set; } = false;
        public float CritHitstopDuration { get => ScreenEffects.HitstopDuration; set { } }
        public float CritHitstopDurationMin { get => 0.02f; set { } }
        public float CritHitstopDurationMax { get => 0.1f; set { } }
        public bool RandomizeCritHitstopDuration { get; set; } = false;
        public bool CritHitstopOnCritOnly { get; set; } = false;
        public bool DismemberOverrideHitstop { get; set; } = false;
        public float DismemberHitstopDuration { get => ScreenEffects.HitstopDuration; set { } }
        public float DismemberHitstopDurationMin { get => 0.02f; set { } }
        public float DismemberHitstopDurationMax { get => 0.1f; set { } }
        public bool RandomizeDismemberHitstopDuration { get; set; } = false;
        public bool DismemberHitstopOnCritOnly { get; set; } = false;
        public bool LongRangeOverrideHitstop { get; set; } = false;
        public float LongRangeHitstopDuration { get => ScreenEffects.HitstopDuration; set { } }
        public float LongRangeHitstopDurationMin { get => 0.02f; set { } }
        public float LongRangeHitstopDurationMax { get => 0.1f; set { } }
        public bool RandomizeLongRangeHitstopDuration { get; set; } = false;
        public bool LongRangeHitstopOnCritOnly { get; set; } = false;
        public bool LowHealthOverrideHitstop { get; set; } = false;
        public float LowHealthHitstopDuration { get => ScreenEffects.HitstopDuration; set { } }
        public float LowHealthHitstopDurationMin { get => 0.02f; set { } }
        public float LowHealthHitstopDurationMax { get => 0.1f; set { } }
        public bool RandomizeLowHealthHitstopDuration { get; set; } = false;
        public bool LowHealthHitstopOnCritOnly { get; set; } = false;
        
        // Trigger-specific SlowMo sound
        public bool CritOverrideSlowMoSound { get; set; } = false;
        public float CritSlowMoSoundVolume { get; set; } = 0.5f;
        public float CritSlowMoSoundVolumeMin { get => 0.3f; set { } }
        public float CritSlowMoSoundVolumeMax { get => 0.7f; set { } }
        public bool RandomizeCritSlowMoSoundVolume { get; set; } = false;
        public bool DismemberOverrideSlowMoSound { get; set; } = false;
        public float DismemberSlowMoSoundVolume { get; set; } = 0.5f;
        public float DismemberSlowMoSoundVolumeMin { get => 0.3f; set { } }
        public float DismemberSlowMoSoundVolumeMax { get => 0.7f; set { } }
        public bool RandomizeDismemberSlowMoSoundVolume { get; set; } = false;
        public bool LongRangeOverrideSlowMoSound { get; set; } = false;
        public float LongRangeSlowMoSoundVolume { get; set; } = 0.5f;
        public float LongRangeSlowMoSoundVolumeMin { get => 0.3f; set { } }
        public float LongRangeSlowMoSoundVolumeMax { get => 0.7f; set { } }
        public bool RandomizeLongRangeSlowMoSoundVolume { get; set; } = false;
        public bool LowHealthOverrideSlowMoSound { get; set; } = false;
        public float LowHealthSlowMoSoundVolume { get; set; } = 0.5f;
        public float LowHealthSlowMoSoundVolumeMin { get => 0.3f; set { } }
        public float LowHealthSlowMoSoundVolumeMax { get => 0.7f; set { } }
        public bool RandomizeLowHealthSlowMoSoundVolume { get; set; } = false;
        
        // Trigger-specific visual/effect sources
        public string CritVisualSource { get; set; } = "Default";
        public string CritFXSource { get; set; } = "Default";
        public string CritFOVSource { get; set; } = "Default";
        public string DismemberVisualSource { get; set; } = "Default";
        public string DismemberFXSource { get; set; } = "Default";
        public string DismemberFOVSource { get; set; } = "Default";
        public string LongRangeVisualSource { get; set; } = "Default";
        public string LongRangeFXSource { get; set; } = "Default";
        public string LongRangeFOVSource { get; set; } = "Default";
        public string LowHealthVisualSource { get; set; } = "Default";
        public string LowHealthFXSource { get; set; } = "Default";
        public string LowHealthFOVSource { get; set; } = "Default";
        public string ProjectileVisualSource { get; set; } = "Default";
        public string ProjectileFXSource { get; set; } = "Default";
        public string ProjectileFOVSource { get; set; } = "Default";
        
        // Killstreak legacy
        public bool EnableKillstreaks { get => Killstreak.Enabled; set => Killstreak.Enabled = value; }
        public int KillstreakThreshold { get => KillstreakKillsRequired; set => KillstreakKillsRequired = value; }
        public int Tier1Kills { get; set; } = 3;
        public int Tier2Kills { get; set; } = 5;
        public int Tier3Kills { get; set; } = 7;
        public float Tier1BonusDuration { get; set; } = 0.2f;
        public float Tier2BonusDuration { get; set; } = 0.4f;
        public float Tier3BonusDuration { get; set; } = 0.6f;

        // ═══════════════════════════════════════════════════════════════════════
        // BASIC KILL SETTINGS - For any kill without special trigger
        // ═══════════════════════════════════════════════════════════════════════
        public CKBasicKillSettings BasicKill = new();

        // ═══════════════════════════════════════════════════════════════════════
        // SPECIAL TRIGGERS - Default settings used by all triggers
        // ═══════════════════════════════════════════════════════════════════════
        public CKTriggerDefaults TriggerDefaults = new();

        // ═══════════════════════════════════════════════════════════════════════
        // TRIGGER TOGGLES & OVERRIDES - Per-trigger enable and optional overrides
        // Priorities: LastEnemy=100, Killstreak=90, Dismember=80, Headshot=70, Critical=60, LongRange=50, LowHealth=40, Sneak=30
        // ═══════════════════════════════════════════════════════════════════════
        public CKTriggerSettings Headshot = CKTriggerSettings.CreateDefault(true, 70);
        public CKTriggerSettings Critical = CKTriggerSettings.CreateDefault(false, 60);
        public CKTriggerSettings LastEnemy = CKTriggerSettings.CreateLastEnemyDefault();
        public CKTriggerSettings LongRange = CKTriggerSettings.CreateDefault(false, 50);
        public CKTriggerSettings LowHealth = CKTriggerSettings.CreateDefault(false, 40);
        public CKTriggerSettings Dismember = CKTriggerSettings.CreateDefault(false, 80);
        public CKTriggerSettings Killstreak = CKTriggerSettings.CreateDefault(false, 90);
        public CKTriggerSettings Sneak = CKTriggerSettings.CreateDefault(false, 30);

        // Trigger-specific thresholds
        public float MasterTriggerChance = 100f;  // Master chance for ALL special triggers (0-100%)
        public float LongRangeDistance = 25f;
        public float LowHealthPercent = 30f;
        public float EnemyScanRadius = 15f;
        public float KillstreakWindow = 8f;
        public int KillstreakKillsRequired = 3;

        // ═══════════════════════════════════════════════════════════════════════
        // PROJECTILE CAMERA SETTINGS
        // ═══════════════════════════════════════════════════════════════════════
        public CKProjectileCameraSettings ProjectileCamera = new();

        // ═══════════════════════════════════════════════════════════════════════
        // SCREEN EFFECTS
        // ═══════════════════════════════════════════════════════════════════════
        public CKScreenEffects ScreenEffects = new();

        // ═══════════════════════════════════════════════════════════════════════
        // HUD & NOTIFICATIONS
        // ═══════════════════════════════════════════════════════════════════════
        public CKHUDSettings HUD = new();
        public CKToastSettings Toast = new();
        public CKHUDElementSettings HUDElements = new();

        // ═══════════════════════════════════════════════════════════════════════
        // WEAPON MODE SETTINGS
        // ═══════════════════════════════════════════════════════════════════════
        public bool MeleeEnabled = true;
        public bool RangedEnabled = true;
        public bool BowEnabled = true;
        public bool ExplosiveEnabled = true;
        public bool TrapEnabled = true;
        
        // Camera overrides per weapon type
        public CameraOverride MeleeCameraOverride = CameraOverride.Auto;
        public CameraOverride RangedCameraOverride = CameraOverride.Auto;
        public CameraOverride BowCameraOverride = CameraOverride.Auto;
        public CameraOverride ExplosiveCameraOverride = CameraOverride.Auto;
        public CameraOverride TrapCameraOverride = CameraOverride.Auto;
        
        // Smart indoor/outdoor detection - auto-select FP indoors, Projectile outdoors
        public bool SmartIndoorOutdoorDetection = false;
        public float IndoorDetectionHeight = 10f; // Raycast upward distance to detect ceiling

        // Clone method removed - settings are now live and don't need cloning

        /// <summary>
        /// Check if any special triggers are enabled
        /// </summary>
        public bool HasEnabledTriggers()
        {
            return Headshot.Enabled || Critical.Enabled || LastEnemy.Enabled ||
                   LongRange.Enabled || LowHealth.Enabled || Dismember.Enabled ||
                   Killstreak.Enabled || Sneak.Enabled;
        }

        /// <summary>
        /// Get the effective settings for a trigger (override or defaults)
        /// </summary>
        public CKCinematicParams GetEffectiveParams(CKTriggerSettings trigger)
        {
            if (trigger.Override)
            {
                return new CKCinematicParams
                {
                    Duration = trigger.Duration,
                    TimeScale = trigger.TimeScale,
                    Cooldown = trigger.Cooldown,
                    FirstPersonCamera = trigger.FirstPersonCamera,
                    ProjectileCamera = trigger.ProjectileCamera,
                    FirstPersonChance = trigger.FirstPersonChance,
                    FOVEnabled = trigger.FOVEnabled,
                    FOVMultiplier = trigger.FOVMultiplier
                };
            }
            else
            {
                return new CKCinematicParams
                {
                    Duration = TriggerDefaults.Duration,
                    TimeScale = TriggerDefaults.TimeScale,
                    Cooldown = 0f, // No global cooldown for triggers
                    FirstPersonCamera = TriggerDefaults.FirstPersonCamera,
                    ProjectileCamera = TriggerDefaults.ProjectileCamera,
                    FirstPersonChance = TriggerDefaults.FirstPersonChance,
                    FOVEnabled = TriggerDefaults.FOVEnabled,
                    FOVMultiplier = TriggerDefaults.FOVMultiplier
                };
            }
        }
    }

    /// <summary>
    /// Effective cinematic parameters (resolved from defaults or overrides)
    /// </summary>
    public struct CKCinematicParams
    {
        public float Duration;
        public float TimeScale;
        public float Cooldown;
        public bool FirstPersonCamera;
        public bool ProjectileCamera;
        public float FirstPersonChance;
        public bool FOVEnabled;
        public float FOVMultiplier;
    }

    /// <summary>
    /// Basic Kill settings - used for any kill without a special trigger
    /// </summary>
    [Serializable]
    public class CKBasicKillSettings
    {
        public bool Enabled = true;
        public float Chance = 15f;            // User default ~14.86%
        public float Duration = 2f;           // User default 2s
        public float TimeScale = 0.2f;        // User default 0.2
        public float Cooldown = 10f;          // User default ~10s
        public bool FirstPersonCamera = true;
        public bool ProjectileCamera = false; // User default: disabled
        public float FirstPersonChance = 60f; // User default 60%
        public FOVMode FOVMode = FOVMode.ZoomIn;  // FP camera: Off, ZoomIn, or ZoomOut
        public float FOVPercent = 25f;        // User default ~25%
        
        // Projectile camera FOV settings
        public FOVMode ProjectileFOVMode = FOVMode.ZoomIn;  // Projectile camera: Off, ZoomIn, or ZoomOut
        public float ProjectileFOVPercent = 15f;       // Projectile camera: 0-50%, percentage to zoom in/out
        
        // FP camera FOV timing (defaults: 70% zoom in, 20% hold, 10% zoom out)
        public float FOVZoomInDuration = 0.84f;   // 70% of ~1.2s cinematic
        public float FOVHoldDuration = 0.24f;     // 20% of ~1.2s cinematic
        public float FOVZoomOutDuration = 0.12f;  // 10% of ~1.2s cinematic
        
        // Projectile camera FOV timing (same 70/20/10 defaults)
        public float ProjectileFOVZoomInDuration = 0.84f;
        public float ProjectileFOVHoldDuration = 0.24f;
        public float ProjectileFOVZoomOutDuration = 0.12f;
        
        // Randomization - when enabled, value is random between Min and Max at runtime
        public bool RandomizeDuration = false;
        public float DurationMin = 0.8f;
        public float DurationMax = 2.0f;
        
        public bool RandomizeTimeScale = false;
        public float TimeScaleMin = 0.1f;
        public float TimeScaleMax = 0.3f;
        
        public bool RandomizeChance = false;
        public float ChanceMin = 15f;
        public float ChanceMax = 40f;
        
        // Legacy compatibility
        public bool FOVEnabled { get => FOVMode != FOVMode.Off; set => FOVMode = value ? FOVMode.ZoomIn : FOVMode.Off; }
        public float FOVMultiplier { get => 1f - (FOVPercent / 100f); set => FOVPercent = (1f - value) * 100f; }
    }

    /// <summary>
    /// Default settings for all special triggers (can be overridden per-trigger)
    /// </summary>
    [Serializable]
    public class CKTriggerDefaults
    {
        public bool EnableTriggers = true;    // Master toggle for special triggers
        public float Chance = 33f;            // User default 33%
        public float Duration = 3f;           // User default 3s
        public float TimeScale = 0.1f;        // User default ~0.1
        public float Cooldown = 10f;          // User default 10s
        public bool FirstPersonCamera = false; // User default: disabled
        public bool ProjectileCamera = true;   // User default: enabled
        public float FirstPersonChance = 50f;
        public FOVMode FOVMode = FOVMode.ZoomIn;  // FP camera: Off, ZoomIn, or ZoomOut
        public float FOVPercent = 20f;        // User default 20%
        
        // Projectile camera FOV settings
        public FOVMode ProjectileFOVMode = FOVMode.ZoomIn;  // Projectile camera: Off, ZoomIn, or ZoomOut
        public float ProjectileFOVPercent = 40f;       // User default ~40%
        
        // FP camera FOV timing (defaults: 70% zoom in, 20% hold, 10% zoom out)
        public float FOVZoomInDuration = 0.84f;   // 70% of ~1.2s cinematic
        public float FOVHoldDuration = 0.24f;     // 20% of ~1.2s cinematic
        public float FOVZoomOutDuration = 0.12f;  // 10% of ~1.2s cinematic
        
        // Projectile camera FOV timing (same 70/20/10 defaults)
        public float ProjectileFOVZoomInDuration = 0.84f;
        public float ProjectileFOVHoldDuration = 0.24f;
        public float ProjectileFOVZoomOutDuration = 0.12f;
        
        // Randomization support for trigger defaults
        public bool RandomizeDuration = false;
        public float DurationMin = 1.0f;
        public float DurationMax = 2.0f;
        
        public bool RandomizeTimeScale = false;
        public float TimeScaleMin = 0.1f;
        public float TimeScaleMax = 0.25f;
        
        // Legacy compatibility
        public bool FOVEnabled { get => FOVMode != FOVMode.Off; set => FOVMode = value ? FOVMode.ZoomIn : FOVMode.Off; }
        public float FOVMultiplier { get => 1f - (FOVPercent / 100f); set => FOVPercent = (1f - value) * 100f; }
    }

    /// <summary>
    /// Per-trigger settings with optional override
    /// </summary>
    [Serializable]
    public class CKTriggerSettings
    {
        // Enable toggle - only enabled triggers can cause cinematics
        public bool Enabled = false;

        // Override toggle - when true, use these settings instead of defaults
        public bool Override = false;

        // Priority for trigger evaluation order (higher = checked first)
        public int Priority = 50;

        // Override chance flag - when true, use per-trigger Chance instead of master chance
        public bool OverrideChance = false;
        
        // Trigger chance - probability this trigger will fire (0-100%) - only used when OverrideChance=true
        public float Chance = 100f;

        // Cinematic settings (only used when Override = true)
        public float Duration = 1.5f;
        public float TimeScale = 0.15f;
        public float Cooldown = 2f;
        public bool FirstPersonCamera = true;
        public bool ProjectileCamera = true;
        public float FirstPersonChance = 50f;
        public FOVMode FOVMode = FOVMode.ZoomIn;  // FP camera: Off, ZoomIn, or ZoomOut
        public float FOVPercent = 20f;       // FP camera: 0-50%, percentage to zoom in/out
        
        // Projectile camera FOV settings
        public FOVMode ProjectileFOVMode = FOVMode.ZoomIn;  // Projectile camera: Off, ZoomIn, or ZoomOut
        public float ProjectileFOVPercent = 15f;       // Projectile camera: 0-50%, percentage to zoom in/out
        
        // Projectile camera preset override - multi-select like main Camera tab
        public bool OverridePresets = false;
        public bool[] EnabledPresets = new bool[7]; // Matches StandardCameraPreset.All.Length
        
        // Legacy compatibility
        public bool FOVEnabled { get => FOVMode != FOVMode.Off; set => FOVMode = value ? FOVMode.ZoomIn : FOVMode.Off; }
        public float FOVMultiplier { get => 1f - (FOVPercent / 100f); set => FOVPercent = (1f - value) * 100f; }

        public static CKTriggerSettings CreateDefault(bool enabled, int priority = 50)
        {
            return new CKTriggerSettings
            {
                Enabled = enabled,
                Override = false,
                Priority = priority,
                OverrideChance = false,
                Chance = 100f,
                Duration = 1.5f,
                TimeScale = 0.15f,
                Cooldown = 2f,
                FirstPersonCamera = true,
                ProjectileCamera = true,
                FirstPersonChance = 50f,
                FOVMode = FOVMode.ZoomIn,
                FOVPercent = 20f,
                ProjectileFOVMode = FOVMode.ZoomIn,
                ProjectileFOVPercent = 15f
            };
        }

        public static CKTriggerSettings CreateLastEnemyDefault()
        {
            return new CKTriggerSettings
            {
                Enabled = true,
                Override = true, // Last enemy has special defaults
                Priority = 100, // Highest priority
                OverrideChance = false,
                Chance = 100f,
                Duration = 2.0f,
                TimeScale = 0.1f,
                Cooldown = 0f,
                FirstPersonCamera = true,
                ProjectileCamera = true,
                FirstPersonChance = 50f,
                FOVMode = FOVMode.ZoomIn,
                FOVPercent = 30f,
                ProjectileFOVMode = FOVMode.ZoomIn,
                ProjectileFOVPercent = 20f
            };
        }
    }

    /// <summary>
    /// Standard camera preset - readonly predefined angles
    /// </summary>
    public readonly struct StandardCameraPreset
    {
        public readonly string Name;
        public readonly float Distance;
        public readonly float Height;
        public readonly float XOffset;
        public readonly float Pitch;
        public readonly float Yaw;
        public readonly float Tilt;

        public StandardCameraPreset(string name, float distance, float height, float xOffset, float pitch, float yaw, float tilt)
        {
            Name = name;
            Distance = distance;
            Height = height;
            XOffset = xOffset;
            Pitch = pitch;
            Yaw = yaw;
            Tilt = tilt;
        }

        /// <summary>
        /// All standard camera presets
        /// </summary>
        public static readonly StandardCameraPreset[] All = new[]
        {
            // Format: Name, Distance, Height, XOffset, Pitch, Yaw, Tilt
            // Naming: Numbered 1-7, D = Distance from target, H = Height above target
            // XOffset = 0 (randomized at runtime based on Side Offset Level)
            // Tilt = 0 (randomized at runtime if enabled)
            new StandardCameraPreset("1. Standard", 1.8f, 1.0f, 0f, -30f, 0f, 0f),
            new StandardCameraPreset("2. Close-High", 0.5f, 3.5f, 0f, -15f, 0f, 0f),
            new StandardCameraPreset("3. Medium-Ground", 2.0f, 0.0f, 0f, -25f, 0f, 0f),
            new StandardCameraPreset("4. Far-High", 4.0f, 4.0f, 0f, -10f, 0f, 0f),
            new StandardCameraPreset("5. Tight-Elevated", 1.0f, 2.65f, 0f, -30f, 0f, 0f),
            new StandardCameraPreset("6. Close-Ground", 1.2f, 0.0f, 0f, -35f, 0f, 0f),
            new StandardCameraPreset("7. Close-Mid", 1.2f, 1.2f, 0f, -40f, 0f, 0f),
        };
    }

    /// <summary>
    /// Custom camera preset created by user
    /// </summary>
    [Serializable]
    public class CameraPreset
    {
        public string Name = "Untitled";
        // Position
        public float Distance = 2.0f;
        public float Height = 0.5f;
        public float XOffset = 0f;
        // Rotation
        public float Pitch = 0f;
        public float Yaw = 0f;
        public float Tilt = 0f;
    }

    /// <summary>
    /// Side offset randomization level
    /// </summary>
    public enum SideOffsetLevel
    {
        Wide = 0,    // -4m to 4m
        Medium = 1,  // -2m to 2m
        Tight = 2    // -1m to 1m
    }

    /// <summary>
    /// Projectile camera settings - now with direct dimension control
    /// </summary>
    [Serializable]
    public class CKProjectileCameraSettings
    {
        // Position dimensions
        public float Distance = 2.0f;       // Distance from target
        public float Height = 0.5f;         // Vertical offset
        public float XOffset = 0f;          // Horizontal offset
        
        // Rotation dimensions
        public float Pitch = 0f;            // Up/down angle (-90 to 90)
        public float Yaw = 0f;              // Left/right angle (-180 to 180)
        public float Tilt = 0f;             // Roll/Dutch angle (-45 to 45)
        
        // FOV settings for projectile camera
        public FOVMode FOVMode = FOVMode.ZoomIn;  // Off, ZoomIn, or ZoomOut
        public float FOVPercent = 15f;       // 0-50%, percentage to zoom in/out
        
        // FOV timing for projectile camera
        public float FOVZoomInDuration = 0.15f;   // Seconds to zoom in
        public float FOVHoldDuration = 0.5f;      // Seconds to hold zoomed
        public float FOVZoomOutDuration = 0.2f;   // Seconds to zoom out
        
        // Randomization for camera positions
        public bool RandomizeDistance = false;
        public float DistanceMin = 1.5f;
        public float DistanceMax = 3.0f;
        
        public bool RandomizeHeight = false;
        public float HeightMin = 0f;
        public float HeightMax = 1.5f;
        
        public bool RandomizeXOffset = false;
        public float XOffsetMin = -1f;
        public float XOffsetMax = 1f;
        
        public bool RandomizePitch = false;
        public float PitchMin = -15f;
        public float PitchMax = 15f;
        
        public bool RandomizeYaw = false;
        public float YawMin = -30f;
        public float YawMax = 30f;
        
        // Standard preset mode toggle (true = use presets, false = use custom dimensions)
        public bool UseStandardPresets = true;
        
        // Multi-select: which presets are enabled for BasicKill cinematics
        public bool[] EnabledPresetsBasicKill = new bool[] { true, true, true, true, true, true, true };
        
        // Multi-select: which presets are enabled for Special Trigger cinematics
        public bool[] EnabledPresetsTriggers = new bool[] { true, true, true, true, true, true, true };
        
        // Legacy: Single EnabledPresets for backward compat - maps to BasicKill
        public bool[] EnabledPresets 
        { 
            get => EnabledPresetsBasicKill;
            set => EnabledPresetsBasicKill = value;
        }
        
        // Legacy: Single selected preset for backward compat (-1 = none)
        public int SelectedStandardPreset = -1;
        
        // Randomize tilt for variety
        public bool RandomizeTilt = false;
        public float RandomTiltRange = 10f; // ±degrees
        
        // Side offset randomization - multi-select toggles
        public bool RandomizeSideOffset = false;
        public bool SideOffsetWide = false;      // -4 to -2 | 2 to 4
        public bool SideOffsetStandard = true;   // -2 to 0 | 0 to 2
        public bool SideOffsetTight = false;     // -1 to 0 | 0 to 1
        
        // Legacy compatibility - maps to new boolean fields
        [Obsolete("Use SideOffsetWide/SideOffsetStandard/SideOffsetTight instead")]
        public SideOffsetLevel SideOffsetLevel 
        { 
            get => SideOffsetWide ? SideOffsetLevel.Wide : (SideOffsetTight ? SideOffsetLevel.Tight : SideOffsetLevel.Medium);
            set 
            {
                SideOffsetWide = value == SideOffsetLevel.Wide;
                SideOffsetStandard = value == SideOffsetLevel.Medium;
                SideOffsetTight = value == SideOffsetLevel.Tight;
            }
        }
        
        /// <summary>
        /// Gets a randomly selected side offset level from enabled levels.
        /// Returns null if no levels are enabled.
        /// </summary>
        public SideOffsetLevel? GetRandomEnabledSideOffsetLevel()
        {
            var enabled = new System.Collections.Generic.List<SideOffsetLevel>();
            if (SideOffsetWide) enabled.Add(SideOffsetLevel.Wide);
            if (SideOffsetStandard) enabled.Add(SideOffsetLevel.Medium);
            if (SideOffsetTight) enabled.Add(SideOffsetLevel.Tight);
            
            if (enabled.Count == 0) return null;
            return enabled[UnityEngine.Random.Range(0, enabled.Count)];
        }
        
        // ═══════════════════════════════════════════════════════════════
        // NEAR PLAYER FALLBACK PRESET
        // This is a special preset that positions the camera near the player
        // instead of at the victim position, useful for hitscan weapons
        // ═══════════════════════════════════════════════════════════════
        public bool EnableNearPlayerBasicKill = false;    // Include for Basic Kill cinematics
        public bool EnableNearPlayerTriggers = false;     // Include for Special Trigger cinematics
        public float NearPlayerDistance = 2.5f;           // Distance from player (behind)
        public float NearPlayerHeight = 1.5f;             // Height above ground
        public float NearPlayerXOffset = 1.0f;            // Side offset (randomized ±)
        
        // ═══════════════════════════════════════════════════════════════
        // BEHIND ENEMY PRESET
        // Positions camera behind enemy looking toward player (like X-Ray)
        // ═══════════════════════════════════════════════════════════════
        public bool EnableBehindEnemy = false;            // Enable Behind Enemy preset
        
        // ═══════════════════════════════════════════════════════════════
        // DYNAMIC ZOOM (ADS simulation during projectile cinematic)
        // ═══════════════════════════════════════════════════════════════
        public bool EnableDynamicZoomIn = false;          // Zoom in (ADS on) during cinematic
        public bool EnableDynamicZoomOut = false;         // Zoom out (ADS off) during cinematic
        public float DynamicZoomBalance = 50f;            // % of duration for zoom in (10-90), rest is zoom out
        
        // Custom presets created by user
        public System.Collections.Generic.List<CameraPreset> CustomPresets = new System.Collections.Generic.List<CameraPreset>();
        
        // Currently selected custom preset (-1 = manual/custom values)
        public int SelectedPresetIndex = -1;
        
        /// <summary>
        /// Apply a standard preset's values to the current settings
        /// </summary>
        public void ApplyStandardPreset(int index)
        {
            if (index < 0 || index >= StandardCameraPreset.All.Length) return;
            var preset = StandardCameraPreset.All[index];
            Distance = preset.Distance;
            Height = preset.Height;
            XOffset = preset.XOffset;
            Pitch = preset.Pitch;
            Yaw = preset.Yaw;
            Tilt = preset.Tilt;
            SelectedStandardPreset = index;
        }
        
        /// <summary>
        /// Apply a preset's values to the current settings
        /// </summary>
        public void ApplyPreset(CameraPreset preset)
        {
            if (preset == null) return;
            Distance = preset.Distance;
            Height = preset.Height;
            XOffset = preset.XOffset;
            Pitch = preset.Pitch;
            Yaw = preset.Yaw;
            Tilt = preset.Tilt;
            SelectedStandardPreset = -1; // Clear standard preset selection
        }
        
        /// <summary>
        /// Create a new preset from current values
        /// </summary>
        public CameraPreset CreatePresetFromCurrent(string name)
        {
            return new CameraPreset
            {
                Name = name,
                Distance = this.Distance,
                Height = this.Height,
                XOffset = this.XOffset,
                Pitch = this.Pitch,
                Yaw = this.Yaw,
                Tilt = this.Tilt
            };
        }
        
        /// <summary>
        /// Get a random preset index from enabled presets for BasicKill
        /// </summary>
        public int GetRandomEnabledPreset() => GetRandomEnabledPreset(false);
        
        /// <summary>
        /// Get a random preset index from enabled presets
        /// Returns -1 if Near Player preset is selected (special handling required by caller)
        /// </summary>
        /// <param name="isSpecialTrigger">If true, use EnabledPresetsTriggers; otherwise use EnabledPresetsBasicKill</param>
        public int GetRandomEnabledPreset(bool isSpecialTrigger)
        {
            var presets = isSpecialTrigger ? EnabledPresetsTriggers : EnabledPresetsBasicKill;
            var enabled = new System.Collections.Generic.List<int>();
            
            // Add standard presets
            for (int i = 0; i < presets.Length && i < StandardCameraPreset.All.Length; i++)
            {
                if (presets[i])
                    enabled.Add(i);
            }
            
            // Add Near Player as a special entry (represented by -1)
            bool nearPlayerEnabled = isSpecialTrigger 
                ? EnableNearPlayerTriggers 
                : EnableNearPlayerBasicKill;
            if (nearPlayerEnabled)
                enabled.Add(-1); // -1 = Near Player preset
            
            if (enabled.Count == 0)
                return 0; // Fallback to first standard preset
            
            return enabled[UnityEngine.Random.Range(0, enabled.Count)];
        }
        
        /// <summary>
        /// Check if any presets are enabled for BasicKill
        /// </summary>
        public bool HasEnabledPresets() => HasEnabledPresets(false);
        
        /// <summary>
        /// Check if any presets are enabled
        /// </summary>
        public bool HasEnabledPresets(bool isSpecialTrigger)
        {
            var presets = isSpecialTrigger ? EnabledPresetsTriggers : EnabledPresetsBasicKill;
            for (int i = 0; i < presets.Length; i++)
            {
                if (presets[i]) return true;
            }
            return false;
        }
        
        /// <summary>
        /// Enable at least one preset (first one) if none enabled
        /// </summary>
        public void EnsureAtLeastOnePreset()
        {
            if (!HasEnabledPresets(false) && EnabledPresetsBasicKill.Length > 0)
            {
                EnabledPresetsBasicKill[0] = true;
            }
            if (!HasEnabledPresets(true) && EnabledPresetsTriggers.Length > 0)
            {
                EnabledPresetsTriggers[0] = true;
            }
        }
    }

    /// <summary>
    /// Screen effects settings
    /// </summary>
    [Serializable]
    public class CKScreenEffects
    {
        public bool Enabled = true;
        
        // ═══════════════════════════════════════════════════════════════════════
        // KILL FLASH - Bright flash effect on kills
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableKillFlash = true;               // Master toggle - enabled by default
        public float KillFlashChance = 100f;              // Chance (0-100%)
        public float KillFlashDuration = 0.3f;            // Duration (0.1-1.0s)
        public float KillFlashIntensity = 1.0f;           // Intensity (0.5-2.0)
        public bool KillFlash_FP = true;                  // Trigger on First Person camera
        public bool KillFlash_Projectile = true;          // Trigger on Projectile camera
        public bool KillFlash_Freeze = true;              // Trigger during Freeze Frame
        
        // ═══════════════════════════════════════════════════════════════════════
        // BLOOD SPLATTER - Screen blood overlay on kills
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableBloodSplatter = false;          // Master toggle
        public float BloodSplatterChance = 100f;          // Chance (0-100%)
        public float BloodSplatterDuration = 0.5f;        // Duration (0.1-1.0s)
        public float BloodSplatterIntensity = 1.5f;       // Intensity (0.5-3.0)
        public bool BloodSplatter_FP = true;              // Trigger on First Person camera
        public bool BloodSplatter_Projectile = true;      // Trigger on Projectile camera
        public bool BloodSplatter_Freeze = true;          // Trigger during Freeze Frame
        
        // ═══════════════════════════════════════════════════════════════════════
        // VIGNETTE - Dark edge vignette effect during cinematics
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableVignette = false;               // Master toggle
        public float VignetteChance = 100f;               // Chance (0-100%)
        public float VignetteDuration = 0.5f;             // Duration (0.1-1.0s)
        public float VignetteIntensity = 0.45f;           // Intensity (0.1-1.0)
        public bool Vignette_FP = true;                   // Trigger on First Person camera
        public bool Vignette_Projectile = true;           // Trigger on Projectile camera
        public bool Vignette_Freeze = true;               // Trigger during Freeze Frame
        
        // ═══════════════════════════════════════════════════════════════════════
        // DESATURATION - Color desaturation effect during cinematics
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableDesaturation = false;           // Master toggle
        public float DesaturationChance = 100f;           // Chance (0-100%)
        public float DesaturationDuration = 0.5f;         // Duration (0.1-1.0s)
        public float DesaturationAmount = 0.3f;           // Amount (0.1-1.0)
        public bool Desaturation_FP = true;               // Trigger on First Person camera
        public bool Desaturation_Projectile = true;       // Trigger on Projectile camera
        public bool Desaturation_Freeze = true;           // Trigger during Freeze Frame
        
        // LEGACY PROPERTIES - Kept for backward config compatibility
        // These are read from config but no longer control behavior
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableXRayVision = false;              // Legacy - replaced by Desaturation
        public bool EnableXRayVision_BasicKill = true;     // Legacy
        public bool EnableXRayVision_SpecialTrigger = true;// Legacy
        public float XRayVisionIntensity = 1.0f;           // Legacy
        public bool EnableKillFlash_BasicKill = true;      // Legacy - replaced by FP/Proj/Freeze
        public bool EnableKillFlash_SpecialTrigger = true; // Legacy
        public bool EnableBloodSplatter_BasicKill = true;  // Legacy
        public bool EnableBloodSplatter_SpecialTrigger = true; // Legacy
        public bool EnableHitstop = true;                  // Legacy
        public float HitstopDuration = 0.15f;              // Legacy
        
        // Legacy randomization fields (no longer in UI)
        public bool RandomizeVignetteIntensity = false;
        public float VignetteIntensityMin = 0f;
        public float VignetteIntensityMax = 1f;
        public bool RandomizeDesaturationAmount = false;
        public float DesaturationAmountMin = 0f;
        public float DesaturationAmountMax = 1f;
        public bool RandomizeBloodSplatterIntensity = false;
        public float BloodSplatterIntensityMin = 0.5f;
        public float BloodSplatterIntensityMax = 2f;
        public bool RandomizeHitstopDuration = false;
        public float HitstopDurationMin = 0.01f;
        public float HitstopDurationMax = 0.3f;
        
        // Legacy Kill Vignette (replaced by main Vignette)
        public bool EnableKillVignette = false;
        public float KillVignetteDuration = 1.0f;
        public float KillVignetteIntensity = 1.5f;
    }
    
    // ═══════════════════════════════════════════════════════════════════════════
    // LEGACY COMPATIBILITY LAYER
    // These classes provide backward compatibility with CinematicKillManager
    // ═══════════════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Legacy MenuV2 settings adapter - provides the old interface backed by new simplified settings
    /// </summary>
    [Serializable]
    public class CKMenuV2Settings
    {
        public CKCoreSettings Core;
        public CKModeSettings Melee;
        public CKModeSettings Ranged;
        public CKModeSettings Bow;
        public CKModeSettings Dismember;
        public CKModeSettings Thrown;
        public CKModeSettings Trap;
        public CKTriggerSystemSettings TriggerSystem;
        public CKGlobalVisualSettings GlobalVisuals;
        public CKMenuFirstPersonCameraSettings FirstPersonCamera;
        public CKMenuProjectileCameraSettings ProjectileCamera;
        public CKMenuProjectilePathSettings ProjectilePath;
        public CKHitstopSettings Hitstop;
        public CKKillstreakSettings Killstreak;
        public CKFirstPersonTimingSettings FirstPersonTiming;
        public CKFirstPersonVisualSettings FirstPersonVisuals;
        public CKProjectileVisualSettings ProjectileVisuals;
        public CKHUDElementSettings HUDElements;
        public CKToastSettings Toast;
        public CKContextRuleSettings ContextRules;
        public CKProfileSettings Profiles;
        
        public static CKMenuV2Settings CreateFromSettings(CinematicKillSettings owner)
        {
            var m = new CKMenuV2Settings();
            m.Core = CKCoreSettings.CreateDefault();
            m.Core.Enabled = owner.EnableCinematics;
            m.Core.GlobalTimeScale = owner.TriggerDefaults.TimeScale;
            m.Core.SlowMoDuration = owner.TriggerDefaults.Duration;
            m.Core.EnableFirstPersonCamera = owner.TriggerDefaults.FirstPersonCamera;
            m.Core.EnableProjectileCamera = owner.TriggerDefaults.ProjectileCamera;
            m.Core.FirstPersonCameraChance = owner.TriggerDefaults.FirstPersonChance;
            m.Core.ProjectileCameraChance = 100f - owner.TriggerDefaults.FirstPersonChance;
            m.Core.GlobalCooldown = owner.TriggerDefaults.Cooldown;
            m.Core.EnemyScanRadius = owner.EnemyScanRadius;
            
            m.Melee = CKModeSettings.CreateDefault(50f, 1f, 1f);
            m.Melee.Enabled = owner.MeleeEnabled;
            m.Ranged = CKModeSettings.CreateDefault(50f, 1f, 1f);
            m.Ranged.Enabled = owner.RangedEnabled;
            m.Bow = CKModeSettings.CreateDefault(50f, 1f, 1f);
            m.Bow.Enabled = owner.BowEnabled;
            m.Dismember = CKModeSettings.CreateDefault(50f, 1f, 1f);
            m.Dismember.Enabled = owner.ExplosiveEnabled;
            m.Thrown = CKModeSettings.CreateDefault(50f, 1f, 1f);
            m.Trap = CKModeSettings.CreateDefault(50f, 1f, 1f);
            m.Trap.Enabled = owner.TrapEnabled;
            
            m.TriggerSystem = CKTriggerSystemSettings.CreateFromOwner(owner);
            m.GlobalVisuals = CKGlobalVisualSettings.CreateDefault();
            m.GlobalVisuals.EnableScreenEffects = owner.ScreenEffects.Enabled;
            m.GlobalVisuals.EnableVignette = owner.ScreenEffects.EnableVignette;
            m.GlobalVisuals.VignetteIntensity = owner.ScreenEffects.VignetteIntensity;
            m.GlobalVisuals.EnableDesaturation = owner.ScreenEffects.EnableDesaturation;
            m.GlobalVisuals.DesaturationAmount = owner.ScreenEffects.DesaturationAmount;
            m.GlobalVisuals.EnableBloodSplatter = owner.ScreenEffects.EnableBloodSplatter;
            m.GlobalVisuals.BloodSplatterIntensity = owner.ScreenEffects.BloodSplatterIntensity;
            m.GlobalVisuals.EnableFOVEffect = owner.TriggerDefaults.FOVEnabled;
            m.GlobalVisuals.FOVMultiplier = owner.TriggerDefaults.FOVMultiplier;
            
            m.FirstPersonCamera = CKMenuFirstPersonCameraSettings.CreateDefault();
            m.ProjectileCamera = CKMenuProjectileCameraSettings.CreateDefault();
            m.ProjectileCamera.FollowDistance = owner.ProjectileCamera.Distance;
            m.ProjectileCamera.FollowHeight = owner.ProjectileCamera.Height;
            m.ProjectileCamera.CameraTilt = owner.ProjectileCamera.Tilt;
            
            m.ProjectilePath = CKMenuProjectilePathSettings.CreateDefault();
            m.ProjectilePath.HeightOffset = owner.ProjectileCamera.Height;
            m.ProjectilePath.DistanceOffset = owner.ProjectileCamera.Distance;
            
            m.Hitstop = CKHitstopSettings.CreateDefault();
            m.Hitstop.Enabled = owner.ScreenEffects.EnableHitstop;
            m.Hitstop.Duration = owner.ScreenEffects.HitstopDuration;
            
            m.Killstreak = CKKillstreakSettings.CreateDefault();
            m.Killstreak.Enabled = owner.Killstreak.Enabled;
            m.Killstreak.StreakTimeout = owner.KillstreakWindow;
            
            m.FirstPersonTiming = CKFirstPersonTimingSettings.CreateDefault();
            m.FirstPersonVisuals = CKFirstPersonVisualSettings.CreateDefault();
            m.ProjectileVisuals = CKProjectileVisualSettings.CreateDefault();
            m.HUDElements = owner.HUDElements ?? CKHUDElementSettings.CreateDefault();
            m.Toast = owner.Toast ?? CKToastSettings.CreateDefault();
            m.ContextRules = CKContextRuleSettings.CreateDefault();
            m.ContextRules.LongRangeMinDistance = owner.LongRangeDistance;
            m.ContextRules.LowHealthThresholdPercent = owner.LowHealthPercent;
            m.Profiles = CKProfileSettings.CreateDefault();
            
            return m;
        }
        
        public static CKMenuV2Settings CreateDefault()
        {
            return CreateFromSettings(CinematicKillSettings.Default);
        }
    }
    
    [Serializable]
    public class CKCoreSettings
    {
        public bool Enabled = true;
        public float GlobalTriggerChance = 100f;
        public float GlobalCooldown = 2.5f;
        public float SlowMoDuration = 1.2f;
        public float GlobalTimeScale = 0.2f;
        public bool EnableFirstPersonCamera = true;
        public bool EnableProjectileCamera = true;
        public float FirstPersonCameraChance = 60f;
        public float ProjectileCameraChance = 40f;
        public bool FinalEnemyAlwaysTriggers = false;
        public float EnemyScanRadius = 15f;
        public float LastEnemySlowMoDuration = 2f;
        public float LastEnemySlowMoScale = 0.15f;
        public bool AllowLastEnemyFirstPerson = true;
        public bool AllowLastEnemyProjectile = true;
        public bool IgnoreCorpseHits = true;
        public bool EnableHUD = true;
        public float HUDOpacity = 0.85f;
        public float HUDMessageDuration = 3f;
        public bool RandomizeDuration = false;
        public float DurationMin = 0.8f;
        public float DurationMax = 1.5f;
        public bool RandomizeTimeScale = false;
        public float TimeScaleMin = 0.15f;
        public float TimeScaleMax = 0.3f;
        
        public static CKCoreSettings CreateDefault() => new CKCoreSettings();
    }
    
    [Serializable]
    public class CKModeSettings
    {
        public bool Enabled = true;
        public float TriggerChancePercent = 50f;
        public float ZoomMultiplier = 1f;
        public float ZoomSpeed = 1f;
        public bool UseFirstPersonCamera = true;
        public bool UseProjectileCamera = true;
        public float FirstPersonCameraChance = 60f;
        public float ProjectileCameraChance = 40f;
        public bool OverrideGlobalSlowMo = false;
        public float OverrideDurationSeconds = 1.2f;
        public bool OverrideTimeScale = false;
        public float OverrideTimeScaleValue = 0.2f;
        public bool OverrideCamera = false;
        public bool OverrideTriggerChance = false;
        public bool OverrideCameraSelection = false;
        public bool OverrideZoom = false;
        public bool RandomizeTriggerChance = false;
        public float MinTriggerChancePercent = 25f;
        public float MaxTriggerChancePercent = 75f;
        public bool RandomizeZoomMultiplier = false;
        public float MinZoomMultiplier = 0.8f;
        public float MaxZoomMultiplier = 1.2f;
        public bool RandomizeZoomSpeed = false;
        public float MinZoomSpeed = 0.8f;
        public float MaxZoomSpeed = 1.2f;
        
        public static CKModeSettings CreateDefault(float chance, float zoom, float speed)
        {
            return new CKModeSettings
            {
                TriggerChancePercent = chance,
                ZoomMultiplier = zoom,
                ZoomSpeed = speed
            };
        }
    }
    
    [Serializable]
    public class CKTriggerSystemSettings
    {
        public bool EnableTriggers = true;
        public float MasterTriggerChance = 100f;  // Master chance for ALL special triggers (0-100%)
        public bool RequireTriggerForCinematic = false;
        // Bonus stacking is deprecated - first matching trigger wins
        public bool StackTriggerBonuses = false;
        public float TriggerBonusDuration = 0f;
        public float TriggerSlowReduction = 0f;
        public float DistanceThreshold = 25f;
        
        public CKContextTriggerSettings BasicKill;
        public CKContextTriggerSettings Critical;
        public CKContextTriggerSettings LastEnemy;
        public CKContextTriggerSettings Headshot;
        public CKContextTriggerSettings LongRangeKill;
        public CKContextTriggerSettings LowHealthKill;
        public CKContextTriggerSettings DismemberKill;
        public CKContextTriggerSettings Killstreak;
        public CKContextTriggerSettings SneakKill;
        
        // Legacy fields
        public bool EnableHeadshotTrigger = true;
        public bool EnableCriticalTrigger = true;
        public bool EnableKillTrigger = false;
        public bool EnableLastEnemyTrigger = true;
        public bool EnableMeleeTrigger = true;
        public bool EnableExplosionTrigger = true;
        public bool EnableLongRangeTrigger = true;
        public bool EnableKillstreakTrigger = true;
        public bool EnableMeleeMode = true;
        public bool EnableRangedMode = true;
        public bool EnableExplosionMode = true;
        public bool ShowDebugInfo = false;
        public string LastTrigger = "None";
        public string CurrentCooldowns = "-";
        public CKTriggerContextSettings Melee;
        public CKTriggerContextSettings Ranged;
        public CKTriggerContextSettings Dismember;
        public CKTriggerContextSettings LongRange;
        public CKTriggerContextSettings LowHealth;
        
        public static CKTriggerSystemSettings CreateFromOwner(CinematicKillSettings owner)
        {
            var ts = new CKTriggerSystemSettings();
            ts.EnableTriggers = owner.TriggerDefaults.EnableTriggers;
            ts.MasterTriggerChance = owner.MasterTriggerChance;
            ts.DistanceThreshold = owner.LongRangeDistance;
            
            ts.BasicKill = CKContextTriggerSettings.FromSimple(owner.BasicKill);
            ts.Critical = CKContextTriggerSettings.FromTrigger(owner.Critical, owner.TriggerDefaults);
            ts.LastEnemy = CKContextTriggerSettings.FromTrigger(owner.LastEnemy, owner.TriggerDefaults);
            ts.Headshot = CKContextTriggerSettings.FromTrigger(owner.Headshot, owner.TriggerDefaults);
            ts.LongRangeKill = CKContextTriggerSettings.FromTrigger(owner.LongRange, owner.TriggerDefaults);
            ts.LowHealthKill = CKContextTriggerSettings.FromTrigger(owner.LowHealth, owner.TriggerDefaults);
            ts.DismemberKill = CKContextTriggerSettings.FromTrigger(owner.Dismember, owner.TriggerDefaults);
            ts.Killstreak = CKContextTriggerSettings.FromTrigger(owner.Killstreak, owner.TriggerDefaults);
            ts.SneakKill = CKContextTriggerSettings.FromTrigger(owner.Sneak, owner.TriggerDefaults);
            
            ts.Melee = CKTriggerContextSettings.CreateDefault(50f, 3f);
            ts.Ranged = CKTriggerContextSettings.CreateDefault(50f, 3f);
            ts.Dismember = CKTriggerContextSettings.CreateDefault(50f, 3f);
            ts.LongRange = CKTriggerContextSettings.CreateDefault(50f, 5f);
            ts.LowHealth = CKTriggerContextSettings.CreateDefault(50f, 5f);
            
            return ts;
        }
        
        public static CKTriggerSystemSettings CreateDefault()
        {
            return CreateFromOwner(CinematicKillSettings.Default);
        }
    }
    
    [Serializable]
    public class CKContextTriggerSettings
    {
        public bool Enabled = true;
        public float ChancePercent = 100f;
        public float CooldownSeconds = 2f;
        public bool AllowFirstPerson = true;
        public bool AllowProjectile = true;
        public float FirstPersonChance = 50f;
        public float ProjectileChance = 50f;
        public int Priority = 50;
        
        public bool OverrideDuration = false;
        public float DurationSeconds = 1.5f;
        public bool RandomizeDuration = false;
        public float DurationMin = 1f;
        public float DurationMax = 2f;
        
        public bool OverrideSlowScale = false;
        public float SlowScale = 0.15f;
        public bool RandomizeSlowScale = false;
        public float SlowScaleMin = 0.1f;
        public float SlowScaleMax = 0.2f;
        
        public bool OverrideFOV = false;
        public float FOVMultiplier = 0.8f;
        public bool RandomizeFOV = false;
        public float FOVMultiplierMin = 0.7f;
        public float FOVMultiplierMax = 0.9f;
        public float ZoomSpeed = 1f;
        
        public bool OverrideHitstop = false;
        public float HitstopDuration = 0.15f;
        public bool HitstopCritOnly = false;
        
        public bool OverrideScreenEffects = false;
        public bool EnableVignette = true;
        public float VignetteIntensity = 0.45f;
        public bool EnableDesaturation = true;
        public float DesaturationAmount = 0.3f;
        public bool EnableBloodSplatter = true;
        public bool EnableRadialBlur = false;
        public float RadialBlurIntensity = 0.3f;
        
        public bool OverrideChance = false;
        public bool OverrideCooldown = false;
        public bool OverrideCamera = false;
        
        public CKRandomizationOverrideSettings RandomizationOverrides;
        
        public static CKContextTriggerSettings FromSimple(CKBasicKillSettings basic)
        {
            return new CKContextTriggerSettings
            {
                Enabled = basic.Enabled,
                ChancePercent = basic.Chance,
                CooldownSeconds = basic.Cooldown,
                AllowFirstPerson = basic.FirstPersonCamera,
                AllowProjectile = basic.ProjectileCamera,
                FirstPersonChance = basic.FirstPersonChance,
                ProjectileChance = 100f - basic.FirstPersonChance,
                OverrideSlowScale = true,
                SlowScale = basic.TimeScale,
                OverrideDuration = true,
                DurationSeconds = basic.Duration,
                OverrideFOV = basic.FOVEnabled,
                FOVMultiplier = basic.FOVMultiplier,
                Priority = 10,
                RandomizationOverrides = CKRandomizationOverrideSettings.CreateDefault()
            };
        }
        
        public static CKContextTriggerSettings FromTrigger(CKTriggerSettings trigger, CKTriggerDefaults defaults)
        {
            var cts = new CKContextTriggerSettings
            {
                Enabled = trigger.Enabled,
                // ChancePercent: Use trigger.Chance if overriding, otherwise will use master at runtime
                OverrideChance = trigger.OverrideChance,
                ChancePercent = trigger.OverrideChance ? trigger.Chance : 100f, // 100% is placeholder - master chance used at runtime
                CooldownSeconds = trigger.Override ? trigger.Cooldown : defaults.Cooldown,
                AllowFirstPerson = trigger.Override ? trigger.FirstPersonCamera : defaults.FirstPersonCamera,
                AllowProjectile = trigger.Override ? trigger.ProjectileCamera : defaults.ProjectileCamera,
                FirstPersonChance = trigger.Override ? trigger.FirstPersonChance : defaults.FirstPersonChance,
                ProjectileChance = trigger.Override ? (100f - trigger.FirstPersonChance) : (100f - defaults.FirstPersonChance),
                OverrideSlowScale = trigger.Override,
                SlowScale = trigger.Override ? trigger.TimeScale : defaults.TimeScale,
                OverrideDuration = trigger.Override,
                DurationSeconds = trigger.Override ? trigger.Duration : defaults.Duration,
                OverrideFOV = trigger.Override && trigger.FOVEnabled,
                FOVMultiplier = trigger.Override ? trigger.FOVMultiplier : defaults.FOVMultiplier,
                OverrideCooldown = trigger.Override,
                Priority = trigger.Priority,
                RandomizationOverrides = CKRandomizationOverrideSettings.CreateDefault()
            };
            return cts;
        }
        
        public static CKContextTriggerSettings CreateDefault(
            bool enabled = true, float chance = 50f, float cooldown = 3f,
            bool allowFp = true, bool allowProj = true, int priority = 50)
        {
            return new CKContextTriggerSettings
            {
                Enabled = enabled,
                ChancePercent = chance,
                CooldownSeconds = cooldown,
                AllowFirstPerson = allowFp,
                AllowProjectile = allowProj,
                Priority = priority,
                RandomizationOverrides = CKRandomizationOverrideSettings.CreateDefault()
            };
        }
        
        public static CKContextTriggerSettings CreateLastEnemyDefault()
        {
            var cts = CreateDefault(true, 100f, 0f, true, true, 100);
            cts.OverrideSlowScale = true;
            cts.SlowScale = 0.1f;
            cts.OverrideDuration = true;
            cts.DurationSeconds = 2f;
            return cts;
        }
    }
    
    [Serializable]
    public class CKTriggerContextSettings
    {
        public float ChancePercent = 50f;
        public float CooldownSeconds = 3f;
        public bool AllowFirstPerson = true;
        public bool AllowProjectile = true;
        
        public static CKTriggerContextSettings CreateDefault(float chance, float cooldown)
        {
            return new CKTriggerContextSettings
            {
                ChancePercent = chance,
                CooldownSeconds = cooldown
            };
        }
    }
    
    [Serializable]
    public class CKRandomizationOverrideSettings
    {
        public bool Enabled = false;
        public bool RandomizeTriggerChance = false;
        public float TriggerChanceMin = 30f;
        public float TriggerChanceMax = 80f;
        public bool RandomizeCooldown = false;
        public float CooldownMin = 1f;
        public float CooldownMax = 5f;
        public bool RandomizeCameraSelection = false;
        public bool AllowFirstPerson = true;
        public bool AllowProjectile = true;
        public float FirstPersonChanceMin = 40f;
        public float FirstPersonChanceMax = 70f;
        public bool RandomizeDuration = false;
        public float DurationMin = 0.8f;
        public float DurationMax = 2f;
        public bool RandomizeTimeScale = false;
        public float TimeScaleMin = 0.1f;
        public float TimeScaleMax = 0.3f;
        public bool RandomizeFOV = false;
        public float FOVMultiplierMin = 0.75f;
        public float FOVMultiplierMax = 0.95f;
        public bool RandomizeZoomSpeed = false;
        public float ZoomSpeedMin = 0.5f;
        public float ZoomSpeedMax = 2f;
        public bool RandomizeHitstop = false;
        public float HitstopDurationMin = 0.05f;
        public float HitstopDurationMax = 0.2f;
        public bool HitstopCritOnly = false;
        public bool RandomizeScreenEffects = false;
        public bool EnableVignette = true;
        public bool RandomizeVignetteIntensity = false;
        public float VignetteIntensityMin = 0.3f;
        public float VignetteIntensityMax = 0.6f;
        public bool EnableDesaturation = true;
        public bool RandomizeDesaturationAmount = false;
        public float DesaturationAmountMin = 0.2f;
        public float DesaturationAmountMax = 0.5f;
        public bool EnableBloodSplatter = true;
        public bool EnableRadialBlur = false;
        public bool RandomizeRadialBlurIntensity = false;
        public float RadialBlurIntensityMin = 0.2f;
        public float RadialBlurIntensityMax = 0.5f;
        public int Priority = 50;
        
        public static CKRandomizationOverrideSettings CreateDefault() => new CKRandomizationOverrideSettings();
    }
    
    [Serializable]
    public class CKGlobalVisualSettings
    {
        public bool EnableScreenEffects = true;
        public bool EnableFOVEffect = true;
        public bool EnableFOVZoom = true; // Legacy alias for EnableFOVEffect
        public float FOVMultiplier = 0.85f;
        public float FOVZoomMultiplier = 0.85f; // Legacy alias for FOVMultiplier
        public float FOVInDuration = 0.3f;
        public float FOVHoldDuration = 0.5f;
        public float FOVOutDuration = 0.4f;
        public bool EnableVignette = true;
        public float VignetteIntensity = 0.45f;
        public bool EnableDesaturation = true;
        public float DesaturationAmount = 0.3f;
        public bool EnableBloodSplatter = true;
        public int BloodSplatterDirection = 4;
        public float BloodSplatterIntensity = 1.5f;
        public float BloodSplatterMaxDistance = 0.3f;
        public bool EnableConcussion = false;
        public float ConcussionIntensity = 0.5f;
        public float ConcussionDuration = 0.3f;
        public bool ConcussionAudioMuffle = false;
        public bool EnableMotionBlur = true;
        public float MotionBlurIntensity = 0.5f;
        public bool EnableChromaticAberration = true;
        public float ChromaticAberrationIntensity = 0.3f;
        public bool EnableDepthOfField = false;
        public float DepthOfFieldFocusDistance = 5f;
        public float DepthOfFieldAperture = 5.6f;
        public float DepthOfFieldFocalLength = 50f;
        public bool EnableRadialBlur = false;
        public float RadialBlurIntensity = 0.3f;
        public float RadialBlurDuration = 0.15f;
        public bool EnableVisualEffects = true;
        public string Style = "Default";
        public bool SeparateStylesPerMode = false;
        public string FirstPersonStyle = "Default";
        public string ProjectileStyle = "Default";
        public bool OverrideColorGradingIntensity = false;
        public float FirstPersonIntensity = 1f;
        public float ProjectileIntensity = 1f;
        public float ZoomMultiplier = 1.2f;
        public float ZoomSpeed = 1f;
        public float ShakeDecay = 2f;
        
        public static CKGlobalVisualSettings CreateDefault() => new CKGlobalVisualSettings();
    }
    
    [Serializable]
    public class CKMenuFirstPersonCameraSettings
    {
        public bool Enabled = true;
        public float Chance = 60f;
        public CKModeTiming MeleeSettings;
        public CKModeTiming RangedSettings;
        public CKModeTiming ExplosionSettings;
        public float ZoomMultiplier = 1.3f;
        public float ZoomSpeed = 2f;
        public float ShakeIntensity = 0.5f;
        public float ShakeDecay = 2f;
        public float MaxZoomChange = 20f;
        public bool AllowFovPastSlowMoDuration = false;
        public float ZoomInTime = 0.35f;
        public float HoldTime = 0.2f;
        public float ZoomOutTime = 0.4f;
        
        public static CKMenuFirstPersonCameraSettings CreateDefault()
        {
            return new CKMenuFirstPersonCameraSettings
            {
                MeleeSettings = CKModeTiming.CreateDefault(1.5f, 0.2f),
                RangedSettings = CKModeTiming.CreateDefault(1.2f, 0.25f),
                ExplosionSettings = CKModeTiming.CreateDefault(2f, 0.15f)
            };
        }
    }
    
    [Serializable]
    public class CKMenuProjectileCameraSettings
    {
        public bool Enabled = true;
        public float Chance = 40f;
        public CKModeTiming RangedSettings;
        public CKModeTiming ExplosionSettings;
        public float FollowDistance = 2f;
        public float FollowHeight = 1.25f;
        public float FollowSmoothing = 8f;
        public float LookAhead = 2f;
        public float CameraTilt = 0f;
        public bool RandomizeTilt = false;
        public float TiltMin = -15f;
        public float TiltMax = 15f;
        public bool RandomizeDuration = false;
        public float DurationMin = 0.8f;
        public float DurationMax = 2f;
        public bool RandomizeTimeScale = false;
        public float TimeScaleMin = 0.1f;
        public float TimeScaleMax = 0.3f;
        public bool RandomizeFOV = false;
        public float FOVMin = 0.75f;
        public float FOVMax = 0.9f;
        public float MaxZoomChange = 25f;
        public bool AllowFovPastDuration = false;
        
        public static CKMenuProjectileCameraSettings CreateDefault()
        {
            return new CKMenuProjectileCameraSettings
            {
                RangedSettings = CKModeTiming.CreateDefault(1.5f, 0.2f),
                ExplosionSettings = CKModeTiming.CreateDefault(2.5f, 0.1f)
            };
        }
    }
    
    [Serializable]
    public class CKMenuProjectilePathSettings
    {
        public bool Enabled = false;
        public float TrailWidth = 0.05f;
        public float TrailDuration = 0.5f;
        public float HeightOffset = 0.5f;
        public float DistanceOffset = 2.5f;
        public float HorizontalOffset = 0f;
        public float LookYaw = 0f;
        public float LookPitch = 0f;
        public bool RandomizeHeight = false;
        public float HeightMin = -0.5f;
        public float HeightMax = 1.5f;
        public bool RandomizeDistance = false;
        public float DistanceMin = 1f;
        public float DistanceMax = 4f;
        public bool RandomizeHorizontal = false;
        public float HorizontalMin = -1f;
        public float HorizontalMax = 1f;
        public bool RandomizeLookYaw = false;
        public float LookYawMin = -15f;
        public float LookYawMax = 15f;
        public bool RandomizeLookPitch = false;
        public float LookPitchMin = -10f;
        public float LookPitchMax = 10f;
        
        public static CKMenuProjectilePathSettings CreateDefault() => new CKMenuProjectilePathSettings();
    }
    
    [Serializable]
    public class CKModeTiming
    {
        public float Duration = 1.2f;
        public float TimeScale = 0.2f;
        
        public static CKModeTiming CreateDefault(float duration, float timeScale)
        {
            return new CKModeTiming { Duration = duration, TimeScale = timeScale };
        }
    }
    
    [Serializable]
    public class CKHitstopSettings
    {
        public bool Enabled = true;
        public float Duration = 0.15f;
        public float TimeScale = 0.01f;
        public bool EnableFirstPersonHitstop = true;
        public float FirstPersonHitstopDuration = 0.15f;
        public bool FirstPersonCritOnly = false;
        public bool EnableProjectileHitstop = true;
        public float ProjectileDuration = 0.12f;
        public bool ProjectileCritOnly = false;
        
        public static CKHitstopSettings CreateDefault() => new CKHitstopSettings();
    }
    
    [Serializable]
    public class CKKillstreakSettings
    {
        public bool Enabled = false;
        public float StreakTimeout = 8f;
        public CKKillstreakTier Tier1;
        public CKKillstreakTier Tier2;
        public CKKillstreakTier Tier3;
        public bool EnableKillstreakBonuses = false;
        public float StreakWindowSeconds = 8f;
        public bool EnableForMelee = true;
        public bool EnableForRanged = true;
        public bool EnableForExplosion = true;
        public CKKillstreakTier Tier4;
        public CKKillstreakTier Tier5;
        
        public static CKKillstreakSettings CreateDefault()
        {
            return new CKKillstreakSettings
            {
                Tier1 = CKKillstreakTier.CreateDefault(3, 0.5f, 1.2f),
                Tier2 = CKKillstreakTier.CreateDefault(5, 0.8f, 1.5f),
                Tier3 = CKKillstreakTier.CreateDefault(8, 1f, 1.8f),
                Tier4 = CKKillstreakTier.CreateDefault(12, 1.3f, 2f),
                Tier5 = CKKillstreakTier.CreateDefault(15, 1.5f, 2.5f)
            };
        }
    }
    
    [Serializable]
    public class CKKillstreakTier
    {
        public int KillsRequired = 3;
        public float BonusDuration = 0.5f;
        public float BonusIntensity = 1.2f;
        public int Kills = 3;
        public float DurationBonusSeconds = 0.5f;
        
        public static CKKillstreakTier CreateDefault(int kills, float bonus, float intensity)
        {
            return new CKKillstreakTier
            {
                KillsRequired = kills,
                BonusDuration = bonus,
                BonusIntensity = intensity,
                Kills = kills,
                DurationBonusSeconds = bonus
            };
        }
    }
    
    [Serializable]
    public class CKFirstPersonTimingSettings
    {
        public float SlowMoScale = 0.2f;
        public float ReturnStartSeconds = 0.6f;
        public bool RandomizeSlowMoScale = false;
        public float SlowMoScaleMin = 0.1f;
        public float SlowMoScaleMax = 0.25f;
        public bool RandomizeDuration = false;
        public float DurationMin = 1.1f;
        public float DurationMax = 1.6f;
        public bool RandomizeFOV = false;
        public float FOVMin = 0.75f;
        public float FOVMax = 0.9f;
        public bool UseCustomReturnPercent = false;
        public float ReturnStartPercent = 50f;
        
        public static CKFirstPersonTimingSettings CreateDefault() => new CKFirstPersonTimingSettings();
    }
    
    [Serializable]
    public class CKFirstPersonVisualSettings
    {
        public bool EnableVignette = true;
        public bool EnableDesaturation = true;
        public bool EnableBloodMoonTint = true;
        public bool OverrideFOVMultiplier = false;
        public float FOVMultiplier = 0.85f;
        public bool Vignette = true;
        public float VignetteIntensity = 0.45f;
        public bool RandomizeVignetteIntensity = false;
        public float VignetteIntensityMin = 0.3f;
        public float VignetteIntensityMax = 0.6f;
        public bool ChromaticAberration = true;
        public float ChromaticAberrationIntensity = 0.25f;
        public bool FilmGrain = true;
        public float FilmGrainIntensity = 0.15f;
        
        public static CKFirstPersonVisualSettings CreateDefault() => new CKFirstPersonVisualSettings();
    }
    
    [Serializable]
    public class CKProjectileVisualSettings
    {
        public bool EnableVignette = true;
        public bool EnableDesaturation = true;
        public bool EnableBloodMoonTint = true;
        public bool OverrideFOVMultiplier = false;
        public float FOVMultiplier = 0.85f;
        public bool ImpactFlash = true;
        public float FlashIntensity = 0.8f;
        public bool RandomizeFlashIntensity = false;
        public float FlashIntensityMin = 0.6f;
        public float FlashIntensityMax = 1f;
        public bool ProjectileOnlyVignette = false;
        public bool UseFirstPersonStyleForProjectile = false;
        
        public static CKProjectileVisualSettings CreateDefault() => new CKProjectileVisualSettings();
    }
    
    [Serializable]
    public class CKHUDElementSettings
    {
        public bool HideAllHUDDuringCinematic = false;
        public bool EnableGlobalHUDHiding = false;
        public bool PreserveSniperScope = true;  // Don't hide scope overlay when aiming with sniper/scoped weapons
        public bool HideCrosshair = false;
        public bool HideHealthBar = false;
        public bool HideStaminaBar = false;
        public bool HideFoodBar = false;
        public bool HideWaterBar = false;
        public bool HideCompass = false;
        public bool HideMinimap = false;
        public bool HideToolbelt = false;
        public bool HideQuickSlots = false;
        public bool HideQuestTracker = false;
        public bool HideBuffIcons = false;
        public bool HideBloodmoonTimer = false;
        public bool HideAimingReticle = false;
        public bool HideInteractionPrompts = false;
        public bool HideAmmoCount = false;
        public bool HideDamageFeedback = false;
        public bool HideHUDDuringCinematic { get => HideAllHUDDuringCinematic; set => HideAllHUDDuringCinematic = value; }
        
        public static CKHUDElementSettings CreateDefault() => new CKHUDElementSettings();
    }
    
    [Serializable]
    public class CKToastSettings
    {
        public bool Enabled = true;
        public float Duration = 2.5f;
        public float FadeInTime = 0.2f;
        public float FadeOutTime = 0.5f;
        public float Opacity = 0.9f;
        public int Position = 2;  // 0=TopRight, 1=TopLeft, 2=BottomRight, 3=BottomLeft
        public float OffsetX = 20f;
        public float OffsetY = 100f;
        public bool ShowCinematicStart = true;
        public bool ShowCameraType = true;
        public bool ShowWeaponMode = true;
        public bool ShowSpecialTriggers = true;
        public bool ShowKillstreak = true;
        public bool ShowCooldown = false;
        public bool ShowSlowMoInfo = false;
        public bool ShowDebugTriggerInfo = false;
        public bool ShowDebugChanceRoll = false;
        public bool ShowDebugCameraChoice = false;
        public bool ShowDebugCooldownTimer = false;
        public bool ShowDebugModeInfo = false;
        public bool ShowDebugOverrides = false;
        public bool PlaySound = false;
        public float SoundVolume = 0.5f;
        
        public static CKToastSettings CreateDefault() => new CKToastSettings();
    }
    
    [Serializable]
    public class CKContextRuleSettings
    {
        public bool ExtraChanceOnFinalEnemy = false;
        public bool ExtraChanceAtLowHealth = false;
        public bool UseDetailedConditions = false;
        public float LongRangeMinDistance = 25f;
        public float LongRangeMaxDistance = 60f;
        public float LowHealthThresholdPercent = 30f;
        public bool OnlyTriggerOnCriticalHit = false;
        
        public static CKContextRuleSettings CreateDefault() => new CKContextRuleSettings();
    }
    
    [Serializable]
    public class CKProfileSettings
    {
        public string ActivePreset = "Simple Default";
        public string CurrentVersion = "v1.0";
        public string ConfigFileName = "CinematicKillSettings.xml";
        public string PresetSummaryMelee = "";
        public string PresetSummaryRanged = "";
        public string PresetSummaryDismember = "";
        public string[] PresetOptions = { "Simple Default", "Cinematic Movie", "Arcade Fast", "Custom" };
        
        public static CKProfileSettings CreateDefault() => new CKProfileSettings();
    }
    
    [Serializable]
    public class CKHUDSettings
    {
        public bool Enabled = true;
        public float Opacity = 0.85f;
        public float MessageDuration = 3f;
        
        public static CKHUDSettings CreateDefault() => new CKHUDSettings();
    }
    
    // FreezeFrameAction enum removed - freeze frame is always priority (blocks cinematics)
    // FreezeCameraMode enum removed - simplified freeze frame stays in current view
    
    /// <summary>
    /// What happens after freeze frame ends
    /// </summary>
    public enum PostFreezeAction
    {
        End = 0,              // End immediately, return to normal gameplay
        ContinueCinematic = 1, // Resume the slow-mo cinematic after freeze
        SwitchCamera = 2,      // Switch to a new camera angle and continue cinematic
        Skip = 3               // End freeze, skip remaining cinematic effects
    }

    /// <summary>
    /// Freeze frame settings for a specific camera type (FP or Projectile)
    /// Each camera type has its own independent freeze frame configuration
    /// </summary>
    [Serializable]
    public class CKFreezeFrameSettings
    {
        // Master controls
        public bool Enabled = false;                    // Master toggle (off by default)
        public float Chance = 100f;                     // Chance to trigger (0-100%)
        public float Duration = 1.0f;                   // Duration of freeze effect (0.5-5 seconds)
        public float Delay = 0f;                        // Delay before freeze starts (0-2 seconds)
        public bool TriggerOnBasicKill = true;          // Trigger on basic kills
        public bool TriggerOnSpecialTrigger = true;     // Trigger on special triggers
        
        // Camera movement during freeze
        public bool EnableCameraMovement = true;        // Enable camera movement during freeze
        public float TimeScale = 0.02f;                 // Super slow motion (not 0) during freeze (0.01-0.1)
        public bool RandomizePreset = true;             // Randomize camera preset
        
        // Post-freeze action
        public PostFreezeAction PostAction = PostFreezeAction.ContinueCinematic;
        public bool RandomizePostCamera = true;         // Randomize camera after freeze
        
        // Visual effects during freeze
        public bool EnableContrastEffect = true;        // High contrast cinematic effect
        public float ContrastAmount = 1.3f;             // Contrast multiplier (1.0 = normal, 1.5 = high)
        
        public static CKFreezeFrameSettings CreateDefault() => new CKFreezeFrameSettings();
    }


    
    /// <summary>
    /// Experimental features - use at your own risk!
    /// </summary>
    [Serializable]
    public class CKExperimentalSettings
    {
        // ═══════════════════════════════════════════════════════════════════════
        // FREEZE FRAME - Pauses action on kill for dramatic effect
        // Duration follows the cinematic duration automatically
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableFreezeFrame = false;              // Master toggle (off by default)
        public float FreezeFrameChance = 100f;              // Chance to trigger (0-100%)
        public int FreezeFrameTiming = 1;                   // 0=Before cinematic, 1=After cinematic, 2=Both
        public bool EnableFreezeFrame_BasicKill = true;     // Trigger on basic kills
        public bool EnableFreezeFrame_SpecialTrigger = true;// Trigger on special triggers
        public float FreezeDelay = 0.0f;                    // Delay before freeze starts (0-2 seconds)
        public float FreezeDuration = 1.0f;                 // Duration of freeze effect (0.5-5 seconds)
        
        // Camera Movement During Freeze
        public bool EnableFreezeCameraMovement = true;      // Enable cinematic camera movement during freeze
        public float FreezeTimeScale = 0.02f;               // Super slow motion (not 0) during freeze (0.01-0.1)
        public bool UseProjectileCameraPresets = true;      // Use projectile camera presets for freeze camera angle
        public bool RandomizeFreezeCameraPreset = true;     // Randomize which preset is used for freeze camera
        
        // Post-Freeze Action
        public PostFreezeAction PostFreeze = PostFreezeAction.ContinueCinematic;
        public bool RandomizePostFreezeCamera = true;       // Randomize camera preset after freeze ends

        
        // ═══════════════════════════════════════════════════════════════════════
        // LEGACY PROPERTIES - Kept for backward config compatibility
        // These are read from config but no longer displayed in UI
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableXRayVision = false;
        public float XRayDuration = 0.5f;
        public float XRayIntensity = 1.0f;
        public bool EnablePredatorVision = false;
        public float PredatorVisionDuration = 1.0f;
        public float PredatorVisionIntensity = 0.8f;
        
        // ═══════════════════════════════════════════════════════════════════════
        // PROJECTILE RIDE CAM - First-person POV on projectile
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableProjectileRideCam = false;     // Master toggle
        public float RideCamFOV = 90f;                   // Wider FOV for bullet POV (60-120)
        public float RideCamChance = 25f;                // Chance to use ride cam vs normal projectile cam (0-100%)
        public float RideCamOffset = 1.5f;               // Distance behind projectile (prevents clipping into target)
        public bool RideCamPredictiveAiming = true;      // Enable predictive aiming detection
        public float RideCamMinTargetHealth = 50f;       // Target health threshold for likely kill (0-100)
        
        // ═══════════════════════════════════════════════════════════════════════
        // DISMEMBERMENT FOCUS CAM - Camera follows severed limb
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableDismemberFocusCam = false;     // Master toggle
        public float FocusCamDistance = 1.5f;            // Distance from limb (0.5-4.0)
        public float FocusCamDuration = 1.0f;            // How long to track the limb (0.5-3.0)
        
        // ═══════════════════════════════════════════════════════════════════════
        // LAST STAND / SECOND WIND - Trigger cinematic on player near-death
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableLastStand = false;             // Master toggle
        public float LastStandDuration = 5.0f;           // How long player has to get a kill (1-10s)
        public float LastStandTimeScale = 0.1f;          // Extreme time dilation (0.05-0.3)
        public float LastStandReviveHealth = 25f;        // Health restored on successful kill (10-50)
        public float LastStandCooldown = 60f;            // Cooldown before can trigger again (30-120s)
        public bool LastStandInfiniteAmmo = true;        // Grant infinite ammo during last stand
        
        // ═══════════════════════════════════════════════════════════════════════
        // CHAIN REACTION - Multi-kill camera daisy chain
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableChainReaction = false;         // Master toggle
        public float ChainReactionWindow = 2.0f;         // Time window to detect next kill (0.5-5s)
        public int ChainReactionMaxKills = 5;            // Maximum kills to chain (2-10)
        public float ChainCameraTransitionTime = 0.5f;   // Time to lerp camera to next victim (0.2-1.0s)
        public bool ChainReactionSlowMoRamp = true;      // Slow time further with each chain
        public float ChainSlowMoMultiplier = 0.8f;       // Multiply timescale by this per chain (0.5-0.9)
        
        // ═══════════════════════════════════════════════════════════════════════
        // SLOW-MO TOGGLE - Toggle slow motion on/off with a keybind
        // ═══════════════════════════════════════════════════════════════════════
        public bool EnableSlowMoToggle = false;          // Master toggle for this feature
        public KeyCode SlowMoToggleKey = KeyCode.Mouse2; // Default: middle mouse button
        public float SlowMoToggleTimeScale = 0.3f;       // Time scale when slow-mo is active (0.1-0.5)
        
        public static CKExperimentalSettings CreateDefault() => new CKExperimentalSettings();
    }
}
