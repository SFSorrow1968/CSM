using System;
using CSM.Configuration;
using UnityEngine;

namespace CSM.Core
{
    /// <summary>
    /// Visual in-game debug overlay using Unity IMGUI.
    /// Displays CSM state, time scale, and performance metrics.
    /// </summary>
    public class DebugOverlay
    {
        private static DebugOverlay _instance;
        public static DebugOverlay Instance => _instance ??= new DebugOverlay();

        // GUI styling
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private Texture2D _backgroundTexture;
        private bool _stylesInitialized;

        // Display state
        private Rect _windowRect = new Rect(10, 10, 280, 150);
        private const int WINDOW_ID = 91827; // Unique ID for CSM overlay

        public void Initialize()
        {
            _stylesInitialized = false;
            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] DebugOverlay initialized (IMGUI mode)");
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            try
            {
                // Create semi-transparent background
                _backgroundTexture = new Texture2D(1, 1);
                _backgroundTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.15f, 0.85f));
                _backgroundTexture.Apply();

                // Box style for window background
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.normal.background = _backgroundTexture;
                _boxStyle.padding = new RectOffset(10, 10, 10, 10);

                // Label style
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.fontSize = 12;
                _labelStyle.normal.textColor = Color.white;
                _labelStyle.richText = true;

                _stylesInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CSM] DebugOverlay style init failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Called from OnGUI - draws the debug overlay when enabled.
        /// </summary>
        public void Draw()
        {
            if (!CSMModOptions.DebugOverlay) return;

            try
            {
                InitializeStyles();
                if (!_stylesInitialized) return;

                _windowRect = GUILayout.Window(WINDOW_ID, _windowRect, DrawWindow, "", _boxStyle);
            }
            catch (Exception ex)
            {
                if (CSMModOptions.DebugLogging)
                    Debug.LogError($"[CSM] DebugOverlay Draw error: {ex.Message}");
            }
        }

        private void DrawWindow(int windowId)
        {
            try
            {
                var manager = CSMManager.Instance;
                bool isActive = manager?.IsActive ?? false;

                // Header
                GUILayout.Label("<color=#66ccff><b>CSM Debug</b></color>", _labelStyle);
                GUILayout.Space(4);

                // Status
                string status = isActive ? "<color=#44ff44>● SLOW-MO ACTIVE</color>" : "<color=#888888>○ Inactive</color>";
                GUILayout.Label(status, _labelStyle);
                GUILayout.Space(2);

                // Time scale
                float timeScale = Time.timeScale;
                string timeColor = timeScale < 0.5f ? "#ffff44" : (timeScale < 1f ? "#ffaa44" : "#ffffff");
                GUILayout.Label($"TimeScale: <color={timeColor}>{timeScale:P0}</color>", _labelStyle);

                // Frame time
                float frameMs = Time.unscaledDeltaTime * 1000f;
                string frameColor = frameMs > 16.67f ? "#ff4444" : "#44ff44";
                GUILayout.Label($"Frame: <color={frameColor}>{frameMs:F1}ms</color>", _labelStyle);

                GUILayout.Space(4);

                // Performance metrics summary
                var perf = PerformanceMetrics.Instance;
                if (perf != null)
                {
                    GUILayout.Label("<color=#aaddff>Performance</color>", _labelStyle);
                    GUILayout.Label(perf.GetOverlaySummary(), _labelStyle);
                }

                // Make window draggable
                GUI.DragWindow();
            }
            catch (Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", _labelStyle);
            }
        }

        public void Shutdown()
        {
            if (_backgroundTexture != null)
            {
                UnityEngine.Object.Destroy(_backgroundTexture);
                _backgroundTexture = null;
            }
            _stylesInitialized = false;
            _instance = null;
        }
    }
}
