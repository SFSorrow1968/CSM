using System;
using System.Collections.Generic;
using CSM.Configuration;
using UnityEngine;

namespace CSM.Core
{
    /// <summary>
    /// Tracks performance metrics during slow motion effects.
    /// Monitors frame times and logs warnings when performance degrades.
    /// </summary>
    public class PerformanceMetrics
    {
        private static PerformanceMetrics _instance;
        public static PerformanceMetrics Instance => _instance ??= new PerformanceMetrics();

        // Frame time tracking
        private readonly List<float> _frameTimeSamples = new List<float>(128);
        private float _sessionStartTime;
        private bool _isTracking;
        private int _frameDropCount;
        private float _worstFrameTime;
        private float _baselineFrameTime;

        // Configurable thresholds
        private const float FRAME_DROP_THRESHOLD_MS = 50f; // 50ms = 20 FPS, considered a frame drop
        private const float SEVERE_FRAME_DROP_MS = 100f;   // 100ms = 10 FPS, severe performance issue
        private const int BASELINE_SAMPLE_COUNT = 30;      // Samples to establish baseline before slow-mo
        private const int WARNING_FRAME_DROP_COUNT = 3;    // Log warning after this many drops

        // Rolling baseline tracking (pre-slow-mo performance)
        private readonly Queue<float> _baselineSamples = new Queue<float>(BASELINE_SAMPLE_COUNT);

        // Session stats
        public float AverageFrameTimeMs { get; private set; }
        public float WorstFrameTimeMs => _worstFrameTime * 1000f;
        public int FrameDropCount => _frameDropCount;
        public bool IsTracking => _isTracking;

        public void Initialize()
        {
            _frameTimeSamples.Clear();
            _baselineSamples.Clear();
            _isTracking = false;
            _frameDropCount = 0;
            _worstFrameTime = 0f;
            _baselineFrameTime = 0.016f; // Assume 60 FPS baseline

            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] PerformanceMetrics initialized");
        }

        /// <summary>
        /// Call every frame to track baseline performance when not in slow motion.
        /// </summary>
        public void UpdateBaseline()
        {
            if (_isTracking) return;

            float frameTime = Time.unscaledDeltaTime;

            // Maintain rolling baseline
            if (_baselineSamples.Count >= BASELINE_SAMPLE_COUNT)
                _baselineSamples.Dequeue();
            _baselineSamples.Enqueue(frameTime);

            // Calculate running average
            if (_baselineSamples.Count > 0)
            {
                float sum = 0f;
                foreach (var sample in _baselineSamples)
                    sum += sample;
                _baselineFrameTime = sum / _baselineSamples.Count;
            }
        }

        /// <summary>
        /// Start tracking performance for a slow motion session.
        /// </summary>
        public void StartSession()
        {
            _frameTimeSamples.Clear();
            _sessionStartTime = Time.unscaledTime;
            _isTracking = true;
            _frameDropCount = 0;
            _worstFrameTime = 0f;
            AverageFrameTimeMs = 0f;

            if (CSMModOptions.DebugLogging)
                Debug.Log($"[CSM] Performance tracking started | Baseline: {_baselineFrameTime * 1000f:F1}ms ({1f / _baselineFrameTime:F0} FPS)");
        }

        /// <summary>
        /// Record a frame during slow motion. Call this every frame while slow-mo is active.
        /// </summary>
        public void RecordFrame()
        {
            if (!_isTracking) return;

            float frameTime = Time.unscaledDeltaTime;
            float frameTimeMs = frameTime * 1000f;

            _frameTimeSamples.Add(frameTime);

            if (frameTime > _worstFrameTime)
                _worstFrameTime = frameTime;

            // Check for frame drops (relative to baseline, accounting for slow-mo overhead)
            // During slow-mo, expect some overhead, so use absolute threshold
            if (frameTimeMs > FRAME_DROP_THRESHOLD_MS)
            {
                _frameDropCount++;
                bool severeDrop = frameTimeMs > SEVERE_FRAME_DROP_MS;
                CSMTelemetry.RecordFrameDrop(frameTimeMs, severeDrop);

                if (_frameDropCount == WARNING_FRAME_DROP_COUNT && CSMModOptions.DebugLogging)
                {
                    Debug.LogWarning($"[CSM] Performance: {_frameDropCount} frame drops detected during slow motion");
                }

                if (severeDrop && CSMModOptions.DebugLogging)
                {
                    Debug.LogWarning($"[CSM] Severe frame drop: {frameTimeMs:F1}ms ({1000f / frameTimeMs:F0} FPS)");
                }
            }
        }

        /// <summary>
        /// End the performance tracking session and calculate final metrics.
        /// </summary>
        public void EndSession()
        {
            if (!_isTracking) return;

            _isTracking = false;

            // Calculate average frame time
            if (_frameTimeSamples.Count > 0)
            {
                float sum = 0f;
                for (int i = 0; i < _frameTimeSamples.Count; i++)
                    sum += _frameTimeSamples[i];
                AverageFrameTimeMs = (sum / _frameTimeSamples.Count) * 1000f;
            }

            float sessionDuration = Time.unscaledTime - _sessionStartTime;

            if (CSMModOptions.DebugLogging)
            {
                float avgFps = AverageFrameTimeMs > 0.0001f ? 1000f / AverageFrameTimeMs : 0f;
                float dropRate = _frameTimeSamples.Count > 0
                    ? (float)_frameDropCount / _frameTimeSamples.Count * 100f
                    : 0f;

                Debug.Log(
                    $"[CSM] Performance session ended | Duration={sessionDuration:F2}s Frames={_frameTimeSamples.Count} " +
                    $"Avg={AverageFrameTimeMs:F1}ms ({avgFps:F0} FPS) Worst={WorstFrameTimeMs:F1}ms Drops={_frameDropCount} ({dropRate:F1}%)");
            }
        }

        /// <summary>
        /// Get a summary string for the debug overlay.
        /// </summary>
        public string GetOverlaySummary()
        {
            if (_isTracking)
            {
                float currentFps = 1f / Time.unscaledDeltaTime;
                return $"FPS: {currentFps:F0} | Drops: {_frameDropCount}";
            }
            else
            {
                float baselineFps = 1f / _baselineFrameTime;
                return $"Baseline: {baselineFps:F0} FPS";
            }
        }

        public void Shutdown()
        {
            _frameTimeSamples.Clear();
            _baselineSamples.Clear();
            _isTracking = false;
            _instance = null;
        }
    }
}
