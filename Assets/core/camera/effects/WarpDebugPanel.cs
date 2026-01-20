#if UNITY_EDITOR
using UnityEngine;

namespace Starfire.Core.Cam.Effects
{
    /// <summary>
    /// Editor-only debug panel that displays warp effect diagnostics.
    /// Press F7 to toggle visibility.
    /// </summary>
    public class WarpDebugPanel : MonoBehaviour
    {
        [Header("Panel Appearance")]
        [SerializeField] private Vector2 panelPosition = new(10f, 10f);
        [SerializeField] private float panelWidth = 300f;
        [SerializeField] private Color backgroundColor = new(0f, 0f, 0f, 0.85f);
        [SerializeField] private Color headerColor = new(0.4f, 0.3f, 0.6f, 1f);
        [SerializeField] private Color sectionHeaderColor = new(0.2f, 0.3f, 0.4f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color okColor = new(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color warningColor = new(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color errorColor = new(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color barBackgroundColor = new(0.2f, 0.2f, 0.2f, 1f);

        [Header("Visibility")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F7;
        [SerializeField] private bool showPanel = true;

        [Header("Test Controls")]
        [SerializeField] private KeyCode forceWarpKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode resetWarpKey = KeyCode.Space;

        private WarpEffectController _warpController;
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private Texture2D _backgroundTexture;
        private Texture2D _headerTexture;
        private Texture2D _sectionTexture;
        private Texture2D _barBackgroundTex;
        private Texture2D _intensityBarTex;
        private Texture2D _speedBarTex;
        private bool _stylesInitialized;

        private bool _forceWarpActive;

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showPanel = !showPanel;
            }

            if (_warpController == null)
            {
                _warpController = FindFirstObjectByType<WarpEffectController>();
            }

            HandleTestControls();
        }

        private void HandleTestControls()
        {
            if (_warpController == null) return;

            if (Input.GetKeyDown(forceWarpKey))
            {
                _forceWarpActive = true;
                _warpController.SetWarpIntensity(1f);
                _warpController.SetWarpDirection(Vector2.up);
            }

            if (Input.GetKeyUp(forceWarpKey) && _forceWarpActive)
            {
                _forceWarpActive = false;
                _warpController.SetWarpIntensity(0f);
            }

            if (Input.GetKeyDown(resetWarpKey))
            {
                _warpController.ResetWarp();
                _forceWarpActive = false;
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _backgroundTexture = MakeTexture(2, 2, backgroundColor);
            _headerTexture = MakeTexture(2, 2, headerColor);
            _sectionTexture = MakeTexture(2, 2, sectionHeaderColor);
            _barBackgroundTex = MakeTexture(1, 1, barBackgroundColor);
            _intensityBarTex = MakeTexture(1, 1, new Color(0.6f, 0.4f, 0.8f));
            _speedBarTex = MakeTexture(1, 1, new Color(0.3f, 0.7f, 0.9f));

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _backgroundTexture },
                padding = new RectOffset(10, 10, 10, 10)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            _sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f, 1f) }
            };

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = textColor },
                alignment = TextAnchor.MiddleRight
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!showPanel) return;

            InitStyles();

            float panelHeight = CalculatePanelHeight();
            Rect panelRect = new(panelPosition.x, panelPosition.y, panelWidth, panelHeight);
            GUI.Box(panelRect, GUIContent.none, _boxStyle);

            GUILayout.BeginArea(new Rect(panelPosition.x + 10, panelPosition.y + 5, panelWidth - 20, panelHeight - 10));
            DrawPanelContent();
            GUILayout.EndArea();
        }

        private float CalculatePanelHeight()
        {
            float height = 30f; // Header
            height += 130f; // System status
            height += 70f; // Velocity data
            height += 70f; // Speed thresholds
            height += 80f; // Warp state
            height += 140f; // Shader globals
            height += 40f; // Test controls
            return height;
        }

        private void DrawPanelContent()
        {
            // Header
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("WARP DEBUG", _headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (_warpController == null)
            {
                DrawSectionHeader("SYSTEM STATUS");
                DrawStatusRow("Controller found", false);
                GUILayout.Label("WarpEffectController not found in scene!", _labelStyle);
                GUILayout.Space(10);
                DrawHotkeysHint();
                return;
            }

            DrawSystemStatus();
            GUILayout.Space(5);
            DrawVelocityData();
            GUILayout.Space(5);
            DrawSpeedThresholds();
            GUILayout.Space(5);
            DrawWarpState();
            GUILayout.Space(5);
            DrawShaderGlobals();
            GUILayout.Space(5);
            DrawHotkeysHint();
        }

        private void DrawSystemStatus()
        {
            DrawSectionHeader("SYSTEM STATUS");

            DrawStatusRow("Controller found", true);
            DrawStatusRow("Camera found", _warpController.CameraControllerRef != null);
            DrawStatusRow("Config assigned", _warpController.Config != null);

            var camController = _warpController.CameraControllerRef;
            bool hasTargetProvider = camController?.GetTargetProvider() != null;
            bool hasTargets = camController?.GetTargetProvider()?.HasTargets ?? false;

            DrawStatusRow("Target provider", hasTargetProvider);
            DrawStatusRow("Has targets", hasTargets);

            var trackedRb = _warpController.TrackedRigidbodyRef;
            string rbName = trackedRb != null ? trackedRb.name : "None";
            DrawLabelValue("Tracked rigidbody", rbName, trackedRb != null ? okColor : textColor);
        }

        private void DrawVelocityData()
        {
            DrawSectionHeader("VELOCITY DATA");

            Vector2 velocity = _warpController.LastTrackedVelocity;
            Vector2 direction = _warpController.WarpDirection;
            float speed = _warpController.CurrentSpeed;

            DrawLabelValue("Raw velocity", $"({velocity.x:F1}, {velocity.y:F1})");
            DrawLabelValue("Speed", $"{speed:F2}");
            DrawLabelValue("Warp direction", $"({direction.x:F2}, {direction.y:F2})");
        }

        private void DrawSpeedThresholds()
        {
            DrawSectionHeader("SPEED THRESHOLDS");

            var config = _warpController.Config;
            float minSpeed = config != null ? config.minSpeedForEffect : 5f;
            float maxSpeed = config != null ? config.maxSpeedForEffect : 50f;
            float currentSpeed = _warpController.CurrentSpeed;

            DrawLabelValue("Min speed", $"{minSpeed:F1}");
            DrawLabelValue("Max speed", $"{maxSpeed:F1}");

            // Speed bar with threshold markers
            GUILayout.Space(3);
            DrawSpeedBar(currentSpeed, minSpeed, maxSpeed);
        }

        private void DrawSpeedBar(float current, float min, float max)
        {
            Rect barRect = GUILayoutUtility.GetRect(panelWidth - 40, 16);
            GUI.DrawTexture(barRect, _barBackgroundTex);

            // Fill based on current speed relative to max
            float fillPercent = Mathf.Clamp01(current / max);
            Rect fillRect = new(barRect.x, barRect.y, barRect.width * fillPercent, barRect.height);

            // Color based on whether above threshold
            Color fillColor = current >= min ? okColor : warningColor;
            var fillTex = MakeTexture(1, 1, fillColor);
            GUI.DrawTexture(fillRect, fillTex);

            // Min threshold marker
            float minMarkerX = barRect.x + (min / max) * barRect.width;
            Rect markerRect = new(minMarkerX - 1, barRect.y, 2, barRect.height);
            GUI.DrawTexture(markerRect, MakeTexture(1, 1, Color.yellow));

            // Label
            var labelStyle = new GUIStyle(_labelStyle) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(barRect, $"{current:F1} / {max:F1}", labelStyle);
        }

        private void DrawWarpState()
        {
            DrawSectionHeader("WARP STATE");

            Color stateColor = _warpController.CurrentState switch
            {
                WarpState.Idle => textColor,
                WarpState.InWarp => okColor,
                _ => warningColor
            };

            DrawLabelValue("Current state", _warpController.CurrentState.ToString(), stateColor);
            DrawLabelValue("Using speed-based", _warpController.UseSpeedBasedWarp ? "Yes" : "No");
            DrawLabelValue("Target intensity", $"{_warpController.TargetIntensityDebug:F3}");

            // Intensity bar
            GUILayout.Space(3);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Intensity:", _labelStyle, GUILayout.Width(60));
            DrawIntensityBar(_warpController.WarpIntensity);
            GUILayout.Label($"{_warpController.WarpIntensity:F3}", _valueStyle, GUILayout.Width(50));
            GUILayout.EndHorizontal();
        }

        private void DrawIntensityBar(float intensity)
        {
            Rect barRect = GUILayoutUtility.GetRect(120, 12);
            GUI.DrawTexture(barRect, _barBackgroundTex);

            float fillPercent = Mathf.Clamp01(intensity);
            Rect fillRect = new(barRect.x, barRect.y, barRect.width * fillPercent, barRect.height);
            GUI.DrawTexture(fillRect, _intensityBarTex);
        }

        private void DrawShaderGlobals()
        {
            DrawSectionHeader("SHADER GLOBALS");

            float warpIntensity = Shader.GetGlobalFloat("_WarpIntensity");
            float warpStretch = Shader.GetGlobalFloat("_WarpStretch");
            Vector4 warpDir = Shader.GetGlobalVector("_WarpDirection");
            float brightnessBoost = Shader.GetGlobalFloat("_WarpBrightnessBoost");
            float nebulaStretch = Shader.GetGlobalFloat("_WarpNebulaStretch");
            float nebulaFade = Shader.GetGlobalFloat("_WarpNebulaFade");

            DrawLabelValue("_WarpIntensity", $"{warpIntensity:F3}", warpIntensity > 0.001f ? okColor : textColor);
            DrawLabelValue("_WarpStretch", $"{warpStretch:F2}");
            DrawLabelValue("_WarpDirection", $"({warpDir.x:F2}, {warpDir.y:F2})");
            DrawLabelValue("_WarpBrightnessBoost", $"{brightnessBoost:F2}");
            DrawLabelValue("_WarpNebulaStretch", $"{nebulaStretch:F2}");
            DrawLabelValue("_WarpNebulaFade", $"{nebulaFade:F2}");
        }

        private void DrawHotkeysHint()
        {
            var hintStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 1f) }
            };
            GUILayout.Label($"[{toggleKey}] Toggle | Hold [{forceWarpKey}] Force Warp | [{resetWarpKey}] Reset", hintStyle);
        }

        private void DrawSectionHeader(string text)
        {
            Rect rect = GUILayoutUtility.GetRect(panelWidth - 40, 18);
            GUI.DrawTexture(rect, _sectionTexture);
            GUI.Label(rect, " " + text, _sectionStyle);
        }

        private void DrawStatusRow(string label, bool isOk)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", _labelStyle, GUILayout.Width(120));

            var statusStyle = new GUIStyle(_valueStyle)
            {
                normal = { textColor = isOk ? okColor : errorColor }
            };
            GUILayout.Label(isOk ? "Yes" : "No", statusStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawLabelValue(string label, string value, Color? valueColor = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", _labelStyle, GUILayout.Width(120));

            var style = new GUIStyle(_valueStyle);
            if (valueColor.HasValue)
            {
                style.normal.textColor = valueColor.Value;
            }
            GUILayout.Label(value, style);
            GUILayout.EndHorizontal();
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            if (_backgroundTexture != null) DestroyImmediate(_backgroundTexture);
            if (_headerTexture != null) DestroyImmediate(_headerTexture);
            if (_sectionTexture != null) DestroyImmediate(_sectionTexture);
            if (_barBackgroundTex != null) DestroyImmediate(_barBackgroundTex);
            if (_intensityBarTex != null) DestroyImmediate(_intensityBarTex);
            if (_speedBarTex != null) DestroyImmediate(_speedBarTex);
        }
    }
}
#endif
