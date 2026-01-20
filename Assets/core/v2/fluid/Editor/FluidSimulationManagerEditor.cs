using UnityEngine;
using UnityEditor;

namespace StarfireV2.Fluid.Editor
{
    [CustomEditor(typeof(FluidSimulationManager))]
    public class FluidSimulationManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);

            var manager = (FluidSimulationManager)target;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("Initialized", manager.IsInitialized);
            EditorGUILayout.Toggle("Simulation Active", manager.IsSimulationActive);
            EditorGUILayout.IntField("Registered Obstacles", manager.RegisteredObstacleCount);
            EditorGUILayout.IntField("Active Obstacles", manager.ActiveObstacleCount);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Reinitialize"))
                {
                    manager.Reinitialize();
                }
            }

            // Repaint during play mode to show live values
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
