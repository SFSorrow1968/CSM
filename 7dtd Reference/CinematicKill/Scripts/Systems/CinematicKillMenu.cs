// ═══════════════════════════════════════════════════════════════════════════════
// CinematicKillMenu.cs - IMGUI configuration menu for CinematicKill mod
// ═══════════════════════════════════════════════════════════════════════════════
//
// UI STRUCTURE:
//   Tab 0 - Main: Basic Kill settings, camera presets, timing
//   Tab 1 - Triggers: Headshot, Critical, Dismember, LongRange, etc.
//   Tab 2 - Visuals: Screen effects, post-processing
//   Tab 3 - Modes: Per-weapon overrides (Melee, Ranged, Bow, etc.)
//   Tab 4 - Advanced: Experimental features, export/import
//
// KEY METHODS:
//   OnGUI()           - Main render loop
//   DrawMainTab()     - Basic Kill UI
//   DrawTriggersTab() - Trigger toggles
//   SliderRow()       - Reusable slider helper
//
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CinematicKill
{
    /// <summary>
    /// Simplified IMGUI-based Cinematic Kill configuration menu.
    /// Two tabs: Main (Basic Kill + Special Triggers) and Triggers (toggles + overrides)
    /// </summary>
    public class CinematicKillMenu : MonoBehaviour
    {
        #region Fields

        #region Singleton
        private static CinematicKillMenu _instance;
        public static CinematicKillMenu Instance => _instance;
        #endregion

        #region Tab State
        private string[] _tabs;
        private int _selectedTab = 0;
        #endregion

        #region Localization
        /// <summary>
        /// Shorthand for localization lookup
        /// </summary>
        private static string L(string key, string fallback = "") => CKLocalization.Get(key, fallback);
        #endregion

        #region Window State
        private bool _showWindow = false;
        private Rect _windowRect = new Rect(50, 50, 1100, 800);
        private Vector2 _scrollPos = Vector2.zero;

        // Per-trigger override expansion
        private bool _headshotExpanded = false;
        private bool _criticalExpanded = false;
        private bool _lastEnemyExpanded = false;
        private bool _longRangeExpanded = false;
        private bool _lowHealthExpanded = false;
        private bool _dismemberExpanded = false;
        private bool _killstreakExpanded = false;
        private bool _sneakExpanded = false;
        
        // Camera sub-tab state (0 = First Person, 1 = Projectile)
        private int _cameraSubTab = 0;
        
        // Advanced FOV timing expansion for camera sub-tabs
        private bool _fpAdvancedFOVExpanded = false;
        private bool _projAdvancedFOVExpanded = false;
        private bool _triggerAdvancedFOVExpanded = false;
        private bool _projBasicKillFOVTimingExpanded = false;
        private bool _projTriggerFOVTimingExpanded = false;
        
        // Randomize mode tracking - key is slider ID, value is whether randomize mode is active
        private Dictionary<string, bool> _randomizeModes = new Dictionary<string, bool>();

        // Save/Reset/Export/Import feedback
        private float _resetFlashTime = 0f;
        private float _exportFlashTime = 0f;
        private float _importFlashTime = 0f;
        private const float FLASH_DURATION = 2f;
        private const float FOOTER_HEIGHT = 70f;
        
        // Summary tab section expansion states - all collapsed by default
        private bool _summaryBKExpanded = false;
        private bool _summaryTriggersExpanded = false;
        private bool _summaryProjCamExpanded = false;
        private bool _summaryCameraConfigExpanded = false;  // Camera Configuration section
        private bool _summaryWeaponModesExpanded = false;
        private bool _summaryEffectsExpanded = false;
        private bool _summarySmartHUDExpanded = false;
        #endregion

        #region Styles & Colors
        private bool _stylesInitialized = false;
        private GUIStyle _windowStyle;
        private GUIStyle _tabNormalStyle;
        private GUIStyle _tabSelectedStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _expandButtonStyle;

        // ═══════════════════════════════════════════════════════════════════════
        //  COLOR PALETTE - Semantic naming for consistent UI color coding
        // ═══════════════════════════════════════════════════════════════════════
        
        // --- Background & Base ---
        private readonly Color _bgColor = new Color(0.05f, 0.05f, 0.08f, 0.98f);
        private readonly Color _accentRed = new Color(0.85f, 0.19f, 0.19f);  // Headers, branding
        
        // --- Status Colors (for toggles, enabled/disabled states) ---
        private readonly Color _enabledColor = new Color(0.3f, 0.9f, 0.4f);       // Bright green - ON/active
        private readonly Color _disabledColor = new Color(0.6f, 0.3f, 0.3f);      // Muted red - OFF/inactive
        private readonly Color _warningColor = new Color(1f, 0.7f, 0.3f);         // Orange - caution/warning
        private readonly Color _infoColor = new Color(0.5f, 0.75f, 0.95f);        // Light blue - informational
        private readonly Color _mutedColor = new Color(0.5f, 0.5f, 0.5f);         // Grey - disabled text
        
        // --- Toggle Button Colors ---
        private readonly Color _toggleOnColor = new Color(0.2f, 0.75f, 0.35f);    // Green toggle background
        private readonly Color _toggleOffColor = new Color(0.45f, 0.2f, 0.2f);    // Dark red toggle background
        
        // --- Category Colors (for differentiating feature types) ---
        private readonly Color _triggerColor = new Color(0.95f, 0.6f, 0.2f);      // Orange - Triggers
        private readonly Color _cameraColor = new Color(0.4f, 0.7f, 0.95f);       // Blue - Camera settings
        private readonly Color _effectColor = new Color(0.85f, 0.5f, 0.85f);      // Purple - Visual effects
        private readonly Color _timingColor = new Color(0.6f, 0.9f, 0.7f);        // Mint - Duration/timing
        private readonly Color _modeColor = new Color(0.9f, 0.85f, 0.4f);         // Gold - Weapon modes
        
        // --- Action Button Colors ---
        private readonly Color _saveColor = new Color(0.2f, 0.55f, 0.25f);        // Green - Save
        private readonly Color _resetColor = new Color(0.65f, 0.45f, 0.15f);      // Amber - Reset
        private readonly Color _exportColor = new Color(0.25f, 0.45f, 0.65f);     // Blue - Export
        private readonly Color _importColor = new Color(0.55f, 0.3f, 0.6f);       // Purple - Import
        
        // --- Expanded/Collapsed Indicators ---
        private static readonly Color ExpandedIndicatorColor = new Color(0.4f, 0.9f, 0.5f);
        private static readonly Color CollapsedIndicatorColor = new Color(0.65f, 0.65f, 0.65f);
        
        // --- Timing Validation ---
        private static readonly Color TimingOkColor = new Color(0.5f, 0.85f, 0.55f);
        private static readonly Color TimingWarningColor = new Color(1f, 0.55f, 0.3f);
        
        // --- Legacy compatibility aliases (reference semantics for consistency) ---
        private Color DisabledTextColor => _mutedColor;
        private Color InfoTextColor => _infoColor;

        private Texture2D _bgTex;
        #endregion

        #region Settings
        private CinematicKillSettings _settings;
        #endregion

        #endregion Fields

        #region Unity Lifecycle

        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            _tabs = new string[] { 
                L("ck_tab_summary", "Summary"), 
                L("ck_tab_main", "Main"), 
                L("ck_tab_triggers", "Triggers"),
                L("ck_tab_camera", "Camera"),
                L("ck_tab_effects", "Effects"), 
                L("ck_tab_hud", "HUD"),
                L("ck_tab_advanced", "Advanced"),
                L("ck_tab_experimental", "⚗ Experimental")
            };
            RefreshSettings();
        }

        /// <summary>
        /// Refreshes settings from CinematicKillManager
        /// </summary>
        public void RefreshSettings()
        {
            _settings = CinematicKillManager.GetSettings();
            if (_settings == null)
            {
                _settings = CinematicKillSettings.Default;
            }
        }

        private void Update()
        {
            // Use settings MenuKey if available, otherwise fall back to default (Backslash)
            KeyCode menuKey = _settings?.MenuKey ?? KeyCode.Backslash;
            
            if (Input.GetKeyDown(menuKey))
            {
                // Refresh settings if they were null
                if (_settings == null)
                {
                    RefreshSettings();
                }
                ToggleMenu();
            }
        }

        /// <summary>
        /// Toggle menu visibility
        /// Auto-saves settings when menu closes
        /// </summary>
        public void ToggleMenu()
        {
            bool wasVisible = _showWindow;
            _showWindow = !_showWindow;
            
            if (_showWindow)
            {
                // Menu opening - refresh settings
                RefreshSettings();
            }
            else if (wasVisible)
            {
                // Menu closing - auto-save settings to file
                CinematicKillManager.SaveSettingsToFile();
                CKLog.Verbose(" Settings auto-saved on menu close");
            }
        }

        public bool IsVisible => _showWindow;

        private void OnGUI()
        {
            if (!_showWindow) return;
            
            // CRITICAL: Ensure ALL required state is initialized BEFORE calling GUI.Window
            // This prevents GUI layout errors when menu opens before Start() completes
            
            // Initialize tabs if needed (normally done in Start)
            if (_tabs == null)
            {
                _tabs = new string[] { 
                    L("ck_tab_summary", "Summary"), 
                    L("ck_tab_main", "Main"), 
                    L("ck_tab_triggers", "Triggers"),
                    L("ck_tab_camera", "Camera"),
                    L("ck_tab_effects", "Effects"), 
                    L("ck_tab_hud", "HUD"),
                    L("ck_tab_advanced", "Advanced"),
                    L("ck_tab_experimental", "⚗ Experimental")
                };
            }
            
            // Ensure settings are available
            if (_settings == null)
            {
                RefreshSettings();
                if (_settings == null) 
                {
                    _settings = CinematicKillSettings.Default;
                }
            }
            
            // Ensure styles are ready (creates _bgTex)
            EnsureStyles();
            
            // Final safety check - don't render if critical state is missing
            if (_tabs == null || _settings == null || _bgTex == null || !_stylesInitialized)
            {
                return;
            }

            _windowRect = GUI.Window(94301, _windowRect, DrawWindow, "", _windowStyle);
        }

        #endregion Unity Lifecycle

        #region Core Window Drawing

        private void DrawWindow(int windowId)
        {
            // NOTE: All initialization is handled in OnGUI before calling GUI.Window
            // DrawWindow should NEVER return early as it breaks Layout/Repaint event balance
            
            // Background
            GUI.DrawTexture(new Rect(0, 0, _windowRect.width, _windowRect.height), _bgTex);

            // Title
            GUI.Label(new Rect(20, 10, 300, 40), L("ck_menu_title", "CINEMATIC KILL"), _sectionTitleStyle);

            // Close button
            if (GUI.Button(new Rect(_windowRect.width - 50, 10, 40, 40), "X", _buttonStyle))
            {
                // Auto-save settings to disk when closing
                CinematicKillManager.SaveSettingsToFile();
                _showWindow = false;
            }

            // Tabs
            float tabY = 55;
            float tabWidth = (_windowRect.width - 40) / _tabs.Length;
            for (int i = 0; i < _tabs.Length; i++)
            {
                Rect tabRect = new Rect(20 + i * tabWidth, tabY, tabWidth - 4, 35);
                GUIStyle style = (i == _selectedTab) ? _tabSelectedStyle : _tabNormalStyle;
                if (GUI.Button(tabRect, _tabs[i], style))
                {
                    _selectedTab = i;
                    _scrollPos = Vector2.zero;
                }
            }

            // Content area
            float contentY = tabY + 45;
            float contentHeight = _windowRect.height - contentY - FOOTER_HEIGHT - 10;
            Rect contentRect = new Rect(20, contentY, _windowRect.width - 40, contentHeight);

            _scrollPos = GUI.BeginScrollView(contentRect, _scrollPos,
                new Rect(0, 0, contentRect.width - 20, GetContentHeight()));

            GUILayout.BeginVertical(GUILayout.Width(contentRect.width - 30));
            GUILayout.Space(10);

            switch (_selectedTab)
            {
                case 0: DrawSummaryTab(); break;
                case 1: DrawMainTab(); break;
                case 2: DrawTriggersTab(); break;
                case 3: DrawCameraTab(); break;
                case 4: DrawEffectsTab(); break;
                case 5: DrawHUDTab(); break;
                case 6: DrawAdvancedTab(); break;
                case 7: DrawExperimentalTab(); break;
            }

            GUILayout.Space(20);
            GUILayout.EndVertical();

            GUI.EndScrollView();

            // Footer
            DrawFooter(contentRect.y + contentRect.height + 5);

            GUI.DragWindow(new Rect(0, 0, _windowRect.width - 60, 50));
        }

        private float GetContentHeight()
        {
            switch (_selectedTab)
            {
                case 0: return 600;    // Summary - two-column layout
                case 1: return 1200;   // Main - Basic Kill + Trigger Defaults (FOV removed)
                case 2: return 2200;   // Triggers - two columns + overrides
                case 3: return 1800;   // Camera - FP/Proj sub-tabs with FOV settings
                case 4: return 1200;   // Effects with randomization
                case 5: return 500;    // HUD - notifications and hiding
                case 6: return 1000;   // Advanced - Developer tools, logging, import/export
                case 7: return 1800;   // Experimental - X-Ray, Predator Vision, Last Stand, Chain Reaction, etc.
                default: return 1200;
            }
        }

        #endregion Core Window Drawing

        #region Tab Drawing

        // ═══════════════════════════════════════════════════════════════
        //  TAB 0: SUMMARY / QUICK EDIT - Comprehensive settings overview
        // ═══════════════════════════════════════════════════════════════
        private void DrawSummaryTab()
        {
            // Master toggle at top
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            _settings.EnableCinematics = QuickToggle(L("ck_ui_cinematics", "CINEMATICS"), _settings.EnableCinematics, 120);
            if (!_settings.EnableCinematics)
            {
                GUI.color = _mutedColor;
                GUILayout.Label("  " + L("ck_ui_all_cinematic_effects_are_disabl_349f4e", "All cinematic effects are disabled. Toggle to enable."), _labelStyle);
                GUI.color = Color.white;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            if (!_settings.EnableCinematics)
            {
                GUILayout.Space(20);
                return;
            }
            
            GUILayout.Space(10);
            
            // ═══════════════════════════════════════════════════════════════
            // TWO-COLUMN LAYOUT
            // ═══════════════════════════════════════════════════════════════
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            
            // ══════════════════════════════════════════════════════════════
            // LEFT COLUMN - MAIN, TRIGGERS, CAMERA
            // ══════════════════════════════════════════════════════════════
            GUILayout.BeginVertical(GUILayout.Width(500));
            
            // ─────────────────────────────────────────────────────────────
            // MAIN SECTION
            // ─────────────────────────────────────────────────────────────
            if (CollapsibleSection(L("ck_section_main", "MAIN"), ref _summaryBKExpanded, true, ""))
            {
                GUILayout.Space(5);
                
                // Smart Indoor/Outdoor
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                _settings.SmartIndoorOutdoorDetection = QuickToggle(L("ck_ui_smart_i_o", "Smart I/O"), _settings.SmartIndoorOutdoorDetection, 85);
                if (_settings.SmartIndoorOutdoorDetection)
                {
                    _settings.IndoorDetectionHeight = QuickTextBox(_settings.IndoorDetectionHeight, 35);
                    GUILayout.Label(L("ck_ui_m", "m"), _labelStyle, GUILayout.Width(15));
                }
                GUILayout.Space(15);
                _settings.EnableLastStand = QuickToggle(L("ck_trigger_last_stand", "Last Stand"), _settings.EnableLastStand, 90);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.Space(8);
                
                // Basic Kill Section
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUI.color = _cameraColor;
                GUILayout.Label(L("ck_ui_basic_kill", "── Basic Kill ──"), _labelStyle, GUILayout.Width(110));
                GUI.color = Color.white;
                _settings.BasicKill.Enabled = QuickToggle(L("ck_ui_enable", "Enable"), _settings.BasicKill.Enabled, 65);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                if (_settings.BasicKill.Enabled)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(L("ck_ui_chance", "Chance"), _labelStyle, GUILayout.Width(50));
                    _settings.BasicKill.Chance = QuickTextBox(_settings.BasicKill.Chance, 35);
                    GUILayout.Label("%", _labelStyle, GUILayout.Width(15));
                    GUILayout.Space(10);
                    GUILayout.Label(L("ck_ui_dur", "Dur"), _labelStyle, GUILayout.Width(25));
                    _settings.BasicKill.Duration = QuickTextBox(_settings.BasicKill.Duration, 35);
                    GUILayout.Label(L("ck_ui_s", "s"), _labelStyle, GUILayout.Width(10));
                    GUILayout.Space(10);
                    GUILayout.Label(L("ck_ui_scale", "Scale"), _labelStyle, GUILayout.Width(35));
                    _settings.BasicKill.TimeScale = QuickTextBox(_settings.BasicKill.TimeScale, 40);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    _settings.BasicKill.FirstPersonCamera = QuickToggle(L("ck_fp", "FP"), _settings.BasicKill.FirstPersonCamera, 35);
                    _settings.BasicKill.ProjectileCamera = QuickToggle(L("ck_proj", "Proj"), _settings.BasicKill.ProjectileCamera, 45);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.Space(8);
                
                // Trigger Defaults Section
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUI.color = _cameraColor;
                GUILayout.Label(L("ck_ui_triggers_2", "── Triggers ──"), _labelStyle, GUILayout.Width(110));
                GUI.color = Color.white;
                _settings.TriggerDefaults.EnableTriggers = QuickToggle(L("ck_ui_enable", "Enable"), _settings.TriggerDefaults.EnableTriggers, 65);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                if (_settings.TriggerDefaults.EnableTriggers)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(L("ck_ui_dur", "Dur"), _labelStyle, GUILayout.Width(25));
                    _settings.TriggerDefaults.Duration = QuickTextBox(_settings.TriggerDefaults.Duration, 35);
                    GUILayout.Label(L("ck_ui_s", "s"), _labelStyle, GUILayout.Width(10));
                    GUILayout.Space(10);
                    GUILayout.Label(L("ck_ui_scale", "Scale"), _labelStyle, GUILayout.Width(35));
                    _settings.TriggerDefaults.TimeScale = QuickTextBox(_settings.TriggerDefaults.TimeScale, 40);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    _settings.TriggerDefaults.FirstPersonCamera = QuickToggle(L("ck_fp", "FP"), _settings.TriggerDefaults.FirstPersonCamera, 35);
                    _settings.TriggerDefaults.ProjectileCamera = QuickToggle(L("ck_proj", "Proj"), _settings.TriggerDefaults.ProjectileCamera, 45);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.Space(5);
            }
            
            GUILayout.Space(8);
            
            // ─────────────────────────────────────────────────────────────
            // TRIGGERS SECTION
            // ─────────────────────────────────────────────────────────────
            if (CollapsibleSection(L("ck_section_triggers", "TRIGGERS"), ref _summaryTriggersExpanded, _settings.TriggerDefaults.EnableTriggers, ""))
            {
                GUILayout.Space(5);
                
                // Weapon Modes
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUI.color = _cameraColor;
                GUILayout.Label(L("ck_ui_weapons", "Weapons:"), _labelStyle, GUILayout.Width(65));
                GUI.color = Color.white;
                _settings.MeleeEnabled = QuickToggle(L("ck_melee", "Melee"), _settings.MeleeEnabled, 55);
                _settings.RangedEnabled = QuickToggle(L("ck_ranged", "Ranged"), _settings.RangedEnabled, 60);
                _settings.BowEnabled = QuickToggle(L("ck_bow", "Bow"), _settings.BowEnabled, 45);
                _settings.ExplosiveEnabled = QuickToggle(L("ck_ui_expl", "Expl"), _settings.ExplosiveEnabled, 45);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                
                // Active Triggers - Row 1
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUI.color = _cameraColor;
                GUILayout.Label(L("ck_ui_triggers", "Triggers:"), _labelStyle, GUILayout.Width(65));
                GUI.color = Color.white;
                _settings.Headshot.Enabled = QuickToggle(L("ck_ui_head", "Head"), _settings.Headshot.Enabled, 50);
                _settings.Critical.Enabled = QuickToggle(L("ck_ui_crit", "Crit"), _settings.Critical.Enabled, 45);
                _settings.LastEnemy.Enabled = QuickToggle(L("ck_ui_last", "Last"), _settings.LastEnemy.Enabled, 45);
                _settings.LongRange.Enabled = QuickToggle(L("ck_ui_long", "Long"), _settings.LongRange.Enabled, 50);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                // Active Triggers - Row 2
                GUILayout.BeginHorizontal();
                GUILayout.Space(75);
                _settings.LowHealth.Enabled = QuickToggle(L("ck_ui_low_hp", "Low HP"), _settings.LowHealth.Enabled, 60);
                _settings.Dismember.Enabled = QuickToggle(L("ck_ui_dismem", "Dismem"), _settings.Dismember.Enabled, 60);
                _settings.Killstreak.Enabled = QuickToggle(L("ck_ui_streak", "Streak"), _settings.Killstreak.Enabled, 55);
                _settings.Sneak.Enabled = QuickToggle(L("ck_trigger_sneak", "Sneak"), _settings.Sneak.Enabled, 55);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
            }
            
            GUILayout.Space(8);
            
            // ─────────────────────────────────────────────────────────────
            // CAMERA SECTION
            // ─────────────────────────────────────────────────────────────
            if (CollapsibleSection(L("ck_section_camera", "CAMERA"), ref _summaryCameraConfigExpanded, true, ""))
            {
                GUILayout.Space(5);
                
                // FP Camera
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUI.color = _cameraColor;
                GUILayout.Label(L("ck_ui_first_person", "── First Person ──"), _labelStyle, GUILayout.Width(130));
                GUI.color = Color.white;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GUILayout.Label(L("ck_ui_fov", "FOV:"), _labelStyle, GUILayout.Width(30));
                string fovLabel = _settings.BasicKill.FOVMode == FOVMode.Off ? L("ck_off", "Off") : 
                    (_settings.BasicKill.FOVMode == FOVMode.ZoomIn ? L("ck_in", "In") : L("ck_out", "Out"));
                if (GUILayout.Button(fovLabel, _labelStyle, GUILayout.Width(30)))
                {
                    _settings.BasicKill.FOVMode = (FOVMode)(((int)_settings.BasicKill.FOVMode + 1) % 3);
                }
                if (_settings.BasicKill.FOVMode != FOVMode.Off)
                {
                    _settings.BasicKill.FOVPercent = QuickTextBox(_settings.BasicKill.FOVPercent, 30);
                    GUILayout.Label("%", _labelStyle, GUILayout.Width(12));
                }
                GUILayout.Space(15);
                _settings.FPFreezeFrame.Enabled = QuickToggle(L("ck_ui_freeze", "Freeze"), _settings.FPFreezeFrame.Enabled, 60);
                if (_settings.FPFreezeFrame.Enabled)
                {
                    _settings.FPFreezeFrame.Chance = QuickTextBox(_settings.FPFreezeFrame.Chance, 30);
                    GUILayout.Label("%", _labelStyle, GUILayout.Width(12));
                    _settings.FPFreezeFrame.Duration = QuickTextBox(_settings.FPFreezeFrame.Duration, 30);
                    GUILayout.Label(L("ck_ui_s", "s"), _labelStyle, GUILayout.Width(10));
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.Space(8);
                
                // Projectile Camera
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUI.color = _cameraColor;
                GUILayout.Label(L("ck_ui_projectile", "── Projectile ──"), _labelStyle, GUILayout.Width(130));
                GUI.color = Color.white;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                _settings.ProjectileCamera.UseStandardPresets = QuickToggle(L("ck_presets_enabled", "Presets"), _settings.ProjectileCamera.UseStandardPresets, 65);
                _settings.Experimental.EnableProjectileRideCam = QuickToggle(L("ck_ui_ride", "Ride"), _settings.Experimental.EnableProjectileRideCam, 45);
                if (_settings.Experimental.EnableProjectileRideCam)
                {
                    _settings.Experimental.RideCamChance = QuickTextBox(_settings.Experimental.RideCamChance, 30);
                    GUILayout.Label("%", _labelStyle, GUILayout.Width(12));
                }
                _settings.Experimental.EnableChainReaction = QuickToggle(L("ck_ui_chain", "Chain"), _settings.Experimental.EnableChainReaction, 50);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                // Dynamic Zoom row
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                _settings.ProjectileCamera.EnableDynamicZoomIn = QuickToggle(L("ck_ui_zoomin", "ZoomIn"), _settings.ProjectileCamera.EnableDynamicZoomIn, 60);
                _settings.ProjectileCamera.EnableDynamicZoomOut = QuickToggle(L("ck_ui_zoomout", "ZoomOut"), _settings.ProjectileCamera.EnableDynamicZoomOut, 65);
                if (_settings.ProjectileCamera.EnableDynamicZoomIn && _settings.ProjectileCamera.EnableDynamicZoomOut)
                {
                    _settings.ProjectileCamera.DynamicZoomBalance = QuickTextBox(_settings.ProjectileCamera.DynamicZoomBalance, 30);
                    GUILayout.Label("%", _labelStyle, GUILayout.Width(12));
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                // Freeze Frame row
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                _settings.ProjectileFreezeFrame.Enabled = QuickToggle(L("ck_ui_freeze", "Freeze"), _settings.ProjectileFreezeFrame.Enabled, 60);
                if (_settings.ProjectileFreezeFrame.Enabled)
                {
                    _settings.ProjectileFreezeFrame.Chance = QuickTextBox(_settings.ProjectileFreezeFrame.Chance, 30);
                    GUILayout.Label("%", _labelStyle, GUILayout.Width(12));
                    _settings.ProjectileFreezeFrame.Duration = QuickTextBox(_settings.ProjectileFreezeFrame.Duration, 30);
                    GUILayout.Label(L("ck_ui_s", "s"), _labelStyle, GUILayout.Width(10));
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical(); // End left column
            
            GUILayout.Space(20);
            
            // ══════════════════════════════════════════════════════════════
            // RIGHT COLUMN - EFFECTS, HUD, ADVANCED, EXPERIMENTAL
            // ══════════════════════════════════════════════════════════════
            GUILayout.BeginVertical(GUILayout.Width(500));
            
            // ─────────────────────────────────────────────────────────────
            // EFFECTS SECTION
            // ─────────────────────────────────────────────────────────────
            if (CollapsibleSection(L("ck_section_effects", "EFFECTS"), ref _summaryEffectsExpanded, true, ""))
            {
                GUILayout.Space(5);
                var fx = _settings.ScreenEffects;
                
                // Kill Flash
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                fx.EnableKillFlash = QuickToggle(L("ck_ui_kill_flash_2", "Kill Flash"), fx.EnableKillFlash, 80);
                if (fx.EnableKillFlash)
                {
                    fx.KillFlashIntensity = QuickTextBox(fx.KillFlashIntensity, 35);
                }
                GUILayout.Space(10);
                fx.EnableBloodSplatter = QuickToggle(L("ck_blood", "Blood"), fx.EnableBloodSplatter, 55);
                if (fx.EnableBloodSplatter)
                {
                    fx.BloodSplatterIntensity = QuickTextBox(fx.BloodSplatterIntensity, 35);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                fx.EnableVignette = QuickToggle(L("ck_vignette", "Vignette"), fx.EnableVignette, 75);
                if (fx.EnableVignette)
                {
                    fx.VignetteIntensity = QuickTextBox(fx.VignetteIntensity, 35);
                }
                GUILayout.Space(10);
                fx.EnableDesaturation = QuickToggle(L("ck_ui_desat", "Desat"), fx.EnableDesaturation, 55);
                if (fx.EnableDesaturation)
                {
                    fx.DesaturationAmount = QuickTextBox(fx.DesaturationAmount, 35);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
            }
            
            GUILayout.Space(8);
            
            // ─────────────────────────────────────────────────────────────
            // HUD SECTION
            // ─────────────────────────────────────────────────────────────
            if (CollapsibleSection(L("ck_section_hud", "HUD"), ref _summarySmartHUDExpanded, true, ""))
            {
                GUILayout.Space(5);
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                _settings.HUD.Enabled = QuickToggle(L("ck_ui_notifications", "Notifications"), _settings.HUD.Enabled, 100);
                if (_settings.HUD.Enabled)
                {
                    GUILayout.Label(L("ck_opacity", "Opacity"), _labelStyle, GUILayout.Width(45));
                    _settings.HUD.Opacity = QuickTextBox(_settings.HUD.Opacity, 35);
                    GUILayout.Space(10);
                    GUILayout.Label(L("ck_ui_dur", "Dur"), _labelStyle, GUILayout.Width(25));
                    _settings.HUD.MessageDuration = QuickTextBox(_settings.HUD.MessageDuration, 35);
                    GUILayout.Label(L("ck_ui_s", "s"), _labelStyle, GUILayout.Width(10));
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                _settings.HUDElements.HideAllHUDDuringCinematic = QuickToggle(L("ck_ui_hide_game_hud", "Hide Game HUD"), _settings.HUDElements.HideAllHUDDuringCinematic, 115);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
            }
            
            GUILayout.Space(8);
            
            // ─────────────────────────────────────────────────────────────
            // ADVANCED SECTION
            // ─────────────────────────────────────────────────────────────
            if (CollapsibleSection(L("ck_section_advanced", "ADVANCED"), ref _summaryWeaponModesExpanded, true, ""))
            {
                GUILayout.Space(5);
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                _settings.SmartIndoorOutdoorDetection = QuickToggle(L("ck_ui_smart_i_o", "Smart I/O"), _settings.SmartIndoorOutdoorDetection, 80);
                if (_settings.SmartIndoorOutdoorDetection)
                {
                    _settings.IndoorDetectionHeight = QuickTextBox(_settings.IndoorDetectionHeight, 35);
                    GUILayout.Label(L("ck_ui_m", "m"), _labelStyle, GUILayout.Width(12));
                }
                GUILayout.Space(15);
                _settings.EnableVerboseLogging = QuickToggle(L("ck_ui_verbose_log", "Verbose Log"), _settings.EnableVerboseLogging, 95);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                _settings.AdvancedMode = QuickToggle(L("ck_ui_advanced_mode", "Advanced Mode"), _settings.AdvancedMode, 115);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
            }
            
            GUILayout.Space(8);
            
            // ─────────────────────────────────────────────────────────────
            // EXPERIMENTAL SECTION
            // ─────────────────────────────────────────────────────────────
            var exp = _settings.Experimental;
            if (CollapsibleSection(L("ck_section_experimental", "⚗ EXPERIMENTAL"), ref _summaryProjCamExpanded, true, ""))
            {
                GUILayout.Space(5);
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                exp.EnableXRayVision = QuickToggle(L("ck_ui_x_ray", "X-Ray"), exp.EnableXRayVision, 55);
                if (exp.EnableXRayVision)
                {
                    exp.XRayDuration = QuickTextBox(exp.XRayDuration, 30);
                    GUILayout.Label(L("ck_ui_s", "s"), _labelStyle, GUILayout.Width(10));
                }
                GUILayout.Space(10);
                exp.EnablePredatorVision = QuickToggle(L("ck_ui_predator", "Predator"), exp.EnablePredatorVision, 70);
                if (exp.EnablePredatorVision)
                {
                    exp.PredatorVisionDuration = QuickTextBox(exp.PredatorVisionDuration, 30);
                    GUILayout.Label(L("ck_ui_s", "s"), _labelStyle, GUILayout.Width(10));
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                exp.EnableDismemberFocusCam = QuickToggle(L("ck_ui_dismember_cam", "Dismember Cam"), exp.EnableDismemberFocusCam, 115);
                exp.EnableLastStand = QuickToggle(L("ck_trigger_last_stand", "Last Stand"), exp.EnableLastStand, 90);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical(); // End right column
            
            GUILayout.Space(10);
            
            GUILayout.EndHorizontal(); // End two-column layout
        }
        
        // Quick toggle helper - clickable text toggle
        private bool QuickToggle(string label, bool value, int width)
        {
            Color oldColor = GUI.color;
            GUI.color = value ? _enabledColor : _mutedColor;
            string text = value ? $"● {label}" : $"○ {label}";
            if (GUILayout.Button(text, _labelStyle, GUILayout.Width(width)))
            {
                value = !value;
            }
            GUI.color = oldColor;
            return value;
        }
        
        // Quick text box helper - editable numeric value
        private float QuickTextBox(float value, int width)
        {
            string text = value.ToString("0.##");
            string newText = GUILayout.TextField(text, GUILayout.Width(width));
            if (newText != text && float.TryParse(newText, out float parsed))
            {
                return parsed;
            }
            return value;
        }


        
        // Compact weapon mode display for summary
        private void WeaponModeCompact(string name, bool enabled, CameraOverride camOverride)
        {
            Color oldColor = GUI.color;
            GUI.color = enabled ? _enabledColor : _mutedColor;
            string indicator = enabled ? "●" : "○";
            string overrideText = "";
            if (enabled && camOverride != CameraOverride.Auto)
            {
                overrideText = camOverride == CameraOverride.FirstPersonOnly ? "[FP]" : "[P]";
            }
            GUILayout.Label($"{indicator}{name}{overrideText}", _labelStyle, GUILayout.Width(85));
            GUI.color = oldColor;
        }
        
        // Compact trigger summary for 2-column display
        private void TriggerSummaryCompact(string name, CKTriggerSettings trigger)
        {
            Color oldColor = GUI.color;
            GUI.color = _enabledColor;
            string overrideIndicator = trigger.Override ? "*" : "";
            GUILayout.Label($"● {name}{overrideIndicator}", _labelStyle, GUILayout.Width(100));
            GUI.color = oldColor;
        }
        
        // Compact effect summary for horizontal layout
        private void EffectSummaryCompact(string name, bool enabled, string value)
        {
            Color oldColor = GUI.color;
            GUI.color = enabled ? _enabledColor : _mutedColor;
            string text = enabled ? $"● {name}:{value}" : $"○ {name}";
            GUILayout.Label(text, _labelStyle, GUILayout.Width(120));
            GUI.color = oldColor;
        }

        // Color-coded status row with enabled/disabled value display
        private void ColorCodedStatusRow(string label, bool enabled, string enabledText, string disabledText)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  {label}:", _labelStyle, GUILayout.Width(140));
            Color oldColor = GUI.color;
            GUI.color = enabled ? _enabledColor : _disabledColor;
            GUILayout.Label(enabled ? $"● {enabledText}" : $"○ {disabledText}", _valueStyle);
            GUI.color = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        
        // Enhanced status row with icon prefix for summary tab
        private void StatusRowWithIcon(string icon, string label, bool enabled, string enabledText, string disabledText)
        {
            GUILayout.BeginHorizontal();
            Color oldColor = GUI.color;
            GUI.color = enabled ? _enabledColor : _mutedColor;
            GUILayout.Label($"  {icon} {label}:", _labelStyle, GUILayout.Width(160));
            GUI.color = enabled ? _enabledColor : _disabledColor;
            GUILayout.Label(enabled ? enabledText : disabledText, _valueStyle);
            GUI.color = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Renders a clickable section header that acts as a toggle.
        /// When enabled: red text, returns true (caller shows content)
        /// When disabled: grey text, returns false (caller hides content)
        /// </summary>
        private bool ToggleableHeader(string title, ref bool enabled)
        {
            GUILayout.BeginHorizontal();
            
            // Store ALL original colors to prevent state leakage
            Color oldColor = GUI.color;
            Color oldContentColor = GUI.contentColor;
            Color oldBgColor = GUI.backgroundColor;
            
            // Set colors based on enabled state
            if (enabled)
            {
                GUI.contentColor = new Color(1f, 0.35f, 0.35f); // Bright vibrant red when enabled
            }
            else
            {
                GUI.contentColor = _mutedColor; // Grey when disabled
            }
            
            // Create clickable button styled as header
            string indicator = enabled ? "▼" : "▶";
            if (GUILayout.Button($"{indicator} {title}", _sectionTitleStyle, GUILayout.ExpandWidth(false)))
            {
                enabled = !enabled;
            }
            
            // Restore ALL colors immediately after button to prevent leakage to child content
            GUI.color = oldColor;
            GUI.contentColor = Color.white; // Force bright white for following content
            GUI.backgroundColor = oldBgColor;
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            // Ensure content colors are fully reset for child controls
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            
            return enabled;
        }

        /// <summary>
        /// Renders a collapsible section header for Summary tab.
        /// Shows arrow indicator, clickable title, and optional status indicator on right.
        /// Returns whether the section is expanded.
        /// </summary>
        private bool CollapsibleSection(string title, ref bool expanded, bool sectionEnabled, string statusText = null)
        {
            GUILayout.BeginHorizontal();
            
            Color oldColor = GUI.color;
            Color oldContentColor = GUI.contentColor;
            
            // Header uses blue (_infoColor) when enabled, muted when disabled
            GUI.contentColor = sectionEnabled ? _infoColor : _mutedColor;
            
            // Arrow indicator + title
            string arrow = expanded ? "▼" : "▶";
            if (GUILayout.Button($"{arrow} {title}", _sectionTitleStyle, GUILayout.ExpandWidth(false)))
            {
                expanded = !expanded;
            }
            
            // Status indicator on right side
            GUILayout.FlexibleSpace();
            if (statusText != null)
            {
                GUI.color = sectionEnabled ? _enabledColor : _mutedColor;
                GUILayout.Label(statusText, _labelStyle, GUILayout.Width(120));
            }
            else
            {
                GUI.color = sectionEnabled ? _enabledColor : _mutedColor;
                GUILayout.Label(sectionEnabled ? "[Enabled ●]" : "[Disabled ○]", _labelStyle);
            }
            
            GUI.color = oldColor;
            GUI.contentColor = oldContentColor;
            GUILayout.EndHorizontal();
            
            return expanded;
        }

        // Info row with custom color
        private void ColorCodedInfoRow(string label, string value, Color valueColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  {label}:", _labelStyle, GUILayout.Width(140));
            Color oldColor = GUI.color;
            GUI.color = valueColor;
            GUILayout.Label(value, _valueStyle);
            GUI.color = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Renders a section header with category color coding
        /// </summary>
        private void ColoredSectionHeader(string title, Color categoryColor)
        {
            Color oldColor = GUI.contentColor;
            GUI.contentColor = categoryColor;
            GUILayout.Label(title, _sectionTitleStyle);
            GUI.contentColor = oldColor;
        }

        // Weapon mode row with camera override indicator
        private void WeaponModeRow(string name, bool enabled, CameraOverride camOverride)
        {
            GUILayout.BeginHorizontal();
            Color oldColor = GUI.color;
            GUI.color = enabled ? _enabledColor : _mutedColor;
            GUILayout.Label(enabled ? $"  ● {name}" : $"  ○ {name}", _labelStyle, GUILayout.Width(100));
            GUI.color = oldColor;
            
            if (enabled && camOverride != CameraOverride.Auto)
            {
                GUI.color = new Color(1f, 0.8f, 0.4f); // Orange for override
                string overrideText = camOverride == CameraOverride.FirstPersonOnly ? $"[{L("ck_fp_only", "FP Only")}]" : $"[{L("ck_proj_only", "Proj Only")}]";
                GUILayout.Label(overrideText, _labelStyle);
                GUI.color = oldColor;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // Trigger summary row with override indicator
        private void TriggerSummaryRow(string name, CKTriggerSettings trigger)
        {
            GUILayout.BeginHorizontal();
            Color oldColor = GUI.color;
            GUI.color = _enabledColor;
            GUILayout.Label($"  ● {name}", _labelStyle, GUILayout.Width(100));
            
            if (trigger.Override)
            {
                GUI.color = _warningColor; // Orange for override
                GUILayout.Label($"[{L("ck_ovr", "OVR")}] {trigger.Duration:0.0}s @ {trigger.TimeScale:0.00}x", _labelStyle);
            }
            else
            {
                GUI.color = _mutedColor;
                GUILayout.Label($"({L("ck_defaults", "defaults")})", _labelStyle);
            }
            GUI.color = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // Effect summary row with value
        private void EffectSummaryRow(string name, bool enabled, string value)
        {
            GUILayout.BeginHorizontal();
            Color oldColor = GUI.color;
            GUI.color = enabled ? _enabledColor : _mutedColor;
            GUILayout.Label(enabled ? $"  ● {name}" : $"  ○ {name}", _labelStyle, GUILayout.Width(130));
            if (enabled)
            {
                GUI.color = _cameraColor; // Blue for value
                GUILayout.Label(value, _labelStyle);
            }
            GUI.color = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void SummaryValueRow(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  {label}:", _labelStyle, GUILayout.Width(120));
            Color oldColor = GUI.color;
            GUI.color = _cameraColor; // Blue
            GUILayout.Label(value, _valueStyle);
            GUI.color = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void SummaryStatusCompact(string label, bool enabled)
        {
            Color oldColor = GUI.color;
            GUI.color = enabled ? _enabledColor : _mutedColor;
            GUILayout.Label(enabled ? $"● {label}" : $"○ {label}", _labelStyle, GUILayout.Width(100));
            GUI.color = oldColor;
        }

        private void StatusRow(string label, bool enabled)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(200));
            Color oldColor = GUI.color;
            GUI.color = enabled ? _enabledColor : _disabledColor;
            GUILayout.Label(enabled ? $"● {L("ck_enabled", "ENABLED")}" : $"○ {L("ck_disabled", "DISABLED")}", _valueStyle);
            GUI.color = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void TriggerStatusRow(string name, CKTriggerSettings trigger)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, _labelStyle, GUILayout.Width(120));
            
            if (trigger.Override)
            {
                GUILayout.Label($"{trigger.Duration:0.0}s @ {trigger.TimeScale:0.00}x", _valueStyle, GUILayout.Width(100));
                Color oldColor = GUI.color;
                GUI.color = _warningColor;
                GUILayout.Label($"[{L("ck_override", "OVERRIDE")}]", _labelStyle);
                GUI.color = oldColor;
            }
            else
            {
                GUILayout.Label($"{_settings.TriggerDefaults.Duration:0.0}s @ {_settings.TriggerDefaults.TimeScale:0.00}x", _valueStyle, GUILayout.Width(100));
                GUILayout.Label($"({L("ck_defaults", "defaults")})", _labelStyle);
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void TriggerPriorityRow(int rank, string name, CKTriggerSettings trigger)
        {
            GUILayout.BeginHorizontal();
            
            // Rank indicator
            Color oldColor = GUI.color;
            GUI.color = rank == 1 ? _modeColor : _mutedColor;
            GUILayout.Label($"#{rank}", _labelStyle, GUILayout.Width(30));
            GUI.color = oldColor;
            
            GUILayout.Label(name, _labelStyle, GUILayout.Width(100));
            
            if (trigger.Override)
            {
                GUILayout.Label($"{trigger.Duration:0.0}s @ {trigger.TimeScale:0.00}x", _valueStyle, GUILayout.Width(100));
                oldColor = GUI.color;
                GUI.color = new Color(1f, 0.8f, 0.4f);
                GUILayout.Label($"[{L("ck_override", "OVERRIDE")}]", _labelStyle);
                GUI.color = oldColor;
            }
            else
            {
                GUILayout.Label($"{_settings.TriggerDefaults.Duration:0.0}s @ {_settings.TriggerDefaults.TimeScale:0.00}x", _valueStyle, GUILayout.Width(100));
                GUILayout.Label($"({L("ck_defaults", "defaults")})", _labelStyle);
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw a preset row with separate BasicKill and Trigger toggles (like weapon mode rows)
        /// Greys out columns when the respective section doesn't have Projectile Camera enabled
        /// </summary>
        private void DrawPresetWithDualToggle(string presetName, int index, ref bool basicKillEnabled, ref bool triggerEnabled)
        {
            GUILayout.BeginHorizontal();
            
            // Check if proj camera is enabled AND the parent feature is enabled for each section
            bool bkHasProj = _settings.BasicKill.Enabled && _settings.BasicKill.ProjectileCamera;
            bool tdHasProj = _settings.TriggerDefaults.EnableTriggers && _settings.TriggerDefaults.ProjectileCamera;
            
            // Preset name - grey if neither section uses proj camera
            Color oldColor = GUI.color;
            if (!bkHasProj && !tdHasProj)
                GUI.color = _mutedColor;
            GUILayout.Label(presetName, _labelStyle, GUILayout.Width(120));
            GUI.color = oldColor;
            
            // BasicKill toggle - grey out if BK doesn't use projectile camera
            Color oldBg = GUI.backgroundColor;
            if (bkHasProj)
            {
                GUI.backgroundColor = basicKillEnabled ? _toggleOnColor : _toggleOffColor;
                if (GUILayout.Button(basicKillEnabled ? "●" : "○", GUILayout.Width(30)))
                {
                    // Ensure at least one BasicKill preset remains enabled
                    int enabledCount = 0;
                    for (int i = 0; i < _settings.ProjectileCamera.EnabledPresetsBasicKill.Length; i++)
                        if (_settings.ProjectileCamera.EnabledPresetsBasicKill[i]) enabledCount++;
                    
                    if (basicKillEnabled && enabledCount > 1)
                        basicKillEnabled = false;
                    else if (!basicKillEnabled)
                        basicKillEnabled = true;
                }
            }
            else
            {
                // Greyed out and readonly when BK doesn't use projectile camera
                GUI.backgroundColor = _bgColor;
                GUI.color = _mutedColor;
                GUILayout.Label("─", _labelStyle, GUILayout.Width(30));
                GUI.color = oldColor;
            }
            GUI.backgroundColor = oldBg;
            
            GUILayout.Space(10);
            
            // Trigger toggle - grey out if Triggers don't use projectile camera
            if (tdHasProj)
            {
                GUI.backgroundColor = triggerEnabled ? _toggleOnColor : _toggleOffColor;
                if (GUILayout.Button(triggerEnabled ? "●" : "○", GUILayout.Width(30)))
                {
                    // Ensure at least one Trigger preset remains enabled
                    int enabledCount = 0;
                    for (int i = 0; i < _settings.ProjectileCamera.EnabledPresetsTriggers.Length; i++)
                        if (_settings.ProjectileCamera.EnabledPresetsTriggers[i]) enabledCount++;
                    
                    if (triggerEnabled && enabledCount > 1)
                        triggerEnabled = false;
                    else if (!triggerEnabled)
                        triggerEnabled = true;
                }
            }
            else
            {
                // Greyed out and readonly when Triggers don't use projectile camera
                GUI.backgroundColor = _bgColor;
                GUI.color = _mutedColor;
                GUILayout.Label("─", _labelStyle, GUILayout.Width(30));
                GUI.color = oldColor;
            }
            GUI.backgroundColor = oldBg;
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the Near Player fallback preset row with dual toggles.
        /// This is a special preset that positions camera near the player instead of at the victim.
        /// </summary>
        private void DrawNearPlayerPreset()
        {
            GUILayout.BeginHorizontal();
            
            // Check if proj camera is enabled for each section
            bool bkHasProj = _settings.BasicKill.Enabled && _settings.BasicKill.ProjectileCamera;
            bool tdHasProj = _settings.TriggerDefaults.EnableTriggers && _settings.TriggerDefaults.ProjectileCamera;
            
            // Preset name with distinct color
            Color oldColor = GUI.color;
            if (!bkHasProj && !tdHasProj)
                GUI.color = _mutedColor;
            else
                GUI.color = new Color(0.8f, 0.6f, 0.2f); // Orange/amber for special preset
            GUILayout.Label(L("ck_ui_over_shoulder", "✦ Over Shoulder"), _labelStyle, GUILayout.Width(120));
            GUI.color = oldColor;
            
            // BasicKill toggle
            Color oldBg = GUI.backgroundColor;
            if (bkHasProj)
            {
                GUI.backgroundColor = _settings.ProjectileCamera.EnableNearPlayerBasicKill ? _toggleOnColor : _toggleOffColor;
                if (GUILayout.Button(_settings.ProjectileCamera.EnableNearPlayerBasicKill ? "●" : "○", GUILayout.Width(30)))
                {
                    _settings.ProjectileCamera.EnableNearPlayerBasicKill = !_settings.ProjectileCamera.EnableNearPlayerBasicKill;
                }
            }
            else
            {
                GUI.backgroundColor = _bgColor;
                GUI.color = _mutedColor;
                GUILayout.Label("─", _labelStyle, GUILayout.Width(30));
                GUI.color = oldColor;
            }
            GUI.backgroundColor = oldBg;
            
            GUILayout.Space(10);
            
            // Trigger toggle
            if (tdHasProj)
            {
                GUI.backgroundColor = _settings.ProjectileCamera.EnableNearPlayerTriggers ? _toggleOnColor : _toggleOffColor;
                if (GUILayout.Button(_settings.ProjectileCamera.EnableNearPlayerTriggers ? "●" : "○", GUILayout.Width(30)))
                {
                    _settings.ProjectileCamera.EnableNearPlayerTriggers = !_settings.ProjectileCamera.EnableNearPlayerTriggers;
                }
            }
            else
            {
                GUI.backgroundColor = _bgColor;
                GUI.color = _mutedColor;
                GUILayout.Label("─", _labelStyle, GUILayout.Width(30));
                GUI.color = oldColor;
            }
            GUI.backgroundColor = oldBg;
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the Behind Enemy preset row with toggle.
        /// This positions camera behind the enemy looking toward the player (like X-Ray angle).
        /// </summary>
        private void DrawBehindEnemyPreset()
        {
            GUILayout.BeginHorizontal();
            
            // Preset name with distinct color
            Color oldColor = GUI.color;
            GUI.color = new Color(0.8f, 0.6f, 0.2f); // Orange/amber for special preset
            GUILayout.Label(L("ck_ui_behind_enemy", "✦ Behind Enemy"), _labelStyle, GUILayout.Width(120));
            GUI.color = oldColor;
            
            // Single toggle for Behind Enemy
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = _settings.ProjectileCamera.EnableBehindEnemy ? _toggleOnColor : _toggleOffColor;
            if (GUILayout.Button(_settings.ProjectileCamera.EnableBehindEnemy ? "●" : "○", GUILayout.Width(30)))
            {
                _settings.ProjectileCamera.EnableBehindEnemy = !_settings.ProjectileCamera.EnableBehindEnemy;
            }
            GUI.backgroundColor = oldBg;
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw a side offset toggle button (Standard/Wide/Tight)
        /// </summary>
        private void DrawSideOffsetToggle(string label, ref bool isEnabled, bool canDisable)
        {
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = isEnabled ? _toggleOnColor : _toggleOffColor;
            
            if (GUILayout.Button(label, GUILayout.Width(80)))
            {
                if (isEnabled)
                {
                    // Only allow disable if at least one other option is enabled
                    if (canDisable)
                        isEnabled = false;
                }
                else
                {
                    isEnabled = true;
                }
            }
            GUI.backgroundColor = oldBg;
        }

        // ═══════════════════════════════════════════════════════════════
        //  TAB 1: MAIN
        // ═══════════════════════════════════════════════════════════════
        private void DrawMainTab()
        {
            // Master Control
            SectionTitle(CKLocalization.L("ck_section_master_control", "MASTER CONTROL"));
            _settings.EnableCinematics = ToggleRow(CKLocalization.L("ck_enable_cinematics", "Enable Cinematics"), _settings.EnableCinematics);

            if (!_settings.EnableCinematics)
            {
                GUILayout.Space(20);
                GUILayout.Label(CKLocalization.L("ck_all_cinematics_disabled", "All cinematic effects are disabled."), _labelStyle);
                return;
            }

            GUILayout.Space(20);

            // ═══════════════════════════════════════════════════════════════
            // SMART OPTIONS
            // ═══════════════════════════════════════════════════════════════
            if (ToggleableHeader(L("ck_smart_options", "SMART OPTIONS"), ref _settings.SmartIndoorOutdoorDetection))
            {
                GUILayout.Label(CKLocalization.L("ck_indoor_outdoor_desc", "  Indoors → First Person, Outdoors → Projectile"), _labelStyle);
                _settings.IndoorDetectionHeight = SliderRow(CKLocalization.L("ck_detection_height", "Detection Height"), _settings.IndoorDetectionHeight, 3f, 20f, "{0:0}m");
            }

            GUILayout.Space(20);

            // ═══════════════════════════════════════════════════════════════
            // LAST STAND / SECOND WIND
            // ═══════════════════════════════════════════════════════════════
            if (ToggleableHeader(L("ck_trigger_last_stand", "LAST STAND"), ref _settings.EnableLastStand))
            {
                GUILayout.Space(10);
                
                _settings.LastStandDuration = SliderRow("ls_dur", L("ck_ui_kill_window", "Kill Window"), _settings.LastStandDuration, 1f, 10f, "{0:0.0}s");
                _settings.LastStandTimeScale = SliderRow("ls_scale", L("ck_time_scale_label", "Time Scale"), _settings.LastStandTimeScale, 0.05f, 0.3f, "{0:0.00}");
                _settings.LastStandReviveHealth = SliderRow("ls_health", L("ck_ui_revive_health", "Revive Health"), _settings.LastStandReviveHealth, 10f, 50f, "{0:0}%");
                _settings.LastStandCooldown = SliderRow("ls_cooldown", L("ck_cooldown", "Cooldown"), _settings.LastStandCooldown, 30f, 120f, "{0:0}s");
                _settings.LastStandInfiniteAmmo = ToggleRow(L("ck_ui_infinite_ammo", "Infinite Ammo"), _settings.LastStandInfiniteAmmo);
                
                GUILayout.Space(5);
                GUILayout.Label(L("ck_ui_when_near_death_time_slows_for_a_8ea3f5", "When near death, time slows for a kill chance. Successful kill restores health."), _labelStyle);
            }

            GUILayout.Space(20);

            // ═══════════════════════════════════════════════════════════════
            // BASIC KILL SETTINGS
            // ═══════════════════════════════════════════════════════════════
            if (ToggleableHeader(L("ck_section_basic_kill", "BASIC KILL"), ref _settings.BasicKill.Enabled))
            {
                GUILayout.Space(10);
                
                // Two-column layout: General | Camera
                GUILayout.BeginHorizontal();
                
                // LEFT COLUMN - General Settings
                GUILayout.BeginVertical(GUILayout.Width(500));
                ColoredSectionHeader(L("ck_ui_general", "── General ──"), _cameraColor);
                GUILayout.Space(5);
                
                
                _settings.BasicKill.Chance = RandomizableSliderRow("bk_chance", CKLocalization.L("ck_chance", "Chance *"), _settings.BasicKill.Chance, 
                    ref _settings.BasicKill.ChanceMin, ref _settings.BasicKill.ChanceMax, 
                    0f, 100f, "{0:0}%", ref _settings.BasicKill.RandomizeChance);
                
                // Duration label changes based on ragdoll setting
                bool anyRagdollEnabled = _settings.EnableDynamicRagdollDuration_BK_FP || _settings.EnableDynamicRagdollDuration_BK_Proj;
                string durationLabel = anyRagdollEnabled 
                    ? CKLocalization.L("ck_post_land_delay", "Post Land Delay *") 
                    : CKLocalization.L("ck_duration", "Duration *");
                    
                _settings.BasicKill.Duration = RandomizableSliderRow("bk_duration", durationLabel, _settings.BasicKill.Duration, 
                    ref _settings.BasicKill.DurationMin, ref _settings.BasicKill.DurationMax, 
                    0.3f, 5f, "{0:0.0}s", ref _settings.BasicKill.RandomizeDuration);
                    
                _settings.BasicKill.TimeScale = RandomizableSliderRow("bk_timescale", CKLocalization.L("ck_time_scale", "Time Scale *"), _settings.BasicKill.TimeScale, 
                    ref _settings.BasicKill.TimeScaleMin, ref _settings.BasicKill.TimeScaleMax, 
                    0.05f, 1f, "{0:0.00}x", ref _settings.BasicKill.RandomizeTimeScale);
                    
                _settings.BasicKill.Cooldown = SliderRow("bk_cooldown", CKLocalization.L("ck_cooldown", "Cooldown"), _settings.BasicKill.Cooldown, 0f, 30f, "{0:0.0}s");
                
                GUILayout.EndVertical();
                
                GUILayout.Space(20);
                
                // RIGHT COLUMN - Camera Settings
                GUILayout.BeginVertical(GUILayout.Width(500));
                ColoredSectionHeader(L("ck_ui_camera", "── Camera ──"), _cameraColor);
                GUILayout.Space(5);
                
                _settings.BasicKill.FirstPersonCamera = ToggleRow(CKLocalization.L("ck_first_person_camera", "First Person Camera"), _settings.BasicKill.FirstPersonCamera);
                _settings.BasicKill.ProjectileCamera = ToggleRow(CKLocalization.L("ck_projectile_camera", "Projectile Camera"), _settings.BasicKill.ProjectileCamera);



                if (_settings.BasicKill.FirstPersonCamera && _settings.BasicKill.ProjectileCamera)
                {
                    GUILayout.Space(5);
                    DrawLinkedCameraChance("bk_fp_chance", ref _settings.BasicKill.FirstPersonChance);
                }
                
                // Ragdoll Floor Hit - Compact grouped section
                if (_settings.BasicKill.FirstPersonCamera || _settings.BasicKill.ProjectileCamera)
                {
                    GUILayout.Space(10);
                    Color oldColor = GUI.color;
                    GUI.color = _timingColor;
                    GUILayout.Label(L("ck_ui_end_on_ragdoll_floor_hit", "─ End on Ragdoll Floor Hit ─"), _labelStyle);
                    GUI.color = oldColor;
                    GUILayout.Space(3);
                    
                    // FP ragdoll option (only if FP camera enabled)
                    if (_settings.BasicKill.FirstPersonCamera)
                    {
                        GUILayout.BeginHorizontal();
                        _settings.EnableDynamicRagdollDuration_BK_FP = ToggleCompact(L("ck_first_person", "First Person"), _settings.EnableDynamicRagdollDuration_BK_FP);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    
                    // Proj ragdoll option (only if Proj camera enabled)
                    if (_settings.BasicKill.ProjectileCamera)
                    {
                        GUILayout.BeginHorizontal();
                        _settings.EnableDynamicRagdollDuration_BK_Proj = ToggleCompact(L("ck_projectile", "Projectile"), _settings.EnableDynamicRagdollDuration_BK_Proj);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    
                    // Note about Duration slider repurposing
                    if (anyRagdollEnabled)
                    {
                        GUILayout.Space(3);
                        GUI.color = _mutedColor;
                        GUILayout.Label(L("ck_ui_duration_slider_above_acts_as_po_edc6f3", "Duration slider above acts as Post Land Delay"), _labelStyle);
                        GUI.color = oldColor;
                    }
                }
                
                GUILayout.EndVertical();
                
                GUILayout.Space(20);
                
                // HINT BOX - Reference information (styled with colored header)
                GUILayout.BeginVertical(GUILayout.Width(220));
                GUILayout.Space(5);
                ColoredSectionHeader(L("ck_ui_quick_reference", "─ Quick Reference ─"), _cameraColor);
                GUI.color = _mutedColor;
                GUILayout.Label(L("ck_ui_randomized_setting", "• * = randomized setting"), _labelStyle);
                GUILayout.Label(L("ck_ui_click_label_to_toggle", "• Click * label to toggle"), _labelStyle);
                GUILayout.Label(L("ck_ui_basic_kill_regular_kills", "• Basic Kill = regular kills"), _labelStyle);
                GUILayout.Label(L("ck_ui_triggers_headshot_crit", "• Triggers = headshot, crit..."), _labelStyle);
                GUILayout.Label(L("ck_ui_fp_first_person_view", "• FP = First Person view"), _labelStyle);
                GUILayout.Label(L("ck_ui_proj_follow_projectile", "• Proj = Follow projectile"), _labelStyle);
                GUILayout.Space(5);
                GUILayout.EndVertical();
                
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(25);

            // ═══════════════════════════════════════════════════════════════
            // SPECIAL TRIGGERS DEFAULTS
            // ═══════════════════════════════════════════════════════════════
            if (ToggleableHeader(L("ck_section_triggers", "SPECIAL TRIGGERS"), ref _settings.TriggerDefaults.EnableTriggers))
            {
                GUILayout.Space(10);
                
                // Reset ALL color states to ensure bright text for content
                GUI.color = Color.white;
                GUI.contentColor = Color.white;
                
                // Two-column layout: General | Camera
                GUILayout.BeginHorizontal();
                
                // LEFT COLUMN - General Settings
                GUILayout.BeginVertical(GUILayout.Width(500));
                ColoredSectionHeader(L("ck_ui_general", "── General ──"), _cameraColor);
                GUILayout.Space(5);
                
                // Track old values to detect ANY TriggerDefaults changes
                float oldTdChance = _settings.MasterTriggerChance;
                float oldTdDuration = _settings.TriggerDefaults.Duration;
                float oldTdTimeScale = _settings.TriggerDefaults.TimeScale;
                float oldTdCooldown = _settings.TriggerDefaults.Cooldown;
                bool oldTdFpCamera = _settings.TriggerDefaults.FirstPersonCamera;
                bool oldTdProjCamera = _settings.TriggerDefaults.ProjectileCamera;
                
                // Chance (formerly Master Trigger Chance)
                _settings.MasterTriggerChance = SliderRow("td_chance", CKLocalization.L("ck_chance", "Chance"), _settings.MasterTriggerChance, 0f, 100f, "{0:0}%");
                
                // Duration label changes based on ragdoll setting
                bool anyTdRagdollEnabled = _settings.EnableDynamicRagdollDuration_TD_FP || _settings.EnableDynamicRagdollDuration_TD_Proj;
                string tdDurationLabel = anyTdRagdollEnabled 
                    ? CKLocalization.L("ck_post_land_delay", "Post Land Delay *") 
                    : CKLocalization.L("ck_duration", "Duration *");
                    
                _settings.TriggerDefaults.Duration = RandomizableSliderRow("td_duration", tdDurationLabel, _settings.TriggerDefaults.Duration, 
                    ref _settings.TriggerDefaults.DurationMin, ref _settings.TriggerDefaults.DurationMax, 
                    0.3f, 5f, "{0:0.0}s", ref _settings.TriggerDefaults.RandomizeDuration);
                    
                _settings.TriggerDefaults.TimeScale = RandomizableSliderRow("td_timescale", CKLocalization.L("ck_time_scale", "Time Scale *"), _settings.TriggerDefaults.TimeScale, 
                    ref _settings.TriggerDefaults.TimeScaleMin, ref _settings.TriggerDefaults.TimeScaleMax, 
                    0.05f, 1f, "{0:0.00}x", ref _settings.TriggerDefaults.RandomizeTimeScale);
                
                _settings.TriggerDefaults.Cooldown = SliderRow("td_cooldown", CKLocalization.L("ck_cooldown", "Cooldown"), _settings.TriggerDefaults.Cooldown, 0f, 30f, "{0:0.0}s");
                
                GUILayout.EndVertical();
                
                GUILayout.Space(20);
                
                // RIGHT COLUMN - Camera Settings
                GUILayout.BeginVertical(GUILayout.Width(500));
                ColoredSectionHeader(L("ck_ui_camera", "── Camera ──"), _cameraColor);
                GUILayout.Space(5);
                
                _settings.TriggerDefaults.FirstPersonCamera = ToggleRow(CKLocalization.L("ck_first_person_camera", "First Person Camera"), _settings.TriggerDefaults.FirstPersonCamera);
                _settings.TriggerDefaults.ProjectileCamera = ToggleRow(CKLocalization.L("ck_projectile_camera", "Projectile Camera"), _settings.TriggerDefaults.ProjectileCamera);

                // Invalidate cache when ANY TriggerDefaults setting changes (settings are now live)
                bool tdSettingsChanged = 
                    oldTdChance != _settings.MasterTriggerChance ||
                    oldTdDuration != _settings.TriggerDefaults.Duration ||
                    oldTdTimeScale != _settings.TriggerDefaults.TimeScale ||
                    oldTdCooldown != _settings.TriggerDefaults.Cooldown ||
                    oldTdFpCamera != _settings.TriggerDefaults.FirstPersonCamera || 
                    oldTdProjCamera != _settings.TriggerDefaults.ProjectileCamera;
                    
                if (tdSettingsChanged)
                {
                    _settings.InvalidateMenuV2Cache();
                }

                if (_settings.TriggerDefaults.FirstPersonCamera && _settings.TriggerDefaults.ProjectileCamera)
                {
                    GUILayout.Space(5);
                    DrawLinkedCameraChance("td_fp_chance", ref _settings.TriggerDefaults.FirstPersonChance);
                }

                // Ragdoll Floor Hit - Compact grouped section
                if (_settings.TriggerDefaults.FirstPersonCamera || _settings.TriggerDefaults.ProjectileCamera)
                {
                    GUILayout.Space(10);
                    Color oldColor = GUI.color;
                    GUI.color = _timingColor;
                    GUILayout.Label(L("ck_ui_end_on_ragdoll_floor_hit", "─ End on Ragdoll Floor Hit ─"), _labelStyle);
                    GUI.color = oldColor;
                    GUILayout.Space(3);
                    
                    // FP ragdoll option (only if FP camera enabled)
                    if (_settings.TriggerDefaults.FirstPersonCamera)
                    {
                        GUILayout.BeginHorizontal();
                        _settings.EnableDynamicRagdollDuration_TD_FP = ToggleCompact(L("ck_first_person", "First Person"), _settings.EnableDynamicRagdollDuration_TD_FP);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    
                    // Proj ragdoll option (only if Proj camera enabled)
                    if (_settings.TriggerDefaults.ProjectileCamera)
                    {
                        GUILayout.BeginHorizontal();
                        _settings.EnableDynamicRagdollDuration_TD_Proj = ToggleCompact(L("ck_projectile", "Projectile"), _settings.EnableDynamicRagdollDuration_TD_Proj);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    
                    // Note about Duration slider repurposing
                    if (anyTdRagdollEnabled)
                    {
                        GUILayout.Space(3);
                        GUI.color = _mutedColor;
                        GUILayout.Label(L("ck_ui_duration_slider_above_acts_as_po_edc6f3", "Duration slider above acts as Post Land Delay"), _labelStyle);
                        GUI.color = oldColor;
                    }
                }
                
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  TAB 4: HUD
        // ═══════════════════════════════════════════════════════════════
        private void DrawHUDTab()
        {
            // Show placeholder if cinematics disabled
            if (!_settings.EnableCinematics)
            {
                SectionTitle(CKLocalization.L("ck_section_cinematics_disabled", "CINEMATICS DISABLED"));
                GUILayout.Space(20);
                GUILayout.Label(CKLocalization.L("ck_enable_cinematics_hud_hint", "Enable Cinematics in Main tab to access HUD settings."), _labelStyle);
                return;
            }
            
            // ═══════════════════════════════════════════════════════════════
            // HUD NOTIFICATIONS
            // ═══════════════════════════════════════════════════════════════
            // Two-column layout: HUD Notifications | Game HUD Hiding
            GUILayout.BeginHorizontal();
            
            // LEFT COLUMN - HUD NOTIFICATIONS
            GUILayout.BeginVertical(GUILayout.Width(480));
            
            if (ToggleableHeader(L("ck_ui_hud_notifications", "HUD NOTIFICATIONS"), ref _settings.HUD.Enabled))
            {
                GUILayout.Space(10);
                ColoredSectionHeader(L("ck_ui_settings", "── Settings ──"), _cameraColor);
                GUILayout.Space(5);
                _settings.HUD.Opacity = SliderRow(CKLocalization.L("ck_opacity", "Opacity"), _settings.HUD.Opacity, 0.1f, 1f, "{0:0.0}");
                _settings.HUD.MessageDuration = SliderRow(CKLocalization.L("ck_message_duration", "Message Duration"), _settings.HUD.MessageDuration, 1f, 10f, "{0:0.0}s");
            }

            GUILayout.EndVertical();
            
            GUILayout.Space(30);
            
            // RIGHT COLUMN - GAME HUD HIDING
            GUILayout.BeginVertical(GUILayout.Width(480));
            
            if (ToggleableHeader(L("ck_ui_game_hud_hiding", "GAME HUD HIDING"), ref _settings.HUDElements.HideAllHUDDuringCinematic))
            {
                GUILayout.Space(10);
                ColoredSectionHeader(L("ck_ui_info", "── Info ──"), _cameraColor);
                GUILayout.Space(5);
                Color infoColor = GUI.color;
                GUI.color = new Color(0.6f, 0.75f, 0.9f);
                GUILayout.Label(CKLocalization.L("ck_scope_info", "Scope: Shown in FP, hidden in Projectile"), _labelStyle);
                GUILayout.Label(CKLocalization.L("ck_hidden_elements", "Hidden: Crosshair, Compass, Health, etc."), _labelStyle);
                GUI.color = infoColor;
            }
            
            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════════════════
        //  TAB 3: CAMERA
        // ═══════════════════════════════════════════════════════════════
        private string _newPresetName = "";
        
        private void DrawCameraTab()
        {
            // Show placeholder if cinematics disabled
            if (!_settings.EnableCinematics)
            {
                SectionTitle(CKLocalization.L("ck_section_cinematics_disabled", "CINEMATICS DISABLED"));
                GUILayout.Space(20);
                GUILayout.Label(CKLocalization.L("ck_enable_cinematics_camera_hint", "Enable Cinematics in the Main tab to configure camera."), _labelStyle);
                return;
            }

            // ═══════════════════════════════════════════════════════════════
            // CAMERA SUB-TAB TOGGLE: First Person | Projectile
            // ═══════════════════════════════════════════════════════════════
            GUILayout.BeginHorizontal();
            
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = _cameraSubTab == 0 
                ? _toggleOnColor : _toggleOffColor;
            if (GUILayout.Button(CKLocalization.L("ck_first_person", "First Person"), 
                GUILayout.Width(180), GUILayout.Height(35)))
            {
                _cameraSubTab = 0;
            }
            GUI.backgroundColor = oldBg;
            
            GUILayout.Space(15);
            
            GUI.backgroundColor = _cameraSubTab == 1 
                ? _toggleOnColor : _toggleOffColor;
            if (GUILayout.Button(CKLocalization.L("ck_projectile", "Projectile"), 
                GUILayout.Width(180), GUILayout.Height(35)))
            {
                _cameraSubTab = 1;
            }
            GUI.backgroundColor = oldBg;
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            
            // Draw selected sub-tab
            if (_cameraSubTab == 0)
            {
                DrawFirstPersonCameraSubTab();
            }
            else
            {
                DrawProjectileCameraSubTab();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  FIRST PERSON CAMERA SUB-TAB
        // ═══════════════════════════════════════════════════════════════
        private void DrawFirstPersonCameraSubTab()
        {
            // Two-column layout: Basic Kill FP Settings | Special Triggers FP Settings
            GUILayout.BeginHorizontal();
            
            // ─────────────────────────────────────────────────────────────
            // LEFT COLUMN - Basic Kill FP Camera Settings
            // ─────────────────────────────────────────────────────────────
            GUILayout.BeginVertical(GUILayout.Width(500));
            
            SectionTitle(CKLocalization.L("ck_basic_kill_fp", "BASIC KILL"));
            GUILayout.Label(CKLocalization.L("ck_fp_settings_for_basic", "First person camera settings for normal kills"), _labelStyle);
            GUILayout.Space(10);
            
            if (!_settings.BasicKill.Enabled)
            {
                DisabledLabel(CKLocalization.L("ck_bk_disabled_hint", "Basic Kill is disabled in Main tab"));
            }
            else if (!_settings.BasicKill.FirstPersonCamera)
            {
                DisabledLabel(CKLocalization.L("ck_fp_disabled_hint", "First Person Camera is disabled in Main tab"));
            }
            else
            {
                // FOV Zoom settings
                ColoredSectionHeader(L("ck_ui_fov_zoom", "── FOV Zoom ──"), _cameraColor);
                GUILayout.Space(5);
                
                _settings.BasicKill.FOVMode = FOVModeToggle(CKLocalization.L("ck_fov_mode", "Mode"), _settings.BasicKill.FOVMode);
                if (_settings.BasicKill.FOVMode != FOVMode.Off)
                {
                    string zoomLabel = _settings.BasicKill.FOVMode == FOVMode.ZoomIn 
                        ? CKLocalization.L("ck_zoom_in", "Zoom In %") 
                        : CKLocalization.L("ck_zoom_out", "Zoom Out %");
                    _settings.BasicKill.FOVPercent = SliderRow("fp_bk_zoom", zoomLabel, _settings.BasicKill.FOVPercent, 5f, 50f, "{0:0}%");
                    
                    // Advanced timing - clickable label to expand
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    Color advColor = GUI.color;
                    GUI.color = _fpAdvancedFOVExpanded ? ExpandedIndicatorColor : CollapsedIndicatorColor;
                    string advLabel = _settings.EnableAdvancedFOVTiming ? L("ck_ui_advanced_timing_active", "Advanced Timing ★") : L("ck_ui_advanced_timing", "Advanced Timing *");
                    if (GUILayout.Button(advLabel, _labelStyle, GUILayout.Width(140)))
                    {
                        _fpAdvancedFOVExpanded = !_fpAdvancedFOVExpanded;
                    }
                    GUI.color = advColor;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    
                    if (_fpAdvancedFOVExpanded)
                    {
                        GUILayout.Space(3);
                        // Toggle for showing return timing slider
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        _settings.EnableAdvancedFOVTiming = GUILayout.Toggle(_settings.EnableAdvancedFOVTiming, "  Show Return Timing", _labelStyle);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.Space(3);
                        DrawFovTimingRows("fp_bk", _settings.BasicKill.FOVMode, ref _settings.BasicKill.FOVZoomInDuration, ref _settings.BasicKill.FOVHoldDuration, ref _settings.BasicKill.FOVZoomOutDuration, _settings.BasicKill.Duration);
                    }
                }
            }
            
            GUILayout.EndVertical();
            
            GUILayout.Space(30);
            
            // ─────────────────────────────────────────────────────────────
            // RIGHT COLUMN - Special Triggers FP Camera Settings
            // ─────────────────────────────────────────────────────────────
            GUILayout.BeginVertical(GUILayout.Width(500));
            
            SectionTitle(CKLocalization.L("ck_special_triggers_fp", "SPECIAL TRIGGERS"));
            GUILayout.Label(CKLocalization.L("ck_fp_settings_for_triggers", "First person camera settings for special trigger kills"), _labelStyle);
            GUILayout.Space(10);
            
            if (!_settings.TriggerDefaults.EnableTriggers)
            {
                DisabledLabel(CKLocalization.L("ck_triggers_disabled_hint", "Special Triggers are disabled in Main tab"));
            }
            else if (!_settings.TriggerDefaults.FirstPersonCamera)
            {
                DisabledLabel(CKLocalization.L("ck_fp_disabled_triggers_hint", "First Person Camera is disabled for triggers in Main tab"));
            }
            else
            {
                // FOV Zoom settings for triggers
                ColoredSectionHeader(L("ck_ui_fov_zoom", "── FOV Zoom ──"), _cameraColor);
                GUILayout.Space(5);
                
                _settings.TriggerDefaults.FOVMode = FOVModeToggle(CKLocalization.L("ck_fov_mode", "FOV Mode"), _settings.TriggerDefaults.FOVMode);
                if (_settings.TriggerDefaults.FOVMode != FOVMode.Off)
                {
                    string zoomLabel = _settings.TriggerDefaults.FOVMode == FOVMode.ZoomIn 
                        ? CKLocalization.L("ck_zoom_in", "Zoom In %") 
                        : CKLocalization.L("ck_zoom_out", "Zoom Out %");
                    _settings.TriggerDefaults.FOVPercent = SliderRow("fp_st_zoom", zoomLabel, _settings.TriggerDefaults.FOVPercent, 5f, 50f, "{0:0}%");
                    
                    // Advanced timing - clickable label to expand (separate state for triggers)
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    Color advColorST = GUI.color;
                    GUI.color = _triggerAdvancedFOVExpanded ? ExpandedIndicatorColor : CollapsedIndicatorColor;
                    if (GUILayout.Button(L("ck_ui_advanced_timing", "Advanced Timing *"), _labelStyle, GUILayout.Width(130)))
                    {
                        _triggerAdvancedFOVExpanded = !_triggerAdvancedFOVExpanded;
                    }
                    GUI.color = advColorST;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    
                    if (_triggerAdvancedFOVExpanded)
                    {
                        GUILayout.Space(3);
                        DrawFovTimingRows("fp_st", _settings.TriggerDefaults.FOVMode, ref _settings.TriggerDefaults.FOVZoomInDuration, ref _settings.TriggerDefaults.FOVHoldDuration, ref _settings.TriggerDefaults.FOVZoomOutDuration, _settings.TriggerDefaults.Duration);
                    }
                }
            }
            
            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            // ═══════════════════════════════════════════════════════════════
            // FP FREEZE FRAME SECTION
            // ═══════════════════════════════════════════════════════════════
            GUILayout.Space(25);
            var fpFreeze = _settings.FPFreezeFrame;
            
            if (ToggleableHeader(L("ck_ui_freeze_frame", "FREEZE FRAME"), ref fpFreeze.Enabled))
            {
                GUILayout.Space(10);
                
                fpFreeze.Chance = SliderRow("fp_freeze_chance", L("ck_ui_chance", "Chance"), fpFreeze.Chance, 0f, 100f, "{0:0}%");
                fpFreeze.Duration = SliderRow("fp_freeze_duration", L("ck_duration_label", "Duration"), fpFreeze.Duration, 0.1f, 5f, "{0:0.0}s");
                fpFreeze.Delay = SliderRow("fp_freeze_delay", L("ck_ui_delay", "Delay"), fpFreeze.Delay, 0f, 2f, "{0:0.00}s");
                
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("ck_ui_trigger_on", "Trigger on:"), _labelStyle, GUILayout.Width(80));
                fpFreeze.TriggerOnBasicKill = TextToggle(L("ck_apply_to_bk", "Basic Kill"), fpFreeze.TriggerOnBasicKill);
                GUILayout.Space(20);
                fpFreeze.TriggerOnSpecialTrigger = TextToggle(L("ck_ui_special_trigger", "Special Trigger"), fpFreeze.TriggerOnSpecialTrigger);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                // Slow Motion
                GUILayout.Space(15);
                ColoredSectionHeader(L("ck_ui_slow_motion", "── Slow Motion ──"), _cameraColor);
                GUILayout.Space(5);
                fpFreeze.EnableCameraMovement = ToggleRow(L("ck_ui_enable_slow_motion", "Enable Slow Motion"), fpFreeze.EnableCameraMovement);
                if (fpFreeze.EnableCameraMovement)
                {
                    fpFreeze.TimeScale = SliderRow("fp_freeze_timescale", L("ck_time_scale_label", "Time Scale"), fpFreeze.TimeScale, 0.01f, 0.1f, "{0:0.00}x");
                }
                
                // Contrast Effect
                GUILayout.Space(15);
                ColoredSectionHeader(L("ck_ui_contrast_effect", "── Contrast Effect ──"), _effectColor);
                GUILayout.Space(5);
                fpFreeze.EnableContrastEffect = ToggleRow(L("ck_ui_enable_contrast_effect", "Enable Contrast Effect"), fpFreeze.EnableContrastEffect);
                if (fpFreeze.EnableContrastEffect)
                {
                    fpFreeze.ContrastAmount = SliderRow("fp_freeze_contrast", L("ck_ui_contrast", "Contrast"), fpFreeze.ContrastAmount, 1f, 1.8f, "{0:0.0}x");
                }
                
                // Post-Freeze Action
                GUILayout.Space(15);
                ColoredSectionHeader(L("ck_ui_after_freeze", "── After Freeze ──"), _cameraColor);
                GUILayout.Space(5);
                DrawPostFreezeActionSelector(fpFreeze, isFirstPerson: true);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  PROJECTILE CAMERA SUB-TAB
        // ═══════════════════════════════════════════════════════════════
        private void DrawProjectileCameraSubTab()
        {
            // ═══════════════════════════════════════════════════════════════
            // STANDARD PRESETS / CUSTOM POSITIONING TOGGLE
            // ═══════════════════════════════════════════════════════════════
            GUILayout.BeginHorizontal();
            
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = _settings.ProjectileCamera.UseStandardPresets 
                ? _toggleOnColor : _toggleOffColor;
            if (GUILayout.Button(CKLocalization.L("ck_standard_presets", "Standard Presets"), 
                GUILayout.Width(180), GUILayout.Height(30)))
            {
                _settings.ProjectileCamera.UseStandardPresets = true;
                _settings.ProjectileCamera.EnsureAtLeastOnePreset();
            }
            GUI.backgroundColor = oldBg;
            
            GUILayout.Space(15);
            
            GUI.backgroundColor = !_settings.ProjectileCamera.UseStandardPresets 
                ? _toggleOnColor : _toggleOffColor;
            if (GUILayout.Button(CKLocalization.L("ck_custom_positioning", "Custom Positioning"), 
                GUILayout.Width(180), GUILayout.Height(30)))
            {
                _settings.ProjectileCamera.UseStandardPresets = false;
            }
            GUI.backgroundColor = oldBg;
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(15);
            
            // ═══════════════════════════════════════════════════════════════
            // STANDARD PRESETS (shown when UseStandardPresets = true)
            // ═══════════════════════════════════════════════════════════════
            if (_settings.ProjectileCamera.UseStandardPresets)
            {
                SectionTitle(CKLocalization.L("ck_section_presets", "PRESETS"));
                GUILayout.Space(10);
                
                // Ensure arrays are proper size
                if (_settings.ProjectileCamera.EnabledPresetsBasicKill == null || 
                    _settings.ProjectileCamera.EnabledPresetsBasicKill.Length < StandardCameraPreset.All.Length)
                {
                    _settings.ProjectileCamera.EnabledPresetsBasicKill = new bool[StandardCameraPreset.All.Length];
                    for (int i = 0; i < _settings.ProjectileCamera.EnabledPresetsBasicKill.Length; i++)
                        _settings.ProjectileCamera.EnabledPresetsBasicKill[i] = true;
                }
                if (_settings.ProjectileCamera.EnabledPresetsTriggers == null || 
                    _settings.ProjectileCamera.EnabledPresetsTriggers.Length < StandardCameraPreset.All.Length)
                {
                    _settings.ProjectileCamera.EnabledPresetsTriggers = new bool[StandardCameraPreset.All.Length];
                    for (int i = 0; i < _settings.ProjectileCamera.EnabledPresetsTriggers.Length; i++)
                        _settings.ProjectileCamera.EnabledPresetsTriggers[i] = true;
                }
                
                // Header row - grey out columns when parent feature is disabled
                bool bkActive = _settings.BasicKill.Enabled && _settings.BasicKill.ProjectileCamera;
                bool tdActive = _settings.TriggerDefaults.EnableTriggers && _settings.TriggerDefaults.ProjectileCamera;
                
                // ═══════════════════════════════════════════════════════════════
                // TWO-COLUMN LAYOUT: Presets (left) | Position + Projectile Follow (right)
                // ═══════════════════════════════════════════════════════════════
                GUILayout.BeginHorizontal();
                
                // ─────────────────────────────────────────────────────────────
                // COLUMN 1 - Presets (Enemy Focus + Player Focus)
                // ─────────────────────────────────────────────────────────────
                GUILayout.BeginVertical(GUILayout.Width(340));
                
                // Enemy Focus header
                Color headerOldColor = GUI.color;
                GUI.color = _cameraColor;
                GUILayout.Label(L("ck_ui_enemy_focus", "── Enemy Focus ──"), _labelStyle);
                GUI.color = headerOldColor;
                GUILayout.Space(5);
                
                // Preset/Basic/Trigger header
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("ck_preset_name", "Preset"), _labelStyle, GUILayout.Width(120));
                GUI.color = bkActive ? Color.white : _mutedColor;
                GUILayout.Label(L("ck_ui_basic", "Basic"), _labelStyle, GUILayout.Width(50));
                GUI.color = tdActive ? Color.white : _mutedColor;
                GUILayout.Label(L("ck_ui_trigger", "Trigger"), _labelStyle, GUILayout.Width(50));
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
                GUILayout.Space(3);
                
                // Draw each standard preset
                int presetCount = StandardCameraPreset.All.Length;
                for (int i = 0; i < presetCount; i++)
                {
                    var preset = StandardCameraPreset.All[i];
                    DrawPresetWithDualToggle(CKLocalization.LocalizePresetName(preset.Name), i,
                        ref _settings.ProjectileCamera.EnabledPresetsBasicKill[i],
                        ref _settings.ProjectileCamera.EnabledPresetsTriggers[i]);
                }
                
                GUILayout.Space(15);
                
                // Player Focus header
                GUI.color = _cameraColor;
                GUILayout.Label(L("ck_ui_player_focus", "── Player Focus ──"), _labelStyle);
                GUI.color = Color.white;
                GUILayout.Space(5);
                
                // Over Shoulder preset
                DrawNearPlayerPreset();
                
                // Behind Enemy preset (X-Ray camera angle)
                GUILayout.Space(3);
                DrawBehindEnemyPreset();
                
                GUILayout.EndVertical();
                
                GUILayout.Space(40);
                
                // ─────────────────────────────────────────────────────────────
                // COLUMN 2 - Position + Projectile Follow
                // ─────────────────────────────────────────────────────────────
                GUILayout.BeginVertical(GUILayout.Width(360));
                
                // Position section
                Color settingsColor = GUI.color;
                GUI.color = _cameraColor;
                GUILayout.Label(L("ck_ui_position_2", "── Position ──"), _labelStyle);
                GUI.color = settingsColor;
                GUILayout.Space(5);
                
                _settings.ProjectileCamera.RandomizeTilt = ToggleRow(L("ck_ui_random_tilt", "Random Tilt"), _settings.ProjectileCamera.RandomizeTilt);
                GUILayout.Space(8);
                
                GUILayout.BeginHorizontal();
                DrawSideOffsetToggle(L("ck_ui_standard", "Standard"), ref _settings.ProjectileCamera.SideOffsetStandard, 
                    _settings.ProjectileCamera.SideOffsetWide || _settings.ProjectileCamera.SideOffsetTight);
                GUILayout.Space(8);
                DrawSideOffsetToggle(L("ck_ui_wide", "Wide"), ref _settings.ProjectileCamera.SideOffsetWide,
                    _settings.ProjectileCamera.SideOffsetStandard || _settings.ProjectileCamera.SideOffsetTight);
                GUILayout.Space(8);
                DrawSideOffsetToggle(L("ck_ui_tight", "Tight"), ref _settings.ProjectileCamera.SideOffsetTight,
                    _settings.ProjectileCamera.SideOffsetStandard || _settings.ProjectileCamera.SideOffsetWide);
                GUILayout.EndHorizontal();
                
                GUILayout.Space(20);
                
                // Projectile Ride Cam - toggleable collapsible
                if (ToggleableHeader(L("ck_ui_projectile_follow", "PROJECTILE FOLLOW"), ref _settings.Experimental.EnableProjectileRideCam))
                {
                    GUILayout.Space(5);
                    
                    // Hint about supported weapons
                    Color hintColor = GUI.color;
                    GUI.color = new Color(0.6f, 0.75f, 0.9f);
                    GUILayout.Label(L("ck_ui_applies_to_bows_crossbows_and_th_84cd0a", "Applies to bows, crossbows, and thrown weapons."), _labelStyle);
                    GUI.color = hintColor;
                    GUILayout.Space(8);
                    
                    _settings.Experimental.RideCamChance = SliderRow("ride_chance", L("ck_ui_chance", "Chance"), _settings.Experimental.RideCamChance, 0f, 100f, "{0:0}%");
                    _settings.Experimental.RideCamFOV = SliderRow("ride_fov", L("ck_fov", "FOV"), _settings.Experimental.RideCamFOV, 60f, 120f, "{0:0}°");
                    _settings.Experimental.RideCamOffset = SliderRow("ride_offset", L("ck_ui_offset", "Offset"), _settings.Experimental.RideCamOffset, 0.5f, 3f, "{0:0.0}m");
                    _settings.Experimental.RideCamPredictiveAiming = ToggleRow(L("ck_ui_predictive_aim", "Predictive Aim"), _settings.Experimental.RideCamPredictiveAiming);
                    _settings.Experimental.RideCamMinTargetHealth = SliderRow("ride_health", L("ck_ui_min_target_hp", "Min Target HP"), _settings.Experimental.RideCamMinTargetHealth, 0f, 100f, "{0:0}");
                }
                
                GUILayout.EndVertical();
                
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                
                // ─────────────────────────────────────────────────────────────
                // DYNAMIC ZOOM (simulates ADS toggle during projectile cinematic)
                // ─────────────────────────────────────────────────────────────
                ColoredSectionHeader(L("ck_ui_dynamic_zoom", "── Dynamic Zoom ──"), _cameraColor);
                GUILayout.Space(3);
                GUI.color = new Color(0.6f, 0.75f, 0.9f);
                GUILayout.Label(L("ck_ui_simulates_ads_toggle_during_proj_f60522", "Simulates ADS toggle during projectile cinematic"), _labelStyle);
                GUI.color = Color.white;
                GUILayout.Space(8);
                
                GUILayout.BeginHorizontal();
                _settings.ProjectileCamera.EnableDynamicZoomIn = ToggleRow(L("ck_ui_zoom_in_ads_on", "Zoom In (ADS On)"), _settings.ProjectileCamera.EnableDynamicZoomIn);
                GUILayout.Space(30);
                _settings.ProjectileCamera.EnableDynamicZoomOut = ToggleRow(L("ck_ui_zoom_out_ads_off", "Zoom Out (ADS Off)"), _settings.ProjectileCamera.EnableDynamicZoomOut);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                // Balance slider only shows when BOTH are enabled
                if (_settings.ProjectileCamera.EnableDynamicZoomIn && _settings.ProjectileCamera.EnableDynamicZoomOut)
                {
                    GUILayout.Space(10);
                    _settings.ProjectileCamera.DynamicZoomBalance = SliderRow("dz_balance", L("ck_ui_balance", "Balance"), 
                        _settings.ProjectileCamera.DynamicZoomBalance, 10f, 90f, "{0:0}% In / {1:0}% Out");
                    
                    // Custom format the balance label manually since SliderRow only supports single value
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(130);
                    float inPct = _settings.ProjectileCamera.DynamicZoomBalance;
                    float outPct = 100f - inPct;
                    GUI.color = new Color(0.6f, 0.75f, 0.9f);
                    GUILayout.Label($"Zoom In for {inPct:0}% of duration, then Zoom Out for {outPct:0}%", _labelStyle);
                    GUI.color = Color.white;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }
            // ═══════════════════════════════════════════════════════════════
            // CAMERA DIMENSIONS (shown when UseStandardPresets = false)
            // ═══════════════════════════════════════════════════════════════
            else
            {
                SectionTitle(CKLocalization.L("ck_section_camera_dimensions", "CAMERA DIMENSIONS"));
                GUILayout.Space(5);
                
                Color noteColor = GUI.color;
                GUI.color = new Color(0.6f, 0.75f, 0.9f);
                GUILayout.Label(CKLocalization.L("ck_camera_tip", "Tip: Pause during a projectile cinematic to tweak sliders and see changes in real-time."), _labelStyle);
                GUI.color = noteColor;
                GUILayout.Space(10);

                // Two-column layout: Position | Rotation
                GUILayout.BeginHorizontal();
                
                // LEFT COLUMN - Position
                GUILayout.BeginVertical(GUILayout.Width(400));
                ColoredSectionHeader(L("ck_ui_position", "── Position * ──"), _cameraColor);
                GUILayout.Space(5);
                
                _settings.ProjectileCamera.Distance = RandomizableSliderRow("cam_dist", 
                    CKLocalization.L("ck_cam_distance", "Distance *"), _settings.ProjectileCamera.Distance, 
                    ref _settings.ProjectileCamera.DistanceMin, ref _settings.ProjectileCamera.DistanceMax, 
                    0.5f, 8f, "{0:0.0}m", ref _settings.ProjectileCamera.RandomizeDistance);
                _settings.ProjectileCamera.Height = RandomizableSliderRow("cam_height", 
                    CKLocalization.L("ck_cam_height", "Height *"), _settings.ProjectileCamera.Height, 
                    ref _settings.ProjectileCamera.HeightMin, ref _settings.ProjectileCamera.HeightMax, 
                    -2f, 4f, "{0:0.0}m", ref _settings.ProjectileCamera.RandomizeHeight);
                _settings.ProjectileCamera.XOffset = RandomizableSliderRow("cam_xoff", 
                    CKLocalization.L("ck_cam_xoffset", "Side Offset *"), _settings.ProjectileCamera.XOffset, 
                    ref _settings.ProjectileCamera.XOffsetMin, ref _settings.ProjectileCamera.XOffsetMax, 
                    -4f, 4f, "{0:0.0}m", ref _settings.ProjectileCamera.RandomizeXOffset);
                
                GUILayout.EndVertical();
                
                GUILayout.Space(30);
                
                // RIGHT COLUMN - Rotation
                GUILayout.BeginVertical(GUILayout.Width(400));
                ColoredSectionHeader(L("ck_ui_rotation", "── Rotation * ──"), _cameraColor);
                GUILayout.Space(5);
                
                _settings.ProjectileCamera.Pitch = RandomizableSliderRow("cam_pitch", 
                    CKLocalization.L("ck_cam_pitch", "Pitch *"), _settings.ProjectileCamera.Pitch, 
                    ref _settings.ProjectileCamera.PitchMin, ref _settings.ProjectileCamera.PitchMax, 
                    -45f, 45f, "{0:0}°", ref _settings.ProjectileCamera.RandomizePitch);
                _settings.ProjectileCamera.Yaw = RandomizableSliderRow("cam_yaw", 
                    CKLocalization.L("ck_cam_yaw", "Yaw *"), _settings.ProjectileCamera.Yaw, 
                    ref _settings.ProjectileCamera.YawMin, ref _settings.ProjectileCamera.YawMax, 
                    -180f, 180f, "{0:0}°", ref _settings.ProjectileCamera.RandomizeYaw);
                
                // Simple tilt toggle
                _settings.ProjectileCamera.RandomizeTilt = ToggleRow(L("ck_ui_random_tilt", "Random Tilt"), _settings.ProjectileCamera.RandomizeTilt);
                
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(25);

                // ═══════════════════════════════════════════════════════════════
                // CUSTOM PRESETS
                // ═══════════════════════════════════════════════════════════════
                SectionTitle(CKLocalization.L("ck_section_custom_presets", "CUSTOM PRESETS"));
                GUILayout.Label(CKLocalization.L("ck_presets_desc", "Save current values as a named preset"), _labelStyle);
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.Label(CKLocalization.L("ck_preset_name", "Preset Name:"), _labelStyle, GUILayout.Width(100));
                _newPresetName = GUILayout.TextField(_newPresetName, GUILayout.Width(200));
                GUILayout.Space(10);
                if (GUILayout.Button(CKLocalization.L("ck_save_preset", "Save Preset"), GUILayout.Width(100)))
                {
                    if (!string.IsNullOrEmpty(_newPresetName))
                    {
                        var preset = _settings.ProjectileCamera.CreatePresetFromCurrent(_newPresetName);
                        _settings.ProjectileCamera.CustomPresets.Add(preset);
                        _newPresetName = "";
                        Log.Out($"CinematicKill: Saved camera preset '{preset.Name}' (D:{preset.Distance:0.0}, H:{preset.Height:0.0}, X:{preset.XOffset:0.0}, T:{preset.Tilt:0})");
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(15);

                if (_settings.ProjectileCamera.CustomPresets.Count > 0)
                {
                    GUILayout.Label(CKLocalization.L("ck_saved_presets", "Saved Presets:"), _labelStyle);
                    GUILayout.Space(5);
                    
                    CameraPreset presetToDelete = null;
                    foreach (var preset in _settings.ProjectileCamera.CustomPresets)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        
                        string info = $"{preset.Name} (D:{preset.Distance:0.0}, H:{preset.Height:0.0}, X:{preset.XOffset:0.0}, T:{preset.Tilt:0}°)";
                        GUILayout.Label(info, _labelStyle, GUILayout.Width(400));
                        
                        if (GUILayout.Button(CKLocalization.L("ck_load", "Load"), GUILayout.Width(60)))
                        {
                            _settings.ProjectileCamera.ApplyPreset(preset);
                        }
                        
                        if (GUILayout.Button(CKLocalization.L("ck_delete", "X"), GUILayout.Width(25)))
                        {
                            presetToDelete = preset;
                        }
                        
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    
                    if (presetToDelete != null)
                    {
                        _settings.ProjectileCamera.CustomPresets.Remove(presetToDelete);
                    }
                }
                else
                {
                    GUILayout.Label(CKLocalization.L("ck_no_presets", "No custom presets saved yet."), _labelStyle);
                }
            }
            
            GUILayout.Space(25);
            
            // ═══════════════════════════════════════════════════════════════
            // CHAIN REACTION - Multi-kill camera daisy chain
            // ═══════════════════════════════════════════════════════════════
            var exp = _settings.Experimental;
            if (ToggleableHeader(L("ck_ui_chain_reaction", "CHAIN REACTION"), ref exp.EnableChainReaction))
            {
                GUILayout.Space(10);
                
                exp.ChainReactionWindow = SliderRow("chain_window", L("ck_ui_kill_window", "Kill Window"), exp.ChainReactionWindow, 0.5f, 5f, "{0:0.0}s");
                exp.ChainReactionMaxKills = Mathf.RoundToInt(SliderRow("chain_max", L("ck_ui_max_kills", "Max Kills"), exp.ChainReactionMaxKills, 2f, 10f, "{0:0}"));
                exp.ChainCameraTransitionTime = SliderRow("chain_trans", L("ck_ui_camera_transition", "Camera Transition"), exp.ChainCameraTransitionTime, 0.2f, 1f, "{0:0.0}s");
                
                GUILayout.Space(5);
                exp.ChainReactionSlowMoRamp = TextToggle(L("ck_ui_ramp_slow_mo_per_kill", "Ramp Slow-Mo Per Kill"), exp.ChainReactionSlowMoRamp);
                if (exp.ChainReactionSlowMoRamp)
                {
                    exp.ChainSlowMoMultiplier = SliderRow("chain_slowmo", L("ck_ui_slow_mo_multiplier", "Slow-Mo Multiplier"), exp.ChainSlowMoMultiplier, 0.5f, 0.9f, "{0:0.00}x");
                }
                
                GUILayout.Space(5);
                GUILayout.Label(L("ck_ui_chain_camera_between_multiple_ki_497cc0", "Chain camera between multiple kills in quick succession."), _labelStyle);
            }
            
            // ═══════════════════════════════════════════════════════════════
            // PROJECTILE FREEZE FRAME SECTION
            // ═══════════════════════════════════════════════════════════════
            GUILayout.Space(25);
            var projFreeze = _settings.ProjectileFreezeFrame;
            
            if (ToggleableHeader(L("ck_ui_freeze_frame", "FREEZE FRAME"), ref projFreeze.Enabled))
            {
                GUILayout.Space(10);
                
                projFreeze.Chance = SliderRow("proj_freeze_chance", L("ck_ui_chance", "Chance"), projFreeze.Chance, 0f, 100f, "{0:0}%");
                projFreeze.Duration = SliderRow("proj_freeze_duration", L("ck_duration_label", "Duration"), projFreeze.Duration, 0.1f, 5f, "{0:0.0}s");
                projFreeze.Delay = SliderRow("proj_freeze_delay", L("ck_ui_delay", "Delay"), projFreeze.Delay, 0f, 2f, "{0:0.00}s");
                
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("ck_ui_trigger_on", "Trigger on:"), _labelStyle, GUILayout.Width(80));
                projFreeze.TriggerOnBasicKill = TextToggle(L("ck_apply_to_bk", "Basic Kill"), projFreeze.TriggerOnBasicKill);
                GUILayout.Space(20);
                projFreeze.TriggerOnSpecialTrigger = TextToggle(L("ck_ui_special_trigger", "Special Trigger"), projFreeze.TriggerOnSpecialTrigger);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                // Camera Movement
                GUILayout.Space(15);
                ColoredSectionHeader(L("ck_ui_camera_movement", "── Camera Movement ──"), _cameraColor);
                GUILayout.Space(5);
                projFreeze.EnableCameraMovement = ToggleRow(L("ck_ui_enable_camera_movement", "Enable Camera Movement"), projFreeze.EnableCameraMovement);
                if (projFreeze.EnableCameraMovement)
                {
                    projFreeze.TimeScale = SliderRow("proj_freeze_timescale", L("ck_time_scale_label", "Time Scale"), projFreeze.TimeScale, 0.01f, 0.1f, "{0:0.00}x");
                    projFreeze.RandomizePreset = ToggleRow(L("ck_ui_randomize_camera_preset", "Randomize Camera Preset"), projFreeze.RandomizePreset);
                }
                
                // Contrast Effect
                GUILayout.Space(15);
                ColoredSectionHeader(L("ck_ui_contrast_effect", "── Contrast Effect ──"), _effectColor);
                GUILayout.Space(5);
                projFreeze.EnableContrastEffect = ToggleRow(L("ck_ui_enable_contrast_effect", "Enable Contrast Effect"), projFreeze.EnableContrastEffect);
                if (projFreeze.EnableContrastEffect)
                {
                    projFreeze.ContrastAmount = SliderRow("proj_freeze_contrast", L("ck_ui_contrast", "Contrast"), projFreeze.ContrastAmount, 1f, 1.8f, "{0:0.0}x");
                }
                
                // Post-Freeze Action
                GUILayout.Space(15);
                ColoredSectionHeader(L("ck_ui_after_freeze", "── After Freeze ──"), _cameraColor);
                GUILayout.Space(5);
                DrawPostFreezeActionSelector(projFreeze);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  SHARED PROJECTILE FOV SETTINGS
        // ═══════════════════════════════════════════════════════════════
        private void DrawProjectileFOVSettings()
        {
            ColoredSectionHeader(L("ck_ui_fov_zoom", "── FOV Zoom ──"), _cameraColor);
            GUILayout.Space(5);
            _settings.ProjectileCamera.FOVMode = FOVModeToggle(CKLocalization.L("ck_proj_fov_mode", "Mode"), _settings.ProjectileCamera.FOVMode);
            
            if (_settings.ProjectileCamera.FOVMode != FOVMode.Off)
            {
                string projZoomLabel = _settings.ProjectileCamera.FOVMode == FOVMode.ZoomIn 
                    ? CKLocalization.L("ck_zoom_in", "Zoom In %") 
                    : CKLocalization.L("ck_zoom_out", "Zoom Out %");
                _settings.ProjectileCamera.FOVPercent = SliderRow(projZoomLabel, _settings.ProjectileCamera.FOVPercent, 5f, 50f, "{0:0}%");
                
                // Advanced timing - clickable label to expand
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                Color advColor = GUI.color;
                GUI.color = _projAdvancedFOVExpanded ? new Color(0.4f, 1f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
                if (GUILayout.Button(L("ck_ui_advanced_timing", "Advanced Timing *"), _labelStyle, GUILayout.Width(130)))
                {
                    _projAdvancedFOVExpanded = !_projAdvancedFOVExpanded;
                }
                GUI.color = advColor;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                if (_projAdvancedFOVExpanded)
                {
                    GUILayout.Space(3);
                    float cinematicDuration = _settings.BasicKill.Duration;
                    DrawFovTimingRows("proj_shared", _settings.ProjectileCamera.FOVMode, ref _settings.ProjectileCamera.FOVZoomInDuration, ref _settings.ProjectileCamera.FOVHoldDuration, ref _settings.ProjectileCamera.FOVZoomOutDuration, cinematicDuration);
                }
            }
        }


        // ═══════════════════════════════════════════════════════════════
        //  TAB 2: TRIGGERS
        // ═══════════════════════════════════════════════════════════════
        private void DrawTriggersTab()
        {
            // Show placeholder if cinematics disabled
            if (!_settings.EnableCinematics)
            {
                SectionTitle(CKLocalization.L("ck_section_cinematics_disabled", "CINEMATICS DISABLED"));
                GUILayout.Space(20);
                GUILayout.Label(CKLocalization.L("ck_enable_cinematics_triggers_hint", "Enable Cinematics in the Main tab to configure triggers."), _labelStyle);
                GUILayout.Space(20);
                GUILayout.Label(CKLocalization.L("ck_triggers_tab_desc1", "This tab allows you to configure weapon modes and"), _labelStyle);
                GUILayout.Label(CKLocalization.L("ck_triggers_tab_desc2", "individual trigger overrides."), _labelStyle);
                return;
            }
            
            if (!_settings.TriggerDefaults.EnableTriggers)
            {
                SectionTitle(CKLocalization.L("ck_section_triggers_disabled", "TRIGGERS DISABLED"));
                GUILayout.Space(10);
                GUILayout.Label(CKLocalization.L("ck_enable_triggers_hint", "Enable Special Triggers in Main tab first."), _labelStyle);
                return;
            }

            // ═══════════════════════════════════════════════════════════════
            // WEAPON MODES + ACTIVE TRIGGERS - Two column layout
            // ═══════════════════════════════════════════════════════════════
            GUILayout.BeginHorizontal();
            
            // LEFT COLUMN - Weapon Modes
            GUILayout.BeginVertical(GUILayout.Width(500));
            SectionTitle(CKLocalization.L("ck_section_weapon_modes", "WEAPON MODES"));
            GUILayout.Label(CKLocalization.L("ck_weapon_modes_desc", "Auto uses default camera | FP/Proj forces specific camera"), _labelStyle);
            GUILayout.Space(10);

            DrawWeaponModeWithOverride(CKLocalization.L("ck_weapon_melee", "Melee"), ref _settings.MeleeEnabled, ref _settings.MeleeCameraOverride);
            DrawWeaponModeWithOverride(CKLocalization.L("ck_weapon_ranged", "Ranged"), ref _settings.RangedEnabled, ref _settings.RangedCameraOverride);
            DrawWeaponModeWithOverride(CKLocalization.L("ck_weapon_bows", "Bows"), ref _settings.BowEnabled, ref _settings.BowCameraOverride);
            DrawWeaponModeWithOverride(CKLocalization.L("ck_weapon_explosives", "Explosives"), ref _settings.ExplosiveEnabled, ref _settings.ExplosiveCameraOverride);
            // Trap mode removed - hardcoded as always disabled
            
            GUILayout.EndVertical();
            
            GUILayout.Space(30);
            
            // RIGHT COLUMN - Active Triggers
            GUILayout.BeginVertical(GUILayout.Width(500));
            SectionTitle(CKLocalization.L("ck_section_active_triggers", "ACTIVE TRIGGERS"));
            GUILayout.Label(CKLocalization.L("ck_active_triggers_desc", "Only enabled triggers can cause cinematics"), _labelStyle);
            GUILayout.Space(10);


            GUILayout.BeginHorizontal();
            _settings.Headshot.Enabled = TextToggle(CKLocalization.L("ck_trigger_headshot", "Headshot"), _settings.Headshot.Enabled);
            GUILayout.Space(10);
            _settings.Critical.Enabled = TextToggle(CKLocalization.L("ck_trigger_critical", "Critical"), _settings.Critical.Enabled);
            GUILayout.Space(10);
            _settings.LastEnemy.Enabled = TextToggle(CKLocalization.L("ck_trigger_last_enemy", "Last Enemy"), _settings.LastEnemy.Enabled);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            _settings.LongRange.Enabled = TextToggle(CKLocalization.L("ck_trigger_long_range", "Long Range"), _settings.LongRange.Enabled);
            GUILayout.Space(10);
            _settings.LowHealth.Enabled = TextToggle(CKLocalization.L("ck_trigger_low_health", "Low Health"), _settings.LowHealth.Enabled);
            GUILayout.Space(10);
            _settings.Dismember.Enabled = TextToggle(CKLocalization.L("ck_trigger_dismember", "Dismember"), _settings.Dismember.Enabled);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            _settings.Killstreak.Enabled = TextToggle(CKLocalization.L("ck_trigger_killstreak", "Killstreak"), _settings.Killstreak.Enabled);
            GUILayout.Space(10);
            _settings.Sneak.Enabled = TextToggle(CKLocalization.L("ck_trigger_sneak", "Sneak"), _settings.Sneak.Enabled);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            

            
            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            // ═══════════════════════════════════════════════════════════════
            // TRIGGER THRESHOLDS
            // ═══════════════════════════════════════════════════════════════
            GUILayout.BeginVertical();
            GUILayout.Space(8);
            ColoredSectionHeader(L("ck_ui_trigger_thresholds", "─ TRIGGER THRESHOLDS ─"), _cameraColor);
            GUILayout.Space(5);
            
            // Two column layout within the box
            GUILayout.BeginHorizontal();
            
            // LEFT - Distance and health thresholds
            GUILayout.BeginVertical(GUILayout.Width(450));
            _settings.LongRangeDistance = SliderRow(CKLocalization.L("ck_long_range_distance", "Long Range Distance"), _settings.LongRangeDistance, 10f, 100f, "{0:0}m");
            _settings.LowHealthPercent = SliderRow(CKLocalization.L("ck_low_health_percent", "Low Health %"), _settings.LowHealthPercent, 5f, 50f, "{0:0}%");
            _settings.EnemyScanRadius = SliderRow(CKLocalization.L("ck_enemy_scan_radius", "Enemy Scan Radius"), _settings.EnemyScanRadius, 5f, 50f, "{0:0}m");
            GUILayout.EndVertical();
            
            GUILayout.Space(30);
            
            // RIGHT - Killstreak thresholds
            GUILayout.BeginVertical(GUILayout.Width(450));
            ColoredSectionHeader(L("ck_ui_killstreak", "── Killstreak ──"), _cameraColor);
            _settings.KillstreakWindow = SliderRow(CKLocalization.L("ck_killstreak_window", "Time Window"), _settings.KillstreakWindow, 3f, 30f, "{0:0}s");
            _settings.KillstreakKillsRequired = (int)SliderRow(CKLocalization.L("ck_killstreak_kills", "Kills Required"), _settings.KillstreakKillsRequired, 2, 10, "{0:0}");
            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(8);
            GUILayout.EndVertical();

            GUILayout.Space(20);

            // ═══════════════════════════════════════════════════════════════
            // TRIGGER OVERRIDES
            // ═══════════════════════════════════════════════════════════════
            SectionTitle(CKLocalization.L("ck_section_trigger_overrides", "TRIGGER OVERRIDES"));
            GUILayout.Label(CKLocalization.L("ck_trigger_overrides_desc", "Override default settings for specific triggers"), _labelStyle);
            GUILayout.Space(10);

            // Only show enabled triggers
            if (_settings.Headshot.Enabled)
                DrawTriggerOverride(CKLocalization.L("ck_trigger_headshot", "Headshot"), ref _settings.Headshot, ref _headshotExpanded);

            if (_settings.Critical.Enabled)
                DrawTriggerOverride(CKLocalization.L("ck_trigger_critical", "Critical"), ref _settings.Critical, ref _criticalExpanded);

            if (_settings.LastEnemy.Enabled)
                DrawTriggerOverride(CKLocalization.L("ck_trigger_last_enemy", "Last Enemy"), ref _settings.LastEnemy, ref _lastEnemyExpanded);

            if (_settings.LongRange.Enabled)
                DrawTriggerOverride(CKLocalization.L("ck_trigger_long_range", "Long Range"), ref _settings.LongRange, ref _longRangeExpanded);

            if (_settings.LowHealth.Enabled)
                DrawTriggerOverride(CKLocalization.L("ck_trigger_low_health", "Low Health"), ref _settings.LowHealth, ref _lowHealthExpanded);

            if (_settings.Dismember.Enabled)
                DrawTriggerOverride(CKLocalization.L("ck_trigger_dismember", "Dismember"), ref _settings.Dismember, ref _dismemberExpanded);

            if (_settings.Killstreak.Enabled)
                DrawTriggerOverride(CKLocalization.L("ck_trigger_killstreak", "Killstreak"), ref _settings.Killstreak, ref _killstreakExpanded);

            if (_settings.Sneak.Enabled)
                DrawTriggerOverride(CKLocalization.L("ck_trigger_sneak", "Sneak"), ref _settings.Sneak, ref _sneakExpanded);

            if (!_settings.HasEnabledTriggers())
            {
                GUILayout.Label(CKLocalization.L("ck_no_triggers_enabled", "No triggers enabled. Enable triggers above."), _labelStyle);
            }
        }

        private void DrawTriggerOverride(string name, ref CKTriggerSettings trigger, ref bool expanded)
        {
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = expanded
                ? new Color(0.2f, 0.35f, 0.2f)
                : (trigger.Override ? new Color(0.35f, 0.3f, 0.15f) : new Color(0.15f, 0.15f, 0.2f));

            string arrow = expanded ? "▼" : "►";
            string status = trigger.Override ? $" [{CKLocalization.L("ck_override", "OVERRIDE")}]" : "";
            if (GUILayout.Button($"{arrow} {name}{status}", _expandButtonStyle, GUILayout.Height(28)))
            {
                expanded = !expanded;
            }
            GUI.backgroundColor = oldBg;

            if (expanded)
            {
                GUILayout.BeginVertical();
                GUILayout.Space(8);

                trigger.Override = ToggleRow(CKLocalization.L("ck_override_global_settings", "Override Global Settings"), trigger.Override);

                if (trigger.Override)
                {
                    GUILayout.Space(8);
                    
                    // Two-column layout: Timing | Camera
                    GUILayout.BeginHorizontal();
                    
                    // LEFT COLUMN - Timing & Chance
                    GUILayout.BeginVertical(GUILayout.Width(450));
                    ColoredSectionHeader(L("ck_ui_timing", "── Timing ──"), _cameraColor);
                    GUILayout.Space(3);
                    
                    // Override Chance
                    GUILayout.BeginHorizontal();
                    trigger.OverrideChance = ToggleCompact(CKLocalization.L("ck_override_chance", "Override Chance"), trigger.OverrideChance);
                    if (trigger.OverrideChance)
                    {
                        GUILayout.Space(10);
                        trigger.Chance = GUILayout.HorizontalSlider(trigger.Chance, 0f, 100f, GUILayout.Width(100));
                        GUILayout.Label($"{trigger.Chance:0}%", _valueStyle, GUILayout.Width(40));
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    
                    trigger.Duration = SliderRow(CKLocalization.L("ck_duration_label", "Duration"), trigger.Duration, 0.3f, 5f, "{0:0.0}s");
                    trigger.TimeScale = SliderRow(CKLocalization.L("ck_time_scale_label", "Time Scale"), trigger.TimeScale, 0.05f, 1f, "{0:0.00}x");
                    trigger.Cooldown = SliderRow(CKLocalization.L("ck_cooldown", "Cooldown"), trigger.Cooldown, 0f, 30f, "{0:0.0}s");
                    
                    GUILayout.EndVertical();
                    
                    GUILayout.Space(20);
                    
                    // RIGHT COLUMN - Camera
                    GUILayout.BeginVertical(GUILayout.Width(450));
                    ColoredSectionHeader(L("ck_ui_camera", "── Camera ──"), _cameraColor);
                    GUILayout.Space(3);
                    
                    GUILayout.BeginHorizontal();
                    trigger.FirstPersonCamera = ToggleCompact(CKLocalization.L("ck_first_person_camera", "First Person"), trigger.FirstPersonCamera);
                    GUILayout.Space(15);
                    trigger.ProjectileCamera = ToggleCompact(CKLocalization.L("ck_projectile_camera", "Projectile"), trigger.ProjectileCamera);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    if (trigger.FirstPersonCamera && trigger.ProjectileCamera)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(L("ck_ui_fp_proj_split", "  FP/Proj Split:"), _labelStyle, GUILayout.Width(100));
                        trigger.FirstPersonChance = GUILayout.HorizontalSlider(trigger.FirstPersonChance, 0f, 100f, GUILayout.Width(100));
                        GUILayout.Label($"{trigger.FirstPersonChance:0}%/{100-trigger.FirstPersonChance:0}%", _valueStyle);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }

                    if (trigger.FirstPersonCamera)
                    {
                        GUILayout.Space(5);
                        GUILayout.Label(L("ck_ui_fp_fov", "  FP FOV:"), _labelStyle);
                        trigger.FOVMode = FOVModeToggle("", trigger.FOVMode);
                        if (trigger.FOVMode != FOVMode.Off)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(L("ck_ui_zoom", "  Zoom:"), _labelStyle, GUILayout.Width(50));
                            trigger.FOVPercent = GUILayout.HorizontalSlider(trigger.FOVPercent, 5f, 50f, GUILayout.Width(100));
                            GUILayout.Label($"{trigger.FOVPercent:0}%", _valueStyle, GUILayout.Width(40));
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                    }
                    
                    if (trigger.ProjectileCamera)
                    {
                        GUILayout.Space(5);
                        GUILayout.Label(L("ck_ui_proj_fov", "  Proj FOV:"), _labelStyle);
                        trigger.ProjectileFOVMode = FOVModeToggle("", trigger.ProjectileFOVMode);
                        if (trigger.ProjectileFOVMode != FOVMode.Off)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(L("ck_ui_zoom", "  Zoom:"), _labelStyle, GUILayout.Width(50));
                            trigger.ProjectileFOVPercent = GUILayout.HorizontalSlider(trigger.ProjectileFOVPercent, 5f, 50f, GUILayout.Width(100));
                            GUILayout.Label($"{trigger.ProjectileFOVPercent:0}%", _valueStyle, GUILayout.Width(40));
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                    }
                    
                    GUILayout.EndVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
            
            // Projectile camera preset override (compact multi-select grid)
            if (trigger.ProjectileCamera)
            {
                GUILayout.Space(15);
                trigger.OverridePresets = ToggleRow(
                    CKLocalization.L("ck_override_presets", "Override Camera Presets"), 
                    trigger.OverridePresets);
                
                if (trigger.OverridePresets)
                {
                    // Ensure array is proper size
                    if (trigger.EnabledPresets == null || trigger.EnabledPresets.Length < StandardCameraPreset.All.Length)
                    {
                        trigger.EnabledPresets = new bool[StandardCameraPreset.All.Length];
                        trigger.EnabledPresets[0] = true;
                    }
                    
                    GUILayout.Space(5);
                    Color tipColor = GUI.color;
                    GUI.color = new Color(0.6f, 0.75f, 0.9f);
                    GUILayout.Label(CKLocalization.L("ck_preset_override_desc", "  Select presets for this trigger only"), _labelStyle);
                    GUI.color = tipColor;
                    GUILayout.Space(5);
                    
                    // Compact preset grid - 6 columns, smaller buttons
                    int presetsPerRow = 6;
                    int presetCount = StandardCameraPreset.All.Length;
                    for (int row = 0; row < (presetCount + presetsPerRow - 1) / presetsPerRow; row++)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(15);
                        for (int col = 0; col < presetsPerRow; col++)
                        {
                            int index = row * presetsPerRow + col;
                            if (index < presetCount)
                            {
                                var preset = StandardCameraPreset.All[index];
                                bool isEnabled = trigger.EnabledPresets[index];
                                
                                Color btnBg = GUI.backgroundColor;
                                GUI.backgroundColor = isEnabled 
                                    ? _toggleOnColor : _toggleOffColor;
                                
                                if (GUILayout.Button(CKLocalization.LocalizePresetName(preset.Name), GUILayout.Width(130), GUILayout.Height(24)))
                                {
                                    if (isEnabled)
                                    {
                                        int enabledCount = 0;
                                        for (int i = 0; i < trigger.EnabledPresets.Length; i++)
                                            if (trigger.EnabledPresets[i]) enabledCount++;
                                        if (enabledCount > 1)
                                            trigger.EnabledPresets[index] = false;
                                    }
                                    else
                                    {
                                        trigger.EnabledPresets[index] = true;
                                    }
                                }
                                GUI.backgroundColor = btnBg;
                            }
                            GUILayout.Space(3);
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2);
                    }
                }
            }
        }
        else
        {
            GUILayout.Label(CKLocalization.L("ck_using_global_settings", "  Using global settings from Main tab"), _labelStyle);
        }

        GUILayout.Space(8);
        GUILayout.EndVertical();
    }

    GUILayout.Space(5);
}

        // ═══════════════════════════════════════════════════════════════
        //  TAB 2: EFFECTS
        // ═══════════════════════════════════════════════════════════════
        private void DrawEffectsTab()
        {
            // Show placeholder if cinematics disabled
            if (!_settings.EnableCinematics)
            {
                SectionTitle(CKLocalization.L("ck_section_cinematics_disabled", "CINEMATICS DISABLED"));
                GUILayout.Space(20);
                GUILayout.Label(CKLocalization.L("ck_enable_cinematics_effects_hint", "Enable Cinematics in Main tab to access Effects settings."), _labelStyle);
                return;
            }
            
            var exp = _settings.Experimental;
            var fx = _settings.ScreenEffects;
            
            
            // ═══════════════════════════════════════════════════════════════
            // KILL FLASH SECTION
            // ═══════════════════════════════════════════════════════════════
            if (ToggleableHeader(L("ck_ui_kill_flash", "KILL FLASH"), ref fx.EnableKillFlash))
            {
                GUILayout.Space(10);
                
                fx.KillFlashIntensity = SliderRow("flash_int", L("ck_ui_intensity", "Intensity"), fx.KillFlashIntensity, 0.5f, 3f, "{0:0.0}");
                
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("ck_ui_trigger_on", "Trigger on:"), _labelStyle, GUILayout.Width(80));
                fx.KillFlash_FP = TextToggle(L("ck_fp", "FP"), fx.KillFlash_FP);
                GUILayout.Space(10);
                fx.KillFlash_Projectile = TextToggle(L("ck_proj", "Proj"), fx.KillFlash_Projectile);
                GUILayout.Space(10);
                if (exp.EnableFreezeFrame) fx.KillFlash_Freeze = TextToggle(L("ck_ui_freeze", "Freeze"), fx.KillFlash_Freeze);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(25);
            
            // ═══════════════════════════════════════════════════════════════
            // BLOOD SPLATTER SECTION
            // ═══════════════════════════════════════════════════════════════
            if (ToggleableHeader(L("ck_ui_blood_splatter", "BLOOD SPLATTER"), ref fx.EnableBloodSplatter))
            {
                GUILayout.Space(10);
                
                fx.BloodSplatterIntensity = SliderRow("blood_int", L("ck_ui_intensity", "Intensity"), fx.BloodSplatterIntensity, 0.5f, 3f, "{0:0.0}");
                
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("ck_ui_trigger_on", "Trigger on:"), _labelStyle, GUILayout.Width(80));
                fx.BloodSplatter_FP = TextToggle(L("ck_fp", "FP"), fx.BloodSplatter_FP);
                GUILayout.Space(10);
                fx.BloodSplatter_Projectile = TextToggle(L("ck_proj", "Proj"), fx.BloodSplatter_Projectile);
                GUILayout.Space(10);
                if (exp.EnableFreezeFrame) fx.BloodSplatter_Freeze = TextToggle(L("ck_ui_freeze", "Freeze"), fx.BloodSplatter_Freeze);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(25);
            
            // ═══════════════════════════════════════════════════════════════
            // VIGNETTE SECTION
            // ═══════════════════════════════════════════════════════════════
            if (ToggleableHeader(L("ck_vignette", "VIGNETTE"), ref fx.EnableVignette))
            {
                GUILayout.Space(10);
                
                fx.VignetteIntensity = SliderRow("vig_int", L("ck_ui_intensity", "Intensity"), fx.VignetteIntensity, 0.1f, 1f, "{0:0.00}");
                
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("ck_ui_trigger_on", "Trigger on:"), _labelStyle, GUILayout.Width(80));
                fx.Vignette_FP = TextToggle(L("ck_fp", "FP"), fx.Vignette_FP);
                GUILayout.Space(10);
                fx.Vignette_Projectile = TextToggle(L("ck_proj", "Proj"), fx.Vignette_Projectile);
                GUILayout.Space(10);
                if (exp.EnableFreezeFrame) fx.Vignette_Freeze = TextToggle(L("ck_ui_freeze", "Freeze"), fx.Vignette_Freeze);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(25);
            
            // ═══════════════════════════════════════════════════════════════
            // DESATURATION SECTION
            // ═══════════════════════════════════════════════════════════════
            if (ToggleableHeader(L("ck_desaturation", "DESATURATION"), ref fx.EnableDesaturation))
            {
                GUILayout.Space(10);
                
                fx.DesaturationAmount = SliderRow("desat_amt", L("ck_ui_amount", "Amount"), fx.DesaturationAmount, 0.1f, 1f, "{0:0.00}");
                
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("ck_ui_trigger_on", "Trigger on:"), _labelStyle, GUILayout.Width(80));
                fx.Desaturation_FP = TextToggle(L("ck_fp", "FP"), fx.Desaturation_FP);
                GUILayout.Space(10);
                fx.Desaturation_Projectile = TextToggle(L("ck_proj", "Proj"), fx.Desaturation_Projectile);
                GUILayout.Space(10);
                if (exp.EnableFreezeFrame) fx.Desaturation_Freeze = TextToggle(L("ck_ui_freeze", "Freeze"), fx.Desaturation_Freeze);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(25);
            
            // ═══════════════════════════════════════════════════════════════
            // SLOW-MO TOGGLE SECTION
            // ═══════════════════════════════════════════════════════════════
            if (ToggleableHeader(L("ck_ui_slow_mo_toggle", "SLOW-MO TOGGLE"), ref exp.EnableSlowMoToggle))
            {
                GUILayout.Space(10);
                
                // Show current keybind
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("ck_ui_toggle_key", "Toggle Key:"), _labelStyle, GUILayout.Width(120));
                GUILayout.Label(exp.SlowMoToggleKey.ToString(), _valueStyle);
                GUILayout.EndHorizontal();
                
                exp.SlowMoToggleTimeScale = SliderRow("slowmo_scale", L("ck_time_scale_label", "Time Scale"), exp.SlowMoToggleTimeScale, 0.1f, 0.5f, "{0:0.00}");
                
                GUILayout.Space(5);
                GUILayout.Label(L("ck_ui_press_middle_mouse_to_toggle_slo_43f37f", "Press Middle Mouse to toggle slow motion on/off at any time."), _labelStyle);
            }
            
        }

        #endregion Tab Drawing

        /// <summary>
        /// Draws the post-freeze action selector for both FP and Projectile freeze settings.
        /// Switch Cam option is only shown for projectile mode (isFirstPerson = false).
        /// </summary>
        private void DrawPostFreezeActionSelector(CKFreezeFrameSettings settings, bool isFirstPerson = false)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(L("ck_ui_action", "Action:"), _labelStyle, GUILayout.Width(80));
            
            // End
            Color oldColor = GUI.color;
            GUI.color = settings.PostAction == PostFreezeAction.End ? _enabledColor : _mutedColor;
            if (GUILayout.Button(settings.PostAction == PostFreezeAction.End ? "● " + L("ck_ui_end", "End") : "○ " + L("ck_ui_end", "End"), _labelStyle, GUILayout.Width(60)))
            {
                settings.PostAction = PostFreezeAction.End;
            }
            
            // Continue
            GUI.color = settings.PostAction == PostFreezeAction.ContinueCinematic ? _enabledColor : _mutedColor;
            if (GUILayout.Button(settings.PostAction == PostFreezeAction.ContinueCinematic ? "● " + L("ck_ui_continue", "Continue") : "○ " + L("ck_ui_continue", "Continue"), _labelStyle, GUILayout.Width(80)))
            {
                settings.PostAction = PostFreezeAction.ContinueCinematic;
            }
            
            // Switch Camera - only show for projectile mode (not FP)
            if (!isFirstPerson)
            {
                GUI.color = settings.PostAction == PostFreezeAction.SwitchCamera ? _cameraColor : _mutedColor;
                if (GUILayout.Button(settings.PostAction == PostFreezeAction.SwitchCamera ? "● " + L("ck_ui_switch_cam", "Switch Cam") : "○ " + L("ck_ui_switch_cam", "Switch Cam"), _labelStyle, GUILayout.Width(95)))
                {
                    settings.PostAction = PostFreezeAction.SwitchCamera;
                }
            }
            else
            {
                // If FP mode has SwitchCamera selected, reset to End
                if (settings.PostAction == PostFreezeAction.SwitchCamera)
                {
                    settings.PostAction = PostFreezeAction.End;
                }
            }
            
            // Skip
            GUI.color = settings.PostAction == PostFreezeAction.Skip ? _warningColor : _mutedColor;
            if (GUILayout.Button(settings.PostAction == PostFreezeAction.Skip ? "● " + L("ck_ui_skip", "Skip") : "○ " + L("ck_ui_skip", "Skip"), _labelStyle, GUILayout.Width(60)))
            {
                settings.PostAction = PostFreezeAction.Skip;
            }
            
            GUI.color = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            // Only show randomize camera option for projectile mode with Switch Camera selected
            if (!isFirstPerson && settings.PostAction == PostFreezeAction.SwitchCamera)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(80);
                settings.RandomizePostCamera = ToggleCompact(L("ck_ui_randomize_camera_after_freeze", "Randomize Camera After Freeze"), settings.RandomizePostCamera);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            
            // Help text
            GUILayout.Space(5);
            string helpText = settings.PostAction switch
            {
                PostFreezeAction.End => L("ck_ui_end_help", "End immediately after freeze, return to normal gameplay."),
                PostFreezeAction.ContinueCinematic => L("ck_ui_continue_help", "Resume the slow-mo cinematic after freeze ends."),
                PostFreezeAction.SwitchCamera => L("ck_ui_switch_cam_help", "Switch to a new camera angle and continue cinematic."),
                PostFreezeAction.Skip => L("ck_ui_skip_help", "End freeze, skip remaining cinematic effects."),
                _ => ""
            };
            Color tipColor = GUI.color;
            GUI.color = new Color(0.6f, 0.75f, 0.9f);
            GUILayout.Label($"  {helpText}", _labelStyle);
            GUI.color = tipColor;
        }

        #region UI Helpers

        /// <summary>
        /// Text-based toggle - displays the label in color based on state
        /// Green = enabled, Gray = disabled
        /// </summary>
        private bool TextToggle(string label, bool value)
        {
            Color oldColor = GUI.color;
            GUI.color = value ? _enabledColor : _mutedColor;
            
            if (GUILayout.Button(value ? $"● {label}" : $"○ {label}", _labelStyle, GUILayout.ExpandWidth(false)))
            {
                value = !value;
            }
            
            GUI.color = oldColor;
            return value;
        }

        /// <summary>
        /// Weapon mode row with enable toggle and camera override selector
        /// </summary>
        private void DrawWeaponModeWithOverride(string name, ref bool enabled, ref CameraOverride camOverride)
        {
            GUILayout.BeginHorizontal();
            
            // Enable toggle
            Color oldColor = GUI.color;
            GUI.color = enabled ? _enabledColor : _mutedColor;
            if (GUILayout.Button(enabled ? $"● {name}" : $"○ {name}", _labelStyle, GUILayout.Width(90)))
            {
                enabled = !enabled;
            }
            GUI.color = oldColor;
            
            if (enabled)
            {
                GUILayout.Space(10);
                GUILayout.Label(CKLocalization.L("ck_camera_label", "Camera:"), _labelStyle, GUILayout.Width(55));
                
                // Auto button
                GUI.color = camOverride == CameraOverride.Auto ? _enabledColor : _mutedColor;
                string autoLabel = CKLocalization.L("ck_auto", "Auto");
                if (GUILayout.Button(camOverride == CameraOverride.Auto ? $"● {autoLabel}" : $"○ {autoLabel}", _labelStyle, GUILayout.Width(55)))
                {
                    camOverride = CameraOverride.Auto;
                }
                
                // First Person Only button
                GUI.color = camOverride == CameraOverride.FirstPersonOnly ? _cameraColor : _mutedColor;
                string fpLabel = CKLocalization.L("ck_fp", "FP");
                if (GUILayout.Button(camOverride == CameraOverride.FirstPersonOnly ? $"● {fpLabel}" : $"○ {fpLabel}", _labelStyle, GUILayout.Width(40)))
                {
                    camOverride = CameraOverride.FirstPersonOnly;
                }
                
                // Projectile Only button
                GUI.color = camOverride == CameraOverride.ProjectileOnly ? _warningColor : _mutedColor;
                string projLabel = CKLocalization.L("ck_proj", "Proj");
                if (GUILayout.Button(camOverride == CameraOverride.ProjectileOnly ? $"● {projLabel}" : $"○ {projLabel}", _labelStyle, GUILayout.Width(50)))
                {
                    camOverride = CameraOverride.ProjectileOnly;
                }
                
                GUI.color = oldColor;
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
        }

        /// <summary>
        /// 3-way toggle for FOV mode: Off → Zoom In → Zoom Out
        /// </summary>
        private FOVMode FOVModeToggle(string label, FOVMode mode)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(150));
            
            // Off button
            Color oldColor = GUI.color;
            string offLabel = CKLocalization.L("ck_off", "Off");
            GUI.color = mode == FOVMode.Off ? _mutedColor : _mutedColor;
            if (GUILayout.Button(mode == FOVMode.Off ? $"● {offLabel}" : $"○ {offLabel}", _labelStyle, GUILayout.Width(60)))
            {
                mode = FOVMode.Off;
            }
            
            // Zoom In button
            string inLabel = CKLocalization.L("ck_in", "In");
            GUI.color = mode == FOVMode.ZoomIn ? _enabledColor : _mutedColor;
            if (GUILayout.Button(mode == FOVMode.ZoomIn ? $"● {inLabel}" : $"○ {inLabel}", _labelStyle, GUILayout.Width(50)))
            {
                mode = FOVMode.ZoomIn;
            }
            
            // Zoom Out button
            string outLabel = CKLocalization.L("ck_out", "Out");
            GUI.color = mode == FOVMode.ZoomOut ? _cameraColor : _mutedColor;
            if (GUILayout.Button(mode == FOVMode.ZoomOut ? $"● {outLabel}" : $"○ {outLabel}", _labelStyle, GUILayout.Width(55)))
            {
                mode = FOVMode.ZoomOut;
            }
            
            GUI.color = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            return mode;
        }

        private void DrawFovTimingRows(string idPrefix, FOVMode mode, ref float inDuration, ref float holdDuration, ref float outDuration, float cinematicDuration)
        {
            // Show main zoom phase + hold always, Return slider only in advanced mode
            bool advancedTiming = _settings != null && _settings.EnableAdvancedFOVTiming;
            
            if (mode == FOVMode.ZoomOut)
            {
                // For ZoomOut: camera zooms OUT first, then holds, then returns (zooms in)
                outDuration = SliderRow($"{idPrefix}_out", "  Zoom Out", outDuration, 0.05f, 0.5f, "{0:0.00}s");
                holdDuration = SliderRow($"{idPrefix}_hold", "  Hold", holdDuration, 0.1f, 2f, "{0:0.00}s");
                if (advancedTiming)
                {
                    inDuration = SliderRow($"{idPrefix}_in", "  Return (In)", inDuration, 0.05f, 0.5f, "{0:0.00}s");
                }
                // Note: inDuration is NOT modified when advanced is disabled - preserves saved value
            }
            else
            {
                // For ZoomIn: camera zooms IN first, then holds, then returns (zooms out)
                inDuration = SliderRow($"{idPrefix}_in", "  Zoom In", inDuration, 0.05f, 0.5f, "{0:0.00}s");
                holdDuration = SliderRow($"{idPrefix}_hold", "  Hold", holdDuration, 0.1f, 2f, "{0:0.00}s");
                if (advancedTiming)
                {
                    outDuration = SliderRow($"{idPrefix}_out", "  Return (Out)", outDuration, 0.05f, 0.5f, "{0:0.00}s");
                }
                // Note: outDuration is NOT modified when advanced is disabled - preserves saved value
            }

            float total = inDuration + holdDuration + outDuration;
            Color warnColor = total > cinematicDuration ? TimingWarningColor : TimingOkColor;
            Color oldColor = GUI.color;
            GUI.color = warnColor;
            GUILayout.Label($"  Total: {total:0.00}s | Duration: {cinematicDuration:0.0}s", _labelStyle);
            GUI.color = oldColor;
        }

        #endregion UI Helpers

        #region Footer

        private void DrawFooter(float footerY)
        {
            if (_resetFlashTime > 0) _resetFlashTime -= Time.unscaledDeltaTime;

            float buttonWidth = 130;
            float buttonHeight = 40;
            float spacing = 20;
            float totalWidth = buttonWidth + 200 + spacing; // Reset button + indicator
            float startX = (_windowRect.width - totalWidth) / 2;

            Color oldBg = GUI.backgroundColor;

            // Auto-save indicator (changes are live)
            Rect indicatorRect = new Rect(startX, footerY + 8, 200, 24);
            Color oldColor = GUI.color;
            GUI.color = new Color(0.5f, 0.8f, 0.5f);
            GUI.Label(indicatorRect, L("ck_ui_changes_apply_instantly", "● Changes apply instantly"), _labelStyle);
            GUI.color = oldColor;

            // Reset button
            Rect resetRect = new Rect(startX + 200 + spacing, footerY, buttonWidth, buttonHeight);
            if (_resetFlashTime > 0)
            {
                GUI.backgroundColor = new Color(0.2f, 0.9f, 0.2f);
                GUI.Button(resetRect, CKLocalization.L("ck_reset_done", "✓ RESET!"), _buttonStyle);
            }
            else
            {
                GUI.backgroundColor = _resetColor;
                if (GUI.Button(resetRect, CKLocalization.L("ck_reset_button", "🔄 RESET"), _buttonStyle))
                {
                    ResetToDefaults();
                    _resetFlashTime = FLASH_DURATION;
                }
            }

            GUI.backgroundColor = oldBg;
        }

        // ─────────────────────────────────────────────────────────────
        //  UI Helpers
        // ─────────────────────────────────────────────────────────────
        private void SectionTitle(string title)
        {
            GUILayout.Space(10);
            // Use blue color and add dashes for consistency
            Color oldColor = GUI.contentColor;
            GUI.contentColor = _cameraColor;
            GUILayout.Label($"─ {title} ─", _sectionTitleStyle);
            GUI.contentColor = oldColor;
            GUILayout.Space(5);
        }

        /// <summary>
        /// Displays a grayed-out disabled message label
        /// </summary>
        private void DisabledLabel(string message)
        {
            Color oldColor = GUI.color;
            GUI.color = DisabledTextColor;
            GUILayout.Label(message, _labelStyle);
            GUI.color = oldColor;
        }

        private bool ToggleRow(string label, bool value)
        {
            GUILayout.BeginHorizontal();
            
            // Text toggle style - colored text instead of button
            Color oldColor = GUI.color;
            GUI.color = value ? _enabledColor : _mutedColor;
            
            string displayText = value ? $"● {label}" : $"○ {label}";
            if (GUILayout.Button(displayText, _labelStyle, GUILayout.Width(250), GUILayout.Height(24)))
            {
                value = !value;
            }
            
            GUI.color = oldColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            return value;
        }

        private bool ToggleCompact(string label, bool value)
        {
            // Text toggle style - colored to match ON/OFF state (like ToggleRow)
            Color oldColor = GUI.color;
            
            // Use same colors as ToggleRow: green when ON, muted grey when OFF
            Color toggleColor = value ? _enabledColor : _mutedColor;
            
            // Draw entire toggle (indicator + label) in the state color
            GUI.color = toggleColor;
            string indicator = value ? "●" : "○";
            
            if (GUILayout.Button($"{indicator} {label}", _labelStyle, GUILayout.ExpandWidth(false)))
            {
                value = !value;
            }
            
            GUI.color = oldColor;
            return value;
        }

        private float SliderRow(string label, float value, float min, float max, string format)
        {
            // Auto-generate unique ID based on label
            return SliderRow(label, label, value, min, max, format);
        }
        
        /// <summary>
        /// Slider row with explicit unique ID to avoid conflicts when multiple sliders have same label
        /// </summary>
        private float SliderRow(string uniqueId, string label, float value, float min, float max, string format)
        {
            string inputId = "slider_" + uniqueId.GetHashCode().ToString();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(160));

            float sliderValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(180));

            GUILayout.Space(8);
            
            // Determine format for text display
            bool hasDecimal = format.Contains("F") || format.Contains("0.") || format.Contains("#.");
            string formatSpec = hasDecimal ? "F1" : "F0";
            
            // Get the expected text for the current slider value
            string expectedText = sliderValue.ToString(formatSpec);
            
            // Get stored text, default to expected text if not stored
            string storedText = _sliderInputs.ContainsKey(inputId) ? _sliderInputs[inputId] : expectedText;
            
            // Display text field
            string newText = GUILayout.TextField(storedText, GUILayout.Width(50));
            _sliderInputs[inputId] = newText;
            
            // Determine final value: use slider unless user typed a different valid value
            float finalValue = sliderValue;
            
            if (newText != storedText || newText != expectedText)
            {
                // User typed something - try to parse it
                if (float.TryParse(newText, out float parsed))
                {
                    finalValue = Mathf.Clamp(parsed, min, max);
                }
            }
            
            // If slider was moved (value changed from input), update the stored text
            if (Mathf.Abs(sliderValue - value) > 0.001f)
            {
                _sliderInputs[inputId] = sliderValue.ToString(formatSpec);
                finalValue = sliderValue;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            return finalValue;
        }

        /// <summary>
        /// Slider with editable text input for precise value entry
        /// </summary>
        private float SliderRowWithInput(string label, float value, float min, float max, string format, string inputId)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(140));

            float sliderValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(160));

            GUILayout.Space(8);
            
            // Get the expected text for current slider value
            string expectedText = sliderValue.ToString("F1");
            string storedText = _sliderInputs.ContainsKey(inputId) ? _sliderInputs[inputId] : expectedText;
            
            // Display text field
            string newText = GUILayout.TextField(storedText, GUILayout.Width(50));
            _sliderInputs[inputId] = newText;
            
            // Determine final value: use slider unless user typed a different valid value
            float finalValue = sliderValue;
            
            if (newText != storedText || newText != expectedText)
            {
                // User typed something - try to parse it
                if (float.TryParse(newText, out float parsed))
                {
                    finalValue = Mathf.Clamp(parsed, min, max);
                }
            }
            
            // If slider was moved, update stored text and use slider value
            if (Mathf.Abs(sliderValue - value) > 0.001f)
            {
                _sliderInputs[inputId] = sliderValue.ToString("F1");
                finalValue = sliderValue;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            return finalValue;
        }
        
        /// <summary>
        /// Compact inline slider with input box - for use within existing horizontal layouts.
        /// Does not create its own horizontal layout or label.
        /// </summary>
        private float InlineSliderWithInput(string uniqueId, float value, float min, float max, int sliderWidth = 80, int inputWidth = 45, string suffix = "")
        {
            string inputId = "inline_" + uniqueId.GetHashCode().ToString();
            
            float sliderValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(sliderWidth));
            GUILayout.Space(5);
            
            // Format based on range
            string formatSpec = (max - min) <= 10f ? "F1" : "F0";
            
            // Get expected and stored text
            string expectedText = sliderValue.ToString(formatSpec);
            string storedText = _sliderInputs.ContainsKey(inputId) ? _sliderInputs[inputId] : expectedText;
            
            // Display text field
            string newText = GUILayout.TextField(storedText, GUILayout.Width(inputWidth));
            _sliderInputs[inputId] = newText;
            
            // Show suffix if provided
            if (!string.IsNullOrEmpty(suffix))
            {
                GUILayout.Label(suffix, _valueStyle, GUILayout.Width(20));
            }
            
            // Determine final value
            float finalValue = sliderValue;
            
            if (newText != storedText || newText != expectedText)
            {
                if (float.TryParse(newText, out float parsed))
                {
                    finalValue = Mathf.Clamp(parsed, min, max);
                }
            }
            
            // If slider was moved, update stored text
            if (Mathf.Abs(sliderValue - value) > 0.001f)
            {
                _sliderInputs[inputId] = sliderValue.ToString(formatSpec);
                finalValue = sliderValue;
            }
            
            return finalValue;
        }
        
        // Storage for text input values
        private Dictionary<string, string> _sliderInputs = new Dictionary<string, string>();
        
        /// <summary>
        /// Toggle + Slider + Input in one row. Returns (enabled, value) tuple.
        /// </summary>
        private (bool enabled, float value) ToggleSliderRow(string label, bool enabled, float value, float min, float max, string suffix = "")
        {
            string inputId = "toggle_slider_" + label.GetHashCode().ToString();
            
            GUILayout.BeginHorizontal();
            bool newEnabled = GUILayout.Toggle(enabled, " " + label, GUILayout.Width(130));
            
            float finalValue = value;
            if (newEnabled)
            {
                float sliderValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(120));
                
                // Determine format
                bool hasDecimal = max <= 1f || min < 0f;
                string formatSpec = hasDecimal ? "F2" : "F0";
                
                // Get expected and stored text
                string expectedText = sliderValue.ToString(formatSpec);
                string storedText = _sliderInputs.ContainsKey(inputId) ? _sliderInputs[inputId] : expectedText;
                
                // Display text field
                string newText = GUILayout.TextField(storedText, GUILayout.Width(45));
                _sliderInputs[inputId] = newText;
                
                // Default to slider value
                finalValue = sliderValue;
                
                // Only use parsed text if user typed something different
                if (newText != storedText || newText != expectedText)
                {
                    if (float.TryParse(newText, out float parsed))
                    {
                        finalValue = Mathf.Clamp(parsed, min, max);
                    }
                }
                
                if (!string.IsNullOrEmpty(suffix))
                {
                    GUILayout.Label(suffix, _labelStyle, GUILayout.Width(50));
                }
                
                // If slider was moved, update stored text and use slider value
                if (Mathf.Abs(sliderValue - value) > 0.001f)
                {
                    _sliderInputs[inputId] = sliderValue.ToString(formatSpec);
                    finalValue = sliderValue;
                }
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            return (newEnabled, finalValue);
        }

        /// <summary>
        /// Side Offset Level with multi-select toggles (Wide/Standard/Tight) and ± master toggle
        /// </summary>
        private void DrawSideOffsetControl()
        {
            GUILayout.BeginHorizontal();
            
            // Toggle for randomization (master enable)
            _settings.ProjectileCamera.RandomizeSideOffset = GUILayout.Toggle(
                _settings.ProjectileCamera.RandomizeSideOffset, 
                "±", 
                GUILayout.Width(30));
            
            GUILayout.Label(CKLocalization.L("ck_side_offset", "Side Offset"), _labelStyle, GUILayout.Width(80));
            
            // Show toggles if enabled
            if (_settings.ProjectileCamera.RandomizeSideOffset)
            {
                Color oldColor = GUI.color;
                
                // Wide toggle
                bool wideEnabled = _settings.ProjectileCamera.SideOffsetWide;
                GUI.color = wideEnabled ? _cameraColor : _mutedColor;
                if (GUILayout.Button(wideEnabled ? "● " + L("ck_ui_wide", "Wide") : "○ " + L("ck_ui_wide", "Wide"), _labelStyle, GUILayout.Width(55)))
                {
                    // Toggle, but ensure at least one is enabled
                    if (wideEnabled && CountEnabledOffsets() > 1)
                        _settings.ProjectileCamera.SideOffsetWide = false;
                    else if (!wideEnabled)
                        _settings.ProjectileCamera.SideOffsetWide = true;
                }
                
                GUILayout.Space(5);
                
                // Standard toggle
                bool standardEnabled = _settings.ProjectileCamera.SideOffsetStandard;
                GUI.color = standardEnabled ? _enabledColor : _mutedColor;
                if (GUILayout.Button(standardEnabled ? "● " + L("ck_ui_std", "Std") : "○ " + L("ck_ui_std", "Std"), _labelStyle, GUILayout.Width(50)))
                {
                    if (standardEnabled && CountEnabledOffsets() > 1)
                        _settings.ProjectileCamera.SideOffsetStandard = false;
                    else if (!standardEnabled)
                        _settings.ProjectileCamera.SideOffsetStandard = true;
                }
                
                GUILayout.Space(5);
                
                // Tight toggle
                bool tightEnabled = _settings.ProjectileCamera.SideOffsetTight;
                GUI.color = tightEnabled ? _warningColor : _mutedColor;
                if (GUILayout.Button(tightEnabled ? "● " + L("ck_ui_tight", "Tight") : "○ " + L("ck_ui_tight", "Tight"), _labelStyle, GUILayout.Width(55)))
                {
                    if (tightEnabled && CountEnabledOffsets() > 1)
                        _settings.ProjectileCamera.SideOffsetTight = false;
                    else if (!tightEnabled)
                        _settings.ProjectileCamera.SideOffsetTight = true;
                }
                
                GUILayout.Space(10);
                
                // Show summary of enabled ranges
                GUI.color = _mutedColor;
                string summary = BuildOffsetSummary();
                GUILayout.Label(summary, _labelStyle);
                
                GUI.color = oldColor;
            }
            else
            {
                Color oldColor = GUI.color;
                GUI.color = _mutedColor;
                GUILayout.Label(CKLocalization.L("ck_side_offset_disabled", "Disabled (no offset)"), _labelStyle);
                GUI.color = oldColor;
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        
        private int CountEnabledOffsets()
        {
            int count = 0;
            if (_settings.ProjectileCamera.SideOffsetWide) count++;
            if (_settings.ProjectileCamera.SideOffsetStandard) count++;
            if (_settings.ProjectileCamera.SideOffsetTight) count++;
            return count;
        }
        
        private string BuildOffsetSummary()
        {
            var parts = new System.Collections.Generic.List<string>();
            if (_settings.ProjectileCamera.SideOffsetWide) parts.Add("±4m");
            if (_settings.ProjectileCamera.SideOffsetStandard) parts.Add("±2m");
            if (_settings.ProjectileCamera.SideOffsetTight) parts.Add("±1m");
            return string.Join(", ", parts);
        }

        /// <summary>
        /// Randomize Tilt control with ± toggle and input box
        /// </summary>
        private void DrawRandomizeTiltControl()
        {
            GUILayout.BeginHorizontal();
            
            // Toggle for randomization
            _settings.ProjectileCamera.RandomizeTilt = GUILayout.Toggle(
                _settings.ProjectileCamera.RandomizeTilt, 
                "±", 
                GUILayout.Width(30));
            
            GUILayout.Label(CKLocalization.L("ck_tilt", "Tilt"), _labelStyle, GUILayout.Width(100));
            
            if (_settings.ProjectileCamera.RandomizeTilt)
            {
                // Current value before slider
                float prevValue = _settings.ProjectileCamera.RandomTiltRange;
                
                // Slider for range
                float sliderValue = GUILayout.HorizontalSlider(
                    _settings.ProjectileCamera.RandomTiltRange, 1f, 45f, GUILayout.Width(100));
                
                GUILayout.Space(8);
                
                // Input box
                string inputId = "tilt_range";
                string expectedText = sliderValue.ToString("F0");
                string storedText = _sliderInputs.ContainsKey(inputId) ? _sliderInputs[inputId] : expectedText;
                
                string newText = GUILayout.TextField(storedText, GUILayout.Width(40));
                _sliderInputs[inputId] = newText;
                
                // Determine final value: slider takes priority if it moved
                float finalValue = sliderValue;
                
                if (Mathf.Abs(sliderValue - prevValue) > 0.01f)
                {
                    // Slider was moved - use slider value and update text
                    finalValue = sliderValue;
                    _sliderInputs[inputId] = sliderValue.ToString("F0");
                }
                else if (newText != storedText)
                {
                    // User typed something different - try to parse it
                    if (float.TryParse(newText, out float parsed))
                    {
                        finalValue = Mathf.Clamp(parsed, 1f, 45f);
                    }
                }
                
                _settings.ProjectileCamera.RandomTiltRange = finalValue;
                
                GUILayout.Label("°", _valueStyle, GUILayout.Width(15));
            }
            else
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUILayout.Label(CKLocalization.L("ck_tilt_disabled", "Disabled (no tilt)"), _labelStyle);
                GUI.color = oldColor;
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Randomizable slider - click label to toggle between fixed value and min/max range
        /// Returns the value (or min value when in randomize mode)
        /// </summary>
        /// <param name="sliderId">Unique ID for this slider's randomize state</param>
        /// <param name="label">Label text (clickable to toggle randomize)</param>
        /// <param name="value">Current value (also used as single-value when not randomizing)</param>
        /// <param name="minVal">Reference for min slider value when randomizing</param>
        /// <param name="maxVal">Reference for max slider value when randomizing</param>
        /// <param name="sliderMin">Absolute minimum of slider range</param>
        /// <param name="sliderMax">Absolute maximum of slider range</param>
        /// <param name="format">Format string for display</param>
        /// <param name="isRandomizing">Reference to the randomize toggle state</param>
        /// <returns>The value (single when not randomizing, min when randomizing)</returns>
        private float RandomizableSliderRow(string sliderId, string label, float value, ref float minVal, ref float maxVal, 
            float sliderMin, float sliderMax, string format, ref bool isRandomizing)
        {
            GUILayout.BeginHorizontal();
            
            // Clickable label to toggle randomize mode
            Color oldColor = GUI.color;
            GUI.color = isRandomizing ? _effectColor : Color.white; // Purple when randomizing
            
            string displayLabel = isRandomizing ? $"🎲 {label}" : label;
            if (GUILayout.Button(displayLabel, _labelStyle, GUILayout.Width(160)))
            {
                isRandomizing = !isRandomizing;
                if (isRandomizing)
                {
                    // Initialize min/max based on current value
                    minVal = Mathf.Max(sliderMin, value * 0.7f);
                    maxVal = Mathf.Min(sliderMax, value * 1.3f);
                }
            }
            GUI.color = oldColor;
            
            if (!isRandomizing)
            {
                // Normal single-value slider with input box
                string inputId = "rand_" + sliderId;
                float sliderValue = GUILayout.HorizontalSlider(value, sliderMin, sliderMax, GUILayout.Width(160));
                GUILayout.Space(6);
                
                // Determine format
                bool hasDecimal = format.Contains("F") || format.Contains("0.") || format.Contains("#.");
                string formatSpec = hasDecimal ? "F2" : "F0";
                string expectedText = sliderValue.ToString(formatSpec);
                string storedText = _sliderInputs.ContainsKey(inputId) ? _sliderInputs[inputId] : expectedText;
                
                string newText = GUILayout.TextField(storedText, GUILayout.Width(50));
                _sliderInputs[inputId] = newText;
                
                float finalValue = sliderValue;
                if (newText != storedText || newText != expectedText)
                {
                    if (float.TryParse(newText, out float parsed))
                    {
                        finalValue = Mathf.Clamp(parsed, sliderMin, sliderMax);
                    }
                }
                if (Mathf.Abs(sliderValue - value) > 0.001f)
                {
                    _sliderInputs[inputId] = sliderValue.ToString(formatSpec);
                    finalValue = sliderValue;
                }
                
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return finalValue;
            }
            else
            {
                // Randomize mode - show min/max with input boxes
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                bool hasDecimal = format.Contains("F") || format.Contains("0.") || format.Contains("#.");
                string formatSpec = hasDecimal ? "F2" : "F0";
                
                // Min slider with input
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L("ck_ui_min", "  Min:"), _labelStyle, GUILayout.Width(50));
                
                string minInputId = "rand_" + sliderId + "_min";
                float minSliderVal = GUILayout.HorizontalSlider(minVal, sliderMin, maxVal, GUILayout.Width(120));
                string minExpected = minSliderVal.ToString(formatSpec);
                string minStored = _sliderInputs.ContainsKey(minInputId) ? _sliderInputs[minInputId] : minExpected;
                string minNewText = GUILayout.TextField(minStored, GUILayout.Width(45));
                _sliderInputs[minInputId] = minNewText;
                
                float minFinal = minSliderVal;
                if (minNewText != minStored || minNewText != minExpected)
                {
                    if (float.TryParse(minNewText, out float parsed)) minFinal = Mathf.Clamp(parsed, sliderMin, maxVal);
                }
                if (Mathf.Abs(minSliderVal - minVal) > 0.001f)
                {
                    _sliderInputs[minInputId] = minSliderVal.ToString(formatSpec);
                    minFinal = minSliderVal;
                }
                minVal = minFinal;
                
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                // Max slider with input
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L("ck_ui_max", "  Max:"), _labelStyle, GUILayout.Width(50));
                
                string maxInputId = "rand_" + sliderId + "_max";
                float maxSliderVal = GUILayout.HorizontalSlider(maxVal, minVal, sliderMax, GUILayout.Width(120));
                string maxExpected = maxSliderVal.ToString(formatSpec);
                string maxStored = _sliderInputs.ContainsKey(maxInputId) ? _sliderInputs[maxInputId] : maxExpected;
                string maxNewText = GUILayout.TextField(maxStored, GUILayout.Width(45));
                _sliderInputs[maxInputId] = maxNewText;
                
                float maxFinal = maxSliderVal;
                if (maxNewText != maxStored || maxNewText != maxExpected)
                {
                    if (float.TryParse(maxNewText, out float parsed)) maxFinal = Mathf.Clamp(parsed, minVal, sliderMax);
                }
                if (Mathf.Abs(maxSliderVal - maxVal) > 0.001f)
                {
                    _sliderInputs[maxInputId] = maxSliderVal.ToString(formatSpec);
                    maxFinal = maxSliderVal;
                }
                maxVal = maxFinal;
                
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                // Return mid value as the "reference" value  
                return (minVal + maxVal) / 2f;
            }
        }

        /// <summary>
        /// Draws linked First Person / Projectile chance sliders that always equal 100%
        /// Includes input boxes for precise value entry
        /// </summary>
        private void DrawLinkedCameraChance(string id, ref float firstPersonChance)
        {
            string fpInputId = "linked_fp_" + id.GetHashCode().ToString();
            string projInputId = "linked_proj_" + id.GetHashCode().ToString();
            
            // First Person slider
            GUILayout.BeginHorizontal();
            GUILayout.Label(CKLocalization.L("ck_first_person_percent", "First Person %"), _labelStyle, GUILayout.Width(120));
            float newFP = GUILayout.HorizontalSlider(firstPersonChance, 0f, 100f, GUILayout.Width(150));
            GUILayout.Space(5);
            
            // FP Input box
            string fpExpected = newFP.ToString("F0");
            string fpStored = _sliderInputs.ContainsKey(fpInputId) ? _sliderInputs[fpInputId] : fpExpected;
            string fpNewText = GUILayout.TextField(fpStored, GUILayout.Width(40));
            _sliderInputs[fpInputId] = fpNewText;
            GUILayout.Label("%", _valueStyle, GUILayout.Width(20));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            // Projectile slider (linked - always 100 - FP)
            float projChance = 100f - newFP;
            GUILayout.BeginHorizontal();
            GUILayout.Label(CKLocalization.L("ck_projectile_percent", "Projectile %"), _labelStyle, GUILayout.Width(120));
            float newProj = GUILayout.HorizontalSlider(projChance, 0f, 100f, GUILayout.Width(150));
            GUILayout.Space(5);
            
            // Proj Input box
            string projExpected = newProj.ToString("F0");
            string projStored = _sliderInputs.ContainsKey(projInputId) ? _sliderInputs[projInputId] : projExpected;
            string projNewText = GUILayout.TextField(projStored, GUILayout.Width(40));
            _sliderInputs[projInputId] = projNewText;
            GUILayout.Label("%", _valueStyle, GUILayout.Width(20));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            // Determine final FP value based on which control was changed
            float finalFP = firstPersonChance;
            
            // Check if FP slider was moved
            if (Mathf.Abs(newFP - firstPersonChance) > 0.01f)
            {
                finalFP = newFP;
                _sliderInputs[fpInputId] = newFP.ToString("F0");
                _sliderInputs[projInputId] = (100f - newFP).ToString("F0");
            }
            // Check if Proj slider was moved
            else if (Mathf.Abs(newProj - projChance) > 0.01f)
            {
                finalFP = 100f - newProj;
                _sliderInputs[fpInputId] = finalFP.ToString("F0");
                _sliderInputs[projInputId] = newProj.ToString("F0");
            }
            // Check if FP input was changed
            else if (fpNewText != fpStored && float.TryParse(fpNewText, out float parsedFP))
            {
                finalFP = Mathf.Clamp(parsedFP, 0f, 100f);
                _sliderInputs[projInputId] = (100f - finalFP).ToString("F0");
            }
            // Check if Proj input was changed  
            else if (projNewText != projStored && float.TryParse(projNewText, out float parsedProj))
            {
                finalFP = 100f - Mathf.Clamp(parsedProj, 0f, 100f);
                _sliderInputs[fpInputId] = finalFP.ToString("F0");
            }
            
            firstPersonChance = finalFP;
        }

        // ═══════════════════════════════════════════════════════════════
        //  TAB 6: ADVANCED - Developer Tools & Import/Export
        // ═══════════════════════════════════════════════════════════════
        private string[] _availableBackups = null;
        private int _selectedBackupIndex = -1;
        private string _exportName = "";
        
        private void DrawAdvancedTab()
        {
            // ═══════════════════════════════════════════════════════════════
            // IMPORT / EXPORT - Two column layout
            // ═══════════════════════════════════════════════════════════════
            SectionTitle(CKLocalization.L("ck_section_import_export", "IMPORT / EXPORT SETTINGS"));
            GUILayout.Space(5);
            
            // Backup directory location
            string backupDir = Path.Combine(Application.persistentDataPath, "CinematicKill", "Backups");
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label(L("ck_ui_backup_folder", "Backup folder:") + $" {backupDir}", _labelStyle);
            GUI.color = Color.white;
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            
            // LEFT COLUMN - Export
            GUILayout.BeginVertical(GUILayout.Width(450));
            GUILayout.Space(8);
            ColoredSectionHeader(L("ck_ui_export_2", "─ EXPORT ─"), _cameraColor);
            GUILayout.Space(5);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(L("ck_ui_name", "Name:"), _labelStyle, GUILayout.Width(50));
            _exportName = GUILayout.TextField(_exportName, GUILayout.Width(240));
            GUILayout.Space(10);
            
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = _exportColor;
            if (GUILayout.Button(L("ck_ui_export", "Export"), GUILayout.Width(70)))
            {
                ExportSettingsWithName(_exportName);
                _exportName = "";
                RefreshBackupList();
            }
            GUI.backgroundColor = oldBg;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUI.color = _mutedColor;
            GUILayout.Label(L("ck_ui_leave_empty_for_timestamp_based_name", "  Leave empty for timestamp-based name"), _labelStyle);
            GUI.color = Color.white;
            
            GUILayout.Space(8);
            GUILayout.EndVertical();
            
            GUILayout.Space(15);
            
            // RIGHT COLUMN - Import
            GUILayout.BeginVertical(GUILayout.Width(500));
            GUILayout.Space(8);
            
            GUILayout.BeginHorizontal();
            ColoredSectionHeader(L("ck_ui_import", "─ IMPORT ─"), _cameraColor);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(L("ck_ui_refresh", "Refresh"), GUILayout.Width(70)))
            {
                RefreshBackupList();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            
            // Initialize backup list if needed
            if (_availableBackups == null)
            {
                RefreshBackupList();
            }
            
            if (_availableBackups.Length == 0)
            {
                GUILayout.Label(L("ck_ui_no_backups_available", "No backups available"), _labelStyle);
            }
            else
            {
                // Compact backup list (max 4 visible at a time)
                int maxVisible = Mathf.Min(4, _availableBackups.Length);
                int backupToDelete = -1;
                
                for (int i = 0; i < maxVisible; i++)
                {
                    GUILayout.BeginHorizontal();
                    
                    bool isSelected = _selectedBackupIndex == i;
                    oldBg = GUI.backgroundColor;
                    GUI.backgroundColor = isSelected ? _toggleOnColor : new Color(0.3f, 0.3f, 0.35f);
                    
                    if (GUILayout.Button(_availableBackups[i], GUILayout.Width(380), GUILayout.Height(22)))
                    {
                        _selectedBackupIndex = i;
                    }
                    GUI.backgroundColor = oldBg;
                    
                    // Delete button
                    GUI.backgroundColor = new Color(0.6f, 0.25f, 0.25f);
                    if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(22)))
                    {
                        backupToDelete = i;
                    }
                    GUI.backgroundColor = oldBg;
                    
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                
                if (_availableBackups.Length > maxVisible)
                {
                    GUILayout.Label($"  +{_availableBackups.Length - maxVisible} more backups...", _labelStyle);
                }
                
                // Handle delete outside of loop
                if (backupToDelete >= 0)
                {
                    DeleteBackup(backupToDelete);
                }
                
                GUILayout.Space(5);
                
                // Import button
                GUILayout.BeginHorizontal();
                bool hasSelection = _selectedBackupIndex >= 0 && _selectedBackupIndex < _availableBackups.Length;
                oldBg = GUI.backgroundColor;
                GUI.backgroundColor = hasSelection ? _importColor : new Color(0.3f, 0.3f, 0.3f);
                
                if (GUILayout.Button(L("ck_ui_import_selected", "Import Selected"), GUILayout.Width(120)))
                {
                    if (hasSelection)
                    {
                        ImportSettingsFromBackup(_selectedBackupIndex);
                    }
                }
                GUI.backgroundColor = oldBg;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(8);
            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(20);
            
            // ═══════════════════════════════════════════════════════════════
            // DEVELOPER OPTIONS - Compact
            // ═══════════════════════════════════════════════════════════════
            SectionTitle(CKLocalization.L("ck_section_developer", "DEVELOPER OPTIONS"));
            GUILayout.Space(5);
            
            GUILayout.BeginHorizontal();
            _settings.EnableVerboseLogging = ToggleRow(
                CKLocalization.L("ck_verbose_logging", "Enable Verbose Logging"), 
                _settings.EnableVerboseLogging);
            GUILayout.Space(20);
            Color tipColor = GUI.color;
            GUI.color = new Color(0.6f, 0.75f, 0.9f);
            GUILayout.Label(L("ck_ui_detailed_debug_messages_to_conso_dcbfef", "Detailed debug messages to console (F1)"), _labelStyle);
            GUI.color = tipColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(15);
            
            // ═══════════════════════════════════════════════════════════════
            // DEBUG STATUS - Compact info bar
            // ═══════════════════════════════════════════════════════════════
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            ColoredSectionHeader(L("ck_ui_status", "─ STATUS ─"), _cameraColor);
            
            Color statusColor = CinematicKillManager.IsActive ? new Color(0.4f, 1f, 0.4f) : new Color(0.5f, 0.5f, 0.5f);
            GUI.color = statusColor;
            GUILayout.Label(L("ck_ui_active", "Active:") + $" {(CinematicKillManager.IsActive ? L("ck_ui_yes", "YES") : L("ck_ui_no", "NO"))}", _labelStyle, GUILayout.Width(100));
            
            GUI.color = new Color(0.7f, 0.85f, 0.7f);
            GUILayout.Label(L("ck_ui_last_trigger", "Last Trigger:") + $" {CKLocalization.LocalizeTriggerReason(CinematicKillManager.LastTriggerReason)}", _labelStyle, GUILayout.Width(250));
            
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.EndVertical();
        }

        // ═══════════════════════════════════════════════════════════════
        //  TAB 7: EXPERIMENTAL - Beta Features
        // ═══════════════════════════════════════════════════════════════
        private void DrawExperimentalTab()
        {
            var exp = _settings.Experimental;
            
            // ═══════════════════════════════════════════════════════════════
            // WARNING BANNER
            // ═══════════════════════════════════════════════════════════════
            Color warnBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 0.4f, 0.1f);
            GUILayout.BeginVertical(GUI.skin.box);
            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 0.85f, 0.4f);
            GUILayout.Label(L("ck_ui_experimental_features", "⚠ EXPERIMENTAL FEATURES"), _sectionTitleStyle);
            GUILayout.Label(L("ck_ui_these_features_are_in_developmen_500999", "These features are in development and may cause unexpected behavior."), _labelStyle);
            GUILayout.Label(L("ck_ui_use_at_your_own_risk_disabled_by_e823ac", "Use at your own risk. Disabled by default."), _labelStyle);
            GUI.color = oldColor;
            GUILayout.EndVertical();
            GUI.backgroundColor = warnBg;
            
            GUILayout.Space(15);
            
            // ═══════════════════════════════════════════════════════════════
            // X-RAY VISION
            // ═══════════════════════════════════════════════════════════════
            SectionTitle(L("ck_ui_x_ray_vision", "☢ X-RAY VISION"));
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label(L("ck_ui_applies_a_high_contrast_screen_e_663e5a", "Applies a high-contrast screen effect during dismemberment kills."), _labelStyle);
            GUI.color = Color.white;
            GUILayout.Space(5);
            
            exp.EnableXRayVision = ToggleRow(L("ck_ui_enable_x_ray_vision", "Enable X-Ray Vision"), exp.EnableXRayVision);
            
            if (exp.EnableXRayVision)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L("ck_duration_label", "Duration"), _labelStyle, GUILayout.Width(120));
                exp.XRayDuration = GUILayout.HorizontalSlider(exp.XRayDuration, 0.1f, 2.0f, GUILayout.Width(150));
                GUILayout.Label($"{exp.XRayDuration:F1}s", _valueStyle, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L("ck_ui_intensity", "Intensity"), _labelStyle, GUILayout.Width(120));
                exp.XRayIntensity = GUILayout.HorizontalSlider(exp.XRayIntensity, 0.5f, 2.0f, GUILayout.Width(150));
                GUILayout.Label($"{exp.XRayIntensity:F1}x", _valueStyle, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(15);
            
            // ═══════════════════════════════════════════════════════════════
            // PREDATOR VISION
            // ═══════════════════════════════════════════════════════════════
            SectionTitle(L("ck_ui_predator_vision", "👁 PREDATOR VISION"));
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label(L("ck_ui_applies_a_thermal_night_vision_e_2dfa41", "Applies a thermal/night vision effect during sneak kill cinematics."), _labelStyle);
            GUI.color = Color.white;
            GUILayout.Space(5);
            
            exp.EnablePredatorVision = ToggleRow(L("ck_ui_enable_predator_vision", "Enable Predator Vision"), exp.EnablePredatorVision);
            
            if (exp.EnablePredatorVision)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L("ck_duration_label", "Duration"), _labelStyle, GUILayout.Width(120));
                exp.PredatorVisionDuration = GUILayout.HorizontalSlider(exp.PredatorVisionDuration, 0.5f, 3.0f, GUILayout.Width(150));
                GUILayout.Label($"{exp.PredatorVisionDuration:F1}s", _valueStyle, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L("ck_ui_intensity", "Intensity"), _labelStyle, GUILayout.Width(120));
                exp.PredatorVisionIntensity = GUILayout.HorizontalSlider(exp.PredatorVisionIntensity, 0.3f, 1.0f, GUILayout.Width(150));
                GUILayout.Label($"{exp.PredatorVisionIntensity:P0}", _valueStyle, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            
            // ═══════════════════════════════════════════════════════════════
            // DISMEMBERMENT FOCUS CAM
            // ═══════════════════════════════════════════════════════════════
            SectionTitle(L("ck_ui_dismemberment_focus_cam", "💀 DISMEMBERMENT FOCUS CAM"));
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label(L("ck_ui_camera_follows_the_severed_limb_6ccfcb", "Camera follows the severed limb instead of the body."), _labelStyle);
            GUI.color = Color.white;
            GUILayout.Space(5);
            
            exp.EnableDismemberFocusCam = ToggleRow(L("ck_ui_enable_dismember_focus_cam", "Enable Dismember Focus Cam"), exp.EnableDismemberFocusCam);
            
            if (exp.EnableDismemberFocusCam)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L("ck_ui_distance", "Distance"), _labelStyle, GUILayout.Width(120));
                exp.FocusCamDistance = GUILayout.HorizontalSlider(exp.FocusCamDistance, 0.5f, 4.0f, GUILayout.Width(150));
                GUILayout.Label($"{exp.FocusCamDistance:F1}m", _valueStyle, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L("ck_duration_label", "Duration"), _labelStyle, GUILayout.Width(120));
                exp.FocusCamDuration = GUILayout.HorizontalSlider(exp.FocusCamDuration, 0.5f, 3.0f, GUILayout.Width(150));
                GUILayout.Label($"{exp.FocusCamDuration:F1}s", _valueStyle, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(15);
            
            // ═══════════════════════════════════════════════════════════════
            // LAST STAND / SECOND WIND
            // ═══════════════════════════════════════════════════════════════
            SectionTitle(L("ck_ui_last_stand_second_wind", "⚔ LAST STAND / SECOND WIND"));
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label(L("ck_ui_trigger_cinematic_when_player_is_1b815d", "Trigger cinematic when player is near death. Get a kill to survive."), _labelStyle);
            GUI.color = Color.white;
            GUILayout.Space(5);
            
            exp.EnableLastStand = ToggleRow(L("ck_ui_enable_last_stand", "Enable Last Stand"), exp.EnableLastStand);
            
            if (exp.EnableLastStand)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L("ck_duration_label", "Duration"), _labelStyle, GUILayout.Width(120));
                exp.LastStandDuration = GUILayout.HorizontalSlider(exp.LastStandDuration, 1f, 10f, GUILayout.Width(150));
                GUILayout.Label($"{exp.LastStandDuration:F1}s", _valueStyle, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L("ck_time_scale_label", "Time Scale"), _labelStyle, GUILayout.Width(120));
                exp.LastStandTimeScale = GUILayout.HorizontalSlider(exp.LastStandTimeScale, 0.05f, 0.3f, GUILayout.Width(150));
                GUILayout.Label($"{exp.LastStandTimeScale:F2}x", _valueStyle, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L("ck_ui_revive_health", "Revive Health"), _labelStyle, GUILayout.Width(120));
                exp.LastStandReviveHealth = GUILayout.HorizontalSlider(exp.LastStandReviveHealth, 10f, 50f, GUILayout.Width(150));
                GUILayout.Label($"{exp.LastStandReviveHealth:F0} HP", _valueStyle, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label(L("ck_cooldown", "Cooldown"), _labelStyle, GUILayout.Width(120));
                exp.LastStandCooldown = GUILayout.HorizontalSlider(exp.LastStandCooldown, 30f, 120f, GUILayout.Width(150));
                GUILayout.Label($"{exp.LastStandCooldown:F0}s", _valueStyle, GUILayout.Width(50));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                exp.LastStandInfiniteAmmo = GUILayout.Toggle(exp.LastStandInfiniteAmmo, " Infinite Ammo During Last Stand", _labelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(20);
            
            // Notes about features in other tabs
            GUI.color = new Color(0.6f, 0.75f, 0.9f);
            GUILayout.Label(L("ck_ui_note_freeze_frame_is_in_the_effe_1dba13", "Note: Freeze Frame is in the Effects tab. Projectile Ride and Chain Reaction are in the Camera tab."), _labelStyle);
            GUI.color = Color.white;
        }

        private void RefreshBackupList()
        {
            string backupDir = Path.Combine(Application.persistentDataPath, "CinematicKill", "Backups");
            if (!Directory.Exists(backupDir))
            {
                _availableBackups = new string[0];
                return;
            }
            
            string[] files = Directory.GetFiles(backupDir, "*.xml");
            System.Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
            
            _availableBackups = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                DateTime modified = File.GetLastWriteTime(files[i]);
                _availableBackups[i] = $"{name}  ({modified:yyyy-MM-dd HH:mm})";
            }
            _selectedBackupIndex = -1;
        }

        private void DeleteBackup(int index)
        {
            try
            {
                string backupDir = Path.Combine(Application.persistentDataPath, "CinematicKill", "Backups");
                string[] files = Directory.GetFiles(backupDir, "*.xml");
                System.Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                
                if (index < 0 || index >= files.Length) return;
                
                string fileToDelete = files[index];
                File.Delete(fileToDelete);
                CKLog.Out($"Backup deleted: {Path.GetFileName(fileToDelete)}");
                
                // Refresh list and reset selection
                RefreshBackupList();
            }
            catch (Exception ex)
            {
                CKLog.Error($"Delete backup failed: {ex.Message}");
            }
        }

        private void ExportSettingsWithName(string customName)
        {
            try
            {
                // First, save current settings to disk to ensure we export live values
                CinematicKillManager.SaveSettingsToFile();
                
                string backupDir = Path.Combine(Application.persistentDataPath, "CinematicKill", "Backups");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }
                
                string filename;
                if (string.IsNullOrWhiteSpace(customName))
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    filename = $"CinematicKillSettings_{timestamp}.xml";
                }
                else
                {
                    // Sanitize custom name
                    string sanitized = customName.Replace(" ", "_");
                    foreach (char c in Path.GetInvalidFileNameChars())
                    {
                        sanitized = sanitized.Replace(c.ToString(), "");
                    }
                    filename = $"{sanitized}.xml";
                }
                
                string backupPath = Path.Combine(backupDir, filename);
                
                Mod mod = ModManager.GetMod("CinematicKill");
                if (mod == null) 
                {
                    CKLog.Error("Export failed: CinematicKill mod not found");
                    return;
                }
                
                string configPath = Path.Combine(mod.Path, "Config", "CinematicKillSettings.xml");
                if (File.Exists(configPath))
                {
                    File.Copy(configPath, backupPath, overwrite: true);
                    CKLog.Out($"Settings exported to: {backupPath}");
                    _exportFlashTime = Time.realtimeSinceStartup;
                }
                else
                {
                    CKLog.Error($"Export failed: Config file not found at {configPath}");
                }
            }
            catch (Exception ex)
            {
                CKLog.Error($"Export failed: {ex.Message}");
            }
        }

        private void ImportSettingsFromBackup(int index)
        {
            try
            {
                string backupDir = Path.Combine(Application.persistentDataPath, "CinematicKill", "Backups");
                string[] files = Directory.GetFiles(backupDir, "*.xml");
                System.Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                
                if (index < 0 || index >= files.Length) return;
                
                string selectedFile = files[index];
                
                Mod mod = ModManager.GetMod("CinematicKill");
                if (mod == null) return;
                
                string configPath = Path.Combine(mod.Path, "Config", "CinematicKillSettings.xml");
                File.Copy(selectedFile, configPath, overwrite: true);
                
                CinematicKillManager.ReloadConfig();
                _settings = CinematicKillManager.GetSettings();
                
                CKLog.Out($"Settings imported from: {selectedFile}");
                _importFlashTime = Time.realtimeSinceStartup;
                _selectedBackupIndex = -1;
            }
            catch (Exception ex)
            {
                CKLog.Error($"Import failed: {ex.Message}");
            }
        }

        #endregion Footer

        #region Settings Management

        private void SaveSettings()
        {
            CinematicKillManager.SaveSettingsFromMenu(_settings);
            Log.Out("[CinematicKill] Settings saved.");
        }

        private void ResetToDefaults()
        {
            // Clear slider input cache so text inputs show fresh default values
            _sliderInputs.Clear();
            
            // Reset the live settings to defaults and get the new reference
            CinematicKillManager.ResetSettingsToDefaults();
            _settings = CinematicKillManager.GetSettings();
            
            Log.Out("[CinematicKill] Settings reset to defaults.");
        }

        private void ExportSettings()
        {
            try
            {
                // Get backup directory in user's persistent data folder
                string backupDir = Path.Combine(Application.persistentDataPath, "CinematicKill", "Backups");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }
                
                // Create timestamped backup filename
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupPath = Path.Combine(backupDir, $"CinematicKillSettings_{timestamp}.xml");
                
                // Find original config path
                Mod mod = ModManager.GetMod("CinematicKill");
                if (mod == null)
                {
                    Log.Error("[CinematicKill] Export failed - could not find mod via ModManager.");
                    return;
                }
                
                string configPath = Path.Combine(mod.Path, "Config", "CinematicKillSettings.xml");
                if (File.Exists(configPath))
                {
                    File.Copy(configPath, backupPath, overwrite: true);
                    Log.Out($"[CinematicKill] Settings exported to: {backupPath}");
                }
                else
                {
                    Log.Error($"[CinematicKill] Export failed - config file not found: {configPath}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[CinematicKill] Export failed: {ex.Message}");
            }
        }

        private void ImportSettings()
        {
            try
            {
                // Get backup directory
                string backupDir = Path.Combine(Application.persistentDataPath, "CinematicKill", "Backups");
                if (!Directory.Exists(backupDir))
                {
                    Log.Warning("[CinematicKill] Import failed - no backup directory found.");
                    return;
                }
                
                // Find all backup files and get the most recent one
                string[] backupFiles = Directory.GetFiles(backupDir, "CinematicKillSettings_*.xml");
                if (backupFiles.Length == 0)
                {
                    Log.Warning("[CinematicKill] Import failed - no backup files found.");
                    return;
                }
                
                // Sort by last write time descending and get most recent
                string mostRecentBackup = backupFiles
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .First();
                
                // Find mod config path
                Mod mod = ModManager.GetMod("CinematicKill");
                if (mod == null)
                {
                    Log.Error("[CinematicKill] Import failed - could not find mod via ModManager.");
                    return;
                }
                
                string configPath = Path.Combine(mod.Path, "Config", "CinematicKillSettings.xml");
                
                // Copy backup to config location
                File.Copy(mostRecentBackup, configPath, overwrite: true);
                
                // Reload settings from the imported file
                CinematicKillManager.ReloadConfig();
                _settings = CinematicKillManager.GetSettings();
                
                Log.Out($"[CinematicKill] Settings imported from: {mostRecentBackup}");
            }
            catch (Exception ex)
            {
                Log.Error($"[CinematicKill] Import failed: {ex.Message}");
            }
        }

        #endregion Settings Management

        #region Styles

        private void EnsureStyles()
        {
            if (_stylesInitialized) return;

            _bgTex = MakeTex(2, 2, _bgColor);

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = _bgTex },
                onNormal = { background = _bgTex }
            };

            _tabNormalStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.gray }
            };

            _tabSelectedStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _accentRed, background = MakeTex(2, 2, new Color(0.2f, 0.1f, 0.1f)) }
            };

            _sectionTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }  // Bright white for readable sub-headers
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.5f, 0.8f, 0.5f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            _expandButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 5, 5)
            };

            _stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = color;
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        #endregion Styles
    }
}
