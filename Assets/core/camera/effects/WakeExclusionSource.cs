using UnityEngine;

namespace Starfire.Core.Cam.Effects
{
    /// <summary>
    /// Attach this component to any entity that should be excluded from gravitational wake distortion.
    /// The component calculates the entity's world-space radius from its SpriteRenderer bounds.
    /// The V3 CameraController uses this to create a screen-space exclusion zone in the wake shader.
    /// </summary>
    public class WakeExclusionSource : MonoBehaviour
    {
        [Tooltip("Additional padding multiplier applied to the calculated radius (1.0 = exact bounds)")]
        [SerializeField] private float radiusPadding = 1.1f;

        [Tooltip("Manual override for world radius. Set to 0 to auto-calculate from sprite bounds.")]
        [SerializeField] private float manualRadius = 0f;

        private SpriteRenderer _spriteRenderer;
        private float _calculatedRadius;

        public float WorldRadius => manualRadius > 0f ? manualRadius : _calculatedRadius * radiusPadding;

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            CalculateRadius();
        }

        private void CalculateRadius()
        {
            if (_spriteRenderer != null && _spriteRenderer.sprite != null)
            {
                Bounds bounds = _spriteRenderer.bounds;
                _calculatedRadius = Mathf.Max(bounds.extents.x, bounds.extents.y);
            }
            else
            {
                _calculatedRadius = 1f;
            }
        }

        public void RecalculateRadius()
        {
            CalculateRadius();
        }
    }
}
