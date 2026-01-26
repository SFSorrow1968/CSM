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
        private CSMModOptions.Preset? _lastIntensityPreset;
        private CSMModOptions.ChancePreset? _lastChancePreset;
        private CSMModOptions.CooldownPreset? _lastCooldownPreset;
        private CSMModOptions.DurationPreset? _lastDurationPreset;
        private CSMModOptions.SmoothnessPreset? _lastSmoothnessPreset;
        private CSMModOptions.CameraDistributionPreset? _lastDistributionPreset;
        private CSMModOptions.TriggerProfilePreset? _lastTriggerProfile;
        private bool _lastDebugLogging;
        private readonly Dictionary<TriggerType, CSMModOptions.TriggerCustomValues> _lastCustomValues =
            new Dictionary<TriggerType, CSMModOptions.TriggerCustomValues>();
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
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionChance) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionChance) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionChance) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionChance) },
            { TriggerType.Parry, MakeKey(CSMModOptions.CategoryCustomParry, CSMModOptions.OptionChance) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionChance) }
        };

        private static readonly Dictionary<TriggerType, string> TimeScaleOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionTimeScale) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionTimeScale) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionTimeScale) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionTimeScale) },
            { TriggerType.Parry, MakeKey(CSMModOptions.CategoryCustomParry, CSMModOptions.OptionTimeScale) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionTimeScale) },
            { TriggerType.LastStand, MakeKey(CSMModOptions.CategoryCustomLastStand, CSMModOptions.OptionTimeScale) }
        };

        private static readonly Dictionary<TriggerType, string> DurationOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionDuration) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionDuration) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionDuration) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionDuration) },
            { TriggerType.Parry, MakeKey(CSMModOptions.CategoryCustomParry, CSMModOptions.OptionDuration) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionDuration) },
            { TriggerType.LastStand, MakeKey(CSMModOptions.CategoryCustomLastStand, CSMModOptions.OptionDuration) }
        };

        private static readonly Dictionary<TriggerType, string> CooldownOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionCooldown) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionCooldown) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionCooldown) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionCooldown) },
            { TriggerType.Parry, MakeKey(CSMModOptions.CategoryCustomParry, CSMModOptions.OptionCooldown) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionCooldown) },
            { TriggerType.LastStand, MakeKey(CSMModOptions.CategoryCustomLastStand, CSMModOptions.OptionCooldown) }
        };

        private static readonly Dictionary<TriggerType, string> SmoothingOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionSmoothing) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionSmoothing) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionSmoothing) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionSmoothing) },
            { TriggerType.Parry, MakeKey(CSMModOptions.CategoryCustomParry, CSMModOptions.OptionSmoothing) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionSmoothing) },
            { TriggerType.LastStand, MakeKey(CSMModOptions.CategoryCustomLastStand, CSMModOptions.OptionSmoothing) }
        };

        private static readonly Dictionary<TriggerType, string> DistributionOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionThirdPersonDistribution) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionThirdPersonDistribution) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionThirdPersonDistribution) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionThirdPersonDistribution) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionThirdPersonDistribution) }
        };

        private static readonly Dictionary<TriggerType, string> TriggerToggleOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryTriggers, CSMModOptions.TriggerBasicKill) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryTriggers, CSMModOptions.TriggerCriticalKill) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryTriggers, CSMModOptions.TriggerDismemberment) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryTriggers, CSMModOptions.TriggerDecapitation) },
            { TriggerType.Parry, MakeKey(CSMModOptions.CategoryTriggers, CSMModOptions.TriggerParry) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryTriggers, CSMModOptions.TriggerLastEnemy) },
            { TriggerType.LastStand, MakeKey(CSMModOptions.CategoryTriggers, CSMModOptions.TriggerLastStand) }
        };
        private static readonly string ThrownImpactOptionKey = MakeKey(CSMModOptions.CategoryTriggers, CSMModOptions.TriggerThrownImpactKill);

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
            changed |= ApplyTriggerDependencies();
            changed |= ApplyDiagnostics(force);
            changed |= UpdateCustomTooltips(force, presetChanged);
            if (force || presetChanged)
                CaptureCustomValues();
            changed |= ApplyCustomOverrides();
            if (CSMModOptions.DebugLogging && _lastDebugLogging != CSMModOptions.DebugLogging)
            {
                LogMenuState("Debug Enabled");
            }
            if (CSMModOptions.DebugLogging && presetChanged)
            {
                LogMenuState("Preset Changed");
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

        private delegate float PresetValueSelector(float chance, float timeScale, float duration, float cooldown, float smoothing);
        private delegate void RefFloatAction(ref float value);

        private static string ResolvePresetLabel(string settingValue, object preset)
        {
            return string.IsNullOrWhiteSpace(settingValue) ? preset.ToString() : settingValue;
        }

        private bool ApplyPresetFromStandard<TPreset>(bool force, TPreset currentPreset, ref TPreset? lastPreset, string label,
            string settingValue, PresetValueSelector selector, RefFloatAction applyPreset, CSMModOptions.TriggerField field,
            Dictionary<TriggerType, string> optionMap) where TPreset : struct
        {
            if (!force && lastPreset.HasValue && EqualityComparer<TPreset>.Default.Equals(lastPreset.Value, currentPreset))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                CSMManager.GetPresetValues(CSMModOptions.Preset.Standard, trigger, out float chance, out float timeScale,
                    out float duration, out float cooldown, out float smoothing);
                float value = selector(chance, timeScale, duration, cooldown, smoothing);
                applyPreset(ref value);
                CSMModOptions.SetTriggerValue(trigger, field, value);
                SyncOptionValue(optionMap, trigger, value);
            }

            lastPreset = currentPreset;
            LogPresetApply(label, ResolvePresetLabel(settingValue, currentPreset));
            return true;
        }

        private bool ApplyIntensityPreset(bool force)
        {
            var preset = CSMModOptions.GetCurrentPreset();
            if (!force && _lastIntensityPreset.HasValue && _lastIntensityPreset.Value.Equals(preset))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                CSMManager.GetPresetValues(preset, trigger, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing);
                CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.TimeScale, timeScale);
                SyncOptionValue(TimeScaleOptionNames, trigger, timeScale);
            }

            _lastIntensityPreset = preset;
            LogPresetApply("Intensity Preset", ResolvePresetLabel(CSMModOptions.CurrentPreset, preset));
            return true;
        }

        private bool ApplyChancePreset(bool force)
        {
            var preset = CSMModOptions.GetChancePreset();
            return ApplyPresetFromStandard(force, preset, ref _lastChancePreset, "Chance Preset",
                CSMModOptions.ChancePresetSetting, (chance, timeScale, duration, cooldown, smoothing) => chance,
                CSMModOptions.ApplyChancePreset, CSMModOptions.TriggerField.Chance, ChanceOptionNames);
        }

        private bool ApplyCooldownPreset(bool force)
        {
            var preset = CSMModOptions.GetCooldownPreset();
            return ApplyPresetFromStandard(force, preset, ref _lastCooldownPreset, "Cooldown Preset",
                CSMModOptions.CooldownPresetSetting, (chance, timeScale, duration, cooldown, smoothing) => cooldown,
                CSMModOptions.ApplyCooldownPreset, CSMModOptions.TriggerField.Cooldown, CooldownOptionNames);
        }

        private bool ApplyDurationPreset(bool force)
        {
            var preset = CSMModOptions.GetDurationPreset();
            return ApplyPresetFromStandard(force, preset, ref _lastDurationPreset, "Duration Preset",
                CSMModOptions.DurationPresetSetting, (chance, timeScale, duration, cooldown, smoothing) => duration,
                CSMModOptions.ApplyDurationPreset, CSMModOptions.TriggerField.Duration, DurationOptionNames);
        }

        private bool ApplySmoothnessPreset(bool force)
        {
            var preset = CSMModOptions.GetSmoothnessPreset();
            return ApplyPresetFromStandard(force, preset, ref _lastSmoothnessPreset, "Smoothness Preset",
                CSMModOptions.SmoothnessPresetSetting, (chance, timeScale, duration, cooldown, smoothing) => smoothing,
                CSMModOptions.ApplySmoothnessPreset, CSMModOptions.TriggerField.Smoothing, SmoothingOptionNames);
        }

        private bool ApplyDistributionPreset(bool force)
        {
            var preset = CSMModOptions.GetCameraDistributionPreset();
            if (!force && _lastDistributionPreset.HasValue && _lastDistributionPreset.Value.Equals(preset))
                return false;

            float multiplier = CSMModOptions.GetCameraDistributionMultiplier(preset);
            foreach (var trigger in TriggerTypes)
            {
                if (!CSMModOptions.IsThirdPersonEligible(trigger))
                    continue;
                CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.Distribution, multiplier);
                SyncOptionValue(DistributionOptionNames, trigger, multiplier);
            }

            _lastDistributionPreset = preset;
            LogPresetApply("Third Person Distribution", ResolvePresetLabel(CSMModOptions.CameraDistribution, preset));
            return true;
        }

        private bool ApplyTriggerProfile(bool force)
        {
            var profile = CSMModOptions.GetTriggerProfilePreset();
            if (!force && _lastTriggerProfile.HasValue && _lastTriggerProfile.Value.Equals(profile))
                return false;

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

            CSMModOptions.SetTriggerEnabled(TriggerType.BasicKill, basicKill);
            CSMModOptions.SetTriggerEnabled(TriggerType.Critical, critical);
            CSMModOptions.SetTriggerEnabled(TriggerType.Dismemberment, dismemberment);
            CSMModOptions.SetTriggerEnabled(TriggerType.Decapitation, decapitation);
            CSMModOptions.SetTriggerEnabled(TriggerType.Parry, parry);
            CSMModOptions.SetTriggerEnabled(TriggerType.LastEnemy, lastEnemy);
            CSMModOptions.SetTriggerEnabled(TriggerType.LastStand, lastStand);

            SyncToggleOption(TriggerToggleOptionNames, TriggerType.BasicKill, basicKill);
            SyncToggleOption(TriggerToggleOptionNames, TriggerType.Critical, critical);
            SyncToggleOption(TriggerToggleOptionNames, TriggerType.Dismemberment, dismemberment);
            SyncToggleOption(TriggerToggleOptionNames, TriggerType.Decapitation, decapitation);
            SyncToggleOption(TriggerToggleOptionNames, TriggerType.Parry, parry);
            SyncToggleOption(TriggerToggleOptionNames, TriggerType.LastEnemy, lastEnemy);
            SyncToggleOption(TriggerToggleOptionNames, TriggerType.LastStand, lastStand);
            if (!basicKill && CSMModOptions.EnableThrownImpactKill)
            {
                CSMModOptions.EnableThrownImpactKill = false;
                SyncToggleOption(ThrownImpactOptionKey, false);
            }

            _lastTriggerProfile = profile;
            if (CSMModOptions.DebugLogging)
            {
                Debug.Log("[CSM] Trigger profile applied: " + ResolvePresetLabel(CSMModOptions.TriggerProfile, profile) +
                          " | Basic=" + basicKill +
                          " ThrownImpact=" + CSMModOptions.EnableThrownImpactKill +
                          " Critical=" + critical +
                          " Dismember=" + dismemberment +
                          " Decap=" + decapitation +
                          " Parry=" + parry +
                          " LastEnemy=" + lastEnemy +
                          " LastStand=" + lastStand);
            }
            return true;
        }

        private bool ApplyTriggerDependencies()
        {
            if (CSMModOptions.EnableBasicKill || !CSMModOptions.EnableThrownImpactKill)
                return false;

            CSMModOptions.EnableThrownImpactKill = false;
            SyncToggleOption(ThrownImpactOptionKey, false);
            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Thrown Impact Kill disabled because Basic Kill is off");
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
            if (!CSMModOptions.IsTriggerEnabled(type))
                return "Disabled";

            var values = CSMModOptions.GetCustomValues(type);
            float smoothing = values.Smoothing;

            if (CSMModOptions.GlobalSmoothing >= 0f)
                smoothing = CSMModOptions.GlobalSmoothing;

            return BuildSummary(type, values, smoothing);
        }

        private static string BuildRawSummary(TriggerType type)
        {
            var values = CSMModOptions.GetCustomValues(type);
            return BuildSummary(type, values, values.Smoothing);
        }

        private static string BuildSummary(TriggerType type, CSMModOptions.TriggerCustomValues values, float smoothing)
        {
            string chanceLabel = (values.Chance * 100f).ToString("F0") + "%";
            string scaleLabel = (values.TimeScale * 100f).ToString("F0") + "%";
            string durationLabel = values.Duration.ToString("F1") + "s";
            string cooldownLabel = values.Cooldown.ToString("F1") + "s";
            string smoothingLabel = smoothing <= 0f ? "Instant" : smoothing.ToString("0.#") + "x";

            string tpLabel;
            if (!CSMModOptions.IsThirdPersonEligible(type))
            {
                tpLabel = "N/A";
            }
            else
            {
                float dist = values.Distribution;
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
                case TriggerType.BasicKill: return CSMModOptions.TriggerBasicKill;
                case TriggerType.Critical: return CSMModOptions.TriggerCriticalKill;
                case TriggerType.Dismemberment: return CSMModOptions.TriggerDismemberment;
                case TriggerType.Decapitation: return CSMModOptions.TriggerDecapitation;
                case TriggerType.Parry: return CSMModOptions.TriggerParry;
                case TriggerType.LastEnemy: return CSMModOptions.TriggerLastEnemy;
                case TriggerType.LastStand: return CSMModOptions.TriggerLastStand;
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
                CSMModOptions.TriggerCustomValues current = ReadCustomValues(trigger);
                if (!_lastCustomValues.TryGetValue(trigger, out CSMModOptions.TriggerCustomValues last))
                {
                    _lastCustomValues[trigger] = current;
                    continue;
                }

                if (!CustomValuesChanged(last, current))
                    continue;

                _lastCustomValues[trigger] = current;
                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Custom override changed: " + GetTriggerUiName(trigger) + " " + BuildSummary(trigger, last, last.Smoothing) + " -> " + BuildSummary(trigger, current, current.Smoothing));
                if (!CSMModOptions.IsTriggerEnabled(trigger))
                {
                    CSMModOptions.SetTriggerEnabled(trigger, true);
                    changed |= SyncToggleOption(TriggerToggleOptionNames, trigger, true);
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Custom override re-enabled trigger: " + GetTriggerUiName(trigger));
                }
            }

            return changed;
        }

        private static bool CustomValuesChanged(CSMModOptions.TriggerCustomValues a, CSMModOptions.TriggerCustomValues b)
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

        private static CSMModOptions.TriggerCustomValues ReadCustomValues(TriggerType type)
        {
            return CSMModOptions.GetCustomValues(type);
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
                      " | Duration=" + CSMModOptions.DurationPresetSetting + " (x" + CSMModOptions.GetDurationMultiplier().ToString("0.##") + ")" +
                      " | Smoothness=" + CSMModOptions.SmoothnessPresetSetting + " (x" + CSMModOptions.GetSmoothnessMultiplier().ToString("0.##") + ")" +
                      " | ThirdPerson=" + CSMModOptions.CameraDistribution +
                      " | TriggerProfile=" + CSMModOptions.TriggerProfile);
            string globalSmoothing = CSMModOptions.GlobalSmoothing < 0f ? "Per Trigger" : CSMModOptions.GlobalSmoothing.ToString("0.##");
            Debug.Log("[CSM] Menu overrides: " +
                      "GlobalCooldown=" + CSMModOptions.GlobalCooldown.ToString("0.##") +
                      " | GlobalSmoothing=" + globalSmoothing +
                      " | DynamicIntensity=" + CSMModOptions.DynamicIntensitySetting +
                      " | Haptic=" + CSMModOptions.HapticIntensity.ToString("0.##"));
            Debug.Log("[CSM] Menu triggers: " +
                      "Basic=" + CSMModOptions.EnableBasicKill +
                      " ThrownImpact=" + CSMModOptions.EnableThrownImpactKill +
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
                Debug.Log("[CSM] " + GetTriggerUiName(trigger) + " raw -> " + BuildRawSummary(trigger) + " | effective -> " + BuildEffectiveSummary(trigger));
            }
        }

    }
}
