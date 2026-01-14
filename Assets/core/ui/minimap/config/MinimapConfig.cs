using UnityEngine;
using Starfire.Entity.Modules.Sensor;
using Starfire.Entity.Modules.Transponder;

namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// Main configuration for the minimap system.
    /// References style and blip sub-configurations.
    /// </summary>
    [CreateAssetMenu(fileName = "MinimapConfig", menuName = "Starfire/UI/Minimap Config")]
    public class MinimapConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string configId = "default";
        [SerializeField] private string displayName = "Default Minimap";

        [Header("Behavior")]
        [SerializeField] private MinimapOrientationMode orientationMode = MinimapOrientationMode.NorthUp;
        [SerializeField] private MinimapEdgeBehavior edgeBehavior = MinimapEdgeBehavior.HardCutoff;

        [Header("Update Settings")]
        [Tooltip("Use sensor polling rate for updates (true) or custom rate (false)")]
        [SerializeField] private bool useSensorPollingRate = true;

        [Tooltip("Custom update rate in seconds (when not using sensor rate)")]
        [SerializeField] private float customUpdateRate = 0.5f;

        [Tooltip("Smoothly interpolate blip positions between updates")]
        [SerializeField] private bool interpolatePositions = true;

        [Tooltip("Speed of position interpolation (higher = faster snap)")]
        [SerializeField] private float interpolationSpeed = 10f;

        [Header("Range Settings")]
        [Tooltip("Use sensor detection range (true) or custom range (false)")]
        [SerializeField] private bool useSensorRange = true;

        [Tooltip("Custom display range in world units (when not using sensor range)")]
        [SerializeField] private float customRange = 100f;

        [Tooltip("Zoom multiplier (1 = sensor range fills display, 2 = see 2x as far but smaller)")]
        [Range(0.25f, 4f)]
        [SerializeField] private float zoomLevel = 1f;

        [Header("Detection Level Filtering")]
        [Tooltip("Minimum detection level required to display a contact")]
        [SerializeField] private DetectionLevel minimumDisplayLevel = DetectionLevel.Presence;

        [Header("Relationship Filtering")]
        [SerializeField] private bool showHostile = true;
        [SerializeField] private bool showUnfriendly = true;
        [SerializeField] private bool showNeutral = true;
        [SerializeField] private bool showFriendly = true;
        [SerializeField] private bool showAllied = true;
        [SerializeField] private bool showUnknown = true;

        [Header("Sub-Configurations")]
        [SerializeField] private MinimapStyleConfig styleConfig;
        [SerializeField] private MinimapBlipConfig blipConfig;

        // Accessors
        public string ConfigId => configId;
        public string DisplayName => displayName;

        public MinimapOrientationMode OrientationMode => orientationMode;
        public MinimapEdgeBehavior EdgeBehavior => edgeBehavior;

        public bool UseSensorPollingRate => useSensorPollingRate;
        public float CustomUpdateRate => customUpdateRate;
        public bool InterpolatePositions => interpolatePositions;
        public float InterpolationSpeed => interpolationSpeed;

        public bool UseSensorRange => useSensorRange;
        public float CustomRange => customRange;
        public float ZoomLevel => zoomLevel;

        public DetectionLevel MinimumDisplayLevel => minimumDisplayLevel;

        public bool ShowHostile => showHostile;
        public bool ShowUnfriendly => showUnfriendly;
        public bool ShowNeutral => showNeutral;
        public bool ShowFriendly => showFriendly;
        public bool ShowAllied => showAllied;
        public bool ShowUnknown => showUnknown;

        public MinimapStyleConfig Style => styleConfig;
        public MinimapBlipConfig Blips => blipConfig;

        /// <summary>
        /// Checks if a contact with the given relationship should be displayed.
        /// </summary>
        public bool ShouldShowRelationship(FactionRelationType relation)
        {
            return relation switch
            {
                FactionRelationType.Hostile => showHostile,
                FactionRelationType.Unfriendly => showUnfriendly,
                FactionRelationType.Neutral => showNeutral,
                FactionRelationType.Friendly => showFriendly,
                FactionRelationType.Allied => showAllied,
                FactionRelationType.Unknown => showUnknown,
                _ => true
            };
        }

        /// <summary>
        /// Gets the effective update rate based on configuration.
        /// </summary>
        public float GetEffectiveUpdateRate(float sensorPollingRate)
        {
            return useSensorPollingRate ? sensorPollingRate : customUpdateRate;
        }

        /// <summary>
        /// Gets the effective display range based on configuration.
        /// </summary>
        public float GetEffectiveRange(float sensorRange)
        {
            float baseRange = useSensorRange ? sensorRange : customRange;
            return baseRange / zoomLevel;
        }

        private void OnValidate()
        {
            if (styleConfig == null)
            {
                Debug.LogWarning($"[{name}] MinimapConfig is missing StyleConfig reference");
            }
            if (blipConfig == null)
            {
                Debug.LogWarning($"[{name}] MinimapConfig is missing BlipConfig reference");
            }
        }
    }
}
