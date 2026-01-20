using HarmonyLib;
using System.Reflection;

namespace CinematicKill
{
    public sealed class Init : IModApi
    {
        private static bool s_initialized;

        public void InitMod(Mod modInstance)
        {
            if (s_initialized)
            {
                return;
            }

            s_initialized = true;
            Log.Out(" Loading Patch: " + GetType());

            // Initialize localization first
            CKLocalization.Load();
            
            CinematicKillManager.Initialize(modInstance?.Path);
            ModEvents.GameStartDone.RegisterHandler(CinematicKillManager.OnGameStartDone);

            var harmony = new Harmony(GetType().FullName);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
