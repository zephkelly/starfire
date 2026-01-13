#if UNITY_EDITOR
using UnityEngine;
using Starfire.Entity;
using Starfire.Entity.Modules.Shield;
using Starfire.Entity.Modules.Hull;

namespace Starfire.Core.Debugging
{
    /// <summary>
    /// Editor-only debug panel that displays entity stats in world-space.
    /// Click an entity to select it and view its stats.
    /// </summary>
    public class EntityDebugPanel : MonoBehaviour
    {
        [Header("Selection")]
        [Tooltip("Layers that can be selected for debug display")]
        [SerializeField] private LayerMask selectableLayers = -1;

        [Header("Panel Appearance")]
        [SerializeField] private Vector2 panelOffset = new(120f, 0f);
        [SerializeField] private float panelWidth = 220f;
        [SerializeField] private Color backgroundColor = new(0f, 0f, 0f, 0.8f);
        [SerializeField] private Color headerColor = new(0.2f, 0.4f, 0.8f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color healthBarBackgroundColor = new(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color shieldBarColor = new(0.3f, 0.6f, 1f, 1f);
        [SerializeField] private Color hullBarColor = new(0.2f, 0.8f, 0.2f, 1f);

        private EntityControllerBase _selectedEntity;
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private Texture2D _backgroundTexture;
        private Texture2D _headerTexture;
        private bool _stylesInitialized;

        private void Update()
        {
            // Handle click selection
            if (Input.GetMouseButtonDown(0))
            {
                TrySelectEntity();
            }

            // Handle deselection
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _selectedEntity = null;
            }

            // Clear selection if entity was destroyed
            if (_selectedEntity != null && _selectedEntity.gameObject == null)
            {
                _selectedEntity = null;
            }
        }

        private void TrySelectEntity()
        {
            var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.Raycast(mousePos, Vector2.zero, 0f, selectableLayers);

            if (hit.collider != null)
            {
                var entity = hit.collider.GetComponentInParent<EntityControllerBase>();
                if (entity != null)
                {
                    _selectedEntity = entity;
                    return;
                }
            }

            // Clicked nothing - deselect
            _selectedEntity = null;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _backgroundTexture = MakeTexture(2, 2, backgroundColor);
            _headerTexture = MakeTexture(2, 2, headerColor);

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

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f, 1f) }
            };

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = textColor },
                alignment = TextAnchor.MiddleRight
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (_selectedEntity == null) return;

            InitStyles();

            // Calculate screen position
            Vector3 worldPos = _selectedEntity.transform.position;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // Flip Y for GUI coordinates
            screenPos.y = Screen.height - screenPos.y;

            // Don't render if behind camera
            if (screenPos.z < 0) return;

            // Panel position with offset
            float panelX = screenPos.x + panelOffset.x;
            float panelY = screenPos.y + panelOffset.y;

            // Calculate panel height based on content
            float panelHeight = CalculatePanelHeight();

            // Keep panel on screen
            panelX = Mathf.Clamp(panelX, 0, Screen.width - panelWidth);
            panelY = Mathf.Clamp(panelY, 0, Screen.height - panelHeight);

            // Draw panel
            Rect panelRect = new(panelX, panelY, panelWidth, panelHeight);
            GUI.Box(panelRect, GUIContent.none, _boxStyle);

            GUILayout.BeginArea(new Rect(panelX + 10, panelY + 5, panelWidth - 20, panelHeight - 10));
            DrawPanelContent();
            GUILayout.EndArea();

            // Draw line from entity to panel
            DrawConnectionLine(screenPos, new Vector2(panelX, panelY + panelHeight / 2));
        }

        private float CalculatePanelHeight()
        {
            float height = 30f; // Header
            height += 60f; // Hull bar
            height += 60f; // Shield bar
            height += 80f; // Velocity section
            height += 60f; // Module summary
            return height;
        }

        private void DrawPanelContent()
        {
            // Header
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(_selectedEntity.name, _headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Hull
            var hull = GetHull();
            if (hull != null)
            {
                DrawHealthBar("Hull", hull.CurrentHealth, hull.MaxHealth, hullBarColor);
            }
            else
            {
                GUILayout.Label("Hull: N/A", _labelStyle);
            }

            GUILayout.Space(5);

            // Shield
            var shield = GetShield();
            if (shield != null)
            {
                DrawHealthBar($"Shield ({shield.State})", shield.CurrentShield, shield.MaxShield, shieldBarColor);
            }
            else
            {
                GUILayout.Label("Shield: N/A", _labelStyle);
            }

            GUILayout.Space(10);

            // Velocity
            GUILayout.Label("Velocity", _labelStyle);
            var rb = _selectedEntity.Rigidbody;
            if (rb != null)
            {
                float speed = rb.linearVelocity.magnitude;
                DrawLabelValue("Speed", $"{speed:F1} u/s");
                DrawLabelValue("Direction", $"({rb.linearVelocity.x:F1}, {rb.linearVelocity.y:F1})");
            }

            GUILayout.Space(5);

            // Rotation
            float rotation = _selectedEntity.transform.eulerAngles.z;
            DrawLabelValue("Rotation", $"{rotation:F1}°");

            GUILayout.Space(10);

            // Module summary
            var shipController = _selectedEntity as ShipController;
            if (shipController?.ShipSystems != null)
            {
                var systems = shipController.ShipSystems;
                DrawLabelValue("Weapons", systems.TotalWeaponCount.ToString());
                DrawLabelValue("FTL", systems.HasFTLCapability ? "Yes" : "No");

                var propulsion = systems.FastestPropulsion;
                if (propulsion != null)
                {
                    DrawLabelValue("Max Speed", $"{propulsion.MaxSpeed:F0} u/s");
                }
            }
        }

        private void DrawHealthBar(string label, float current, float max, Color barColor)
        {
            GUILayout.Label($"{label}: {current:F0}/{max:F0}", _labelStyle);

            Rect barRect = GUILayoutUtility.GetRect(panelWidth - 40, 16);

            // Background
            GUI.DrawTexture(barRect, MakeTexture(1, 1, healthBarBackgroundColor));

            // Fill
            float fillPercent = max > 0 ? current / max : 0;
            Rect fillRect = new(barRect.x, barRect.y, barRect.width * fillPercent, barRect.height);
            GUI.DrawTexture(fillRect, MakeTexture(1, 1, barColor));
        }

        private void DrawLabelValue(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", _labelStyle, GUILayout.Width(80));
            GUILayout.Label(value, _valueStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawConnectionLine(Vector2 entityPos, Vector2 panelPos)
        {
            // Simple line using GL
            if (Event.current.type != EventType.Repaint) return;

            GL.PushMatrix();
            GL.LoadPixelMatrix();

            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 1f, 1f, 0.3f));
            GL.Vertex3(entityPos.x, entityPos.y, 0);
            GL.Vertex3(panelPos.x, panelPos.y, 0);
            GL.End();

            GL.PopMatrix();
        }

        private IHullModule GetHull()
        {
            var shipController = _selectedEntity as ShipController;
            return shipController?.ShipSystems?.PrimaryHull;
        }

        private IShieldModule GetShield()
        {
            var shipController = _selectedEntity as ShipController;
            return shipController?.ShipSystems?.PrimaryShield;
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
        }
    }
}
#endif
