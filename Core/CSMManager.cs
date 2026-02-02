using System;
using System.Collections.Generic;
using CSM.Configuration;
using ThunderRoad;
using UnityEngine;

namespace CSM.Core
{
    public class CSMManager
    {
        private static CSMManager _instance;
        public static CSMManager Instance => _instance ??= new CSMManager();

        private bool _isSlowMotionActive;
        private float _slowMotionEndTime;
        private float _slowMotionStartTime;
        private float _originalTimeScale = 1f;
        private float _originalFixedDeltaTime = 0.02f;
        private TriggerType _activeTriggerType;
        private float _globalCooldownEndTime;
        private readonly Dictionary<TriggerType, float> _triggerCooldownEndTimes = new Dictionary<TriggerType, float>();

        private bool _isTransitioning;
        private float _targetTimeScale;
        private float _currentTimeScale;
        private float _transitionStartTime;
        private float _transitionDuration;
        private float _transitionStartScale;
        private const float TransitionTimeoutSeconds = 5f;
        private const float EndOverrunGraceSeconds = 2f;
        private bool _isEasingOut;
        private float _easingOutDuration;

        public bool IsActive => _isSlowMotionActive;

        public void Initialize()
        {
            try
            {
                _originalTimeScale = 1f;
                _originalFixedDeltaTime = Time.fixedDeltaTime;
                _isSlowMotionActive = false;
                _slowMotionStartTime = 0f;
                _globalCooldownEndTime = 0f;
                _triggerCooldownEndTimes.Clear();

                Debug.Log("[CSM] Manager initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] Manager init failed: " + ex.Message);
            }
        }

        public void Update()
        {
            try
            {
                if (_isTransitioning)
                {
                    UpdateTransition();
                }

                if (!_isSlowMotionActive) return;

                if (Time.unscaledTime >= _slowMotionEndTime)
                {
                    EndSlowMotion();
                }

                if (_isSlowMotionActive && Time.unscaledTime > _slowMotionEndTime + EndOverrunGraceSeconds)
                {
                    Debug.LogWarning("[CSM] SlowMo exceeded expected duration. Forcing cancel.");
                    CancelSlowMotion();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] Update error: " + ex.Message);
                TryRestoreTimeScale();
            }
        }

        private void UpdateTransition()
        {
            if (_transitionDuration <= 0f)
            {
                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Transition immediate: target=" + _targetTimeScale.ToString("0.###"));
                _currentTimeScale = _targetTimeScale;
                _isTransitioning = false;
                ApplyTimeScale(_currentTimeScale);
                return;
            }

            float elapsed = Time.unscaledTime - _transitionStartTime;
            if (elapsed > TransitionTimeoutSeconds)
            {
                _currentTimeScale = _targetTimeScale;
                _isTransitioning = false;
                ApplyTimeScale(_currentTimeScale);
                Debug.LogWarning("[CSM] Transition timed out. Forcing time scale.");
                return;
            }

            float t = Mathf.Clamp01(elapsed / _transitionDuration);
            float eased = EaseInOut(t);
            _currentTimeScale = Mathf.Lerp(_transitionStartScale, _targetTimeScale, eased);

            if (t >= 1f)
            {
                _currentTimeScale = _targetTimeScale;
                _isTransitioning = false;
                _isEasingOut = false;

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Transition complete: " + _currentTimeScale);
            }

            ApplyTimeScale(_currentTimeScale);
        }

        private float EaseInOut(float x)
        {
            var curve = CSMModOptions.GetEasingCurve(_activeTriggerType);
            switch (curve)
            {
                case CSMModOptions.EasingCurve.Off:
                    return 1f; // Instant - jump to target immediately
                case CSMModOptions.EasingCurve.Linear:
                    return x;
                default: // Smoothstep
                    return x * x * (3f - 2f * x);
            }
        }

        private void ApplyTimeScale(float scale)
        {
            float clampedScale = Mathf.Clamp(scale, 0.005f, 1f);
            Time.timeScale = clampedScale;
            Time.fixedDeltaTime = _originalFixedDeltaTime * clampedScale;
        }

        public bool TriggerSlow(TriggerType type)
        {
            return TriggerSlow(type, 0f, null, DamageType.Unknown, 0f, false);
        }

        public bool TriggerSlow(TriggerType type, float damageDealt)
        {
            return TriggerSlow(type, damageDealt, null, DamageType.Unknown, 0f, false);
        }

        public bool TriggerSlow(TriggerType type, float damageDealt, Creature targetCreature)
        {
            return TriggerSlow(type, damageDealt, targetCreature, DamageType.Unknown, 0f, false);
        }

        public bool TriggerSlow(TriggerType type, float damageDealt, Creature targetCreature, DamageType damageType, float intensity, bool isQuickTest = false)
        {
            try
            {
                if (!CSMModOptions.EnableMod)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Mod disabled");
                    SetLastTriggerDebug(type, "Blocked: Mod disabled", isQuickTest);
                    return false;
                }

                bool enabled;
                float chance, timeScale, duration, cooldown;
                GetTriggerConfig(type, out enabled, out chance, out timeScale, out duration, out cooldown);

                // Apply damage type and intensity multipliers
                float damageTypeMultiplier = CSMModOptions.GetDamageTypeMultiplier(damageType);
                float intensityMultiplier = CSMModOptions.GetIntensityMultiplier(intensity);
                float combinedMultiplier = damageTypeMultiplier * intensityMultiplier;

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Damage input: damageType=" + damageType + " (" + damageTypeMultiplier.ToString("F1") + "x), " +
                              "intensity=" + intensity.ToString("F2") + " (" + intensityMultiplier.ToString("F2") + "x), " +
                              "combined=" + combinedMultiplier.ToString("F2") + "x");

                // Multiplier > 1 = more intense slow-mo = lower timeScale
                if (combinedMultiplier != 1.0f)
                {
                    float originalTimeScale = timeScale;
                    timeScale = timeScale / combinedMultiplier;
                    timeScale = Mathf.Clamp(timeScale, 0.01f, 1f);

                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Multiplier applied: timeScale " + originalTimeScale.ToString("F3") + " -> " + timeScale.ToString("F3"));
                }

                if (CSMModOptions.DebugLogging)
                {
                    var raw = CSMModOptions.GetCustomValues(type);
                    Debug.Log("[CSM] TriggerSlow(" + type + ") enabled=" + enabled + " raw: " + FormatValues(raw.Chance, raw.TimeScale, raw.Duration, raw.Cooldown, type, raw.Distribution));
                    Debug.Log("[CSM] TriggerSlow(" + type + ") effective: " + FormatValues(chance, timeScale, duration, cooldown, type, raw.Distribution));
                    Debug.Log("[CSM] TriggerSlow(" + type + ") presets: " +
                              "Intensity=" + CSMModOptions.CurrentPreset +
                              " | Chance=" + CSMModOptions.ChancePresetSetting +
                              " | Cooldown=" + CSMModOptions.CooldownPresetSetting +
                              " | Duration=" + CSMModOptions.DurationPresetSetting);
                }

                if (!enabled)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Trigger disabled");
                    SetLastTriggerDebug(type, "Blocked: Trigger disabled", isQuickTest);
                    return false;
                }

                float now = Time.unscaledTime;

                if (now < _globalCooldownEndTime)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Global cooldown");
                    SetLastTriggerDebug(type, "Blocked: Global cooldown", isQuickTest);
                    return false;
                }

                if (_triggerCooldownEndTimes.TryGetValue(type, out float triggerCooldownEnd) && now < triggerCooldownEnd)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Trigger cooldown");
                    SetLastTriggerDebug(type, "Blocked: Trigger cooldown", isQuickTest);
                    return false;
                }

                if (_isSlowMotionActive && (int)type <= (int)_activeTriggerType)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - SlowMo already active");
                    SetLastTriggerDebug(type, "Blocked: SlowMo already active", isQuickTest);
                    return false;
                }

                float roll = UnityEngine.Random.value;
                if (chance < 1.0f && roll > chance)
                {
                    if (CSMModOptions.DebugLogging)
                    {
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Chance roll failed (" + (roll*100).ToString("F0") + "% vs " + (chance*100).ToString("F0") + "%)");
                        SetLastTriggerDebug(type, "Blocked: Chance failed (" + (roll * 100f).ToString("F0") + "% > " + (chance * 100f).ToString("F0") + "%)", isQuickTest);
                    }
                    return false;
                }

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] SlowMo START: " + type + " at " + (timeScale*100).ToString("F0") + "% for " + duration.ToString("F1") + "s");
                StartSlowMotion(type, timeScale, duration, cooldown, damageDealt);
                SetLastTriggerDebug(type, "Triggered", isQuickTest);

                CSMModOptions.IncrementTriggerCount(type);
                CSMModOptions.AddSlowMoTime(duration);

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] " + GetTriggerDisplayName(type));

                float distribution = CSMModOptions.GetThirdPersonDistribution(type);
                bool allowThirdPerson = distribution > 0f;
                if (CSMModOptions.IsThirdPersonEligible(type) && allowThirdPerson)
                {
                    CSMKillcam.Instance.TryStartKillcam(type, targetCreature, duration, allowThirdPerson);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] TriggerSlow error: " + ex.Message);
                SetLastTriggerDebug(type, "Error: " + ex.Message, isQuickTest);
                return false;
            }
        }

        private void GetTriggerConfig(TriggerType type, out bool enabled, out float chance, out float timeScale, out float duration, out float cooldown)
        {
            enabled = CSMModOptions.IsTriggerEnabled(type);
            if (!enabled)
            {
                chance = 0f;
                timeScale = 0.5f;
                duration = 1f;
                cooldown = 0f;
                return;
            }
            GetCustomTriggerConfig(type, out chance, out timeScale, out duration, out cooldown);
        }

        public static void GetPresetValues(CSMModOptions.Preset preset, TriggerType type, out float chance, out float timeScale, out float duration, out float cooldown)
        {
            // Intensity multipliers: Subtle=1.5, Default=1.0, Dramatic=0.8, Cinematic=0.5, Epic=0.3
            chance = 0.5f;
            timeScale = 0.25f;
            duration = 1.5f;
            cooldown = 5f;

            switch (type)
            {
                case TriggerType.BasicKill:
                    chance = 0.25f;
                    duration = 2.5f;
                    cooldown = 10f;
                    timeScale = 0.28f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle: timeScale = 0.42f; break;
                        case CSMModOptions.Preset.Default: timeScale = 0.28f; break;
                        case CSMModOptions.Preset.Dramatic: timeScale = 0.22f; break;
                        case CSMModOptions.Preset.Cinematic: timeScale = 0.14f; break;
                        case CSMModOptions.Preset.Epic: timeScale = 0.08f; break;
                    }
                    break;

                case TriggerType.Critical:
                    chance = 0.75f;
                    duration = 3.0f;
                    cooldown = 10f;
                    timeScale = 0.25f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle: timeScale = 0.38f; break;
                        case CSMModOptions.Preset.Default: timeScale = 0.25f; break;
                        case CSMModOptions.Preset.Dramatic: timeScale = 0.20f; break;
                        case CSMModOptions.Preset.Cinematic: timeScale = 0.13f; break;
                        case CSMModOptions.Preset.Epic: timeScale = 0.08f; break;
                    }
                    break;

                case TriggerType.Dismemberment:
                    chance = 0.30f;
                    duration = 2.0f;
                    cooldown = 10f;
                    timeScale = 0.30f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle: timeScale = 0.45f; break;
                        case CSMModOptions.Preset.Default: timeScale = 0.30f; break;
                        case CSMModOptions.Preset.Dramatic: timeScale = 0.24f; break;
                        case CSMModOptions.Preset.Cinematic: timeScale = 0.15f; break;
                        case CSMModOptions.Preset.Epic: timeScale = 0.09f; break;
                    }
                    break;

                case TriggerType.Decapitation:
                    chance = 0.9f;
                    duration = 3.25f;
                    cooldown = 10f;
                    timeScale = 0.23f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle: timeScale = 0.35f; break;
                        case CSMModOptions.Preset.Default: timeScale = 0.23f; break;
                        case CSMModOptions.Preset.Dramatic: timeScale = 0.18f; break;
                        case CSMModOptions.Preset.Cinematic: timeScale = 0.12f; break;
                        case CSMModOptions.Preset.Epic: timeScale = 0.07f; break;
                    }
                    break;

                case TriggerType.Parry:
                    chance = 0.5f;
                    duration = 1.5f;
                    cooldown = 5f;
                    timeScale = 0.34f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle: timeScale = 0.51f; break;
                        case CSMModOptions.Preset.Default: timeScale = 0.34f; break;
                        case CSMModOptions.Preset.Dramatic: timeScale = 0.27f; break;
                        case CSMModOptions.Preset.Cinematic: timeScale = 0.17f; break;
                        case CSMModOptions.Preset.Epic: timeScale = 0.10f; break;
                    }
                    break;

                case TriggerType.LastEnemy:
                    chance = 1.0f;
                    duration = 2.75f;
                    cooldown = 30f;
                    timeScale = 0.26f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle: timeScale = 0.39f; break;
                        case CSMModOptions.Preset.Default: timeScale = 0.26f; break;
                        case CSMModOptions.Preset.Dramatic: timeScale = 0.21f; break;
                        case CSMModOptions.Preset.Cinematic: timeScale = 0.13f; break;
                        case CSMModOptions.Preset.Epic: timeScale = 0.08f; break;
                    }
                    break;

                case TriggerType.LastStand:
                    chance = 1.0f;
                    duration = 4.0f;
                    cooldown = 90f;
                    timeScale = 0.30f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle: timeScale = 0.45f; break;
                        case CSMModOptions.Preset.Default: timeScale = 0.30f; break;
                        case CSMModOptions.Preset.Dramatic: timeScale = 0.24f; break;
                        case CSMModOptions.Preset.Cinematic: timeScale = 0.15f; break;
                        case CSMModOptions.Preset.Epic: timeScale = 0.09f; break;
                    }
                    break;
            }
        }

        private void GetCustomTriggerConfig(TriggerType type, out float chance, out float timeScale, out float duration, out float cooldown)
        {
            // Values are now set directly by preset apply methods, no runtime multiplication needed
            var values = CSMModOptions.GetCustomValues(type);
            chance = values.Chance;
            timeScale = values.TimeScale;
            duration = values.Duration;
            cooldown = values.Cooldown;
        }

        private void StartSlowMotion(TriggerType type, float timeScale, float duration, float cooldown, float damageDealt)
        {
            try
            {
                if (!_isSlowMotionActive)
                {
                    _originalTimeScale = Time.timeScale;
                    _originalFixedDeltaTime = Time.fixedDeltaTime;
                    _currentTimeScale = _originalTimeScale;
                }

                _isSlowMotionActive = true;
                _activeTriggerType = type;
                _slowMotionStartTime = Time.unscaledTime;
                _slowMotionEndTime = _slowMotionStartTime + duration;

                // Easing uses percentage of duration for transition (20%)
                // If easing curve is Off, no transition (instant)
                var curve = CSMModOptions.GetEasingCurve(type);
                float easingDuration = (curve != CSMModOptions.EasingCurve.Off) ? (duration * CSMModOptions.TransitionRampPercent) : 0f;
                _easingOutDuration = easingDuration;
                _isEasingOut = false;

                _targetTimeScale = Mathf.Clamp(timeScale, 0.005f, 1f);
                _transitionStartScale = _currentTimeScale;
                _transitionStartTime = Time.unscaledTime;
                _transitionDuration = easingDuration;
                _isTransitioning = easingDuration > 0f;

                // If no transition (instant), apply time scale immediately
                if (!_isTransitioning)
                {
                    _currentTimeScale = _targetTimeScale;
                    ApplyTimeScale(_currentTimeScale);
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Instant transition applied: " + _currentTimeScale.ToString("0.###"));
                }

                float now = Time.unscaledTime;
                _globalCooldownEndTime = now + duration;
                _triggerCooldownEndTimes[type] = now + duration + cooldown;

                if (CSMModOptions.DebugLogging)
                {
                    Debug.Log("[CSM] SlowMo config: target=" + _targetTimeScale.ToString("0.###") +
                              " duration=" + duration.ToString("0.###") +
                              " cooldown=" + cooldown.ToString("0.###") +
                              " easing=" + easingDuration.ToString("0.###") + "s (" + curve + ")" +
                              " endAt=" + _slowMotionEndTime.ToString("0.###") +
                              " now=" + now.ToString("0.###"));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] StartSlowMotion error: " + ex.Message);
                TryRestoreTimeScale();
            }
        }

        public void EndSlowMotion()
        {
            if (!_isSlowMotionActive) return;

            try
            {
                var endedType = _activeTriggerType;
                
                // Check if we should ease out or snap back
                var curve = CSMModOptions.GetEasingCurve(_activeTriggerType);
                bool shouldEaseOut = curve != CSMModOptions.EasingCurve.Off && _easingOutDuration > 0f;
                
                _isSlowMotionActive = false;
                
                if (shouldEaseOut && !_isEasingOut)
                {
                    // Start easing out transition
                    _isEasingOut = true;
                    _targetTimeScale = _originalTimeScale > 0 ? _originalTimeScale : 1f;
                    _transitionStartScale = _currentTimeScale;
                    _transitionStartTime = Time.unscaledTime;
                    _transitionDuration = _easingOutDuration;
                    _isTransitioning = true;
                    
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Starting easing out transition: " + _easingOutDuration.ToString("0.###") + "s");
                }
                else
                {
                    // Snap back immediately
                    _targetTimeScale = _originalTimeScale > 0 ? _originalTimeScale : 1f;
                    _currentTimeScale = _targetTimeScale;
                    _isTransitioning = false;
                    _isEasingOut = false;
                    ApplyTimeScale(_currentTimeScale);
                }

                if (CSMModOptions.DebugLogging)
                {
                    float elapsed = Time.unscaledTime - _slowMotionStartTime;
                    float expected = _slowMotionEndTime - _slowMotionStartTime;
                    Debug.Log("[CSM] SlowMo elapsed: " + elapsed.ToString("0.###") +
                              "s (expected " + expected.ToString("0.###") +
                              "s, delta " + (elapsed - expected).ToString("0.###") + "s)");
                }

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] SlowMo END: " + endedType);
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] EndSlowMotion error: " + ex.Message);
                TryRestoreTimeScale();
            }
        }

        public void CancelSlowMotion()
        {
            if (!_isSlowMotionActive) return;

            try
            {
                float elapsed = Time.unscaledTime - _slowMotionStartTime;
                float expected = _slowMotionEndTime - _slowMotionStartTime;
                _isSlowMotionActive = false;
                _slowMotionEndTime = 0f;

                TryRestoreTimeScale();
                Debug.Log("[CSM] SlowMo cancelled");
                if (CSMModOptions.DebugLogging)
                {
                    Debug.Log("[CSM] SlowMo elapsed (cancel): " + elapsed.ToString("0.###") +
                              "s (expected " + expected.ToString("0.###") +
                              "s, delta " + (elapsed - expected).ToString("0.###") + "s)");
                }
                CSMKillcam.Instance.Stop(false);
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] CancelSlowMotion error: " + ex.Message);
                TryRestoreTimeScale();
            }
        }

        private void TryRestoreTimeScale()
        {
            try
            {
                Time.timeScale = _originalTimeScale > 0 ? _originalTimeScale : 1f;
                Time.fixedDeltaTime = _originalFixedDeltaTime > 0 ? _originalFixedDeltaTime : 0.02f;
            }
            catch
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
        }

        public void Shutdown()
        {
            CancelSlowMotion();
            _instance = null;
        }

        private static string GetTriggerDisplayName(TriggerType type)
        {
            switch (type)
            {
                case TriggerType.BasicKill: return "KILL";
                case TriggerType.Critical: return "CRITICAL!";
                case TriggerType.Dismemberment: return "DISMEMBER!";
                case TriggerType.Decapitation: return "DECAPITATION!";
                case TriggerType.LastEnemy: return "LAST ENEMY!";
                case TriggerType.LastStand: return "LAST STAND!";
                case TriggerType.Parry: return "PARRY!";
                default: return "SLOW MOTION";
            }
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

        private static string FormatValues(float chance, float timeScale, float duration, float cooldown, TriggerType type, float distribution)
        {
            string chanceLabel = (chance * 100f).ToString("F0") + "%";
            string scaleLabel = (timeScale * 100f).ToString("F0") + "%";
            string durationLabel = duration.ToString("F1") + "s";
            string cooldownLabel = cooldown.ToString("F1") + "s";

            string tpLabel;
            if (!CSMModOptions.IsThirdPersonEligible(type))
            {
                tpLabel = "N/A";
            }
            else
            {
                if (distribution <= 0f)
                    tpLabel = "Off";
                else if (distribution >= 99f)
                    tpLabel = "Always";
                else
                    tpLabel = distribution.ToString("0.#") + "x";
            }

            return "Chance " + chanceLabel +
                   " | Scale " + scaleLabel +
                   " | Dur " + durationLabel +
                   " | CD " + cooldownLabel +
                   " | TP " + tpLabel;
        }

        private static void SetLastTriggerDebug(TriggerType type, string reason, bool isQuickTest)
        {
            if (!CSMModOptions.DebugLogging)
                return;

            string summary = GetTriggerUiName(type);
            string finalReason = isQuickTest ? reason + " (Quick Test)" : reason;
            Debug.Log("[CSM] " + summary + " -> " + finalReason);
        }

    }
}
