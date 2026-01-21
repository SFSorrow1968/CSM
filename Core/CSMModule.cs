using System;
using CSM.Configuration;
using CSM.Hooks;
#if !NOMAD
using CSM.Patches;
#endif
using ThunderRoad;
using UnityEngine;

namespace CSM.Core
{
    /// <summary>
    /// Main ThunderScript entry point for CSM.
    /// Simplified to match working Nomad mod pattern.
    /// </summary>
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

                // Initialize managers
                CSMManager.Instance.Initialize();
                KillcamManager.Instance.Initialize();

#if NOMAD
                // Nomad: Use EventManager hooks (IL2CPP compatible)
                Debug.Log("[CSM] Subscribing event hooks (Nomad mode)...");
                EventHooks.Subscribe();
#else
                // PCVR: Use Harmony patches
                Debug.Log("[CSM] Applying Harmony patches (PCVR mode)...");
                try
                {
                    CSMPatches.ApplyPatches();
                    Debug.Log("[CSM] Harmony patches applied");
                }
                catch (Exception ex)
                {
                    Debug.LogError("[CSM] Harmony patches failed: " + ex.Message);
                    EventHooks.Subscribe();
                }
#endif

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
                CSMManager.Instance?.Update();
                KillcamManager.Instance?.Update();
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

                KillcamManager.Instance?.Shutdown();
                CSMManager.Instance?.CancelSlowMotion();

#if NOMAD
                EventHooks.Unsubscribe();
                EventHooks.ResetState();
#else
                try { CSMPatches.RemovePatches(); } catch { }
                try { EventHooks.Unsubscribe(); EventHooks.ResetState(); } catch { }
#endif

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
