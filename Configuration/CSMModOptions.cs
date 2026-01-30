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
        public const string CategoryTriggers = "CSM Triggers";
        public const string CategoryKillcam = "CSM Killcam";
        public const string CategoryAdvanced = "CSM Advanced";
        public const string CategoryCustomBasic = "Basic Kill";
        public const string CategoryCustomCritical = "Critical Kill";
        public const string CategoryCustomDismemberment = "Dismemberment";
        public const string CategoryCustomDecapitation = "Decapitation";
        public const string CategoryCustomLastEnemy = "Last Enemy";
        public const string CategoryCustomLastStand = "Last Stand";
        public const string CategoryCustomParry = "Parry";

        public const string OptionEnableMod = "Enable Mod";
        public const string OptionThirdPersonDistribution = "Third Person Distribution";
        public const string OptionIntensityPreset = "Intensity Preset";
        public const string OptionChancePreset = "Chance Preset";
        public const string OptionCooldownPreset = "Cooldown Preset";
        public const string OptionDurationPreset = "Duration Preset";
        public const string OptionTransitionPreset = "Transition Preset";
        public const string OptionTriggerProfile = "Trigger Profile";

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

        // Legacy shared names (kept for reference, but not used in ModOption attributes)
        public const string OptionChance = "Chance";
        public const string OptionTimeScale = "Time Scale";
        public const string OptionDuration = "Duration";
        public const string OptionCooldown = "Cooldown";

        // Per-trigger unique option names (required for B&S storage key uniqueness)
        public const string OptionBasicChance = "Basic Chance";
        public const string OptionBasicTimeScale = "Basic Time Scale";
        public const string OptionBasicDuration = "Basic Duration";
        public const string OptionBasicCooldown = "Basic Cooldown";
        public const string OptionBasicTransition = "Basic Transition";
        public const string OptionBasicThirdPerson = "Basic Third Person";

        public const string OptionCriticalChance = "Critical Chance";
        public const string OptionCriticalTimeScale = "Critical Time Scale";
        public const string OptionCriticalDuration = "Critical Duration";
        public const string OptionCriticalCooldown = "Critical Cooldown";
        public const string OptionCriticalTransition = "Critical Transition";
        public const string OptionCriticalThirdPerson = "Critical Third Person";

        public const string OptionDismemberChance = "Dismember Chance";
        public const string OptionDismemberTimeScale = "Dismember Time Scale";
        public const string OptionDismemberDuration = "Dismember Duration";
        public const string OptionDismemberCooldown = "Dismember Cooldown";
        public const string OptionDismemberTransition = "Dismember Transition";
        public const string OptionDismemberThirdPerson = "Dismember Third Person";

        public const string OptionDecapChance = "Decap Chance";
        public const string OptionDecapTimeScale = "Decap Time Scale";
        public const string OptionDecapDuration = "Decap Duration";
        public const string OptionDecapCooldown = "Decap Cooldown";
        public const string OptionDecapTransition = "Decap Transition";
        public const string OptionDecapThirdPerson = "Decap Third Person";

        public const string OptionLastEnemyChance = "LastEnemy Chance";
        public const string OptionLastEnemyTimeScale = "LastEnemy Time Scale";
        public const string OptionLastEnemyDuration = "LastEnemy Duration";
        public const string OptionLastEnemyCooldown = "LastEnemy Cooldown";
        public const string OptionLastEnemyTransition = "LastEnemy Transition";
        public const string OptionLastEnemyThirdPerson = "LastEnemy Third Person";

        public const string OptionLastStandTimeScale = "LastStand Time Scale";
        public const string OptionLastStandDuration = "LastStand Duration";
        public const string OptionLastStandCooldown = "LastStand Cooldown";
        public const string OptionLastStandTransition = "LastStand Transition";

        public const string OptionParryChance = "Parry Chance";
        public const string OptionParryTimeScale = "Parry Time Scale";
        public const string OptionParryDuration = "Parry Duration";
        public const string OptionParryCooldown = "Parry Cooldown";
        public const string OptionParryTransition = "Parry Transition";

        public const string OptionDebugLogging = "Debug Logging";
        public const string OptionQuickTestTrigger = "Quick Test Trigger";
        public const string OptionQuickTestNow = "Quick Test Now";

        public const string OptionEasingCurve = "Easing Curve";
        public const string OptionResetStats = "Reset Statistics";

        public const string CategoryStatistics = "CSM Statistics";
        public const string OptionStatTotalSlowMoTime = "Total Slow-Mo Time";
        public const string OptionStatTriggerCounts = "Trigger Counts";

        #endregion

        #region Enums

        public enum Preset
        {
            Subtle = 0,
            Default = 1,
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
            Default = 3,
            Frequent = 4
        }

        public enum CooldownPreset
        {
            Off = 0,
            Short = 1,
            Default = 2,
            Long = 3,
            Extended = 4
        }

        public enum DurationPreset
        {
            VeryShort = 0,
            Short = 1,
            Default = 2,
            Long = 3,
            Extended = 4
        }

        public enum TransitionPreset
        {
            Off = 0,
            Smoothstep = 1,
            Linear = 2
        }

        // Transition ramp time as percentage of duration (20%)
        public const float TransitionRampPercent = 0.20f;

        public enum EasingCurve
        {
            Off = 0,
            Smoothstep = 1,
            Linear = 2
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
            new PresetOption<Preset>("Default", "Default", Preset.Default),
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
            new PresetOption<ChancePreset>("Default", "Default", ChancePreset.Default),
            new PresetOption<ChancePreset>("Frequent", "Frequent", ChancePreset.Frequent)
        };

        private static readonly PresetOption<CooldownPreset>[] CooldownPresetOptions =
        {
            new PresetOption<CooldownPreset>("Off (No Cooldown)", "Off", CooldownPreset.Off),
            new PresetOption<CooldownPreset>("Short", "Short", CooldownPreset.Short),
            new PresetOption<CooldownPreset>("Default", "Default", CooldownPreset.Default),
            new PresetOption<CooldownPreset>("Long", "Long", CooldownPreset.Long),
            new PresetOption<CooldownPreset>("Extended", "Extended", CooldownPreset.Extended)
        };

        private static readonly PresetOption<DurationPreset>[] DurationPresetOptions =
        {
            new PresetOption<DurationPreset>("Very Short", "Very Short", DurationPreset.VeryShort),
            new PresetOption<DurationPreset>("Short", "Short", DurationPreset.Short),
            new PresetOption<DurationPreset>("Default", "Default", DurationPreset.Default),
            new PresetOption<DurationPreset>("Long", "Long", DurationPreset.Long),
            new PresetOption<DurationPreset>("Extended", "Extended", DurationPreset.Extended)
        };

        private static readonly PresetOption<TransitionPreset>[] TransitionPresetOptions =
        {
            new PresetOption<TransitionPreset>("Off (Instant)", "Off", TransitionPreset.Off),
            new PresetOption<TransitionPreset>("Smoothstep", "Smoothstep", TransitionPreset.Smoothstep),
            new PresetOption<TransitionPreset>("Linear", "Linear", TransitionPreset.Linear)
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
                { "Balanced", Preset.Default }
            });

        private static readonly Dictionary<string, TriggerProfilePreset> TriggerProfileMap = BuildPresetMap(TriggerProfileOptions);

        private static readonly Dictionary<string, ChancePreset> ChancePresetMap = BuildPresetMap(ChancePresetOptions,
            new Dictionary<string, ChancePreset>
            {
                { "Always", ChancePreset.Off },
                { "Chaos", ChancePreset.Off },
                { "Balanced", ChancePreset.Default }
            });

        private static readonly Dictionary<string, CooldownPreset> CooldownPresetMap = BuildPresetMap(CooldownPresetOptions,
            new Dictionary<string, CooldownPreset>
            {
                { "Rare", CooldownPreset.Long },
                { "Frequent", CooldownPreset.Short },
                { "Chaos", CooldownPreset.Short },
                { "Balanced", CooldownPreset.Default }
            });

        private static readonly Dictionary<string, DurationPreset> DurationPresetMap = BuildPresetMap(DurationPresetOptions,
            new Dictionary<string, DurationPreset>
            {
                { "Balanced", DurationPreset.Default }
            });

        private static readonly Dictionary<string, TransitionPreset> TransitionPresetMap = BuildPresetMap(TransitionPresetOptions,
            new Dictionary<string, TransitionPreset>
            {
                { "Balanced", TransitionPreset.Smoothstep }
            });

        private static readonly Dictionary<string, CameraDistributionPreset> CameraDistributionMap = BuildPresetMap(CameraDistributionOptions,
            new Dictionary<string, CameraDistributionPreset>
            {
                { "Mostly First Person", CameraDistributionPreset.MostlyFirstPerson },
                { "Rare", CameraDistributionPreset.MostlyFirstPerson },
                { "Default", CameraDistributionPreset.Mixed },
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

        public static ModOptionString[] TransitionPresetProvider()
        {
            return BuildStringOptions(TransitionPresetOptions);
        }

        public static ModOptionString[] EasingCurveProvider()
        {
            return new ModOptionString[]
            {
                new ModOptionString("Off", "Off"),
                new ModOptionString("Smoothstep", "Smoothstep"),
                new ModOptionString("Linear", "Linear")
            };
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
            var list = new System.Collections.Generic.List<ModOptionFloat>();
            // 5% increments from 5% to 100%
            for (int i = 5; i <= 100; i += 5)
            {
                list.Add(new ModOptionFloat($"{i}%", i / 100f));
            }
            return list.ToArray();
        }

        public static ModOptionFloat[] CustomTimeScaleProvider()
        {
            var list = new System.Collections.Generic.List<ModOptionFloat>();
            // 0.01 increments from 0.05 to 0.55 to cover all intensity preset values
            for (int i = 5; i <= 55; i++)
            {
                float val = i / 100f;
                list.Add(new ModOptionFloat($"{val:0.00}x", val));
            }
            return list.ToArray();
        }

        public static ModOptionFloat[] CustomDurationProvider()
        {
            var list = new System.Collections.Generic.List<ModOptionFloat>();
            // 0.01s increments from 0.5s to 7s to cover all duration preset values
            for (int i = 50; i <= 700; i++)
            {
                float val = i / 100f;
                list.Add(new ModOptionFloat($"{val:0.##}s", val));
            }
            return list.ToArray();
        }

        public static ModOptionFloat[] CustomCooldownProvider()
        {
            var list = new System.Collections.Generic.List<ModOptionFloat>();
            list.Add(new ModOptionFloat("0s", 0f));
            // 1s increments from 1s to 30s
            for (int i = 1; i <= 30; i++)
            {
                list.Add(new ModOptionFloat($"{i}s", (float)i));
            }
            // 3s increments from 33s to 60s (covers 54s from cooldown presets)
            for (int i = 33; i <= 60; i += 3)
            {
                list.Add(new ModOptionFloat($"{i}s", (float)i));
            }
            // 10s increments from 70s to 120s
            for (int i = 70; i <= 120; i += 10)
            {
                list.Add(new ModOptionFloat($"{i}s", (float)i));
            }
            // 30s increments from 150s to 270s (covers extended Last Stand cooldown)
            for (int i = 150; i <= 270; i += 30)
            {
                list.Add(new ModOptionFloat($"{i}s", (float)i));
            }
            return list.ToArray();
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

        [ModOption(name = OptionIntensityPreset, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 20, defaultValueIndex = 1, valueSourceName = "PresetProvider", tooltip = "Intensity profile. Subtle = brief, Default = balanced, Dramatic = stronger, Cinematic = dramatic, Epic = extreme")]
        public static string CurrentPreset = "Default";

        [ModOption(name = OptionChancePreset, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 30, defaultValueIndex = 0, valueSourceName = "ChancePresetProvider", tooltip = "Sets per-trigger chance values. Off means chance is ignored (cooldown only).")]
        public static string ChancePresetSetting = "Off";

        [ModOption(name = OptionCooldownPreset, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 40, defaultValueIndex = 2, valueSourceName = "CooldownPresetProvider", tooltip = "Sets per-trigger cooldown values. Off disables cooldown.")]
        public static string CooldownPresetSetting = "Default";

        [ModOption(name = OptionDurationPreset, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 50, defaultValueIndex = 2, valueSourceName = "DurationPresetProvider", tooltip = "Sets per-trigger duration values.")]
        public static string DurationPresetSetting = "Default";

        [ModOption(name = OptionTransitionPreset, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 55, defaultValueIndex = 1, valueSourceName = "TransitionPresetProvider", tooltip = "Sets per-trigger transition curve. Off = instant, Smoothstep = smooth ramp, Linear = constant rate.")]
        public static string TransitionPresetSetting = "Smoothstep";

        [ModOption(name = OptionTriggerProfile, category = CategoryPresetSelection, categoryOrder = CategoryOrderPreset, order = 60, defaultValueIndex = 0, valueSourceName = "TriggerProfileProvider", tooltip = "Which triggers are active. Selecting a profile updates the per-trigger toggles.")]
        public static string TriggerProfile = "All";

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

        [ModOption(name = OptionBasicChance, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 10, defaultValueIndex = 2, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float BasicKillChance = 0.25f;

        [ModOption(name = OptionBasicTimeScale, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 20, defaultValueIndex = 23, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float BasicKillTimeScale = 0.28f;

        [ModOption(name = OptionBasicDuration, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 30, defaultValueIndex = 200, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float BasicKillDuration = 2.5f;

        [ModOption(name = OptionBasicCooldown, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 40, defaultValueIndex = 10, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float BasicKillCooldown = 10f;

        [ModOption(name = OptionBasicTransition, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 50, defaultValueIndex = 1, valueSourceName = "EasingCurveProvider", tooltip = "Curve shape for ramping into slow-mo (Off = instant)")]
        public static string BasicKillEasing = "Smoothstep";

        [ModOption(name = OptionBasicThirdPerson, category = CategoryCustomBasic, categoryOrder = CategoryOrderCustomBasic, order = 60, defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float BasicKillThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Critical Kill

        [ModOption(name = OptionCriticalChance, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 10, defaultValueIndex = 12, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float CriticalKillChance = 0.75f;

        [ModOption(name = OptionCriticalTimeScale, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 20, defaultValueIndex = 20, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float CriticalKillTimeScale = 0.25f;

        [ModOption(name = OptionCriticalDuration, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 30, defaultValueIndex = 250, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float CriticalKillDuration = 3.0f;

        [ModOption(name = OptionCriticalCooldown, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 40, defaultValueIndex = 10, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float CriticalKillCooldown = 10f;

        [ModOption(name = OptionCriticalTransition, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 50, defaultValueIndex = 1, valueSourceName = "EasingCurveProvider", tooltip = "Curve shape for ramping into slow-mo (Off = instant)")]
        public static string CriticalKillEasing = "Smoothstep";

        [ModOption(name = OptionCriticalThirdPerson, category = CategoryCustomCritical, categoryOrder = CategoryOrderCustomCritical, order = 60, defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float CriticalKillThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Dismemberment

        [ModOption(name = OptionDismemberChance, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 10, defaultValueIndex = 4, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float DismembermentChance = 0.3f;

        [ModOption(name = OptionDismemberTimeScale, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 20, defaultValueIndex = 25, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float DismembermentTimeScale = 0.3f;

        [ModOption(name = OptionDismemberDuration, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 30, defaultValueIndex = 150, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float DismembermentDuration = 2.0f;

        [ModOption(name = OptionDismemberCooldown, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 40, defaultValueIndex = 10, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float DismembermentCooldown = 10f;

        [ModOption(name = OptionDismemberTransition, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 50, defaultValueIndex = 1, valueSourceName = "EasingCurveProvider", tooltip = "Curve shape for ramping into slow-mo (Off = instant)")]
        public static string DismembermentEasing = "Smoothstep";

        [ModOption(name = OptionDismemberThirdPerson, category = CategoryCustomDismemberment, categoryOrder = CategoryOrderCustomDismemberment, order = 60, defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float DismembermentThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Decapitation

        [ModOption(name = OptionDecapChance, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 10, defaultValueIndex = 14, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float DecapitationChance = 0.9f;

        [ModOption(name = OptionDecapTimeScale, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 20, defaultValueIndex = 18, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float DecapitationTimeScale = 0.23f;

        [ModOption(name = OptionDecapDuration, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 30, defaultValueIndex = 275, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float DecapitationDuration = 3.25f;

        [ModOption(name = OptionDecapCooldown, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 40, defaultValueIndex = 10, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float DecapitationCooldown = 10f;

        [ModOption(name = OptionDecapTransition, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 50, defaultValueIndex = 1, valueSourceName = "EasingCurveProvider", tooltip = "Curve shape for ramping into slow-mo (Off = instant)")]
        public static string DecapitationEasing = "Smoothstep";

        [ModOption(name = OptionDecapThirdPerson, category = CategoryCustomDecapitation, categoryOrder = CategoryOrderCustomDecapitation, order = 60, defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float DecapitationThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Last Enemy

        [ModOption(name = OptionLastEnemyChance, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 10, defaultValueIndex = 15, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float LastEnemyChance = 1.0f;

        [ModOption(name = OptionLastEnemyTimeScale, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 20, defaultValueIndex = 21, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float LastEnemyTimeScale = 0.26f;

        [ModOption(name = OptionLastEnemyDuration, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 30, defaultValueIndex = 225, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float LastEnemyDuration = 2.75f;

        [ModOption(name = OptionLastEnemyCooldown, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 40, defaultValueIndex = 30, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float LastEnemyCooldown = 30f;

        [ModOption(name = OptionLastEnemyTransition, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 50, defaultValueIndex = 1, valueSourceName = "EasingCurveProvider", tooltip = "Curve shape for ramping into slow-mo (Off = instant)")]
        public static string LastEnemyEasing = "Smoothstep";

        [ModOption(name = OptionLastEnemyThirdPerson, category = CategoryCustomLastEnemy, categoryOrder = CategoryOrderCustomLastEnemy, order = 60, defaultValueIndex = 0, valueSourceName = "CustomThirdPersonDistributionProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Third-person killcam frequency multiplier (0% disables)")]
        public static float LastEnemyThirdPersonDistribution = 0f;

        #endregion

        #region Custom: Last Stand

        [ModOption(name = OptionLastStandTimeScale, category = CategoryCustomLastStand, categoryOrder = CategoryOrderCustomLastStand, order = 10, defaultValueIndex = 25, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float LastStandTimeScale = 0.30f;

        [ModOption(name = OptionLastStandDuration, category = CategoryCustomLastStand, categoryOrder = CategoryOrderCustomLastStand, order = 20, defaultValueIndex = 350, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float LastStandDuration = 4.0f;

        [ModOption(name = OptionLastStandCooldown, category = CategoryCustomLastStand, categoryOrder = CategoryOrderCustomLastStand, order = 30, defaultValueIndex = 43, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float LastStandCooldown = 90f;

        [ModOption(name = OptionLastStandTransition, category = CategoryCustomLastStand, categoryOrder = CategoryOrderCustomLastStand, order = 40, defaultValueIndex = 1, valueSourceName = "EasingCurveProvider", tooltip = "Curve shape for ramping into slow-mo (Off = instant)")]
        public static string LastStandEasing = "Smoothstep";

        #endregion

        #region Custom: Parry

        [ModOption(name = OptionParryChance, category = CategoryCustomParry, categoryOrder = CategoryOrderCustomParry, order = 10, defaultValueIndex = 8, valueSourceName = "CustomChanceProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Chance to trigger")]
        public static float ParryChance = 0.5f;

        [ModOption(name = OptionParryTimeScale, category = CategoryCustomParry, categoryOrder = CategoryOrderCustomParry, order = 20, defaultValueIndex = 29, valueSourceName = "CustomTimeScaleProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Time scale")]
        public static float ParryTimeScale = 0.34f;

        [ModOption(name = OptionParryDuration, category = CategoryCustomParry, categoryOrder = CategoryOrderCustomParry, order = 30, defaultValueIndex = 100, valueSourceName = "CustomDurationProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Duration")]
        public static float ParryDuration = 1.5f;

        [ModOption(name = OptionParryCooldown, category = CategoryCustomParry, categoryOrder = CategoryOrderCustomParry, order = 40, defaultValueIndex = 5, valueSourceName = "CustomCooldownProvider", interactionType = (ModOption.InteractionType)2, tooltip = "Cooldown")]
        public static float ParryCooldown = 5f;

        [ModOption(name = OptionParryTransition, category = CategoryCustomParry, categoryOrder = CategoryOrderCustomParry, order = 50, defaultValueIndex = 1, valueSourceName = "EasingCurveProvider", tooltip = "Curve shape for ramping into slow-mo (Off = instant)")]
        public static string ParryEasing = "Smoothstep";

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
            Distribution
        }

        public struct TriggerCustomValues
        {
            public float Chance;
            public float TimeScale;
            public float Duration;
            public float Cooldown;
            public string Easing;
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

        public static EasingCurve GetEasingCurve(TriggerType triggerType)
        {
            string easingSetting = GetCustomValues(triggerType).Easing ?? "Off";
            switch (easingSetting)
            {
                case "Smoothstep": return EasingCurve.Smoothstep;
                case "Linear": return EasingCurve.Linear;
                default: return EasingCurve.Off;
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
                    values.Easing = BasicKillEasing;
                    values.Distribution = BasicKillThirdPersonDistribution;
                    break;
                case TriggerType.Critical:
                    values.Chance = CriticalKillChance;
                    values.TimeScale = CriticalKillTimeScale;
                    values.Duration = CriticalKillDuration;
                    values.Cooldown = CriticalKillCooldown;
                    values.Easing = CriticalKillEasing;
                    values.Distribution = CriticalKillThirdPersonDistribution;
                    break;
                case TriggerType.Dismemberment:
                    values.Chance = DismembermentChance;
                    values.TimeScale = DismembermentTimeScale;
                    values.Duration = DismembermentDuration;
                    values.Cooldown = DismembermentCooldown;
                    values.Easing = DismembermentEasing;
                    values.Distribution = DismembermentThirdPersonDistribution;
                    break;
                case TriggerType.Decapitation:
                    values.Chance = DecapitationChance;
                    values.TimeScale = DecapitationTimeScale;
                    values.Duration = DecapitationDuration;
                    values.Cooldown = DecapitationCooldown;
                    values.Easing = DecapitationEasing;
                    values.Distribution = DecapitationThirdPersonDistribution;
                    break;
                case TriggerType.Parry:
                    values.Chance = ParryChance;
                    values.TimeScale = ParryTimeScale;
                    values.Duration = ParryDuration;
                    values.Cooldown = ParryCooldown;
                    values.Easing = ParryEasing;
                    values.Distribution = 0f;
                    break;
                case TriggerType.LastEnemy:
                    values.Chance = LastEnemyChance;
                    values.TimeScale = LastEnemyTimeScale;
                    values.Duration = LastEnemyDuration;
                    values.Cooldown = LastEnemyCooldown;
                    values.Easing = LastEnemyEasing;
                    values.Distribution = LastEnemyThirdPersonDistribution;
                    break;
                case TriggerType.LastStand:
                    values.Chance = 1f;
                    values.TimeScale = LastStandTimeScale;
                    values.Duration = LastStandDuration;
                    values.Cooldown = LastStandCooldown;
                    values.Easing = LastStandEasing;
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
            return ParsePreset(CurrentPreset, IntensityPresetMap, Preset.Default);
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
            return ParsePreset(CooldownPresetSetting, CooldownPresetMap, CooldownPreset.Default);
        }

        public static DurationPreset GetDurationPreset()
        {
            return ParsePreset(DurationPresetSetting, DurationPresetMap, DurationPreset.Default);
        }

        public static TransitionPreset GetTransitionPreset()
        {
            return ParsePreset(TransitionPresetSetting, TransitionPresetMap, TransitionPreset.Smoothstep);
        }

        public static string GetTransitionPresetValue()
        {
            var preset = GetTransitionPreset();
            switch (preset)
            {
                case TransitionPreset.Off: return "Off";
                case TransitionPreset.Linear: return "Linear";
                default: return "Smoothstep";
            }
        }

        public static void SetTriggerEasing(TriggerType triggerType, string value)
        {
            switch (triggerType)
            {
                case TriggerType.BasicKill: BasicKillEasing = value; break;
                case TriggerType.Critical: CriticalKillEasing = value; break;
                case TriggerType.Dismemberment: DismembermentEasing = value; break;
                case TriggerType.Decapitation: DecapitationEasing = value; break;
                case TriggerType.Parry: ParryEasing = value; break;
                case TriggerType.LastEnemy: LastEnemyEasing = value; break;
                case TriggerType.LastStand: LastStandEasing = value; break;
            }
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

        /// <summary>
        /// Get chance value for a trigger based on the current ChancePreset.
        /// All values are final (from XLSX), no runtime calculations.
        /// </summary>
        public static float GetPresetChanceValue(TriggerType trigger)
        {
            var preset = GetChancePreset();
            
            // Off = 100% for all triggers
            if (preset == ChancePreset.Off)
                return 1.0f;
            
            // Final values per trigger/preset (from XLSX)
            switch (trigger)
            {
                case TriggerType.BasicKill:
                    switch (preset)
                    {
                        case ChancePreset.VeryRare: return 0.12f;
                        case ChancePreset.Rare: return 0.15f;
                        case ChancePreset.Default: return 0.25f;
                        case ChancePreset.Frequent: return 0.35f;
                    }
                    break;
                case TriggerType.Critical:
                    switch (preset)
                    {
                        case ChancePreset.VeryRare: return 0.38f;
                        case ChancePreset.Rare: return 0.45f;
                        case ChancePreset.Default: return 0.75f;
                        case ChancePreset.Frequent: return 1.0f;
                    }
                    break;
                case TriggerType.Dismemberment:
                    switch (preset)
                    {
                        case ChancePreset.VeryRare: return 0.15f;
                        case ChancePreset.Rare: return 0.18f;
                        case ChancePreset.Default: return 0.30f;
                        case ChancePreset.Frequent: return 0.42f;
                    }
                    break;
                case TriggerType.Decapitation:
                    switch (preset)
                    {
                        case ChancePreset.VeryRare: return 0.45f;
                        case ChancePreset.Rare: return 0.54f;
                        case ChancePreset.Default: return 0.90f;
                        case ChancePreset.Frequent: return 1.0f;
                    }
                    break;
                case TriggerType.Parry:
                    switch (preset)
                    {
                        case ChancePreset.VeryRare: return 0.25f;
                        case ChancePreset.Rare: return 0.30f;
                        case ChancePreset.Default: return 0.50f;
                        case ChancePreset.Frequent: return 0.70f;
                    }
                    break;
                case TriggerType.LastEnemy:
                case TriggerType.LastStand:
                    return 1.0f; // Always 100% for these
            }
            return 0.5f;
        }

        /// <summary>
        /// Get cooldown value for a trigger based on the current CooldownPreset.
        /// All values are final (from XLSX), no runtime calculations.
        /// </summary>
        public static float GetPresetCooldownValue(TriggerType trigger)
        {
            var preset = GetCooldownPreset();
            
            // Off = 0 for all triggers
            if (preset == CooldownPreset.Off)
                return 0f;
            
            // Final values per trigger/preset (from XLSX)
            switch (trigger)
            {
                case TriggerType.BasicKill:
                case TriggerType.Critical:
                case TriggerType.Dismemberment:
                case TriggerType.Decapitation:
                    switch (preset)
                    {
                        case CooldownPreset.Short: return 6f;
                        case CooldownPreset.Default: return 10f;
                        case CooldownPreset.Long: return 20f;
                        case CooldownPreset.Extended: return 30f;
                    }
                    break;
                case TriggerType.Parry:
                    switch (preset)
                    {
                        case CooldownPreset.Short: return 3f;
                        case CooldownPreset.Default: return 5f;
                        case CooldownPreset.Long: return 10f;
                        case CooldownPreset.Extended: return 15f;
                    }
                    break;
                case TriggerType.LastEnemy:
                    switch (preset)
                    {
                        case CooldownPreset.Short: return 18f;
                        case CooldownPreset.Default: return 30f;
                        case CooldownPreset.Long: return 60f;
                        case CooldownPreset.Extended: return 90f;
                    }
                    break;
                case TriggerType.LastStand:
                    switch (preset)
                    {
                        case CooldownPreset.Short: return 54f;
                        case CooldownPreset.Default: return 90f;
                        case CooldownPreset.Long: return 180f;
                        case CooldownPreset.Extended: return 270f;
                    }
                    break;
            }
            return 10f;
        }

        /// <summary>
        /// Get duration value for a trigger based on the current DurationPreset.
        /// All values are final (from XLSX), no runtime calculations.
        /// </summary>
        public static float GetPresetDurationValue(TriggerType trigger)
        {
            var preset = GetDurationPreset();
            
            // Final values per trigger/preset (from XLSX)
            switch (trigger)
            {
                case TriggerType.BasicKill:
                    switch (preset)
                    {
                        case DurationPreset.VeryShort: return 0.9f;
                        case DurationPreset.Short: return 1.75f;
                        case DurationPreset.Default: return 2.5f;
                        case DurationPreset.Long: return 3.4f;
                        case DurationPreset.Extended: return 4.25f;
                    }
                    break;
                case TriggerType.Critical:
                    switch (preset)
                    {
                        case DurationPreset.VeryShort: return 1.05f;
                        case DurationPreset.Short: return 2.1f;
                        case DurationPreset.Default: return 3.0f;
                        case DurationPreset.Long: return 4.05f;
                        case DurationPreset.Extended: return 5.1f;
                    }
                    break;
                case TriggerType.Dismemberment:
                    switch (preset)
                    {
                        case DurationPreset.VeryShort: return 0.7f;
                        case DurationPreset.Short: return 1.4f;
                        case DurationPreset.Default: return 2.0f;
                        case DurationPreset.Long: return 2.7f;
                        case DurationPreset.Extended: return 3.4f;
                    }
                    break;
                case TriggerType.Decapitation:
                    switch (preset)
                    {
                        case DurationPreset.VeryShort: return 1.1f;
                        case DurationPreset.Short: return 2.3f;
                        case DurationPreset.Default: return 3.25f;
                        case DurationPreset.Long: return 4.4f;
                        case DurationPreset.Extended: return 5.5f;
                    }
                    break;
                case TriggerType.Parry:
                    switch (preset)
                    {
                        case DurationPreset.VeryShort: return 0.5f;
                        case DurationPreset.Short: return 1.05f;
                        case DurationPreset.Default: return 1.5f;
                        case DurationPreset.Long: return 2.0f;
                        case DurationPreset.Extended: return 2.55f;
                    }
                    break;
                case TriggerType.LastEnemy:
                    switch (preset)
                    {
                        case DurationPreset.VeryShort: return 0.95f;
                        case DurationPreset.Short: return 1.9f;
                        case DurationPreset.Default: return 2.75f;
                        case DurationPreset.Long: return 3.7f;
                        case DurationPreset.Extended: return 4.7f;
                    }
                    break;
                case TriggerType.LastStand:
                    switch (preset)
                    {
                        case DurationPreset.VeryShort: return 1.4f;
                        case DurationPreset.Short: return 2.8f;
                        case DurationPreset.Default: return 4.0f;
                        case DurationPreset.Long: return 5.4f;
                        case DurationPreset.Extended: return 6.8f;
                    }
                    break;
            }
            return 2.0f;
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
