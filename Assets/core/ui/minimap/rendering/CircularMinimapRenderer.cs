using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// Circular radar-style minimap renderer.
    /// </summary>
    public class CircularMinimapRenderer : IMinimapRenderer
    {
        private MinimapConfig _config;
        private RectTransform _container;
        private float _radius;
        private float _displayRange;

        // UI elements
        private GameObject _backgroundObj;
        private Image _backgroundImage;
        private Image _borderImage;
        private RectTransform _playerIconTransform;
        private Image _playerIconImage;
        private List<Image> _rangeRings = new();
        private Image _sweepImage;
        private Image _staleIndicator;
        private RectTransform _blipContainer;

        // Circular clipping material (shader-based)
        private Material _circularClipMaterial;

        // Sweep state
        private float _sweepAngle;

        public bool IsInitialized { get; private set; }
        public float DisplayRange => _displayRange;
        public RectTransform BlipContainer => _blipContainer;

        public void Initialize(MinimapConfig config, RectTransform container)
        {
            _config = config;
            _container = container;
            _radius = config.Style.Radius;
            _displayRange = config.UseSensorRange ? 100f : config.CustomRange; // Will be updated

            SetupContainer();
            CreateBackground();
            CreateMask();
            CreateBlipContainer();
            CreateRangeRings();
            CreatePlayerIcon();
            CreateSweepEffect();
            CreateStaleIndicator();
            CreateBorder();

            IsInitialized = true;
        }

        private void SetupContainer()
        {
            var style = _config.Style;

            Vector2 anchor, pivot, offset;

            switch (style.AnchorPreset)
            {
                case MinimapAnchorPreset.TopRight:
                    anchor = pivot = new Vector2(1f, 1f);
                    offset = new Vector2(-style.Margin.x, -style.Margin.y);
                    break;
                case MinimapAnchorPreset.TopLeft:
                    anchor = pivot = new Vector2(0f, 1f);
                    offset = new Vector2(style.Margin.x, -style.Margin.y);
                    break;
                case MinimapAnchorPreset.BottomRight:
                    anchor = pivot = new Vector2(1f, 0f);
                    offset = new Vector2(-style.Margin.x, style.Margin.y);
                    break;
                case MinimapAnchorPreset.BottomLeft:
                    anchor = pivot = new Vector2(0f, 0f);
                    offset = new Vector2(style.Margin.x, style.Margin.y);
                    break;
                default: // Custom
                    anchor = style.ScreenPosition;
                    pivot = new Vector2(0.5f, 0.5f);
                    offset = style.ScreenOffset;
                    break;
            }

            _container.anchorMin = anchor;
            _container.anchorMax = anchor;
            _container.pivot = pivot;
            _container.anchoredPosition = offset;
            _container.sizeDelta = new Vector2(_radius * 2, _radius * 2);
        }

        private void CreateBackground()
        {
            var style = _config.Style;

            _backgroundObj = new GameObject("Background");
            _backgroundObj.transform.SetParent(_container, false);

            var rect = _backgroundObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _backgroundImage = _backgroundObj.AddComponent<Image>();
            _backgroundImage.sprite = style.BackgroundSprite ?? CreateCircleSprite();
            _backgroundImage.color = style.BackgroundColor;
            _backgroundImage.material = GetCircularClipMaterial();
            _backgroundImage.raycastTarget = false;
        }

        private void CreateMask()
        {
            // No longer using Unity's Mask component - shader handles circular clipping
            // The circular clip material is applied to each element that needs clipping
        }

        private void CreateBlipContainer()
        {
            var blipContainerObj = new GameObject("BlipContainer");
            blipContainerObj.transform.SetParent(_backgroundObj.transform, false);

            _blipContainer = blipContainerObj.AddComponent<RectTransform>();
            _blipContainer.anchorMin = new Vector2(0.5f, 0.5f);
            _blipContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _blipContainer.sizeDelta = Vector2.zero;
            _blipContainer.anchoredPosition = Vector2.zero;
        }

        private void CreateRangeRings()
        {
            var style = _config.Style;
            if (!style.ShowRangeRings) return;

            _rangeRings.Clear();

            for (int i = 1; i <= style.RangeRingCount; i++)
            {
                float ringRadius = _radius * ((float)i / (style.RangeRingCount + 1));

                var ringObj = new GameObject($"RangeRing_{i}");
                ringObj.transform.SetParent(_backgroundObj.transform, false);

                var rect = ringObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(ringRadius * 2, ringRadius * 2);
                rect.anchoredPosition = Vector2.zero;

                var ring = ringObj.AddComponent<Image>();
                ring.sprite = CreateRingSprite(style.RangeRingWidth / ringRadius);
                ring.color = style.RangeRingColor;
                ring.material = GetCircularClipMaterial();
                ring.raycastTarget = false;

                _rangeRings.Add(ring);
            }
        }

        private void CreatePlayerIcon()
        {
            var style = _config.Style;

            var playerObj = new GameObject("PlayerIcon");
            playerObj.transform.SetParent(_backgroundObj.transform, false);

            _playerIconTransform = playerObj.AddComponent<RectTransform>();
            _playerIconTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _playerIconTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _playerIconTransform.sizeDelta = new Vector2(style.PlayerIconSize, style.PlayerIconSize);
            _playerIconTransform.anchoredPosition = Vector2.zero;

            _playerIconImage = playerObj.AddComponent<Image>();
            _playerIconImage.sprite = style.PlayerIcon ?? CreateCircleSprite();
            _playerIconImage.color = style.PlayerColor;
            _playerIconImage.raycastTarget = false;
        }

        private void CreateSweepEffect()
        {
            var style = _config.Style;
            if (!style.ShowSweepEffect) return;

            var sweepObj = new GameObject("Sweep");
            sweepObj.transform.SetParent(_backgroundObj.transform, false);

            var rect = sweepObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            _sweepImage = sweepObj.AddComponent<Image>();
            _sweepImage.sprite = CreateSweepSprite();
            _sweepImage.color = style.SweepColor;
            _sweepImage.material = GetCircularClipMaterial();
            _sweepImage.raycastTarget = false;
        }

        private void CreateStaleIndicator()
        {
            var style = _config.Style;
            if (!style.ShowStaleDataIndicator) return;

            var staleObj = new GameObject("StaleIndicator");
            staleObj.transform.SetParent(_container, false);

            var rect = staleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(10f, 10f);
            // Position inside the top of the minimap circle
            rect.anchoredPosition = new Vector2(0, _radius - 15f);

            _staleIndicator = staleObj.AddComponent<Image>();
            _staleIndicator.color = style.StaleIndicatorColor;
            _staleIndicator.raycastTarget = false;
            _staleIndicator.enabled = false;
        }

        private void CreateBorder()
        {
            var style = _config.Style;
            if (style.BorderWidth <= 0) return;

            var borderObj = new GameObject("Border");
            borderObj.transform.SetParent(_container, false);

            var rect = borderObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _borderImage = borderObj.AddComponent<Image>();
            _borderImage.sprite = style.BorderSprite ?? CreateRingSprite(style.BorderWidth / _radius);
            _borderImage.color = style.BorderColor;
            _borderImage.material = GetCircularClipMaterial();
            _borderImage.raycastTarget = false;
        }

        public void SetDisplayRange(float range)
        {
            _displayRange = range;
        }

        public Vector2 WorldToMinimapPosition(
            Vector2 relativeWorldPos,
            float sourceRotation,
            MinimapOrientationMode orientation)
        {
            if (_displayRange <= 0) return Vector2.zero;

            // Normalize to -1 to 1 range based on display range
            Vector2 normalized = relativeWorldPos / _displayRange;

            // Apply rotation based on orientation mode
            if (orientation == MinimapOrientationMode.ShipUp)
            {
                // Rotate so ship's forward (up) stays at top of minimap
                float rotRad = (-sourceRotation + 90f) * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rotRad);
                float sin = Mathf.Sin(rotRad);
                normalized = new Vector2(
                    normalized.x * cos - normalized.y * sin,
                    normalized.x * sin + normalized.y * cos);
            }

            // Scale to pixel space (radius)
            return normalized * _radius;
        }

        public bool IsInBounds(Vector2 minimapPos)
        {
            return minimapPos.magnitude <= _radius;
        }

        public Vector2 ApplyEdgeBehavior(
            Vector2 minimapPos,
            MinimapEdgeBehavior behavior,
            out float edgeFactor)
        {
            float distance = minimapPos.magnitude;

            if (distance <= _radius)
            {
                edgeFactor = 1f;
                return minimapPos;
            }

            switch (behavior)
            {
                case MinimapEdgeBehavior.ClampToEdge:
                    edgeFactor = 1f;
                    // Clamp to just inside edge to leave room for indicator
                    return minimapPos.normalized * (_radius - 8f);

                case MinimapEdgeBehavior.FadeAtEdge:
                    // Fade from 80% to 100% of radius
                    float fadeStart = _radius * 0.8f;
                    edgeFactor = 1f - Mathf.InverseLerp(fadeStart, _radius * 1.2f, distance);
                    return minimapPos.normalized * Mathf.Min(distance, _radius - 2f);

                case MinimapEdgeBehavior.HardCutoff:
                default:
                    edgeFactor = 0f;
                    return minimapPos;
            }
        }

        public void UpdatePlayerRotation(float rotation)
        {
            if (_playerIconTransform != null && _config.OrientationMode == MinimapOrientationMode.NorthUp)
            {
                // In NorthUp mode, rotate player icon to show facing direction
                // Ship uses 0° = facing up convention, matching UI coordinate space
                _playerIconTransform.localRotation = Quaternion.Euler(0, 0, rotation);
            }
            else if (_playerIconTransform != null)
            {
                // In ShipUp mode, player icon always points up
                _playerIconTransform.localRotation = Quaternion.identity;
            }
        }

        public void UpdateVisuals(float deltaTime, float sensorPollingRate)
        {
            if (_sweepImage != null && _config.Style.ShowSweepEffect)
            {
                float speed;
                if (_config.Style.SyncSweepToSensor && sensorPollingRate > 0)
                {
                    // One full rotation per polling cycle
                    speed = 360f / sensorPollingRate;
                }
                else
                {
                    speed = _config.Style.SweepSpeed * 360f;
                }

                _sweepAngle += speed * deltaTime;
                _sweepAngle %= 360f;

                _sweepImage.transform.localRotation = Quaternion.Euler(0, 0, -_sweepAngle);
            }
        }

        public void SetStaleIndicatorVisible(bool visible)
        {
            if (_staleIndicator != null)
            {
                _staleIndicator.enabled = visible;
            }
        }

        public void Dispose()
        {
            if (_backgroundObj != null)
            {
                Object.Destroy(_backgroundObj);
            }
            if (_borderImage != null)
            {
                Object.Destroy(_borderImage.gameObject);
            }
            if (_staleIndicator != null)
            {
                Object.Destroy(_staleIndicator.gameObject);
            }
            if (_circularClipMaterial != null)
            {
                Object.Destroy(_circularClipMaterial);
                _circularClipMaterial = null;
            }

            _rangeRings.Clear();
            IsInitialized = false;
        }

        #region Material Management

        private Material GetCircularClipMaterial()
        {
            if (_circularClipMaterial == null)
            {
                var shader = Shader.Find("Starfire/UI/MinimapCircularClip");
                if (shader != null)
                {
                    _circularClipMaterial = new Material(shader);
                }
                else
                {
                    Debug.LogWarning("MinimapCircularClip shader not found, using default UI shader");
                    _circularClipMaterial = new Material(Shader.Find("UI/Default"));
                }
            }
            return _circularClipMaterial;
        }

        #endregion

        #region Sprite Generation

        private Sprite CreateCircleSprite()
        {
            int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.alphaIsTransparency = true;
            float center = size / 2f;
            float radiusSqr = center * center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float distSqr = dx * dx + dy * dy;

                    // Smooth edge with anti-aliasing
                    float alpha = Mathf.Clamp01((center - Mathf.Sqrt(distSqr)) * 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateRingSprite(float thickness)
        {
            int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.alphaIsTransparency = true;
            float center = size / 2f;
            float outerRadius = center;
            float innerRadius = center * (1f - thickness);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 0f;
                    if (dist <= outerRadius && dist >= innerRadius)
                    {
                        // Anti-aliased edges
                        float outerEdge = Mathf.Clamp01((outerRadius - dist) * 2f);
                        float innerEdge = Mathf.Clamp01((dist - innerRadius) * 2f);
                        alpha = Mathf.Min(outerEdge, innerEdge);
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateTriangleSprite()
        {
            int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.alphaIsTransparency = true;

            // Clear
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    texture.SetPixel(x, y, Color.clear);

            // Draw filled triangle pointing up
            Vector2 p0 = new Vector2(size / 2f, size - 4);   // Top
            Vector2 p1 = new Vector2(4, 4);                   // Bottom left
            Vector2 p2 = new Vector2(size - 4, 4);            // Bottom right

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (PointInTriangle(p, p0, p1, p2))
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateSweepSprite()
        {
            int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.alphaIsTransparency = true;
            float center = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > center)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Get angle (0-360)
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    // Sweep line is at 90 degrees (pointing up), with trail behind
                    float sweepAngle = 90f;
                    float trailLength = 60f;

                    float angleDiff = Mathf.DeltaAngle(angle, sweepAngle);
                    float alpha = 0f;

                    if (angleDiff >= 0 && angleDiff <= trailLength)
                    {
                        // Trail fades from sweep line backward
                        alpha = 1f - (angleDiff / trailLength);
                        alpha *= alpha; // Quadratic falloff
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            float s = (p0.x - p2.x) * (p.y - p2.y) - (p0.y - p2.y) * (p.x - p2.x);
            float t = (p1.x - p0.x) * (p.y - p0.y) - (p1.y - p0.y) * (p.x - p0.x);

            if ((s < 0) != (t < 0) && s != 0 && t != 0)
                return false;

            float d = (p2.x - p1.x) * (p.y - p1.y) - (p2.y - p1.y) * (p.x - p1.x);
            return d == 0 || (d < 0) == (s + t <= 0);
        }

        #endregion
    }
}
