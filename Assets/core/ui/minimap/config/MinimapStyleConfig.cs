using UnityEngine;

namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// Visual styling configuration for the minimap display.
    /// </summary>
    [CreateAssetMenu(fileName = "MinimapStyleConfig", menuName = "Starfire/UI/Minimap Style Config")]
    public class MinimapStyleConfig : ScriptableObject
    {
        [Header("Dimensions")]
        [Tooltip("Radius of the circular minimap in pixels")]
        [SerializeField] private float radius = 100f;

        [Tooltip("Screen anchor position (0-1 range, where 1,1 is top-right)")]
        [SerializeField] private Vector2 screenPosition = new(0.95f, 0.95f);

        [Tooltip("Pixel offset from anchor position")]
        [SerializeField] private Vector2 screenOffset = new(-10f, -10f);

        [Header("Background")]
        [Tooltip("Background sprite (if null, uses solid color)")]
        [SerializeField] private Sprite backgroundSprite;

        [SerializeField] private Color backgroundColor = new(0f, 0.1f, 0.15f, 0.8f);

        [Tooltip("Border sprite (if null, uses procedural circle)")]
        [SerializeField] private Sprite borderSprite;

        [SerializeField] private Color borderColor = new(0.2f, 0.6f, 0.8f, 1f);
        [SerializeField] private float borderWidth = 3f;

        [Header("Range Rings")]
        [SerializeField] private bool showRangeRings = true;
        [Range(1, 5)]
        [SerializeField] private int rangeRingCount = 2;
        [SerializeField] private Color rangeRingColor = new(0.3f, 0.5f, 0.6f, 0.3f);
        [SerializeField] private float rangeRingWidth = 1f;

        [Header("Player Icon")]
        [Tooltip("Icon for the player at center (if null, uses default triangle)")]
        [SerializeField] private Sprite playerIcon;

        [SerializeField] private Color playerColor = Color.white;
        [SerializeField] private float playerIconSize = 16f;
        [SerializeField] private bool showPlayerDirectionIndicator = true;

        [Header("Cardinal Markers")]
        [Tooltip("Show N/E/S/W markers (useful for NorthUp orientation)")]
        [SerializeField] private bool showCardinalMarkers = false;

        [SerializeField] private Color cardinalMarkerColor = new(0.5f, 0.5f, 0.5f, 0.5f);

        [Header("Sweep Effect")]
        [Tooltip("Show rotating sweep line for radar effect")]
        [SerializeField] private bool showSweepEffect = true;

        [SerializeField] private Color sweepColor = new(0.3f, 0.8f, 1f, 0.2f);
        [Tooltip("Sweep speed synced to sensor polling rate when true")]
        [SerializeField] private bool syncSweepToSensor = true;
        [Tooltip("Manual sweep rotations per second (when not synced)")]
        [SerializeField] private float sweepSpeed = 0.5f;

        [Header("Stale Data Indicator")]
        [Tooltip("Visual indicator when sensor data is old")]
        [SerializeField] private bool showStaleDataIndicator = true;

        [SerializeField] private Color staleIndicatorColor = new(1f, 0.5f, 0f, 0.8f);
        [Tooltip("Show stale indicator when time since update exceeds (polling rate * this multiplier)")]
        [SerializeField] private float staleThresholdMultiplier = 2f;

        // Accessors
        public float Radius => radius;
        public Vector2 ScreenPosition => screenPosition;
        public Vector2 ScreenOffset => screenOffset;

        public Sprite BackgroundSprite => backgroundSprite;
        public Color BackgroundColor => backgroundColor;
        public Sprite BorderSprite => borderSprite;
        public Color BorderColor => borderColor;
        public float BorderWidth => borderWidth;

        public bool ShowRangeRings => showRangeRings;
        public int RangeRingCount => rangeRingCount;
        public Color RangeRingColor => rangeRingColor;
        public float RangeRingWidth => rangeRingWidth;

        public Sprite PlayerIcon => playerIcon;
        public Color PlayerColor => playerColor;
        public float PlayerIconSize => playerIconSize;
        public bool ShowPlayerDirectionIndicator => showPlayerDirectionIndicator;

        public bool ShowCardinalMarkers => showCardinalMarkers;
        public Color CardinalMarkerColor => cardinalMarkerColor;

        public bool ShowSweepEffect => showSweepEffect;
        public Color SweepColor => sweepColor;
        public bool SyncSweepToSensor => syncSweepToSensor;
        public float SweepSpeed => sweepSpeed;

        public bool ShowStaleDataIndicator => showStaleDataIndicator;
        public Color StaleIndicatorColor => staleIndicatorColor;
        public float StaleThresholdMultiplier => staleThresholdMultiplier;
    }
}
