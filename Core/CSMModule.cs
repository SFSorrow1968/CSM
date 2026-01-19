using CSM.Configuration;
using CSM.Patches;
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
            Debug.Log("[CSM] Loading...");
            new CSMSettings().Load(ModPath);
            CSMManager.Instance.Initialize();
            CSMPatches.ApplyPatches();
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
            CSMPatches.RemovePatches();
            Debug.Log("[CSM] Unloaded.");
        }
    }
}
