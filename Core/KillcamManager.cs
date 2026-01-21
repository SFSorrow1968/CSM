using System;
using CSM.Camera;
using CSM.Configuration;
using ThunderRoad;
using UnityEngine;

namespace CSM.Core
{
    /// <summary>
    /// Manages Skyrim-style killcam that shows dramatic kills from third-person.
    /// Coordinates with CSMManager for slow motion timing.
    /// </summary>
    public class KillcamManager
    {
        private static KillcamManager _instance;
        public static KillcamManager Instance => _instance ??= new KillcamManager();

        // State
        private bool _isKillcamActive;
        private float _killcamEndTime;
        private Creature _targetCreature;
        private TriggerType _triggerType;

        // Camera state
        private UnityEngine.Camera _mainCamera;
        private Transform _originalCameraParent;
        private Vector3 _originalCameraLocalPosition;
        private Quaternion _originalCameraLocalRotation;
        private GameObject _killcamRig;

        // Transition state
        private bool _isTransitioningIn;
        private bool _isTransitioningOut;
        private float _transitionProgress;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;

        // Orbit state
        private float _orbitAngle;

        // Constants
        private const float TRANSITION_IN_SPEED = 5f;
        private const float TRANSITION_OUT_SPEED = 8f;
        private const float SIDE_OFFSET = 0.5f;

        public bool IsActive => _isKillcamActive;

        public void Initialize()
        {
            _isKillcamActive = false;
            _killcamRig = null;
            _targetCreature = null;
            Debug.Log("[CSM] KillcamManager initialized");
        }

        public void Update()
        {
            if (!_isKillcamActive && !_isTransitioningOut) return;

            try
            {
                // Handle transitions
                if (_isTransitioningIn)
                {
                    UpdateTransitionIn();
                }
                else if (_isTransitioningOut)
                {
                    UpdateTransitionOut();
                }
                else if (_isKillcamActive)
                {
                    // Update orbit if enabled
                    UpdateCameraOrbit();

                    // Check if killcam should end (synced with slow motion)
                    if (Time.unscaledTime >= _killcamEndTime)
                    {
                        EndKillcam();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] Killcam Update error: " + ex.Message);
                ForceEndKillcam();
            }
        }

        /// <summary>
        /// Try to start killcam for the given target creature.
        /// </summary>
        public bool TryStartKillcam(Creature target, TriggerType triggerType, float duration)
        {
            try
            {
                if (!CSMModOptions.KillcamEnabled)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Killcam: Disabled in options");
                    return false;
                }

                if (_isKillcamActive || _isTransitioningIn || _isTransitioningOut)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Killcam: Already active or transitioning");
                    return false;
                }

                if (target == null || target.isPlayer)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Killcam: Invalid target");
                    return false;
                }

                // Check if this trigger type should activate killcam
                if (!ShouldTriggerKillcam(triggerType))
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Killcam: Trigger type " + triggerType + " not enabled for killcam");
                    return false;
                }

                // Get main camera
                _mainCamera = UnityEngine.Camera.main;
                if (_mainCamera == null)
                {
                    if (CSMModOptions.DebugLogging)
                        Debug.Log("[CSM] Killcam: No main camera found");
                    return false;
                }

                _targetCreature = target;
                _triggerType = triggerType;
                _killcamEndTime = Time.unscaledTime + duration;

                // Store original camera state
                _originalCameraParent = _mainCamera.transform.parent;
                _originalCameraLocalPosition = _mainCamera.transform.localPosition;
                _originalCameraLocalRotation = _mainCamera.transform.localRotation;

                // Calculate killcam position
                _targetPosition = CalculateKillcamPosition(target);
                _targetRotation = CalculateKillcamRotation(_targetPosition, target);

                // Setup transition
                _startPosition = _mainCamera.transform.position;
                _startRotation = _mainCamera.transform.rotation;
                _transitionProgress = 0f;
                _isTransitioningIn = true;
                _orbitAngle = 0f;

                // Create killcam rig to hold camera during killcam
                CreateKillcamRig();

                // Show player body if enabled
                if (CSMModOptions.KillcamShowPlayerBody)
                {
                    MeshVisibilityController.ShowPlayerBody(true);
                }

                Debug.Log("[CSM] Killcam START: " + triggerType + " targeting " + target.name);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] Killcam TryStart error: " + ex.Message);
                ForceEndKillcam();
                return false;
            }
        }

        private bool ShouldTriggerKillcam(TriggerType type)
        {
            switch (type)
            {
                case TriggerType.Decapitation:
                    return CSMModOptions.KillcamOnDecapitation;
                case TriggerType.Critical:
                    return CSMModOptions.KillcamOnCritical;
                case TriggerType.LastEnemy:
                    return CSMModOptions.KillcamOnLastEnemy;
                default:
                    return false;
            }
        }

        private void CreateKillcamRig()
        {
            if (_killcamRig != null)
            {
                UnityEngine.Object.Destroy(_killcamRig);
            }

            _killcamRig = new GameObject("CSM_KillcamRig");
            _killcamRig.transform.position = _startPosition;
            _killcamRig.transform.rotation = _startRotation;
        }

        private Vector3 CalculateKillcamPosition(Creature target)
        {
            // Get player and target positions
            Transform playerHead = Player.local?.creature?.centerEyes;
            Transform targetCenter = GetTargetCenter(target);

            if (playerHead == null || targetCenter == null)
            {
                return _mainCamera.transform.position;
            }

            // Calculate direction from player to target
            Vector3 playerToTarget = (targetCenter.position - playerHead.position).normalized;
            Vector3 cameraRight = Vector3.Cross(Vector3.up, playerToTarget).normalized;

            // Position camera behind/beside the target, looking at it
            float distance = CSMModOptions.KillcamDistance;
            float height = CSMModOptions.KillcamHeight;

            Vector3 cameraPos = targetCenter.position
                               - playerToTarget * distance
                               + Vector3.up * height
                               + cameraRight * SIDE_OFFSET;

            return cameraPos;
        }

        private Quaternion CalculateKillcamRotation(Vector3 cameraPos, Creature target)
        {
            Transform targetCenter = GetTargetCenter(target);
            if (targetCenter == null)
            {
                return _mainCamera.transform.rotation;
            }

            Vector3 lookDirection = (targetCenter.position - cameraPos).normalized;
            return Quaternion.LookRotation(lookDirection);
        }

        private Transform GetTargetCenter(Creature target)
        {
            if (target == null) return null;

            // Try to get torso for better center point
            try
            {
                var torso = target.ragdoll?.GetPart(RagdollPart.Type.Torso);
                if (torso != null)
                {
                    return torso.transform;
                }
            }
            catch { }

            return target.transform;
        }

        private void UpdateTransitionIn()
        {
            _transitionProgress += Time.unscaledDeltaTime * TRANSITION_IN_SPEED;

            if (_transitionProgress >= 1f)
            {
                _transitionProgress = 1f;
                _isTransitioningIn = false;
                _isKillcamActive = true;

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Killcam: Transition in complete");
            }

            // Smooth interpolation
            float t = Mathf.SmoothStep(0f, 1f, _transitionProgress);

            // Update killcam rig position/rotation
            _killcamRig.transform.position = Vector3.Lerp(_startPosition, _targetPosition, t);
            _killcamRig.transform.rotation = Quaternion.Slerp(_startRotation, _targetRotation, t);

            // Detach camera from player and parent to killcam rig during transition
            if (_transitionProgress > 0.1f && _mainCamera.transform.parent != _killcamRig.transform)
            {
                _mainCamera.transform.SetParent(_killcamRig.transform, true);
                _mainCamera.transform.localPosition = Vector3.zero;
                _mainCamera.transform.localRotation = Quaternion.identity;
            }
        }

        private void UpdateTransitionOut()
        {
            _transitionProgress += Time.unscaledDeltaTime * TRANSITION_OUT_SPEED;

            if (_transitionProgress >= 1f)
            {
                _transitionProgress = 1f;
                _isTransitioningOut = false;
                FinishKillcam();

                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Killcam: Transition out complete");
                return;
            }

            // Smooth interpolation back to original
            float t = Mathf.SmoothStep(0f, 1f, _transitionProgress);

            Vector3 currentPos = _killcamRig.transform.position;
            Quaternion currentRot = _killcamRig.transform.rotation;

            // Get current player head position as target (it may have moved)
            Vector3 returnPos = _originalCameraLocalPosition;
            Quaternion returnRot = _originalCameraLocalRotation;

            if (_originalCameraParent != null)
            {
                returnPos = _originalCameraParent.TransformPoint(_originalCameraLocalPosition);
                returnRot = _originalCameraParent.rotation * _originalCameraLocalRotation;
            }

            _killcamRig.transform.position = Vector3.Lerp(currentPos, returnPos, t);
            _killcamRig.transform.rotation = Quaternion.Slerp(currentRot, returnRot, t);
        }

        private void UpdateCameraOrbit()
        {
            if (_targetCreature == null || !_isKillcamActive) return;

            float orbitSpeed = CSMModOptions.KillcamOrbitSpeed;
            if (orbitSpeed <= 0f)
            {
                // Static camera - just track target
                Transform targetCenter = GetTargetCenter(_targetCreature);
                if (targetCenter != null)
                {
                    _killcamRig.transform.LookAt(targetCenter);
                }
                return;
            }

            // Orbit around target
            _orbitAngle += orbitSpeed * Time.unscaledDeltaTime;

            Transform targetCenter2 = GetTargetCenter(_targetCreature);
            if (targetCenter2 == null) return;

            float distance = CSMModOptions.KillcamDistance;
            float height = CSMModOptions.KillcamHeight;

            Vector3 offset = new Vector3(
                Mathf.Sin(_orbitAngle * Mathf.Deg2Rad) * distance,
                height,
                Mathf.Cos(_orbitAngle * Mathf.Deg2Rad) * distance
            );

            Vector3 newPos = targetCenter2.position + offset;
            _killcamRig.transform.position = newPos;
            _killcamRig.transform.LookAt(targetCenter2);
        }

        /// <summary>
        /// End killcam and begin transition back to first-person.
        /// </summary>
        public void EndKillcam()
        {
            if (!_isKillcamActive && !_isTransitioningIn) return;

            try
            {
                _isKillcamActive = false;
                _isTransitioningIn = false;
                _isTransitioningOut = true;
                _transitionProgress = 0f;

                Debug.Log("[CSM] Killcam END: Beginning transition out");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] Killcam EndKillcam error: " + ex.Message);
                ForceEndKillcam();
            }
        }

        private void FinishKillcam()
        {
            try
            {
                // Restore camera to original parent
                if (_mainCamera != null && _originalCameraParent != null)
                {
                    _mainCamera.transform.SetParent(_originalCameraParent, false);
                    _mainCamera.transform.localPosition = _originalCameraLocalPosition;
                    _mainCamera.transform.localRotation = _originalCameraLocalRotation;
                }

                // Hide player body again
                MeshVisibilityController.ShowPlayerBody(false);

                // Cleanup killcam rig
                if (_killcamRig != null)
                {
                    UnityEngine.Object.Destroy(_killcamRig);
                    _killcamRig = null;
                }

                _targetCreature = null;
                _isTransitioningOut = false;

                Debug.Log("[CSM] Killcam: Finished and cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] Killcam FinishKillcam error: " + ex.Message);
            }
        }

        /// <summary>
        /// Force end killcam immediately without transition (for error recovery).
        /// </summary>
        public void ForceEndKillcam()
        {
            _isKillcamActive = false;
            _isTransitioningIn = false;
            _isTransitioningOut = false;

            // Restore camera immediately
            if (_mainCamera != null && _originalCameraParent != null)
            {
                try
                {
                    _mainCamera.transform.SetParent(_originalCameraParent, false);
                    _mainCamera.transform.localPosition = _originalCameraLocalPosition;
                    _mainCamera.transform.localRotation = _originalCameraLocalRotation;
                }
                catch { }
            }

            MeshVisibilityController.ForceRestore();

            if (_killcamRig != null)
            {
                try
                {
                    UnityEngine.Object.Destroy(_killcamRig);
                }
                catch { }
                _killcamRig = null;
            }

            _targetCreature = null;

            Debug.Log("[CSM] Killcam: Force ended");
        }

        public void Shutdown()
        {
            ForceEndKillcam();
            _instance = null;
            Debug.Log("[CSM] KillcamManager shutdown");
        }
    }
}
