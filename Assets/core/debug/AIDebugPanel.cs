#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Starfire.Entity;
using Starfire.Entity.AI.BT;
using Starfire.Entity.AI.Goals;
using Starfire.Entity.AI.Heuristics;
using Starfire.Entity.Modules.AICore;
using Starfire.Entity.Modules.Damage;

namespace Starfire.Core.Debugging
{
    /// <summary>
    /// Editor-only debug panel that displays AI state information.
    /// Select an entity in the Unity hierarchy to view its AI state.
    /// </summary>
    public class AIDebugPanel : MonoBehaviour
    {
        [Header("Panel Appearance")]
        [SerializeField] private Vector2 panelOffset = new(-340f, 0f);
        [SerializeField] private float panelWidth = 336f;
        [SerializeField] private Color backgroundColor = new(0f, 0f, 0f, 0.85f);
        [SerializeField] private Color headerColor = new(0.3f, 0.5f, 0.2f, 1f);
        [SerializeField] private Color sectionHeaderColor = new(0.2f, 0.3f, 0.4f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color barBackgroundColor = new(0.2f, 0.2f, 0.2f, 1f);

        [Header("Visibility")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F9;
        [SerializeField] private bool showPanel = true;

        [Header("Test Actions")]
        [SerializeField] private KeyCode damageKey = KeyCode.F10;
        [SerializeField] private KeyCode healKey = KeyCode.F11;
        [SerializeField] private float damageAmount = 25f;

        private EntityControllerBase _selectedEntity;
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private Texture2D _backgroundTexture;
        private Texture2D _headerTexture;
        private Texture2D _sectionTexture;
        private Texture2D _barBackgroundTex;
        private Texture2D _goalBarTex;
        private Texture2D _goalBarActiveTex;
        private Texture2D _hullBarTex;
        private Texture2D _shieldBarTex;
        private bool _stylesInitialized;

        // Cached AI references
        private ShipController _shipController;
        private IAICoreShipModule _aiCore;
        private BasicAICoreModule _basicAICore;
        private BTContext _btContext;
        private GoalManager _goalManager;

        // Hierarchy selection tracking
        private GameObject _lastSelectedObject;

        private void Update()
        {
            // Toggle visibility
            if (Input.GetKeyDown(toggleKey))
            {
                showPanel = !showPanel;
            }

            // Sync with Unity hierarchy selection
            var selected = Selection.activeGameObject;
            if (selected != _lastSelectedObject)
            {
                _lastSelectedObject = selected;
                var entity = selected?.GetComponentInParent<EntityControllerBase>()
                          ?? selected?.GetComponentInChildren<EntityControllerBase>();
                if (entity != null)
                    SelectEntity(entity);
                else
                    ClearSelection();
            }

            // Clear selection if entity was destroyed
            if (_selectedEntity != null && _selectedEntity.gameObject == null)
            {
                ClearSelection();
            }

            // Test actions
            if (_selectedEntity != null)
            {
                if (Input.GetKeyDown(damageKey))
                {
                    ApplyDamage(damageAmount);
                }
                if (Input.GetKeyDown(healKey))
                {
                    HealToFull();
                }
            }
        }

        private void SelectEntity(EntityControllerBase entity)
        {
            _selectedEntity = entity;
            _shipController = entity as ShipController;

            if (_shipController?.ShipSystems != null)
            {
                _aiCore = _shipController.ShipSystems.PrimaryAICore;
                _basicAICore = _aiCore as BasicAICoreModule;
                _btContext = _aiCore?.Context;
                _goalManager = _basicAICore?.GoalManager;
            }
            else
            {
                _aiCore = null;
                _basicAICore = null;
                _btContext = null;
                _goalManager = null;
            }
        }

        private void ClearSelection()
        {
            _selectedEntity = null;
            _shipController = null;
            _aiCore = null;
            _basicAICore = null;
            _btContext = null;
            _goalManager = null;
        }

        private void ApplyDamage(float amount)
        {
            var hull = _shipController?.ShipSystems?.PrimaryHull;
            if (hull != null)
            {
                hull.TakeDamage(DamageInfo.Simple(amount));
            }
        }

        private void HealToFull()
        {
            var hull = _shipController?.ShipSystems?.PrimaryHull;
            if (hull != null)
            {
                hull.CurrentHealth = hull.MaxHealth;
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _backgroundTexture = MakeTexture(2, 2, backgroundColor);
            _headerTexture = MakeTexture(2, 2, headerColor);
            _sectionTexture = MakeTexture(2, 2, sectionHeaderColor);
            _barBackgroundTex = MakeTexture(1, 1, barBackgroundColor);
            _goalBarTex = MakeTexture(1, 1, new Color(0.4f, 0.6f, 0.4f));
            _goalBarActiveTex = MakeTexture(1, 1, Color.yellow);
            _hullBarTex = MakeTexture(1, 1, new Color(0.2f, 0.8f, 0.2f));
            _shieldBarTex = MakeTexture(1, 1, new Color(0.3f, 0.6f, 1f));

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
            if (!showPanel || _selectedEntity == null) return;

            InitStyles();

            // Calculate screen position
            Vector3 worldPos = _selectedEntity.transform.position;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            screenPos.y = Screen.height - screenPos.y;

            if (screenPos.z < 0) return;

            float panelX = screenPos.x + panelOffset.x;
            float panelY = screenPos.y + panelOffset.y;
            float panelHeight = CalculatePanelHeight();

            panelX = Mathf.Clamp(panelX, 0, Screen.width - panelWidth);
            panelY = Mathf.Clamp(panelY, 0, Screen.height - panelHeight);

            Rect panelRect = new(panelX, panelY, panelWidth, panelHeight);
            GUI.Box(panelRect, GUIContent.none, _boxStyle);

            GUILayout.BeginArea(new Rect(panelX + 10, panelY + 5, panelWidth - 20, panelHeight - 10));
            DrawPanelContent();
            GUILayout.EndArea();

            DrawConnectionLine(screenPos, new Vector2(panelX + panelWidth, panelY + panelHeight / 2));
        }

        private float CalculatePanelHeight()
        {
            float height = 30f; // Header

            // Goal state: section header + labels + dynamic goal count
            int goalCount = _goalManager?.AvailableGoals?.Count ?? 0;
            height += 70f + (goalCount * 16f);

            height += 200f; // Heuristics
            height += 80f; // Steering
            height += 20f; // Hotkeys hint
            return height;
        }

        private void DrawPanelContent()
        {
            // Header
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label($"AI: {_selectedEntity.name}", _headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Check if AI module exists
            if (_btContext == null)
            {
                GUILayout.Label("No AI Module on this entity", _labelStyle);
                GUILayout.Space(10);
                DrawHotkeysHint();
                return;
            }

            // Get heuristics data
            _btContext.TryGet<HeuristicData>(HeuristicKeys.HeuristicData, out var heuristics);

            DrawGoalState(heuristics);
            GUILayout.Space(5);
            DrawHeuristics(heuristics);
            GUILayout.Space(5);
            DrawSteering();
            GUILayout.Space(5);
            DrawHotkeysHint();
        }

        private void DrawGoalState(HeuristicData heuristics)
        {
            DrawSectionHeader("GOAL STATE");

            if (_goalManager == null)
            {
                GUILayout.Label("No GoalManager", _labelStyle);
                return;
            }

            var currentGoal = _goalManager.CurrentGoal;
            string goalName = currentGoal?.Type.ToString() ?? "None";
            bool isCommander = _goalManager.HasCommanderAssignment;

            DrawLabelValue("Current Goal", goalName);
            DrawLabelValue("Commander Assigned", isCommander ? "Yes" : "No");

            GUILayout.Space(3);
            GUILayout.Label("Goal Scores:", _labelStyle);

            foreach (var goal in _goalManager.AvailableGoals)
            {
                float score = goal.Evaluate(heuristics, _btContext) * goal.BasePriority;
                bool isCurrent = goal == currentGoal;
                string prefix = isCurrent ? "> " : "  ";
                DrawGoalScoreBar(prefix + goal.Type.ToString(), score, isCurrent);
            }
        }

        private void DrawHeuristics(HeuristicData heuristics)
        {
            DrawSectionHeader("HEURISTICS");

            // Health row
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hull:", _labelStyle, GUILayout.Width(50));
            DrawMiniBar(heuristics.HullPercent, isHull: true);
            GUILayout.Label("Shield:", _labelStyle, GUILayout.Width(50));
            DrawMiniBar(heuristics.ShieldPercent, isHull: false);
            GUILayout.EndHorizontal();

            // Derived states
            DrawLabelValue("Confidence", $"{heuristics.Confidence:F2}");
            DrawLabelValue("Skittishness", $"{heuristics.Skittishness:F2}");
            DrawLabelValue("Vulnerability", $"{heuristics.Vulnerability:F2}");

            GUILayout.Space(3);

            // Threat
            DrawLabelValue("Threat Level", $"{heuristics.ThreatLevel:F2}");
            DrawLabelValue("Hostiles Nearby", heuristics.NearbyHostileCount.ToString());
            if (heuristics.NearbyHostileCount > 0)
            {
                DrawLabelValue("Closest Threat", $"{heuristics.ClosestThreatDistance:F1} units");
            }

            GUILayout.Space(3);

            // Detection
            DrawLabelValue("Unidentified", $"{heuristics.UnidentifiedContactCount}");
            if (heuristics.HasUnidentifiedContacts)
            {
                DrawLabelValue("Closest Unid", $"{heuristics.ClosestUnidentifiedDistance:F1} units");
            }

            // Capabilities
            GUILayout.Space(3);
            DrawLabelValue("Max Speed", $"{heuristics.MaxSpeed:F1} u/s");
            DrawLabelValue("Max Accel", $"{heuristics.MaxAcceleration:F1} u/s²");
            DrawLabelValue("Sensor Range", $"{heuristics.SensorRange:F0} units");
        }

        private void DrawSteering()
        {
            DrawSectionHeader("STEERING");

            // Steering target
            if (_btContext.TryGet<Vector2>("steering_target", out var target))
            {
                Vector2 pos = _selectedEntity.transform.position;
                float dist = Vector2.Distance(pos, target);
                DrawLabelValue("Target", $"({target.x:F0}, {target.y:F0})");
                DrawLabelValue("Distance", $"{dist:F1} units");
            }
            else
            {
                DrawLabelValue("Target", "None");
            }

            // Steering force
            if (_btContext.TryGet<Vector2>("steering_force", out var force))
            {
                DrawLabelValue("Force", $"({force.x:F1}, {force.y:F1}) | {force.magnitude:F1}");
            }

            // Cruise limits
            if (_btContext.TryGet<float>("cruise_speed", out var cruiseSpeed) && cruiseSpeed > 0)
            {
                DrawLabelValue("Cruise Speed", $"{cruiseSpeed:F1} u/s");
            }
        }

        private void DrawHotkeysHint()
        {
            var hintStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 1f) }
            };
            GUILayout.Label($"[{toggleKey}] Toggle | [{damageKey}] Damage | [{healKey}] Heal", hintStyle);
        }

        private void DrawSectionHeader(string text)
        {
            Rect rect = GUILayoutUtility.GetRect(panelWidth - 40, 18);
            GUI.DrawTexture(rect, _sectionTexture);
            GUI.Label(rect, " " + text, _sectionStyle);
        }

        private void DrawGoalScoreBar(string label, float score, bool isCurrent)
        {
            GUILayout.BeginHorizontal();

            var labelStyle = new GUIStyle(_labelStyle);
            if (isCurrent)
            {
                labelStyle.normal.textColor = Color.yellow;
                labelStyle.fontStyle = FontStyle.Bold;
            }

            GUILayout.Label(label, labelStyle, GUILayout.Width(120));

            Rect barRect = GUILayoutUtility.GetRect(100, 12);
            GUI.DrawTexture(barRect, _barBackgroundTex);

            float fillPercent = Mathf.Clamp01(score);
            Texture2D fillTex = isCurrent ? _goalBarActiveTex : _goalBarTex;
            Rect fillRect = new(barRect.x, barRect.y, barRect.width * fillPercent, barRect.height);
            GUI.DrawTexture(fillRect, fillTex);

            GUILayout.Label($"{score:F2}", _valueStyle, GUILayout.Width(50));
            GUILayout.EndHorizontal();
        }

        private void DrawMiniBar(float percent, bool isHull)
        {
            Rect barRect = GUILayoutUtility.GetRect(60, 12);
            GUI.DrawTexture(barRect, _barBackgroundTex);

            float fillPercent = Mathf.Clamp01(percent);
            Texture2D fillTex = isHull ? _hullBarTex : _shieldBarTex;
            Rect fillRect = new(barRect.x, barRect.y, barRect.width * fillPercent, barRect.height);
            GUI.DrawTexture(fillRect, fillTex);
        }

        private void DrawLabelValue(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", _labelStyle, GUILayout.Width(110));
            GUILayout.Label(value, _valueStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawConnectionLine(Vector2 entityPos, Vector2 panelPos)
        {
            if (Event.current.type != EventType.Repaint) return;

            GL.PushMatrix();
            GL.LoadPixelMatrix();

            GL.Begin(GL.LINES);
            GL.Color(new Color(0.5f, 0.8f, 0.5f, 0.3f));
            GL.Vertex3(entityPos.x, entityPos.y, 0);
            GL.Vertex3(panelPos.x, panelPos.y, 0);
            GL.End();

            GL.PopMatrix();
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
            if (_goalBarTex != null) DestroyImmediate(_goalBarTex);
            if (_goalBarActiveTex != null) DestroyImmediate(_goalBarActiveTex);
            if (_hullBarTex != null) DestroyImmediate(_hullBarTex);
            if (_shieldBarTex != null) DestroyImmediate(_shieldBarTex);
        }
    }
}
#endif
