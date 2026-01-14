using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Starfire.Entity.Modules.Sensor;

namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// UI component representing a single contact on the minimap.
    /// Managed by MinimapBlipPool for object reuse.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class MinimapBlip : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Image _mainImage;
        private Image _edgeIndicator;
        private RectTransform _edgeIndicatorTransform;

        private int _entityId;
        private Vector2 _targetPosition;
        private Vector2 _currentPosition;
        private float _interpolationSpeed = 10f;
        private float _baseAlpha = 1f;
        private bool _isInterpolating;

        private Coroutine _pulseCoroutine;
        private Coroutine _fadeCoroutine;

        // Cached fallback sprite for when no icon is configured
        private static Sprite _fallbackSprite;

        public int EntityId => _entityId;
        public bool IsActive { get; private set; }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _mainImage = GetComponent<Image>();

            SetupRectTransform();
            CreateEdgeIndicator();
        }

        private void SetupRectTransform()
        {
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private void CreateEdgeIndicator()
        {
            var indicatorObj = new GameObject("EdgeIndicator");
            indicatorObj.transform.SetParent(transform, false);

            _edgeIndicatorTransform = indicatorObj.AddComponent<RectTransform>();
            _edgeIndicatorTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _edgeIndicatorTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _edgeIndicatorTransform.sizeDelta = new Vector2(12f, 12f);

            _edgeIndicator = indicatorObj.AddComponent<Image>();
            _edgeIndicator.raycastTarget = false;
            _edgeIndicator.enabled = false;
        }

        /// <summary>
        /// Initialize the blip with contact data.
        /// </summary>
        public void Initialize(MinimapContactData contact, MinimapBlipConfig config)
        {
            _entityId = contact.EntityId;
            IsActive = true;

            UpdateAppearance(contact, config);
        }

        /// <summary>
        /// Update blip appearance based on current detection level and relationship.
        /// </summary>
        public void UpdateAppearance(MinimapContactData contact, MinimapBlipConfig config)
        {
            var levelStyle = config.GetStyleForLevel(contact.Level);

            // Determine sprite
            Sprite icon = levelStyle.defaultIcon;

            if (contact.Level >= DetectionLevel.Full && levelStyle.useShipClassIcon && contact.ShipClass?.ClassIcon != null)
            {
                icon = contact.ShipClass.ClassIcon;
            }
            else if (contact.Level >= DetectionLevel.Silhouette && levelStyle.useFactionIcon && contact.Faction?.FactionIcon != null)
            {
                icon = contact.Faction.FactionIcon;
            }

            // Fallback to generated circle sprite if no icon configured
            if (icon == null)
            {
                icon = GetOrCreateFallbackSprite();
            }

            _mainImage.sprite = icon;

            // Determine color
            Color blipColor;
            if (contact.Level >= DetectionLevel.Silhouette && levelStyle.useFactionColor && contact.Faction != null)
            {
                blipColor = contact.Faction.FactionColor;
            }
            else
            {
                blipColor = config.GetColorForRelationship(contact.Relationship);
            }

            _baseAlpha = levelStyle.alphaMultiplier;
            blipColor.a = _baseAlpha;
            _mainImage.color = blipColor;

            // Update edge indicator color to match
            if (_edgeIndicator != null)
            {
                _edgeIndicator.color = blipColor;
            }

            // Set size
            _rectTransform.sizeDelta = new Vector2(levelStyle.size, levelStyle.size);

            // Configure edge indicator
            if (config.EdgeIndicatorSprite != null)
            {
                _edgeIndicator.sprite = config.EdgeIndicatorSprite;
                _edgeIndicatorTransform.sizeDelta = new Vector2(config.EdgeIndicatorSize, config.EdgeIndicatorSize);
            }
        }

        /// <summary>
        /// Set the target position for this blip.
        /// </summary>
        public void UpdatePosition(Vector2 position, bool interpolate)
        {
            _targetPosition = position;
            _isInterpolating = interpolate;

            if (!interpolate)
            {
                _currentPosition = position;
                _rectTransform.anchoredPosition = position;
            }
        }

        /// <summary>
        /// Set interpolation speed.
        /// </summary>
        public void SetInterpolationSpeed(float speed)
        {
            _interpolationSpeed = speed;
        }

        private void Update()
        {
            if (!IsActive || !_isInterpolating) return;

            // Smooth interpolation to target position
            if (Vector2.Distance(_currentPosition, _targetPosition) > 0.1f)
            {
                _currentPosition = Vector2.Lerp(
                    _currentPosition,
                    _targetPosition,
                    Time.deltaTime * _interpolationSpeed);
                _rectTransform.anchoredPosition = _currentPosition;
            }
            else if (_currentPosition != _targetPosition)
            {
                _currentPosition = _targetPosition;
                _rectTransform.anchoredPosition = _currentPosition;
            }
        }

        /// <summary>
        /// Show or hide the blip.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// Set the alpha multiplier for this blip.
        /// </summary>
        public void SetAlpha(float alpha)
        {
            var color = _mainImage.color;
            color.a = _baseAlpha * alpha;
            _mainImage.color = color;

            if (_edgeIndicator != null)
            {
                var edgeColor = _edgeIndicator.color;
                edgeColor.a = _baseAlpha * alpha;
                _edgeIndicator.color = edgeColor;
            }
        }

        /// <summary>
        /// Switch between normal blip display and edge indicator mode.
        /// </summary>
        public void ShowAsEdgeIndicator(bool asEdge, Vector2 direction)
        {
            _mainImage.enabled = !asEdge;
            _edgeIndicator.enabled = asEdge;

            if (asEdge && direction != Vector2.zero)
            {
                // Rotate indicator to point toward actual position
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                _edgeIndicatorTransform.localRotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }

        /// <summary>
        /// Play pulse animation for new contacts.
        /// </summary>
        public void PlayPulseAnimation(float duration, float scale)
        {
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(PulseRoutine(duration, scale));
        }

        /// <summary>
        /// Play fade out animation for lost contacts.
        /// </summary>
        public void PlayFadeOutAnimation(float duration, Action onComplete)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOutRoutine(duration, onComplete));
        }

        private IEnumerator PulseRoutine(float duration, float maxScale)
        {
            Vector2 originalSize = _rectTransform.sizeDelta;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Pulse up then back down
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * (maxScale - 1f);
                _rectTransform.sizeDelta = originalSize * scale;
                yield return null;
            }

            _rectTransform.sizeDelta = originalSize;
            _pulseCoroutine = null;
        }

        private IEnumerator FadeOutRoutine(float duration, Action onComplete)
        {
            float elapsed = 0f;
            Color startColor = _mainImage.color;
            Color edgeStartColor = _edgeIndicator != null ? _edgeIndicator.color : Color.clear;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float alpha = Mathf.Lerp(1f, 0f, t);

                var color = startColor;
                color.a = startColor.a * alpha;
                _mainImage.color = color;

                if (_edgeIndicator != null)
                {
                    var edgeColor = edgeStartColor;
                    edgeColor.a = edgeStartColor.a * alpha;
                    _edgeIndicator.color = edgeColor;
                }

                yield return null;
            }

            _fadeCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Reset the blip for reuse in the pool.
        /// </summary>
        public void Reset()
        {
            IsActive = false;
            _entityId = 0;
            _baseAlpha = 1f;
            _isInterpolating = false;

            _mainImage.sprite = null;
            _mainImage.color = Color.white;
            _rectTransform.sizeDelta = Vector2.one * 8f;
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.localScale = Vector3.one;

            _currentPosition = Vector2.zero;
            _targetPosition = Vector2.zero;

            ShowAsEdgeIndicator(false, Vector2.zero);
            SetVisible(false);

            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }

        /// <summary>
        /// Gets or creates a fallback circle sprite for blips without configured icons.
        /// </summary>
        private static Sprite GetOrCreateFallbackSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;

            int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = center - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            _fallbackSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _fallbackSprite;
        }
    }
}
