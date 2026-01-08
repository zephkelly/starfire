using UnityEditor;
using UnityEngine;

namespace Starfire.Core.Cam.Editor
{
    [CustomEditor(typeof(CameraController))]
    public class CameraControllerEditor : UnityEditor.Editor
    {
        private bool _statusFoldout = true;
        private bool _effectsFoldout = true;
        private bool _zoomFoldout = true;

        // Effect parameters
        private float _shakeIntensity = 0.5f;
        private Vector2 _punchDirection = Vector2.right;
        private float _punchForce = 0.3f;
        private float _punchDuration = 0.15f;
        private float _chromaticIntensity = 0.5f;
        private float _chromaticDuration = 0.3f;
        private float _vignetteIntensity = 0.3f;
        private float _vignetteDuration = 0.5f;
        private Color _flashColor = Color.white;
        private float _flashDuration = 0.1f;

        // Zoom parameters
        private float _targetZoom = 10f;
        private float _zoomTransitionTime = 0.5f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CameraController controller = (CameraController)target;

            EditorGUILayout.Space(10);

            // Only show controls in play mode
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Debug controls are only available in Play Mode.", MessageType.Info);
                return;
            }

            // Status Section
            _statusFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_statusFoldout, "Camera Status");
            if (_statusFoldout)
            {
                EditorGUI.indentLevel++;

                // Update mode info
                EditorGUILayout.LabelField("Update Mode", EditorStyles.boldLabel);

                string configuredMode = controller.UpdateMode.ToString();
                string effectiveMode = controller.EffectiveUpdateMode.ToString();

                EditorGUILayout.LabelField("Configured", configuredMode);
                EditorGUILayout.LabelField("Effective", effectiveMode);

                // Target type info
                var provider = controller.GetTargetProvider();
                if (provider != null && provider.HasTargets)
                {
                    string targetType = provider.GetPrimaryTargetType().ToString();
                    EditorGUILayout.LabelField("Target Type", targetType);
                }
                else
                {
                    EditorGUILayout.LabelField("Target Type", "No Target");
                }

                EditorGUILayout.Space(5);

                // Update mode buttons
                EditorGUILayout.LabelField("Change Mode", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Auto"))
                {
                    controller.SetUpdateMode(CameraUpdateMode.Auto);
                }
                if (GUILayout.Button("LateUpdate"))
                {
                    controller.SetUpdateMode(CameraUpdateMode.LateUpdate);
                }
                if (GUILayout.Button("FixedUpdate"))
                {
                    controller.SetUpdateMode(CameraUpdateMode.FixedUpdate);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(5);

            // Effects Section
            _effectsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_effectsFoldout, "Effect Controls");
            if (_effectsFoldout)
            {
                EditorGUI.indentLevel++;

                // Shake
                EditorGUILayout.LabelField("Screen Shake", EditorStyles.boldLabel);
                _shakeIntensity = EditorGUILayout.Slider("Intensity", _shakeIntensity, 0f, 1f);
                if (GUILayout.Button("Shake"))
                {
                    controller.Shake(_shakeIntensity);
                }

                EditorGUILayout.Space(5);

                // Punch
                EditorGUILayout.LabelField("Directional Punch", EditorStyles.boldLabel);
                _punchDirection = EditorGUILayout.Vector2Field("Direction", _punchDirection);
                _punchForce = EditorGUILayout.Slider("Force", _punchForce, 0f, 1f);
                _punchDuration = EditorGUILayout.Slider("Duration", _punchDuration, 0.05f, 0.5f);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Punch"))
                {
                    controller.Punch(_punchDirection.normalized, _punchForce, _punchDuration);
                }
                if (GUILayout.Button("Random Punch"))
                {
                    Vector2 randomDir = Random.insideUnitCircle.normalized;
                    controller.Punch(randomDir, _punchForce, _punchDuration);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Chromatic Aberration
                EditorGUILayout.LabelField("Chromatic Aberration", EditorStyles.boldLabel);
                _chromaticIntensity = EditorGUILayout.Slider("Intensity", _chromaticIntensity, 0f, 1f);
                _chromaticDuration = EditorGUILayout.Slider("Duration", _chromaticDuration, 0f, 2f);
                if (GUILayout.Button("Apply Chromatic Aberration"))
                {
                    controller.SetChromaticAberration(_chromaticIntensity, _chromaticDuration);
                }

                EditorGUILayout.Space(5);

                // Vignette
                EditorGUILayout.LabelField("Vignette", EditorStyles.boldLabel);
                _vignetteIntensity = EditorGUILayout.Slider("Intensity", _vignetteIntensity, 0f, 1f);
                _vignetteDuration = EditorGUILayout.Slider("Duration", _vignetteDuration, 0f, 2f);
                if (GUILayout.Button("Apply Vignette"))
                {
                    controller.SetVignette(_vignetteIntensity, _vignetteDuration);
                }

                EditorGUILayout.Space(5);

                // Flash
                EditorGUILayout.LabelField("Screen Flash", EditorStyles.boldLabel);
                _flashColor = EditorGUILayout.ColorField("Color", _flashColor);
                _flashDuration = EditorGUILayout.Slider("Duration", _flashDuration, 0.01f, 0.5f);
                if (GUILayout.Button("Flash"))
                {
                    controller.Flash(_flashColor, _flashDuration);
                }

                EditorGUILayout.Space(5);

                // Reset
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("Reset All Effects"))
                {
                    controller.ResetEffects();
                }
                GUI.backgroundColor = Color.white;

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(5);

            // Zoom Section
            _zoomFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_zoomFoldout, "Zoom Controls");
            if (_zoomFoldout)
            {
                EditorGUI.indentLevel++;

                // Current zoom info
                EditorGUILayout.LabelField("Current Zoom", controller.GetCurrentZoom().ToString("F2"));
                EditorGUILayout.LabelField("User Zoom Level", controller.GetUserZoomLevel().ToString("F2"));
                EditorGUILayout.LabelField("Speed Zoom Delta", controller.GetSpeedZoomDelta().ToString("F2"));

                EditorGUILayout.Space(5);

                _targetZoom = EditorGUILayout.Slider("Target Zoom", _targetZoom, 1f, 30f);
                _zoomTransitionTime = EditorGUILayout.Slider("Transition Time", _zoomTransitionTime, 0f, 2f);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Set Zoom"))
                {
                    controller.SetZoom(_targetZoom, _zoomTransitionTime);
                }
                if (GUILayout.Button("Reset Zoom"))
                {
                    controller.ResetZoom(_zoomTransitionTime);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // Force repaint for live values
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
