using System;
using System.Reflection;
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

            _initialized = true;
        }

        private bool ApplyAllPresets(bool force)
        {
            bool changed = false;
            changed |= ApplyIntensityPreset(force);
            changed |= ApplyChancePreset(force);
            changed |= ApplyCooldownPreset(force);
            changed |= ApplyDurationPreset(force);
            changed |= ApplySmoothnessPreset(force);
            changed |= ApplyDistributionPreset(force);
            changed |= ApplyTriggerProfile(force);
            return changed;
        }

        private bool ApplyIntensityPreset(bool force)
        {
            string presetValue = NormalizePreset(ref CSMModOptions.CurrentPreset, "Balanced");
            if (!force && string.Equals(_lastIntensityPreset, presetValue, StringComparison.Ordinal))
                return false;

            var preset = CSMModOptions.GetCurrentPreset();
            foreach (var trigger in TriggerTypes)
            {
                if (!IsTriggerEnabled(trigger))
                    continue;
                CSMManager.GetPresetValues(preset, trigger, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing);
                SetTimeScale(trigger, timeScale);
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
                if (!IsTriggerEnabled(trigger))
                    continue;
                CSMManager.GetPresetValues(CSMModOptions.Preset.Balanced, trigger, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing);
                CSMModOptions.ApplyChancePreset(ref chance);
                SetChance(trigger, chance);
            }

            _lastChancePreset = presetValue;
            return true;
        }

        private bool ApplyCooldownPreset(bool force)
        {
            string presetValue = NormalizePreset(ref CSMModOptions.CooldownPresetSetting, "Balanced");
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
            if (!force && string.Equals(_lastCooldownPreset, presetValue, StringComparison.Ordinal))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                if (!IsTriggerEnabled(trigger))
                    continue;
                CSMManager.GetPresetValues(CSMModOptions.Preset.Balanced, trigger, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing);
                CSMModOptions.ApplyCooldownPreset(ref cooldown);
                SetCooldown(trigger, cooldown);
            }

            _lastCooldownPreset = presetValue;
            return true;
        }

        private bool ApplyDurationPreset(bool force)
        {
            string presetValue = NormalizePreset(ref CSMModOptions.DurationPresetSetting, "Balanced");
            if (!force && string.Equals(_lastDurationPreset, presetValue, StringComparison.Ordinal))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                if (!IsTriggerEnabled(trigger))
                    continue;
                CSMManager.GetPresetValues(CSMModOptions.Preset.Balanced, trigger, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing);
                CSMModOptions.ApplyDurationPreset(ref duration);
                SetDuration(trigger, duration);
            }

            _lastDurationPreset = presetValue;
            return true;
        }

        private bool ApplySmoothnessPreset(bool force)
        {
            string presetValue = NormalizePreset(ref CSMModOptions.SmoothnessPresetSetting, "Balanced");
            if (!force && string.Equals(_lastSmoothnessPreset, presetValue, StringComparison.Ordinal))
                return false;

            foreach (var trigger in TriggerTypes)
            {
                if (!IsTriggerEnabled(trigger))
                    continue;
                CSMManager.GetPresetValues(CSMModOptions.Preset.Balanced, trigger, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing);
                CSMModOptions.ApplySmoothnessPreset(ref smoothing);
                SetSmoothing(trigger, smoothing);
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
                if (!IsTriggerEnabled(trigger))
                    continue;
                if (!CSMModOptions.IsThirdPersonEligible(trigger))
                    continue;
                SetDistribution(trigger, multiplier);
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
