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
        public static string ModPath { get; private set; }

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            ModPath = modData.fullPath;
#if NOMAD
            Debug.Log("[CSM] Loading (Nomad)...");
#else
            Debug.Log("[CSM] Loading (PCVR)...");
#endif
            new CSMSettings().Load(ModPath);
            CSMManager.Instance.Initialize();

#if NOMAD
            // Nomad: Use EventManager hooks (IL2CPP compatible)
            EventHooks.Subscribe();
#else
            // PCVR: Use Harmony patches for more comprehensive hooks
            CSMPatches.ApplyPatches();
#endif
            Debug.Log("[CSM] Loaded!");
        }

        public override void ScriptUpdate()
        {
            base.ScriptUpdate();
            CSMManager.Instance.Update();
        }

        public override void ScriptDisable()
        {
            base.ScriptDisable();
            CSMManager.Instance.CancelSlowMotion();

#if NOMAD
            EventHooks.Unsubscribe();
            EventHooks.ResetState();
#else
            CSMPatches.RemovePatches();
            PlayerPatches.ResetState();
#endif
            Debug.Log("[CSM] Unloaded.");
        }
    }
}
