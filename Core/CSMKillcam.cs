using CSM.Configuration;
using ThunderRoad;
using UnityEngine;

namespace CSM.Core
{
    public class CSMKillcam
    {
        private const float DefaultDuration = 0.9f;
        private const float MinDuration = 0.5f;
        private const float TargetHeightOffset = 0.2f;
        private const float PositionDamping = 12f;
        private const float RotationDamping = 14f;
        private const float FovDamping = 10f;
        private const float KillcamFov = 55f;
        private const float CooldownSeconds = 1.0f;
        private const float RandomYawOffsetMax = 20f;
        private const float KillcamDistanceMin = 2f;
        private const float KillcamDistanceMax = 5f;
        private const float KillcamHeightMin = 1f;
        private const float KillcamHeightMax = 2f;
        private const float RandomizeRangePercent = 0.2f;

        public static CSMKillcam Instance { get; } = new CSMKillcam();

        private bool _active;
        private float _startTime;
        private float _endTime;
        private float _nextAllowedTime;
        private Camera _camera;
        private Transform _target;
        private float _initialFov;
        private float _baseYaw;
        private float _orbitSweep;
        private float _orbitDirection;
        private float _orbitOffset;
        private bool _restoreFov;
        private bool _dumpedCameras;
        private ThirdPersonView _thirdPersonView;
        private bool _thirdPersonWasActive;
        private bool _thirdPersonWasEnabled;
        private bool _thirdPersonActivated;
        private float _distance;
        private float _height;

        private CSMKillcam() { }

        public void Initialize()
        {
            _active = false;
            _startTime = 0f;
            _endTime = 0f;
            _nextAllowedTime = 0f;
            _camera = null;
            _target = null;
            _restoreFov = false;
            _dumpedCameras = false;
            _thirdPersonView = null;
            _thirdPersonWasActive = false;
            _thirdPersonWasEnabled = false;
            _thirdPersonActivated = false;
            _orbitDirection = 1f;
            _orbitOffset = 0f;
            _distance = 0f;
            _height = 0f;
        }

        public void Shutdown()
        {
            Stop(true);
        }

        public void Update()
        {
            if (!_active) return;

            if (_camera == null || _target == null || Time.unscaledTime >= _endTime)
            {
                Stop(false);
                return;
            }

            float t = Mathf.InverseLerp(_startTime, _endTime, Time.unscaledTime);
            float eased = EaseInOut(t);

            Vector3 pivot = _target.position + Vector3.up * TargetHeightOffset;
            float sweep = Mathf.Lerp(-_orbitSweep * 0.5f, _orbitSweep * 0.5f, eased);
            float angle = _baseYaw + _orbitOffset + (_orbitDirection * sweep);

            float distance = Mathf.Max(0.5f, _distance);
            float height = _height;

            Vector3 desiredOffset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, height, -distance);
            Vector3 desiredPos = pivot + desiredOffset;

            float dt = Time.unscaledDeltaTime;
            float posAlpha = 1f - Mathf.Exp(-PositionDamping * dt);
            float rotAlpha = 1f - Mathf.Exp(-RotationDamping * dt);

            Transform camTransform = _camera.transform;
            camTransform.position = Vector3.Lerp(camTransform.position, desiredPos, posAlpha);

            Quaternion desiredRot = Quaternion.LookRotation(pivot - camTransform.position, Vector3.up);
            camTransform.rotation = Quaternion.Slerp(camTransform.rotation, desiredRot, rotAlpha);

            if (_restoreFov)
            {
                float fovAlpha = 1f - Mathf.Exp(-FovDamping * dt);
                _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, KillcamFov, fovAlpha);
            }
        }

        public bool TryStartKillcam(TriggerType triggerType, Creature targetCreature, float slowMoDuration, bool allowThirdPerson)
        {
            if (_active) return false;
            if (targetCreature == null) return false;
            if (Time.unscaledTime < _nextAllowedTime) return false;
            if (!allowThirdPerson) return false;

            float chance = CSMModOptions.GetKillcamChance(triggerType);
            if (chance < 1f && UnityEngine.Random.value > chance)
                return false;

            Transform targetTransform = ResolveTargetTransform(targetCreature);
            if (targetTransform == null) return false;

            if (CSMModOptions.DebugLogging)
            {
                DumpCamerasOnce();
            }

            Camera cam = null;
            if (!TryUseThirdPersonCamera(out cam))
            {
                cam = FindBestNonHmdCamera();
            }

            if (cam == null)
            {
                if (CSMModOptions.DebugLogging)
                    Debug.Log("[CSM] Killcam skipped: no suitable non-HMD camera found");
                return false;
            }

            float duration = ComputeDuration(slowMoDuration);

            _camera = cam;
            _target = targetTransform;
            _startTime = Time.unscaledTime;
            _endTime = _startTime + duration;
            _nextAllowedTime = _endTime + CooldownSeconds;
            _distance = ResolveDistance();
            _height = ResolveHeight();
            _baseYaw = ComputeBaseYaw(cam, targetTransform);
            _orbitSweep = Mathf.Abs(CSMModOptions.KillcamOrbitSpeed) * duration;
            _orbitDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            _orbitOffset = UnityEngine.Random.Range(-RandomYawOffsetMax, RandomYawOffsetMax);
            _initialFov = cam.fieldOfView;
            _restoreFov = !cam.orthographic;
            _active = true;

            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Killcam started on camera: " + cam.name);

            return true;
        }

        public void Stop(bool force)
        {
            if (!_active && !force) return;

            if (_camera != null && _restoreFov)
                _camera.fieldOfView = _initialFov;

            RestoreThirdPersonView();

            _active = false;
            _camera = null;
            _target = null;
            _restoreFov = false;
        }

        private static float ComputeDuration(float slowMoDuration)
        {
            if (slowMoDuration <= 0f) return DefaultDuration;
            return Mathf.Max(MinDuration, slowMoDuration);
        }

        private static Transform ResolveTargetTransform(Creature creature)
        {
            if (creature == null) return null;

            var ragdoll = creature.ragdoll;
            if (ragdoll?.parts != null)
            {
                foreach (var part in ragdoll.parts)
                {
                    if (part == null) continue;
                    if ((part.type & (RagdollPart.Type.Head | RagdollPart.Type.Neck)) != 0)
                        return part.transform;
                }
            }

            return creature.transform;
        }

        private static float ComputeBaseYaw(Camera cam, Transform target)
        {
            Vector3 pivot = target.position + Vector3.up * TargetHeightOffset;
            Vector3 offset = cam.transform.position - pivot;
            if (offset.sqrMagnitude < 0.01f)
            {
                Vector3 forward = cam.transform.forward;
                return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            }

            return Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }

        private static float ResolveDistance()
        {
            float distance = Mathf.Clamp(CSMModOptions.KillcamDistance, 0.5f, KillcamDistanceMax);
            if (!CSMModOptions.KillcamRandomizeDistance)
                return distance;
            return RandomizeValue(distance, KillcamDistanceMin, KillcamDistanceMax);
        }

        private static float ResolveHeight()
        {
            float height = Mathf.Clamp(CSMModOptions.KillcamHeight, KillcamHeightMin, KillcamHeightMax);
            if (!CSMModOptions.KillcamRandomizeHeight)
                return height;
            return RandomizeValue(height, KillcamHeightMin, KillcamHeightMax);
        }

        private static float RandomizeValue(float baseValue, float min, float max)
        {
            float range = baseValue * RandomizeRangePercent;
            float low = Mathf.Max(min, baseValue - range);
            float high = Mathf.Min(max, baseValue + range);
            if (high <= low)
                return baseValue;
            return UnityEngine.Random.Range(low, high);
        }

        private static Camera FindBestNonHmdCamera()
        {
            Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);
            Camera best = null;
            int bestScore = int.MinValue;

            foreach (var cam in cameras)
            {
                if (cam == null) continue;

                int score = 0;
                string name = (cam.name ?? string.Empty).ToLowerInvariant();

                if (!cam.enabled) score -= 10;
                if (!cam.gameObject.activeInHierarchy) score -= 5;
                if (cam.targetTexture != null) score -= 100;

                if (name.Contains("third") || name.Contains("spect") || name.Contains("external") || name.Contains("desktop") || name.Contains("observer"))
                    score += 50;

                if (LooksLikeHmd(cam, name)) score -= 100;
                if (name.Contains("note") || name.Contains("book") || name.Contains("ui") || name.Contains("menu"))
                    score -= 50;

                if (cam.tag == "MainCamera") score -= 15;
                if (cam.stereoTargetEye == StereoTargetEyeMask.None) score += 5;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = cam;
                }
            }

            return bestScore <= -50 ? null : best;
        }

        private bool TryUseThirdPersonCamera(out Camera cam)
        {
            cam = null;

            var view = ThirdPersonView.local;
            if (view == null)
            {
                view = UnityEngine.Object.FindObjectOfType<ThirdPersonView>(true);
            }

            if (view == null) return false;

            bool wasActive = view.isActive;
            bool wasEnabled = view.enabled;

            if (!wasActive)
            {
                view.Activate(true);
            }

            cam = view.cam;
            if (cam == null) return false;

            _thirdPersonView = view;
            _thirdPersonWasActive = wasActive;
            _thirdPersonWasEnabled = wasEnabled;
            _thirdPersonActivated = !wasActive && view.isActive;

            if (view.enabled)
                view.enabled = false;

            if (CSMModOptions.DebugLogging)
                Debug.Log("[CSM] Killcam using ThirdPersonView camera: " + cam.name);

            return true;
        }

        private void RestoreThirdPersonView()
        {
            if (_thirdPersonView == null) return;

            if (_thirdPersonActivated)
            {
                if (!_thirdPersonView.enabled)
                    _thirdPersonView.enabled = true;
                _thirdPersonView.Activate(false);
            }

            if (_thirdPersonView.enabled != _thirdPersonWasEnabled)
                _thirdPersonView.enabled = _thirdPersonWasEnabled;

            _thirdPersonView = null;
            _thirdPersonWasActive = false;
            _thirdPersonWasEnabled = false;
            _thirdPersonActivated = false;
        }

        private void DumpCamerasOnce()
        {
            if (_dumpedCameras) return;
            _dumpedCameras = true;

            Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>(true);
            Debug.Log("[CSM] Killcam camera dump (" + cameras.Length + " cameras):");
            foreach (var cam in cameras)
            {
                if (cam == null) continue;

                string name = cam.name ?? "<null>";
                string parentPath = GetParentPath(cam.transform);
                string stereo = cam.stereoTargetEye.ToString();
                string line = "[CSM] - " + name +
                              " enabled=" + cam.enabled +
                              " activeInHierarchy=" + cam.gameObject.activeInHierarchy +
                              " depth=" + cam.depth.ToString("F1") +
                              " stereo=" + stereo +
                              " tag=" + cam.tag +
                              " parent=" + parentPath;
                Debug.Log(line);
            }
        }

        private static string GetParentPath(Transform transform)
        {
            if (transform == null) return "<null>";

            string path = string.Empty;
            Transform current = transform.parent;
            int depth = 0;

            while (current != null && depth < 6)
            {
                string name = current.name ?? "<null>";
                path = string.IsNullOrEmpty(path) ? name : name + "/" + path;
                current = current.parent;
                depth++;
            }

            if (string.IsNullOrEmpty(path)) return "<root>";
            if (current != null) path = ".../" + path;
            return path;
        }

        private static bool LooksLikeHmd(Camera cam, string lowerName)
        {
            if (ContainsHmdToken(lowerName)) return true;

            Transform t = cam.transform.parent;
            while (t != null)
            {
                string name = (t.name ?? string.Empty).ToLowerInvariant();
                if (ContainsHmdToken(name)) return true;
                t = t.parent;
            }

            return false;
        }

        private static bool ContainsHmdToken(string name)
        {
            return name.Contains("hmd") ||
                   name.Contains("xr") ||
                   name.Contains("eye") ||
                   name.Contains("oculus") ||
                   name.Contains("openxr") ||
                   name.Contains("steamvr") ||
                   name.Contains("tracked");
        }

        private static float EaseInOut(float x)
        {
            return x * x * (3f - 2f * x);
        }
    }
}
