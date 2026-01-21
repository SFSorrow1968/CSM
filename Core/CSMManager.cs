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

        // Killcam state
        private Creature _targetCreature;
        private bool _useThirdPerson;

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

            _currentTimeScale = Mathf.Lerp(_currentTimeScale, _targetTimeScale, Time.unscaledDeltaTime * speed);

            // Check if we've reached the target
            if (Mathf.Abs(_currentTimeScale - _targetTimeScale) < 0.01f)
            {
                _currentTimeScale = _targetTimeScale;
                _isTransitioning = false;

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

        public bool TriggerSlow(TriggerType type)
        {
            return TriggerSlow(type, 0f, null);
        }

        public bool TriggerSlow(TriggerType type, float damageDealt)
        {
            return TriggerSlow(type, damageDealt, null);
        }

        public bool TriggerSlow(TriggerType type, float damageDealt, Creature targetCreature)
        {
            _targetCreature = targetCreature;
            try
            {
                // Check if mod is enabled
                if (!CSMModOptions.EnableMod)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Mod disabled");
                    return false;
                }

                // Get config for this trigger type
                bool enabled;
                float chance, timeScale, duration, cooldown, smoothing;
                bool thirdPerson;
                GetTriggerConfig(type, out enabled, out chance, out timeScale, out duration, out cooldown, out smoothing, out thirdPerson);

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] TriggerSlow(" + type + "): enabled=" + enabled + " chance=" + chance + " timeScale=" + timeScale + " duration=" + duration + " smoothing=" + smoothing + " thirdPerson=" + thirdPerson);

                if (!enabled)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Trigger disabled");
                    return false;
                }

                float now = Time.unscaledTime;

                // Check global cooldown
                if (now < _globalCooldownEndTime)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Global cooldown");
                    return false;
                }

                // Check trigger-specific cooldown
                if (_triggerCooldownEndTimes.TryGetValue(type, out float triggerCooldownEnd) && now < triggerCooldownEnd)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Trigger cooldown");
                    return false;
                }

                // If slow motion is already active, only allow higher priority triggers
                if (_isSlowMotionActive && (int)type <= (int)_activeTriggerType)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - SlowMo already active");
                    return false;
                }

                // Roll for chance
                float roll = UnityEngine.Random.value;
                if (chance < 1.0f && roll > chance)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] TriggerSlow(" + type + "): BLOCKED - Chance roll failed (" + (roll*100).ToString("F0") + "% vs " + (chance*100).ToString("F0") + "%)");
                    return false;
                }

                // Start slow motion
                Debug.Log("[CSM] SlowMo START: " + type + " at " + (timeScale*100).ToString("F0") + "% for " + duration.ToString("F1") + "s");
                StartSlowMotion(type, timeScale, duration, cooldown, damageDealt, smoothing, thirdPerson);
                
                // Trigger haptic feedback
                TriggerHapticFeedback();
                
                // Log trigger name for visibility
                Debug.Log("[CSM] " + GetTriggerDisplayName(type));
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] TriggerSlow error: " + ex.Message);
                return false;
            }
        }

        private void GetTriggerConfig(TriggerType type, out bool enabled, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing, out bool thirdPerson)
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
                thirdPerson = false;
                return;
            }

            var preset = CSMModOptions.GetCurrentPreset();

            // Custom mode: read from per-trigger ModOptions
            if (preset == CSMModOptions.Preset.Custom)
            {
                GetCustomTriggerConfig(type, out chance, out timeScale, out duration, out cooldown, out smoothing, out thirdPerson);
                return;
            }

            // Preset mode: use hardcoded values tailored per trigger
            GetPresetValues(preset, type, out chance, out timeScale, out duration, out cooldown, out smoothing, out thirdPerson);
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
        private void GetPresetValues(CSMModOptions.Preset preset, TriggerType type, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing, out bool thirdPerson)
        {
            // Default values
            chance = 0.5f;
            timeScale = 0.25f;
            duration = 1.5f;
            cooldown = 5f;
            smoothing = 8f;
            thirdPerson = false;

            switch (type)
            {
                case TriggerType.BasicKill:
                    // Basic kills are common - keep subtle
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            chance = 0.15f; timeScale = 0.5f; duration = 0.5f; cooldown = 10f; smoothing = 12f; thirdPerson = false;
                            break;
                        case CSMModOptions.Preset.Balanced:
                            chance = 0.25f; timeScale = 0.35f; duration = 1.0f; cooldown = 5f; smoothing = 8f; thirdPerson = false;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            chance = 0.4f; timeScale = 0.25f; duration = 1.5f; cooldown = 3f; smoothing = 4f; thirdPerson = false;
                            break;
                    }
                    break;

                case TriggerType.Critical:
                    // Head/throat shots are impactful - more dramatic
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            chance = 0.5f; timeScale = 0.4f; duration = 1.0f; cooldown = 8f; smoothing = 12f; thirdPerson = false;
                            break;
                        case CSMModOptions.Preset.Balanced:
                            chance = 0.75f; timeScale = 0.25f; duration = 1.5f; cooldown = 5f; smoothing = 8f; thirdPerson = false;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            chance = 1.0f; timeScale = 0.15f; duration = 2.5f; cooldown = 3f; smoothing = 4f; thirdPerson = true;
                            break;
                    }
                    break;

                case TriggerType.Dismemberment:
                    // Limb severing - moderately dramatic
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            chance = 0.4f; timeScale = 0.45f; duration = 1.0f; cooldown = 8f; smoothing = 12f; thirdPerson = false;
                            break;
                        case CSMModOptions.Preset.Balanced:
                            chance = 0.6f; timeScale = 0.3f; duration = 1.5f; cooldown = 5f; smoothing = 8f; thirdPerson = false;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            chance = 0.85f; timeScale = 0.2f; duration = 2.5f; cooldown = 3f; smoothing = 4f; thirdPerson = false;
                            break;
                    }
                    break;

                case TriggerType.Decapitation:
                    // Decapitation is rare and epic - maximum impact
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            chance = 0.7f; timeScale = 0.35f; duration = 1.5f; cooldown = 5f; smoothing = 8f; thirdPerson = false;
                            break;
                        case CSMModOptions.Preset.Balanced:
                            chance = 0.9f; timeScale = 0.2f; duration = 2.0f; cooldown = 4f; smoothing = 6f; thirdPerson = true;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            chance = 1.0f; timeScale = 0.1f; duration = 3.5f; cooldown = 2f; smoothing = 4f; thirdPerson = true;
                            break;
                    }
                    break;

                case TriggerType.Parry:
                    // Parries need quick response - shorter duration
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            chance = 0.3f; timeScale = 0.45f; duration = 0.8f; cooldown = 10f; smoothing = 12f; thirdPerson = false;
                            break;
                        case CSMModOptions.Preset.Balanced:
                            chance = 0.5f; timeScale = 0.3f; duration = 1.2f; cooldown = 7f; smoothing = 10f; thirdPerson = false;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            chance = 0.75f; timeScale = 0.2f; duration = 1.8f; cooldown = 5f; smoothing = 8f; thirdPerson = false;
                            break;
                    }
                    break;

                case TriggerType.LastEnemy:
                    // Final kill of wave - celebratory, dramatic
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            chance = 0.8f; timeScale = 0.35f; duration = 2.0f; cooldown = 0f; smoothing = 6f; thirdPerson = false;
                            break;
                        case CSMModOptions.Preset.Balanced:
                            chance = 1.0f; timeScale = 0.2f; duration = 3.0f; cooldown = 0f; smoothing = 4f; thirdPerson = true;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            chance = 1.0f; timeScale = 0.1f; duration = 5.0f; cooldown = 0f; smoothing = 2f; thirdPerson = true;
                            break;
                    }
                    break;

                case TriggerType.LastStand:
                    // Near-death experience - intense and prolonged
                    chance = 1.0f; // Always triggers when threshold is met
                    switch (preset)
                    {
                        case CSMModOptions.Preset.Subtle:
                            timeScale = 0.25f; duration = 3.0f; cooldown = 60f; smoothing = 4f; thirdPerson = false;
                            break;
                        case CSMModOptions.Preset.Balanced:
                            timeScale = 0.15f; duration = 5.0f; cooldown = 45f; smoothing = 4f; thirdPerson = false;
                            break;
                        case CSMModOptions.Preset.Cinematic:
                            timeScale = 0.1f; duration = 8.0f; cooldown = 30f; smoothing = 2f; thirdPerson = false;
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Get trigger config from Custom ModOption settings.
        /// </summary>
        private void GetCustomTriggerConfig(TriggerType type, out float chance, out float timeScale, out float duration, out float cooldown, out float smoothing, out bool thirdPerson)
        {
            switch (type)
            {
                case TriggerType.BasicKill:
                    chance = CSMModOptions.BasicKillChance;
                    timeScale = CSMModOptions.BasicKillTimeScale;
                    duration = CSMModOptions.BasicKillDuration;
                    cooldown = CSMModOptions.BasicKillCooldown;
                    smoothing = CSMModOptions.BasicKillSmoothing;
                    thirdPerson = CSMModOptions.BasicKillThirdPerson;
                    break;
                case TriggerType.Critical:
                    chance = CSMModOptions.CriticalKillChance;
                    timeScale = CSMModOptions.CriticalKillTimeScale;
                    duration = CSMModOptions.CriticalKillDuration;
                    cooldown = CSMModOptions.CriticalKillCooldown;
                    smoothing = CSMModOptions.CriticalKillSmoothing;
                    thirdPerson = CSMModOptions.CriticalKillThirdPerson;
                    break;
                case TriggerType.Dismemberment:
                    chance = CSMModOptions.DismembermentChance;
                    timeScale = CSMModOptions.DismembermentTimeScale;
                    duration = CSMModOptions.DismembermentDuration;
                    cooldown = CSMModOptions.DismembermentCooldown;
                    smoothing = CSMModOptions.DismembermentSmoothing;
                    thirdPerson = CSMModOptions.DismembermentThirdPerson;
                    break;
                case TriggerType.Decapitation:
                    chance = CSMModOptions.DecapitationChance;
                    timeScale = CSMModOptions.DecapitationTimeScale;
                    duration = CSMModOptions.DecapitationDuration;
                    cooldown = CSMModOptions.DecapitationCooldown;
                    smoothing = CSMModOptions.DecapitationSmoothing;
                    thirdPerson = CSMModOptions.DecapitationThirdPerson;
                    break;
                case TriggerType.Parry:
                    chance = CSMModOptions.ParryChance;
                    timeScale = CSMModOptions.ParryTimeScale;
                    duration = CSMModOptions.ParryDuration;
                    cooldown = CSMModOptions.ParryCooldown;
                    smoothing = CSMModOptions.ParrySmoothing;
                    thirdPerson = CSMModOptions.ParryThirdPerson;
                    break;
                case TriggerType.LastEnemy:
                    chance = CSMModOptions.LastEnemyChance;
                    timeScale = CSMModOptions.LastEnemyTimeScale;
                    duration = CSMModOptions.LastEnemyDuration;
                    cooldown = CSMModOptions.LastEnemyCooldown;
                    smoothing = CSMModOptions.LastEnemySmoothing;
                    thirdPerson = CSMModOptions.LastEnemyThirdPerson;
                    break;
                case TriggerType.LastStand:
                    chance = 1.0f; // Always trigger
                    timeScale = CSMModOptions.LastStandTimeScale;
                    duration = CSMModOptions.LastStandDuration;
                    cooldown = CSMModOptions.LastStandCooldown;
                    smoothing = CSMModOptions.LastStandSmoothing;
                    thirdPerson = CSMModOptions.LastStandThirdPerson;
                    break;
                default:
                    chance = 0f;
                    timeScale = 0.5f;
                    duration = 1f;
                    cooldown = 0f;
                    smoothing = 8f;
                    thirdPerson = false;
                    break;
            }
        }

        private void StartSlowMotion(TriggerType type, float timeScale, float duration, float cooldown, float damageDealt, float smoothing, bool thirdPerson)
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
                _useThirdPerson = thirdPerson;

                // Set smoothing speeds (use half for exit transition)
                _transitionInSpeed = smoothing;
                _transitionOutSpeed = smoothing > 0 ? smoothing / 2f : 0f;

                // Dynamic intensity: scale time based on damage dealt
                float finalTimeScale = timeScale;
                if (damageDealt > 0f && CSMModOptions.DynamicIntensity)
                {
                    // More damage = slower time (more dramatic)
                    // Scale factor: 100+ damage gets full effect, less damage scales down
                    float damageMultiplier = Mathf.Clamp01(damageDealt / 100f);
                    float intensityBonus = (1f - timeScale) * damageMultiplier * 0.3f; // Up to 30% slower
                    finalTimeScale = Mathf.Max(0.05f, timeScale - intensityBonus);

                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Dynamic intensity: damage=" + damageDealt + " multiplier=" + damageMultiplier + " finalScale=" + finalTimeScale);
                }

                // Start smooth transition
                _targetTimeScale = Mathf.Clamp(finalTimeScale, 0.05f, 1f);
                _isTransitioning = smoothing > 0;
                _transitioningOut = false;

                // If no smoothing, apply time scale immediately
                if (smoothing <= 0)
                {
                    _currentTimeScale = _targetTimeScale;
                    ApplyTimeScale(_currentTimeScale);
                }

                float now = Time.unscaledTime;
                _globalCooldownEndTime = now + duration + CSMModOptions.GlobalCooldown;
                _triggerCooldownEndTimes[type] = now + duration + cooldown;

                // Try to start killcam if target creature is available AND third person is enabled for this trigger
                if (_targetCreature != null && thirdPerson && CSMModOptions.KillcamEnabled)
                {
                    KillcamManager.Instance.TryStartKillcam(_targetCreature, type, duration);
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
                _isSlowMotionActive = false;

                // End killcam if active
                KillcamManager.Instance.EndKillcam();

                // Start smooth transition back to normal
                _targetTimeScale = _originalTimeScale > 0 ? _originalTimeScale : 1f;
                _isTransitioning = true;
                _transitioningOut = true;

                _targetCreature = null;

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
                _targetCreature = null;

                // Force end killcam immediately
                KillcamManager.Instance.ForceEndKillcam();

                TryRestoreTimeScale();
                Debug.Log("[CSM] SlowMo cancelled");
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
    }
}
