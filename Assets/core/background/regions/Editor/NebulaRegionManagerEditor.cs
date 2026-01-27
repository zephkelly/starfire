using UnityEngine;
using UnityEditor;

namespace Starfire.Core.Background.Regions.Editor
{
    /// <summary>
    /// Custom editor for NebulaRegionManager that adds scene GUI labels for debug visualization.
    /// </summary>
    [CustomEditor(typeof(NebulaRegionManager))]
    public class NebulaRegionManagerEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var manager = (NebulaRegionManager)target;
            if (manager == null || !manager.ShowDebugGizmos) return;

            var regions = manager.GetAllRegions();
            if (regions == null) return;

            foreach (var region in regions)
            {
                if (region == null || !region.IsActive) continue;

                Vector3 center = new Vector3(region.WorldPosition.x, region.WorldPosition.y, 0);

                // Determine color based on edge behavior
                Color labelColor = region.Config.edgeBehavior switch
                {
                    NebulaEdgeBehavior.SmoothFalloff => new Color(0.4f, 0.8f, 1f),
                    NebulaEdgeBehavior.SharpBoundary => new Color(1f, 0.6f, 0.4f),
                    NebulaEdgeBehavior.InverseFalloff => new Color(1f, 0.5f, 1f),
                    _ => Color.white
                };

                // Create label style
                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = labelColor },
                    fontSize = 11
                };

                // Build label text
                string label = $"{region.Config.edgeBehavior}\n" +
                               $"R: {region.Radius:F0}\n" +
                               $"Falloff: {region.Config.falloffDistance:F0}\n" +
                               $"Power: {region.Config.falloffPower:F1}";

                // Position label above the region
                Vector3 labelPos = center + Vector3.up * (region.Radius + 15f);
                Handles.Label(labelPos, label, labelStyle);
            }
        }
    }
}
