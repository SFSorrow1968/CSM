using ThunderRoad;

namespace CSM.Configuration
{
    /// <summary>
    /// ModOptions-based configuration for CSM.
    /// Consolidated menu with Preset system for simplified user experience.
    /// Custom mode exposes per-trigger settings for power users.
    /// </summary>
    public static class CSMModOptions
    {
        public const string VERSION = "1.5.0";

        #region Enums

        public enum Preset
        {
            Subtle = 0,
            Balanced = 1,
            Cinematic = 2,
            Custom = 3
        }

        #endregion

        #region Value Providers

        public static ModOptionString[] PresetProvider()
        {
            return new ModOptionString[]
            {
                new ModOptionString("Subtle", "Subtle"),
                new ModOptionString("Balanced", "Balanced"),
                new ModOptionString("Cinematic", "Cinematic"),
                new ModOptionString("Custom", "Custom")
            };
        }

        public static ModOptionFloat[] TimeScaleProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("0.05x", 0.05f),
                new ModOptionFloat("0.10x", 0.1f),
                new ModOptionFloat("0.15x", 0.15f),
                new ModOptionFloat("0.20x", 0.2f),
                new ModOptionFloat("0.25x", 0.25f),
                new ModOptionFloat("0.30x", 0.3f),
                new ModOptionFloat("0.40x", 0.4f),
                new ModOptionFloat("0.50x", 0.5f)
            };
        }

        public static ModOptionFloat[] DurationProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("0.5s", 0.5f),
                new ModOptionFloat("1.0s", 1.0f),
                new ModOptionFloat("1.5s", 1.5f),
                new ModOptionFloat("2.0s", 2.0f),
                new ModOptionFloat("2.5s", 2.5f),
                new ModOptionFloat("3.0s", 3.0f),
                new ModOptionFloat("4.0s", 4.0f),
                new ModOptionFloat("5.0s", 5.0f),
                new ModOptionFloat("8.0s", 8.0f)
            };
        }

        public static ModOptionFloat[] CooldownProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("0s", 0f),
                new ModOptionFloat("2s", 2f),
                new ModOptionFloat("3s", 3f),
                new ModOptionFloat("5s", 5f),
                new ModOptionFloat("10s", 10f),
                new ModOptionFloat("30s", 30f),
                new ModOptionFloat("60s", 60f)
            };
        }

        public static ModOptionFloat[] ChanceProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("10%", 0.1f),
                new ModOptionFloat("20%", 0.2f),
                new ModOptionFloat("30%", 0.3f),
                new ModOptionFloat("50%", 0.5f),
                new ModOptionFloat("75%", 0.75f),
                new ModOptionFloat("100%", 1.0f)
            };
        }

        public static ModOptionFloat[] ThresholdProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("10%", 0.10f),
                new ModOptionFloat("15%", 0.15f),
                new ModOptionFloat("20%", 0.20f),
                new ModOptionFloat("25%", 0.25f),
                new ModOptionFloat("30%", 0.30f)
            };
        }

        public static ModOptionInt[] MinEnemyGroupProvider()
        {
            return new ModOptionInt[]
            {
                new ModOptionInt("1 (every kill)", 1),
                new ModOptionInt("2 enemies", 2),
                new ModOptionInt("3 enemies", 3),
                new ModOptionInt("5 enemies", 5)
            };
        }

        public static ModOptionFloat[] HapticIntensityProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("Off", 0f),
                new ModOptionFloat("Light", 0.3f),
                new ModOptionFloat("Medium", 0.6f),
                new ModOptionFloat("Strong", 1.0f)
            };
        }

        public static ModOptionFloat[] SmoothingSpeedProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("Instant", 0f),
                new ModOptionFloat("Fast", 12f),
                new ModOptionFloat("Medium", 8f),
                new ModOptionFloat("Slow", 4f)
            };
        }

        public static ModOptionFloat[] KillcamDistanceProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("2m", 2f),
                new ModOptionFloat("3m", 3f),
                new ModOptionFloat("4m", 4f),
                new ModOptionFloat("5m", 5f)
            };
        }

        public static ModOptionFloat[] KillcamHeightProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("1m", 1f),
                new ModOptionFloat("1.5m", 1.5f),
                new ModOptionFloat("2m", 2f)
            };
        }

        public static ModOptionFloat[] KillcamOrbitSpeedProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("None", 0f),
                new ModOptionFloat("Slow", 15f),
                new ModOptionFloat("Medium", 30f),
                new ModOptionFloat("Fast", 45f)
            };
        }

        #endregion

        #region CSM (Main Settings)

        [ModOption(name = "Enable Mod", category = "CSM", defaultValueIndex = 1, tooltip = "Master switch for the entire mod")]
        public static bool EnableMod = true;

        [ModOption(name = "Preset", category = "CSM", defaultValueIndex = 1, valueSourceName = "PresetProvider", tooltip = "Intensity profile. Subtle = brief, Balanced = default, Cinematic = dramatic, Custom = full control")]
        public static string CurrentPreset = "Balanced";

        [ModOption(name = "Haptic Feedback", category = "CSM", defaultValueIndex = 2, valueSourceName = "HapticIntensityProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Controller vibration when slow motion triggers")]
        public static float HapticIntensity = 0.6f;

        [ModOption(name = "Dynamic Intensity", category = "CSM", defaultValueIndex = 1, tooltip = "Scale intensity based on damage dealt")]
        public static bool DynamicIntensity = true;

        #endregion

        #region CSM Triggers (Enable/Disable)

        [ModOption(name = "Basic Kill", category = "CSM Triggers", defaultValueIndex = 1, tooltip = "Trigger on any enemy kill")]
        public static bool EnableBasicKill = true;

        [ModOption(name = "Critical Kill", category = "CSM Triggers", defaultValueIndex = 1, tooltip = "Trigger on head/throat kills")]
        public static bool EnableCriticalKill = true;

        [ModOption(name = "Dismemberment", category = "CSM Triggers", defaultValueIndex = 1, tooltip = "Trigger when severing limbs")]
        public static bool EnableDismemberment = true;

        [ModOption(name = "Decapitation", category = "CSM Triggers", defaultValueIndex = 1, tooltip = "Trigger on decapitation")]
        public static bool EnableDecapitation = true;

        [ModOption(name = "Last Enemy", category = "CSM Triggers", defaultValueIndex = 1, tooltip = "Trigger when killing the final enemy of a wave")]
        public static bool EnableLastEnemy = true;

        [ModOption(name = "Last Stand", category = "CSM Triggers", defaultValueIndex = 1, tooltip = "Trigger when your health drops critically low")]
        public static bool EnableLastStand = true;

        [ModOption(name = "Parry", category = "CSM Triggers", defaultValueIndex = 1, tooltip = "Trigger on successful weapon deflections")]
        public static bool EnableParry = true;

        #endregion

        #region CSM Advanced

        [ModOption(name = "Global Cooldown", category = "CSM Advanced", defaultValueIndex = 2, valueSourceName = "CooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Minimum time between any slow motion triggers")]
        public static float GlobalCooldown = 3f;

        [ModOption(name = "Last Stand Threshold", category = "CSM Advanced", defaultValueIndex = 1, valueSourceName = "ThresholdProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Health % to trigger Last Stand")]
        public static float LastStandThreshold = 0.15f;

        [ModOption(name = "Last Enemy Min Group", category = "CSM Advanced", defaultValueIndex = 1, valueSourceName = "MinEnemyGroupProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Minimum enemies in wave for Last Enemy to trigger")]
        public static int LastEnemyMinimumGroup = 2;

        [ModOption(name = "Debug Logging", category = "CSM Advanced", defaultValueIndex = 0, tooltip = "Enable verbose debug logging")]
        public static bool DebugLogging = false;

        #endregion

        #region CSM Killcam

        [ModOption(name = "Enable Killcam", category = "CSM Killcam", defaultValueIndex = 0, tooltip = "WARNING: May cause VR motion sickness")]
        public static bool KillcamEnabled = false;

        [ModOption(name = "Camera Distance", category = "CSM Killcam", defaultValueIndex = 1, valueSourceName = "KillcamDistanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Distance from target")]
        public static float KillcamDistance = 3f;

        [ModOption(name = "Camera Height", category = "CSM Killcam", defaultValueIndex = 1, valueSourceName = "KillcamHeightProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Height offset")]
        public static float KillcamHeight = 1.5f;

        [ModOption(name = "Orbit Speed", category = "CSM Killcam", defaultValueIndex = 1, valueSourceName = "KillcamOrbitSpeedProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Camera rotation speed (0 for static)")]
        public static float KillcamOrbitSpeed = 15f;

        [ModOption(name = "On Decapitation", category = "CSM Killcam", defaultValueIndex = 1, tooltip = "Trigger killcam on decapitation kills")]
        public static bool KillcamOnDecapitation = true;

        [ModOption(name = "On Critical Kill", category = "CSM Killcam", defaultValueIndex = 1, tooltip = "Trigger killcam on critical (head/neck) kills")]
        public static bool KillcamOnCritical = true;

        [ModOption(name = "On Last Enemy", category = "CSM Killcam", defaultValueIndex = 1, tooltip = "Trigger killcam when killing the last enemy")]
        public static bool KillcamOnLastEnemy = true;

        [ModOption(name = "Show Player Body", category = "CSM Killcam", defaultValueIndex = 1, tooltip = "Show player body during killcam (third-person view)")]
        public static bool KillcamShowPlayerBody = true;

        #endregion

        #region Custom: Basic Kill

        [ModOption(name = "Chance", category = "Custom: Basic Kill", defaultValueIndex = 1, valueSourceName = "ChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger (Custom mode only)")]
        public static float BasicKillChance = 0.2f;

        [ModOption(name = "Time Scale", category = "Custom: Basic Kill", defaultValueIndex = 5, valueSourceName = "TimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale (Custom mode only)")]
        public static float BasicKillTimeScale = 0.3f;

        [ModOption(name = "Duration", category = "Custom: Basic Kill", defaultValueIndex = 1, valueSourceName = "DurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration (Custom mode only)")]
        public static float BasicKillDuration = 1.0f;

        [ModOption(name = "Cooldown", category = "Custom: Basic Kill", defaultValueIndex = 3, valueSourceName = "CooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown (Custom mode only)")]
        public static float BasicKillCooldown = 5f;

        [ModOption(name = "Smoothing", category = "Custom: Basic Kill", defaultValueIndex = 2, valueSourceName = "SmoothingSpeedProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed (Custom mode only)")]
        public static float BasicKillSmoothing = 8f;

        [ModOption(name = "Third Person", category = "Custom: Basic Kill", defaultValueIndex = 0, tooltip = "Enable killcam (Custom mode only)")]
        public static bool BasicKillThirdPerson = false;

        #endregion

        #region Custom: Critical Kill

        [ModOption(name = "Chance", category = "Custom: Critical Kill", defaultValueIndex = 5, valueSourceName = "ChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger (Custom mode only)")]
        public static float CriticalKillChance = 1.0f;

        [ModOption(name = "Time Scale", category = "Custom: Critical Kill", defaultValueIndex = 3, valueSourceName = "TimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale (Custom mode only)")]
        public static float CriticalKillTimeScale = 0.2f;

        [ModOption(name = "Duration", category = "Custom: Critical Kill", defaultValueIndex = 2, valueSourceName = "DurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration (Custom mode only)")]
        public static float CriticalKillDuration = 1.5f;

        [ModOption(name = "Cooldown", category = "Custom: Critical Kill", defaultValueIndex = 3, valueSourceName = "CooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown (Custom mode only)")]
        public static float CriticalKillCooldown = 5f;

        [ModOption(name = "Smoothing", category = "Custom: Critical Kill", defaultValueIndex = 2, valueSourceName = "SmoothingSpeedProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed (Custom mode only)")]
        public static float CriticalKillSmoothing = 8f;

        [ModOption(name = "Third Person", category = "Custom: Critical Kill", defaultValueIndex = 1, tooltip = "Enable killcam (Custom mode only)")]
        public static bool CriticalKillThirdPerson = true;

        #endregion

        #region Custom: Dismemberment

        [ModOption(name = "Chance", category = "Custom: Dismemberment", defaultValueIndex = 4, valueSourceName = "ChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger (Custom mode only)")]
        public static float DismembermentChance = 0.75f;

        [ModOption(name = "Time Scale", category = "Custom: Dismemberment", defaultValueIndex = 3, valueSourceName = "TimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale (Custom mode only)")]
        public static float DismembermentTimeScale = 0.2f;

        [ModOption(name = "Duration", category = "Custom: Dismemberment", defaultValueIndex = 2, valueSourceName = "DurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration (Custom mode only)")]
        public static float DismembermentDuration = 1.5f;

        [ModOption(name = "Cooldown", category = "Custom: Dismemberment", defaultValueIndex = 3, valueSourceName = "CooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown (Custom mode only)")]
        public static float DismembermentCooldown = 5f;

        [ModOption(name = "Smoothing", category = "Custom: Dismemberment", defaultValueIndex = 2, valueSourceName = "SmoothingSpeedProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed (Custom mode only)")]
        public static float DismembermentSmoothing = 8f;

        [ModOption(name = "Third Person", category = "Custom: Dismemberment", defaultValueIndex = 0, tooltip = "Enable killcam (Custom mode only)")]
        public static bool DismembermentThirdPerson = false;

        #endregion

        #region Custom: Decapitation

        [ModOption(name = "Chance", category = "Custom: Decapitation", defaultValueIndex = 5, valueSourceName = "ChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger (Custom mode only)")]
        public static float DecapitationChance = 1.0f;

        [ModOption(name = "Time Scale", category = "Custom: Decapitation", defaultValueIndex = 2, valueSourceName = "TimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale (Custom mode only)")]
        public static float DecapitationTimeScale = 0.15f;

        [ModOption(name = "Duration", category = "Custom: Decapitation", defaultValueIndex = 3, valueSourceName = "DurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration (Custom mode only)")]
        public static float DecapitationDuration = 2.0f;

        [ModOption(name = "Cooldown", category = "Custom: Decapitation", defaultValueIndex = 3, valueSourceName = "CooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown (Custom mode only)")]
        public static float DecapitationCooldown = 5f;

        [ModOption(name = "Smoothing", category = "Custom: Decapitation", defaultValueIndex = 2, valueSourceName = "SmoothingSpeedProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed (Custom mode only)")]
        public static float DecapitationSmoothing = 8f;

        [ModOption(name = "Third Person", category = "Custom: Decapitation", defaultValueIndex = 1, tooltip = "Enable killcam (Custom mode only)")]
        public static bool DecapitationThirdPerson = true;

        #endregion

        #region Custom: Last Enemy

        [ModOption(name = "Chance", category = "Custom: Last Enemy", defaultValueIndex = 5, valueSourceName = "ChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger (Custom mode only)")]
        public static float LastEnemyChance = 1.0f;

        [ModOption(name = "Time Scale", category = "Custom: Last Enemy", defaultValueIndex = 2, valueSourceName = "TimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale (Custom mode only)")]
        public static float LastEnemyTimeScale = 0.15f;

        [ModOption(name = "Duration", category = "Custom: Last Enemy", defaultValueIndex = 4, valueSourceName = "DurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration (Custom mode only)")]
        public static float LastEnemyDuration = 2.5f;

        [ModOption(name = "Cooldown", category = "Custom: Last Enemy", defaultValueIndex = 0, valueSourceName = "CooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown (Custom mode only)")]
        public static float LastEnemyCooldown = 0f;

        [ModOption(name = "Smoothing", category = "Custom: Last Enemy", defaultValueIndex = 3, valueSourceName = "SmoothingSpeedProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed (Custom mode only)")]
        public static float LastEnemySmoothing = 4f;

        [ModOption(name = "Third Person", category = "Custom: Last Enemy", defaultValueIndex = 1, tooltip = "Enable killcam (Custom mode only)")]
        public static bool LastEnemyThirdPerson = true;

        #endregion

        #region Custom: Last Stand

        [ModOption(name = "Time Scale", category = "Custom: Last Stand", defaultValueIndex = 1, valueSourceName = "TimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale (Custom mode only)")]
        public static float LastStandTimeScale = 0.1f;

        [ModOption(name = "Duration", category = "Custom: Last Stand", defaultValueIndex = 7, valueSourceName = "DurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration (Custom mode only)")]
        public static float LastStandDuration = 5.0f;

        [ModOption(name = "Cooldown", category = "Custom: Last Stand", defaultValueIndex = 6, valueSourceName = "CooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown (Custom mode only)")]
        public static float LastStandCooldown = 60f;

        [ModOption(name = "Smoothing", category = "Custom: Last Stand", defaultValueIndex = 3, valueSourceName = "SmoothingSpeedProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed (Custom mode only)")]
        public static float LastStandSmoothing = 4f;

        [ModOption(name = "Third Person", category = "Custom: Last Stand", defaultValueIndex = 0, tooltip = "Enable killcam (Custom mode only)")]
        public static bool LastStandThirdPerson = false;

        #endregion

        #region Custom: Parry

        [ModOption(name = "Chance", category = "Custom: Parry", defaultValueIndex = 3, valueSourceName = "ChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger (Custom mode only)")]
        public static float ParryChance = 0.5f;

        [ModOption(name = "Time Scale", category = "Custom: Parry", defaultValueIndex = 4, valueSourceName = "TimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale (Custom mode only)")]
        public static float ParryTimeScale = 0.25f;

        [ModOption(name = "Duration", category = "Custom: Parry", defaultValueIndex = 1, valueSourceName = "DurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration (Custom mode only)")]
        public static float ParryDuration = 1.0f;

        [ModOption(name = "Cooldown", category = "Custom: Parry", defaultValueIndex = 4, valueSourceName = "CooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown (Custom mode only)")]
        public static float ParryCooldown = 10f;

        [ModOption(name = "Smoothing", category = "Custom: Parry", defaultValueIndex = 1, valueSourceName = "SmoothingSpeedProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed (Custom mode only)")]
        public static float ParrySmoothing = 12f;

        [ModOption(name = "Third Person", category = "Custom: Parry", defaultValueIndex = 0, tooltip = "Enable killcam (Custom mode only)")]
        public static bool ParryThirdPerson = false;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Returns true if currently in Custom preset mode.
        /// </summary>
        public static bool IsCustomPreset => CurrentPreset == "Custom";

        /// <summary>
        /// Get the current preset enum value.
        /// </summary>
        public static Preset GetCurrentPreset()
        {
            switch (CurrentPreset)
            {
                case "Subtle": return Preset.Subtle;
                case "Balanced": return Preset.Balanced;
                case "Cinematic": return Preset.Cinematic;
                case "Custom": return Preset.Custom;
                default: return Preset.Balanced;
            }
        }

        #endregion
    }
}
