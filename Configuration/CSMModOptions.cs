using ThunderRoad;
using UnityEngine;

namespace CSM.Configuration
{
    /// <summary>
    /// ModOptions-based configuration for CSM.
    /// Consolidated menu with Preset system for simplified user experience.
    /// </summary>
    public static class CSMModOptions
    {
        public const string VERSION = "1.5.0";

        #region Enums

        public enum Preset
        {
            Subtle = 0,
            Balanced = 1,
            Dramatic = 2,
            Cinematic = 3,
            Epic = 4
        }

        public enum CameraModePreference
        {
            Default = 0,
            FirstPersonOnly = 1,
            ThirdPersonOnly = 2
        }

        public enum TriggerProfilePreset
        {
            All = 0,
            KillsOnly = 1,
            Highlights = 2,
            LastEnemyOnly = 3
        }

        public enum ChancePreset
        {
            Off = 0,
            Rare = 1,
            Balanced = 2,
            Frequent = 3
        }

        public enum CooldownPreset
        {
            Off = 0,
            Short = 1,
            Balanced = 2,
            Long = 3,
            Extended = 4
        }

        public enum DurationPreset
        {
            Short = 0,
            Balanced = 1,
            Long = 2,
            Extended = 3
        }

        public enum SmoothnessPreset
        {
            VerySnappy = 0,
            Snappy = 1,
            Balanced = 2,
            Smooth = 3,
            Cinematic = 4,
            UltraSmooth = 5
        }

        public enum CameraDistributionPreset
        {
            FirstPersonOnly = 0,
            MostlyFirstPerson = 1,
            Mixed = 2,
            MostlyThirdPerson = 3,
            ThirdPersonOnly = 4
        }

        #endregion

        #region Value Providers

        public static ModOptionString[] PresetProvider()
        {
            return new ModOptionString[]
            {
                new ModOptionString("Subtle", "Subtle"),
                new ModOptionString("Balanced", "Balanced"),
                new ModOptionString("Dramatic", "Dramatic"),
                new ModOptionString("Cinematic", "Cinematic"),
                new ModOptionString("Epic", "Epic")
            };
        }

        public static ModOptionString[] CameraModeProvider()
        {
            return new ModOptionString[]
            {
                new ModOptionString("Default (Preset)", "Default"),
                new ModOptionString("First Person Only", "First Person Only"),
                new ModOptionString("Third Person Only", "Third Person Only")
            };
        }

        public static ModOptionString[] TriggerProfileProvider()
        {
            return new ModOptionString[]
            {
                new ModOptionString("All Triggers", "All"),
                new ModOptionString("Kills Only", "Kills Only"),
                new ModOptionString("Highlights", "Highlights"),
                new ModOptionString("Last Enemy Only", "Last Enemy Only")
            };
        }

        public static ModOptionString[] ChancePresetProvider()
        {
            return new ModOptionString[]
            {
                new ModOptionString("Off (Cooldown Only)", "Off"),
                new ModOptionString("Rare", "Rare"),
                new ModOptionString("Balanced", "Balanced"),
                new ModOptionString("Frequent", "Frequent")
            };
        }

        public static ModOptionString[] CooldownPresetProvider()
        {
            return new ModOptionString[]
            {
                new ModOptionString("Off (No Cooldown)", "Off"),
                new ModOptionString("Short", "Short"),
                new ModOptionString("Balanced", "Balanced"),
                new ModOptionString("Long", "Long"),
                new ModOptionString("Extended", "Extended")
            };
        }

        public static ModOptionString[] DurationPresetProvider()
        {
            return new ModOptionString[]
            {
                new ModOptionString("Short", "Short"),
                new ModOptionString("Balanced", "Balanced"),
                new ModOptionString("Long", "Long"),
                new ModOptionString("Extended", "Extended")
            };
        }

        public static ModOptionString[] SmoothnessPresetProvider()
        {
            return new ModOptionString[]
            {
                new ModOptionString("Very Snappy", "Very Snappy"),
                new ModOptionString("Snappy", "Snappy"),
                new ModOptionString("Balanced", "Balanced"),
                new ModOptionString("Smooth", "Smooth"),
                new ModOptionString("Cinematic", "Cinematic"),
                new ModOptionString("Ultra Smooth", "Ultra Smooth")
            };
        }

        public static ModOptionString[] CameraDistributionProvider()
        {
            return new ModOptionString[]
            {
                new ModOptionString("First Person Only", "First Person Only"),
                new ModOptionString("Mixed (Rare Third Person)", "Mixed (Rare Third Person)"),
                new ModOptionString("Mixed", "Mixed"),
                new ModOptionString("Mostly Third Person", "Mostly Third Person"),
                new ModOptionString("Third Person Only", "Third Person Only")
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

        public static ModOptionFloat[] CustomChanceProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("15%", 0.15f),
                new ModOptionFloat("25%", 0.25f),
                new ModOptionFloat("30%", 0.3f),
                new ModOptionFloat("35%", 0.35f),
                new ModOptionFloat("36%", 0.36f),
                new ModOptionFloat("45%", 0.45f),
                new ModOptionFloat("50%", 0.5f),
                new ModOptionFloat("54%", 0.54f),
                new ModOptionFloat("60%", 0.6f),
                new ModOptionFloat("70%", 0.7f),
                new ModOptionFloat("75%", 0.75f),
                new ModOptionFloat("84%", 0.84f),
                new ModOptionFloat("90%", 0.9f),
                new ModOptionFloat("100%", 1.0f)
            };
        }

        public static ModOptionFloat[] CustomTimeScaleProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("0.08x", 0.08f),
                new ModOptionFloat("0.10x", 0.1f),
                new ModOptionFloat("0.12x", 0.12f),
                new ModOptionFloat("0.15x", 0.15f),
                new ModOptionFloat("0.20x", 0.2f),
                new ModOptionFloat("0.25x", 0.25f),
                new ModOptionFloat("0.30x", 0.3f),
                new ModOptionFloat("0.35x", 0.35f),
                new ModOptionFloat("0.40x", 0.4f),
                new ModOptionFloat("0.45x", 0.45f),
                new ModOptionFloat("0.50x", 0.5f)
            };
        }

        public static ModOptionFloat[] CustomDurationProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("0.75s", 0.75f),
                new ModOptionFloat("0.90s", 0.9f),
                new ModOptionFloat("1.0s", 1.0f),
                new ModOptionFloat("1.125s", 1.125f),
                new ModOptionFloat("1.2s", 1.2f),
                new ModOptionFloat("1.25s", 1.25f),
                new ModOptionFloat("1.5s", 1.5f),
                new ModOptionFloat("1.8s", 1.8f),
                new ModOptionFloat("1.875s", 1.875f),
                new ModOptionFloat("2.0s", 2.0f),
                new ModOptionFloat("2.25s", 2.25f),
                new ModOptionFloat("2.5s", 2.5f),
                new ModOptionFloat("3.0s", 3.0f),
                new ModOptionFloat("3.75s", 3.75f),
                new ModOptionFloat("4.5s", 4.5f),
                new ModOptionFloat("5.0s", 5.0f),
                new ModOptionFloat("6.25s", 6.25f),
                new ModOptionFloat("7.5s", 7.5f)
            };
        }

        public static ModOptionFloat[] CustomCooldownProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("0s", 0f),
                new ModOptionFloat("1.6s", 1.6f),
                new ModOptionFloat("2.0s", 2f),
                new ModOptionFloat("2.8s", 2.8f),
                new ModOptionFloat("3.5s", 3.5f),
                new ModOptionFloat("4.0s", 4f),
                new ModOptionFloat("4.9s", 4.9f),
                new ModOptionFloat("5.0s", 5f),
                new ModOptionFloat("6.0s", 6f),
                new ModOptionFloat("7.0s", 7f),
                new ModOptionFloat("7.5s", 7.5f),
                new ModOptionFloat("10.5s", 10.5f),
                new ModOptionFloat("18.0s", 18f),
                new ModOptionFloat("31.5s", 31.5f),
                new ModOptionFloat("45.0s", 45f),
                new ModOptionFloat("67.5s", 67.5f)
            };
        }

        public static ModOptionFloat[] CustomSmoothingProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("2", 2f),
                new ModOptionFloat("3", 3f),
                new ModOptionFloat("4", 4f),
                new ModOptionFloat("4.5", 4.5f),
                new ModOptionFloat("5", 5f),
                new ModOptionFloat("6", 6f),
                new ModOptionFloat("7.5", 7.5f),
                new ModOptionFloat("8", 8f),
                new ModOptionFloat("10", 10f),
                new ModOptionFloat("12.5", 12.5f)
            };
        }

        public static ModOptionFloat[] CustomThirdPersonDistributionProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("Off (0%)", 0f),
                new ModOptionFloat("Rare (40%)", 0.4f),
                new ModOptionFloat("Mixed (100%)", 1.0f),
                new ModOptionFloat("Frequent (140%)", 1.4f),
                new ModOptionFloat("Always (10000%)", 100f)
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

        public static ModOptionFloat[] GlobalSmoothingProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("Per Trigger", -1f),
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

        [ModOption(name = "Enable Mod", category = "Presets", defaultValueIndex = 1, tooltip = "Master switch for the entire mod")]
        public static bool EnableMod = true;

        [ModOption(name = "Third Person Distribution", category = "Preset Settings", defaultValueIndex = 0, valueSourceName = "CameraDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Controls how often third-person killcam appears when Camera Mode is Default.")]
        public static string CameraDistribution = "First Person Only";

        [ModOption(name = "Camera Mode", category = "Preset Settings", defaultValueIndex = 0, valueSourceName = "CameraModeProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Default = Third Person Distribution decides. First Person only disables killcam. Third Person only forces third-person killcam when eligible.")]
        public static string CameraMode = "Default";

        [ModOption(name = "Intensity Preset", category = "Preset Settings", defaultValueIndex = 1, valueSourceName = "PresetProvider", tooltip = "Intensity profile. Subtle = brief, Balanced = default, Dramatic = stronger, Cinematic = dramatic, Epic = extreme")]
        public static string CurrentPreset = "Balanced";

        [ModOption(name = "Chance Preset", category = "Preset Settings", defaultValueIndex = 0, valueSourceName = "ChancePresetProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Sets per-trigger chance values. Off means chance is ignored (cooldown only).")]
        public static string ChancePresetSetting = "Off";

        [ModOption(name = "Cooldown Preset", category = "Preset Settings", defaultValueIndex = 2, valueSourceName = "CooldownPresetProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Sets per-trigger cooldown values. Off disables cooldown.")]
        public static string CooldownPresetSetting = "Balanced";

        [ModOption(name = "Duration Preset", category = "Preset Settings", defaultValueIndex = 1, valueSourceName = "DurationPresetProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Sets per-trigger duration values.")]
        public static string DurationPresetSetting = "Balanced";

        [ModOption(name = "Smoothness Preset", category = "Preset Settings", defaultValueIndex = 2, valueSourceName = "SmoothnessPresetProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Sets per-trigger transition speed values (lower = smoother).")]
        public static string SmoothnessPresetSetting = "Balanced";

        [ModOption(name = "Trigger Profile", category = "Preset Settings", defaultValueIndex = 0, valueSourceName = "TriggerProfileProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Which triggers are active. Selecting a profile updates the per-trigger toggles.")]
        public static string TriggerProfile = "All";

        [ModOption(name = "Global Cooldown", category = "Global Overrides", defaultValueIndex = 2, valueSourceName = "CooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Minimum time between any slow motion triggers")]
        public static float GlobalCooldown = 3f;

        [ModOption(name = "Global Smoothing", category = "Global Overrides", defaultValueIndex = 0, valueSourceName = "GlobalSmoothingProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Override transition speed for all triggers. Per Trigger uses per-trigger smoothing (plus Smoothness Preset).")]
        public static float GlobalSmoothing = -1f;

        [ModOption(name = "Haptic Feedback", category = "Global Overrides", defaultValueIndex = 2, valueSourceName = "HapticIntensityProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Controller vibration when slow motion triggers")]
        public static float HapticIntensity = 0.6f;

        [ModOption(name = "Dynamic Intensity", category = "Global Overrides", defaultValueIndex = 1, tooltip = "Scale intensity based on damage dealt")]
        public static bool DynamicIntensity = true;

        public static int LastEnemyMinimumGroup = 1;

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

        [ModOption(name = "Last Stand Threshold", category = "CSM Triggers", defaultValueIndex = 1, valueSourceName = "ThresholdProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Health % to trigger Last Stand")]
        public static float LastStandThreshold = 0.15f;

        [ModOption(name = "Parry", category = "CSM Triggers", defaultValueIndex = 1, tooltip = "Trigger on successful weapon deflections")]
        public static bool EnableParry = true;

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

        [ModOption(name = "Basic Chance", category = "Custom: Basic Kill", defaultValueIndex = 1, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float BasicKillChance = 0.25f;

        [ModOption(name = "Basic Time Scale", category = "Custom: Basic Kill", defaultValueIndex = 7, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float BasicKillTimeScale = 0.35f;

        [ModOption(name = "Basic Duration", category = "Custom: Basic Kill", defaultValueIndex = 2, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float BasicKillDuration = 1.0f;

        [ModOption(name = "Basic Cooldown", category = "Custom: Basic Kill", defaultValueIndex = 7, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float BasicKillCooldown = 5f;

        [ModOption(name = "Basic Smoothing", category = "Custom: Basic Kill", defaultValueIndex = 7, valueSourceName = "CustomSmoothingProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed")]
        public static float BasicKillSmoothing = 8f;

        [ModOption(name = "Basic Third Person Distribution", category = "Custom: Basic Kill", defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float BasicKillThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Critical Kill

        [ModOption(name = "Critical Chance", category = "Custom: Critical Kill", defaultValueIndex = 10, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float CriticalKillChance = 0.75f;

        [ModOption(name = "Critical Time Scale", category = "Custom: Critical Kill", defaultValueIndex = 5, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float CriticalKillTimeScale = 0.25f;

        [ModOption(name = "Critical Duration", category = "Custom: Critical Kill", defaultValueIndex = 6, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float CriticalKillDuration = 1.5f;

        [ModOption(name = "Critical Cooldown", category = "Custom: Critical Kill", defaultValueIndex = 7, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float CriticalKillCooldown = 5f;

        [ModOption(name = "Critical Smoothing", category = "Custom: Critical Kill", defaultValueIndex = 7, valueSourceName = "CustomSmoothingProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed")]
        public static float CriticalKillSmoothing = 8f;

        [ModOption(name = "Critical Third Person Distribution", category = "Custom: Critical Kill", defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float CriticalKillThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Dismemberment

        [ModOption(name = "Dismember Chance", category = "Custom: Dismemberment", defaultValueIndex = 8, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float DismembermentChance = 0.6f;

        [ModOption(name = "Dismember Time Scale", category = "Custom: Dismemberment", defaultValueIndex = 6, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float DismembermentTimeScale = 0.3f;

        [ModOption(name = "Dismember Duration", category = "Custom: Dismemberment", defaultValueIndex = 6, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float DismembermentDuration = 1.5f;

        [ModOption(name = "Dismember Cooldown", category = "Custom: Dismemberment", defaultValueIndex = 7, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float DismembermentCooldown = 5f;

        [ModOption(name = "Dismember Smoothing", category = "Custom: Dismemberment", defaultValueIndex = 7, valueSourceName = "CustomSmoothingProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed")]
        public static float DismembermentSmoothing = 8f;

        [ModOption(name = "Dismember Third Person Distribution", category = "Custom: Dismemberment", defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float DismembermentThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Decapitation

        [ModOption(name = "Decapitation Chance", category = "Custom: Decapitation", defaultValueIndex = 12, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float DecapitationChance = 0.9f;

        [ModOption(name = "Decapitation Time Scale", category = "Custom: Decapitation", defaultValueIndex = 4, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float DecapitationTimeScale = 0.2f;

        [ModOption(name = "Decapitation Duration", category = "Custom: Decapitation", defaultValueIndex = 9, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float DecapitationDuration = 2.0f;

        [ModOption(name = "Decapitation Cooldown", category = "Custom: Decapitation", defaultValueIndex = 5, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float DecapitationCooldown = 4f;

        [ModOption(name = "Decapitation Smoothing", category = "Custom: Decapitation", defaultValueIndex = 5, valueSourceName = "CustomSmoothingProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed")]
        public static float DecapitationSmoothing = 6f;

        [ModOption(name = "Decapitation Third Person Distribution", category = "Custom: Decapitation", defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float DecapitationThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Last Enemy

        [ModOption(name = "Last Enemy Chance", category = "Custom: Last Enemy", defaultValueIndex = 13, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float LastEnemyChance = 1.0f;

        [ModOption(name = "Last Enemy Time Scale", category = "Custom: Last Enemy", defaultValueIndex = 4, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float LastEnemyTimeScale = 0.2f;

        [ModOption(name = "Last Enemy Duration", category = "Custom: Last Enemy", defaultValueIndex = 12, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float LastEnemyDuration = 3.0f;

        [ModOption(name = "Last Enemy Cooldown", category = "Custom: Last Enemy", defaultValueIndex = 0, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float LastEnemyCooldown = 0f;

        [ModOption(name = "Last Enemy Smoothing", category = "Custom: Last Enemy", defaultValueIndex = 2, valueSourceName = "CustomSmoothingProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed")]
        public static float LastEnemySmoothing = 4f;

        [ModOption(name = "Last Enemy Third Person Distribution", category = "Custom: Last Enemy", defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float LastEnemyThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Last Stand

        [ModOption(name = "Last Stand Time Scale", category = "Custom: Last Stand", defaultValueIndex = 3, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float LastStandTimeScale = 0.15f;

        [ModOption(name = "Last Stand Duration", category = "Custom: Last Stand", defaultValueIndex = 15, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float LastStandDuration = 5.0f;

        [ModOption(name = "Last Stand Cooldown", category = "Custom: Last Stand", defaultValueIndex = 14, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float LastStandCooldown = 45f;

        [ModOption(name = "Last Stand Smoothing", category = "Custom: Last Stand", defaultValueIndex = 2, valueSourceName = "CustomSmoothingProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed")]
        public static float LastStandSmoothing = 4f;

        #endregion

        #region Custom: Parry

        [ModOption(name = "Parry Chance", category = "Custom: Parry", defaultValueIndex = 6, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float ParryChance = 0.5f;

        [ModOption(name = "Parry Time Scale", category = "Custom: Parry", defaultValueIndex = 6, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float ParryTimeScale = 0.3f;

        [ModOption(name = "Parry Duration", category = "Custom: Parry", defaultValueIndex = 4, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float ParryDuration = 1.2f;

        [ModOption(name = "Parry Cooldown", category = "Custom: Parry", defaultValueIndex = 9, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float ParryCooldown = 7f;

        [ModOption(name = "Parry Smoothing", category = "Custom: Parry", defaultValueIndex = 8, valueSourceName = "CustomSmoothingProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Transition speed")]
        public static float ParrySmoothing = 10f;

        #endregion

        #region CSM Advanced

        [ModOption(name = "Debug Logging", category = "CSM Advanced", defaultValueIndex = 0, tooltip = "Enable verbose debug logging")]
        public static bool DebugLogging = false;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get the current preset enum value.
        /// </summary>
        public static Preset GetCurrentPreset()
        {
            switch (CurrentPreset)
            {
                case "Subtle": return Preset.Subtle;
                case "Balanced": return Preset.Balanced;
                case "Dramatic": return Preset.Dramatic;
                case "Cinematic": return Preset.Cinematic;
                case "Epic": return Preset.Epic;
                default: return Preset.Balanced;
            }
        }

        /// <summary>
        /// Get the global camera mode preference.
        /// </summary>
        public static CameraModePreference GetCameraMode()
        {
            switch (CameraMode)
            {
                case "First Person Only": return CameraModePreference.FirstPersonOnly;
                case "Third Person Only": return CameraModePreference.ThirdPersonOnly;
                default: return CameraModePreference.Default;
            }
        }

        /// <summary>
        /// Get trigger profile preset.
        /// </summary>
        public static TriggerProfilePreset GetTriggerProfilePreset()
        {
            switch (TriggerProfile)
            {
                case "All": return TriggerProfilePreset.All;
                case "Kills Only": return TriggerProfilePreset.KillsOnly;
                case "Highlights": return TriggerProfilePreset.Highlights;
                case "Last Enemy Only": return TriggerProfilePreset.LastEnemyOnly;
                default: return TriggerProfilePreset.All;
            }
        }

        /// <summary>
        /// Get chance preset.
        /// </summary>
        public static ChancePreset GetChancePreset()
        {
            switch (ChancePresetSetting)
            {
                case "Rare": return ChancePreset.Rare;
                case "Balanced": return ChancePreset.Balanced;
                case "Frequent": return ChancePreset.Frequent;
                case "Always": return ChancePreset.Off;
                case "Chaos": return ChancePreset.Off;
                default: return ChancePreset.Off;
            }
        }

        /// <summary>
        /// Get cooldown preset.
        /// </summary>
        public static CooldownPreset GetCooldownPreset()
        {
            switch (CooldownPresetSetting)
            {
                case "Off": return CooldownPreset.Off;
                case "Short": return CooldownPreset.Short;
                case "Long": return CooldownPreset.Long;
                case "Extended": return CooldownPreset.Extended;
                case "Rare": return CooldownPreset.Long;
                case "Frequent": return CooldownPreset.Short;
                case "Chaos": return CooldownPreset.Short;
                default: return CooldownPreset.Balanced;
            }
        }

        /// <summary>
        /// Get duration preset.
        /// </summary>
        public static DurationPreset GetDurationPreset()
        {
            switch (DurationPresetSetting)
            {
                case "Short": return DurationPreset.Short;
                case "Long": return DurationPreset.Long;
                case "Extended": return DurationPreset.Extended;
                default: return DurationPreset.Balanced;
            }
        }

        /// <summary>
        /// Get smoothness preset.
        /// </summary>
        public static SmoothnessPreset GetSmoothnessPreset()
        {
            switch (SmoothnessPresetSetting)
            {
                case "Very Snappy": return SmoothnessPreset.VerySnappy;
                case "Snappy": return SmoothnessPreset.Snappy;
                case "Smooth": return SmoothnessPreset.Smooth;
                case "Cinematic": return SmoothnessPreset.Cinematic;
                case "Ultra Smooth": return SmoothnessPreset.UltraSmooth;
                default: return SmoothnessPreset.Balanced;
            }
        }

        /// <summary>
        /// Get camera distribution preset.
        /// </summary>
        public static CameraDistributionPreset GetCameraDistributionPreset()
        {
            switch (CameraDistribution)
            {
                case "First Person Only": return CameraDistributionPreset.FirstPersonOnly;
                case "Mixed (Rare Third Person)":
                case "Mostly First Person":
                case "Rare":
                    return CameraDistributionPreset.MostlyFirstPerson;
                case "Mixed":
                case "Balanced":
                    return CameraDistributionPreset.Mixed;
                case "Mostly Third Person":
                case "Frequent":
                    return CameraDistributionPreset.MostlyThirdPerson;
                case "Third Person Only":
                case "Always":
                    return CameraDistributionPreset.ThirdPersonOnly;
                default:
                    return CameraDistributionPreset.FirstPersonOnly;
            }
        }

        /// <summary>
        /// Killcam chance by trigger, scaled by per-trigger distribution.
        /// </summary>
        public static float GetKillcamChance(TriggerType triggerType)
        {
            if (!IsThirdPersonEligible(triggerType))
                return 0f;
            float baseChance = GetKillcamBaseChance(triggerType);
            float distribution = GetThirdPersonDistribution(triggerType);
            float chance = baseChance * distribution;
            if (chance > 1f) chance = 1f;
            if (chance < 0f) chance = 0f;
            return chance;
        }

        /// <summary>
        /// Per-trigger third-person distribution multiplier (0 disables).
        /// </summary>
        public static float GetThirdPersonDistribution(TriggerType triggerType)
        {
            if (!IsThirdPersonEligible(triggerType))
                return 0f;
            switch (triggerType)
            {
                case TriggerType.BasicKill: return Mathf.Max(0f, BasicKillThirdPersonDistribution);
                case TriggerType.Critical: return Mathf.Max(0f, CriticalKillThirdPersonDistribution);
                case TriggerType.Dismemberment: return Mathf.Max(0f, DismembermentThirdPersonDistribution);
                case TriggerType.Decapitation: return Mathf.Max(0f, DecapitationThirdPersonDistribution);
                case TriggerType.LastEnemy: return Mathf.Max(0f, LastEnemyThirdPersonDistribution);
                default: return 0f;
            }
        }

        /// <summary>
        /// Whether a trigger can use third-person killcam.
        /// </summary>
        public static bool IsThirdPersonEligible(TriggerType triggerType)
        {
            switch (triggerType)
            {
                case TriggerType.Parry:
                case TriggerType.LastStand:
                    return false;
                default:
                    return true;
            }
        }

        private static float GetKillcamBaseChance(TriggerType triggerType)
        {
            switch (triggerType)
            {
                case TriggerType.BasicKill: return 0.15f;
                case TriggerType.Dismemberment: return 0.35f;
                case TriggerType.Critical: return 0.6f;
                case TriggerType.Decapitation: return 0.9f;
                case TriggerType.LastEnemy: return 1.0f;
                case TriggerType.Parry: return 0.2f;
                case TriggerType.LastStand: return 0f;
                default: return 0.3f;
            }
        }

        public static float GetCameraDistributionMultiplier(CameraDistributionPreset preset)
        {
            switch (preset)
            {
                case CameraDistributionPreset.FirstPersonOnly: return 0f;
                case CameraDistributionPreset.MostlyFirstPerson: return 0.4f;
                case CameraDistributionPreset.Mixed: return 1.0f;
                case CameraDistributionPreset.MostlyThirdPerson: return 1.4f;
                case CameraDistributionPreset.ThirdPersonOnly: return 100f;
                default: return 1.0f;
            }
        }

        public static void ApplyChancePreset(ref float chance)
        {
            var preset = GetChancePreset();
            float chanceMultiplier;

            switch (preset)
            {
                case ChancePreset.Off:
                    chance = 1.0f;
                    return;
                case ChancePreset.Rare:
                    chanceMultiplier = 0.6f;
                    break;
                case ChancePreset.Frequent:
                    chanceMultiplier = 1.4f;
                    break;
                default:
                    chanceMultiplier = 1.0f;
                    break;
            }

            chance = Mathf.Clamp01(chance * chanceMultiplier);
        }

        public static void ApplyCooldownPreset(ref float cooldown)
        {
            var preset = GetCooldownPreset();
            float cooldownMultiplier;

            switch (preset)
            {
                case CooldownPreset.Off:
                    cooldown = 0f;
                    return;
                case CooldownPreset.Short:
                    cooldownMultiplier = 0.7f;
                    break;
                case CooldownPreset.Long:
                    cooldownMultiplier = 1.5f;
                    break;
                case CooldownPreset.Extended:
                    cooldownMultiplier = 2.0f;
                    break;
                default:
                    cooldownMultiplier = 1.0f;
                    break;
            }

            cooldown = Mathf.Max(0f, cooldown * cooldownMultiplier);
        }

        public static void ApplyDurationPreset(ref float duration)
        {
            var preset = GetDurationPreset();
            float durationMultiplier;

            switch (preset)
            {
                case DurationPreset.Short:
                    durationMultiplier = 0.75f;
                    break;
                case DurationPreset.Long:
                    durationMultiplier = 1.25f;
                    break;
                case DurationPreset.Extended:
                    durationMultiplier = 1.5f;
                    break;
                default:
                    durationMultiplier = 1.0f;
                    break;
            }

            duration = Mathf.Max(0.05f, duration * durationMultiplier);
        }

        public static void ApplySmoothnessPreset(ref float smoothing)
        {
            var preset = GetSmoothnessPreset();
            float smoothingMultiplier;

            switch (preset)
            {
                case SmoothnessPreset.VerySnappy:
                    smoothingMultiplier = 1.6f;
                    break;
                case SmoothnessPreset.Snappy:
                    smoothingMultiplier = 1.25f;
                    break;
                case SmoothnessPreset.Smooth:
                    smoothingMultiplier = 0.75f;
                    break;
                case SmoothnessPreset.Cinematic:
                    smoothingMultiplier = 0.6f;
                    break;
                case SmoothnessPreset.UltraSmooth:
                    smoothingMultiplier = 0.5f;
                    break;
                default:
                    smoothingMultiplier = 1.0f;
                    break;
            }

            smoothing = Mathf.Max(0f, smoothing * smoothingMultiplier);
        }

        #endregion
    }
}
