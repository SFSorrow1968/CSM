using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CSM.Patches
{
    public static class CSMPatches
    {
        private static Harmony _harmony;
        private const string HARMONY_ID = "com.csm.patches";

        public static void ApplyPatches()
        {
            try
            {
                _harmony = new Harmony(HARMONY_ID);
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
                Debug.Log("[CSM] Patches applied.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CSM] Patch error: {ex.Message}");
            }
        }

        public static void RemovePatches()
        {
            _harmony?.UnpatchAll(HARMONY_ID);
        }
    }
}
