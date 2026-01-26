using System.Collections.Generic;
using CSM.Configuration;
using ThunderRoad;
using UnityEngine;

namespace CSM.Core
{
    public static class ThrowTracker
    {
        private static readonly Dictionary<int, float> RecentThrownCreatures = new Dictionary<int, float>();
        private static float _lastCleanupTime = 0f;
        private const float CleanupInterval = 5f;

        public static void Reset()
        {
            RecentThrownCreatures.Clear();
            _lastCleanupTime = 0f;
        }

        public static void RecordThrow(Creature creature, string source)
        {
            if (creature == null || creature.isPlayer) return;
            RecentThrownCreatures[creature.GetInstanceID()] = Time.unscaledTime;
            Cleanup(Time.unscaledTime);
            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Thrown release recorded (" + source + ", window " + CSMModOptions.ThrownImpactWindowSeconds.ToString("0.###") + "s): " + creature.name);
        }

        public static bool WasRecentlyThrown(Creature creature)
        {
            if (creature == null) return false;
            if (!CSMModOptions.EnableBasicKill || !CSMModOptions.EnableThrownImpactKill)
                return false;

            int id = creature.GetInstanceID();
            if (!RecentThrownCreatures.TryGetValue(id, out float releaseTime))
                return false;

            float window = CSMModOptions.ThrownImpactWindowSeconds;
            if (window <= 0f)
            {
                RecentThrownCreatures.Remove(id);
                return true;
            }

            float now = Time.unscaledTime;
            if (now - releaseTime > window)
                return false;

            RecentThrownCreatures.Remove(id);
            return true;
        }

        private static void Cleanup(float now)
        {
            float window = CSMModOptions.ThrownImpactWindowSeconds;
            if (window <= 0f)
                return;

            if (now - _lastCleanupTime < CleanupInterval)
                return;

            _lastCleanupTime = now;
            float expireAfter = window * 2f;
            List<int> expired = null;
            foreach (var kvp in RecentThrownCreatures)
            {
                if (now - kvp.Value > expireAfter)
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
