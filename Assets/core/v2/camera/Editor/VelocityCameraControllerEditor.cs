using UnityEditor;
using UnityEngine;

namespace Starfire.Core.V2.Cam.Editor
{
    [CustomEditor(typeof(VelocityCameraController))]
    public class VelocityCameraControllerEditor : UnityEditor.Editor
    {
        private VelocityCameraController _controller;

        private void OnEnable()
        {
            _controller = (VelocityCameraController)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Debug Info", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                if (_controller.Follower != null)
                {
                    EditorGUILayout.LabelField("Following State", EditorStyles.miniBoldLabel);

                    float trackingFactor = _controller.GetTrackingFactor();
                    string trackingMode = trackingFactor < 0.01f ? "Low Speed (SmoothDamp)"
                        : trackingFactor > 0.99f ? "High Speed (Velocity Tracking)"
                        : "Blend Mode";

                    EditorGUILayout.TextField("Tracking Mode", trackingMode);

                    Rect trackingRect = EditorGUILayout.GetControlRect(false, 20);
                    trackingRect.x += EditorGUIUtility.labelWidth;
                    trackingRect.width -= EditorGUIUtility.labelWidth;

                    EditorGUI.ProgressBar(trackingRect, trackingFactor, $"Tracking Factor: {trackingFactor:F2}");

                    EditorGUILayout.Vector2Field("Camera Position", _controller.Follower.CurrentPosition);
                    EditorGUILayout.Vector2Field("Camera Velocity", _controller.Follower.CurrentVelocity);
                    EditorGUILayout.Vector2Field("Position Error", _controller.GetPositionError());
                    EditorGUILayout.Vector2Field("Focus Offset", _controller.GetFocusOffset());

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Zoom State", EditorStyles.miniBoldLabel);
                    EditorGUILayout.FloatField("Current Zoom", _controller.GetCurrentZoom());
                    EditorGUILayout.FloatField("User Zoom Level", _controller.GetUserZoomLevel());
                    EditorGUILayout.FloatField("Speed Zoom Delta", _controller.GetSpeedZoomDelta());
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Debug Actions", EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Shake"))
                {
                    _controller.Shake(0.5f);
                }
                if (GUILayout.Button("Punch"))
                {
                    _controller.Punch(Random.insideUnitCircle.normalized, 0.3f);
                }
                if (GUILayout.Button("Snap"))
                {
                    _controller.SnapToTarget();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Flash"))
                {
                    _controller.Flash(Color.white, 0.1f);
                }
                if (GUILayout.Button("Chromatic"))
                {
                    _controller.SetChromaticAberration(0.5f, 0.3f);
                }
                if (GUILayout.Button("Reset Effects"))
                {
                    _controller.ResetEffects();
                }
            }

            Repaint();
        }

        private void OnSceneGUI()
        {
            if (!Application.isPlaying) return;
            if (_controller.Follower == null) return;
            if (_controller.CurrentPreset == null) return;

            var preset = _controller.CurrentPreset;
            Vector2 cameraPos = _controller.Follower.CurrentPosition;
            float trackingFactor = _controller.GetTrackingFactor();

            // Draw velocity tracking thresholds
            Handles.color = new Color(0, 1, 0, 0.3f);
            Handles.DrawWireDisc(cameraPos, Vector3.forward, preset.VelocityTrackingThreshold);

            Handles.color = new Color(1, 0, 0, 0.3f);
            Handles.DrawWireDisc(cameraPos, Vector3.forward, preset.VelocityTrackingFullSpeed);

            // Draw labels
            Handles.Label(
                cameraPos + Vector2.right * (preset.VelocityTrackingThreshold + 0.2f),
                $"Threshold: {preset.VelocityTrackingThreshold:F1}",
                new GUIStyle { normal = { textColor = Color.green } }
            );

            Handles.Label(
                cameraPos + Vector2.right * (preset.VelocityTrackingFullSpeed + 0.2f),
                $"Full Speed: {preset.VelocityTrackingFullSpeed:F1}",
                new GUIStyle { normal = { textColor = Color.red } }
            );

            // Draw tracking mode indicator
            Color modeColor = trackingFactor < 0.01f ? Color.green
                : trackingFactor > 0.99f ? Color.red
                : Color.yellow;

            Handles.color = modeColor;
            Handles.DrawSolidDisc(cameraPos, Vector3.forward, 0.2f);

            // Draw focus offset
            Vector2 focusOffset = _controller.GetFocusOffset();
            if (focusOffset.sqrMagnitude > 0.01f)
            {
                var targetProvider = _controller.GetTargetProvider();
                if (targetProvider != null && targetProvider.HasTargets)
                {
                    Vector2 targetPos = targetProvider.GetTargetPosition();
                    Handles.color = new Color(0, 0.5f, 1, 0.8f);
                    Handles.DrawLine(targetPos, targetPos + focusOffset);
                    Handles.DrawSolidDisc(targetPos + focusOffset, Vector3.forward, 0.1f);
                }
            }

            // Draw position error
            Vector2 posError = _controller.GetPositionError();
            if (posError.sqrMagnitude > 0.1f)
            {
                Handles.color = new Color(1, 0.5f, 0, 0.8f);
                Handles.DrawLine(cameraPos, cameraPos + posError);
            }
        }
    }
}
