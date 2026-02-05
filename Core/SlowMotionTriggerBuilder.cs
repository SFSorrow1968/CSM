using CSM.Configuration;
using ThunderRoad;

namespace CSM.Core
{
    /// <summary>
    /// Fluent builder for triggering slow motion effects.
    /// Usage: CSMManager.Instance.Trigger(TriggerType.BasicKill).WithCreature(creature).Execute();
    /// </summary>
    public class SlowMotionTriggerBuilder
    {
        private readonly CSMManager _manager;
        private readonly TriggerType _type;
        private float _damageDealt;
        private Creature _targetCreature;
        private DamageType _damageType = DamageType.Unknown;
        private float _intensity;
        private bool _isQuickTest;
        private bool _isStatusKill;
        private bool _isThrown;

        internal SlowMotionTriggerBuilder(CSMManager manager, TriggerType type)
        {
            _manager = manager;
            _type = type;
        }

        /// <summary>
        /// Set the amount of damage dealt (used for logging/analytics).
        /// </summary>
        public SlowMotionTriggerBuilder WithDamage(float damage)
        {
            _damageDealt = damage;
            return this;
        }

        /// <summary>
        /// Set the target creature for killcam tracking.
        /// </summary>
        public SlowMotionTriggerBuilder WithCreature(Creature creature)
        {
            _targetCreature = creature;
            return this;
        }

        /// <summary>
        /// Set the damage type for multiplier calculation.
        /// </summary>
        public SlowMotionTriggerBuilder WithDamageType(DamageType damageType)
        {
            _damageType = damageType;
            return this;
        }

        /// <summary>
        /// Set the impact intensity (0-1) for intensity multiplier.
        /// </summary>
        public SlowMotionTriggerBuilder WithIntensity(float intensity)
        {
            _intensity = intensity;
            return this;
        }

        /// <summary>
        /// Mark this as a quick test trigger (for debug purposes).
        /// </summary>
        public SlowMotionTriggerBuilder AsQuickTest()
        {
            _isQuickTest = true;
            return this;
        }

        /// <summary>
        /// Mark this as a status effect kill (DOT from fire, lightning, DOT bleeds).
        /// </summary>
        public SlowMotionTriggerBuilder AsStatusKill()
        {
            _isStatusKill = true;
            return this;
        }

        /// <summary>
        /// Mark this as a thrown weapon kill.
        /// </summary>
        public SlowMotionTriggerBuilder AsThrown()
        {
            _isThrown = true;
            return this;
        }

        /// <summary>
        /// Execute the slow motion trigger with all configured parameters.
        /// </summary>
        /// <returns>True if slow motion was triggered, false otherwise.</returns>
        public bool Execute()
        {
            return _manager.TriggerSlow(
                _type,
                _damageDealt,
                _targetCreature,
                _damageType,
                _intensity,
                _isQuickTest,
                _isStatusKill,
                _isThrown
            );
        }

        /// <summary>
        /// Execute the slow motion trigger and return detailed result.
        /// </summary>
        /// <returns>The result of the trigger attempt.</returns>
        public TriggerResult ExecuteWithResult()
        {
            return _manager.TriggerSlowWithResult(
                _type,
                _damageDealt,
                _targetCreature,
                _damageType,
                _intensity,
                _isQuickTest,
                _isStatusKill,
                _isThrown
            );
        }
    }
}
