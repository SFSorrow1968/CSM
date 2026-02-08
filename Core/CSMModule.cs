using System;
using CSM.Configuration;
using CSM.Hooks;
using ThunderRoad;
using UnityEngine;

namespace CSM.Core
{
    public class CSMModule : ThunderScript
    {
        public static CSMModule Instance { get; private set; }

        public override void ScriptEnable()
        {
            base.ScriptEnable();

            try
            {
                Instance = this;

#if NOMAD
                Debug.Log("[CSM] === CSM v" + CSMModOptions.VERSION + " (Nomad) ===");
#else
                Debug.Log("[CSM] === CSM v" + CSMModOptions.VERSION + " (PCVR) ===");
#endif

                CSMTelemetry.Initialize();
                CSMManager.Instance.Initialize();
                CSMModOptionVisibility.Instance.Initialize();
                PerformanceMetrics.Instance.Initialize();
                Debug.Log(
                    "[CSM] Damage multipliers: " +
                    "Pierce=" + CSMModOptions.PierceMultiplier.ToString("0.##") + "x, " +
                    "Slash=" + CSMModOptions.SlashMultiplier.ToString("0.##") + "x, " +
                    "Blunt=" + CSMModOptions.BluntMultiplier.ToString("0.##") + "x, " +
                    "Elemental=" + CSMModOptions.ElementalMultiplier.ToString("0.##") + "x, " +
                    "DOT=" + CSMModOptions.GetDOTMultiplier().ToString("0.##") + "x, " +
                    "Thrown=" + CSMModOptions.GetThrownMultiplier().ToString("0.##") + "x, " +
                    "IntensityScaling=" + (CSMModOptions.IntensityScalingEnabled
                        ? ("on(max=" + CSMModOptions.IntensityScalingMax.ToString("0.##") + "x)")
                        : "off"));
                Debug.Log("[CSM] Trigger pipeline: deferredQueue=off (cooldown-blocked triggers are not queued)");

#if NOMAD
                Debug.Log("[CSM] Subscribing event hooks (Nomad mode)...");
#else
                Debug.Log("[CSM] Subscribing event hooks (PCVR mode)...");
#endif
                EventHooks.Subscribe();

                Debug.Log("[CSM] ScriptEnable complete - CSM is active!");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] ScriptEnable FAILED: " + ex.Message);
            }
        }

        public override void ScriptUpdate()
        {
            try
            {
                base.ScriptUpdate();
                CSMTelemetry.Update(Time.unscaledTime);
                CSMManager.Instance?.Update();
                CSMModOptionVisibility.Instance?.Update();

                // Update performance metrics baseline when not in slow motion
                if (!CSMManager.Instance.IsActive)
                    PerformanceMetrics.Instance?.UpdateBaseline();
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] ScriptUpdate error: " + ex.Message);
            }
        }

        public override void ScriptDisable()
        {
            try
            {
                Debug.Log("[CSM] ScriptDisable...");

                CSMManager.Instance?.CancelSlowMotion();
                CSMModOptionVisibility.Instance?.Shutdown();
                PerformanceMetrics.Instance?.Shutdown();
                CSMTelemetry.Shutdown();

                EventHooks.Unsubscribe();
                EventHooks.ResetState();

                Debug.Log("[CSM] CSM deactivated");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] ScriptDisable error: " + ex.Message);
            }

            base.ScriptDisable();
        }
    }
}
