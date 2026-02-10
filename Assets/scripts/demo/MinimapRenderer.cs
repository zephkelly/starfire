using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Starfire.Systems;

namespace Starfire.Demo
{
    public class MinimapRenderer : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] int _textureSize = 512;
        [SerializeField] float _minimapScreenSize = 250f;
        [SerializeField] float _margin = 10f;

        [Header("Update Rate")]
        [SerializeField] float _updateInterval = 0.1f;

        [Header("Zoom")]
        [SerializeField] float _viewRadius = 50000f;
        [SerializeField] float _minViewRadius = 500f;
        [SerializeField] float _maxViewRadius = 600000f;
        [SerializeField] float _zoomSpeed = 0.15f;

        [Header("Dot Sizes")]
        [SerializeField] int _playerDotSize = 3;
        [SerializeField] int _shipDotSize = 2;
        [SerializeField] int _fleetDotSize = 3;
        [SerializeField] int _dormantDotSize = 1;
        [SerializeField] int _asteroidDotSize = 1;
        [SerializeField] int _starDotSize = 3;
        [SerializeField] int _asteroidFieldDotSize = 2;

        [Header("Colors")]
        [SerializeField] Color _backgroundColor = new Color(0.02f, 0.02f, 0.06f, 0.9f);
        [SerializeField] Color _borderColor = new Color(0.1f, 0.4f, 0.1f, 1f);
        [SerializeField] Color _playerColor = new Color(0f, 1f, 0f, 1f);
        [SerializeField] Color _fleetColor = new Color(0.5f, 0.7f, 1f, 1f);
        [SerializeField] Color _dormantColor = new Color(0.25f, 0.25f, 0.25f, 0.6f);

        [Header("Type Colors")]
        [SerializeField] Color _shipTypeColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] Color _stationTypeColor = new Color(0f, 0.8f, 1f, 1f);
        [SerializeField] Color _asteroidTypeColor = new Color(0.7f, 0.5f, 0.2f, 1f);
        [SerializeField] Color _debrisTypeColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        [SerializeField] Color _projectileTypeColor = new Color(1f, 1f, 0.3f, 1f);
        [SerializeField] Color _starTypeColor = new Color(1f, 0.94f, 0.78f, 1f);
        [SerializeField] Color _asteroidFieldColor = new Color(0.43f, 0.37f, 0.27f, 0.55f);

        static readonly string[] TypeNames = { "Ship", "Station", "Asteroid", "Debris", "Projectile", "Star" };
        static readonly string[] TierNames = { "Loaded", "Active", "Sensor", "Strategic", "Dormant" };
        static readonly string[] AIStateNames = { "Idle", "Patrol", "Pursue", "Combat", "Flee" };
        static readonly string[] FleetBehaviorNames = { "Patrol", "Intercept", "Retreat", "Hold" };

        Texture2D _texture;
        Texture2D _swatchTexture;
        Rect _screenRect;

        MinimapDataSystem _system;
        bool _systemCached;

        MinimapEntry[] _cachedEntries;
        int _cachedEntryCount;
        int _hoveredIndex = -1;
        int _selectedIndex = -1;
        double2 _playerWorldPos;
        int _shipCount, _fleetCount, _dormantCount, _asteroidCount, _starCount, _asteroidFieldCount;
        int[] _countByTier = new int[5];

        GUIStyle _labelStyle;
        GUIStyle _tooltipBoxStyle;
        GUIStyle _tooltipTextStyle;
        GUIStyle _statsStyle;

        void Start()
        {
            _texture = new Texture2D(_textureSize, _textureSize, TextureFormat.RGBA32, false);
            _texture.filterMode = FilterMode.Point;

            _swatchTexture = new Texture2D(1, 1);
            _swatchTexture.SetPixel(0, 0, Color.white);
            _swatchTexture.Apply();

            _cachedEntries = new MinimapEntry[4096];
        }

        bool TryCacheSystem()
        {
            if (_systemCached) return _system != null;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return false;

            _system = world.GetExistingSystemManaged<MinimapDataSystem>();
            _systemCached = _system != null;

            if (_systemCached)
                PushConfig();

            return _systemCached;
        }

        void PushConfig()
        {
            _system.TextureSize = _textureSize;
            _system.ViewRadius = _viewRadius;
            _system.UpdateInterval = _updateInterval;
            _system.PlayerDotSize = _playerDotSize;
            _system.ShipDotSize = _shipDotSize;
            _system.FleetDotSize = _fleetDotSize;
            _system.DormantDotSize = _dormantDotSize;
            _system.AsteroidDotSize = _asteroidDotSize;
            _system.StarDotSize = _starDotSize;
            _system.AsteroidFieldDotSize = _asteroidFieldDotSize;
            _system.PlayerColor = ToColor32(_playerColor);
            _system.FleetColor = ToColor32(_fleetColor);
            _system.DormantColor = ToColor32(_dormantColor);
            _system.AsteroidColor = ToColor32(_asteroidTypeColor);
            _system.StarColor = ToColor32(_starTypeColor);
            _system.AsteroidFieldColor = ToColor32(_asteroidFieldColor);
            _system.BorderColor = ToColor32(_borderColor);
            _system.BackgroundColor = ToColor32(_backgroundColor);
            _system.TypeColors = new[]
            {
                ToColor32(_shipTypeColor),
                ToColor32(_stationTypeColor),
                ToColor32(_asteroidTypeColor),
                ToColor32(_debrisTypeColor),
                ToColor32(_projectileTypeColor),
                ToColor32(_starTypeColor)
            };
            _system.MarkBackgroundDirty();
        }

        void LateUpdate()
        {
            if (!TryCacheSystem()) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                _systemCached = false;
                return;
            }

            _screenRect = new Rect(
                Screen.width - _minimapScreenSize - _margin,
                _margin,
                _minimapScreenSize,
                _minimapScreenSize);

            HandleZoomInput();

            if (_system.DataReady)
            {
                _texture.SetPixelData(_system.Pixels, 0);
                _texture.Apply(false);

                CacheEntries();

                _playerWorldPos = _system.PlayerWorldPos;
                _shipCount = _system.ShipCount;
                _fleetCount = _system.FleetCount;
                _dormantCount = _system.DormantCount;
                _asteroidCount = _system.AsteroidCount;
                _starCount = _system.StarCount;
                _asteroidFieldCount = _system.AsteroidFieldCount;
                for (int i = 0; i < 5; i++)
                    _countByTier[i] = _system.CountByTier[i];
            }

            HandleHoverAndClick();
        }

        void CacheEntries()
        {
            int count = _system.Entries.Length;
            if (_cachedEntries.Length < count)
                _cachedEntries = new MinimapEntry[count * 2];

            for (int i = 0; i < count; i++)
                _cachedEntries[i] = _system.Entries[i];

            _cachedEntryCount = count;

            if (_selectedIndex >= _cachedEntryCount)
                _selectedIndex = -1;
        }

        void HandleZoomInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var mouseScreen = mouse.position.ReadValue();
            var mouseGui = new Vector2(mouseScreen.x, Screen.height - mouseScreen.y);

            if (!_screenRect.Contains(mouseGui)) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (math.abs(scroll) < 0.01f) return;

            float factor = 1f - math.sign(scroll) * _zoomSpeed;
            _viewRadius = math.clamp(_viewRadius * factor, _minViewRadius, _maxViewRadius);
            _system.ViewRadius = _viewRadius;
        }

        void HandleHoverAndClick()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var mouseScreen = mouse.position.ReadValue();
            var mouseGui = new Vector2(mouseScreen.x, Screen.height - mouseScreen.y);

            if (!_screenRect.Contains(mouseGui))
            {
                _hoveredIndex = -1;
                return;
            }

            float relX = (mouseGui.x - _screenRect.x) / _screenRect.width;
            float relY = (mouseGui.y - _screenRect.y) / _screenRect.height;
            int pixelX = (int)(relX * _textureSize);
            int pixelY = (int)((1f - relY) * _textureSize);

            _hoveredIndex = -1;
            float closestDistSq = float.MaxValue;

            for (int i = 0; i < _cachedEntryCount; i++)
            {
                var e = _cachedEntries[i];
                float dx = pixelX - e.PixelX;
                float dy = pixelY - e.PixelY;
                float distSq = dx * dx + dy * dy;
                float hitRadius = e.DotRadius + 4f;

                if (distSq < hitRadius * hitRadius && distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    _hoveredIndex = i;
                }
            }

            if (mouse.leftButton.wasPressedThisFrame)
                _selectedIndex = _hoveredIndex;
        }

        void OnGUI()
        {
            if (_texture == null) return;

            EnsureStyles();

            GUI.DrawTexture(_screenRect, _texture);

            float yOffset = _screenRect.yMax + 4f;
            yOffset = DrawRangeLabel(yOffset);
            yOffset = DrawLegend(yOffset);
            DrawStats(yOffset);
            DrawTooltip();
        }

        void EnsureStyles()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            _labelStyle.normal.textColor = _borderColor;

            _statsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft
            };
            _statsStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1f);

            var tooltipBg = new Texture2D(1, 1);
            tooltipBg.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.1f, 0.92f));
            tooltipBg.Apply();

            _tooltipBoxStyle = new GUIStyle(GUI.skin.box);
            _tooltipBoxStyle.normal.background = tooltipBg;
            _tooltipBoxStyle.padding = new RectOffset(6, 6, 4, 4);

            _tooltipTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                wordWrap = true
            };
            _tooltipTextStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        }

        float DrawRangeLabel(float y)
        {
            string rangeText = _viewRadius >= 1000f
                ? $"Range: {_viewRadius / 1000f:F0}k"
                : $"Range: {_viewRadius:F0}";

            GUI.Label(new Rect(_screenRect.x, y, _screenRect.width, 16f), rangeText, _labelStyle);
            return y + 18f;
        }

        float DrawLegend(float y)
        {
            float startX = _screenRect.x;
            float colWidth = _screenRect.width / 3f;
            float rowHeight = 14f;

            DrawSwatchLabel(startX, y, _playerColor, "Player", colWidth);
            DrawSwatchLabel(startX + colWidth, y, _shipTypeColor, "Ship", colWidth);
            DrawSwatchLabel(startX + colWidth * 2f, y, _starTypeColor, "Star", colWidth);
            y += rowHeight;

            DrawSwatchLabel(startX, y, _asteroidTypeColor, "Asteroid", colWidth);
            DrawSwatchLabel(startX + colWidth, y, _fleetColor, "Fleet", colWidth);
            DrawSwatchLabel(startX + colWidth * 2f, y, _dormantColor, "Dormant", colWidth);
            y += rowHeight;

            DrawSwatchLabel(startX, y, _asteroidFieldColor, "A.Field", colWidth);
            DrawSwatchLabel(startX + colWidth, y, _stationTypeColor, "Station", colWidth);
            y += rowHeight + 4f;

            return y;
        }

        void DrawSwatchLabel(float x, float y, Color color, string label, float width)
        {
            var prevColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x + 2f, y + 3f, 8f, 8f), _swatchTexture);
            GUI.color = prevColor;

            GUI.Label(new Rect(x + 12f, y, width - 14f, 14f), label, _statsStyle);
        }

        float DrawStats(float y)
        {
            float startX = _screenRect.x + 2f;
            float width = _screenRect.width - 4f;

            var tierLine = $"T0:{_countByTier[0]}  T1:{_countByTier[1]}  T2:{_countByTier[2]}  T3:{_countByTier[3]}  T4:{_countByTier[4]}";
            GUI.Label(new Rect(startX, y, width, 14f), tierLine, _statsStyle);
            y += 14f;

            var shipLine = $"Ships:{_shipCount}  Fleets:{_fleetCount}  Dormant:{_dormantCount}";
            GUI.Label(new Rect(startX, y, width, 14f), shipLine, _statsStyle);
            y += 14f;

            var celestialLine = $"Asteroids:{_asteroidCount}  Stars:{_starCount}  Fields:{_asteroidFieldCount}";
            GUI.Label(new Rect(startX, y, width, 14f), celestialLine, _statsStyle);
            y += 16f;

            return y;
        }

        void DrawTooltip()
        {
            int idx = _selectedIndex >= 0 && _selectedIndex < _cachedEntryCount
                ? _selectedIndex
                : _hoveredIndex;

            if (idx < 0 || idx >= _cachedEntryCount) return;

            var entry = _cachedEntries[idx];
            string text = FormatTooltip(entry);

            var mouse = Mouse.current;
            if (mouse == null) return;

            var mouseScreen = mouse.position.ReadValue();
            var mouseGui = new Vector2(mouseScreen.x, Screen.height - mouseScreen.y);

            float tooltipWidth = 180f;
            float tooltipHeight;
            switch (entry.Category)
            {
                case MinimapDataSystem.CategoryShip: tooltipHeight = 72f; break;
                case MinimapDataSystem.CategoryPlayer: tooltipHeight = 24f; break;
                case MinimapDataSystem.CategoryAsteroid:
                case MinimapDataSystem.CategoryStar: tooltipHeight = 36f; break;
                default: tooltipHeight = 48f; break;
            }

            float tx = mouseGui.x + 14f;
            float ty = mouseGui.y - tooltipHeight - 4f;

            if (tx + tooltipWidth > Screen.width)
                tx = mouseGui.x - tooltipWidth - 14f;
            if (ty < 0f)
                ty = mouseGui.y + 20f;

            var tooltipRect = new Rect(tx, ty, tooltipWidth, tooltipHeight);
            GUI.Box(tooltipRect, GUIContent.none, _tooltipBoxStyle);
            GUI.Label(new Rect(tx + 6f, ty + 4f, tooltipWidth - 12f, tooltipHeight - 8f), text, _tooltipTextStyle);
        }

        string FormatTooltip(MinimapEntry entry)
        {
            double dist = math.length(entry.WorldPos - _playerWorldPos);
            string distStr = dist >= 1000.0 ? $"{dist / 1000.0:F1}k" : $"{dist:F0}";

            switch (entry.Category)
            {
                case MinimapDataSystem.CategoryPlayer:
                    return "Player (You)";

                case MinimapDataSystem.CategoryShip:
                    string typeName = entry.EntityType < TypeNames.Length ? TypeNames[entry.EntityType] : "Unknown";
                    string tierName = entry.Tier < TierNames.Length ? TierNames[entry.Tier] : "?";
                    string aiName = entry.AIState < AIStateNames.Length ? AIStateNames[entry.AIState] : "?";
                    return $"{typeName} | {tierName}\n" +
                           $"Hull: {entry.HullPercent * 100f:F0}% | Spd: {entry.Speed:F0}\n" +
                           $"AI: {aiName} | Rng: {entry.SensorRange:F0}\n" +
                           $"Dist: {distStr}";

                case MinimapDataSystem.CategoryFleet:
                    string behavior = entry.Behavior < FleetBehaviorNames.Length
                        ? FleetBehaviorNames[entry.Behavior] : "?";
                    return $"Fleet ({entry.MemberCount} ships)\n" +
                           $"HP: {entry.TotalHP:F0} | {behavior}\n" +
                           $"Dist: {distStr}";

                case MinimapDataSystem.CategoryDormant:
                    string dType = entry.EntityType < TypeNames.Length ? TypeNames[entry.EntityType] : "Unknown";
                    string countStr = entry.MemberCount > 1 ? $" ({entry.MemberCount})" : "";
                    return $"Dormant {dType}{countStr}\n" +
                           $"Dist: {distStr}";

                case MinimapDataSystem.CategoryAsteroid:
                    string aTier = entry.Tier < TierNames.Length ? TierNames[entry.Tier] : "?";
                    return $"Asteroid | {aTier}\nDist: {distStr}";

                case MinimapDataSystem.CategoryStar:
                    return $"Star\nDist: {distStr}";

                case MinimapDataSystem.CategoryAsteroidField:
                    return $"Asteroid Field ({entry.MemberCount})\n" +
                           $"Mass: {entry.TotalHP:F0}\nDist: {distStr}";

                default:
                    return "Unknown";
            }
        }

        static Color32 ToColor32(Color c)
        {
            return new Color32(
                (byte)(c.r * 255f),
                (byte)(c.g * 255f),
                (byte)(c.b * 255f),
                (byte)(c.a * 255f));
        }

        void OnDestroy()
        {
            if (_texture != null) Destroy(_texture);
            if (_swatchTexture != null) Destroy(_swatchTexture);
        }
    }
}
