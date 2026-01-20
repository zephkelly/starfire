using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Starfire.Core.Background.Regions;

namespace StarfireV2.Fluid
{
    /// <summary>
    /// Central manager for fluid wake simulation.
    /// Handles compute shader dispatch, obstacle gathering, and visualization rendering.
    /// </summary>
    [ExecuteAlways]
    public class FluidSimulationManager : MonoBehaviour
    {
        public static FluidSimulationManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private FluidSimulationConfig simulationConfig;
        [SerializeField] private FluidVisualizationConfig visualizationConfig;

        [Header("Shaders")]
        [SerializeField] private ComputeShader fluidSimulationShader;
        [SerializeField] private Shader fluidVisualizationShader;

        [Header("Rendering")]
        [SerializeField] private float renderDepth = 50f;
        [SerializeField] private int sortingOrder = -500;

        [Header("Debug")]
        [SerializeField] private bool enableDebugView = false;
        [SerializeField] private bool showSimulationBounds = false;
        [SerializeField] private bool forceSimulationActive = false;
        [SerializeField] private bool logDebugInfo = false;

        // Render Textures (ping-pong buffers)
        private RenderTexture _velocityRead, _velocityWrite;
        private RenderTexture _pressureRead, _pressureWrite;
        private RenderTexture _densityRead, _densityWrite;
        private RenderTexture _divergence;
        private RenderTexture _densitySource;

        // Compute buffer for obstacles
        private ComputeBuffer _obstacleBuffer;
        private ObstacleData[] _obstacleDataArray;
        private const int MaxObstacles = 16;

        // Visualization
        private GameObject _visualizationQuad;
        private MeshRenderer _visualizationRenderer;
        private Material _visualizationMaterial;
        private Mesh _quadMesh;

        // Cached kernel indices
        private int _addForcesKernel;
        private int _advectKernel;
        private int _diffuseKernel;
        private int _computeDivergenceKernel;
        private int _pressureJacobiKernel;
        private int _projectVelocityKernel;
        private int _applyBoundariesKernel;
        private int _advectDensityKernel;
        private int _decayDensityKernel;
        private int _injectDensitySourceKernel;
        private int _initializeDensityKernel;
        private int _clearTextureKernel;

        // State
        private Camera _camera;
        private bool _initialized = false;
        private bool _simulationActive = false;
        private readonly HashSet<IFluidObstacle> _registeredObstacles = new HashSet<IFluidObstacle>();
        private readonly List<IFluidObstacle> _activeObstacles = new List<IFluidObstacle>();

        // Shader property IDs (cached for performance)
        private static readonly int ResolutionID = Shader.PropertyToID("_Resolution");
        private static readonly int DeltaTimeID = Shader.PropertyToID("_DeltaTime");
        private static readonly int ViscosityID = Shader.PropertyToID("_Viscosity");
        private static readonly int DensityDecayID = Shader.PropertyToID("_DensityDecay");
        private static readonly int VelocityDampingID = Shader.PropertyToID("_VelocityDamping");
        private static readonly int SimulationCenterID = Shader.PropertyToID("_SimulationCenter");
        private static readonly int SimulationSizeID = Shader.PropertyToID("_SimulationSize");
        private static readonly int ObstacleCountID = Shader.PropertyToID("_ObstacleCount");
        private static readonly int ShipPushStrengthID = Shader.PropertyToID("_ShipPushStrength");
        private static readonly int InfluenceRadiusMultiplierID = Shader.PropertyToID("_InfluenceRadiusMultiplier");
        private static readonly int DensitySourceStrengthID = Shader.PropertyToID("_DensitySourceStrength");
        private static readonly int InitialDensityID = Shader.PropertyToID("_InitialDensity");
        private static readonly int ClearValueID = Shader.PropertyToID("_ClearValue");
        private static readonly int ClearModeID = Shader.PropertyToID("_ClearMode");

        // Visualization shader property IDs
        private static readonly int DensityTextureID = Shader.PropertyToID("_DensityTexture");
        private static readonly int VelocityTextureID = Shader.PropertyToID("_VelocityTexture");
        private static readonly int Color1ID = Shader.PropertyToID("_Color1");
        private static readonly int Color2ID = Shader.PropertyToID("_Color2");
        private static readonly int Color3ID = Shader.PropertyToID("_Color3");
        private static readonly int EmissionIntensityID = Shader.PropertyToID("_EmissionIntensity");
        private static readonly int DensityMultiplierID = Shader.PropertyToID("_DensityMultiplier");
        private static readonly int AlphaMultiplierID = Shader.PropertyToID("_AlphaMultiplier");
        private static readonly int AlphaThresholdID = Shader.PropertyToID("_AlphaThreshold");
        private static readonly int ShowVelocityID = Shader.PropertyToID("_ShowVelocity");
        private static readonly int VelocityScaleID = Shader.PropertyToID("_VelocityScale");
        private static readonly int VisualNoiseScaleID = Shader.PropertyToID("_VisualNoiseScale");
        private static readonly int VisualNoiseContrastID = Shader.PropertyToID("_VisualNoiseContrast");
        private static readonly int VisualNoiseOctavesID = Shader.PropertyToID("_VisualNoiseOctaves");
        private static readonly int VisualNoisePersistenceID = Shader.PropertyToID("_VisualNoisePersistence");

        // Nebula masking property IDs
        private static readonly int EnableNebulaMaskID = Shader.PropertyToID("_EnableNebulaMask");
        private static readonly int NebulaRegionCountID = Shader.PropertyToID("_NebulaRegionCount");
        private static readonly int NebulaRegionsID = Shader.PropertyToID("_NebulaRegions");
        private static readonly int NebulaMaskSoftnessID = Shader.PropertyToID("_NebulaMaskSoftness");

        // Struct matching the compute shader (must match memory layout exactly)
        [StructLayout(LayoutKind.Sequential)]
        private struct ObstacleData
        {
            public Vector2 position;
            public Vector2 velocity;
            public Vector2 halfExtents;
            public float rotation;
            public float velocityScale;
        }

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple FluidSimulationManagers detected. Destroying duplicate.");
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            Instance = this;
            _camera = Camera.main;
            if (_camera == null)
            {
                _camera = FindFirstObjectByType<Camera>();
            }

            Initialize();
        }

        private void OnDisable()
        {
            Cleanup();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void LateUpdate()
        {
            if (!_initialized || _camera == null) return;
            if (simulationConfig == null || visualizationConfig == null) return;

            CheckSimulationState();

            if (_simulationActive)
            {
                SimulationStep();
                UpdateVisualizationTransform();
                UpdateVisualizationMaterial();
                UpdateNebulaRegionMask();
            }
        }

        private void OnDrawGizmos()
        {
            if (!showSimulationBounds || _camera == null || simulationConfig == null) return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Vector3 center = _camera.transform.position;
            center.z = 0;
            float size = simulationConfig.simulationWorldSize;
            Gizmos.DrawWireCube(center, new Vector3(size, size, 0.1f));
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (_initialized) return;
            if (fluidSimulationShader == null || fluidVisualizationShader == null)
            {
                Debug.LogWarning("FluidSimulationManager: Missing shaders. Assign ComputeShader and Visualization Shader.");
                return;
            }
            if (simulationConfig == null || visualizationConfig == null)
            {
                Debug.LogWarning("FluidSimulationManager: Missing configuration assets.");
                return;
            }

            CacheKernelIndices();
            CreateRenderTextures();
            CreateObstacleBuffer();
            CreateVisualizationQuad();
            InitializeDensityField();

            _initialized = true;
            _simulationActive = false;

            if (_visualizationQuad != null)
            {
                _visualizationQuad.SetActive(false);
            }

            // Discover any obstacles that were created before the manager initialized
            DiscoverExistingObstacles();

            Debug.Log($"[FluidSim] Initialized successfully. Resolution: {simulationConfig.resolution}, Simulation size: {simulationConfig.simulationWorldSize}");
        }

        private void DiscoverExistingObstacles()
        {
            var existingObstacles = FindObjectsByType<FluidObstacleComponent>(FindObjectsSortMode.None);
            int discovered = 0;
            foreach (var obstacle in existingObstacles)
            {
                if (obstacle.enabled && obstacle.gameObject.activeInHierarchy)
                {
                    if (_registeredObstacles.Add(obstacle))
                    {
                        discovered++;
                    }
                }
            }
            if (discovered > 0)
            {
                Debug.Log($"[FluidSim] Discovered {discovered} existing obstacle(s) in scene. Total registered: {_registeredObstacles.Count}");
            }
        }

        private void CacheKernelIndices()
        {
            _addForcesKernel = fluidSimulationShader.FindKernel("AddForces");
            _advectKernel = fluidSimulationShader.FindKernel("Advect");
            _diffuseKernel = fluidSimulationShader.FindKernel("Diffuse");
            _computeDivergenceKernel = fluidSimulationShader.FindKernel("ComputeDivergence");
            _pressureJacobiKernel = fluidSimulationShader.FindKernel("PressureJacobi");
            _projectVelocityKernel = fluidSimulationShader.FindKernel("ProjectVelocity");
            _applyBoundariesKernel = fluidSimulationShader.FindKernel("ApplyBoundaries");
            _advectDensityKernel = fluidSimulationShader.FindKernel("AdvectDensity");
            _decayDensityKernel = fluidSimulationShader.FindKernel("DecayDensity");
            _injectDensitySourceKernel = fluidSimulationShader.FindKernel("InjectDensitySource");
            _initializeDensityKernel = fluidSimulationShader.FindKernel("InitializeDensity");
            _clearTextureKernel = fluidSimulationShader.FindKernel("ClearTexture");
        }

        private void CreateRenderTextures()
        {
            int res = simulationConfig.resolution;

            // Velocity: RG = Vx, Vy
            _velocityRead = CreateRenderTexture(res, RenderTextureFormat.RGFloat, "_VelocityRead");
            _velocityWrite = CreateRenderTexture(res, RenderTextureFormat.RGFloat, "_VelocityWrite");

            // Pressure: single float
            _pressureRead = CreateRenderTexture(res, RenderTextureFormat.RFloat, "_PressureRead");
            _pressureWrite = CreateRenderTexture(res, RenderTextureFormat.RFloat, "_PressureWrite");

            // Density: single float
            _densityRead = CreateRenderTexture(res, RenderTextureFormat.RFloat, "_DensityRead");
            _densityWrite = CreateRenderTexture(res, RenderTextureFormat.RFloat, "_DensityWrite");

            // Divergence: single float (no ping-pong needed)
            _divergence = CreateRenderTexture(res, RenderTextureFormat.RFloat, "_Divergence");

            // Density source (for replenishment) - uniform for now
            _densitySource = CreateRenderTexture(res, RenderTextureFormat.RFloat, "_DensitySource");
        }

        private RenderTexture CreateRenderTexture(int resolution, RenderTextureFormat format, string name)
        {
            var rt = new RenderTexture(resolution, resolution, 0, format)
            {
                name = name,
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            rt.Create();
            return rt;
        }

        private void CreateObstacleBuffer()
        {
            int stride = Marshal.SizeOf<ObstacleData>();
            _obstacleBuffer = new ComputeBuffer(MaxObstacles, stride);
            _obstacleDataArray = new ObstacleData[MaxObstacles];
        }

        private void CreateVisualizationQuad()
        {
            _quadMesh = CreateQuadMesh();

            _visualizationQuad = new GameObject("FluidVisualizationQuad")
            {
                hideFlags = HideFlags.DontSave
            };
            _visualizationQuad.transform.SetParent(transform);

            var meshFilter = _visualizationQuad.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = _quadMesh;

            _visualizationRenderer = _visualizationQuad.AddComponent<MeshRenderer>();
            _visualizationRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _visualizationRenderer.receiveShadows = false;
            _visualizationRenderer.sortingLayerName = "Default";
            _visualizationRenderer.sortingOrder = sortingOrder;

            _visualizationMaterial = new Material(fluidVisualizationShader)
            {
                hideFlags = HideFlags.DontSave
            };
            _visualizationRenderer.sharedMaterial = _visualizationMaterial;

            if (logDebugInfo)
            {
                Debug.Log($"[FluidSim] Created visualization quad with sortingOrder: {sortingOrder}");
            }
        }

        private Mesh CreateQuadMesh()
        {
            var mesh = new Mesh
            {
                name = "FluidVisualizationQuadMesh",
                hideFlags = HideFlags.DontSave
            };

            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0)
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };

            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void InitializeDensityField()
        {
            if (fluidSimulationShader == null) return;

            int res = simulationConfig.resolution;
            int threadGroups = Mathf.CeilToInt(res / 8f);

            // Initialize density to uniform value
            fluidSimulationShader.SetFloat(InitialDensityID, simulationConfig.initialDensity);
            fluidSimulationShader.SetVector(ResolutionID, new Vector2(res, res));

            fluidSimulationShader.SetTexture(_initializeDensityKernel, "_DensityWrite", _densityRead);
            fluidSimulationShader.Dispatch(_initializeDensityKernel, threadGroups, threadGroups, 1);

            fluidSimulationShader.SetTexture(_initializeDensityKernel, "_DensityWrite", _densityWrite);
            fluidSimulationShader.Dispatch(_initializeDensityKernel, threadGroups, threadGroups, 1);

            // Initialize density source
            fluidSimulationShader.SetTexture(_initializeDensityKernel, "_DensityWrite", _densitySource);
            fluidSimulationShader.Dispatch(_initializeDensityKernel, threadGroups, threadGroups, 1);

            // Clear velocity using compute shader
            ClearVelocityTexture(_velocityRead);
            ClearVelocityTexture(_velocityWrite);

            // Pressure textures start at 0 by default (RenderTexture initialization)
            // No need to explicitly clear them

            if (logDebugInfo)
            {
                Debug.Log($"[FluidSim] Initialized density field at resolution {res}x{res}, initial density: {simulationConfig.initialDensity}");
            }
        }

        private void ClearVelocityTexture(RenderTexture texture)
        {
            int res = simulationConfig.resolution;
            int threadGroups = Mathf.CeilToInt(res / 8f);

            fluidSimulationShader.SetFloat(ClearValueID, 0f);
            fluidSimulationShader.SetInt(ClearModeID, 1); // 1 = velocity (float2)
            fluidSimulationShader.SetVector(ResolutionID, new Vector2(res, res));
            fluidSimulationShader.SetTexture(_clearTextureKernel, "_VelocityWrite", texture);
            fluidSimulationShader.Dispatch(_clearTextureKernel, threadGroups, threadGroups, 1);
        }

        private void Cleanup()
        {
            _initialized = false;

            // Release render textures
            ReleaseRenderTexture(ref _velocityRead);
            ReleaseRenderTexture(ref _velocityWrite);
            ReleaseRenderTexture(ref _pressureRead);
            ReleaseRenderTexture(ref _pressureWrite);
            ReleaseRenderTexture(ref _densityRead);
            ReleaseRenderTexture(ref _densityWrite);
            ReleaseRenderTexture(ref _divergence);
            ReleaseRenderTexture(ref _densitySource);

            // Release compute buffer
            if (_obstacleBuffer != null)
            {
                _obstacleBuffer.Release();
                _obstacleBuffer = null;
            }

            // Cleanup visualization
            if (_visualizationMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(_visualizationMaterial);
                else
                    DestroyImmediate(_visualizationMaterial);
                _visualizationMaterial = null;
            }

            if (_visualizationQuad != null)
            {
                if (Application.isPlaying)
                    Destroy(_visualizationQuad);
                else
                    DestroyImmediate(_visualizationQuad);
                _visualizationQuad = null;
            }

            if (_quadMesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_quadMesh);
                else
                    DestroyImmediate(_quadMesh);
                _quadMesh = null;
            }
        }

        private void ReleaseRenderTexture(ref RenderTexture rt)
        {
            if (rt != null)
            {
                rt.Release();
                if (Application.isPlaying)
                    Destroy(rt);
                else
                    DestroyImmediate(rt);
                rt = null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Whether the simulation is currently initialized.
        /// </summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// Whether the simulation is currently running.
        /// </summary>
        public bool IsSimulationActive => _simulationActive;

        /// <summary>
        /// Number of registered obstacles.
        /// </summary>
        public int RegisteredObstacleCount => _registeredObstacles.Count;

        /// <summary>
        /// Number of active obstacles affecting the simulation this frame.
        /// </summary>
        public int ActiveObstacleCount => _activeObstacles.Count;

        /// <summary>
        /// Register an obstacle to affect the fluid simulation.
        /// </summary>
        public void RegisterObstacle(IFluidObstacle obstacle)
        {
            if (obstacle != null)
            {
                _registeredObstacles.Add(obstacle);
                if (logDebugInfo) Debug.Log($"[FluidSim] Registered obstacle. Total: {_registeredObstacles.Count}");
            }
        }

        /// <summary>
        /// Unregister an obstacle from the fluid simulation.
        /// </summary>
        public void UnregisterObstacle(IFluidObstacle obstacle)
        {
            if (obstacle != null)
            {
                _registeredObstacles.Remove(obstacle);
                if (logDebugInfo) Debug.Log($"[FluidSim] Unregistered obstacle. Total: {_registeredObstacles.Count}");
            }
        }

        /// <summary>
        /// Force the simulation to reinitialize (useful after config changes).
        /// </summary>
        public void Reinitialize()
        {
            Cleanup();
            Initialize();
        }

        #endregion

        #region Simulation

        private void CheckSimulationState()
        {
            // Force active for debugging
            if (forceSimulationActive)
            {
                if (!_simulationActive)
                {
                    ActivateSimulation();
                    if (logDebugInfo) Debug.Log("[FluidSim] Force activated simulation");
                }
                return;
            }

            bool shouldBeActive = false;
            string deactivateReason = "";

            // Check 1: Any nebula regions visible?
            if (simulationConfig.requireNebulaPresence)
            {
                if (NebulaRegionManager.Instance != null)
                {
                    var visibleRegions = NebulaRegionManager.Instance.GetVisibleRegions();
                    shouldBeActive = visibleRegions.Count > 0;
                    if (!shouldBeActive) deactivateReason = "No visible nebula regions";
                }
                else
                {
                    deactivateReason = "NebulaRegionManager.Instance is null";
                }
            }
            else
            {
                shouldBeActive = true;
            }

            // Check 2: Any obstacles registered?
            if (shouldBeActive && simulationConfig.skipWhenNoObstacles)
            {
                shouldBeActive = _registeredObstacles.Count > 0;
                if (!shouldBeActive) deactivateReason = "No obstacles registered";
            }

            if (shouldBeActive && !_simulationActive)
            {
                ActivateSimulation();
                if (logDebugInfo) Debug.Log($"[FluidSim] Activated simulation. Obstacles: {_registeredObstacles.Count}");
            }
            else if (!shouldBeActive && _simulationActive)
            {
                DeactivateSimulation();
                if (logDebugInfo) Debug.Log($"[FluidSim] Deactivated simulation. Reason: {deactivateReason}");
            }
        }

        private void ActivateSimulation()
        {
            _simulationActive = true;
            if (_visualizationQuad != null)
            {
                _visualizationQuad.SetActive(true);
            }
        }

        private void DeactivateSimulation()
        {
            _simulationActive = false;
            if (_visualizationQuad != null)
            {
                _visualizationQuad.SetActive(false);
            }
        }

        private void SimulationStep()
        {
            if (fluidSimulationShader == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            int res = simulationConfig.resolution;
            int threadGroups = Mathf.CeilToInt(res / 8f);

            // Update simulation bounds (follow camera)
            Vector2 simCenter = _camera.transform.position;
            Vector2 simSize = new Vector2(simulationConfig.simulationWorldSize, simulationConfig.simulationWorldSize);

            // Gather active obstacles
            GatherActiveObstacles();

            // Skip if no obstacles and configured to do so
            if (_activeObstacles.Count == 0 && simulationConfig.skipWhenNoObstacles)
            {
                return;
            }

            // Upload obstacle data
            UploadObstacleData();

            // Set common uniforms
            fluidSimulationShader.SetVector(ResolutionID, new Vector2(res, res));
            fluidSimulationShader.SetFloat(DeltaTimeID, dt);
            fluidSimulationShader.SetFloat(ViscosityID, simulationConfig.viscosity);
            fluidSimulationShader.SetFloat(DensityDecayID, simulationConfig.densityDecay);
            fluidSimulationShader.SetFloat(VelocityDampingID, simulationConfig.velocityDamping);
            fluidSimulationShader.SetVector(SimulationCenterID, simCenter);
            fluidSimulationShader.SetVector(SimulationSizeID, simSize);
            fluidSimulationShader.SetFloat(ShipPushStrengthID, simulationConfig.shipPushStrength);
            fluidSimulationShader.SetFloat(InfluenceRadiusMultiplierID, simulationConfig.influenceRadiusMultiplier);
            fluidSimulationShader.SetFloat(DensitySourceStrengthID, simulationConfig.densitySourceStrength);

            // 1. ADD FORCES
            fluidSimulationShader.SetTexture(_addForcesKernel, "_VelocityRead", _velocityRead);
            fluidSimulationShader.SetTexture(_addForcesKernel, "_VelocityWrite", _velocityWrite);
            fluidSimulationShader.SetBuffer(_addForcesKernel, "_Obstacles", _obstacleBuffer);
            fluidSimulationShader.Dispatch(_addForcesKernel, threadGroups, threadGroups, 1);
            SwapVelocityBuffers();

            // 2. ADVECT VELOCITY
            fluidSimulationShader.SetTexture(_advectKernel, "_VelocityRead", _velocityRead);
            fluidSimulationShader.SetTexture(_advectKernel, "_VelocityWrite", _velocityWrite);
            fluidSimulationShader.Dispatch(_advectKernel, threadGroups, threadGroups, 1);
            SwapVelocityBuffers();

            // 3. DIFFUSE VELOCITY (viscosity)
            if (simulationConfig.viscosity > 0.001f && simulationConfig.diffuseIterations > 0)
            {
                for (int i = 0; i < simulationConfig.diffuseIterations; i++)
                {
                    fluidSimulationShader.SetTexture(_diffuseKernel, "_VelocityRead", _velocityRead);
                    fluidSimulationShader.SetTexture(_diffuseKernel, "_VelocityWrite", _velocityWrite);
                    fluidSimulationShader.Dispatch(_diffuseKernel, threadGroups, threadGroups, 1);
                    SwapVelocityBuffers();
                }
            }

            // 4. COMPUTE DIVERGENCE
            fluidSimulationShader.SetTexture(_computeDivergenceKernel, "_VelocityRead", _velocityRead);
            fluidSimulationShader.SetTexture(_computeDivergenceKernel, "_Divergence", _divergence);
            fluidSimulationShader.Dispatch(_computeDivergenceKernel, threadGroups, threadGroups, 1);

            // 5. PRESSURE JACOBI ITERATIONS
            for (int i = 0; i < simulationConfig.pressureIterations; i++)
            {
                fluidSimulationShader.SetTexture(_pressureJacobiKernel, "_PressureRead", _pressureRead);
                fluidSimulationShader.SetTexture(_pressureJacobiKernel, "_PressureWrite", _pressureWrite);
                fluidSimulationShader.SetTexture(_pressureJacobiKernel, "_Divergence", _divergence);
                fluidSimulationShader.Dispatch(_pressureJacobiKernel, threadGroups, threadGroups, 1);
                SwapPressureBuffers();
            }

            // 6. PROJECT VELOCITY (subtract pressure gradient)
            fluidSimulationShader.SetTexture(_projectVelocityKernel, "_VelocityRead", _velocityRead);
            fluidSimulationShader.SetTexture(_projectVelocityKernel, "_VelocityWrite", _velocityWrite);
            fluidSimulationShader.SetTexture(_projectVelocityKernel, "_PressureRead", _pressureRead);
            fluidSimulationShader.Dispatch(_projectVelocityKernel, threadGroups, threadGroups, 1);
            SwapVelocityBuffers();

            // 7. APPLY BOUNDARIES
            fluidSimulationShader.SetTexture(_applyBoundariesKernel, "_VelocityWrite", _velocityRead);
            fluidSimulationShader.SetBuffer(_applyBoundariesKernel, "_Obstacles", _obstacleBuffer);
            fluidSimulationShader.Dispatch(_applyBoundariesKernel, threadGroups, threadGroups, 1);

            // 8. ADVECT DENSITY
            fluidSimulationShader.SetTexture(_advectDensityKernel, "_VelocityRead", _velocityRead);
            fluidSimulationShader.SetTexture(_advectDensityKernel, "_DensityRead", _densityRead);
            fluidSimulationShader.SetTexture(_advectDensityKernel, "_DensityWrite", _densityWrite);
            fluidSimulationShader.Dispatch(_advectDensityKernel, threadGroups, threadGroups, 1);
            SwapDensityBuffers();

            // 9. DECAY DENSITY (optional - gas fades back)
            if (simulationConfig.densityDecay > 0f)
            {
                fluidSimulationShader.SetTexture(_decayDensityKernel, "_DensityRead", _densityRead);
                fluidSimulationShader.SetTexture(_decayDensityKernel, "_DensityWrite", _densityWrite);
                fluidSimulationShader.Dispatch(_decayDensityKernel, threadGroups, threadGroups, 1);
                SwapDensityBuffers();
            }

            // 10. INJECT DENSITY SOURCE (replenish gas)
            if (simulationConfig.densitySourceStrength > 0f)
            {
                fluidSimulationShader.SetTexture(_injectDensitySourceKernel, "_DensityRead", _densityRead);
                fluidSimulationShader.SetTexture(_injectDensitySourceKernel, "_DensityWrite", _densityWrite);
                fluidSimulationShader.SetTexture(_injectDensitySourceKernel, "_DensitySource", _densitySource);
                fluidSimulationShader.Dispatch(_injectDensitySourceKernel, threadGroups, threadGroups, 1);
                SwapDensityBuffers();
            }
        }

        private void SwapVelocityBuffers()
        {
            (_velocityRead, _velocityWrite) = (_velocityWrite, _velocityRead);
        }

        private void SwapPressureBuffers()
        {
            (_pressureRead, _pressureWrite) = (_pressureWrite, _pressureRead);
        }

        private void SwapDensityBuffers()
        {
            (_densityRead, _densityWrite) = (_densityWrite, _densityRead);
        }

        #endregion

        #region Obstacle Gathering

        private void GatherActiveObstacles()
        {
            _activeObstacles.Clear();

            if (_camera == null) return;

            Vector2 cameraPos = _camera.transform.position;
            float maxDist = simulationConfig.maxObstacleDistance;
            float maxDistSq = maxDist * maxDist;
            float minSpeed = simulationConfig.minimumSpeedThreshold;
            float minSpeedSq = minSpeed * minSpeed;

            int skippedInactive = 0;
            int skippedDistance = 0;
            int skippedSpeed = 0;
            int skippedNebula = 0;

            foreach (var obstacle in _registeredObstacles)
            {
                if (obstacle == null || !obstacle.IsActive)
                {
                    skippedInactive++;
                    continue;
                }

                // Distance check
                float distSq = (obstacle.Position - cameraPos).sqrMagnitude;
                if (distSq > maxDistSq)
                {
                    skippedDistance++;
                    continue;
                }

                // Speed check
                if (obstacle.Velocity.sqrMagnitude < minSpeedSq)
                {
                    skippedSpeed++;
                    continue;
                }

                // Nebula check (only if required)
                if (simulationConfig.requireNebulaPresence && NebulaRegionManager.Instance != null)
                {
                    var region = NebulaRegionManager.Instance.GetRegionAtPoint(obstacle.Position);
                    if (region == null)
                    {
                        skippedNebula++;
                        continue;
                    }
                }

                _activeObstacles.Add(obstacle);

                if (_activeObstacles.Count >= MaxObstacles) break;
            }

            if (logDebugInfo && Time.frameCount % 60 == 0) // Log once per second
            {
                Debug.Log($"[FluidSim] Obstacles - Registered: {_registeredObstacles.Count}, Active: {_activeObstacles.Count}, " +
                          $"Skipped (inactive: {skippedInactive}, distance: {skippedDistance}, speed: {skippedSpeed}, nebula: {skippedNebula})");
            }
        }

        private void UploadObstacleData()
        {
            for (int i = 0; i < MaxObstacles; i++)
            {
                if (i < _activeObstacles.Count)
                {
                    var obs = _activeObstacles[i];
                    _obstacleDataArray[i] = new ObstacleData
                    {
                        position = obs.Position,
                        velocity = obs.Velocity,
                        halfExtents = obs.HalfExtents,
                        rotation = obs.Rotation,
                        velocityScale = obs.VelocityScale
                    };
                }
                else
                {
                    // Zero out unused slots
                    _obstacleDataArray[i] = default;
                }
            }

            _obstacleBuffer.SetData(_obstacleDataArray);
            fluidSimulationShader.SetInt(ObstacleCountID, _activeObstacles.Count);
        }

        #endregion

        #region Visualization

        private void UpdateVisualizationTransform()
        {
            if (_visualizationQuad == null || _camera == null) return;

            Vector3 cameraPos = _camera.transform.position;
            float size = simulationConfig.simulationWorldSize;

            _visualizationQuad.transform.position = new Vector3(cameraPos.x, cameraPos.y, cameraPos.z + renderDepth);
            _visualizationQuad.transform.rotation = Quaternion.identity;
            _visualizationQuad.transform.localScale = new Vector3(size, size, 1f);
        }

        private void UpdateVisualizationMaterial()
        {
            if (_visualizationMaterial == null || visualizationConfig == null) return;

            // Simulation textures
            _visualizationMaterial.SetTexture(DensityTextureID, _densityRead);
            _visualizationMaterial.SetTexture(VelocityTextureID, _velocityRead);

            // Simulation bounds
            Vector2 simCenter = _camera.transform.position;
            Vector2 simSize = new Vector2(simulationConfig.simulationWorldSize, simulationConfig.simulationWorldSize);
            _visualizationMaterial.SetVector(SimulationCenterID, simCenter);
            _visualizationMaterial.SetVector(SimulationSizeID, simSize);

            // Colors
            _visualizationMaterial.SetColor(Color1ID, visualizationConfig.lowDensityColor);
            _visualizationMaterial.SetColor(Color2ID, visualizationConfig.midDensityColor);
            _visualizationMaterial.SetColor(Color3ID, visualizationConfig.highDensityColor);
            _visualizationMaterial.SetFloat(EmissionIntensityID, visualizationConfig.emissionIntensity);

            // Density display
            _visualizationMaterial.SetFloat(DensityMultiplierID, visualizationConfig.densityMultiplier);
            _visualizationMaterial.SetFloat(AlphaMultiplierID, visualizationConfig.alphaMultiplier);
            _visualizationMaterial.SetFloat(AlphaThresholdID, visualizationConfig.alphaThreshold);

            // Debug
            _visualizationMaterial.SetFloat(ShowVelocityID, visualizationConfig.showVelocityVisualization ? 1f : 0f);
            _visualizationMaterial.SetFloat(VelocityScaleID, visualizationConfig.velocityColorScale);

            // Visual noise
            _visualizationMaterial.SetFloat(VisualNoiseScaleID, visualizationConfig.visualNoiseScale);
            _visualizationMaterial.SetFloat(VisualNoiseContrastID, visualizationConfig.visualNoiseContrast);
            _visualizationMaterial.SetFloat(VisualNoiseOctavesID, visualizationConfig.visualNoiseOctaves);
            _visualizationMaterial.SetFloat(VisualNoisePersistenceID, visualizationConfig.visualNoisePersistence);
        }

        private void UpdateNebulaRegionMask()
        {
            if (_visualizationMaterial == null || visualizationConfig == null) return;

            if (!visualizationConfig.enableNebulaMask || NebulaRegionManager.Instance == null)
            {
                _visualizationMaterial.SetFloat(EnableNebulaMaskID, 0f);
                return;
            }

            _visualizationMaterial.SetFloat(EnableNebulaMaskID, 1f);
            _visualizationMaterial.SetFloat(NebulaMaskSoftnessID, visualizationConfig.nebulaMaskSoftness);

            var visibleRegions = NebulaRegionManager.Instance.GetVisibleRegions();
            int count = Mathf.Min(visibleRegions.Count, 8);

            Vector4[] regionData = new Vector4[8];
            for (int i = 0; i < count; i++)
            {
                var region = visibleRegions[i];
                regionData[i] = new Vector4(
                    region.WorldPosition.x,
                    region.WorldPosition.y,
                    region.Radius,
                    region.Config != null ? region.Config.falloffDistance : 10f
                );
            }

            _visualizationMaterial.SetInt(NebulaRegionCountID, count);
            _visualizationMaterial.SetVectorArray(NebulaRegionsID, regionData);
        }

        #endregion
    }
}
