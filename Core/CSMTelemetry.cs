using System;
using System.Collections.Generic;
using CSM.Configuration;
using UnityEngine;

namespace CSM.Core
{
    internal static class CSMTelemetry
    {
        private const float SummaryIntervalSeconds = 30f;

        private static string runId = "none";
        private static bool initialized;
        private static float nextSummaryTime;
        private static float sessionStartTime;
        private static int summaryCount;

        private static int killEvaluations;
        private static int killPlayerAttributed;
        private static int parryEvents;
        private static int parryPlayerQualified;
        private static int deflectEvents;
        private static int deflectPlayerQualified;
        private static int lastStandTriggers;
        private static int triggerAttempts;
        private static int triggerSuccesses;
        private static int triggerQuickTests;
        private static int slowmoStarts;
        private static int slowmoEnds;
        private static int slowmoCancels;
        private static int frameDrops;
        private static int severeFrameDrops;
        private static float worstFrameDropMs;
        private static int errorCount;
        private static int deferredQueued;
        private static int deferredExecuted;
        private static int deferredDropped;
        private static int deferredExpired;

        private static int totalKillEvaluations;
        private static int totalKillPlayerAttributed;
        private static int totalParryEvents;
        private static int totalParryPlayerQualified;
        private static int totalDeflectEvents;
        private static int totalDeflectPlayerQualified;
        private static int totalLastStandTriggers;
        private static int totalTriggerAttempts;
        private static int totalTriggerSuccesses;
        private static int totalTriggerQuickTests;
        private static int totalSlowmoStarts;
        private static int totalSlowmoEnds;
        private static int totalSlowmoCancels;
        private static int totalFrameDrops;
        private static int totalSevereFrameDrops;
        private static float totalWorstFrameDropMs;
        private static int totalErrorCount;
        private static int totalDeferredQueued;
        private static int totalDeferredExecuted;
        private static int totalDeferredDropped;
        private static int totalDeferredExpired;

        private static readonly Dictionary<string, int> killSkipReasonsInterval = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> killSkipReasonsTotal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> triggerBlockReasonsInterval = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> triggerBlockReasonsTotal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> triggerBlockByTypeInterval = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> triggerBlockByTypeTotal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> triggerBlockByFamilyInterval = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> triggerBlockByFamilyTotal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> triggerSuccessByTypeInterval = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> triggerSuccessByTypeTotal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> errorReasonsInterval = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> errorReasonsTotal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> deferredReasonsInterval = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> deferredReasonsTotal = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public static void Initialize()
        {
            runId = Guid.NewGuid().ToString("N").Substring(0, 8);
            initialized = true;
            sessionStartTime = Time.unscaledTime;
            nextSummaryTime = sessionStartTime + SummaryIntervalSeconds;
            summaryCount = 0;
            ResetInterval();
            ResetTotals();

            if (CSMModOptions.DebugLogging)
            {
                Debug.Log(
                    "[CSM] diag evt=session_start run=" + runId +
                    " preset=" + CSMModOptions.CurrentPreset +
                    " chancePreset=" + CSMModOptions.ChancePresetSetting +
                    " cooldownPreset=" + CSMModOptions.CooldownPresetSetting +
                    " durationPreset=" + CSMModOptions.DurationPresetSetting +
                    " deferredQueue=off");
            }
        }

        public static void Shutdown()
        {
            if (!initialized)
            {
                return;
            }

            EmitSummary(force: true);
            EmitSessionTotals();
            if (CSMModOptions.DebugLogging)
            {
                Debug.Log(
                    "[CSM] diag evt=session_end run=" + runId +
                    " uptimeSec=" + Mathf.Max(0f, Time.unscaledTime - sessionStartTime).ToString("F1") +
                    " summaryCount=" + summaryCount);
            }
            initialized = false;
        }

        public static void Update(float now)
        {
            if (!initialized || now < nextSummaryTime)
            {
                return;
            }

            EmitSummary(force: false);
            nextSummaryTime = now + SummaryIntervalSeconds;
        }

        public static void RecordKillEvaluation(bool playerAttributed)
        {
            if (!initialized)
            {
                return;
            }

            killEvaluations++;
            totalKillEvaluations++;
            if (playerAttributed)
            {
                killPlayerAttributed++;
                totalKillPlayerAttributed++;
            }
        }

        public static void RecordKillSkip(string reason)
        {
            if (!initialized)
            {
                return;
            }

            Increment(killSkipReasonsInterval, reason);
            Increment(killSkipReasonsTotal, reason);
        }

        public static void RecordParryEvaluation(bool playerQualified)
        {
            if (!initialized)
            {
                return;
            }

            parryEvents++;
            totalParryEvents++;
            if (playerQualified)
            {
                parryPlayerQualified++;
                totalParryPlayerQualified++;
            }
        }

        public static void RecordDeflectEvaluation(bool playerQualified)
        {
            if (!initialized)
            {
                return;
            }

            deflectEvents++;
            totalDeflectEvents++;
            if (playerQualified)
            {
                deflectPlayerQualified++;
                totalDeflectPlayerQualified++;
            }
        }

        public static void RecordLastStandTrigger()
        {
            if (!initialized)
            {
                return;
            }

            lastStandTriggers++;
            totalLastStandTriggers++;
        }

        public static void RecordTriggerAttempt(TriggerType type, TriggerResult result, bool isQuickTest)
        {
            if (!initialized)
            {
                return;
            }

            triggerAttempts++;
            totalTriggerAttempts++;
            if (isQuickTest)
            {
                triggerQuickTests++;
                totalTriggerQuickTests++;
            }

            if (result == TriggerResult.Success)
            {
                triggerSuccesses++;
                totalTriggerSuccesses++;
                Increment(triggerSuccessByTypeInterval, type.ToString());
                Increment(triggerSuccessByTypeTotal, type.ToString());
            }
            else
            {
                string reason = result.ToString();
                Increment(triggerBlockReasonsInterval, reason);
                Increment(triggerBlockReasonsTotal, reason);
                Increment(triggerBlockByTypeInterval, type + "_" + reason);
                Increment(triggerBlockByTypeTotal, type + "_" + reason);
                string family = GetTriggerFamily(type);
                Increment(triggerBlockByFamilyInterval, family + "_" + reason);
                Increment(triggerBlockByFamilyTotal, family + "_" + reason);
            }
        }

        public static void RecordDeferredQueued(TriggerType type, TriggerResult blockedReason)
        {
            if (!initialized)
            {
                return;
            }

            deferredQueued++;
            totalDeferredQueued++;
            Increment(deferredReasonsInterval, "queued_" + type + "_" + blockedReason);
            Increment(deferredReasonsTotal, "queued_" + type + "_" + blockedReason);
        }

        public static void RecordDeferredExecuted(TriggerType type)
        {
            if (!initialized)
            {
                return;
            }

            deferredExecuted++;
            totalDeferredExecuted++;
            Increment(deferredReasonsInterval, "executed_" + type);
            Increment(deferredReasonsTotal, "executed_" + type);
        }

        public static void RecordDeferredDropped(TriggerType type, string reason)
        {
            if (!initialized)
            {
                return;
            }

            deferredDropped++;
            totalDeferredDropped++;
            string safeReason = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason;
            Increment(deferredReasonsInterval, "dropped_" + type + "_" + safeReason);
            Increment(deferredReasonsTotal, "dropped_" + type + "_" + safeReason);
        }

        public static void RecordDeferredExpired(TriggerType type)
        {
            if (!initialized)
            {
                return;
            }

            deferredExpired++;
            totalDeferredExpired++;
            Increment(deferredReasonsInterval, "expired_" + type);
            Increment(deferredReasonsTotal, "expired_" + type);
        }

        public static void RecordSlowmoStart(TriggerType type)
        {
            if (!initialized)
            {
                return;
            }

            slowmoStarts++;
            totalSlowmoStarts++;
            Increment(triggerSuccessByTypeInterval, "start_" + type);
            Increment(triggerSuccessByTypeTotal, "start_" + type);
        }

        public static void RecordSlowmoEnd(bool cancelled)
        {
            if (!initialized)
            {
                return;
            }

            if (cancelled)
            {
                slowmoCancels++;
                totalSlowmoCancels++;
            }
            else
            {
                slowmoEnds++;
                totalSlowmoEnds++;
            }
        }

        public static void RecordFrameDrop(float frameTimeMs, bool severe)
        {
            if (!initialized)
            {
                return;
            }

            frameDrops++;
            totalFrameDrops++;
            if (severe)
            {
                severeFrameDrops++;
                totalSevereFrameDrops++;
            }
            if (frameTimeMs > worstFrameDropMs)
            {
                worstFrameDropMs = frameTimeMs;
            }
            if (frameTimeMs > totalWorstFrameDropMs)
            {
                totalWorstFrameDropMs = frameTimeMs;
            }
        }

        public static void RecordError(string context)
        {
            if (!initialized)
            {
                return;
            }

            errorCount++;
            totalErrorCount++;
            string key = string.IsNullOrWhiteSpace(context) ? "unknown" : context;
            Increment(errorReasonsInterval, key);
            Increment(errorReasonsTotal, key);
        }

        private static void EmitSummary(bool force)
        {
            if (!force &&
                killEvaluations == 0 &&
                parryEvents == 0 &&
                deflectEvents == 0 &&
                triggerAttempts == 0 &&
                slowmoStarts == 0 &&
                frameDrops == 0 &&
                errorCount == 0)
            {
                return;
            }

            summaryCount++;
            float triggerRate = triggerAttempts > 0
                ? (triggerSuccesses * 100f) / triggerAttempts
                : 0f;

            if (CSMModOptions.DebugLogging)
            {
                Debug.Log(
                    "[CSM] diag evt=summary run=" + runId +
                    " intervalSec=" + SummaryIntervalSeconds.ToString("F0") +
                    " killEval=" + killEvaluations +
                    " killPlayer=" + killPlayerAttributed +
                    " parry=" + parryEvents +
                    " parryPlayer=" + parryPlayerQualified +
                    " deflect=" + deflectEvents +
                    " deflectPlayer=" + deflectPlayerQualified +
                    " lastStand=" + lastStandTriggers +
                    " triggerTry=" + triggerAttempts +
                    " triggerOk=" + triggerSuccesses +
                    " triggerRate=" + triggerRate.ToString("F1") + "%" +
                    " quickTests=" + triggerQuickTests +
                    " slowStart=" + slowmoStarts +
                    " slowEnd=" + slowmoEnds +
                    " slowCancel=" + slowmoCancels +
                    " frameDrop=" + frameDrops +
                    " severeDrop=" + severeFrameDrops +
                    " worstDropMs=" + worstFrameDropMs.ToString("F1") +
                    " deferredQueued=" + deferredQueued +
                    " deferredExecuted=" + deferredExecuted +
                    " deferredDropped=" + deferredDropped +
                    " deferredExpired=" + deferredExpired +
                    " errors=" + errorCount +
                    " topKillSkips=" + FormatTop(killSkipReasonsInterval) +
                    " topTriggerBlocks=" + FormatTop(triggerBlockReasonsInterval) +
                    " topTriggerBlocksByType=" + FormatTop(triggerBlockByTypeInterval) +
                    " topTriggerBlocksByFamily=" + FormatTop(triggerBlockByFamilyInterval) +
                    " topTriggerOk=" + FormatTop(triggerSuccessByTypeInterval) +
                    " topDeferred=" + FormatTop(deferredReasonsInterval) +
                    " topErrors=" + FormatTop(errorReasonsInterval));
            }

            ResetInterval();
        }

        private static void EmitSessionTotals()
        {
            float uptime = Mathf.Max(0f, Time.unscaledTime - sessionStartTime);
            float triggerRate = totalTriggerAttempts > 0
                ? (totalTriggerSuccesses * 100f) / totalTriggerAttempts
                : 0f;

            if (CSMModOptions.DebugLogging)
            {
                Debug.Log(
                    "[CSM] diag evt=session_totals run=" + runId +
                    " uptimeSec=" + uptime.ToString("F1") +
                    " summaryCount=" + summaryCount +
                    " killEval=" + totalKillEvaluations +
                    " killPlayer=" + totalKillPlayerAttributed +
                    " parry=" + totalParryEvents +
                    " parryPlayer=" + totalParryPlayerQualified +
                    " deflect=" + totalDeflectEvents +
                    " deflectPlayer=" + totalDeflectPlayerQualified +
                    " lastStand=" + totalLastStandTriggers +
                    " triggerTry=" + totalTriggerAttempts +
                    " triggerOk=" + totalTriggerSuccesses +
                    " triggerRate=" + triggerRate.ToString("F1") + "%" +
                    " quickTests=" + totalTriggerQuickTests +
                    " slowStart=" + totalSlowmoStarts +
                    " slowEnd=" + totalSlowmoEnds +
                    " slowCancel=" + totalSlowmoCancels +
                    " frameDrop=" + totalFrameDrops +
                    " severeDrop=" + totalSevereFrameDrops +
                    " worstDropMs=" + totalWorstFrameDropMs.ToString("F1") +
                    " deferredQueued=" + totalDeferredQueued +
                    " deferredExecuted=" + totalDeferredExecuted +
                    " deferredDropped=" + totalDeferredDropped +
                    " deferredExpired=" + totalDeferredExpired +
                    " errors=" + totalErrorCount +
                    " topKillSkips=" + FormatTop(killSkipReasonsTotal) +
                    " topTriggerBlocks=" + FormatTop(triggerBlockReasonsTotal) +
                    " topTriggerBlocksByType=" + FormatTop(triggerBlockByTypeTotal) +
                    " topTriggerBlocksByFamily=" + FormatTop(triggerBlockByFamilyTotal) +
                    " topTriggerOk=" + FormatTop(triggerSuccessByTypeTotal) +
                    " topDeferred=" + FormatTop(deferredReasonsTotal) +
                    " topErrors=" + FormatTop(errorReasonsTotal));
            }
        }

        private static void ResetInterval()
        {
            killEvaluations = 0;
            killPlayerAttributed = 0;
            parryEvents = 0;
            parryPlayerQualified = 0;
            deflectEvents = 0;
            deflectPlayerQualified = 0;
            lastStandTriggers = 0;
            triggerAttempts = 0;
            triggerSuccesses = 0;
            triggerQuickTests = 0;
            slowmoStarts = 0;
            slowmoEnds = 0;
            slowmoCancels = 0;
            frameDrops = 0;
            severeFrameDrops = 0;
            worstFrameDropMs = 0f;
            errorCount = 0;
            deferredQueued = 0;
            deferredExecuted = 0;
            deferredDropped = 0;
            deferredExpired = 0;
            killSkipReasonsInterval.Clear();
            triggerBlockReasonsInterval.Clear();
            triggerBlockByTypeInterval.Clear();
            triggerBlockByFamilyInterval.Clear();
            triggerSuccessByTypeInterval.Clear();
            errorReasonsInterval.Clear();
            deferredReasonsInterval.Clear();
        }

        private static void ResetTotals()
        {
            totalKillEvaluations = 0;
            totalKillPlayerAttributed = 0;
            totalParryEvents = 0;
            totalParryPlayerQualified = 0;
            totalDeflectEvents = 0;
            totalDeflectPlayerQualified = 0;
            totalLastStandTriggers = 0;
            totalTriggerAttempts = 0;
            totalTriggerSuccesses = 0;
            totalTriggerQuickTests = 0;
            totalSlowmoStarts = 0;
            totalSlowmoEnds = 0;
            totalSlowmoCancels = 0;
            totalFrameDrops = 0;
            totalSevereFrameDrops = 0;
            totalWorstFrameDropMs = 0f;
            totalErrorCount = 0;
            totalDeferredQueued = 0;
            totalDeferredExecuted = 0;
            totalDeferredDropped = 0;
            totalDeferredExpired = 0;
            killSkipReasonsTotal.Clear();
            triggerBlockReasonsTotal.Clear();
            triggerBlockByTypeTotal.Clear();
            triggerBlockByFamilyTotal.Clear();
            triggerSuccessByTypeTotal.Clear();
            errorReasonsTotal.Clear();
            deferredReasonsTotal.Clear();
        }

        private static void Increment(Dictionary<string, int> map, string key)
        {
            if (map == null)
            {
                return;
            }

            string safeKey = string.IsNullOrWhiteSpace(key) ? "unknown" : key.Trim().ToLowerInvariant();
            if (map.TryGetValue(safeKey, out int count))
            {
                map[safeKey] = count + 1;
            }
            else
            {
                map[safeKey] = 1;
            }
        }

        private static string FormatTop(Dictionary<string, int> map)
        {
            if (map == null || map.Count == 0)
            {
                return "none";
            }

            List<KeyValuePair<string, int>> pairs = new List<KeyValuePair<string, int>>(map);
            pairs.Sort((a, b) => b.Value.CompareTo(a.Value));

            int take = Mathf.Min(6, pairs.Count);
            string result = string.Empty;
            for (int i = 0; i < take; i++)
            {
                if (i > 0)
                {
                    result += "|";
                }

                result += pairs[i].Key + ":" + pairs[i].Value;
            }

            return result;
        }

        private static string GetTriggerFamily(TriggerType type)
        {
            switch (type)
            {
                case TriggerType.Parry:
                    return "parry";
                case TriggerType.LastStand:
                    return "survival";
                default:
                    return "kill";
            }
        }
    }
}
