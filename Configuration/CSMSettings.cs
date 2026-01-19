using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace CSM.Configuration
{
    public class CSMSettings
    {
        private static CSMSettings _instance;
        public static CSMSettings Instance => _instance ??= new CSMSettings();

        public bool Enabled { get; set; } = true;
        public bool VerboseLogging { get; set; } = false;
        public float GlobalCooldown { get; set; } = 0f;
        public float LastStandHealthThreshold { get; set; } = 0.15f;
        public float CriticalDamageThreshold { get; set; } = 50f;
        public Dictionary<TriggerType, TriggerSettings> Triggers { get; set; }

        private string _settingsPath;

        public CSMSettings()
        {
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            Triggers = new Dictionary<TriggerType, TriggerSettings>();
            foreach (TriggerType type in Enum.GetValues(typeof(TriggerType)))
            {
                Triggers[type] = TriggerSettings.GetDefaults(type);
            }
        }

        public void Load(string modPath)
        {
            _settingsPath = Path.Combine(modPath, "settings.json");

            if (File.Exists(_settingsPath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsPath);
                    var loaded = JsonConvert.DeserializeObject<CSMSettings>(json);
                    if (loaded != null)
                    {
                        Enabled = loaded.Enabled;
                        VerboseLogging = loaded.VerboseLogging;
                        GlobalCooldown = loaded.GlobalCooldown;
                        LastStandHealthThreshold = loaded.LastStandHealthThreshold;
                        CriticalDamageThreshold = loaded.CriticalDamageThreshold;
                        if (loaded.Triggers != null)
                        {
                            foreach (var kvp in loaded.Triggers)
                                Triggers[kvp.Key] = kvp.Value;
                        }
                    }
                    Debug.Log("[CSM] Settings loaded.");
                }
                catch (Exception ex)
                {
                    Debug.LogError("[CSM] Failed to load settings: " + ex.Message);
                    InitializeDefaults();
                }
            }
            else
            {
                Save();
            }
            _instance = this;
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(_settingsPath)) return;
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError("[CSM] Failed to save settings: " + ex.Message);
            }
        }

        public TriggerSettings Get(TriggerType type)
        {
            return Triggers.TryGetValue(type, out var settings) ? settings : TriggerSettings.GetDefaults(type);
        }
    }
}
