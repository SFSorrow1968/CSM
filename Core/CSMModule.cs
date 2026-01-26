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

                CSMManager.Instance.Initialize();
                CSMKillcam.Instance.Initialize();
                CSMModOptionVisibility.Instance.Initialize();

#if NOMAD
                Debug.Log("[CSM] Subscribing event hooks (Nomad mode)...");
                EventHooks.Subscribe();
#else
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

                EventHooks.SubscribeDeflect();
                EventHooks.SubscribeThrowTracking();
                CSM.Patches.KillPatches.EnsureHooks();
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
                CSMKillcam.Instance?.Update();
                CSMModOptionVisibility.Instance?.Update();
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
                CSMKillcam.Instance?.Shutdown();
                CSMModOptionVisibility.Instance?.Shutdown();

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
