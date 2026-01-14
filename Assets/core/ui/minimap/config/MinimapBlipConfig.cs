using System;
using UnityEngine;
using Starfire.Entity.Modules.Sensor;
using Starfire.Entity.Modules.Transponder;

namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// Configuration for minimap blip appearance based on detection level and relationship.
    /// </summary>
    [CreateAssetMenu(fileName = "MinimapBlipConfig", menuName = "Starfire/UI/Minimap Blip Config")]
    public class MinimapBlipConfig : ScriptableObject
    {
        [Header("Detection Level Visuals")]
        [SerializeField] private BlipLevelStyle presenceStyle = new()
        {
            size = 6f,
            alphaMultiplier = 0.7f,
            useFactionColor = false,
            useFactionIcon = false,
            useShipClassIcon = false
        };

        [SerializeField] private BlipLevelStyle silhouetteStyle = new()
        {
            size = 8f,
            alphaMultiplier = 0.85f,
            useFactionColor = true,
            useFactionIcon = false,
            useShipClassIcon = false
        };

        [SerializeField] private BlipLevelStyle fullStyle = new()
        {
            size = 10f,
            alphaMultiplier = 1f,
            useFactionColor = true,
            useFactionIcon = true,
            useShipClassIcon = true
        };

        [Header("Relationship Colors")]
        [SerializeField] private Color hostileColor = new(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color unfriendlyColor = new(1f, 0.5f, 0.2f, 1f);
        [SerializeField] private Color neutralColor = new(1f, 1f, 0.2f, 1f);
        [SerializeField] private Color friendlyColor = new(0.2f, 1f, 0.5f, 1f);
        [SerializeField] private Color alliedColor = new(0.2f, 0.5f, 1f, 1f);
        [SerializeField] private Color unknownColor = new(0.5f, 0.5f, 0.5f, 1f);

        [Header("Animation")]
        [SerializeField] private bool pulseNewContacts = true;
        [SerializeField] private float pulseDuration = 0.5f;
        [SerializeField] private float pulseScale = 1.5f;
        [SerializeField] private bool fadeOnLoss = true;
        [SerializeField] private float fadeOutDuration = 0.3f;

        [Header("Edge Indicators")]
        [SerializeField] private Sprite edgeIndicatorSprite;
        [SerializeField] private float edgeIndicatorSize = 12f;

        // Accessors
        public BlipLevelStyle PresenceStyle => presenceStyle;
        public BlipLevelStyle SilhouetteStyle => silhouetteStyle;
        public BlipLevelStyle FullStyle => fullStyle;

        public Color HostileColor => hostileColor;
        public Color UnfriendlyColor => unfriendlyColor;
        public Color NeutralColor => neutralColor;
        public Color FriendlyColor => friendlyColor;
        public Color AlliedColor => alliedColor;
        public Color UnknownColor => unknownColor;

        public bool PulseNewContacts => pulseNewContacts;
        public float PulseDuration => pulseDuration;
        public float PulseScale => pulseScale;
        public bool FadeOnLoss => fadeOnLoss;
        public float FadeOutDuration => fadeOutDuration;

        public Sprite EdgeIndicatorSprite => edgeIndicatorSprite;
        public float EdgeIndicatorSize => edgeIndicatorSize;

        public BlipLevelStyle GetStyleForLevel(DetectionLevel level)
        {
            return level switch
            {
                DetectionLevel.Presence => presenceStyle,
                DetectionLevel.Silhouette => silhouetteStyle,
                DetectionLevel.Full => fullStyle,
                _ => presenceStyle
            };
        }

        public Color GetColorForRelationship(FactionRelationType relation)
        {
            return relation switch
            {
                FactionRelationType.Hostile => hostileColor,
                FactionRelationType.Unfriendly => unfriendlyColor,
                FactionRelationType.Neutral => neutralColor,
                FactionRelationType.Friendly => friendlyColor,
                FactionRelationType.Allied => alliedColor,
                _ => unknownColor
            };
        }
    }

    /// <summary>
    /// Visual style configuration for a specific detection level.
    /// </summary>
    [Serializable]
    public class BlipLevelStyle
    {
        [Tooltip("Default icon for this detection level (used when faction/class icons unavailable)")]
        public Sprite defaultIcon;

        [Tooltip("Size of the blip in pixels")]
        public float size = 8f;

        [Tooltip("Use faction color for blip tint (requires Silhouette+ detection)")]
        public bool useFactionColor = false;

        [Tooltip("Use faction icon if available (requires Silhouette+ detection)")]
        public bool useFactionIcon = false;

        [Tooltip("Use ship class icon if available (requires Full detection)")]
        public bool useShipClassIcon = false;

        [Tooltip("Alpha multiplier for this detection level")]
        [Range(0f, 1f)]
        public float alphaMultiplier = 1f;
    }
}
