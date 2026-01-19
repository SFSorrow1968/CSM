namespace CSM.Configuration
{
    public class TriggerSettings
    {
        public bool Enabled { get; set; } = true;
        public float Chance { get; set; } = 1.0f;
        public float TimeScale { get; set; } = 0.2f;
        public float Duration { get; set; } = 1.5f;
        public float Cooldown { get; set; } = 0f;

        public static TriggerSettings GetDefaults(TriggerType type)
        {
            return type switch
            {
                TriggerType.BasicKill => new TriggerSettings
                {
                    Enabled = true,
                    Chance = 0.15f,
                    TimeScale = 0.3f,
                    Duration = 1.0f,
                    Cooldown = 0f
                },
                TriggerType.Critical => new TriggerSettings
                {
                    Enabled = true,
                    Chance = 1.0f,
                    TimeScale = 0.2f,
                    Duration = 1.5f,
                    Cooldown = 0f
                },
                TriggerType.Dismemberment => new TriggerSettings
                {
                    Enabled = true,
                    Chance = 0.8f,
                    TimeScale = 0.2f,
                    Duration = 1.5f,
                    Cooldown = 0f
                },
                TriggerType.Decapitation => new TriggerSettings
                {
                    Enabled = true,
                    Chance = 1.0f,
                    TimeScale = 0.15f,
                    Duration = 2.0f,
                    Cooldown = 0f
                },
                TriggerType.Parry => new TriggerSettings
                {
                    Enabled = true,
                    Chance = 0.5f,
                    TimeScale = 0.25f,
                    Duration = 1.0f,
                    Cooldown = 0f
                },
                TriggerType.LastEnemy => new TriggerSettings
                {
                    Enabled = true,
                    Chance = 1.0f,
                    TimeScale = 0.15f,
                    Duration = 2.5f,
                    Cooldown = 0f
                },
                TriggerType.LastStand => new TriggerSettings
                {
                    Enabled = true,
                    Chance = 1.0f,
                    TimeScale = 0.1f,
                    Duration = 5.0f,
                    Cooldown = 0f
                },
                _ => new TriggerSettings()
            };
        }
    }
}
