using UnityEngine;

namespace CinematicKill
{
    /// <summary>
    /// Tracks projectile entities for debugging and camera follow purposes.
    /// Attach to projectile GameObjects to monitor their flight path.
    /// </summary>
    public class ProjectileTracker : MonoBehaviour
    {
        private Entity entity;
        private float lifetime;
        private float logTimer;
        
        private const float LogInterval = 0.1f;

        public void Initialize(Entity targetEntity)
        {
            entity = targetEntity;
            CKLog.Verbose($"ProjectileTracker attached to {entity.GetType().Name} (ID: {entity.entityId})");
        }

        private void Update()
        {
            if (entity == null)
            {
                Destroy(this);
                return;
            }

            lifetime += Time.deltaTime;
        }

        private void LateUpdate()
        {
            if (entity == null) return;

            // Verbose logging for projectile tracking
            logTimer += Time.deltaTime;
            if (logTimer >= LogInterval)
            {
                logTimer = 0f;
                CKLog.Verbose($"TRACKING: Time={lifetime:F2}, Pos={transform.position}, Rot={transform.rotation.eulerAngles}, Vel={entity.motion}");
            }
        }

        private void OnDestroy()
        {
            if (entity != null)
            {
                CKLog.Verbose($"ProjectileTracker destroyed for {entity.GetType().Name} after {lifetime:F2}s. Final Pos: {entity.position}");
            }
        }
    }
}
