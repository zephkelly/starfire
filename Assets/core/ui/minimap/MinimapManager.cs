using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Starfire.Entity;
using Starfire.Entity.Modules.Sensor;
using Starfire.Entity.Modules.Transponder;

namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// Main orchestrator for the minimap system.
    /// Coordinates data provider, renderer, and blip pool to display contacts.
    /// </summary>
    public class MinimapManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private MinimapConfig config;

        [Header("Target")]
        [Tooltip("If null, will search for player entity on Start")]
        [SerializeField] private EntityControllerBase targetEntity;

        [Header("Canvas")]
        [Tooltip("If null, will create a new canvas")]
        [SerializeField] private Canvas targetCanvas;

        private IMinimapDataProvider _dataProvider;
        private SensorMinimapDataProvider _sensorProvider; // Keep typed reference for disposal
        private IMinimapRenderer _renderer;
        private MinimapBlipPool _blipPool;
        private Dictionary<int, MinimapBlip> _activeBlips = new();
        private HashSet<int> _blipsToRemove = new();

        private RectTransform _minimapContainer;
        private bool _isInitialized;
        private bool _initializationAttempted;
        private ISensorModule _playerSensor;
        private ITransponderModule _playerTransponder;
        private float _lastDataRefreshTime;

        public bool IsInitialized => _isInitialized;
        public MinimapConfig Config => config;

        private void Start()
        {
            // Initialization moved to Update() to ensure other scripts have initialized first
        }

        /// <summary>
        /// Initialize the minimap system.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            if (config == null)
            {
                Debug.LogError("MinimapManager: No MinimapConfig assigned!");
                return;
            }

            if (config.Style == null)
            {
                Debug.LogError("MinimapManager: MinimapConfig has no StyleConfig!");
                return;
            }

            if (config.Blips == null)
            {
                Debug.LogError("MinimapManager: MinimapConfig has no BlipConfig!");
                return;
            }

            // Find player if not assigned
            if (targetEntity == null)
            {
                targetEntity = FindPlayerEntity();
            }

            if (targetEntity == null)
            {
                Debug.LogWarning("MinimapManager: No target entity found. Minimap disabled.");
                return;
            }

            // Get sensor and transponder
            _playerSensor = targetEntity.Systems?.GetAllModulesOfType<ISensorModule>().FirstOrDefault();
            _playerTransponder = targetEntity.Systems?.GetAllModulesOfType<ITransponderModule>().FirstOrDefault();

            if (_playerSensor == null)
            {
                Debug.LogWarning("MinimapManager: Target entity has no sensor module. Minimap disabled.");
                return;
            }

            // Setup canvas
            EnsureCanvas();

            // Create minimap container
            CreateMinimapContainer();

            // Initialize data provider
            _sensorProvider = new SensorMinimapDataProvider(
                _playerSensor,
                targetEntity,
                _playerTransponder?.Faction);
            _dataProvider = _sensorProvider;
            _dataProvider.OnDataUpdated += OnSensorDataUpdated;

            // Initialize renderer
            _renderer = new CircularMinimapRenderer();
            _renderer.Initialize(config, _minimapContainer);
            _renderer.SetDisplayRange(config.GetEffectiveRange(_playerSensor.DetectionRange));

            // Initialize blip pool
            _blipPool = new MinimapBlipPool(_renderer.BlipContainer, initialSize: 20);

            // Subscribe to sensor events for reactive updates
            _playerSensor.OnEntityDetected += OnEntityDetected;
            _playerSensor.OnEntityLost += OnEntityLost;

            // Force an immediate sensor poll to ensure initial data is available
            _playerSensor.RefreshNow();

            // Initial data refresh
            _dataProvider.Refresh();

            _isInitialized = true;
            _lastDataRefreshTime = Time.time;

            Debug.Log($"MinimapManager: Initialized with sensor range {_playerSensor.DetectionRange}, " +
                      $"detected entities: {_playerSensor.DetectedCount}, " +
                      $"contacts: {_dataProvider.Contacts.Count}");
        }

        private void Update()
        {
            // Delayed initialization - wait one frame for other scripts to initialize
            if (!_initializationAttempted)
            {
                _initializationAttempted = true;
                Initialize();
            }

            if (!_isInitialized || _dataProvider == null) return;

            float deltaTime = Time.deltaTime;

            // Periodic data refresh based on sensor polling rate
            float refreshInterval = _dataProvider.UpdateInterval;
            if (Time.time - _lastDataRefreshTime >= refreshInterval)
            {
                _lastDataRefreshTime = Time.time;
                _dataProvider.Refresh();
            }

            // Update blip positions
            UpdateBlipPositions();

            // Update player icon rotation
            float playerRotation = targetEntity.transform.eulerAngles.z;
            _renderer.UpdatePlayerRotation(playerRotation);

            // Update visual effects (sweep, etc.)
            _renderer.UpdateVisuals(deltaTime, _dataProvider.UpdateInterval);

            // Update stale indicator
            UpdateStaleIndicator();
        }

        private void UpdateBlipPositions()
        {
            float sourceRotation = targetEntity.transform.eulerAngles.z;

            foreach (var contact in _dataProvider.Contacts)
            {
                if (!ShouldDisplayContact(contact)) continue;

                if (_activeBlips.TryGetValue(contact.EntityId, out var blip))
                {
                    Vector2 minimapPos = _renderer.WorldToMinimapPosition(
                        contact.RelativePosition,
                        sourceRotation,
                        config.OrientationMode);

                    if (_renderer.IsInBounds(minimapPos))
                    {
                        blip.UpdatePosition(minimapPos, config.InterpolatePositions);
                        blip.ShowAsEdgeIndicator(false, Vector2.zero);
                        blip.SetVisible(true);
                    }
                    else
                    {
                        HandleOutOfBoundsBlip(blip, minimapPos, contact);
                    }
                }
            }
        }

        private void HandleOutOfBoundsBlip(MinimapBlip blip, Vector2 minimapPos, MinimapContactData contact)
        {
            switch (config.EdgeBehavior)
            {
                case MinimapEdgeBehavior.HardCutoff:
                    blip.SetVisible(false);
                    break;

                case MinimapEdgeBehavior.ClampToEdge:
                    var clampedPos = _renderer.ApplyEdgeBehavior(
                        minimapPos,
                        config.EdgeBehavior,
                        out _);
                    blip.UpdatePosition(clampedPos, config.InterpolatePositions);
                    blip.ShowAsEdgeIndicator(true, minimapPos.normalized);
                    blip.SetAlpha(1f);
                    blip.SetVisible(true);
                    break;

                case MinimapEdgeBehavior.FadeAtEdge:
                    var fadedPos = _renderer.ApplyEdgeBehavior(
                        minimapPos,
                        config.EdgeBehavior,
                        out float edgeFactor);
                    blip.UpdatePosition(fadedPos, config.InterpolatePositions);
                    blip.ShowAsEdgeIndicator(false, Vector2.zero);
                    blip.SetAlpha(edgeFactor);
                    blip.SetVisible(edgeFactor > 0.01f);
                    break;
            }
        }

        private bool ShouldDisplayContact(MinimapContactData contact)
        {
            if (!contact.IsValid) return false;
            if (contact.Level < config.MinimumDisplayLevel) return false;
            if (!config.ShouldShowRelationship(contact.Relationship)) return false;
            return true;
        }

        private void OnSensorDataUpdated()
        {
            RefreshBlips();
        }

        private void OnEntityDetected(DetectedEntity entity)
        {
            var contact = CreateContactData(entity);
            if (ShouldDisplayContact(contact))
            {
                CreateBlipForContact(contact);
            }
        }

        private void OnEntityLost(DetectedEntity entity)
        {
            if (entity.Controller != null)
            {
                int entityId = entity.Controller.GetInstanceID();
                RemoveBlip(entityId);
            }
        }

        private void RefreshBlips()
        {
            // Track which blips should be removed
            _blipsToRemove.Clear();
            foreach (var kvp in _activeBlips)
            {
                _blipsToRemove.Add(kvp.Key);
            }

            // Process current contacts
            foreach (var contact in _dataProvider.Contacts)
            {
                if (!ShouldDisplayContact(contact)) continue;

                _blipsToRemove.Remove(contact.EntityId);

                if (!_activeBlips.ContainsKey(contact.EntityId))
                {
                    CreateBlipForContact(contact);
                }
                else
                {
                    UpdateBlipAppearance(_activeBlips[contact.EntityId], contact);
                }
            }

            // Remove stale blips
            foreach (int entityId in _blipsToRemove)
            {
                RemoveBlip(entityId);
            }
        }

        private void CreateBlipForContact(MinimapContactData contact)
        {
            if (_activeBlips.ContainsKey(contact.EntityId)) return;

            var blip = _blipPool.Get();
            blip.Initialize(contact, config.Blips);
            blip.SetInterpolationSpeed(config.InterpolationSpeed);
            _activeBlips[contact.EntityId] = blip;

            if (config.Blips.PulseNewContacts)
            {
                blip.PlayPulseAnimation(config.Blips.PulseDuration, config.Blips.PulseScale);
            }
        }

        private void UpdateBlipAppearance(MinimapBlip blip, MinimapContactData contact)
        {
            blip.UpdateAppearance(contact, config.Blips);
        }

        private void RemoveBlip(int entityId)
        {
            if (_activeBlips.TryGetValue(entityId, out var blip))
            {
                _activeBlips.Remove(entityId);

                if (config.Blips.FadeOnLoss)
                {
                    blip.PlayFadeOutAnimation(config.Blips.FadeOutDuration, () =>
                    {
                        _blipPool.Return(blip);
                    });
                }
                else
                {
                    _blipPool.Return(blip);
                }
            }
        }

        private MinimapContactData CreateContactData(DetectedEntity entity)
        {
            return new MinimapContactData(
                entity,
                (Vector2)targetEntity.transform.position,
                _dataProvider.MaxRange,
                _playerTransponder?.Faction);
        }

        private EntityControllerBase FindPlayerEntity()
        {
            var playerSetup = FindFirstObjectByType<PlayerSetup>();
            return playerSetup?.GetComponent<EntityControllerBase>();
        }

        private void UpdateStaleIndicator()
        {
            if (!config.Style.ShowStaleDataIndicator) return;

            float staleThreshold = _dataProvider.UpdateInterval * config.Style.StaleThresholdMultiplier;
            bool isStale = _dataProvider.TimeSinceLastUpdate > staleThreshold;
            _renderer.SetStaleIndicatorVisible(isStale);
        }

        private void EnsureCanvas()
        {
            if (targetCanvas != null) return;

            // Try to find existing UI canvas
            targetCanvas = FindFirstObjectByType<Canvas>();

            if (targetCanvas == null)
            {
                // Create a new canvas
                var canvasObj = new GameObject("MinimapCanvas");
                targetCanvas = canvasObj.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 100;

                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }

        private void CreateMinimapContainer()
        {
            var containerObj = new GameObject("MinimapContainer");
            containerObj.transform.SetParent(targetCanvas.transform, false);

            _minimapContainer = containerObj.AddComponent<RectTransform>();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        /// <summary>
        /// Clean up all minimap resources.
        /// </summary>
        public void Cleanup()
        {
            if (_playerSensor != null)
            {
                _playerSensor.OnEntityDetected -= OnEntityDetected;
                _playerSensor.OnEntityLost -= OnEntityLost;
            }

            if (_dataProvider != null)
            {
                _dataProvider.OnDataUpdated -= OnSensorDataUpdated;
            }

            _sensorProvider?.Dispose();
            _renderer?.Dispose();
            _blipPool?.Dispose();

            _activeBlips.Clear();

            if (_minimapContainer != null)
            {
                Destroy(_minimapContainer.gameObject);
            }

            _isInitialized = false;
        }

        /// <summary>
        /// Reinitialize with a new configuration.
        /// </summary>
        public void SetConfig(MinimapConfig newConfig)
        {
            Cleanup();
            config = newConfig;
            Initialize();
        }

        /// <summary>
        /// Manually refresh sensor data and blips.
        /// </summary>
        public void ForceRefresh()
        {
            if (!_isInitialized) return;
            _dataProvider.Refresh();
        }
    }
}
