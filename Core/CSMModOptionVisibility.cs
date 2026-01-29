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
        private CSMModOptions.DelayPreset? _lastDelayInPreset;
        private CSMModOptions.CameraDistributionPreset? _lastDistributionPreset;
        private CSMModOptions.TriggerProfilePreset? _lastTriggerProfile;
        private bool _lastDebugLogging;
        private bool _lastResetStats;
        private readonly Dictionary<TriggerType, CSMModOptions.TriggerCustomValues> _lastCustomValues =
            new Dictionary<TriggerType, CSMModOptions.TriggerCustomValues>();
        private readonly Dictionary<TriggerType, CSMModOptions.TriggerCustomValues> _expectedPresetValues =
            new Dictionary<TriggerType, CSMModOptions.TriggerCustomValues>();
        private float _presetAppliedTime;
        private const float PresetLockDuration = 30f; // Seconds to protect preset values from UI corruption
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
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionBasicChance) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionCriticalChance) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionDismemberChance) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionDecapChance) },
            { TriggerType.Parry, MakeKey(CSMModOptions.CategoryCustomParry, CSMModOptions.OptionParryChance) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionLastEnemyChance) }
        };

        private static readonly Dictionary<TriggerType, string> TimeScaleOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionBasicTimeScale) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionCriticalTimeScale) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionDismemberTimeScale) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionDecapTimeScale) },
            { TriggerType.Parry, MakeKey(CSMModOptions.CategoryCustomParry, CSMModOptions.OptionParryTimeScale) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionLastEnemyTimeScale) },
            { TriggerType.LastStand, MakeKey(CSMModOptions.CategoryCustomLastStand, CSMModOptions.OptionLastStandTimeScale) }
        };

        private static readonly Dictionary<TriggerType, string> DurationOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionBasicDuration) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionCriticalDuration) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionDismemberDuration) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionDecapDuration) },
            { TriggerType.Parry, MakeKey(CSMModOptions.CategoryCustomParry, CSMModOptions.OptionParryDuration) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionLastEnemyDuration) },
            { TriggerType.LastStand, MakeKey(CSMModOptions.CategoryCustomLastStand, CSMModOptions.OptionLastStandDuration) }
        };

        private static readonly Dictionary<TriggerType, string> CooldownOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionBasicCooldown) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionCriticalCooldown) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionDismemberCooldown) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionDecapCooldown) },
            { TriggerType.Parry, MakeKey(CSMModOptions.CategoryCustomParry, CSMModOptions.OptionParryCooldown) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionLastEnemyCooldown) },
            { TriggerType.LastStand, MakeKey(CSMModOptions.CategoryCustomLastStand, CSMModOptions.OptionLastStandCooldown) }
        };


        private static readonly Dictionary<TriggerType, string> DistributionOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionBasicThirdPerson) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionCriticalThirdPerson) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionDismemberThirdPerson) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionDecapThirdPerson) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionLastEnemyThirdPerson) }
        };

        private static readonly Dictionary<TriggerType, string> DelayInOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionBasicDelayIn) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionCriticalDelayIn) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionDismemberDelayIn) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionDecapDelayIn) },
            { TriggerType.Parry, MakeKey(CSMModOptions.CategoryCustomParry, CSMModOptions.OptionParryDelayIn) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionLastEnemyDelayIn) },
            { TriggerType.LastStand, MakeKey(CSMModOptions.CategoryCustomLastStand, CSMModOptions.OptionLastStandDelayIn) }
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
            _lastDelayInPreset = null;
            _lastDistributionPreset = null;
            _lastTriggerProfile = null;
            _lastDebugLogging = false;
            _lastResetStats = false;
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
            local = ApplyDelayPreset(force);
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
            changed |= ApplyStatisticsReset();
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

        private bool ApplyStatisticsReset()
        {
            if (!CSMModOptions.ResetStatsToggle || _lastResetStats)
            {
                _lastResetStats = CSMModOptions.ResetStatsToggle;
                return false;
            }

            CSMModOptions.ResetStatistics();
            CSMModOptions.ResetStatsToggle = false;
            _lastResetStats = false;
            Debug.Log("[CSM] Statistics reset");
            return true;
        }

        private static string ResolvePresetLabel(string settingValue, object preset)
        {
            return string.IsNullOrWhiteSpace(settingValue) ? preset.ToString() : settingValue;
        }

        private bool ApplyIntensityPreset(bool force)
        {
            var preset = CSMModOptions.GetCurrentPreset();
            if (!force && _lastIntensityPreset.HasValue && _lastIntensityPreset.Value.Equals(preset))
                return false;

            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Applying Intensity Preset: " + ResolvePresetLabel(CSMModOptions.CurrentPreset, preset));

            foreach (var trigger in TriggerTypes)
            {
                CSMManager.GetPresetValues(preset, trigger, out float chance, out float timeScale, out float duration, out float cooldown);
                CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.TimeScale, timeScale);
                SyncOptionValue(TimeScaleOptionNames, trigger, timeScale);

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM]   " + GetTriggerUiName(trigger) + " TimeScale = " + timeScale.ToString("0.00") + " (" + (timeScale * 100f).ToString("F0") + "%)");
            }

            _lastIntensityPreset = preset;
            _presetAppliedTime = Time.unscaledTime;
            StoreExpectedPresetValues();
            LogPresetApply("Intensity Preset", ResolvePresetLabel(CSMModOptions.CurrentPreset, preset));
            return true;
        }

        private void StoreExpectedPresetValues()
        {
            foreach (var trigger in TriggerTypes)
            {
                _expectedPresetValues[trigger] = CSMModOptions.GetCustomValues(trigger);
            }
        }

        private bool ApplyChancePreset(bool force)
        {
            var preset = CSMModOptions.GetChancePreset();
            
            // When Chance is Off, always force apply 100% values to ensure consistency
            // (other presets might have changed values, so we need to enforce 100%)
            bool isOff = preset == CSMModOptions.ChancePreset.Off;
            if (!force && !isOff && _lastChancePreset.HasValue && _lastChancePreset.Value.Equals(preset))
                return false;

            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Applying Chance Preset: " + ResolvePresetLabel(CSMModOptions.ChancePresetSetting, preset));

            foreach (var trigger in TriggerTypes)
            {
                float value = CSMModOptions.GetPresetChanceValue(trigger);
                CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.Chance, value);
                SyncOptionValue(ChanceOptionNames, trigger, value);

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM]   " + GetTriggerUiName(trigger) + " Chance = " + (value * 100f).ToString("F0") + "%");
            }

            _lastChancePreset = preset;
            _presetAppliedTime = Time.unscaledTime;
            StoreExpectedPresetValues();
            LogPresetApply("Chance Preset", ResolvePresetLabel(CSMModOptions.ChancePresetSetting, preset));
            return true;
        }

        private bool ApplyCooldownPreset(bool force)
        {
            var preset = CSMModOptions.GetCooldownPreset();
            
            // When Cooldown is Off, always force apply 0 values to ensure consistency
            bool isOff = preset == CSMModOptions.CooldownPreset.Off;
            if (!force && !isOff && _lastCooldownPreset.HasValue && _lastCooldownPreset.Value.Equals(preset))
                return false;

            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Applying Cooldown Preset: " + ResolvePresetLabel(CSMModOptions.CooldownPresetSetting, preset));

            foreach (var trigger in TriggerTypes)
            {
                float value = CSMModOptions.GetPresetCooldownValue(trigger);
                CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.Cooldown, value);
                SyncOptionValue(CooldownOptionNames, trigger, value);

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM]   " + GetTriggerUiName(trigger) + " Cooldown = " + value.ToString("0.##") + "s");
            }

            _lastCooldownPreset = preset;
            _presetAppliedTime = Time.unscaledTime;
            StoreExpectedPresetValues();
            LogPresetApply("Cooldown Preset", ResolvePresetLabel(CSMModOptions.CooldownPresetSetting, preset));
            return true;
        }

        private bool ApplyDurationPreset(bool force)
        {
            var preset = CSMModOptions.GetDurationPreset();
            if (!force && _lastDurationPreset.HasValue && _lastDurationPreset.Value.Equals(preset))
                return false;

            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Applying Duration Preset: " + ResolvePresetLabel(CSMModOptions.DurationPresetSetting, preset));

            foreach (var trigger in TriggerTypes)
            {
                float value = CSMModOptions.GetPresetDurationValue(trigger);
                CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.Duration, value);
                SyncOptionValue(DurationOptionNames, trigger, value);

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM]   " + GetTriggerUiName(trigger) + " Duration = " + value.ToString("0.##") + "s");
            }

            _lastDurationPreset = preset;
            _presetAppliedTime = Time.unscaledTime;
            StoreExpectedPresetValues();
            LogPresetApply("Duration Preset", ResolvePresetLabel(CSMModOptions.DurationPresetSetting, preset));
            return true;
        }

        private bool ApplyDelayPreset(bool force)
        {
            var preset = CSMModOptions.GetDelayInPreset();

            bool changed = !_lastDelayInPreset.HasValue || !_lastDelayInPreset.Value.Equals(preset);

            if (!force && !changed)
                return false;

            string value = CSMModOptions.DelayInPresetSetting;

            foreach (var trigger in TriggerTypes)
            {
                CSMModOptions.SetTriggerDelayPreset(trigger, value);
                SyncStringOption(DelayInOptionNames, trigger, value);
            }

            _lastDelayInPreset = preset;
            _presetAppliedTime = Time.unscaledTime;
            StoreExpectedPresetValues();

            float delayTime = CSMModOptions.GetDelayTime(preset);
            LogPresetApply("Delay Preset", value + " (" + delayTime.ToString("F2") + "s)");
            return true;
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
            _presetAppliedTime = Time.unscaledTime;
            StoreExpectedPresetValues();
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

        private bool SyncStringOption(Dictionary<TriggerType, string> map, TriggerType type, string value)
        {
            if (!map.TryGetValue(type, out string optionKey))
                return false;
            return SyncStringOption(optionKey, value);
        }

        private bool SyncStringOption(string optionKey, string value)
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

            int index = FindParameterIndexByName(option.parameterValues, value);
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

        private static int FindParameterIndexByName(ModOptionParameter[] parameters, string value)
        {
            if (parameters == null || string.IsNullOrEmpty(value)) return -1;

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramValue = parameters[i]?.value;
                if (paramValue is string sValue && sValue.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }

            return -1;
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
            return BuildSummary(type, values);
        }

        private static string BuildSummary(TriggerType type, CSMModOptions.TriggerCustomValues values)
        {
            string chanceLabel = (values.Chance * 100f).ToString("F0") + "%";
            string scaleLabel = (values.TimeScale * 100f).ToString("F0") + "%";
            string durationLabel = values.Duration.ToString("F1") + "s";
            string cooldownLabel = values.Cooldown.ToString("F1") + "s";

            float delayTime = CSMModOptions.GetDelayTime(values.DelayIn);
            string smoothLabel = delayTime.ToString("0.##") + "s";

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
                   " | Delay " + smoothLabel +
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
            float timeSincePreset = Time.unscaledTime - _presetAppliedTime;
            bool withinLockWindow = timeSincePreset < PresetLockDuration;

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

                // Check if values were corrupted by UI (don't match expected preset values)
                // Only revert within the lock window after preset was applied
                if (withinLockWindow && _expectedPresetValues.TryGetValue(trigger, out CSMModOptions.TriggerCustomValues expected))
                {
                    bool reverted = false;

                    // Check and revert TimeScale
                    if (Mathf.Abs(current.TimeScale - expected.TimeScale) > 0.0001f &&
                        Mathf.Abs(last.TimeScale - expected.TimeScale) < 0.0001f)
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] TimeScale corruption: " + GetTriggerUiName(trigger) +
                                      " expected " + expected.TimeScale.ToString("0.00") +
                                      ", got " + current.TimeScale.ToString("0.00") + " - reverting");
                        CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.TimeScale, expected.TimeScale);
                        SyncOptionValue(TimeScaleOptionNames, trigger, expected.TimeScale);
                        current.TimeScale = expected.TimeScale;
                        reverted = true;
                    }

                    // Check and revert Chance
                    if (Mathf.Abs(current.Chance - expected.Chance) > 0.0001f &&
                        Mathf.Abs(last.Chance - expected.Chance) < 0.0001f)
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Chance corruption: " + GetTriggerUiName(trigger) +
                                      " expected " + (expected.Chance * 100f).ToString("F0") + "%" +
                                      ", got " + (current.Chance * 100f).ToString("F0") + "% - reverting");
                        CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.Chance, expected.Chance);
                        SyncOptionValue(ChanceOptionNames, trigger, expected.Chance);
                        current.Chance = expected.Chance;
                        reverted = true;
                    }

                    // Check and revert Duration
                    if (Mathf.Abs(current.Duration - expected.Duration) > 0.0001f &&
                        Mathf.Abs(last.Duration - expected.Duration) < 0.0001f)
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Duration corruption: " + GetTriggerUiName(trigger) +
                                      " expected " + expected.Duration.ToString("0.##") + "s" +
                                      ", got " + current.Duration.ToString("0.##") + "s - reverting");
                        CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.Duration, expected.Duration);
                        SyncOptionValue(DurationOptionNames, trigger, expected.Duration);
                        current.Duration = expected.Duration;
                        reverted = true;
                    }

                    // Check and revert Cooldown
                    if (Mathf.Abs(current.Cooldown - expected.Cooldown) > 0.0001f &&
                        Mathf.Abs(last.Cooldown - expected.Cooldown) < 0.0001f)
                    {
                        if (CSMModOptions.DebugLogging)
                            Debug.Log("[CSM] Cooldown corruption: " + GetTriggerUiName(trigger) +
                                      " expected " + expected.Cooldown.ToString("0.##") + "s" +
                                      ", got " + current.Cooldown.ToString("0.##") + "s - reverting");
                        CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.Cooldown, expected.Cooldown);
                        SyncOptionValue(CooldownOptionNames, trigger, expected.Cooldown);
                        current.Cooldown = expected.Cooldown;
                        reverted = true;
                    }

                    if (reverted)
                    {
                        _lastCustomValues[trigger] = current;
                        changed = true;
                        continue;
                    }
                }

                _lastCustomValues[trigger] = current;
                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Custom override changed: " + GetTriggerUiName(trigger) + " " + BuildSummary(trigger, last) + " -> " + BuildSummary(trigger, current));
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
            if (a.DelayIn != b.DelayIn) return true;
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
                      " | Delay=" + CSMModOptions.DelayInPresetSetting +
                      " | ThirdPerson=" + CSMModOptions.CameraDistribution +
                      " | TriggerProfile=" + CSMModOptions.TriggerProfile);
            Debug.Log("[CSM] Menu overrides: " +
                      "GlobalCooldown=" + CSMModOptions.GlobalCooldown.ToString("0.##") +
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
            Debug.Log("[CSM] Trigger Values:");
            foreach (var trigger in TriggerTypes)
            {
                Debug.Log("[CSM] " + GetTriggerUiName(trigger) + " -> " + BuildEffectiveSummary(trigger));
            }
        }

    }
}
