namespace CSM.Configuration
{
    /// <summary>
    /// Trigger types for slow motion events.
    /// Integer values represent priority (higher = higher priority).
    /// </summary>
    public enum TriggerType
    {
        BasicKill = 10,
        Dismemberment = 20,
        Critical = 30,
        Parry = 40,
        Decapitation = 50,
        LastEnemy = 60,
        LastStand = 100
    }
}
