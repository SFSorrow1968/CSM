using System;
using System.Collections.Generic;
using UnityEngine;

namespace CinematicKill
{
    /// <summary>
    /// Analyzes game state to determine modifiers for the kill cam sequence
    /// </summary>
    public class CinematicContextManager
    {
        [Flags]
        public enum KillContext
        {
            None = 0,
            Crit = 1 << 0,
            Dismember = 1 << 1,
            LongRange = 1 << 2,
            LowHealth = 1 << 3,
            Headshot = 1 << 4,
            Killstreak = 1 << 5,
            Sneak = 1 << 6
        }

        public struct ContextModifiers
        {
            public float DurationMultiplier;
            public float SlowScaleMultiplier;
            public float ZoomMultiplier;
            public float ZoomSpeedMultiplier;
            public Color ScreenTint;
            public bool TriggerFlash;
            public string DebugInfo;
            public KillContext TriggeredContexts;
            public bool IsHeadshot;
            public int KillstreakCount;
            
            // Trigger bonuses (applied when contextual trigger matches)
            public float BonusDuration;
            public float BonusSlowScale;
            
            // Distance from player to target at kill time (for effects like blood splatter)
            public float TargetDistance;
        }

        private static CinematicContextManager instance;
        public static CinematicContextManager Instance => instance ??= new CinematicContextManager();

        // Configuration (will be set from settings)
        public float DistanceThreshold = 20f;
        public float LowHealthThreshold = 0.3f;
        
        public float LongRangeZoomMultiplier = 1.5f;
        public float LongRangeZoomSpeed = 1f;
        public float LowHealthSlowScale = 0.5f; // 50% slower
        public float CritZoomMultiplier = 1.3f;
        public float CritZoomSpeed = 1f;
        
        public string ColorGradingMode = "Default";
        public float ColorGradingIntensity = 0.2f;

        // Deprecated but kept for compatibility if needed by Settings (though Settings shouldn't need them logic-wise)
        // Actually, Manager might try to set them on ContextManager, so let's keep fields but ignore them
        public float StreakWindowDuration = 10f;
        public float StreakDurationBoost = 0.5f;

        public ContextModifiers GetContextModifiers(EntityPlayerLocal player, EntityAlive target, DamageSource source, bool isCrit)
        {
            return GetContextModifiers(player, target, source, isCrit, false, 0, false);
        }
        
        public ContextModifiers GetContextModifiers(EntityPlayerLocal player, EntityAlive target, DamageSource source, bool isCrit, bool isHeadshot, int currentKillstreak, bool wasUnaware = false)
        {
            ContextModifiers mods = new ContextModifiers
            {
                DurationMultiplier = 1f,
                SlowScaleMultiplier = 1f,
                ZoomMultiplier = 1f,
                ZoomSpeedMultiplier = 1f,
                ScreenTint = GetBaseTint(ColorGradingMode, ColorGradingIntensity),
                TriggerFlash = false,
                DebugInfo = "",
                TriggeredContexts = KillContext.None,
                IsHeadshot = isHeadshot,
                KillstreakCount = currentKillstreak,
                TargetDistance = float.MaxValue // Default to far away if we can't calculate
            };

            if (player == null || target == null) return mods;

            List<string> activeContexts = new List<string>();

            // 1. Distance Analysis
            float distance = Vector3.Distance(player.position, target.position);
            mods.TargetDistance = distance; // Store for effects that need distance (e.g., blood splatter)
            
            if (distance >= DistanceThreshold)
            {
                mods.ZoomMultiplier *= LongRangeZoomMultiplier;
                mods.ZoomSpeedMultiplier *= LongRangeZoomSpeed;
                mods.TriggeredContexts |= KillContext.LongRange;
                activeContexts.Add($"LongRange({distance:F1}m)");
            }

            // 2. Health Analysis
            float healthPercent = player.Health / (float)player.GetMaxHealth();
            if (healthPercent <= LowHealthThreshold)
            {
                mods.SlowScaleMultiplier *= LowHealthSlowScale; // Slower time for dramatic near-death
                mods.ScreenTint = new Color(0.3f, 0f, 0f, 0.3f); // Red tint
                mods.TriggeredContexts |= KillContext.LowHealth;
                activeContexts.Add($"LowHealth({healthPercent:P0})");
            }

            // 3. Critical/Headshot Analysis
            if (isCrit)
            {
                mods.ZoomMultiplier *= CritZoomMultiplier;
                mods.ZoomSpeedMultiplier *= CritZoomSpeed;
                mods.TriggerFlash = true; // Flash on crits
                mods.TriggeredContexts |= KillContext.Crit;
                activeContexts.Add("Crit");
            }
            
            // 4. Headshot Analysis
            if (isHeadshot)
            {
                mods.ZoomMultiplier *= 1.2f; // Extra zoom on headshots
                mods.TriggerFlash = true;
                mods.TriggeredContexts |= KillContext.Headshot;
                activeContexts.Add("Headshot");
            }
            
            // 5. Killstreak Analysis
            if (currentKillstreak >= 3) // Minimum streak for bonus
            {
                mods.TriggeredContexts |= KillContext.Killstreak;
                activeContexts.Add($"Killstreak(x{currentKillstreak})");
            }
            
            // 6. Sneak Analysis - Use wasUnaware captured BEFORE damage in Prefix
            // The wasUnaware flag is set by EntityAliveDamagePatch.Prefix before the target becomes aware
            // This ensures true sneak detection - the target must have been unaware at moment of shot
            bool playerCrouching = player.IsCrouching;
            bool isSneakKill = wasUnaware; // Use the pre-damage awareness state
            
            // Log for debugging
            CKLog.Verbose($"Sneak check - Crouching:{playerCrouching}, WasUnaware:{wasUnaware}, IsSneakKill:{isSneakKill}");
            
            if (isSneakKill)
            {
                mods.TriggeredContexts |= KillContext.Sneak;
                activeContexts.Add("Sneak");
            }
            
            // 7. Dismember Analysis - Detect kills likely to cause dismemberment
            // Dismemberment is detected when:
            // - DismemberChance > 0 on the damage source (set by game based on weapon/hit)
            // - Headshot kills (head often pops off on kill)
            try
            {
                bool isDismember = false;
                string dismemberReason = "";
                
                if (source != null)
                {
                    // Check DismemberChance - the game sets this when damage can cause dismemberment
                    // This is the primary and most reliable detection method
                    float dismemberChance = source.DismemberChance;
                    if (dismemberChance > 0f)
                    {
                        isDismember = true;
                        dismemberReason = $"DismemberChance({dismemberChance:P0})";
                    }
                    
                    // Fallback: Headshots with lethal damage often cause head dismemberment
                    if (!isDismember && mods.IsHeadshot)
                    {
                        try
                        {
                            var bodyPart = source.GetEntityDamageBodyPart(target);
                            if ((bodyPart & EnumBodyPartHit.Head) != 0)
                            {
                                isDismember = true;
                                dismemberReason = "HeadshotKill";
                            }
                        }
                        catch { }
                    }
                }
                
                if (isDismember)
                {
                    mods.TriggeredContexts |= KillContext.Dismember;
                    activeContexts.Add($"Dismember({dismemberReason})");
                    CKLog.Verbose($" [EXPERIMENTAL] Dismember context detected: {dismemberReason}");
                }
            }
            catch (Exception ex)
            {
                CKLog.Verbose($" [EXPERIMENTAL] Dismember detection failed: {ex.Message}");
            }

            mods.DebugInfo = string.Join(", ", activeContexts);
            return mods;
        }

        
        public Color GetBaseTint(string mode, float intensity)
        {
            Color baseColor;
            switch (mode?.ToLower())
            {
                case "gritty":
                    baseColor = new Color(0.3f, 0.2f, 0.05f); // Brown/Orange
                    break;
                case "horror":
                    baseColor = new Color(0.3f, 0f, 0f); // Deep Red
                    break;
                case "noir":
                    baseColor = new Color(0.1f, 0.1f, 0.1f); // Dark Grey
                    break;
                case "toxic":
                    baseColor = new Color(0f, 0.2f, 0f); // Green
                    break;
                case "bloodmoon":
                    baseColor = new Color(0.55f, 0f, 0.28f); // Brighter magenta for contrast
                    break;
                case "cold":
                    baseColor = new Color(0f, 0.1f, 0.3f); // Deep Blue
                    break;
                default: // Default
                    baseColor = new Color(0f, 0f, 0.2f); // Subtle Blue
                    break;
            }
            
            return new Color(baseColor.r, baseColor.g, baseColor.b, intensity);
        }
    }
}
