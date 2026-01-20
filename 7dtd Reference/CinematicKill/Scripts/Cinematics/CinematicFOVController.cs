using UnityEngine;

namespace CinematicKill
{
    /// <summary>
    /// Controls cinematic FOV zoom effect during kill sequences
    /// </summary>
    public class CinematicFOVController
    {
        private enum FovPhase
        {
            Inactive,
            Entering,
            Holding,
            Exiting
        }

        private bool isActive;
        private Camera playerCamera;
        private float originalFOV;
        
        // Configuration
        private float targetFOV;
        private float enterDuration;
        private float holdDuration;
        private float exitDuration;
        
        // Runtime state
        private FovPhase currentPhase;
        private float phaseTimer;
        private float currentFOV;

        public bool IsActive => isActive;

        /// <summary>
        /// Start the FOV zoom effect
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <param name="zoomAmount">How much to reduce FOV (degrees)</param>
        /// <param name="enterTime">Desired duration of entry phase (seconds)</param>
        /// <param name="holdTime">Desired duration of hold phase (seconds)</param>
        /// <param name="exitTime">Desired duration of return phase (seconds)</param>
        /// <param name="totalDuration">Total slow motion duration (seconds) - phases will be scaled to fit within this</param>
        public void StartFOVEffect(EntityPlayerLocal player, float zoomAmount, float enterTime, float holdTime, float exitTime, float totalDuration, bool targetIsAbsolute = false)
        {
            if (player == null)
            {
                Log.Warning("CinematicKill: Cannot start FOV effect - invalid player");
                return;
            }

            if (isActive)
            {
                Log.Warning("CinematicKill: FOV effect already active");
                return;
            }

            // Get the player's camera
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Log.Warning("CinematicKill: Cannot find main camera for FOV effect");
                return;
            }

            // Save original FOV
            originalFOV = playerCamera.fieldOfView;
            currentFOV = originalFOV;

            // Calculate target FOV
            if (targetIsAbsolute)
            {
                // zoomAmount is the absolute target FOV
                targetFOV = Mathf.Max(10f, zoomAmount);
            }
            else
            {
                // zoomAmount is a multiplier (0.7 = 70% of original FOV = zoom in)
                // Lower multiplier = more zoom (smaller FOV)
                targetFOV = Mathf.Max(10f, originalFOV * zoomAmount);
            }

            // Fixed ratios already provided (20/70/10) - scale uniformly to fit total duration
            float desiredTotal = enterTime + holdTime + exitTime;
            float scaleFactor = desiredTotal > 0f ? Mathf.Min(1f, totalDuration / desiredTotal) : 1f;
            
            // Apply scaling and enforce minimums
            this.enterDuration = Mathf.Max(0.05f, enterTime * scaleFactor);
            this.holdDuration = Mathf.Max(0f, holdTime * scaleFactor);
            this.exitDuration = Mathf.Max(0.05f, exitTime * scaleFactor);
            
            float actualTotal = this.enterDuration + this.holdDuration + this.exitDuration;

            // Initialize state
            currentPhase = FovPhase.Entering;
            phaseTimer = 0f;
            isActive = true;

            CKLog.Verbose($"Starting FOV effect (original: {originalFOV:F1}°, target: {targetFOV:F1}°, phases: {this.enterDuration:F2}s/{this.holdDuration:F2}s/{this.exitDuration:F2}s = {actualTotal:F2}s total)");
        }

        /// <summary>
        /// Update the FOV effect - call this every frame while active
        /// </summary>
        /// <param name="deltaTime">Time since last frame</param>
        public void Update(float deltaTime)
        {
            if (!isActive || playerCamera == null)
            {
                return;
            }

            phaseTimer += deltaTime;

            switch (currentPhase)
            {
                case FovPhase.Entering:
                    UpdateEnter();
                    break;

                case FovPhase.Holding:
                    UpdateHold();
                    break;

                case FovPhase.Exiting:
                    UpdateExit();
                    break;
            }

            // Apply the current FOV to the camera
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = currentFOV;
            }
        }

        private void UpdateEnter()
        {
            if (phaseTimer >= enterDuration)
            {
                // Entry complete, move to hold phase
                currentFOV = targetFOV;
                currentPhase = FovPhase.Holding;
                phaseTimer = 0f;
            }
            else
            {
                // Smooth interpolation from original to target FOV
                float t = phaseTimer / enterDuration;
                // Use smooth step for ease-in-out effect
                t = Mathf.SmoothStep(0f, 1f, t);
                currentFOV = Mathf.Lerp(originalFOV, targetFOV, t);
            }
        }

        private void UpdateHold()
        {
            if (phaseTimer >= holdDuration)
            {
                // Hold complete, move to return phase
                currentPhase = FovPhase.Exiting;
                phaseTimer = 0f;
            }
            else
            {
                // Maintain zoomed FOV
                currentFOV = targetFOV;
            }
        }

        private void UpdateExit()
        {
            if (phaseTimer >= exitDuration)
            {
                // Zoom out complete, effect finished
                currentFOV = originalFOV;
                StopFOVEffect();
            }
            else
            {
                // Smooth interpolation from target back to original FOV
                float t = phaseTimer / exitDuration;
                // Use smooth step for ease-in-out effect
                t = Mathf.SmoothStep(0f, 1f, t);
                currentFOV = Mathf.Lerp(targetFOV, originalFOV, t);
            }
        }

        /// <summary>
        /// Forces the current FOV to be applied to the camera.
        /// Call this after the game's native camera update to prevent FOV override.
        /// </summary>
        public void ApplyCurrentFOV()
        {
            if (isActive && playerCamera != null)
            {
                playerCamera.fieldOfView = currentFOV;
            }
        }

        /// <summary>
        /// Stop the FOV effect and restore original FOV immediately
        /// </summary>
        public void StopFOVEffect()
        {
            if (!isActive)
            {
                return;
            }

            // Restore original FOV
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = originalFOV;
                CKLog.Verbose($"FOV effect stopped, restored to {originalFOV:F1}°");
            }

            isActive = false;
            currentPhase = FovPhase.Inactive;
            phaseTimer = 0f;
            playerCamera = null;
        }

        /// <summary>
        /// Force cleanup in case of errors
        /// </summary>
        public void ForceCleanup()
        {
            if (isActive)
            {
                Log.Warning("CinematicKill: Force cleanup of FOV controller");
                StopFOVEffect();
            }
        }

        /// <summary>
        /// Get the current zoom phase for debugging
        /// </summary>
        public string GetCurrentPhase()
        {
            return currentPhase.ToString();
        }
    }
}
