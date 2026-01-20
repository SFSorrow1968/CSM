using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CinematicKill
{
    /// <summary>
    /// Localization helper that loads strings from Config/Localization.txt.
    /// Parses the CSV file directly and caches all translations.
    /// Falls back to provided default values if key is not found.
    /// </summary>
    public static class CKLocalization
    {
        private static bool _initialized = false;
        private static Dictionary<string, string> _strings = new Dictionary<string, string>();

        /// <summary>
        /// Gets a localized string.
        /// </summary>
        public static string Get(string key, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(key))
            {
                return defaultValue;
            }

            // Try our loaded strings first
            if (_strings.TryGetValue(key, out string value))
            {
                return value;
            }

            // Try 7DTD's built-in Localization system as backup
            try
            {
                string result = Localization.Get(key, false);
                if (!string.IsNullOrEmpty(result) && result != key)
                {
                    return result;
                }
            }
            catch
            {
                // Localization system may not be available during startup
            }

            // Return default value or key itself
            return string.IsNullOrEmpty(defaultValue) ? key : defaultValue;
        }

        /// <summary>
        /// Short alias for Get() - makes menu code more concise.
        /// </summary>
        public static string L(string key, string defaultValue = "")
        {
            return Get(key, defaultValue);
        }

        /// <summary>
        /// Localizes a standard camera preset name (1. Standard, 2. Close-High, etc.)
        /// </summary>
        public static string LocalizePresetName(string presetName)
        {
            return presetName switch
            {
                "1. Standard" => L("ck_preset_standard", "1. Standard"),
                "2. Close-High" => L("ck_preset_close_high", "2. Close-High"),
                "3. Medium-Ground" => L("ck_preset_medium_ground", "3. Medium-Ground"),
                "4. Far-High" => L("ck_preset_far_high", "4. Far-High"),
                "5. Tight-Elevated" => L("ck_preset_tight_elevated", "5. Tight-Elevated"),
                "6. Close-Ground" => L("ck_preset_close_ground", "6. Close-Ground"),
                "7. Close-Mid" => L("ck_preset_close_mid", "7. Close-Mid"),
                _ => presetName
            };
        }

        /// <summary>
        /// Localizes a trigger reason (BasicKill, Headshot, etc.) for display
        /// </summary>
        public static string LocalizeTriggerReason(string reason)
        {
            return reason switch
            {
                "BasicKill" => L("ck_trigger_basic_kill", "Basic Kill"),
                "LastEnemy" => L("ck_trigger_last_enemy", "Last Enemy"),
                "LongRange" => L("ck_trigger_long_range", "Long Range"),
                "LowHealth" => L("ck_trigger_low_health", "Low Health"),
                "Projectile" => L("ck_trigger_projectile_kill", "Projectile Kill"),
                "Hitscan" => L("ck_trigger_ranged_kill", "Ranged Kill"),
                "Crit" => L("ck_trigger_critical", "Critical Hit"),
                "Dismember" => L("ck_trigger_dismember", "Dismember"),
                "Killstreak" => L("ck_trigger_killstreak", "Killstreak"),
                "Headshot" => L("ck_trigger_headshot", "Headshot"),
                "Sneak" => L("ck_trigger_sneak_kill", "Sneak Kill"),
                "None" => L("ck_ui_none", "None"),
                _ => reason
            };
        }

        /// <summary>
        /// Initialize localization by loading Config/Localization.txt.
        /// </summary>
        public static void Load()
        {
            if (_initialized) return;
            _initialized = true;

            // Find mod path using ModManager
            Mod mod = ModManager.GetMod("CinematicKill");
            if (mod == null)
            {
                Log.Warning("[CinematicKill] Could not find mod via ModManager - using fallback localization");
                return;
            }

            string locPath = Path.Combine(mod.Path, "Config", "Localization.txt");
            if (!File.Exists(locPath))
            {
                Log.Warning($"[CinematicKill] Localization file not found at: {locPath}");
                return;
            }

            try
            {
                LoadLocalizationFile(locPath);
                Log.Out($"[CinematicKill] Loaded {_strings.Count} localization strings from: {locPath}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CinematicKill] Failed to load localization file: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses the Localization.txt CSV file.
        /// Detects the player's current language and loads strings from the appropriate column.
        /// Falls back to English if the player's language isn't available.
        /// </summary>
        private static void LoadLocalizationFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            if (lines.Length < 2) return;

            // Get the current game language (e.g., "german", "french", "english")
            string currentLanguage = "english";
            try
            {
                currentLanguage = Localization.language?.ToLowerInvariant() ?? "english";
            }
            catch
            {
                // Localization system may not be available during early startup
            }

            // Parse header to find language column indices
            string[] headers = ParseCSVLine(lines[0]);
            int keyIndex = -1;
            int englishIndex = -1;
            int targetLangIndex = -1;

            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i].Trim().ToLowerInvariant();
                if (header == "key")
                {
                    keyIndex = i;
                }
                else if (header == "english")
                {
                    englishIndex = i;
                }
                
                // Check for exact match with current language
                if (header == currentLanguage)
                {
                    targetLangIndex = i;
                }
            }

            if (keyIndex < 0)
            {
                Log.Warning("[CinematicKill] Localization file missing 'Key' column");
                return;
            }

            if (englishIndex < 0)
            {
                Log.Warning("[CinematicKill] Localization file missing 'english' column");
                return;
            }

            // Use current language if found, otherwise fallback to English
            int langIndex = targetLangIndex >= 0 ? targetLangIndex : englishIndex;
            string usedLanguage = targetLangIndex >= 0 ? currentLanguage : "english";
            
            if (targetLangIndex < 0 && currentLanguage != "english")
            {
                Log.Out($"[CinematicKill] Language '{currentLanguage}' not found in localization file, using English");
            }
            else
            {
                Log.Out($"[CinematicKill] Loading localization for language: {usedLanguage}");
            }

            // Parse data rows
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] columns = ParseCSVLine(line);
                if (columns.Length <= langIndex || columns.Length <= keyIndex) continue;

                string key = columns[keyIndex].Trim();
                string value = columns[langIndex].Trim();

                // If target language value is empty, fall back to English
                if (string.IsNullOrEmpty(value) && langIndex != englishIndex && columns.Length > englishIndex)
                {
                    value = columns[englishIndex].Trim();
                }

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                {
                    _strings[key] = value;
                }
            }
        }

        /// <summary>
        /// Parses a single CSV line, handling quoted fields with commas.
        /// </summary>
        private static string[] ParseCSVLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // Check for escaped quote
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
