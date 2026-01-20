namespace CinematicKill
{
    /// <summary>
    /// Centralized logging helper that respects verbose logging setting.
    /// Use Verbose() for debug/spam messages, Out() for important user-facing messages.
    /// </summary>
    public static class CKLog
    {
        private const string PREFIX = "[CinematicKill] ";
        
        /// <summary>
        /// Log verbose/debug message - only shown when EnableVerboseLogging is true
        /// </summary>
        public static void Verbose(string message)
        {
            // Use direct Settings accessor (no clone) for performance
            if (CinematicKillManager.Settings?.EnableVerboseLogging == true)
            {
                Log.Out(PREFIX + message);
            }
        }
        
        /// <summary>
        /// Log important message - always shown regardless of verbose setting
        /// </summary>
        public static void Out(string message)
        {
            Log.Out(PREFIX + message);
        }
        
        /// <summary>
        /// Log warning - always shown
        /// </summary>
        public static void Warning(string message)
        {
            Log.Warning(PREFIX + message);
        }
        
        /// <summary>
        /// Log error - always shown
        /// </summary>
        public static void Error(string message)
        {
            Log.Error(PREFIX + message);
        }
    }
}
