using System;
using System.Collections.Generic;
using CSM.Configuration;
using ThunderRoad;
using UnityEngine;

namespace CSM.Camera
{
    /// <summary>
    /// Controls player body mesh visibility for killcam third-person view.
    /// Toggles meshes that are normally hidden in first-person.
    /// </summary>
    public static class MeshVisibilityController
    {
        private static List<SkinnedMeshRenderer> _affectedMeshes = new List<SkinnedMeshRenderer>();
        private static bool _isShowingPlayerBody = false;

        /// <summary>
        /// Show or hide the player's body meshes.
        /// When showing, enables meshes normally hidden for FPV.
        /// </summary>
        public static void ShowPlayerBody(bool show)
        {
            try
            {
                if (show == _isShowingPlayerBody) return;

                var player = Player.local?.creature;
                if (player == null)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] MeshVisibility: No player creature found");
                    return;
                }

                if (show)
                {
                    EnablePlayerBodyMeshes(player);
                }
                else
                {
                    RestorePlayerBodyMeshes();
                }

                _isShowingPlayerBody = show;

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] MeshVisibility: Player body " + (show ? "shown" : "hidden"));
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] MeshVisibility error: " + ex.Message);
            }
        }

        private static void EnablePlayerBodyMeshes(Creature player)
        {
            _affectedMeshes.Clear();

            // meshesToHideForFPV contains meshes hidden when in first-person view
            if (player.meshesToHideForFPV != null)
            {
                foreach (var mesh in player.meshesToHideForFPV)
                {
                    if (mesh != null && !mesh.enabled)
                    {
                        mesh.enabled = true;
                        _affectedMeshes.Add(mesh);
                    }
                }
            }

            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] MeshVisibility: Enabled " + _affectedMeshes.Count + " meshes");
        }

        private static void RestorePlayerBodyMeshes()
        {
            foreach (var mesh in _affectedMeshes)
            {
                if (mesh != null)
                {
                    mesh.enabled = false;
                }
            }

            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] MeshVisibility: Restored " + _affectedMeshes.Count + " meshes to hidden");

            _affectedMeshes.Clear();
        }

        /// <summary>
        /// Force restore all meshes to hidden state.
        /// Call this on shutdown or error recovery.
        /// </summary>
        public static void ForceRestore()
        {
            RestorePlayerBodyMeshes();
            _isShowingPlayerBody = false;
        }
    }
}
