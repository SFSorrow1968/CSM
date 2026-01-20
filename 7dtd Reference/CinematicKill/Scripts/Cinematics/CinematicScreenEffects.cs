// ═══════════════════════════════════════════════════════════════════════════════
// CinematicScreenEffects.cs - Visual effects system for CinematicKill mod
// ═══════════════════════════════════════════════════════════════════════════════
//
// EFFECTS PROVIDED:
//   - Vignette (GUI overlay)
//   - Color grading / Desaturation (GUI overlay)
//   - Blood splatter (7DTD native: ForceBloodSplatter)
//   - Camera shake (7DTD native: vp_FPCamera.DoBomb)
//   - Concussion (7DTD native: ScreenEffectManager)
//   - Motion blur, Chromatic aberration, DoF (Unity PostProcessing via reflection)
//   - Flash effect (GUI overlay)
//   - Radial blur (GUI overlay)
//
// USAGE:
//   CinematicScreenEffects.Instance.TriggerBloodSplatter(direction, intensity);
//   CinematicScreenEffects.Instance.EnableMotionBlur(intensity);
//   CinematicScreenEffects.Instance.DisableEffects();
//
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using System.Collections;
using System.Reflection;

namespace CinematicKill
{
    /// <summary>
    /// Manages screen effects like vignette, color grading, desaturation,
    /// blood splatter, concussion, camera shake, post-processing effects, and flash
    /// Uses 7DTD native effects and Unity Post-Processing where possible
    /// </summary>
    public class CinematicScreenEffects : MonoBehaviour
    {
        private static CinematicScreenEffects instance;
        
        // Effect states
        private bool isVignetteActive;
        private bool isColorGradingActive;
        private bool isFlashActive;
        private bool isDesaturationActive;
        private bool isRadialBlurActive;
        
        // Effect parameters
        private Color vignetteColor = Color.black;
        private float vignetteIntensity = 0.5f;
        private Color gradingColor = new Color(0f, 0f, 0.2f, 0.3f); // Default blue tint
        private Color flashColor = Color.white;
        private float flashDuration = 0.1f;
        private float flashTimer;
        
        // Desaturation parameters
        private float desaturationAmount = 0.5f;
        
        // Radial blur parameters
        private float radialBlurIntensity = 0.3f;
        private float radialBlurTimer;
        private float radialBlurDuration;
        
        // Concussion effect state
        private bool isConcussionActive;
        private bool wasMuffled;
        
        // Post-processing state tracking (using reflection for Unity.Postprocessing)
        private object motionBlurSettings;
        private object chromaticSettings;
        private object dofSettings;
        private bool motionBlurWasEnabled;
        private float originalMotionBlurIntensity;
        private bool chromaticWasEnabled;
        private float originalChromaticIntensity;
        private bool dofWasEnabled;
        private float originalDofFocusDistance;
        private float originalDofAperture;
        private float originalDofFocalLength;
        private bool postProcessingActive;
        
        // Textures
        private Texture2D vignetteTexture;
        private Texture2D whiteTexture;
        private Texture2D desatTexture;
        private Texture2D radialBlurTexture;
        
        public static CinematicScreenEffects Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("CinematicScreenEffects");
                    instance = go.AddComponent<CinematicScreenEffects>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            // Create textures
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
            
            // Create desaturation texture (gray)
            desatTexture = new Texture2D(1, 1);
            desatTexture.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 1f));
            desatTexture.Apply();
            
            // Create vignette texture (simple radial gradient)
            CreateVignetteTexture();
            
            // Create radial blur texture
            CreateRadialBlurTexture();
        }
        
        private void CreateVignetteTexture()
        {
            int size = 256;
            vignetteTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = Vector2.Distance(Vector2.zero, center);
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01((dist / maxDist) - 0.2f); // Start fading at 20% from center
                    alpha = Mathf.Pow(alpha, 2f); // Curve for smoother falloff
                    vignetteTexture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }
            vignetteTexture.Apply();
        }
        
        private void CreateRadialBlurTexture()
        {
            // Create a radial gradient for zoom blur effect (white center fading to transparent)
            int size = 256;
            radialBlurTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = Vector2.Distance(Vector2.zero, center);
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normalizedDist = dist / maxDist;
                    // Create streaks from center
                    float alpha = Mathf.Clamp01(normalizedDist * 0.8f);
                    alpha = Mathf.Pow(alpha, 1.5f);
                    radialBlurTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            radialBlurTexture.Apply();
        }
        
        public void EnableEffects(bool vignette, bool colorGrading, Color tintColor, float intensity)
        {
            isVignetteActive = vignette;
            isColorGradingActive = colorGrading;
            gradingColor = tintColor;
            vignetteIntensity = intensity;
        }
        
        /// <summary>
        /// Extended enable method with GUI-based effects only (vignette, color grading, desaturation)
        /// </summary>
        public void EnableEffectsExtended(
            bool vignette, float vignetteInt,
            bool colorGrading, Color tintColor,
            bool desaturation, float desatAmount)
        {
            isVignetteActive = vignette;
            vignetteIntensity = vignetteInt;
            
            isColorGradingActive = colorGrading;
            gradingColor = tintColor;
            
            isDesaturationActive = desaturation;
            desaturationAmount = desatAmount;
        }
        
        /// <summary>
        /// Triggers blood splatter effect using 7DTD's native damage overlay system
        /// </summary>
        /// <param name="direction">0=Front, 1=Back, 2=Left, 3=Right, 4=Random</param>
        /// <param name="intensity">Intensity multiplier for the effect (higher = more intense/closer)</param>
        public void TriggerBloodSplatter(int direction = 4, float intensity = 1.5f)
        {
            EntityPlayerLocal player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player == null) return;
            
            // Choose direction
            Utils.EnumHitDirection hitDir;
            if (direction == 4) // Random
            {
                int rand = UnityEngine.Random.Range(0, 4);
                hitDir = (Utils.EnumHitDirection)(rand + 1); // +1 because None is 0
            }
            else
            {
                hitDir = (Utils.EnumHitDirection)(direction + 1);
            }
            
            // Apply blood splatter with intensity
            // Multiple calls create a more intense effect
            player.lastHitDirection = hitDir;
            player.ForceBloodSplatter();
            
            // For higher intensity, trigger additional splatter effects
            if (intensity >= 1.5f)
            {
                player.ForceBloodSplatter();
            }
            if (intensity >= 2f)
            {
                player.ForceBloodSplatter();
            }
            
            CKLog.Verbose($"Blood splatter triggered (direction: {hitDir}, intensity: {intensity:F1}x)");
        }
        
        /// <summary>
        /// Triggers camera shake using 7DTD's native vp_FPCamera shake system
        /// </summary>
        public void TriggerCameraShake(float intensity, float duration)
        {
            EntityPlayerLocal player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player == null) return;
            
            // Use 7DTD's native camera shake via coroutine
            // shakeCamera is private, so use DoBomb which is public
            var camera = player.vp_FPCamera;
            if (camera != null)
            {
                // DoBomb creates an impact shake effect
                // Scale intensity to match expected shake strength
                Vector3 shakeForce = new Vector3(1f, -1f, 1f) * intensity;
                camera.DoBomb(shakeForce, 0.5f, 1.5f);
                CKLog.Verbose($"Camera shake triggered (intensity: {intensity})");
            }
        }
        
        /// <summary>
        /// Triggers concussion screen effect using 7DTD's screen effect system
        /// </summary>
        public void TriggerConcussion(float intensity, float duration, bool audioMuffle)
        {
            EntityPlayerLocal player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player == null) return;
            
            // Use 7DTD's screen effect manager for visual
            if (player.ScreenEffectManager != null)
            {
                // "Dying" effect creates a similar concussion-like screen distortion
                player.ScreenEffectManager.SetScreenEffect("Dying", intensity, duration);
                isConcussionActive = true;
                CKLog.Verbose($"Concussion visual triggered (intensity: {intensity}, duration: {duration}s)");
            }
            
            // Audio muffle via stunned state
            if (audioMuffle)
            {
                wasMuffled = player.isStunned;
                player.isStunned = true;
                StartCoroutine(EndConcussionAudio(player, duration));
                CKLog.Verbose("Concussion audio muffle enabled");
            }
        }
        
        /// <summary>
        /// Triggers Kill Flash effect - a bright flash effect on kills.
        /// Alias for TriggerXRay for semantic clarity with new naming.
        /// </summary>
        public void TriggerKillFlash(float duration, float intensity) => TriggerXRay(duration, intensity);
        
        /// <summary>
        /// Legacy method: Triggers X-Ray vision effect.
        /// Creates a bright flash followed by a brief inverted/high-contrast effect.
        /// </summary>
        public void TriggerXRay(float duration, float intensity)
        {
            EntityPlayerLocal player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player == null) return;
            
            // Initial bright flash
            TriggerFlash(new Color(1f, 1f, 0.9f, intensity), 0.1f);
            
            // Apply our GUI-based high-contrast effect (removed FadeToBlack as it caused screen to stay black)
            StartCoroutine(XRayEffectCoroutine(duration, intensity));
            
            CKLog.Verbose($"Kill Flash triggered (intensity: {intensity}, duration: {duration}s)");
        }
        
        private IEnumerator XRayEffectCoroutine(float duration, float intensity)
        {
            // Brief high-contrast color grading
            isColorGradingActive = true;
            gradingColor = new Color(0.9f, 0.95f, 1f, intensity * 0.3f); // Bright white-blue tint
            isVignetteActive = true;
            vignetteIntensity = intensity * 0.4f;
            vignetteColor = new Color(0.2f, 0.3f, 0.4f); // Dark blue vignette
            
            yield return new WaitForSecondsRealtime(duration);
            
            isColorGradingActive = false;
            isVignetteActive = false;
        }
        
        /// <summary>
        /// Triggers Kill Vignette effect - a strong vignette on kills.
        /// Alias for TriggerPredatorVision for semantic clarity with new naming.
        /// </summary>
        public void TriggerKillVignette(float duration, float intensity) => TriggerPredatorVision(duration, intensity);
        
        /// <summary>
        /// Legacy method: Triggers Predator Vision effect.
        /// Creates a high intensity vignette effect.
        /// </summary>
        public void TriggerPredatorVision(float duration, float intensity)
        {
            // Simple high-intensity vignette effect
            StartCoroutine(PredatorVisionCoroutine(duration, intensity));
            CKLog.Verbose($"Kill Vignette triggered (intensity: {intensity}, duration: {duration}s)");
        }
        
        private IEnumerator PredatorVisionCoroutine(float duration, float intensity)
        {
            // High intensity vignette - creates a focused, predatory look
            isVignetteActive = true;
            vignetteIntensity = intensity; // Use full intensity
            vignetteColor = Color.black; // Dark vignette edges
            
            yield return new WaitForSecondsRealtime(duration);
            
            isVignetteActive = false;
        }
        
        /// <summary>
        /// Triggers blood splatter screen overlay effect.
        /// </summary>
        public void TriggerBloodSplatter(float duration, float intensity)
        {
            StartCoroutine(BloodSplatterCoroutine(duration, intensity));
            CKLog.Verbose($"Blood Splatter triggered (intensity: {intensity}, duration: {duration}s)");
        }
        
        private IEnumerator BloodSplatterCoroutine(float duration, float intensity)
        {
            // Red blood splatter vignette effect
            isVignetteActive = true;
            vignetteIntensity = intensity * 0.8f;
            vignetteColor = new Color(0.5f, 0f, 0f); // Dark red blood color
            
            yield return new WaitForSecondsRealtime(duration);
            
            isVignetteActive = false;
        }
        
        /// <summary>
        /// Triggers desaturation effect - removes color from the screen for dramatic effect.
        /// Uses grayscale color grading similar to X-Ray but without the blue tint.
        /// </summary>
        public void TriggerDesaturation(float duration, float amount)
        {
            StartCoroutine(DesaturationCoroutine(duration, amount));
            CKLog.Verbose($"Desaturation triggered (amount: {amount}, duration: {duration}s)");
        }
        
        private IEnumerator DesaturationCoroutine(float duration, float amount)
        {
            // High contrast desaturation effect - makes darks darker and lights lighter
            // Creates a dramatic, stylized look without color
            isColorGradingActive = true;
            // Use high contrast settings: boost brightness and contrast together
            // Darker overall with high contrast punch
            gradingColor = new Color(0.2f, 0.2f, 0.2f, amount * 0.7f); // Dark, high contrast
            
            // Also add subtle vignette for focus
            isVignetteActive = true;
            vignetteIntensity = amount * 0.3f;
            vignetteColor = Color.black;
            
            yield return new WaitForSecondsRealtime(duration);
            
            isColorGradingActive = false;
            isVignetteActive = false;
        }
        
        /// <summary>
        /// Enables motion blur post-processing effect using reflection
        /// </summary>
        public void EnableMotionBlur(float intensity)
        {
            try
            {
                var profile = GetPostProcessProfile();
                if (profile == null) return;
                
                var motionBlur = GetPostProcessSetting(profile, "MotionBlur");
                if (motionBlur == null) return;
                
                if (!postProcessingActive)
                {
                    motionBlurSettings = motionBlur;
                    motionBlurWasEnabled = GetParameterValue<bool>(motionBlur, "enabled");
                    originalMotionBlurIntensity = GetParameterValue<float>(motionBlur, "shutterAngle");
                }
                
                SetParameterOverride(motionBlur, "enabled", true);
                SetParameterOverride(motionBlur, "shutterAngle", intensity * 360f); // Convert 0-1 to shutter angle
                CKLog.Verbose($"Motion blur enabled (intensity: {intensity})");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"CinematicKill: Failed to enable motion blur: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Enables chromatic aberration post-processing effect using reflection
        /// </summary>
        public void EnableChromaticAberration(float intensity)
        {
            try
            {
                var profile = GetPostProcessProfile();
                if (profile == null) return;
                
                var chromatic = GetPostProcessSetting(profile, "ChromaticAberration");
                if (chromatic == null) return;
                
                if (!postProcessingActive)
                {
                    chromaticSettings = chromatic;
                    chromaticWasEnabled = GetParameterValue<bool>(chromatic, "enabled");
                    originalChromaticIntensity = GetParameterValue<float>(chromatic, "intensity");
                }
                
                SetParameterOverride(chromatic, "enabled", true);
                SetParameterOverride(chromatic, "intensity", intensity);
                CKLog.Verbose($"Chromatic aberration enabled (intensity: {intensity})");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"CinematicKill: Failed to enable chromatic aberration: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Enables depth of field post-processing effect using reflection
        /// </summary>
        public void EnableDepthOfField(float focusDistance, float aperture, float focalLength)
        {
            try
            {
                var profile = GetPostProcessProfile();
                if (profile == null) return;
                
                var dof = GetPostProcessSetting(profile, "DepthOfField");
                if (dof == null) return;
                
                if (!postProcessingActive)
                {
                    dofSettings = dof;
                    dofWasEnabled = GetParameterValue<bool>(dof, "enabled");
                    originalDofFocusDistance = GetParameterValue<float>(dof, "focusDistance");
                    originalDofAperture = GetParameterValue<float>(dof, "aperture");
                    originalDofFocalLength = GetParameterValue<float>(dof, "focalLength");
                }
                
                SetParameterOverride(dof, "enabled", true);
                SetParameterOverride(dof, "focusDistance", focusDistance);
                SetParameterOverride(dof, "aperture", aperture);
                SetParameterOverride(dof, "focalLength", focalLength);
                CKLog.Verbose($"Depth of field enabled (focus: {focusDistance}m, aperture: f/{aperture})");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"CinematicKill: Failed to enable depth of field: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Mark post-processing as active (call after enabling all desired effects)
        /// </summary>
        public void MarkPostProcessingActive()
        {
            postProcessingActive = true;
        }
        
        /// <summary>
        /// Triggers radial blur (zoom blur) effect using GUI overlay
        /// </summary>
        public void TriggerRadialBlur(float intensity, float duration)
        {
            isRadialBlurActive = true;
            radialBlurIntensity = intensity;
            radialBlurDuration = duration;
            radialBlurTimer = duration;
            CKLog.Verbose($"Radial blur triggered (intensity: {intensity}, duration: {duration}s)");
        }
        
        /// <summary>
        /// Restores all post-processing effects to their original state
        /// Forces effects off if the user has them disabled in settings
        /// </summary>
        public void RestorePostProcessing()
        {
            try
            {
                var profile = GetPostProcessProfile();
                if (profile != null)
                {
                    // Get global visuals settings to check if effects should stay disabled
                    var settings = CinematicKillManager.GetCurrentSettings();
                    var globalVisuals = settings?.MenuV2?.GlobalVisuals;
                    
                    // Motion blur - restore or force off
                    if (motionBlurSettings != null)
                    {
                        bool shouldBeEnabled = postProcessingActive ? motionBlurWasEnabled : false;
                        if (globalVisuals != null && !globalVisuals.EnableMotionBlur)
                        {
                            shouldBeEnabled = false;
                        }
                        SetParameterOverride(motionBlurSettings, "enabled", shouldBeEnabled);
                        if (postProcessingActive)
                        {
                            SetParameterOverride(motionBlurSettings, "shutterAngle", originalMotionBlurIntensity);
                        }
                    }
                    
                    // Chromatic aberration - restore or force off
                    if (chromaticSettings != null)
                    {
                        bool shouldBeEnabled = postProcessingActive ? chromaticWasEnabled : false;
                        if (globalVisuals != null && !globalVisuals.EnableChromaticAberration)
                        {
                            shouldBeEnabled = false;
                        }
                        SetParameterOverride(chromaticSettings, "enabled", shouldBeEnabled);
                        if (postProcessingActive)
                        {
                            SetParameterOverride(chromaticSettings, "intensity", originalChromaticIntensity);
                        }
                    }
                    
                    // Depth of field - restore or force off
                    if (dofSettings != null)
                    {
                        bool shouldBeEnabled = postProcessingActive ? dofWasEnabled : false;
                        if (globalVisuals != null && !globalVisuals.EnableDepthOfField)
                        {
                            shouldBeEnabled = false;
                        }
                        SetParameterOverride(dofSettings, "enabled", shouldBeEnabled);
                        if (postProcessingActive)
                        {
                            SetParameterOverride(dofSettings, "focusDistance", originalDofFocusDistance);
                            SetParameterOverride(dofSettings, "aperture", originalDofAperture);
                            SetParameterOverride(dofSettings, "focalLength", originalDofFocalLength);
                        }
                    }
                    
                    // Also try to force-disable chromatic if not already captured
                    if (chromaticSettings == null && globalVisuals != null && !globalVisuals.EnableChromaticAberration)
                    {
                        var chromatic = GetPostProcessSetting(profile, "ChromaticAberration");
                        if (chromatic != null)
                        {
                            SetParameterOverride(chromatic, "enabled", false);
                        }
                    }
                }
                
                if (postProcessingActive)
                {
                    CKLog.Verbose("Post-processing effects restored");
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"CinematicKill: Failed to restore post-processing: {ex.Message}");
            }
            
            postProcessingActive = false;
            motionBlurSettings = null;
            chromaticSettings = null;
            dofSettings = null;
        }
        
        /// <summary>
        /// Gets the PostProcessProfile using reflection to avoid direct assembly reference
        /// </summary>
        private object GetPostProcessProfile()
        {
            try
            {
                Camera main = Camera.main;
                if (main == null) return null;
                
                // Get PostProcessVolume component using reflection
                var volumeType = System.Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessVolume, Unity.Postprocessing.Runtime");
                if (volumeType == null)
                {
                    // Try alternate assembly name
                    volumeType = System.Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessVolume, Assembly-CSharp");
                }
                if (volumeType == null) return null;
                
                var volume = main.GetComponent(volumeType);
                if (volume == null) return null;
                
                // Get profile property
                var profileProp = volumeType.GetProperty("profile");
                return profileProp?.GetValue(volume);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"CinematicKill: Failed to get post-process profile: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets a post-processing setting by name using reflection
        /// </summary>
        private object GetPostProcessSetting(object profile, string settingName)
        {
            try
            {
                // Get the GetSetting method
                var profileType = profile.GetType();
                var methods = profileType.GetMethods();
                
                foreach (var method in methods)
                {
                    if (method.Name == "GetSetting" && method.IsGenericMethod)
                    {
                        // Find the setting type
                        var settingType = System.Type.GetType($"UnityEngine.Rendering.PostProcessing.{settingName}, Unity.Postprocessing.Runtime");
                        if (settingType == null)
                        {
                            settingType = System.Type.GetType($"UnityEngine.Rendering.PostProcessing.{settingName}, Assembly-CSharp");
                        }
                        if (settingType == null) return null;
                        
                        var genericMethod = method.MakeGenericMethod(settingType);
                        return genericMethod.Invoke(profile, null);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"CinematicKill: Failed to get setting {settingName}: {ex.Message}");
            }
            return null;
        }
        
        /// <summary>
        /// Gets a parameter value from a post-processing setting using reflection
        /// </summary>
        private T GetParameterValue<T>(object setting, string parameterName)
        {
            try
            {
                var settingType = setting.GetType();
                var paramField = settingType.GetField(parameterName);
                if (paramField == null) return default(T);
                
                var param = paramField.GetValue(setting);
                if (param == null) return default(T);
                
                // Get the value property from the parameter
                var paramType = param.GetType();
                var valueProp = paramType.GetProperty("value");
                if (valueProp == null) return default(T);
                
                return (T)valueProp.GetValue(param);
            }
            catch
            {
                return default(T);
            }
        }
        
        /// <summary>
        /// Sets a parameter override using reflection
        /// </summary>
        private void SetParameterOverride(object setting, string parameterName, object value)
        {
            try
            {
                var settingType = setting.GetType();
                var paramField = settingType.GetField(parameterName);
                if (paramField == null) return;
                
                var param = paramField.GetValue(setting);
                if (param == null) return;
                
                // Call Override method on the parameter
                var paramType = param.GetType();
                var overrideMethod = paramType.GetMethod("Override", new System.Type[] { value.GetType() });
                if (overrideMethod != null)
                {
                    overrideMethod.Invoke(param, new object[] { value });
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"CinematicKill: Failed to set parameter {parameterName}: {ex.Message}");
            }
        }
        
        private IEnumerator EndConcussionAudio(EntityPlayerLocal player, float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            if (player != null && !wasMuffled)
            {
                player.isStunned = false;
            }
            isConcussionActive = false;
        }
        
        public void TriggerFlash(Color color, float duration)
        {
            isFlashActive = true;
            flashColor = color;
            flashDuration = duration;
            flashTimer = duration;
        }
        
        public void DisableEffects()
        {
            isVignetteActive = false;
            isColorGradingActive = false;
            isFlashActive = false;
            isDesaturationActive = false;
            isRadialBlurActive = false;
            
            // Restore post-processing effects
            RestorePostProcessing();
            
            // Stop any running experimental effect coroutines
            StopAllCoroutines();
            
            // Clear experimental screen effects (NightVision, X-Ray, etc.)
            EntityPlayerLocal player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player?.ScreenEffectManager != null)
            {
                // Disable NightVision (Predator Vision)
                player.ScreenEffectManager.DisableScreenEffect("NightVision");
                
                // Disable any other experimental effects that might be active
                player.ScreenEffectManager.DisableScreenEffect("Blinded");
                player.ScreenEffectManager.DisableScreenEffect("FlashBang");
            }
            
            // End concussion audio if active
            if (isConcussionActive)
            {
                if (player != null)
                {
                    player.ScreenEffectManager?.DisableScreenEffect("Dying");
                    if (!wasMuffled)
                    {
                        player.isStunned = false;
                    }
                }
                isConcussionActive = false;
            }
        }
        
        private void Update()
        {
            if (isFlashActive)
            {
                flashTimer -= Time.unscaledDeltaTime;
                if (flashTimer <= 0f)
                {
                    isFlashActive = false;
                }
            }
            
            if (isRadialBlurActive)
            {
                radialBlurTimer -= Time.unscaledDeltaTime;
                if (radialBlurTimer <= 0f)
                {
                    isRadialBlurActive = false;
                }
            }
        }
        
        private void OnGUI()
        {
            if (!isVignetteActive && !isColorGradingActive && !isFlashActive && !isDesaturationActive && !isRadialBlurActive)
            {
                return;
            }
            
            // Draw Radial Blur (zoom blur effect)
            if (isRadialBlurActive && radialBlurTimer > 0f)
            {
                float progress = radialBlurTimer / radialBlurDuration;
                float alpha = radialBlurIntensity * progress;
                GUI.color = new Color(1f, 1f, 1f, alpha * 0.4f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), radialBlurTexture);
            }
            
            // Draw Desaturation (bluish tinted overlay for cinematic look)
            if (isDesaturationActive && desaturationAmount > 0f)
            {
                // Draw a blue-tinted overlay for cinematic desaturation effect
                GUI.color = new Color(0.35f, 0.35f, 0.55f, desaturationAmount * 0.5f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTexture);
            }
            
            // Draw Color Grading (Tint)
            if (isColorGradingActive)
            {
                GUI.color = gradingColor;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTexture);
            }
            
            // Draw Vignette
            if (isVignetteActive)
            {
                GUI.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, vignetteIntensity);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture);
            }
            
            // Draw Flash
            if (isFlashActive)
            {
                float alpha = flashTimer / flashDuration;
                GUI.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTexture);
            }
            
            GUI.color = Color.white;
        }
        
        private void OnDestroy()
        {
            // Restore post-processing before destruction
            RestorePostProcessing();
            
            if (whiteTexture != null) Destroy(whiteTexture);
            if (vignetteTexture != null) Destroy(vignetteTexture);
            if (desatTexture != null) Destroy(desatTexture);
            if (radialBlurTexture != null) Destroy(radialBlurTexture);
        }
    }
}
