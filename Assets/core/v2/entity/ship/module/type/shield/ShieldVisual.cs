using UnityEngine;

namespace StarfireV2
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ShieldVisual : MonoBehaviour
    {
        private const int MaxImpacts = 8;
        private const string ShaderName = "Starfire/ShieldBarrier";

        private IShipShieldModule _shield;
        private ShieldVisualConfig _config;
        private IEntityController _controller;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _material;
        private MaterialPropertyBlock _propertyBlock;

        private Vector4[] _impactData = new Vector4[MaxImpacts];
        private int _currentImpactIndex;
        private float _lastHitTime = -100f;
        private float _currentOpacity;
        private bool _isShieldDestroyed = false;

        private static readonly int ShieldColorId = Shader.PropertyToID("_ShieldColor");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int HitFlashColorId = Shader.PropertyToID("_HitFlashColor");
        private static readonly int DamagedColorId = Shader.PropertyToID("_DamagedColor");
        private static readonly int EdgeIntensityId = Shader.PropertyToID("_EdgeIntensity");
        private static readonly int EdgePowerId = Shader.PropertyToID("_EdgePower");
        private static readonly int EdgeThicknessId = Shader.PropertyToID("_EdgeThickness");
        private static readonly int EdgeDitherId = Shader.PropertyToID("_EdgeDither");
        private static readonly int PatternTypeId = Shader.PropertyToID("_PatternType");
        private static readonly int PatternScaleId = Shader.PropertyToID("_PatternScale");
        private static readonly int PatternSpeedId = Shader.PropertyToID("_PatternSpeed");
        private static readonly int PatternIntensityId = Shader.PropertyToID("_PatternIntensity");
        private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int VisibilityModeId = Shader.PropertyToID("_VisibilityMode");
        private static readonly int IdleOpacityId = Shader.PropertyToID("_IdleOpacity");
        private static readonly int ActiveOpacityId = Shader.PropertyToID("_ActiveOpacity");
        private static readonly int CurrentOpacityId = Shader.PropertyToID("_CurrentOpacity");
        private static readonly int RippleSpeedId = Shader.PropertyToID("_RippleSpeed");
        private static readonly int RippleWidthId = Shader.PropertyToID("_RippleWidth");
        private static readonly int RippleIntensityId = Shader.PropertyToID("_RippleIntensity");
        private static readonly int RippleDurationId = Shader.PropertyToID("_RippleDuration");
        private static readonly int RippleOpacityId = Shader.PropertyToID("_RippleOpacity");
        private static readonly int RippleSoftnessId = Shader.PropertyToID("_RippleSoftness");
        private static readonly int RipplePerspectiveId = Shader.PropertyToID("_RipplePerspective");
        private static readonly int ImpactVisibilityRadiusId = Shader.PropertyToID("_ImpactVisibilityRadius");
        private static readonly int ImpactVisibilityFalloffId = Shader.PropertyToID("_ImpactVisibilityFalloff");
        private static readonly int ImpactVisibilitySpeedId = Shader.PropertyToID("_ImpactVisibilitySpeed");
        private static readonly int ShieldHealthId = Shader.PropertyToID("_ShieldHealth");
        private static readonly int DomeCurvatureId = Shader.PropertyToID("_DomeCurvature");
        private static readonly int DomeHighlightId = Shader.PropertyToID("_DomeHighlight");
        private static readonly int DomeShadowId = Shader.PropertyToID("_DomeShadow");
        private static readonly int ImpactPositionsId = Shader.PropertyToID("_ImpactPositions");

        public void Initialize(IShipShieldModule shield, IEntityController controller)
        {
            _shield = shield;
            _config = shield.VisualConfig;
            _controller = controller;

            if (_config == null)
            {
                Debug.LogWarning("ShieldVisual: No VisualConfig assigned to shield module");
                gameObject.SetActive(false);
                return;
            }

            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();

            for (int i = 0; i < MaxImpacts; i++)
            {
                _impactData[i] = new Vector4(0, 0, -100f, 0);
            }

            GenerateShieldMesh();
            SetupMaterial();
            UpdateInitialVisibility();

            _shield.OnShieldDestroyed += OnShieldDestroyed;
            _shield.OnShieldRestored += OnShieldRestored;
        }

        private void OnDestroy()
        {
            if (_shield != null)
            {
                _shield.OnShieldDestroyed -= OnShieldDestroyed;
                _shield.OnShieldRestored -= OnShieldRestored;
            }

            if (_material != null)
            {
                Destroy(_material);
            }
        }

        private void GenerateShieldMesh()
        {
            Vector2 size = _shield.BoundarySize;
            Vector2 offset = _shield.BoundaryOffset;
            int resolution = Mathf.Max(16, _shield.BoundaryResolution);

            Mesh mesh = new Mesh();
            mesh.name = "ShieldMesh";

            Vector3[] vertices = new Vector3[resolution + 1];
            Vector3[] normals = new Vector3[resolution + 1];
            Vector2[] uvs = new Vector2[resolution + 1];

            vertices[0] = new Vector3(offset.x, offset.y, 0);
            normals[0] = Vector3.back;
            uvs[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < resolution; i++)
            {
                float angle = (i / (float)resolution) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * size.x + offset.x;
                float y = Mathf.Sin(angle) * size.y + offset.y;

                vertices[i + 1] = new Vector3(x, y, 0);

                Vector3 toVertex = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                normals[i + 1] = toVertex.normalized;

                float u = (Mathf.Cos(angle) + 1f) * 0.5f;
                float v = (Mathf.Sin(angle) + 1f) * 0.5f;
                uvs[i + 1] = new Vector2(u, v);
            }

            int[] triangles = new int[resolution * 3];
            for (int i = 0; i < resolution; i++)
            {
                int tri = i * 3;
                triangles[tri] = 0;
                triangles[tri + 1] = i + 1;
                triangles[tri + 2] = (i + 1) % resolution + 1;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            _meshFilter.mesh = mesh;
        }

        private void SetupMaterial()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"ShieldVisual: Could not find shader '{ShaderName}'");
                return;
            }

            _material = new Material(shader);
            _meshRenderer.material = _material;

            _material.SetColor(ShieldColorId, _config.shieldColor);
            _material.SetColor(EdgeColorId, _config.edgeColor);
            _material.SetColor(HitFlashColorId, _config.hitFlashColor);
            _material.SetColor(DamagedColorId, _config.damagedColor);
            _material.SetFloat(EdgeIntensityId, _config.edgeIntensity);
            _material.SetFloat(EdgePowerId, _config.fresnelPower);
            _material.SetFloat(EdgeThicknessId, _config.edgeThickness);
            _material.SetFloat(EdgeDitherId, _config.edgeDither);
            _material.SetInt(PatternTypeId, (int)_config.patternType);
            _material.SetFloat(PatternScaleId, _config.patternScale);
            _material.SetFloat(PatternSpeedId, _config.patternSpeed);
            _material.SetFloat(PatternIntensityId, _config.patternIntensity);
            _material.SetFloat(PulseAmountId, _config.pulseAmount);
            _material.SetFloat(PulseSpeedId, _config.pulseSpeed);
            _material.SetInt(VisibilityModeId, (int)_config.visibilityMode);
            _material.SetFloat(IdleOpacityId, _config.idleOpacity);
            _material.SetFloat(ActiveOpacityId, _config.activeOpacity);
            _material.SetFloat(RippleSpeedId, _config.rippleSpeed);
            _material.SetFloat(RippleWidthId, _config.rippleWidth);
            _material.SetFloat(RippleIntensityId, _config.rippleIntensity);
            _material.SetFloat(RippleDurationId, _config.rippleDuration);
            _material.SetFloat(RippleOpacityId, _config.rippleOpacity);
            _material.SetFloat(RippleSoftnessId, _config.rippleSoftness);
            _material.SetFloat(RipplePerspectiveId, _config.ripplePerspective);
            _material.SetFloat(ImpactVisibilityRadiusId, _config.impactVisibilityRadius);
            _material.SetFloat(ImpactVisibilityFalloffId, _config.impactVisibilityFalloff);
            _material.SetFloat(ImpactVisibilitySpeedId, _config.impactVisibilitySpeed);
            _material.SetFloat(DomeCurvatureId, _config.domeCurvature);
            _material.SetFloat(DomeHighlightId, _config.domeHighlight);
            _material.SetFloat(DomeShadowId, _config.domeShadow);

            _material.renderQueue = 2990;

            _meshRenderer.sortingLayerName = "Default";
            _meshRenderer.sortingOrder = 100;
        }

        private void UpdateInitialVisibility()
        {
            switch (_config.visibilityMode)
            {
                case ShieldVisibilityMode.AlwaysSubtle:
                case ShieldVisibilityMode.Both:
                    _currentOpacity = _config.idleOpacity;
                    _meshRenderer.enabled = true;
                    break;

                case ShieldVisibilityMode.OnlyOnHit:
                    _currentOpacity = 0f;
                    _meshRenderer.enabled = false;
                    break;
            }

            _material.SetFloat(CurrentOpacityId, _currentOpacity);
        }

        private void Update()
        {
            if (_shield == null || _config == null || _material == null) return;

            UpdateVisibility();
            UpdateShieldHealth();
            UpdateImpactData();
        }

        private void UpdateVisibility()
        {
            if (_isShieldDestroyed)
            {
                _meshRenderer.enabled = false;
                return;
            }

            float targetOpacity;
            float timeSinceHit = Time.time - _lastHitTime;
            bool isRecentlyHit = timeSinceHit < _config.hitFadeDuration;

            switch (_config.visibilityMode)
            {
                case ShieldVisibilityMode.AlwaysSubtle:
                    targetOpacity = _config.idleOpacity;
                    _meshRenderer.enabled = true;
                    break;

                case ShieldVisibilityMode.OnlyOnHit:
                    targetOpacity = isRecentlyHit ? _config.activeOpacity : 0f;
                    _meshRenderer.enabled = _currentOpacity > 0.01f || isRecentlyHit;
                    break;

                case ShieldVisibilityMode.Both:
                    if (isRecentlyHit)
                    {
                        float fadeProgress = timeSinceHit / _config.hitFadeDuration;
                        targetOpacity = Mathf.Lerp(_config.activeOpacity, _config.idleOpacity, fadeProgress);
                    }
                    else
                    {
                        targetOpacity = _config.idleOpacity;
                    }
                    _meshRenderer.enabled = true;
                    break;

                default:
                    targetOpacity = _config.idleOpacity;
                    break;
            }

            _currentOpacity = Mathf.Lerp(_currentOpacity, targetOpacity, Time.deltaTime * 10f);
            _material.SetFloat(CurrentOpacityId, _currentOpacity);
        }

        private void UpdateShieldHealth()
        {
            float healthPercent = _shield.MaxShield > 0
                ? (float)_shield.CurrentShield / _shield.MaxShield
                : 0f;

            _material.SetFloat(ShieldHealthId, healthPercent);
        }

        private void UpdateImpactData()
        {
            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetVectorArray(ImpactPositionsId, _impactData);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        public void RegisterImpact(Vector2 worldPosition)
        {
            if (_config == null) return;

            Vector2 localPos = transform.InverseTransformPoint(worldPosition);

            _impactData[_currentImpactIndex] = new Vector4(localPos.x, localPos.y, Time.time, 0);
            _currentImpactIndex = (_currentImpactIndex + 1) % MaxImpacts;

            _lastHitTime = Time.time;
        }

        private void OnShieldDestroyed()
        {
            _isShieldDestroyed = true;
            _meshRenderer.enabled = false;
        }

        private void OnShieldRestored()
        {
            _isShieldDestroyed = false;
            UpdateInitialVisibility();
        }

        public void RefreshFromConfig()
        {
            if (_config == null || _material == null) return;

            _material.SetColor(ShieldColorId, _config.shieldColor);
            _material.SetColor(EdgeColorId, _config.edgeColor);
            _material.SetColor(HitFlashColorId, _config.hitFlashColor);
            _material.SetColor(DamagedColorId, _config.damagedColor);
            _material.SetFloat(EdgeIntensityId, _config.edgeIntensity);
            _material.SetFloat(EdgePowerId, _config.fresnelPower);
            _material.SetFloat(EdgeThicknessId, _config.edgeThickness);
            _material.SetFloat(EdgeDitherId, _config.edgeDither);
            _material.SetInt(PatternTypeId, (int)_config.patternType);
            _material.SetFloat(PatternScaleId, _config.patternScale);
            _material.SetFloat(PatternSpeedId, _config.patternSpeed);
            _material.SetFloat(PatternIntensityId, _config.patternIntensity);
            _material.SetFloat(PulseAmountId, _config.pulseAmount);
            _material.SetFloat(PulseSpeedId, _config.pulseSpeed);
            _material.SetInt(VisibilityModeId, (int)_config.visibilityMode);
            _material.SetFloat(IdleOpacityId, _config.idleOpacity);
            _material.SetFloat(ActiveOpacityId, _config.activeOpacity);
            _material.SetFloat(RippleSpeedId, _config.rippleSpeed);
            _material.SetFloat(RippleWidthId, _config.rippleWidth);
            _material.SetFloat(RippleIntensityId, _config.rippleIntensity);
            _material.SetFloat(RippleDurationId, _config.rippleDuration);
            _material.SetFloat(RippleOpacityId, _config.rippleOpacity);
            _material.SetFloat(RippleSoftnessId, _config.rippleSoftness);
            _material.SetFloat(RipplePerspectiveId, _config.ripplePerspective);
            _material.SetFloat(ImpactVisibilityRadiusId, _config.impactVisibilityRadius);
            _material.SetFloat(ImpactVisibilityFalloffId, _config.impactVisibilityFalloff);
            _material.SetFloat(ImpactVisibilitySpeedId, _config.impactVisibilitySpeed);
            _material.SetFloat(DomeCurvatureId, _config.domeCurvature);
            _material.SetFloat(DomeHighlightId, _config.domeHighlight);
            _material.SetFloat(DomeShadowId, _config.domeShadow);
        }
    }
}
