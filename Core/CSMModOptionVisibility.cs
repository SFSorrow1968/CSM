using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CSM.Configuration;
using ThunderRoad;

namespace CSM.Core
{
    // Simplified: When preset changes -> update values -> sync UI -> done
    // No throttling, no lock windows, no corruption detection - just direct preset application
    public class CSMModOptionVisibility
    {
        public static CSMModOptionVisibility Instance { get; } = new CSMModOptionVisibility();

        private ModManager.ModData _modData;
        private bool _initialized;
        
        // Track only preset changes to know when to update UI
        private CSMModOptions.Preset? _lastIntensityPreset;
        private CSMModOptions.ChancePreset? _lastChancePreset;
        private CSMModOptions.CooldownPreset? _lastCooldownPreset;
        private CSMModOptions.DurationPreset? _lastDurationPreset;
        private CSMModOptions.TransitionPreset? _lastTransitionPreset;
        private CSMModOptions.CameraDistributionPreset? _lastDistributionPreset;
        private CSMModOptions.TriggerProfilePreset? _lastTriggerProfile;
        
        private bool _lastDebugLogging;
        private bool _lastResetStats;
        
        private readonly Dictionary<string, string> _baseTooltips = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, ModOption> _modOptionsByKey = new Dictionary<string, ModOption>(StringComparer.Ordinal);

        private const string OptionKeySeparator = "||";
        private const float UpdateIntervalSeconds = 0.1f;
        private float _nextUpdateTime;

        private static readonly TriggerType[] TriggerTypes =
        {
            TriggerType.BasicKill, TriggerType.Critical, TriggerType.Dismemberment, TriggerType.Decapitation,
            TriggerType.Parry, TriggerType.LastEnemy, TriggerType.LastStand,
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

        private static readonly Dictionary<TriggerType, string> TransitionOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, MakeKey(CSMModOptions.CategoryCustomBasic, CSMModOptions.OptionBasicTransition) },
            { TriggerType.Critical, MakeKey(CSMModOptions.CategoryCustomCritical, CSMModOptions.OptionCriticalTransition) },
            { TriggerType.Dismemberment, MakeKey(CSMModOptions.CategoryCustomDismemberment, CSMModOptions.OptionDismemberTransition) },
            { TriggerType.Decapitation, MakeKey(CSMModOptions.CategoryCustomDecapitation, CSMModOptions.OptionDecapTransition) },
            { TriggerType.Parry, MakeKey(CSMModOptions.CategoryCustomParry, CSMModOptions.OptionParryTransition) },
            { TriggerType.LastEnemy, MakeKey(CSMModOptions.CategoryCustomLastEnemy, CSMModOptions.OptionLastEnemyTransition) },
            { TriggerType.LastStand, MakeKey(CSMModOptions.CategoryCustomLastStand, CSMModOptions.OptionLastStandTransition) }
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
            _lastTransitionPreset = null;
            _lastDistributionPreset = null;
            _lastTriggerProfile = null;
            _lastDebugLogging = false;
            _lastResetStats = false;
            _nextUpdateTime = 0f;
            _baseTooltips.Clear();

            TryInitialize();
            if (_initialized)
            {
                ApplyAllPresets(true);
                ModManager.RefreshModOptionsUI();
            }
        }

        public void Shutdown() { }

        public void Update()
        {
            if (!_initialized)
            {
                TryInitialize();
                if (_initialized)
                {
                    ApplyAllPresets(true);
                    ModManager.RefreshModOptionsUI();
                }
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextUpdateTime)
                return;

            _nextUpdateTime = now + UpdateIntervalSeconds;

            if (ApplyAllPresets(false))
                ModManager.RefreshModOptionsUI();
        }

        private void TryInitialize()
        {
            if (_initialized || !ModManager.TryGetModData(Assembly.GetExecutingAssembly(), out _modData) || 
                _modData?.modOptions == null || _modData.modOptions.Count == 0)
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
            changed |= ApplyIntensityPreset(force);
            changed |= ApplyChancePreset(force);
            changed |= ApplyCooldownPreset(force);
            changed |= ApplyDurationPreset(force);
            changed |= ApplyTransitionPreset(force);
            changed |= ApplyDistributionPreset(force);
            changed |= ApplyTriggerProfile(force);
            changed |= ApplyTriggerDependencies();
            changed |= ApplyDiagnostics();
            changed |= ApplyStatisticsReset();
            changed |= UpdateDebugTooltips();
            return changed;
        }

        private bool ApplyDiagnostics()
        {
            if (!CSMModOptions.QuickTestNow) return false;
            CSMManager.Instance.TriggerSlow(CSMModOptions.GetQuickTestTrigger(), 0f, null, DamageType.Unknown, 0f, true);
            CSMModOptions.QuickTestNow = false;
            return true;
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

        private bool ApplyIntensityPreset(bool force)
        {
            var preset = CSMModOptions.GetCurrentPreset();
            if (!force && _lastIntensityPreset.HasValue && _lastIntensityPreset.Value.Equals(preset))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                CSMManager.GetPresetValues(preset, trigger, out _, out float timeScale, out _, out _);
                CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.TimeScale, timeScale);
                SyncOptionValue(TimeScaleOptionNames, trigger, timeScale);
            }
            _lastIntensityPreset = preset;
            return true;
        }

        private bool ApplyChancePreset(bool force)
        {
            var preset = CSMModOptions.GetChancePreset();
            if (!force && _lastChancePreset.HasValue && _lastChancePreset.Value.Equals(preset))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                float value = CSMModOptions.GetPresetChanceValue(trigger);
                CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.Chance, value);
                SyncOptionValue(ChanceOptionNames, trigger, value);
            }
            _lastChancePreset = preset;
            return true;
        }

        private bool ApplyCooldownPreset(bool force)
        {
            var preset = CSMModOptions.GetCooldownPreset();
            if (!force && _lastCooldownPreset.HasValue && _lastCooldownPreset.Value.Equals(preset))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                float value = CSMModOptions.GetPresetCooldownValue(trigger);
                CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.Cooldown, value);
                SyncOptionValue(CooldownOptionNames, trigger, value);
            }
            _lastCooldownPreset = preset;
            return true;
        }

        private bool ApplyDurationPreset(bool force)
        {
            var preset = CSMModOptions.GetDurationPreset();
            if (!force && _lastDurationPreset.HasValue && _lastDurationPreset.Value.Equals(preset))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                float value = CSMModOptions.GetPresetDurationValue(trigger);
                CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.Duration, value);
                SyncOptionValue(DurationOptionNames, trigger, value);
            }
            _lastDurationPreset = preset;
            return true;
        }

        private bool ApplyTransitionPreset(bool force)
        {
            var preset = CSMModOptions.GetTransitionPreset();
            if (!force && _lastTransitionPreset.HasValue && _lastTransitionPreset.Value.Equals(preset))
                return false;

            string transitionValue = CSMModOptions.GetTransitionPresetValue();
            foreach (var trigger in TriggerTypes)
            {
                CSMModOptions.SetTriggerEasing(trigger, transitionValue);
                SyncStringOption(TransitionOptionNames, trigger, transitionValue);
            }
            _lastTransitionPreset = preset;
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
                if (!CSMModOptions.IsThirdPersonEligible(trigger)) continue;
                CSMModOptions.SetTriggerValue(trigger, CSMModOptions.TriggerField.Distribution, multiplier);
                SyncOptionValue(DistributionOptionNames, trigger, multiplier);
            }
            _lastDistributionPreset = preset;
            return true;
        }

        private bool ApplyTriggerProfile(bool force)
        {
            var profile = CSMModOptions.GetTriggerProfilePreset();
            if (!force && _lastTriggerProfile.HasValue && _lastTriggerProfile.Value.Equals(profile))
                return false;

            bool basicKill = true, critical = true, dismemberment = true, decapitation = true;
            bool parry = true, lastEnemy = true, lastStand = true;

            switch (profile)
            {
                case CSMModOptions.TriggerProfilePreset.KillsOnly:
                    parry = lastStand = false;
                    break;
                case CSMModOptions.TriggerProfilePreset.Highlights:
                    basicKill = dismemberment = parry = lastStand = false;
                    break;
                case CSMModOptions.TriggerProfilePreset.LastEnemyOnly:
                    basicKill = critical = dismemberment = decapitation = parry = lastStand = false;
                    break;
                case CSMModOptions.TriggerProfilePreset.ParryOnly:
                    basicKill = critical = dismemberment = decapitation = lastEnemy = lastStand = false;
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
            return true;
        }

        private bool ApplyTriggerDependencies()
        {
            if (CSMModOptions.EnableBasicKill || !CSMModOptions.EnableThrownImpactKill)
                return false;

            CSMModOptions.EnableThrownImpactKill = false;
            SyncToggleOption(ThrownImpactOptionKey, false);
            return true;
        }

        private bool UpdateDebugTooltips()
        {
            bool debugChanged = _lastDebugLogging != CSMModOptions.DebugLogging;
            _lastDebugLogging = CSMModOptions.DebugLogging;
            
            if (!debugChanged || !CSMModOptions.DebugLogging)
                return false;

            // When debug enabled, show effective values in tooltips
            foreach (var trigger in TriggerTypes)
            {
                var v = CSMModOptions.GetCustomValues(trigger);
                string summary = string.Format("Effective: {0:F0}% chance | {1:F0}% scale | {2:F1}s dur | {3:F1}s cd",
                    v.Chance * 100f, v.TimeScale * 100f, v.Duration, v.Cooldown);
                
                UpdateTooltip(ChanceOptionNames, trigger, summary);
                UpdateTooltip(TimeScaleOptionNames, trigger, summary);
                UpdateTooltip(DurationOptionNames, trigger, summary);
                UpdateTooltip(CooldownOptionNames, trigger, summary);
            }
            return true;
        }

        private static string MakeKey(string category, string name)
        {
            return (category ?? string.Empty) + OptionKeySeparator + (name ?? string.Empty);
        }

        private bool SyncOptionValue(Dictionary<TriggerType, string> map, TriggerType type, float value)
        {
            return map.TryGetValue(type, out string key) && SyncOptionValue(key, value);
        }

        private bool SyncToggleOption(Dictionary<TriggerType, string> map, TriggerType type, bool value)
        {
            return map.TryGetValue(type, out string key) && SyncToggleOption(key, value);
        }

        private bool SyncOptionValue(string optionKey, float value)
        {
            if (!_modOptionsByKey.TryGetValue(optionKey, out ModOption option))
                return false;

            if (option.parameterValues == null || option.parameterValues.Length == 0)
                option.LoadModOptionParameters();

            int index = FindParameterIndex(option.parameterValues, value);
            if (index < 0 || option.currentValueIndex == index)
                return false;

            option.Apply(index);
            option.RefreshUI();
            return true;
        }

        private bool SyncToggleOption(string optionKey, bool value)
        {
            if (!_modOptionsByKey.TryGetValue(optionKey, out ModOption option))
                return false;

            if (option.parameterValues == null || option.parameterValues.Length == 0)
                option.LoadModOptionParameters();

            int index = value ? 1 : 0;
            if (option.currentValueIndex == index)
                return false;

            option.Apply(index);
            option.RefreshUI();
            return true;
        }

        private bool SyncStringOption(Dictionary<TriggerType, string> map, TriggerType type, string value)
        {
            return map.TryGetValue(type, out string key) && SyncStringOption(key, value);
        }

        private bool SyncStringOption(string optionKey, string value)
        {
            if (!_modOptionsByKey.TryGetValue(optionKey, out ModOption option))
                return false;

            if (option.parameterValues == null || option.parameterValues.Length == 0)
                option.LoadModOptionParameters();

            int index = FindParameterIndexByName(option.parameterValues, value);
            if (index < 0 || option.currentValueIndex == index)
                return false;

            option.Apply(index);
            option.RefreshUI();
            return true;
        }

        private static int FindParameterIndexByName(ModOptionParameter[] parameters, string value)
        {
            if (parameters == null || string.IsNullOrEmpty(value)) return -1;
            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i]?.value is string sValue && sValue.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            return -1;
        }

        private static int FindParameterIndex(ModOptionParameter[] parameters, float value)
        {
            if (parameters == null) return -1;
            for (int i = 0; i < parameters.Length; i++)
            {
                var pv = parameters[i]?.value;
                if ((pv is float fv && Mathf.Abs(fv - value) < 0.0001f) ||
                    (pv is double dv && Mathf.Abs((float)dv - value) < 0.0001f) ||
                    (pv is int iv && Mathf.Abs(iv - value) < 0.0001f))
                    return i;
            }
            return -1;
        }

        private bool UpdateTooltip(Dictionary<TriggerType, string> map, TriggerType type, string effectiveSummary)
        {
            if (!map.TryGetValue(type, out string key) || !_modOptionsByKey.TryGetValue(key, out ModOption option))
                return false;

            if (!_baseTooltips.TryGetValue(key, out string baseTooltip))
                baseTooltip = option.tooltip ?? string.Empty;

            string newTooltip = string.IsNullOrEmpty(effectiveSummary) ? baseTooltip :
                string.IsNullOrEmpty(baseTooltip) ? effectiveSummary : baseTooltip + " | " + effectiveSummary;

            if (option.tooltip == newTooltip)
                return false;

            option.tooltip = newTooltip;
            option.RefreshUI();
            return true;
        }
    }
}
