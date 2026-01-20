using System;
using HarmonyLib;
using UnityEngine;

namespace CinematicKill
{
    /// <summary>
    /// Prevents rare NullReference exceptions thrown by the base game's reload state exit handler
    /// when the animator context has been torn down during cinematics (e.g., camera swaps).
    /// </summary>
    [HarmonyPatch(typeof(AnimatorWeaponRangedReloadState), "OnStateExit")]
    internal static class AnimatorReloadExitPatch
    {
        // Swallow only NullReference exceptions to avoid hard crashes while keeping other errors visible.
        private static Exception Finalizer(Exception __exception, Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (__exception is NullReferenceException)
            {
                Log.Warning("CinematicKill: Suppressed NullReference in AnimatorWeaponRangedReloadState.OnStateExit (animator context missing during cinematic).");
                return null;
            }

            return __exception;
        }
    }
}
