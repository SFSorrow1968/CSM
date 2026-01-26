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
        private readonly Dictionary<string, string> _baseTooltips =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly Dictionary<string, ModOption> _modOptionsByName =
            new Dictionary<string, ModOption>(StringComparer.Ordinal);

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
            { TriggerType.BasicKill, "Basic Chance" },
            { TriggerType.Critical, "Critical Chance" },
            { TriggerType.Dismemberment, "Dismember Chance" },
            { TriggerType.Decapitation, "Decapitation Chance" },
            { TriggerType.Parry, "Parry Chance" },
            { TriggerType.LastEnemy, "Last Enemy Chance" }
        };

        private static readonly Dictionary<TriggerType, string> TimeScaleOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, "Basic Time Scale" },
            { TriggerType.Critical, "Critical Time Scale" },
            { TriggerType.Dismemberment, "Dismember Time Scale" },
            { TriggerType.Decapitation, "Decapitation Time Scale" },
            { TriggerType.Parry, "Parry Time Scale" },
            { TriggerType.LastEnemy, "Last Enemy Time Scale" },
            { TriggerType.LastStand, "Last Stand Time Scale" }
        };

        private static readonly Dictionary<TriggerType, string> DurationOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, "Basic Duration" },
            { TriggerType.Critical, "Critical Duration" },
            { TriggerType.Dismemberment, "Dismember Duration" },
            { TriggerType.Decapitation, "Decapitation Duration" },
            { TriggerType.Parry, "Parry Duration" },
            { TriggerType.LastEnemy, "Last Enemy Duration" },
            { TriggerType.LastStand, "Last Stand Duration" }
        };

        private static readonly Dictionary<TriggerType, string> CooldownOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, "Basic Cooldown" },
            { TriggerType.Critical, "Critical Cooldown" },
            { TriggerType.Dismemberment, "Dismember Cooldown" },
            { TriggerType.Decapitation, "Decapitation Cooldown" },
            { TriggerType.Parry, "Parry Cooldown" },
            { TriggerType.LastEnemy, "Last Enemy Cooldown" },
            { TriggerType.LastStand, "Last Stand Cooldown" }
        };

        private static readonly Dictionary<TriggerType, string> SmoothingOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, "Basic Smoothing" },
            { TriggerType.Critical, "Critical Smoothing" },
            { TriggerType.Dismemberment, "Dismember Smoothing" },
            { TriggerType.Decapitation, "Decapitation Smoothing" },
            { TriggerType.Parry, "Parry Smoothing" },
            { TriggerType.LastEnemy, "Last Enemy Smoothing" },
            { TriggerType.LastStand, "Last Stand Smoothing" }
        };

        private static readonly Dictionary<TriggerType, string> DistributionOptionNames = new Dictionary<TriggerType, string>
        {
            { TriggerType.BasicKill, "Basic Third Person Distribution" },
            { TriggerType.Critical, "Critical Third Person Distribution" },
            { TriggerType.Dismemberment, "Dismember Third Person Distribution" },
            { TriggerType.Decapitation, "Decapitation Third Person Distribution" },
            { TriggerType.LastEnemy, "Last Enemy Third Person Distribution" }
        };

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
            _modOptionsByName.Clear();
            if (_modData?.modOptions == null) return;

            foreach (var option in _modData.modOptions)
            {
                if (option == null || string.IsNullOrEmpty(option.name)) continue;
                _modOptionsByName[option.name] = option;
                if (!_baseTooltips.ContainsKey(option.name))
                    _baseTooltips[option.name] = option.tooltip ?? string.Empty;
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
            }

            CSMModOptions.EnableBasicKill = basicKill;
            CSMModOptions.EnableCriticalKill = critical;
            CSMModOptions.EnableDismemberment = dismemberment;
            CSMModOptions.EnableDecapitation = decapitation;
            CSMModOptions.EnableParry = parry;
            CSMModOptions.EnableLastEnemy = lastEnemy;
            CSMModOptions.EnableLastStand = lastStand;

            _lastTriggerProfile = presetValue;
            return true;
        }

        private static string NormalizePreset(ref string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "Custom")
                value = fallback;
            return value;
        }

        private bool SyncOptionValue(Dictionary<TriggerType, string> map, TriggerType type, float value)
        {
            if (!map.TryGetValue(type, out string optionName))
                return false;
            return SyncOptionValue(optionName, value);
        }

        private bool SyncOptionValue(string optionName, float value)
        {
            if (!_modOptionsByName.TryGetValue(optionName, out ModOption option))
                return false;

            if (option.parameterValues == null || option.parameterValues.Length == 0)
                option.LoadModOptionParameters();

            int index = FindParameterIndex(option.parameterValues, value);
            if (index < 0)
                return false;

            if (option.currentValueIndex == index)
                return false;

            option.Apply(index);
            option.RefreshUI();
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
            if (!map.TryGetValue(type, out string optionName))
                return false;

            return UpdateTooltip(optionName, effectiveSummary);
        }

        private bool UpdateTooltip(string optionName, string effectiveSummary)
        {
            if (!_modOptionsByName.TryGetValue(optionName, out ModOption option))
                return false;

            if (!_baseTooltips.TryGetValue(optionName, out string baseTooltip))
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
            return true;
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
