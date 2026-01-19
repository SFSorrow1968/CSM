using System;
using System.Collections.Generic;
using CSM.Configuration;
using UnityEngine;

namespace CSM.Core
{
    public class CSMManager
    {
        private static CSMManager _instance;
        public static CSMManager Instance => _instance ??= new CSMManager();

        private bool _isSlowMotionActive;
        private float _slowMotionTimer;
        private float _originalTimeScale;
        private float _originalFixedDeltaTime;
        private TriggerType _activeTriggerType;
        private float _globalCooldownTimer;
        private readonly Dictionary<TriggerType, float> _triggerCooldowns = new();

        public bool IsActive => _isSlowMotionActive;
        public TriggerType ActiveTrigger => _activeTriggerType;

        public void Initialize()
        {
            _originalTimeScale = Time.timeScale;
            _originalFixedDeltaTime = Time.fixedDeltaTime;
            _isSlowMotionActive = false;
            _globalCooldownTimer = 0f;
            _triggerCooldowns.Clear();
            foreach (TriggerType type in Enum.GetValues(typeof(TriggerType)))
                _triggerCooldowns[type] = 0f;
            Debug.Log("[CSM] Manager initialized");
        }

        public void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_globalCooldownTimer > 0) _globalCooldownTimer -= dt;
            foreach (TriggerType type in Enum.GetValues(typeof(TriggerType)))
                if (_triggerCooldowns[type] > 0) _triggerCooldowns[type] -= dt;

            if (_isSlowMotionActive)
            {
                _slowMotionTimer -= dt;
                if (_slowMotionTimer <= 0) EndSlowMotion();
            }
        }

        public bool TriggerSlow(TriggerType type)
        {
            var settings = CSMSettings.Instance;
            if (!settings.Enabled) return false;

            var ts = settings.Get(type);
            if (!ts.Enabled) return false;
            if (_globalCooldownTimer > 0) return false;
            if (_triggerCooldowns[type] > 0) return false;
            if (_isSlowMotionActive && (int)type <= (int)_activeTriggerType) return false;
            if (ts.Chance < 1.0f && UnityEngine.Random.value > ts.Chance) return false;

            StartSlowMotion(type, ts);
            return true;
        }

        private void StartSlowMotion(TriggerType type, TriggerSettings ts)
        {
            if (!_isSlowMotionActive)
            {
                _originalTimeScale = Time.timeScale;
                _originalFixedDeltaTime = Time.fixedDeltaTime;
            }
            _isSlowMotionActive = true;
            _activeTriggerType = type;
            _slowMotionTimer = ts.Duration;
            Time.timeScale = Mathf.Clamp(ts.TimeScale, 0.01f, 1f);
            Time.fixedDeltaTime = _originalFixedDeltaTime * Time.timeScale;
            _globalCooldownTimer = CSMSettings.Instance.GlobalCooldown;
            _triggerCooldowns[type] = ts.Cooldown;
            Debug.Log($"[CSM] SlowMo: {type} scale={ts.TimeScale} dur={ts.Duration}s");
        }

        public void EndSlowMotion()
        {
            if (!_isSlowMotionActive) return;
            _isSlowMotionActive = false;
            Time.timeScale = _originalTimeScale;
            Time.fixedDeltaTime = _originalFixedDeltaTime;
            Debug.Log($"[CSM] SlowMo ended: {_activeTriggerType}");
        }

        public void CancelSlowMotion()
        {
            if (!_isSlowMotionActive) return;
            _isSlowMotionActive = false;
            _slowMotionTimer = 0f;
            Time.timeScale = _originalTimeScale;
            Time.fixedDeltaTime = _originalFixedDeltaTime;
        }
    }
}
