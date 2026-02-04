using System;
using CSM.Configuration;
using UnityEngine;

namespace CSM.Core
{
    /// <summary>
    /// Debug logging helper for CSM state information.
    /// Logs to console when DebugOverlay option is enabled.
    /// Note: Visual overlay removed due to missing UnityEngine.IMGUIModule reference.
    /// </summary>
    public class DebugOverlay
    {
        private static DebugOverlay _instance;
        public static DebugOverlay Instance => _instance ??= new DebugOverlay();

        private float _lastLogTime;
        private const float LOG_INTERVAL = 2f; // Log every 2 seconds max

        public void Initialize()
        {
            _lastLogTime = 0f;
            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] DebugOverlay initialized (console mode)");
        }

        /// <summary>
        /// Called from OnGUI - logs state periodically when overlay is enabled.
        /// </summary>
        public void Draw()
        {
            if (!CSMModOptions.DebugOverlay) return;

            // Rate-limit logging to avoid spam
            if (Time.unscaledTime - _lastLogTime < LOG_INTERVAL) return;
            _lastLogTime = Time.unscaledTime;

            try
            {
                var manager = CSMManager.Instance;
                bool isActive = manager.IsActive;

                if (isActive)
                {
                    // Only log detailed info when slow motion is active
                    var perf = PerformanceMetrics.Instance;
                    Debug.Log($"[CSM] Overlay: ACTIVE | TimeScale={Time.timeScale:P0} | {perf.GetOverlaySummary()}");
                }
            }
            catch (Exception ex)
            {
                if (CSMModOptions.DebugLogging)
                    Debug.LogError($"[CSM] DebugOverlay error: {ex.Message}");
            }
        }

        private int CountActiveTriggers()
        {
            int count = 0;
            if (CSMModOptions.EnableBasicKill) count++;
            if (CSMModOptions.EnableCriticalKill) count++;
            if (CSMModOptions.EnableDismemberment) count++;
            if (CSMModOptions.EnableDecapitation) count++;
            if (CSMModOptions.EnableLastEnemy) count++;
            if (CSMModOptions.EnableLastStand) count++;
            if (CSMModOptions.EnableParry) count++;
            return count;
        }

        public void Shutdown()
        {
            _instance = null;
        }
    }
}
