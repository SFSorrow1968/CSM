using UnityEngine;
using System.Collections.Generic;

namespace CinematicKill
{
    /// <summary>
    /// On-screen HUD for displaying cinematic kill information in real-time.
    /// Shows triggers, effects, timing, and other useful info with color coding.
    /// </summary>
    public class CinematicKillHUD : MonoBehaviour
    {
        private static CinematicKillHUD _instance;
        public static CinematicKillHUD Instance => _instance;

        // ─────────────────────────────────────────────────────────────
        //  Settings
        // ─────────────────────────────────────────────────────────────
        private bool _hudEnabled = true;
        private float _hudOpacity = 0.85f;
        private float _messageDuration = 3f;
        private float _fadeOutDuration = 0.5f;

        // ─────────────────────────────────────────────────────────────
        //  Message Queue
        // ─────────────────────────────────────────────────────────────
        private class HUDMessage
        {
            public string Text;
            public Color Color;
            public float TimeAdded;
            public float Duration;
            public MessageCategory Category;
            public string Icon;
        }

        public enum MessageCategory
        {
            Trigger,        // Kill triggers (Last Enemy, Headshot, etc.)
            Camera,         // Camera selection info
            Timing,         // Duration and time scale info
            Effect,         // Visual effects activated
            Bonus,          // Killstreak and bonus info
            System          // System messages (cooldown, blocked, etc.)
        }

        private readonly List<HUDMessage> _activeMessages = new List<HUDMessage>();
        private readonly object _messageLock = new object();

        // ─────────────────────────────────────────────────────────────
        //  Color Palette (matching menu colors for consistency)
        // ─────────────────────────────────────────────────────────────
        private static readonly Color COLOR_TRIGGER = new Color(1f, 0.85f, 0.3f);      // Gold/Yellow for triggers
        private static readonly Color COLOR_CAMERA = new Color(0.31f, 0.76f, 0.97f);   // Cyan for camera info
        private static readonly Color COLOR_TIMING = new Color(0.7f, 0.4f, 1f);        // Purple for timing
        private static readonly Color COLOR_EFFECT = new Color(0.51f, 0.78f, 0.52f);   // Green for effects
        private static readonly Color COLOR_BONUS = new Color(1f, 0.5f, 0.2f);         // Orange for bonuses
        private static readonly Color COLOR_SYSTEM = new Color(0.7f, 0.7f, 0.7f);      // Gray for system
        private static readonly Color COLOR_WARNING = new Color(0.85f, 0.19f, 0.19f);  // Red for warnings
        private static readonly Color COLOR_SUCCESS = new Color(0.4f, 0.9f, 0.4f);     // Bright green for success

        // ─────────────────────────────────────────────────────────────
        //  Icons (Unicode symbols)
        // ─────────────────────────────────────────────────────────────
        private static readonly string ICON_TRIGGER = "⚡";
        private static readonly string ICON_CAMERA = "📷";
        private static readonly string ICON_TIMING = "⏱";
        private static readonly string ICON_EFFECT = "✨";
        private static readonly string ICON_BONUS = "🔥";
        private static readonly string ICON_SYSTEM = "ℹ";
        private static readonly string ICON_WARNING = "⚠";
        private static readonly string ICON_KILL = "🎯";

        // ─────────────────────────────────────────────────────────────
        //  GUI Styles
        // ─────────────────────────────────────────────────────────────
        private GUIStyle _messageStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _iconStyle;
        private bool _stylesInitialized;
        private Texture2D _backgroundTexture;

        // ─────────────────────────────────────────────────────────────
        //  Position Settings
        // ─────────────────────────────────────────────────────────────
        private const float MARGIN_RIGHT = 20f;
        private const float MARGIN_TOP = 100f;
        private const float MESSAGE_WIDTH = 320f;
        private const float MESSAGE_HEIGHT = 28f;
        private const float MESSAGE_SPACING = 4f;
        private const int MAX_VISIBLE_MESSAGES = 12;

        private void Awake()
        {
            _instance = this;
            CreateBackgroundTexture();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;

            if (_backgroundTexture != null)
                Destroy(_backgroundTexture);
        }

        private void CreateBackgroundTexture()
        {
            _backgroundTexture = new Texture2D(1, 1);
            _backgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            _backgroundTexture.Apply();
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _messageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(8, 8, 4, 4)
            };

            _headerStyle = new GUIStyle(_messageStyle)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            _iconStyle = new GUIStyle(_messageStyle)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 4, 4)
            };

            _stylesInitialized = true;
        }

        private void Update()
        {
            // Remove expired messages
            float currentTime = Time.realtimeSinceStartup;
            lock (_messageLock)
            {
                _activeMessages.RemoveAll(m => currentTime > m.TimeAdded + m.Duration + _fadeOutDuration);
            }
        }

        private void OnGUI()
        {
            if (!_hudEnabled || !ShouldShowHUD()) return;

            InitializeStyles();

            // Get position settings
            var settings = CinematicKillManager.Settings?.MenuV2?.Toast;
            int position = settings?.Position ?? 0;  // Default: TopRight
            float offsetX = settings?.OffsetX ?? MARGIN_RIGHT;
            float offsetY = settings?.OffsetY ?? MARGIN_TOP;
            float opacity = settings?.Opacity ?? _hudOpacity;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float startX, startY;
            bool growDown = true;  // Whether messages grow downward from start position

            // Calculate position based on setting
            // 0=TopRight, 1=TopLeft, 2=BottomRight, 3=BottomLeft, 4=TopCenter, 5=BottomCenter
            switch (position)
            {
                case 0: // Top Right
                    startX = screenWidth - MESSAGE_WIDTH - offsetX;
                    startY = offsetY;
                    growDown = true;
                    break;
                case 1: // Top Left
                    startX = offsetX;
                    startY = offsetY;
                    growDown = true;
                    break;
                case 2: // Bottom Right
                    startX = screenWidth - MESSAGE_WIDTH - offsetX;
                    startY = screenHeight - offsetY - MESSAGE_HEIGHT;
                    growDown = false;
                    break;
                case 3: // Bottom Left
                    startX = offsetX;
                    startY = screenHeight - offsetY - MESSAGE_HEIGHT;
                    growDown = false;
                    break;
                case 4: // Top Center
                    startX = (screenWidth - MESSAGE_WIDTH) / 2f;
                    startY = offsetY;
                    growDown = true;
                    break;
                case 5: // Bottom Center
                    startX = (screenWidth - MESSAGE_WIDTH) / 2f;
                    startY = screenHeight - offsetY - MESSAGE_HEIGHT;
                    growDown = false;
                    break;
                default: // Default to Top Right
                    startX = screenWidth - MESSAGE_WIDTH - offsetX;
                    startY = offsetY;
                    growDown = true;
                    break;
            }

            lock (_messageLock)
            {
                int visibleCount = Mathf.Min(_activeMessages.Count, MAX_VISIBLE_MESSAGES);
                float currentTime = Time.realtimeSinceStartup;

                for (int i = 0; i < visibleCount; i++)
                {
                    var msg = _activeMessages[i];
                    float age = currentTime - msg.TimeAdded;
                    float alpha = opacity;

                    // Fade out at end of life
                    if (age > msg.Duration)
                    {
                        float fadeProgress = (age - msg.Duration) / _fadeOutDuration;
                        alpha *= 1f - Mathf.Clamp01(fadeProgress);
                    }
                    // Fade in at start
                    else if (age < 0.2f)
                    {
                        alpha *= age / 0.2f;
                    }

                    if (alpha <= 0.01f) continue;

                    // Calculate Y position based on growth direction
                    float y = growDown 
                        ? startY + i * (MESSAGE_HEIGHT + MESSAGE_SPACING)
                        : startY - i * (MESSAGE_HEIGHT + MESSAGE_SPACING);
                    DrawMessage(startX, y, msg, alpha);
                }
            }
        }

        private void DrawMessage(float x, float y, HUDMessage msg, float alpha)
        {
            Rect bgRect = new Rect(x, y, MESSAGE_WIDTH, MESSAGE_HEIGHT);

            // Background
            Color bgColor = new Color(0f, 0f, 0f, 0.65f * alpha);
            GUI.color = bgColor;
            GUI.DrawTexture(bgRect, _backgroundTexture);

            // Left accent bar (color coded by category)
            Color accentColor = msg.Color;
            accentColor.a = alpha;
            Rect accentRect = new Rect(x, y, 4f, MESSAGE_HEIGHT);
            GUI.color = accentColor;
            GUI.DrawTexture(accentRect, _backgroundTexture);

            // Icon
            GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, alpha);
            Rect iconRect = new Rect(x + 8, y, 24, MESSAGE_HEIGHT);
            _iconStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);
            GUI.Label(iconRect, msg.Icon, _iconStyle);

            // Text
            Color textColor = msg.Color;
            textColor.a = alpha;
            _messageStyle.normal.textColor = textColor;
            Rect textRect = new Rect(x + 32, y, MESSAGE_WIDTH - 40, MESSAGE_HEIGHT);
            GUI.Label(textRect, msg.Text, _messageStyle);

            GUI.color = Color.white;
        }

        private bool ShouldShowHUD()
        {
            // Check if HUD is enabled in settings
            var settings = CinematicKillManager.Settings;
            if (settings?.MenuV2?.Core == null) return false;
            
            return settings.MenuV2.Core.Enabled && settings.MenuV2.Core.EnableHUD;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Public API - Add Messages
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Add a message to the HUD with a specific category
        /// </summary>
        public void AddMessage(string text, MessageCategory category, float duration = -1f)
        {
            if (duration < 0) duration = _messageDuration;

            var msg = new HUDMessage
            {
                Text = text,
                Color = GetCategoryColor(category),
                TimeAdded = Time.realtimeSinceStartup,
                Duration = duration,
                Category = category,
                Icon = GetCategoryIcon(category)
            };

            lock (_messageLock)
            {
                // Remove old messages of same category to prevent spam
                _activeMessages.RemoveAll(m => m.Category == category && m.Text == text);
                
                // Insert at top
                _activeMessages.Insert(0, msg);

                // Limit total messages
                while (_activeMessages.Count > MAX_VISIBLE_MESSAGES * 2)
                {
                    _activeMessages.RemoveAt(_activeMessages.Count - 1);
                }
            }
        }

        /// <summary>
        /// Add a custom colored message
        /// </summary>
        public void AddMessage(string text, Color color, string icon, float duration = -1f)
        {
            if (duration < 0) duration = _messageDuration;

            var msg = new HUDMessage
            {
                Text = text,
                Color = color,
                TimeAdded = Time.realtimeSinceStartup,
                Duration = duration,
                Category = MessageCategory.System,
                Icon = icon
            };

            lock (_messageLock)
            {
                _activeMessages.Insert(0, msg);
                while (_activeMessages.Count > MAX_VISIBLE_MESSAGES * 2)
                {
                    _activeMessages.RemoveAt(_activeMessages.Count - 1);
                }
            }
        }

        /// <summary>
        /// Clear all messages
        /// </summary>
        public void ClearMessages()
        {
            lock (_messageLock)
            {
                _activeMessages.Clear();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Convenience Methods for Common Messages
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show trigger activation message
        /// </summary>
        public void ShowTrigger(string triggerName, float chance = -1f)
        {
            string text = chance >= 0 
                ? $"{triggerName} triggered ({chance:0}% chance)"
                : $"{triggerName} triggered!";
            AddMessage(text, MessageCategory.Trigger);
        }

        /// <summary>
        /// Show camera selection
        /// </summary>
        public void ShowCamera(string cameraType, float chance = -1f)
        {
            string text = chance >= 0
                ? $"{cameraType} Camera ({chance:0}%)"
                : $"{cameraType} Camera";
            AddMessage(text, MessageCategory.Camera);
        }

        /// <summary>
        /// Show timing info
        /// </summary>
        public void ShowTiming(float duration, float timeScale)
        {
            string text = $"{duration:0.0}s @ {timeScale:0.00}x speed";
            AddMessage(text, MessageCategory.Timing);
        }

        /// <summary>
        /// Show bonus applied
        /// </summary>
        public void ShowBonus(string bonusName, float value, string unit = "")
        {
            string sign = value >= 0 ? "+" : "";
            string text = $"{bonusName}: {sign}{value:0.0}{unit}";
            AddMessage(text, MessageCategory.Bonus);
        }

        /// <summary>
        /// Show effect activation
        /// </summary>
        public void ShowEffect(string effectName, bool enabled = true)
        {
            string status = enabled ? "ON" : "OFF";
            AddMessage($"{effectName}: {status}", MessageCategory.Effect);
        }

        /// <summary>
        /// Show multiple effects at once
        /// </summary>
        public void ShowEffects(List<string> effects)
        {
            if (effects == null || effects.Count == 0) return;
            string text = string.Join(" • ", effects);
            AddMessage(text, MessageCategory.Effect);
        }

        /// <summary>
        /// Show system message
        /// </summary>
        public void ShowSystem(string message)
        {
            AddMessage(message, MessageCategory.System);
        }

        /// <summary>
        /// Show warning message
        /// </summary>
        public void ShowWarning(string message)
        {
            AddMessage(message, COLOR_WARNING, ICON_WARNING);
        }

        /// <summary>
        /// Show success message
        /// </summary>
        public void ShowSuccess(string message)
        {
            AddMessage(message, COLOR_SUCCESS, ICON_KILL);
        }

        /// <summary>
        /// Show cooldown info
        /// </summary>
        public void ShowCooldown(float remaining)
        {
            AddMessage($"Cooldown: {remaining:0.0}s remaining", MessageCategory.System, 1.5f);
        }

        /// <summary>
        /// Show killstreak info
        /// </summary>
        public void ShowKillstreak(int streak, int tier, float bonusDuration)
        {
            string tierText = tier > 0 ? $" (Tier {tier})" : "";
            AddMessage($"Killstreak: {streak} kills{tierText} +{bonusDuration:0.0}s", MessageCategory.Bonus);
        }

        /// <summary>
        /// Show full cinematic start info
        /// </summary>
        public void ShowCinematicStart(string triggerName, string cameraType, float duration, float timeScale, List<string> activeEffects)
        {
            // Primary trigger
            AddMessage($"{triggerName} Kill!", COLOR_SUCCESS, ICON_KILL, _messageDuration + 0.5f);

            // Camera
            ShowCamera(cameraType);

            // Timing
            ShowTiming(duration, timeScale);

            // Effects
            if (activeEffects != null && activeEffects.Count > 0)
            {
                ShowEffects(activeEffects);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Helper Methods
        // ═══════════════════════════════════════════════════════════════

        private Color GetCategoryColor(MessageCategory category)
        {
            return category switch
            {
                MessageCategory.Trigger => COLOR_TRIGGER,
                MessageCategory.Camera => COLOR_CAMERA,
                MessageCategory.Timing => COLOR_TIMING,
                MessageCategory.Effect => COLOR_EFFECT,
                MessageCategory.Bonus => COLOR_BONUS,
                MessageCategory.System => COLOR_SYSTEM,
                _ => COLOR_SYSTEM
            };
        }

        private string GetCategoryIcon(MessageCategory category)
        {
            return category switch
            {
                MessageCategory.Trigger => ICON_TRIGGER,
                MessageCategory.Camera => ICON_CAMERA,
                MessageCategory.Timing => ICON_TIMING,
                MessageCategory.Effect => ICON_EFFECT,
                MessageCategory.Bonus => ICON_BONUS,
                MessageCategory.System => ICON_SYSTEM,
                _ => ICON_SYSTEM
            };
        }

        // ═══════════════════════════════════════════════════════════════
        //  Settings Integration
        // ═══════════════════════════════════════════════════════════════

        public bool HUDEnabled
        {
            get => _hudEnabled;
            set => _hudEnabled = value;
        }

        public float HUDOpacity
        {
            get => _hudOpacity;
            set => _hudOpacity = Mathf.Clamp01(value);
        }

        public float MessageDuration
        {
            get => _messageDuration;
            set => _messageDuration = Mathf.Max(0.5f, value);
        }
    }
}
