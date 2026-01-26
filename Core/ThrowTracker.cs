using System.Collections.Generic;
using CSM.Configuration;
using ThunderRoad;
using UnityEngine;

namespace CSM.Core
{
    public static class ThrowTracker
    {
        private class ThrowState
        {
            public float ReleaseTime;
            public int LastImpactFrame = -1;
            public float LastImpactTime = -1f;
        }

        private static readonly Dictionary<int, ThrowState> RecentThrownCreatures = new Dictionary<int, ThrowState>();
        private static float _lastCleanupTime = 0f;
        private const float CleanupInterval = 5f;
        private const float MaxThrowAgeSeconds = 10f;

        public static void Reset()
        {
            RecentThrownCreatures.Clear();
            _lastCleanupTime = 0f;
        }

        public static void RecordThrow(Creature creature, string source)
        {
            if (creature == null || creature.isPlayer) return;
            int id = creature.GetInstanceID();
            if (!RecentThrownCreatures.TryGetValue(id, out ThrowState state))
            {
                state = new ThrowState();
                RecentThrownCreatures[id] = state;
            }

            state.ReleaseTime = Time.unscaledTime;
            state.LastImpactFrame = -1;
            state.LastImpactTime = -1f;
            Cleanup(Time.unscaledTime);
            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Thrown release recorded (" + source + "): " + creature.name);
        }

        public static void RecordImpact(Creature creature)
        {
            if (creature == null) return;
            if (!CSMModOptions.EnableBasicKill || !CSMModOptions.EnableThrownImpactKill)
                return;

            int id = creature.GetInstanceID();
            if (!RecentThrownCreatures.TryGetValue(id, out ThrowState state))
                return;

            state.LastImpactFrame = Time.frameCount;
            state.LastImpactTime = Time.unscaledTime;
            Cleanup(Time.unscaledTime);
            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Thrown impact frame recorded: " + creature.name + " frame=" + state.LastImpactFrame);
        }

        public static bool WasImpactThisFrame(Creature creature)
        {
            if (creature == null) return false;
            if (!CSMModOptions.EnableBasicKill || !CSMModOptions.EnableThrownImpactKill)
                return false;

            int id = creature.GetInstanceID();
            if (!RecentThrownCreatures.TryGetValue(id, out ThrowState state))
                return false;

            if (state.LastImpactFrame != Time.frameCount)
                return false;

            RecentThrownCreatures.Remove(id);
            return true;
        }

        private static void Cleanup(float now)
        {
            if (now - _lastCleanupTime < CleanupInterval)
                return;

            _lastCleanupTime = now;
            List<int> expired = null;
            foreach (var kvp in RecentThrownCreatures)
            {
                if (now - kvp.Value.ReleaseTime > MaxThrowAgeSeconds)
                {
                    if (expired == null) expired = new List<int>();
                    expired.Add(kvp.Key);
                }
            }

            if (expired == null) return;
            foreach (var key in expired)
                RecentThrownCreatures.Remove(key);
        }
    }
}
