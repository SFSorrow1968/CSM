using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace CSM.Configuration
{
    public static class CSMModOptions
    {
        public const string VERSION = "1.5.0";

        #region Labels and Categories

        public const string CategoryPresetSelection = "Preset Selection";
        public const string CategoryOptionalOverrides = "Optional Overrides";
        public const string CategoryTriggers = "CSM Triggers";
        public const string CategoryKillcam = "CSM Killcam";
        public const string CategoryAdvanced = "CSM Advanced";
        public const string CategoryCustomBasic = "Custom: Basic Kill";
        public const string CategoryCustomCritical = "Custom: Critical Kill";
        public const string CategoryCustomDismemberment = "Custom: Dismemberment";
        public const string CategoryCustomDecapitation = "Custom: Decapitation";
        public const string CategoryCustomLastEnemy = "Custom: Last Enemy";
        public const string CategoryCustomLastStand = "Custom: Last Stand";
        public const string CategoryCustomParry = "Custom: Parry";

        public const string OptionEnableMod = "Enable Mod";
        public const string OptionThirdPersonDistribution = "Third Person Distribution";
        public const string OptionIntensityPreset = "Intensity Preset";
        public const string OptionChancePreset = "Chance Preset";
        public const string OptionCooldownPreset = "Cooldown Preset";
        public const string OptionDurationPreset = "Duration Preset";
        public const string OptionSmoothInPreset = "Fade In";
        public const string OptionSmoothOutPreset = "Fade Out";
        public const string OptionSmoothIn = "Fade In";
        public const string OptionSmoothOut = "Fade Out";
        public const string OptionTriggerProfile = "Trigger Profile";
        public const string OptionGlobalCooldown = "Global Cooldown";
        // Deprecated: Global Smoothing removed in new duration-based system
        public const string OptionHapticFeedback = "Haptic Feedback";
        public const string OptionDynamicIntensity = "Dynamic Intensity";

        public const string TriggerBasicKill = "Basic Kill";
        public const string TriggerThrownImpactKill = "Thrown Impact Kill";
        public const string TriggerCriticalKill = "Critical Kill";
        public const string TriggerDismemberment = "Dismemberment";
        public const string TriggerDecapitation = "Decapitation";
        public const string TriggerLastEnemy = "Last Enemy";
        public const string TriggerLastStand = "Last Stand";
        public const string TriggerParry = "Parry";

        public const string OptionLastStandThreshold = "Last Stand Threshold";

        public const string OptionCameraDistance = "Camera Distance";
        public const string OptionRandomizeDistance = "Randomize Distance";
        public const string OptionCameraHeight = "Camera Height";
        public const string OptionRandomizeHeight = "Randomize Height";
        public const string OptionOrbitSpeed = "Orbit Speed";

        public const string OptionChance = "Chance";
        public const string OptionTimeScale = "Time Scale";
        public const string OptionDuration = "Duration";
        public const string OptionCooldown = "Cooldown";
        public const string OptionSmoothing = "Smoothing";

        public const string OptionDebugLogging = "Debug Logging";
        public const string OptionQuickTestTrigger = "Quick Test Trigger";
        public const string OptionQuickTestNow = "Quick Test Now";

        public const string OptionEasingCurve = "Easing Curve";
        public const string OptionMinTimeScale = "Min Time Scale";
        public const string OptionResetStats = "Reset Statistics";

        public const string CategoryStatistics = "CSM Statistics";
        public const string OptionStatTotalSlowMoTime = "Total Slow-Mo Time";
        public const string OptionStatTriggerCounts = "Trigger Counts";

        #endregion

        #region Enums

        public enum Preset
        {
            Subtle = 0,
            Standard = 1,
            Dramatic = 2,
            Cinematic = 3,
            Epic = 4
        }

        public enum TriggerProfilePreset
        {
            All = 0,
            KillsOnly = 1,
            Highlights = 2,
            LastEnemyOnly = 3,
            ParryOnly = 4
        }

        public enum ChancePreset
        {
            Off = 0,
            VeryRare = 1,
            Rare = 2,
            Standard = 3,
            Frequent = 4
        }

        public enum CooldownPreset
        {
            Off = 0,
            Short = 1,
            Standard = 2,
            Long = 3,
            Extended = 4
        }

        public enum DurationPreset
        {
            VeryShort = 0,
            Short = 1,
            Standard = 2,
            Long = 3,
            Extended = 4
        }

        public enum SmoothnessPreset
        {
            Instant = 0,
            Default = 1,
            QuickFade = 2,
            MediumFade = 3,
            LongFade = 4,
            VeryLongFade = 5
        }

        public enum EasingCurve
        {
            Smoothstep = 0,
            Linear = 1,
            EaseIn = 2,
            EaseOut = 3
        }

        public enum DynamicIntensityPreset
        {
            Off = 0,
            LowSensitivity = 1,
            MediumSensitivity = 2,
            HighSensitivity = 3
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

        private readonly struct PresetOption<TEnum>
        {
            public PresetOption(string label, string value, TEnum preset)
            {
                Label = label;
                Value = value;
                Preset = preset;
            }

            public string Label { get; }
            public string Value { get; }
            public TEnum Preset { get; }
        }

        private static ModOptionString[] BuildStringOptions<TEnum>(PresetOption<TEnum>[] options)
        {
            var result = new ModOptionString[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                result[i] = new ModOptionString(option.Label, option.Value);
            }
            return result;
        }

        private static Dictionary<string, TEnum> BuildPresetMap<TEnum>(PresetOption<TEnum>[] options, Dictionary<string, TEnum> aliases = null)
        {
            var map = new Dictionary<string, TEnum>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (!string.IsNullOrEmpty(option.Label))
                    map[option.Label] = option.Preset;
                if (!string.IsNullOrEmpty(option.Value))
                    map[option.Value] = option.Preset;
            }
            if (aliases != null)
            {
                foreach (var pair in aliases)
                    map[pair.Key] = pair.Value;
            }
            return map;
        }

        private static TEnum ParsePreset<TEnum>(string value, Dictionary<string, TEnum> map, TEnum fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;
            return map.TryGetValue(value, out var preset) ? preset : fallback;
        }

        private static readonly PresetOption<Preset>[] IntensityPresetOptions =
        {
            new PresetOption<Preset>("Subtle", "Subtle", Preset.Subtle),
            new PresetOption<Preset>("Standard", "Standard", Preset.Standard),
            new PresetOption<Preset>("Dramatic", "Dramatic", Preset.Dramatic),
            new PresetOption<Preset>("Cinematic", "Cinematic", Preset.Cinematic),
            new PresetOption<Preset>("Epic", "Epic", Preset.Epic)
        };

        private static readonly PresetOption<TriggerProfilePreset>[] TriggerProfileOptions =
        {
            new PresetOption<TriggerProfilePreset>("All Triggers", "All", TriggerProfilePreset.All),
            new PresetOption<TriggerProfilePreset>("Kills Only", "Kills Only", TriggerProfilePreset.KillsOnly),
            new PresetOption<TriggerProfilePreset>("Highlights", "Highlights", TriggerProfilePreset.Highlights),
            new PresetOption<TriggerProfilePreset>("Last Enemy Only", "Last Enemy Only", TriggerProfilePreset.LastEnemyOnly),
            new PresetOption<TriggerProfilePreset>("Parry Only", "Parry Only", TriggerProfilePreset.ParryOnly)
        };

        private static readonly PresetOption<ChancePreset>[] ChancePresetOptions =
        {
            new PresetOption<ChancePreset>("Off (Cooldown Only)", "Off", ChancePreset.Off),
            new PresetOption<ChancePreset>("Very Rare", "Very Rare", ChancePreset.VeryRare),
            new PresetOption<ChancePreset>("Rare", "Rare", ChancePreset.Rare),
            new PresetOption<ChancePreset>("Standard", "Standard", ChancePreset.Standard),
            new PresetOption<ChancePreset>("Frequent", "Frequent", ChancePreset.Frequent)
        };

        private static readonly PresetOption<CooldownPreset>[] CooldownPresetOptions =
        {
            new PresetOption<CooldownPreset>("Off (No Cooldown)", "Off", CooldownPreset.Off),
            new PresetOption<CooldownPreset>("Short", "Short", CooldownPreset.Short),
            new PresetOption<CooldownPreset>("Standard", "Standard", CooldownPreset.Standard),
            new PresetOption<CooldownPreset>("Long", "Long", CooldownPreset.Long),
            new PresetOption<CooldownPreset>("Extended", "Extended", CooldownPreset.Extended)
        };

        private static readonly PresetOption<DurationPreset>[] DurationPresetOptions =
        {
            new PresetOption<DurationPreset>("Very Short", "Very Short", DurationPreset.VeryShort),
            new PresetOption<DurationPreset>("Short", "Short", DurationPreset.Short),
            new PresetOption<DurationPreset>("Standard", "Standard", DurationPreset.Standard),
            new PresetOption<DurationPreset>("Long", "Long", DurationPreset.Long),
            new PresetOption<DurationPreset>("Extended", "Extended", DurationPreset.Extended)
        };

        private static readonly PresetOption<SmoothnessPreset>[] SmoothnessPresetOptions =
        {
            new PresetOption<SmoothnessPreset>("Instant", "Instant", SmoothnessPreset.Instant),
            new PresetOption<SmoothnessPreset>("Default", "Default", SmoothnessPreset.Default),
            new PresetOption<SmoothnessPreset>("Quick Fade", "Quick Fade", SmoothnessPreset.QuickFade),
            new PresetOption<SmoothnessPreset>("Medium Fade", "Medium Fade", SmoothnessPreset.MediumFade),
            new PresetOption<SmoothnessPreset>("Long Fade", "Long Fade", SmoothnessPreset.LongFade),
            new PresetOption<SmoothnessPreset>("Very Long Fade", "Very Long Fade", SmoothnessPreset.VeryLongFade)
        };

        private static readonly PresetOption<DynamicIntensityPreset>[] DynamicIntensityOptions =
        {
            new PresetOption<DynamicIntensityPreset>("Off", "Off", DynamicIntensityPreset.Off),
            new PresetOption<DynamicIntensityPreset>("Low Sensitivity", "Low Sensitivity", DynamicIntensityPreset.LowSensitivity),
            new PresetOption<DynamicIntensityPreset>("Medium Sensitivity", "Medium Sensitivity", DynamicIntensityPreset.MediumSensitivity),
            new PresetOption<DynamicIntensityPreset>("High Sensitivity", "High Sensitivity", DynamicIntensityPreset.HighSensitivity)
        };

        private static readonly PresetOption<CameraDistributionPreset>[] CameraDistributionOptions =
        {
            new PresetOption<CameraDistributionPreset>("First Person Only", "First Person Only", CameraDistributionPreset.FirstPersonOnly),
            new PresetOption<CameraDistributionPreset>("Mixed (Rare Third Person)", "Mixed (Rare Third Person)", CameraDistributionPreset.MostlyFirstPerson),
            new PresetOption<CameraDistributionPreset>("Mixed", "Mixed", CameraDistributionPreset.Mixed),
            new PresetOption<CameraDistributionPreset>("Mostly Third Person", "Mostly Third Person", CameraDistributionPreset.MostlyThirdPerson),
            new PresetOption<CameraDistributionPreset>("Third Person Only", "Third Person Only", CameraDistributionPreset.ThirdPersonOnly)
        };

        private static readonly Dictionary<string, Preset> IntensityPresetMap = BuildPresetMap(IntensityPresetOptions,
            new Dictionary<string, Preset>
            {
                { "Balanced", Preset.Standard }
            });

        private static readonly Dictionary<string, TriggerProfilePreset> TriggerProfileMap = BuildPresetMap(TriggerProfileOptions);

        private static readonly Dictionary<string, ChancePreset> ChancePresetMap = BuildPresetMap(ChancePresetOptions,
            new Dictionary<string, ChancePreset>
            {
                { "Always", ChancePreset.Off },
                { "Chaos", ChancePreset.Off },
                { "Balanced", ChancePreset.Standard }
            });

        private static readonly Dictionary<string, CooldownPreset> CooldownPresetMap = BuildPresetMap(CooldownPresetOptions,
            new Dictionary<string, CooldownPreset>
            {
                { "Rare", CooldownPreset.Long },
                { "Frequent", CooldownPreset.Short },
                { "Chaos", CooldownPreset.Short },
                { "Balanced", CooldownPreset.Standard }
            });

        private static readonly Dictionary<string, DurationPreset> DurationPresetMap = BuildPresetMap(DurationPresetOptions,
            new Dictionary<string, DurationPreset>
            {
                { "Balanced", DurationPreset.Standard }
            });

        private static readonly Dictionary<string, SmoothnessPreset> SmoothnessPresetMap = BuildPresetMap(SmoothnessPresetOptions);

        private static readonly Dictionary<string, DynamicIntensityPreset> DynamicIntensityMap = BuildPresetMap(DynamicIntensityOptions,
            new Dictionary<string, DynamicIntensityPreset>
            {
                { "True", DynamicIntensityPreset.MediumSensitivity },
                { "true", DynamicIntensityPreset.MediumSensitivity },
                { "On", DynamicIntensityPreset.MediumSensitivity },
                { "False", DynamicIntensityPreset.Off },
                { "false", DynamicIntensityPreset.Off }
            });

        private static readonly Dictionary<string, CameraDistributionPreset> CameraDistributionMap = BuildPresetMap(CameraDistributionOptions,
            new Dictionary<string, CameraDistributionPreset>
            {
                { "Mostly First Person", CameraDistributionPreset.MostlyFirstPerson },
                { "Rare", CameraDistributionPreset.MostlyFirstPerson },
                { "Standard", CameraDistributionPreset.Mixed },
                { "Balanced", CameraDistributionPreset.Mixed },
                { "Frequent", CameraDistributionPreset.MostlyThirdPerson },
                { "Always", CameraDistributionPreset.ThirdPersonOnly }
            });

        #region Value Providers

        public static ModOptionString[] PresetProvider()
        {
            return BuildStringOptions(IntensityPresetOptions);
        }

        public static ModOptionString[] TriggerProfileProvider()
        {
            return BuildStringOptions(TriggerProfileOptions);
        }

        public static ModOptionString[] QuickTestTriggerProvider()
        {
            return new ModOptionString[]
            {
                new ModOptionString(TriggerBasicKill, TriggerBasicKill),
                new ModOptionString(TriggerCriticalKill, TriggerCriticalKill),
                new ModOptionString(TriggerDismemberment, TriggerDismemberment),
                new ModOptionString(TriggerDecapitation, TriggerDecapitation),
                new ModOptionString(TriggerParry, TriggerParry),
                new ModOptionString(TriggerLastEnemy, TriggerLastEnemy),
                new ModOptionString(TriggerLastStand, TriggerLastStand)
            };
        }

        public static ModOptionString[] ChancePresetProvider()
        {
            return BuildStringOptions(ChancePresetOptions);
        }

        public static ModOptionString[] CooldownPresetProvider()
        {
            return BuildStringOptions(CooldownPresetOptions);
        }

        public static ModOptionString[] DurationPresetProvider()
        {
            return BuildStringOptions(DurationPresetOptions);
        }

        public static ModOptionString[] SmoothnessPresetProvider()
        {
            return BuildStringOptions(SmoothnessPresetOptions);
        }

        public static ModOptionString[] EasingCurveProvider()
        {
            return new ModOptionString[]
            {
                new ModOptionString("Smoothstep", "Smoothstep"),
                new ModOptionString("Linear", "Linear"),
                new ModOptionString("Ease In", "EaseIn"),
                new ModOptionString("Ease Out", "EaseOut")
            };
        }

        public static ModOptionFloat[] MinTimeScaleProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("Off (0%)", 0f),
                new ModOptionFloat("5%", 0.05f),
                new ModOptionFloat("8%", 0.08f),
                new ModOptionFloat("10%", 0.1f),
                new ModOptionFloat("15%", 0.15f),
                new ModOptionFloat("20%", 0.2f)
            };
        }

        public static ModOptionString[] DynamicIntensityPresetProvider()
        {
            return BuildStringOptions(DynamicIntensityOptions);
        }

        public static ModOptionString[] CameraDistributionProvider()
        {
            return BuildStringOptions(CameraDistributionOptions);
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
                new ModOptionFloat("12.5%", 0.125f),
                new ModOptionFloat("15%", 0.15f),
                new ModOptionFloat("25%", 0.25f),
                new ModOptionFloat("30%", 0.3f),
                new ModOptionFloat("35%", 0.35f),
                new ModOptionFloat("36%", 0.36f),
                new ModOptionFloat("37.5%", 0.375f),
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
                new ModOptionFloat("0.21x", 0.21f),
                new ModOptionFloat("0.23x", 0.23f),
                new ModOptionFloat("0.25x", 0.25f),
                new ModOptionFloat("0.26x", 0.26f),
                new ModOptionFloat("0.28x", 0.28f),
                new ModOptionFloat("0.30x", 0.3f),
                new ModOptionFloat("0.34x", 0.34f),
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
                new ModOptionFloat("0.5s", 0.5f),
                new ModOptionFloat("0.6s", 0.6f),
                new ModOptionFloat("0.72s", 0.72f),
                new ModOptionFloat("0.75s", 0.75f),
                new ModOptionFloat("0.90s", 0.9f),
                new ModOptionFloat("1.0s", 1.0f),
                new ModOptionFloat("1.125s", 1.125f),
                new ModOptionFloat("1.2s", 1.2f),
                new ModOptionFloat("1.25s", 1.25f),
                new ModOptionFloat("1.4s", 1.4f),
                new ModOptionFloat("1.5s", 1.5f),
                new ModOptionFloat("1.68s", 1.68f),
                new ModOptionFloat("1.8s", 1.8f),
                new ModOptionFloat("1.875s", 1.875f),
                new ModOptionFloat("2.0s", 2.0f),
                new ModOptionFloat("2.1s", 2.1f),
                new ModOptionFloat("2.16s", 2.16f),
                new ModOptionFloat("2.25s", 2.25f),
                new ModOptionFloat("2.4s", 2.4f),
                new ModOptionFloat("2.5s", 2.5f),
                new ModOptionFloat("2.7s", 2.7f),
                new ModOptionFloat("2.75s", 2.75f),
                new ModOptionFloat("2.8s", 2.8f),
                new ModOptionFloat("3.0s", 3.0f),
                new ModOptionFloat("3.25s", 3.25f),
                new ModOptionFloat("3.5s", 3.5f),
                new ModOptionFloat("3.6s", 3.6f),
                new ModOptionFloat("3.75s", 3.75f),
                new ModOptionFloat("4.0s", 4.0f),
                new ModOptionFloat("4.2s", 4.2f),
                new ModOptionFloat("4.5s", 4.5f),
                new ModOptionFloat("5.0s", 5.0f),
                new ModOptionFloat("5.4s", 5.4f),
                new ModOptionFloat("6.0s", 6.0f),
                new ModOptionFloat("6.25s", 6.25f),
                new ModOptionFloat("7.0s", 7.0f),
                new ModOptionFloat("7.5s", 7.5f),
                new ModOptionFloat("9.0s", 9.0f),
                new ModOptionFloat("10.0s", 10.0f),
            };
        }

        public static ModOptionFloat[] CustomCooldownProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("0s", 0f),
                new ModOptionFloat("1.6s", 1.6f),
                new ModOptionFloat("2.0s", 2f),
                new ModOptionFloat("2.4s", 2.4f),
                new ModOptionFloat("2.5s", 2.5f),
                new ModOptionFloat("2.8s", 2.8f),
                new ModOptionFloat("3.0s", 3.0f),
                new ModOptionFloat("3.5s", 3.5f),
                new ModOptionFloat("4.0s", 4f),
                new ModOptionFloat("4.2s", 4.2f),
                new ModOptionFloat("4.9s", 4.9f),
                new ModOptionFloat("5.0s", 5f),
                new ModOptionFloat("6.0s", 6f),
                new ModOptionFloat("7.0s", 7f),
                new ModOptionFloat("7.2s", 7.2f),
                new ModOptionFloat("7.5s", 7.5f),
                new ModOptionFloat("8.0s", 8f),
                new ModOptionFloat("9.0s", 9f),
                new ModOptionFloat("10.0s", 10f),
                new ModOptionFloat("10.5s", 10.5f),
                new ModOptionFloat("11.2s", 11.2f),
                new ModOptionFloat("12.6s", 12.6f),
                new ModOptionFloat("14.0s", 14f),
                new ModOptionFloat("17.5s", 17.5f),
                new ModOptionFloat("18.0s", 18f),
                new ModOptionFloat("20.0s", 20f),
                new ModOptionFloat("22.5s", 22.5f),
                new ModOptionFloat("24.5s", 24.5f),
                new ModOptionFloat("27.0s", 27.0f),
                new ModOptionFloat("28.0s", 28f),
                new ModOptionFloat("31.5s", 31.5f),
                new ModOptionFloat("45.0s", 45f),
                new ModOptionFloat("60.0s", 60f),
                new ModOptionFloat("67.5s", 67.5f),
                new ModOptionFloat("81.0s", 81.0f),
                new ModOptionFloat("90.0s", 90f),
                new ModOptionFloat("126.0s", 126.0f),
                new ModOptionFloat("157.5s", 157.5f)
            };
        }

        public static ModOptionFloat[] CustomSmoothingProvider()
        {
            return new ModOptionFloat[]
            {
                new ModOptionFloat("1.6x", 1.6f),
                new ModOptionFloat("1.8x", 1.8f),
                new ModOptionFloat("2x", 2f),
                new ModOptionFloat("2.4x", 2.4f),
                new ModOptionFloat("2.7x", 2.7f),
                new ModOptionFloat("3x", 3f),
                new ModOptionFloat("3.2x", 3.2f),
                new ModOptionFloat("3.6x", 3.6f),
                new ModOptionFloat("4x", 4f),
                new ModOptionFloat("4.5x", 4.5f),
                new ModOptionFloat("5x", 5f),
                new ModOptionFloat("6x", 6f),
                new ModOptionFloat("7.5x", 7.5f),
                new ModOptionFloat("8x", 8f),
                new ModOptionFloat("9x", 9f),
                new ModOptionFloat("10x", 10f),
                new ModOptionFloat("12x", 12f),
                new ModOptionFloat("12.5x", 12.5f),
                new ModOptionFloat("15x", 15f),
                new ModOptionFloat("16x", 16f),
                new ModOptionFloat("20x", 20f),
                new ModOptionFloat("25x", 25f)
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

        private const int CategoryOrderPreset = 10;
        private const int CategoryOrderOptional = 20;
        private const int CategoryOrderTriggers = 30;
        private const int CategoryOrderKillcam = 40;
        private const int CategoryOrderCustomBasic = 50;
        private const int CategoryOrderCustomCritical = 51;
        private const int CategoryOrderCustomDismemberment = 52;
        private const int CategoryOrderCustomDecapitation = 53;
        private const int CategoryOrderCustomLastEnemy = 54;
        private const int CategoryOrderCustomLastStand = 55;
        private const int CategoryOrderCustomParry = 56;
        private const int CategoryOrderAdvanced = 90;
        private const int CategoryOrderStatistics = 95;

        #region CSM (Main Settings)

        [ModOption(name = OptionEnableMod, order = 0, defaultValueIndex = 1, tooltip = "Master switch for the entire mod")]
        public static bool EnableMod = true;

        [ModOption(name = OptionThirdPersonDistribution, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 10, defaultValueIndex = 0, valueSourceName = "CameraDistributionProvider", tooltip = "Controls how often third-person killcam appears.")]
        public static string CameraDistribution = "First Person Only";

        [ModOption(name = OptionIntensityPreset, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 20, defaultValueIndex = 1, valueSourceName = "PresetProvider", tooltip = "Intensity profile. Subtle = brief, Standard = default, Dramatic = stronger, Cinematic = dramatic, Epic = extreme")]
        public static string CurrentPreset = "Standard";

        [ModOption(name = OptionChancePreset, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 30, defaultValueIndex = 0, valueSourceName = "ChancePresetProvider", tooltip = "Sets per-trigger chance values. Off means chance is ignored (cooldown only).")]
        public static string ChancePresetSetting = "Off";

        [ModOption(name = OptionCooldownPreset, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 40, defaultValueIndex = 2, valueSourceName = "CooldownPresetProvider", tooltip = "Sets per-trigger cooldown values. Off disables cooldown.")]
        public static string CooldownPresetSetting = "Standard";

        [ModOption(name = OptionDurationPreset, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 50, defaultValueIndex = 2, valueSourceName = "DurationPresetProvider", tooltip = "Sets per-trigger duration values.")]
        public static string DurationPresetSetting = "Standard";

        [ModOption(name = OptionSmoothInPreset, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 60, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition into slow motion. Instant = immediate. Default = natural feel.")]
        public static string SmoothInPresetSetting = "Default";

        [ModOption(name = OptionSmoothOutPreset, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 65, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition out of slow motion. Instant = immediate. Default = natural feel.")]
        public static string SmoothOutPresetSetting = "Default";

        [ModOption(name = OptionTriggerProfile, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 70, defaultValueIndex = 0, valueSourceName = "TriggerProfileProvider", tooltip = "Which triggers are active. Selecting a profile updates the per-trigger toggles.")]
        public static string TriggerProfile = "All";

        #region Optional Overrides

        [ModOption(name = OptionGlobalCooldown, category = CategoryOptionalOverrides, categoryOrder = CategoryOrderOptional, order = 10, defaultValueIndex = 0, valueSourceName = "CooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Minimum time between any slow motion triggers")]
        public static float GlobalCooldown = 0f;

        [ModOption(name = OptionHapticFeedback, category = CategoryOptionalOverrides, categoryOrder = CategoryOrderOptional, order = 20, defaultValueIndex = 0, valueSourceName = "HapticIntensityProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Controller vibration when slow motion triggers")]
        public static float HapticIntensity = 0f;

        [ModOption(name = OptionEasingCurve, category = CategoryOptionalOverrides, categoryOrder = CategoryOrderOptional, order = 30, defaultValueIndex = 0, valueSourceName = "EasingCurveProvider", tooltip = "Transition curve shape. Smoothstep = smooth both ends, Linear = constant speed, EaseIn = slow start, EaseOut = slow end.")]
        public static string EasingCurveSetting = "Smoothstep";

        [ModOption(name = OptionMinTimeScale, category = CategoryOptionalOverrides, categoryOrder = CategoryOrderOptional, order = 40, defaultValueIndex = 0, valueSourceName = "MinTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Floor for time scale to prevent extreme slow-mo stutter. 0% = no limit.")]
        public static float MinTimeScale = 0f;

        // Deprecated: GlobalSmoothing and DynamicIntensity are no longer used in the new duration-based smoothing system
        public static float GlobalSmoothing = -1f;
        public static string DynamicIntensitySetting = "Off";

        #endregion

        public static int LastEnemyMinimumGroup = 1;

        #endregion

        #region CSM Triggers (Enable/Disable)

        [ModOption(name = TriggerBasicKill, category = CategoryTriggers, categoryOrder = CategoryOrderTriggers, order = 10, defaultValueIndex = 1, tooltip = "Trigger on any enemy kill")]
        public static bool EnableBasicKill = true;

        [ModOption(name = TriggerThrownImpactKill, category = CategoryTriggers, categoryOrder = CategoryOrderTriggers, order = 15, defaultValueIndex = 0, tooltip = "Also trigger Basic Kill when a recently thrown enemy dies from the environment")]
        public static bool EnableThrownImpactKill = false;

        [ModOption(name = TriggerCriticalKill, category = CategoryTriggers, categoryOrder = CategoryOrderTriggers, order = 20, defaultValueIndex = 1, tooltip = "Trigger on head/throat kills")]
        public static bool EnableCriticalKill = true;

        [ModOption(name = TriggerDismemberment, category = CategoryTriggers, categoryOrder = CategoryOrderTriggers, order = 30, defaultValueIndex = 1, tooltip = "Trigger when severing limbs")]
        public static bool EnableDismemberment = true;

        [ModOption(name = TriggerDecapitation, category = CategoryTriggers, categoryOrder = CategoryOrderTriggers, order = 40, defaultValueIndex = 1, tooltip = "Trigger on decapitation")]
        public static bool EnableDecapitation = true;

        [ModOption(name = TriggerLastEnemy, category = CategoryTriggers, categoryOrder = CategoryOrderTriggers, order = 50, defaultValueIndex = 1, tooltip = "Trigger when killing the final enemy of a wave")]
        public static bool EnableLastEnemy = true;

        [ModOption(name = TriggerLastStand, category = CategoryTriggers, categoryOrder = CategoryOrderTriggers, order = 60, defaultValueIndex = 1, tooltip = "Trigger when your health drops critically low")]
        public static bool EnableLastStand = true;

        [ModOption(name = OptionLastStandThreshold, category = CategoryTriggers, categoryOrder = CategoryOrderTriggers, order = 70, defaultValueIndex = 1, valueSourceName = "ThresholdProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Health % to trigger Last Stand")]
        public static float LastStandThreshold = 0.15f;

        [ModOption(name = TriggerParry, category = CategoryTriggers, categoryOrder = CategoryOrderTriggers, order = 80, defaultValueIndex = 1, tooltip = "Trigger on successful weapon deflections")]
        public static bool EnableParry = true;

        #endregion

        #region CSM Killcam

        [ModOption(name = OptionCameraDistance, category = CategoryKillcam, categoryOrder = CategoryOrderKillcam, order = 10, defaultValueIndex = 1, valueSourceName = "KillcamDistanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Distance from target")]
        public static float KillcamDistance = 3f;

        [ModOption(name = OptionRandomizeDistance, category = CategoryKillcam, categoryOrder = CategoryOrderKillcam, order = 20, defaultValueIndex = 0, tooltip = "Randomize distance per killcam")]
        public static bool KillcamRandomizeDistance = false;

        [ModOption(name = OptionCameraHeight, category = CategoryKillcam, categoryOrder = CategoryOrderKillcam, order = 30, defaultValueIndex = 1, valueSourceName = "KillcamHeightProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Height offset")]
        public static float KillcamHeight = 1.5f;

        [ModOption(name = OptionRandomizeHeight, category = CategoryKillcam, categoryOrder = CategoryOrderKillcam, order = 40, defaultValueIndex = 0, tooltip = "Randomize height per killcam")]
        public static bool KillcamRandomizeHeight = false;

        [ModOption(name = OptionOrbitSpeed, category = CategoryKillcam, categoryOrder = CategoryOrderKillcam, order = 50, defaultValueIndex = 1, valueSourceName = "KillcamOrbitSpeedProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Camera rotation speed (0 for static)")]
        public static float KillcamOrbitSpeed = 15f;

        #endregion

        #region Custom: Basic Kill

        [ModOption(name = OptionChance, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 10, defaultValueIndex = 2, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float BasicKillChance = 0.25f;

        [ModOption(name = OptionTimeScale, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 20, defaultValueIndex = 9, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float BasicKillTimeScale = 0.28f;

        [ModOption(name = OptionDuration, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 30, defaultValueIndex = 19, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float BasicKillDuration = 2.5f;

        [ModOption(name = OptionCooldown, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 40, defaultValueIndex = 18, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float BasicKillCooldown = 10f;

        [ModOption(name = OptionSmoothIn, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 50, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition into slow motion")]
        public static string BasicKillSmoothIn = "Default";

        [ModOption(name = OptionSmoothOut, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 55, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition out of slow motion")]
        public static string BasicKillSmoothOut = "Default";

        [ModOption(name = OptionThirdPersonDistribution, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 60, defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float BasicKillThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Critical Kill

        [ModOption(name = OptionChance, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 10, defaultValueIndex = 12, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float CriticalKillChance = 0.75f;

        [ModOption(name = OptionTimeScale, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 20, defaultValueIndex = 7, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float CriticalKillTimeScale = 0.25f;

        [ModOption(name = OptionDuration, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 30, defaultValueIndex = 23, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float CriticalKillDuration = 3.0f;

        [ModOption(name = OptionCooldown, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 40, defaultValueIndex = 18, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float CriticalKillCooldown = 10f;

        [ModOption(name = OptionSmoothIn, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 50, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition into slow motion")]
        public static string CriticalKillSmoothIn = "Default";

        [ModOption(name = OptionSmoothOut, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 55, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition out of slow motion")]
        public static string CriticalKillSmoothOut = "Default";

        [ModOption(name = OptionThirdPersonDistribution, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 60, defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float CriticalKillThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Dismemberment

        [ModOption(name = OptionChance, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 10, defaultValueIndex = 10, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float DismembermentChance = 0.6f;

        [ModOption(name = OptionTimeScale, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 20, defaultValueIndex = 10, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float DismembermentTimeScale = 0.3f;

        [ModOption(name = OptionDuration, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 30, defaultValueIndex = 14, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float DismembermentDuration = 2.0f;

        [ModOption(name = OptionCooldown, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 40, defaultValueIndex = 18, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float DismembermentCooldown = 10f;

        [ModOption(name = OptionSmoothIn, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 50, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition into slow motion")]
        public static string DismembermentSmoothIn = "Default";

        [ModOption(name = OptionSmoothOut, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 55, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition out of slow motion")]
        public static string DismembermentSmoothOut = "Default";

        [ModOption(name = OptionThirdPersonDistribution, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 60, defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float DismembermentThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Decapitation

        [ModOption(name = OptionChance, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 10, defaultValueIndex = 14, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float DecapitationChance = 0.9f;

        [ModOption(name = OptionTimeScale, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 20, defaultValueIndex = 6, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float DecapitationTimeScale = 0.23f;

        [ModOption(name = OptionDuration, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 30, defaultValueIndex = 24, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float DecapitationDuration = 3.25f;

        [ModOption(name = OptionCooldown, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 40, defaultValueIndex = 18, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float DecapitationCooldown = 10f;

        [ModOption(name = OptionSmoothIn, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 50, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition into slow motion")]
        public static string DecapitationSmoothIn = "Default";

        [ModOption(name = OptionSmoothOut, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 55, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition out of slow motion")]
        public static string DecapitationSmoothOut = "Default";

        [ModOption(name = OptionThirdPersonDistribution, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 60, defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float DecapitationThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Last Enemy

        [ModOption(name = OptionChance, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 10, defaultValueIndex = 15, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float LastEnemyChance = 1.0f;

        [ModOption(name = OptionTimeScale, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 20, defaultValueIndex = 8, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float LastEnemyTimeScale = 0.26f;

        [ModOption(name = OptionDuration, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 30, defaultValueIndex = 21, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float LastEnemyDuration = 2.75f;

        [ModOption(name = OptionCooldown, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 40, defaultValueIndex = 25, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float LastEnemyCooldown = 20f;

        [ModOption(name = OptionSmoothIn, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 50, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition into slow motion")]
        public static string LastEnemySmoothIn = "Default";

        [ModOption(name = OptionSmoothOut, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 55, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition out of slow motion")]
        public static string LastEnemySmoothOut = "Default";

        [ModOption(name = OptionThirdPersonDistribution, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 60, defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float LastEnemyThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Last Stand

        [ModOption(name = OptionTimeScale, category = CategoryCustomLastStand, categoryOrder = CategoryOrderCustomLastStand, order = 10, defaultValueIndex = 5, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float LastStandTimeScale = 0.21f;

        [ModOption(name = OptionDuration, category = CategoryCustomLastStand, categoryOrder = CategoryOrderCustomLastStand, order = 20, defaultValueIndex = 25, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float LastStandDuration = 3.5f;

        [ModOption(name = OptionCooldown, category = CategoryCustomLastStand, categoryOrder = CategoryOrderCustomLastStand, order = 30, defaultValueIndex = 32, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float LastStandCooldown = 60f;

        [ModOption(name = OptionSmoothIn, category = CategoryCustomLastStand, categoryOrder = CategoryOrderCustomLastStand, order = 40, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition into slow motion")]
        public static string LastStandSmoothIn = "Default";

        [ModOption(name = OptionSmoothOut, category = CategoryCustomLastStand, categoryOrder = CategoryOrderCustomLastStand, order = 45, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition out of slow motion")]
        public static string LastStandSmoothOut = "Default";

        #endregion

        #region Custom: Parry

        [ModOption(name = OptionChance, category = CategoryCustomParry, categoryOrder = CategoryOrderCustomParry, order = 10, defaultValueIndex = 8, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float ParryChance = 0.5f;

        [ModOption(name = OptionTimeScale, category = CategoryCustomParry, categoryOrder = CategoryOrderCustomParry, order = 20, defaultValueIndex = 11, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float ParryTimeScale = 0.34f;

        [ModOption(name = OptionDuration, category = CategoryCustomParry, categoryOrder = CategoryOrderCustomParry, order = 30, defaultValueIndex = 10, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float ParryDuration = 1.5f;

        [ModOption(name = OptionCooldown, category = CategoryCustomParry, categoryOrder = CategoryOrderCustomParry, order = 40, defaultValueIndex = 11, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float ParryCooldown = 5f;

        [ModOption(name = OptionSmoothIn, category = CategoryCustomParry, categoryOrder = CategoryOrderCustomParry, order = 50, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition into slow motion")]
        public static string ParrySmoothIn = "Default";

        [ModOption(name = OptionSmoothOut, category = CategoryCustomParry, categoryOrder = CategoryOrderCustomParry, order = 55, defaultValueIndex = 1, valueSourceName = "SmoothnessPresetProvider", tooltip = "Transition out of slow motion")]
        public static string ParrySmoothOut = "Default";

        #endregion

        #region CSM Advanced

        [ModOption(name = OptionDebugLogging, category = CategoryAdvanced, categoryOrder = CategoryOrderAdvanced, order = 10, defaultValueIndex = 0, tooltip = "Enable verbose debug logging")]
        public static bool DebugLogging = false;

        [ModOption(name = OptionQuickTestTrigger, category = CategoryAdvanced, categoryOrder = CategoryOrderAdvanced, order = 20, defaultValueIndex = 0, valueSourceName = "QuickTestTriggerProvider", tooltip = "Which trigger to simulate")]
        public static string QuickTestTrigger = TriggerBasicKill;

        [ModOption(name = OptionQuickTestNow, category = CategoryAdvanced, categoryOrder = CategoryOrderAdvanced, order = 30, defaultValueIndex = 0, tooltip = "Toggle to fire the selected trigger once")]
        public static bool QuickTestNow = false;

        #endregion

        #region CSM Statistics

        [ModOption(name = OptionResetStats, category = CategoryStatistics, categoryOrder = CategoryOrderStatistics, order = 10, defaultValueIndex = 0, tooltip = "Toggle to reset all statistics")]
        public static bool ResetStatsToggle = false;

        #endregion

        public const float ThrownImpactWindowSeconds = 0f;

        #region Helper Methods

        public enum TriggerField
        {
            Chance,
            TimeScale,
            Duration,
            Cooldown,
            SmoothIn,
            SmoothOut,
            Distribution
        }

        public struct TriggerCustomValues
        {
            public float Chance;
            public float TimeScale;
            public float Duration;
            public float Cooldown;
            public SmoothnessPreset SmoothIn;
            public SmoothnessPreset SmoothOut;
            public float Distribution;
        }

        public static bool IsTriggerEnabled(TriggerType triggerType)
        {
            switch (triggerType)
            {
                case TriggerType.BasicKill: return EnableBasicKill;
                case TriggerType.Critical: return EnableCriticalKill;
                case TriggerType.Dismemberment: return EnableDismemberment;
                case TriggerType.Decapitation: return EnableDecapitation;
                case TriggerType.Parry: return EnableParry;
                case TriggerType.LastEnemy: return EnableLastEnemy;
                case TriggerType.LastStand: return EnableLastStand;
                default: return false;
            }
        }

        public static void SetTriggerEnabled(TriggerType triggerType, bool enabled)
        {
            switch (triggerType)
            {
                case TriggerType.BasicKill: EnableBasicKill = enabled; break;
                case TriggerType.Critical: EnableCriticalKill = enabled; break;
                case TriggerType.Dismemberment: EnableDismemberment = enabled; break;
                case TriggerType.Decapitation: EnableDecapitation = enabled; break;
                case TriggerType.Parry: EnableParry = enabled; break;
                case TriggerType.LastEnemy: EnableLastEnemy = enabled; break;
                case TriggerType.LastStand: EnableLastStand = enabled; break;
            }
        }

        public static bool IsTriggerInstant(TriggerType triggerType)
        {
            var values = GetCustomValues(triggerType);
            return values.SmoothIn == SmoothnessPreset.Instant && values.SmoothOut == SmoothnessPreset.Instant;
        }

        public static EasingCurve GetEasingCurve()
        {
            switch (EasingCurveSetting)
            {
                case "Linear": return EasingCurve.Linear;
                case "EaseIn": return EasingCurve.EaseIn;
                case "EaseOut": return EasingCurve.EaseOut;
                default: return EasingCurve.Smoothstep;
            }
        }

        public static TriggerCustomValues GetCustomValues(TriggerType triggerType)
        {
            var values = new TriggerCustomValues();
            switch (triggerType)
            {
                case TriggerType.BasicKill:
                    values.Chance = BasicKillChance;
                    values.TimeScale = BasicKillTimeScale;
                    values.Duration = BasicKillDuration;
                    values.Cooldown = BasicKillCooldown;
                    values.SmoothIn = ParsePreset(BasicKillSmoothIn, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.SmoothOut = ParsePreset(BasicKillSmoothOut, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.Distribution = BasicKillThirdPersonDistribution;
                    break;
                case TriggerType.Critical:
                    values.Chance = CriticalKillChance;
                    values.TimeScale = CriticalKillTimeScale;
                    values.Duration = CriticalKillDuration;
                    values.Cooldown = CriticalKillCooldown;
                    values.SmoothIn = ParsePreset(CriticalKillSmoothIn, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.SmoothOut = ParsePreset(CriticalKillSmoothOut, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.Distribution = CriticalKillThirdPersonDistribution;
                    break;
                case TriggerType.Dismemberment:
                    values.Chance = DismembermentChance;
                    values.TimeScale = DismembermentTimeScale;
                    values.Duration = DismembermentDuration;
                    values.Cooldown = DismembermentCooldown;
                    values.SmoothIn = ParsePreset(DismembermentSmoothIn, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.SmoothOut = ParsePreset(DismembermentSmoothOut, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.Distribution = DismembermentThirdPersonDistribution;
                    break;
                case TriggerType.Decapitation:
                    values.Chance = DecapitationChance;
                    values.TimeScale = DecapitationTimeScale;
                    values.Duration = DecapitationDuration;
                    values.Cooldown = DecapitationCooldown;
                    values.SmoothIn = ParsePreset(DecapitationSmoothIn, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.SmoothOut = ParsePreset(DecapitationSmoothOut, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.Distribution = DecapitationThirdPersonDistribution;
                    break;
                case TriggerType.Parry:
                    values.Chance = ParryChance;
                    values.TimeScale = ParryTimeScale;
                    values.Duration = ParryDuration;
                    values.Cooldown = ParryCooldown;
                    values.SmoothIn = ParsePreset(ParrySmoothIn, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.SmoothOut = ParsePreset(ParrySmoothOut, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.Distribution = 0f;
                    break;
                case TriggerType.LastEnemy:
                    values.Chance = LastEnemyChance;
                    values.TimeScale = LastEnemyTimeScale;
                    values.Duration = LastEnemyDuration;
                    values.Cooldown = LastEnemyCooldown;
                    values.SmoothIn = ParsePreset(LastEnemySmoothIn, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.SmoothOut = ParsePreset(LastEnemySmoothOut, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.Distribution = LastEnemyThirdPersonDistribution;
                    break;
                case TriggerType.LastStand:
                    values.Chance = 1f;
                    values.TimeScale = LastStandTimeScale;
                    values.Duration = LastStandDuration;
                    values.Cooldown = LastStandCooldown;
                    values.SmoothIn = ParsePreset(LastStandSmoothIn, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.SmoothOut = ParsePreset(LastStandSmoothOut, SmoothnessPresetMap, SmoothnessPreset.Default);
                    values.Distribution = 0f;
                    break;
            }
            return values;
        }

        public static void SetTriggerValue(TriggerType triggerType, TriggerField field, float value)
        {
            switch (triggerType)
            {
                case TriggerType.BasicKill:
                    SetTriggerValue(ref BasicKillChance, ref BasicKillTimeScale, ref BasicKillDuration, ref BasicKillCooldown,
                        ref BasicKillThirdPersonDistribution, field, value);
                    break;
                case TriggerType.Critical:
                    SetTriggerValue(ref CriticalKillChance, ref CriticalKillTimeScale, ref CriticalKillDuration, ref CriticalKillCooldown,
                        ref CriticalKillThirdPersonDistribution, field, value);
                    break;
                case TriggerType.Dismemberment:
                    SetTriggerValue(ref DismembermentChance, ref DismembermentTimeScale, ref DismembermentDuration, ref DismembermentCooldown,
                        ref DismembermentThirdPersonDistribution, field, value);
                    break;
                case TriggerType.Decapitation:
                    SetTriggerValue(ref DecapitationChance, ref DecapitationTimeScale, ref DecapitationDuration, ref DecapitationCooldown,
                        ref DecapitationThirdPersonDistribution, field, value);
                    break;
                case TriggerType.Parry:
                    {
                        float unusedDistribution = 0f;
                        SetTriggerValue(ref ParryChance, ref ParryTimeScale, ref ParryDuration, ref ParryCooldown,
                            ref unusedDistribution, field, value);
                    }
                    break;
                case TriggerType.LastEnemy:
                    SetTriggerValue(ref LastEnemyChance, ref LastEnemyTimeScale, ref LastEnemyDuration, ref LastEnemyCooldown,
                        ref LastEnemyThirdPersonDistribution, field, value);
                    break;
                case TriggerType.LastStand:
                    if (field == TriggerField.Chance || field == TriggerField.Distribution)
                        break;
                    {
                        float unusedChance = 1f;
                        float unusedDistribution = 0f;
                        SetTriggerValue(ref unusedChance, ref LastStandTimeScale, ref LastStandDuration, ref LastStandCooldown,
                            ref unusedDistribution, field, value);
                    }
                    break;
            }
        }

        public static void SetTriggerSmoothPreset(TriggerType triggerType, TriggerField field, string value)
        {
            switch (triggerType)
            {
                case TriggerType.BasicKill:
                    if (field == TriggerField.SmoothIn) BasicKillSmoothIn = value;
                    else if (field == TriggerField.SmoothOut) BasicKillSmoothOut = value;
                    break;
                case TriggerType.Critical:
                    if (field == TriggerField.SmoothIn) CriticalKillSmoothIn = value;
                    else if (field == TriggerField.SmoothOut) CriticalKillSmoothOut = value;
                    break;
                case TriggerType.Dismemberment:
                    if (field == TriggerField.SmoothIn) DismembermentSmoothIn = value;
                    else if (field == TriggerField.SmoothOut) DismembermentSmoothOut = value;
                    break;
                case TriggerType.Decapitation:
                    if (field == TriggerField.SmoothIn) DecapitationSmoothIn = value;
                    else if (field == TriggerField.SmoothOut) DecapitationSmoothOut = value;
                    break;
                case TriggerType.Parry:
                    if (field == TriggerField.SmoothIn) ParrySmoothIn = value;
                    else if (field == TriggerField.SmoothOut) ParrySmoothOut = value;
                    break;
                case TriggerType.LastEnemy:
                    if (field == TriggerField.SmoothIn) LastEnemySmoothIn = value;
                    else if (field == TriggerField.SmoothOut) LastEnemySmoothOut = value;
                    break;
                case TriggerType.LastStand:
                    if (field == TriggerField.SmoothIn) LastStandSmoothIn = value;
                    else if (field == TriggerField.SmoothOut) LastStandSmoothOut = value;
                    break;
            }
        }

        private static void SetTriggerValue(ref float chance, ref float timeScale, ref float duration, ref float cooldown,
            ref float distribution, TriggerField field, float value)
        {
            switch (field)
            {
                case TriggerField.Chance:
                    chance = value;
                    break;
                case TriggerField.TimeScale:
                    timeScale = value;
                    break;
                case TriggerField.Duration:
                    duration = value;
                    break;
                case TriggerField.Cooldown:
                    cooldown = value;
                    break;
                case TriggerField.Distribution:
                    distribution = value;
                    break;
            }
        }

        public static Preset GetCurrentPreset()
        {
            return ParsePreset(CurrentPreset, IntensityPresetMap, Preset.Standard);
        }

        public static TriggerProfilePreset GetTriggerProfilePreset()
        {
            return ParsePreset(TriggerProfile, TriggerProfileMap, TriggerProfilePreset.All);
        }

        public static TriggerType GetQuickTestTrigger()
        {
            switch (QuickTestTrigger)
            {
                case TriggerBasicKill: return TriggerType.BasicKill;
                case TriggerCriticalKill: return TriggerType.Critical;
                case TriggerDismemberment: return TriggerType.Dismemberment;
                case TriggerDecapitation: return TriggerType.Decapitation;
                case TriggerParry: return TriggerType.Parry;
                case TriggerLastEnemy: return TriggerType.LastEnemy;
                case TriggerLastStand: return TriggerType.LastStand;
                default: return TriggerType.BasicKill;
            }
        }

        public static ChancePreset GetChancePreset()
        {
            return ParsePreset(ChancePresetSetting, ChancePresetMap, ChancePreset.Off);
        }

        public static CooldownPreset GetCooldownPreset()
        {
            return ParsePreset(CooldownPresetSetting, CooldownPresetMap, CooldownPreset.Standard);
        }

        public static DurationPreset GetDurationPreset()
        {
            return ParsePreset(DurationPresetSetting, DurationPresetMap, DurationPreset.Standard);
        }

        public static SmoothnessPreset GetSmoothInPreset()
        {
            return ParsePreset(SmoothInPresetSetting, SmoothnessPresetMap, SmoothnessPreset.Default);
        }

        public static SmoothnessPreset GetSmoothOutPreset()
        {
            return ParsePreset(SmoothOutPresetSetting, SmoothnessPresetMap, SmoothnessPreset.Default);
        }

        public static float GetSmoothingPercent(SmoothnessPreset preset)
        {
            switch (preset)
            {
                case SmoothnessPreset.Instant: return 0f;
                case SmoothnessPreset.Default: return 0.10f;
                case SmoothnessPreset.QuickFade: return 0.15f;
                case SmoothnessPreset.MediumFade: return 0.20f;
                case SmoothnessPreset.LongFade: return 0.30f;
                case SmoothnessPreset.VeryLongFade: return 0.40f;
                default: return 0.10f;
            }
        }

        public static DynamicIntensityPreset GetDynamicIntensityPreset()
        {
            return ParsePreset(DynamicIntensitySetting, DynamicIntensityMap, DynamicIntensityPreset.Off);
        }

        public static CameraDistributionPreset GetCameraDistributionPreset()
        {
            return ParsePreset(CameraDistribution, CameraDistributionMap, CameraDistributionPreset.FirstPersonOnly);
        }

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
                case ChancePreset.VeryRare:
                    chanceMultiplier = 0.5f;
                    break;
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
                    cooldownMultiplier = 0.5f;
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

        public static float GetDurationMultiplier()
        {
            var preset = GetDurationPreset();
            switch (preset)
            {
                case DurationPreset.VeryShort:
                    return 0.5f;
                case DurationPreset.Short:
                    return 0.7f;
                case DurationPreset.Long:
                    return 1.3f;
                case DurationPreset.Extended:
                    return 1.5f;
                default:
                    return 1.0f;
            }
        }

        public static void ApplyDurationPreset(ref float duration)
        {
            float durationMultiplier = GetDurationMultiplier();
            duration = Mathf.Max(0.05f, duration * durationMultiplier);
        }

        public static void GetSmoothingDurations(TriggerType triggerType, float duration, out float smoothInDuration, out float smoothOutDuration)
        {
            var values = GetCustomValues(triggerType);
            float inPercent = GetSmoothingPercent(values.SmoothIn);
            float outPercent = GetSmoothingPercent(values.SmoothOut);
            float totalPercent = inPercent + outPercent;

            if (totalPercent > 1f)
            {
                float scale = 1f / totalPercent;
                inPercent *= scale;
                outPercent *= scale;
            }

            smoothInDuration = duration * inPercent;
            smoothOutDuration = duration * outPercent;
        }

        #endregion

        #region Statistics

        private const string StatPrefixSlowMoTime = "CSM_TotalSlowMoTime";
        private const string StatPrefixTriggerCount = "CSM_TriggerCount_";

        public static float GetTotalSlowMoTime()
        {
            return PlayerPrefs.GetFloat(StatPrefixSlowMoTime, 0f);
        }

        public static void AddSlowMoTime(float duration)
        {
            float current = GetTotalSlowMoTime();
            PlayerPrefs.SetFloat(StatPrefixSlowMoTime, current + duration);
        }

        public static int GetTriggerCount(TriggerType type)
        {
            return PlayerPrefs.GetInt(StatPrefixTriggerCount + type.ToString(), 0);
        }

        public static void IncrementTriggerCount(TriggerType type)
        {
            int current = GetTriggerCount(type);
            PlayerPrefs.SetInt(StatPrefixTriggerCount + type.ToString(), current + 1);
        }

        public static void ResetStatistics()
        {
            PlayerPrefs.SetFloat(StatPrefixSlowMoTime, 0f);
            foreach (TriggerType type in Enum.GetValues(typeof(TriggerType)))
            {
                PlayerPrefs.SetInt(StatPrefixTriggerCount + type.ToString(), 0);
            }
            PlayerPrefs.Save();
        }

        public static string GetStatisticsSummary()
        {
            float totalTime = GetTotalSlowMoTime();
            int totalTriggers = 0;
            var sb = new System.Text.StringBuilder();
            sb.Append("Total: ").Append(totalTime.ToString("F1")).Append("s | ");

            foreach (TriggerType type in Enum.GetValues(typeof(TriggerType)))
            {
                int count = GetTriggerCount(type);
                totalTriggers += count;
            }

            sb.Append(totalTriggers).Append(" triggers");
            return sb.ToString();
        }

        #endregion
    }
}
