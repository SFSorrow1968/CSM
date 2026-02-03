namespace CSM.Core
{
    /// <summary>
    /// Represents the result of a slow motion trigger attempt.
    /// Provides detailed information about why a trigger succeeded or failed.
    /// </summary>
    public enum TriggerResult
    {
        /// <summary>Slow motion was successfully triggered.</summary>
        Success,

        /// <summary>The mod is disabled in settings.</summary>
        ModDisabled,

        /// <summary>DOT kills are disabled (0x multiplier).</summary>
        DOTKillDisabled,

        /// <summary>Thrown weapon kills are disabled (0x multiplier).</summary>
        ThrownWeaponDisabled,

        /// <summary>This damage type is disabled (0x multiplier).</summary>
        DamageTypeDisabled,

        /// <summary>This trigger type is disabled in settings.</summary>
        TriggerDisabled,

        /// <summary>Global cooldown is still active.</summary>
        GlobalCooldown,

        /// <summary>This trigger's specific cooldown is still active.</summary>
        TriggerCooldown,

        /// <summary>Slow motion is already active with equal or higher priority.</summary>
        AlreadyActive,

        /// <summary>Time scale is currently transitioning back to normal (easing out).</summary>
        EasingOut,

        /// <summary>Random chance roll failed.</summary>
        ChanceFailed,

        /// <summary>An error occurred during trigger processing.</summary>
        Error
    }
}
