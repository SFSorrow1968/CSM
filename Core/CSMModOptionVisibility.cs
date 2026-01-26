using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CSM.Configuration;
using ThunderRoad;

namespace CSM.Core
{
    public class CSMModOptionVisibility
    {
        public static CSMModOptionVisibility Instance { get; } = new CSMModOptionVisibility();

        private ModManager.ModData _modData;
        private bool _initialized;
        private string _lastIntensityPreset;
        private string _lastChancePreset;
        private string _lastCooldownPreset;
        private string _lastDurationPreset;
        private string _lastSmoothnessPreset;
        private string _lastDistributionPreset;
        private string _lastTriggerProfile;
        private bool _lastDebugLogging;
        private readonly Dictionary<TriggerType, CustomValues> _lastCustomValues =
            new Dictionary<TriggerType, CustomValues>();
        private readonly Dictionary<string, string> _baseTooltips =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly Dictionary<string, ModOption> _modOptionsByKey =
            new Dictionary<string, ModOption>(StringComparer.Ordinal);

        private const string OptionKeySeparator = "||";

        private static readonly TriggerType[] TriggerTypes =
        {
            TriggerType.BasicKill,
            TriggerType.Critical,
            TriggerType.Dismemberment,
            TriggerType.Decapitation,
            TriggerType.Parry,
            TriggerType.LastEnemy,
            TriggerType.LastStand,
        };

        private static readonly Dictionary<TriggerType, string> ChanceOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey("Custom: Basic Kill", "Chance") },
            { TriggerType.Critical, MakeKey("Custom: Critical Kill", "Chance") },
            { TriggerType.Dismemberment, MakeKey("Custom: Dismemberment", "Chance") },
            { TriggerType.Decapitation, MakeKey("Custom: Decapitation", "Chance") },
            { TriggerType.Parry, MakeKey("Custom: Parry", "Chance") },
            { TriggerType.LastEnemy, MakeKey("Custom: Last Enemy", "Chance") }
        };

        private static readonly Dictionary<TriggerType, string> TimeScaleOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey("Custom: Basic Kill", "Time Scale") },
            { TriggerType.Critical, MakeKey("Custom: Critical Kill", "Time Scale") },
            { TriggerType.Dismemberment, MakeKey("Custom: Dismemberment", "Time Scale") },
            { TriggerType.Decapitation, MakeKey("Custom: Decapitation", "Time Scale") },
            { TriggerType.Parry, MakeKey("Custom: Parry", "Time Scale") },
            { TriggerType.LastEnemy, MakeKey("Custom: Last Enemy", "Time Scale") },
            { TriggerType.LastStand, MakeKey("Custom: Last Stand", "Time Scale") }
        };

        private static readonly Dictionary<TriggerType, string> DurationOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey("Custom: Basic Kill", "Duration") },
            { TriggerType.Critical, MakeKey("Custom: Critical Kill", "Duration") },
            { TriggerType.Dismemberment, MakeKey("Custom: Dismemberment", "Duration") },
            { TriggerType.Decapitation, MakeKey("Custom: Decapitation", "Duration") },
            { TriggerType.Parry, MakeKey("Custom: Parry", "Duration") },
            { TriggerType.LastEnemy, MakeKey("Custom: Last Enemy", "Duration") },
            { TriggerType.LastStand, MakeKey("Custom: Last Stand", "Duration") }
        };

        private static readonly Dictionary<TriggerType, string> CooldownOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey("Custom: Basic Kill", "Cooldown") },
            { TriggerType.Critical, MakeKey("Custom: Critical Kill", "Cooldown") },
            { TriggerType.Dismemberment, MakeKey("Custom: Dismemberment", "Cooldown") },
            { TriggerType.Decapitation, MakeKey("Custom: Decapitation", "Cooldown") },
            { TriggerType.Parry, MakeKey("Custom: Parry", "Cooldown") },
            { TriggerType.LastEnemy, MakeKey("Custom: Last Enemy", "Cooldown") },
            { TriggerType.LastStand, MakeKey("Custom: Last Stand", "Cooldown") }
        };

        private static readonly Dictionary<TriggerType, string> SmoothingOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey("Custom: Basic Kill", "Smoothing") },
            { TriggerType.Critical, MakeKey("Custom: Critical Kill", "Smoothing") },
            { TriggerType.Dismemberment, MakeKey("Custom: Dismemberment", "Smoothing") },
            { TriggerType.Decapitation, MakeKey("Custom: Decapitation", "Smoothing") },
            { TriggerType.Parry, MakeKey("Custom: Parry", "Smoothing") },
            { TriggerType.LastEnemy, MakeKey("Custom: Last Enemy", "Smoothing") },
            { TriggerType.LastStand, MakeKey("Custom: Last Stand", "Smoothing") }
        };

        private static readonly Dictionary<TriggerType, string> DistributionOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey("Custom: Basic Kill", "Third Person Distribution") },
            { TriggerType.Critical, MakeKey("Custom: Critical Kill", "Third Person Distribution") },
            { TriggerType.Dismemberment, MakeKey("Custom: Dismemberment", "Third Person Distribution") },
            { TriggerType.Decapitation, MakeKey("Custom: Decapitation", "Third Person Distribution") },
            { TriggerType.LastEnemy, MakeKey("Custom: Last Enemy", "Third Person Distribution") }
        };

        private static readonly Dictionary<TriggerType, string> TriggerToggleOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey("CSM Triggers", "Basic Kill") },
            { TriggerType.Critical, MakeKey("CSM Triggers", "Critical Kill") },
            { TriggerType.Dismemberment, MakeKey("CSM Triggers", "Dismemberment") },
            { TriggerType.Decapitation, MakeKey("CSM Triggers", "Decapitation") },
            { TriggerType.Parry, MakeKey("CSM Triggers", "Parry") },
            { TriggerType.LastEnemy, MakeKey("CSM Triggers", "Last Enemy") },
            { TriggerType.LastStand, MakeKey("CSM Triggers", "Last Stand") }
        };

        private struct CustomValues
        {
            public float Chance;
            public float TimeScale;
            public float Duration;
            public float Cooldown;
            public float Smoothing;
            public float Distribution;
        }

        private CSMModOptionVisibility() { }

        public void Initialize()
        {
            _initialized = false;
            _modData = null;
            _lastIntensityPreset = null;
            _lastChancePreset = null;
            _lastCooldownPreset = null;
            _lastDurationPreset = null;
            _lastSmoothnessPreset = null;
            _lastDistributionPreset = null;
            _lastTriggerProfile = null;
            _lastDebugLogging = false;
            _lastCustomValues.Clear();
            _baseTooltips.Clear();

            TryInitialize();
            if (_initialized)
            {
                if (ApplyAllPresets(true))
                    ModManager.RefreshModOptionsUI();
            }
        }

        public void Shutdown()
        {
        }

        public void Update()
        {
            if (!_initialized)
            {
                TryInitialize();
                if (!_initialized)
                    return;

                if (ApplyAllPresets(true))
                    ModManager.RefreshModOptionsUI();
                return;
            }

            if (ApplyAllPresets(false))
                ModManager.RefreshModOptionsUI();
        }

        private void TryInitialize()
        {
            if (_initialized) return;

            if (!ModManager.TryGetModData(Assembly.GetExecutingAssembly(), out _modData))
                return;

            if (_modData?.modOptions == null || _modData.modOptions.Count == 0)
                return;

            CacheModOptions();
            _initialized = true;
        }

        private void CacheModOptions()
        {
            _modOptionsByKey.Clear();
            if (_modData?.modOptions == null) return;

            foreach (var option in _modData.modOptions)
            {
                if (option == null || string.IsNullOrEmpty(option.name)) continue;
                string key = MakeKey(option.category, option.name);
                _modOptionsByKey[key] = option;
                if (!_baseTooltips.ContainsKey(key))
                    _baseTooltips[key] = option.tooltip ?? string.Empty;
            }
        }

        private bool ApplyAllPresets(bool force)
        {
            bool changed = false;
            bool presetChanged = false;
            bool local;

            local = ApplyIntensityPreset(force);
            changed |= local;
            presetChanged |= local;
            local = ApplyChancePreset(force);
            changed |= local;
            presetChanged |= local;
            local = ApplyCooldownPreset(force);
            changed |= local;
            presetChanged |= local;
            local = ApplyDurationPreset(force);
            changed |= local;
            presetChanged |= local;
            local = ApplySmoothnessPreset(force);
            changed |= local;
            presetChanged |= local;
            local = ApplyDistributionPreset(force);
            changed |= local;
            presetChanged |= local;
            local = ApplyTriggerProfile(force);
            changed |= local;
            presetChanged |= local;
            changed |= ApplyDiagnostics(force);
            changed |= UpdateCustomTooltips(force, presetChanged);
            if (force || presetChanged)
                CaptureCustomValues();
            changed |= ApplyCustomOverrides();
            if (CSMModOptions.DebugLogging && _lastDebugLogging != CSMModOptions.DebugLogging)
            {
                LogMenuState("Debug Enabled");
            }
            if ((force || presetChanged || _lastDebugLogging != CSMModOptions.DebugLogging) && CSMModOptions.DebugLogging)
            {
                LogEffectiveValues();
            }
            _lastDebugLogging = CSMModOptions.DebugLogging;
            return changed;
        }

        private bool ApplyDiagnostics(bool force)
        {
            bool changed = false;

            if (CSMModOptions.QuickTestNow)
            {
                var trigger = CSMModOptions.GetQuickTestTrigger();
                CSMManager.Instance.TriggerSlow(trigger, 0f, null, true);
                CSMModOptions.QuickTestNow = false;
                changed = true;
            }
            return changed;
        }

        private bool ApplyIntensityPreset(bool force)
        {
            string presetValue = NormalizePreset(ref CSMModOptions.CurrentPreset, "Standard");
            if (presetValue == "Balanced")
            {
                CSMModOptions.CurrentPreset = "Standard";
                presetValue = "Standard";
            }
            if (!force && string.Equals(_lastIntensityPreset, presetValue, StringComparison.Ordinal))
                return false;

            var preset = CSMModOptions.GetCurrentPreset();
            foreach (var trigger in TriggerTypes)
            {
                CSMManager.GetPresetValues(preset, trigger, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing);
                SetTimeScale(trigger, timeScale);
                SyncOptionValue(TimeScaleOptionNames, trigger, timeScale);
            }

            _lastIntensityPreset = presetValue;
            LogPresetApply("Intensity Preset", presetValue);
            return true;
        }

        private bool ApplyChancePreset(bool force)
        {
            string presetValue = NormalizePreset(ref CSMModOptions.ChancePresetSetting, "Off");
            if (presetValue == "Chaos" || presetValue == "Always")
            {
                CSMModOptions.ChancePresetSetting = "Off";
                presetValue = "Off";
            }
            if (!force && string.Equals(_lastChancePreset, presetValue, StringComparison.Ordinal))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                CSMManager.GetPresetValues(CSMModOptions.Preset.Standard, trigger, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing);
                CSMModOptions.ApplyChancePreset(ref chance);
                SetChance(trigger, chance);
                SyncOptionValue(ChanceOptionNames, trigger, chance);
            }

            _lastChancePreset = presetValue;
            LogPresetApply("Chance Preset", presetValue);
            return true;
        }

        private bool ApplyCooldownPreset(bool force)
        {
            string presetValue = NormalizePreset(ref CSMModOptions.CooldownPresetSetting, "Standard");
            if (presetValue == "Rare")
            {
                CSMModOptions.CooldownPresetSetting = "Long";
                presetValue = "Long";
            }
            else if (presetValue == "Frequent")
            {
                CSMModOptions.CooldownPresetSetting = "Short";
                presetValue = "Short";
            }
            else if (presetValue == "Chaos")
            {
                CSMModOptions.CooldownPresetSetting = "Short";
                presetValue = "Short";
            }
            else if (presetValue == "Balanced")
            {
                CSMModOptions.CooldownPresetSetting = "Standard";
                presetValue = "Standard";
            }
            if (!force && string.Equals(_lastCooldownPreset, presetValue, StringComparison.Ordinal))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                CSMManager.GetPresetValues(CSMModOptions.Preset.Standard, trigger, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing);
                CSMModOptions.ApplyCooldownPreset(ref cooldown);
                SetCooldown(trigger, cooldown);
                SyncOptionValue(CooldownOptionNames, trigger, cooldown);
            }

            _lastCooldownPreset = presetValue;
            LogPresetApply("Cooldown Preset", presetValue);
            return true;
        }

        private bool ApplyDurationPreset(bool force)
        {
            string presetValue = NormalizePreset(ref CSMModOptions.DurationPresetSetting, "Standard");
            if (presetValue == "Balanced")
            {
                CSMModOptions.DurationPresetSetting = "Standard";
                presetValue = "Standard";
            }
            if (!force && string.Equals(_lastDurationPreset, presetValue, StringComparison.Ordinal))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                CSMManager.GetPresetValues(CSMModOptions.Preset.Standard, trigger, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing);
                CSMModOptions.ApplyDurationPreset(ref duration);
                SetDuration(trigger, duration);
                SyncOptionValue(DurationOptionNames, trigger, duration);
            }

            _lastDurationPreset = presetValue;
            LogPresetApply("Duration Preset", presetValue);
            return true;
        }

        private bool ApplySmoothnessPreset(bool force)
        {
            string presetValue = NormalizePreset(ref CSMModOptions.SmoothnessPresetSetting, "Standard");
            if (presetValue == "Balanced")
            {
                CSMModOptions.SmoothnessPresetSetting = "Standard";
                presetValue = "Standard";
            }
            if (!force && string.Equals(_lastSmoothnessPreset, presetValue, StringComparison.Ordinal))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                CSMManager.GetPresetValues(CSMModOptions.Preset.Standard, trigger, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing);
                CSMModOptions.ApplySmoothnessPreset(ref smoothing);
                SetSmoothing(trigger, smoothing);
                SyncOptionValue(SmoothingOptionNames, trigger, smoothing);
            }

            _lastSmoothnessPreset = presetValue;
            LogPresetApply("Smoothness Preset", presetValue);
            return true;
        }

        private bool ApplyDistributionPreset(bool force)
        {
            string presetValue = NormalizePreset(ref CSMModOptions.CameraDistribution, "First Person Only");
            if (!force && string.Equals(_lastDistributionPreset, presetValue, StringComparison.Ordinal))
                return false;

            float multiplier = CSMModOptions.GetCameraDistributionMultiplier(CSMModOptions.GetCameraDistributionPreset());
            foreach (var trigger in TriggerTypes)
            {
                if (!CSMModOptions.IsThirdPersonEligible(trigger))
                    continue;
                SetDistribution(trigger, multiplier);
                SyncOptionValue(DistributionOptionNames, trigger, multiplier);
            }

            _lastDistributionPreset = presetValue;
            LogPresetApply("Third Person Distribution", presetValue);
            return true;
        }

        private bool ApplyTriggerProfile(bool force)
        {
            string presetValue = NormalizePreset(ref CSMModOptions.TriggerProfile, "All");
            if (!force && string.Equals(_lastTriggerProfile, presetValue, StringComparison.Ordinal))
                return false;

            var profile = CSMModOptions.GetTriggerProfilePreset();
            bool basicKill = true;
            bool critical = true;
            bool dismemberment = true;
            bool decapitation = true;
            bool parry = true;
            bool lastEnemy = true;
            bool lastStand = true;

            switch (profile)
            {
                case CSMModOptions.TriggerProfilePreset.KillsOnly:
                    parry = false;
                    lastStand = false;
                    break;
                case CSMModOptions.TriggerProfilePreset.Highlights:
                    basicKill = false;
                    dismemberment = false;
                    parry = false;
                    lastStand = false;
                    break;
                case CSMModOptions.TriggerProfilePreset.LastEnemyOnly:
                    basicKill = false;
                    critical = false;
                    dismemberment = false;
                    decapitation = false;
                    parry = false;
                    lastStand = false;
                    break;
                case CSMModOptions.TriggerProfilePreset.ParryOnly:
                    basicKill = false;
                    critical = false;
                    dismemberment = false;
                    decapitation = false;
                    lastEnemy = false;
                    lastStand = false;
                    break;
            }

            CSMModOptions.EnableBasicKill = basicKill;
            CSMModOptions.EnableCriticalKill = critical;
            CSMModOptions.EnableDismemberment = dismemberment;
            CSMModOptions.EnableDecapitation = decapitation;
            CSMModOptions.EnableParry = parry;
            CSMModOptions.EnableLastEnemy = lastEnemy;
            CSMModOptions.EnableLastStand = lastStand;

            SyncToggleOption(TriggerToggleOptionNames, TriggerType.BasicKill, basicKill);
            SyncToggleOption(TriggerToggleOptionNames, TriggerType.Critical, critical);
            SyncToggleOption(TriggerToggleOptionNames, TriggerType.Dismemberment, dismemberment);
            SyncToggleOption(TriggerToggleOptionNames, TriggerType.Decapitation, decapitation);
            SyncToggleOption(TriggerToggleOptionNames, TriggerType.Parry, parry);
            SyncToggleOption(TriggerToggleOptionNames, TriggerType.LastEnemy, lastEnemy);
            SyncToggleOption(TriggerToggleOptionNames, TriggerType.LastStand, lastStand);

            _lastTriggerProfile = presetValue;
            if (CSMModOptions.DebugLogging)
            {
                Debug.Log("[CSM] Trigger profile applied: " + presetValue +
                          " | Basic=" + basicKill +
                          " Critical=" + critical +
                          " Dismember=" + dismemberment +
                          " Decap=" + decapitation +
                          " Parry=" + parry +
                          " LastEnemy=" + lastEnemy +
                          " LastStand=" + lastStand);
            }
            return true;
        }

        private static string MakeKey(string category, string name)
        {
            return (category ?? string.Empty) + OptionKeySeparator + (name ?? string.Empty);
        }

        private static string DescribeOption(ModOption option)
        {
            if (option == null) return string.Empty;
            if (string.IsNullOrEmpty(option.category)) return option.name;
            return option.category + " / " + option.name;
        }

        private static string NormalizePreset(ref string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "Custom")
                value = fallback;
            return value;
        }

        private bool SyncOptionValue(Dictionary<TriggerType, string> map, TriggerType type, float value)
        {
            if (!map.TryGetValue(type, out string optionKey))
                return false;
            return SyncOptionValue(optionKey, value);
        }

        private bool SyncToggleOption(Dictionary<TriggerType, string> map, TriggerType type, bool value)
        {
            if (!map.TryGetValue(type, out string optionKey))
                return false;
            return SyncToggleOption(optionKey, value);
        }

        private bool SyncOptionValue(string optionKey, float value)
        {
            if (!_modOptionsByKey.TryGetValue(optionKey, out ModOption option))
            {
                if (CSMModOptions.DebugLogging)
                    Debug.LogWarning("[CSM] Menu sync missing option: " + optionKey);
                return false;
            }

            if (option.parameterValues == null || option.parameterValues.Length == 0)
                option.LoadModOptionParameters();

            if (option.parameterValues == null || option.parameterValues.Length == 0)
            {
                if (CSMModOptions.DebugLogging)
                    Debug.LogWarning("[CSM] Menu sync missing parameters: " + DescribeOption(option));
                return false;
            }

            int index = FindParameterIndex(option.parameterValues, value);
            if (index < 0)
            {
                if (CSMModOptions.DebugLogging)
                    Debug.LogWarning("[CSM] Menu sync no parameter match: " + DescribeOption(option) + " value=" + value);
                return false;
            }

            if (option.currentValueIndex == index)
                return false;

            option.Apply(index);
            option.RefreshUI();
            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Menu sync updated: " + DescribeOption(option) + " -> " + value);
            return true;
        }

        private bool SyncToggleOption(string optionKey, bool value)
        {
            if (!_modOptionsByKey.TryGetValue(optionKey, out ModOption option))
            {
                if (CSMModOptions.DebugLogging)
                    Debug.LogWarning("[CSM] Menu sync missing option: " + optionKey);
                return false;
            }

            if (option.parameterValues == null || option.parameterValues.Length == 0)
                option.LoadModOptionParameters();

            int index = value ? 1 : 0;
            if (option.currentValueIndex == index)
                return false;

            option.Apply(index);
            option.RefreshUI();
            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Menu toggle updated: " + DescribeOption(option) + " -> " + (value ? "Enabled" : "Disabled"));
            return true;
        }

        private static int FindParameterIndex(ModOptionParameter[] parameters, float value)
        {
            if (parameters == null) return -1;

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramValue = parameters[i]?.value;
                if (paramValue is float fValue)
                {
                    if (Mathf.Abs(fValue - value) < 0.0001f)
                        return i;
                }
                else if (paramValue is double dValue)
                {
                    if (Mathf.Abs((float)dValue - value) < 0.0001f)
                        return i;
                }
                else if (paramValue is int iValue)
                {
                    if (Mathf.Abs(iValue - value) < 0.0001f)
                        return i;
                }
            }

            return -1;
        }

        private static string BuildEffectiveSummary(TriggerType type)
        {
            if (!IsTriggerEnabled(type))
                return "Disabled";

            float chance;
            float timeScale;
            float duration;
            float cooldown;
            float smoothing;

            switch (type)
            {
                case TriggerType.BasicKill:
                    chance = CSMModOptions.BasicKillChance;
                    timeScale = CSMModOptions.BasicKillTimeScale;
                    duration = CSMModOptions.BasicKillDuration;
                    cooldown = CSMModOptions.BasicKillCooldown;
                    smoothing = CSMModOptions.BasicKillSmoothing;
                    break;
                case TriggerType.Critical:
                    chance = CSMModOptions.CriticalKillChance;
                    timeScale = CSMModOptions.CriticalKillTimeScale;
                    duration = CSMModOptions.CriticalKillDuration;
                    cooldown = CSMModOptions.CriticalKillCooldown;
                    smoothing = CSMModOptions.CriticalKillSmoothing;
                    break;
                case TriggerType.Dismemberment:
                    chance = CSMModOptions.DismembermentChance;
                    timeScale = CSMModOptions.DismembermentTimeScale;
                    duration = CSMModOptions.DismembermentDuration;
                    cooldown = CSMModOptions.DismembermentCooldown;
                    smoothing = CSMModOptions.DismembermentSmoothing;
                    break;
                case TriggerType.Decapitation:
                    chance = CSMModOptions.DecapitationChance;
                    timeScale = CSMModOptions.DecapitationTimeScale;
                    duration = CSMModOptions.DecapitationDuration;
                    cooldown = CSMModOptions.DecapitationCooldown;
                    smoothing = CSMModOptions.DecapitationSmoothing;
                    break;
                case TriggerType.Parry:
                    chance = CSMModOptions.ParryChance;
                    timeScale = CSMModOptions.ParryTimeScale;
                    duration = CSMModOptions.ParryDuration;
                    cooldown = CSMModOptions.ParryCooldown;
                    smoothing = CSMModOptions.ParrySmoothing;
                    break;
                case TriggerType.LastEnemy:
                    chance = CSMModOptions.LastEnemyChance;
                    timeScale = CSMModOptions.LastEnemyTimeScale;
                    duration = CSMModOptions.LastEnemyDuration;
                    cooldown = CSMModOptions.LastEnemyCooldown;
                    smoothing = CSMModOptions.LastEnemySmoothing;
                    break;
                case TriggerType.LastStand:
                    chance = 1.0f;
                    timeScale = CSMModOptions.LastStandTimeScale;
                    duration = CSMModOptions.LastStandDuration;
                    cooldown = CSMModOptions.LastStandCooldown;
                    smoothing = CSMModOptions.LastStandSmoothing;
                    break;
                default:
                    return "Disabled";
            }

            if (CSMModOptions.GlobalSmoothing >= 0f)
                smoothing = CSMModOptions.GlobalSmoothing;

            string chanceLabel = (chance * 100f).ToString("F0") + "%";
            string scaleLabel = (timeScale * 100f).ToString("F0") + "%";
            string durationLabel = duration.ToString("F1") + "s";
            string cooldownLabel = cooldown.ToString("F1") + "s";
            string smoothingLabel = smoothing <= 0f ? "Instant" : smoothing.ToString("0.#") + "x";

            string tpLabel;
            if (!CSMModOptions.IsThirdPersonEligible(type))
            {
                tpLabel = "N/A";
            }
            else
            {
                float dist = CSMModOptions.GetThirdPersonDistribution(type);
                if (dist <= 0f)
                    tpLabel = "Off";
                else if (dist >= 99f)
                    tpLabel = "Always";
                else
                    tpLabel = dist.ToString("0.#") + "x";
            }

            return "Chance " + chanceLabel +
                   " | Scale " + scaleLabel +
                   " | Dur " + durationLabel +
                   " | CD " + cooldownLabel +
                   " | Smooth " + smoothingLabel +
                   " | TP " + tpLabel;
        }

        private static string GetTriggerUiName(TriggerType type)
        {
            switch (type)
            {
                case TriggerType.BasicKill: return "Basic Kill";
                case TriggerType.Critical: return "Critical Kill";
                case TriggerType.Dismemberment: return "Dismemberment";
                case TriggerType.Decapitation: return "Decapitation";
                case TriggerType.Parry: return "Parry";
                case TriggerType.LastEnemy: return "Last Enemy";
                case TriggerType.LastStand: return "Last Stand";
                default: return "Unknown";
            }
        }

        private void CaptureCustomValues()
        {
            foreach (var trigger in TriggerTypes)
            {
                _lastCustomValues[trigger] = ReadCustomValues(trigger);
            }
        }

        private bool ApplyCustomOverrides()
        {
            bool changed = false;
            foreach (var trigger in TriggerTypes)
            {
                CustomValues current = ReadCustomValues(trigger);
                if (!_lastCustomValues.TryGetValue(trigger, out CustomValues last))
                {
                    _lastCustomValues[trigger] = current;
                    continue;
                }

                if (!CustomValuesChanged(last, current))
                    continue;

                _lastCustomValues[trigger] = current;
                if (!IsTriggerEnabled(trigger))
                {
                    SetTriggerEnabled(trigger, true);
                    changed |= SyncToggleOption(TriggerToggleOptionNames, trigger, true);
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Custom override re-enabled trigger: " + GetTriggerUiName(trigger));
                }
            }

            return changed;
        }

        private static bool CustomValuesChanged(CustomValues a, CustomValues b)
        {
            const float epsilon = 0.0001f;
            if (Mathf.Abs(a.Chance - b.Chance) > epsilon) return true;
            if (Mathf.Abs(a.TimeScale - b.TimeScale) > epsilon) return true;
            if (Mathf.Abs(a.Duration - b.Duration) > epsilon) return true;
            if (Mathf.Abs(a.Cooldown - b.Cooldown) > epsilon) return true;
            if (Mathf.Abs(a.Smoothing - b.Smoothing) > epsilon) return true;
            if (Mathf.Abs(a.Distribution - b.Distribution) > epsilon) return true;
            return false;
        }

        private static CustomValues ReadCustomValues(TriggerType type)
        {
            var values = new CustomValues();
            switch (type)
            {
                case TriggerType.BasicKill:
                    values.Chance = CSMModOptions.BasicKillChance;
                    values.TimeScale = CSMModOptions.BasicKillTimeScale;
                    values.Duration = CSMModOptions.BasicKillDuration;
                    values.Cooldown = CSMModOptions.BasicKillCooldown;
                    values.Smoothing = CSMModOptions.BasicKillSmoothing;
                    values.Distribution = CSMModOptions.BasicKillThirdPersonDistribution;
                    break;
                case TriggerType.Critical:
                    values.Chance = CSMModOptions.CriticalKillChance;
                    values.TimeScale = CSMModOptions.CriticalKillTimeScale;
                    values.Duration = CSMModOptions.CriticalKillDuration;
                    values.Cooldown = CSMModOptions.CriticalKillCooldown;
                    values.Smoothing = CSMModOptions.CriticalKillSmoothing;
                    values.Distribution = CSMModOptions.CriticalKillThirdPersonDistribution;
                    break;
                case TriggerType.Dismemberment:
                    values.Chance = CSMModOptions.DismembermentChance;
                    values.TimeScale = CSMModOptions.DismembermentTimeScale;
                    values.Duration = CSMModOptions.DismembermentDuration;
                    values.Cooldown = CSMModOptions.DismembermentCooldown;
                    values.Smoothing = CSMModOptions.DismembermentSmoothing;
                    values.Distribution = CSMModOptions.DismembermentThirdPersonDistribution;
                    break;
                case TriggerType.Decapitation:
                    values.Chance = CSMModOptions.DecapitationChance;
                    values.TimeScale = CSMModOptions.DecapitationTimeScale;
                    values.Duration = CSMModOptions.DecapitationDuration;
                    values.Cooldown = CSMModOptions.DecapitationCooldown;
                    values.Smoothing = CSMModOptions.DecapitationSmoothing;
                    values.Distribution = CSMModOptions.DecapitationThirdPersonDistribution;
                    break;
                case TriggerType.Parry:
                    values.Chance = CSMModOptions.ParryChance;
                    values.TimeScale = CSMModOptions.ParryTimeScale;
                    values.Duration = CSMModOptions.ParryDuration;
                    values.Cooldown = CSMModOptions.ParryCooldown;
                    values.Smoothing = CSMModOptions.ParrySmoothing;
                    values.Distribution = 0f;
                    break;
                case TriggerType.LastEnemy:
                    values.Chance = CSMModOptions.LastEnemyChance;
                    values.TimeScale = CSMModOptions.LastEnemyTimeScale;
                    values.Duration = CSMModOptions.LastEnemyDuration;
                    values.Cooldown = CSMModOptions.LastEnemyCooldown;
                    values.Smoothing = CSMModOptions.LastEnemySmoothing;
                    values.Distribution = CSMModOptions.LastEnemyThirdPersonDistribution;
                    break;
                case TriggerType.LastStand:
                    values.Chance = 1f;
                    values.TimeScale = CSMModOptions.LastStandTimeScale;
                    values.Duration = CSMModOptions.LastStandDuration;
                    values.Cooldown = CSMModOptions.LastStandCooldown;
                    values.Smoothing = CSMModOptions.LastStandSmoothing;
                    values.Distribution = 0f;
                    break;
            }
            return values;
        }

        private bool UpdateCustomTooltips(bool force, bool presetChanged)
        {
            bool debugLogging = CSMModOptions.DebugLogging;
            if (!force && !presetChanged && debugLogging == _lastDebugLogging)
                return false;

            bool changed = false;
            foreach (var trigger in TriggerTypes)
            {
                string summary = debugLogging ? BuildEffectiveSummary(trigger) : null;
                changed |= UpdateTooltip(ChanceOptionNames, trigger, summary);
                changed |= UpdateTooltip(TimeScaleOptionNames, trigger, summary);
                changed |= UpdateTooltip(DurationOptionNames, trigger, summary);
                changed |= UpdateTooltip(CooldownOptionNames, trigger, summary);
                changed |= UpdateTooltip(SmoothingOptionNames, trigger, summary);
                changed |= UpdateTooltip(DistributionOptionNames, trigger, summary);
            }

            return changed;
        }

        private bool UpdateTooltip(Dictionary<TriggerType, string> map, TriggerType type, string effectiveSummary)
        {
            if (!map.TryGetValue(type, out string optionKey))
                return false;

            return UpdateTooltip(optionKey, effectiveSummary);
        }

        private bool UpdateTooltip(string optionKey, string effectiveSummary)
        {
            if (!_modOptionsByKey.TryGetValue(optionKey, out ModOption option))
                return false;

            if (!_baseTooltips.TryGetValue(optionKey, out string baseTooltip))
                baseTooltip = option.tooltip ?? string.Empty;

            string newTooltip = baseTooltip ?? string.Empty;
            if (!string.IsNullOrEmpty(effectiveSummary))
            {
                if (string.IsNullOrEmpty(newTooltip))
                    newTooltip = "Effective: " + effectiveSummary;
                else
                    newTooltip = newTooltip.TrimEnd() + " | Effective: " + effectiveSummary;
            }

            if (string.Equals(option.tooltip ?? string.Empty, newTooltip, StringComparison.Ordinal))
                return false;

            option.tooltip = newTooltip;
            option.RefreshUI();
            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Menu tooltip updated: " + DescribeOption(option));
            return true;
        }

        private void LogPresetApply(string label, string value)
        {
            if (!CSMModOptions.DebugLogging)
                return;

            Debug.Log("[CSM] Menu preset applied: " + label + " = " + value);
        }

        private void LogMenuState(string context)
        {
            Debug.Log("[CSM] Menu state (" + context + "): " +
                      "Intensity=" + CSMModOptions.CurrentPreset +
                      " | Chance=" + CSMModOptions.ChancePresetSetting +
                      " | Cooldown=" + CSMModOptions.CooldownPresetSetting +
                      " | Duration=" + CSMModOptions.DurationPresetSetting +
                      " | Smoothness=" + CSMModOptions.SmoothnessPresetSetting +
                      " | ThirdPerson=" + CSMModOptions.CameraDistribution +
                      " | TriggerProfile=" + CSMModOptions.TriggerProfile);
            Debug.Log("[CSM] Menu triggers: " +
                      "Basic=" + CSMModOptions.EnableBasicKill +
                      " Critical=" + CSMModOptions.EnableCriticalKill +
                      " Dismember=" + CSMModOptions.EnableDismemberment +
                      " Decap=" + CSMModOptions.EnableDecapitation +
                      " Parry=" + CSMModOptions.EnableParry +
                      " LastEnemy=" + CSMModOptions.EnableLastEnemy +
                      " LastStand=" + CSMModOptions.EnableLastStand);
        }

        private void LogEffectiveValues()
        {
            Debug.Log("[CSM] Effective Values:");
            foreach (var trigger in TriggerTypes)
            {
                Debug.Log("[CSM] " + GetTriggerUiName(trigger) + " -> " + BuildEffectiveSummary(trigger));
            }
        }

        private static void SetChance(TriggerType type, float value)
        {
            switch (type)
            {
                case TriggerType.BasicKill: CSMModOptions.BasicKillChance = value; break;
                case TriggerType.Critical: CSMModOptions.CriticalKillChance = value; break;
                case TriggerType.Dismemberment: CSMModOptions.DismembermentChance = value; break;
                case TriggerType.Decapitation: CSMModOptions.DecapitationChance = value; break;
                case TriggerType.Parry: CSMModOptions.ParryChance = value; break;
                case TriggerType.LastEnemy: CSMModOptions.LastEnemyChance = value; break;
            }
        }

        private static void SetTimeScale(TriggerType type, float value)
        {
            switch (type)
            {
                case TriggerType.BasicKill: CSMModOptions.BasicKillTimeScale = value; break;
                case TriggerType.Critical: CSMModOptions.CriticalKillTimeScale = value; break;
                case TriggerType.Dismemberment: CSMModOptions.DismembermentTimeScale = value; break;
                case TriggerType.Decapitation: CSMModOptions.DecapitationTimeScale = value; break;
                case TriggerType.Parry: CSMModOptions.ParryTimeScale = value; break;
                case TriggerType.LastEnemy: CSMModOptions.LastEnemyTimeScale = value; break;
                case TriggerType.LastStand: CSMModOptions.LastStandTimeScale = value; break;
            }
        }

        private static void SetDuration(TriggerType type, float value)
        {
            switch (type)
            {
                case TriggerType.BasicKill: CSMModOptions.BasicKillDuration = value; break;
                case TriggerType.Critical: CSMModOptions.CriticalKillDuration = value; break;
                case TriggerType.Dismemberment: CSMModOptions.DismembermentDuration = value; break;
                case TriggerType.Decapitation: CSMModOptions.DecapitationDuration = value; break;
                case TriggerType.Parry: CSMModOptions.ParryDuration = value; break;
                case TriggerType.LastEnemy: CSMModOptions.LastEnemyDuration = value; break;
                case TriggerType.LastStand: CSMModOptions.LastStandDuration = value; break;
            }
        }

        private static void SetCooldown(TriggerType type, float value)
        {
            switch (type)
            {
                case TriggerType.BasicKill: CSMModOptions.BasicKillCooldown = value; break;
                case TriggerType.Critical: CSMModOptions.CriticalKillCooldown = value; break;
                case TriggerType.Dismemberment: CSMModOptions.DismembermentCooldown = value; break;
                case TriggerType.Decapitation: CSMModOptions.DecapitationCooldown = value; break;
                case TriggerType.Parry: CSMModOptions.ParryCooldown = value; break;
                case TriggerType.LastEnemy: CSMModOptions.LastEnemyCooldown = value; break;
                case TriggerType.LastStand: CSMModOptions.LastStandCooldown = value; break;
            }
        }

        private static void SetSmoothing(TriggerType type, float value)
        {
            switch (type)
            {
                case TriggerType.BasicKill: CSMModOptions.BasicKillSmoothing = value; break;
                case TriggerType.Critical: CSMModOptions.CriticalKillSmoothing = value; break;
                case TriggerType.Dismemberment: CSMModOptions.DismembermentSmoothing = value; break;
                case TriggerType.Decapitation: CSMModOptions.DecapitationSmoothing = value; break;
                case TriggerType.Parry: CSMModOptions.ParrySmoothing = value; break;
                case TriggerType.LastEnemy: CSMModOptions.LastEnemySmoothing = value; break;
                case TriggerType.LastStand: CSMModOptions.LastStandSmoothing = value; break;
            }
        }

        private static void SetDistribution(TriggerType type, float value)
        {
            switch (type)
            {
                case TriggerType.BasicKill: CSMModOptions.BasicKillThirdPersonDistribution = value; break;
                case TriggerType.Critical: CSMModOptions.CriticalKillThirdPersonDistribution = value; break;
                case TriggerType.Dismemberment: CSMModOptions.DismembermentThirdPersonDistribution = value; break;
                case TriggerType.Decapitation: CSMModOptions.DecapitationThirdPersonDistribution = value; break;
                case TriggerType.LastEnemy: CSMModOptions.LastEnemyThirdPersonDistribution = value; break;
            }
        }

        private static void SetTriggerEnabled(TriggerType type, bool value)
        {
            switch (type)
            {
                case TriggerType.BasicKill: CSMModOptions.EnableBasicKill = value; break;
                case TriggerType.Critical: CSMModOptions.EnableCriticalKill = value; break;
                case TriggerType.Dismemberment: CSMModOptions.EnableDismemberment = value; break;
                case TriggerType.Decapitation: CSMModOptions.EnableDecapitation = value; break;
                case TriggerType.Parry: CSMModOptions.EnableParry = value; break;
                case TriggerType.LastEnemy: CSMModOptions.EnableLastEnemy = value; break;
                case TriggerType.LastStand: CSMModOptions.EnableLastStand = value; break;
            }
        }

        private static bool IsTriggerEnabled(TriggerType type)
        {
            switch (type)
            {
                case TriggerType.BasicKill: return CSMModOptions.EnableBasicKill;
                case TriggerType.Critical: return CSMModOptions.EnableCriticalKill;
                case TriggerType.Dismemberment: return CSMModOptions.EnableDismemberment;
                case TriggerType.Decapitation: return CSMModOptions.EnableDecapitation;
                case TriggerType.Parry: return CSMModOptions.EnableParry;
                case TriggerType.LastEnemy: return CSMModOptions.EnableLastEnemy;
                case TriggerType.LastStand: return CSMModOptions.EnableLastStand;
                default: return false;
            }
        }
    }
}
