using System;
using System.Collections.Generic;
using CSM.Configuration;
using ThunderRoad;
using UnityEngine;

namespace CSM.Core
{
    /// <summary>
    /// Core slow motion manager.
    /// </summary>
    public class CSMManager
    {
        private static CSMManager _instance;
        public static CSMManager Instance => _instance ??= new CSMManager();

        private bool _isSlowMotionActive;
        private float _slowMotionEndTime;
        private float _originalTimeScale = 1f;
        private float _originalFixedDeltaTime = 0.02f;
        private TriggerType _activeTriggerType;
        private float _globalCooldownEndTime;
        private readonly Dictionary<TriggerType, float> _triggerCooldownEndTimes = new Dictionary<TriggerType, float>();

        // Smooth transition state
        private bool _isTransitioning;
        private float _targetTimeScale;
        private float _currentTimeScale;
        private bool _transitioningOut;
        private float _transitionInSpeed = 8f;
        private float _transitionOutSpeed = 4f;
        private float _transitionOutStartTime;
        private const float TransitionOutTimeoutSeconds = 5f;
        private const float EndOverrunGraceSeconds = 2f;

        public bool IsActive => _isSlowMotionActive;

        public void Initialize()
        {
            try
            {
                _originalTimeScale = 1f;
                _originalFixedDeltaTime = Time.fixedDeltaTime;
                _isSlowMotionActive = false;
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
                // Handle smooth transitions
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
            float speed = _transitioningOut ? _transitionOutSpeed : _transitionInSpeed;

            // If speed is 0 (instant), skip lerping
            if (speed <= 0f)
            {
                _currentTimeScale = _targetTimeScale;
                _isTransitioning = false;
                ApplyTimeScale(_currentTimeScale);
                return;
            }

            if (_transitioningOut && _transitionOutStartTime > 0f &&
                Time.unscaledTime - _transitionOutStartTime > TransitionOutTimeoutSeconds)
            {
                _currentTimeScale = _targetTimeScale;
                _isTransitioning = false;
                _transitioningOut = false;
                ApplyTimeScale(_currentTimeScale);
                Debug.LogWarning("[CSM] Transition out timed out. Forcing time scale restore.");
                return;
            }

            float delta = _transitioningOut ? Time.deltaTime : Time.unscaledDeltaTime;
            _currentTimeScale = Mathf.Lerp(_currentTimeScale, _targetTimeScale, delta * speed);

            // Check if we've reached the target
            if (Mathf.Abs(_currentTimeScale - _targetTimeScale) < 0.01f)
            {
                _currentTimeScale = _targetTimeScale;
                _isTransitioning = false;
                _transitioningOut = false;

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Transition complete: " + _currentTimeScale);
            }

            // Apply the interpolated time scale
            ApplyTimeScale(_currentTimeScale);
        }

        private void ApplyTimeScale(float scale)
        {
            float clampedScale = Mathf.Clamp(scale, 0.05f, 1f);
            Time.timeScale = clampedScale;
            Time.fixedDeltaTime = _originalFixedDeltaTime * clampedScale;
        }

        private static float ApplyGlobalSmoothing(float smoothing)
        {
            float global = CSMModOptions.GlobalSmoothing;
            if (global < 0f) return smoothing;
            return global;
        }

        public bool TriggerSlow(TriggerType type)
        {
            return TriggerSlow(type, 0f, null);
        }

        public bool TriggerSlow(TriggerType type, float damageDealt)
        {
            return TriggerSlow(type, damageDealt, null);
        }

        public bool TriggerSlow(TriggerType type, float damageDealt, Creature targetCreature, bool isQuickTest = false)
        {
            try
            {
                // Check if mod is enabled
                if (!CSMModOptions.EnableMod)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Mod disabled");
                    SetLastTriggerDebug(type, "Blocked: Mod disabled", isQuickTest);
                    return false;
                }

                // Get config for this trigger type
                bool enabled;
                float chance, timeScale, duration, cooldown, smoothing;
                GetTriggerConfig(type, out enabled, out chance, out timeScale, out duration, out cooldown, out smoothing);
                smoothing = ApplyGlobalSmoothing(smoothing);

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] TriggerSlow(" + type + "): enabled=" + enabled + " chance=" + chance + " timeScale=" + timeScale + " duration=" + duration + " smoothing=" + smoothing);

                if (!enabled)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Trigger disabled");
                    SetLastTriggerDebug(type, "Blocked: Trigger disabled", isQuickTest);
                    return false;
                }

                float now = Time.unscaledTime;

                // Check global cooldown
                if (now < _globalCooldownEndTime)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Global cooldown");
                    SetLastTriggerDebug(type, "Blocked: Global cooldown", isQuickTest);
                    return false;
                }

                // Check trigger-specific cooldown
                if (_triggerCooldownEndTimes.TryGetValue(type, out float triggerCooldownEnd) && now < triggerCooldownEnd)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Trigger cooldown");
                    SetLastTriggerDebug(type, "Blocked: Trigger cooldown", isQuickTest);
                    return false;
                }

                // If slow motion is already active, only allow higher priority triggers
                if (_isSlowMotionActive && (int)type <= (int)_activeTriggerType)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - SlowMo already active");
                    SetLastTriggerDebug(type, "Blocked: SlowMo already active", isQuickTest);
                    return false;
                }

                // Roll for chance
                float roll = UnityEngine.Random.value;
                if (chance < 1.0f && roll > chance)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Chance roll failed (" + (roll*100).ToString("F0") + "% vs " + (chance*100).ToString("F0") + "%)");
                    SetLastTriggerDebug(type, "Blocked: Chance failed (" + (roll * 100f).ToString("F0") + "% > " + (chance * 100f).ToString("F0") + "%)", isQuickTest);
                    return false;
                }

                // Start slow motion
                Debug.Log("[CSM] SlowMo START: " + type + " at " + (timeScale*100).ToString("F0") + "% for " + duration.ToString("F1") + "s");
                StartSlowMotion(type, timeScale, duration, cooldown, damageDealt, smoothing);
                SetLastTriggerDebug(type, "Triggered", isQuickTest);
                
                // Trigger haptic feedback
                TriggerHapticFeedback();
                
                // Log trigger name for visibility
                Debug.Log("[CSM] " + GetTriggerDisplayName(type));

                // Optional killcam (third-person) if enabled by distribution
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

        private void GetTriggerConfig(TriggerType type, out bool enabled, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing)
        {
            // Check if trigger is enabled in the Triggers menu
            enabled = IsTriggerEnabled(type);
            if (!enabled)
            {
                chance = 0f;
                timeScale = 0.5f;
                duration = 1f;
                cooldown = 0f;
                smoothing = 8f;
                return;
            }
            GetCustomTriggerConfig(type, out chance, out timeScale, out duration, out cooldown, out smoothing);
        }

        /// <summary>
        /// Check if a trigger type is enabled in the CSM Triggers menu.
        /// </summary>
        private bool IsTriggerEnabled(TriggerType type)
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

        /// <summary>
        /// Get hardcoded preset values for each trigger type.
        /// Each trigger has unique values tailored to its cinematic importance.
        /// </summary>
        public static void GetPresetValues(CSMModOptions.Preset preset, TriggerType type, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing)
        {
            // Default values
            chance = 0.5f;
            timeScale = 0.25f;
            duration = 1.5f;
            cooldown = 5f;
            smoothing = 8f;

            switch (type)
            {
                case TriggerType.BasicKill:
                    // Basic kills are common - keep subtle
                    chance = 0.25f;
                    duration = 1.0f;
                    cooldown = 5f;
                    smoothing = 8f;
                    timeScale = 0.35f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            timeScale = 0.5f;
                            break;
                        case CSMModOptions.Preset.Standard:
                            timeScale = 0.35f;
                            break;
                        case CSMModOptions.Preset.Dramatic:
                            timeScale = 0.3f;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            timeScale = 0.25f;
                            break;
                        case CSMModOptions.Preset.Epic:
                            timeScale = 0.2f;
                            break;
                    }
                    break;

                case TriggerType.Critical:
                    // Head/throat shots are impactful - more dramatic
                    chance = 0.75f;
                    duration = 1.5f;
                    cooldown = 5f;
                    smoothing = 8f;
                    timeScale = 0.25f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            timeScale = 0.4f;
                            break;
                        case CSMModOptions.Preset.Standard:
                            timeScale = 0.25f;
                            break;
                        case CSMModOptions.Preset.Dramatic:
                            timeScale = 0.2f;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            timeScale = 0.15f;
                            break;
                        case CSMModOptions.Preset.Epic:
                            timeScale = 0.1f;
                            break;
                    }
                    break;

                case TriggerType.Dismemberment:
                    // Limb severing - moderately dramatic
                    chance = 0.6f;
                    duration = 1.5f;
                    cooldown = 5f;
                    smoothing = 8f;
                    timeScale = 0.3f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            timeScale = 0.45f;
                            break;
                        case CSMModOptions.Preset.Standard:
                            timeScale = 0.3f;
                            break;
                        case CSMModOptions.Preset.Dramatic:
                            timeScale = 0.25f;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            timeScale = 0.2f;
                            break;
                        case CSMModOptions.Preset.Epic:
                            timeScale = 0.15f;
                            break;
                    }
                    break;

                case TriggerType.Decapitation:
                    // Decapitation is rare and epic - maximum impact
                    chance = 0.9f;
                    duration = 2.0f;
                    cooldown = 4f;
                    smoothing = 6f;
                    timeScale = 0.2f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            timeScale = 0.35f;
                            break;
                        case CSMModOptions.Preset.Standard:
                            timeScale = 0.2f;
                            break;
                        case CSMModOptions.Preset.Dramatic:
                            timeScale = 0.15f;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            timeScale = 0.1f;
                            break;
                        case CSMModOptions.Preset.Epic:
                            timeScale = 0.08f;
                            break;
                    }
                    break;

                case TriggerType.Parry:
                    // Parries need quick response - shorter duration
                    chance = 0.5f;
                    duration = 1.2f;
                    cooldown = 7f;
                    smoothing = 10f;
                    timeScale = 0.3f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            timeScale = 0.45f;
                            break;
                        case CSMModOptions.Preset.Standard:
                            timeScale = 0.3f;
                            break;
                        case CSMModOptions.Preset.Dramatic:
                            timeScale = 0.25f;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            timeScale = 0.2f;
                            break;
                        case CSMModOptions.Preset.Epic:
                            timeScale = 0.15f;
                            break;
                    }
                    break;

                case TriggerType.LastEnemy:
                    // Final kill of wave - celebratory, dramatic
                    chance = 1.0f;
                    duration = 3.0f;
                    cooldown = 0f;
                    smoothing = 4f;
                    timeScale = 0.2f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            timeScale = 0.35f;
                            break;
                        case CSMModOptions.Preset.Standard:
                            timeScale = 0.2f;
                            break;
                        case CSMModOptions.Preset.Dramatic:
                            timeScale = 0.15f;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            timeScale = 0.1f;
                            break;
                        case CSMModOptions.Preset.Epic:
                            timeScale = 0.08f;
                            break;
                    }
                    break;

                case TriggerType.LastStand:
                    // Near-death experience - intense and prolonged
                    chance = 1.0f; // Always triggers when threshold is met
                    duration = 5.0f;
                    cooldown = 45f;
                    smoothing = 4f;
                    timeScale = 0.15f;
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            timeScale = 0.25f;
                            break;
                        case CSMModOptions.Preset.Standard:
                            timeScale = 0.15f;
                            break;
                        case CSMModOptions.Preset.Dramatic:
                            timeScale = 0.12f;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            timeScale = 0.1f;
                            break;
                        case CSMModOptions.Preset.Epic:
                            timeScale = 0.08f;
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Get trigger config from Custom ModOption settings.
        /// </summary>
        private void GetCustomTriggerConfig(TriggerType type, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing)
        {
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
                    chance = 1.0f; // Always trigger
                    timeScale = CSMModOptions.LastStandTimeScale;
                    duration = CSMModOptions.LastStandDuration;
                    cooldown = CSMModOptions.LastStandCooldown;
                    smoothing = CSMModOptions.LastStandSmoothing;
                    break;
                default:
                    chance = 0f;
                    timeScale = 0.5f;
                    duration = 1f;
                    cooldown = 0f;
                    smoothing = 8f;
                    break;
            }
        }

        private void StartSlowMotion(TriggerType type, float timeScale, float duration, float cooldown, float damageDealt, float smoothing)
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
                _slowMotionEndTime = Time.unscaledTime + duration;
                _transitionOutStartTime = 0f;

                // Dynamic intensity: scale transition-in speed based on damage dealt
                float transitionInSpeed = smoothing;
                var dynamicPreset = CSMModOptions.GetDynamicIntensityPreset();
                if (damageDealt > 0f && dynamicPreset != CSMModOptions.DynamicIntensityPreset.Off && smoothing > 0f)
                {
                    GetDynamicIntensitySettings(dynamicPreset, out float damageForInstant, out float maxSpeedMultiplier, out bool allowInstant);
                    float damageMultiplier = Mathf.Clamp01(damageDealt / damageForInstant);
                    float speedMultiplier = Mathf.Lerp(1f, maxSpeedMultiplier, damageMultiplier);
                    transitionInSpeed = smoothing * speedMultiplier;

                    if (allowInstant && damageMultiplier >= 1f)
                        transitionInSpeed = 0f;

                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Dynamic intensity: preset=" + dynamicPreset + " damage=" + damageDealt + " multiplier=" + damageMultiplier + " transitionIn=" + transitionInSpeed);
                }

                // Set smoothing speeds (use half for exit transition)
                _transitionInSpeed = transitionInSpeed;
                _transitionOutSpeed = smoothing > 0 ? smoothing / 2f : 0f;

                // Start smooth transition
                _targetTimeScale = Mathf.Clamp(timeScale, 0.05f, 1f);
                _isTransitioning = transitionInSpeed > 0f;
                _transitioningOut = false;

                // If no smoothing, apply time scale immediately
                if (transitionInSpeed <= 0f)
                {
                    _currentTimeScale = _targetTimeScale;
                    ApplyTimeScale(_currentTimeScale);
                }

                float now = Time.unscaledTime;
                _globalCooldownEndTime = now + duration + CSMModOptions.GlobalCooldown;
                _triggerCooldownEndTimes[type] = now + duration + cooldown;
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
                _isSlowMotionActive = false;

                // Start smooth transition back to normal
                _targetTimeScale = _originalTimeScale > 0 ? _originalTimeScale : 1f;
                _isTransitioning = true;
                _transitioningOut = true;
                _transitionOutStartTime = Time.unscaledTime;

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
                _isSlowMotionActive = false;
                _slowMotionEndTime = 0f;

                TryRestoreTimeScale();
                Debug.Log("[CSM] SlowMo cancelled");
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

        /// <summary>
        /// Trigger haptic feedback on both controllers when slow motion starts.
        /// </summary>
        private void TriggerHapticFeedback()
        {
            try
            {
                float intensity = CSMModOptions.HapticIntensity;
                if (intensity <= 0f) return;

                var player = ThunderRoad.Player.local;
                if (player == null) return;

                // Haptic pulse on both hands - short burst to signal slow motion activation
                if (player.handLeft?.controlHand != null)
                {
                    player.handLeft.controlHand.HapticShort(intensity);
                }

                if (player.handRight?.controlHand != null)
                {
                    player.handRight.controlHand.HapticShort(intensity);
                }

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Haptic feedback triggered at intensity " + intensity);
            }
            catch (Exception ex)
            {
                if (CSMModOptions.DebugLogging)
                    Debug.LogWarning("[CSM] Haptic feedback failed: " + ex.Message);
            }
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

        private static void SetLastTriggerDebug(TriggerType type, string reason, bool isQuickTest)
        {
            if (!CSMModOptions.DebugLogging)
                return;

            string summary = GetTriggerUiName(type);
            string finalReason = isQuickTest ? reason + " (Quick Test)" : reason;
            Debug.Log("[CSM] " + summary + " -> " + finalReason);
        }

        private static void GetDynamicIntensitySettings(CSMModOptions.DynamicIntensityPreset preset, out float damageForInstant, out float maxSpeedMultiplier, out bool allowInstant)
        {
            switch (preset)
            {
                case CSMModOptions.DynamicIntensityPreset.LowSensitivity:
                    damageForInstant = 250f;
                    maxSpeedMultiplier = 1.5f;
                    allowInstant = false;
                    break;
                case CSMModOptions.DynamicIntensityPreset.MediumSensitivity:
                    damageForInstant = 150f;
                    maxSpeedMultiplier = 2.5f;
                    allowInstant = false;
                    break;
                case CSMModOptions.DynamicIntensityPreset.HighSensitivity:
                    damageForInstant = 90f;
                    maxSpeedMultiplier = 4.0f;
                    allowInstant = true;
                    break;
                default:
                    damageForInstant = 9999f;
                    maxSpeedMultiplier = 1.0f;
                    allowInstant = false;
                    break;
            }
        }
    }
}
