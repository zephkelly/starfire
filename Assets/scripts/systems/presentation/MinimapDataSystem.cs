using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    public struct MinimapEntry
    {
        public int PixelX, PixelY, DotRadius;
        public byte Category;
        public byte EntityType;
        public byte Tier;
        public float HullPercent, Speed, SensorRange;
        public byte AIState;
        public int MemberCount;
        public float TotalHP;
        public byte Behavior;
        public double2 WorldPos;
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MinimapDataSystem : SystemBase
    {
        public const byte CategoryShip = 0;
        public const byte CategoryFleet = 1;
        public const byte CategoryDormant = 2;
        public const byte CategoryPlayer = 3;

        const float FleetMinDistance = 85000f;
        const float DormantMinDistance = 170000f;

        public NativeArray<Color32> Pixels;
        public NativeList<MinimapEntry> Entries;
        public NativeArray<int> CountByTier;
        public int ShipCount;
        public int FleetCount;
        public int DormantCount;
        public double2 PlayerWorldPos;
        public bool DataReady;

        public int TextureSize = 512;
        public float ViewRadius = 50000f;
        public float UpdateInterval = 0.1f;

        public int PlayerDotSize = 3;
        public int ShipDotSize = 2;
        public int FleetDotSize = 3;
        public int DormantDotSize = 1;

        public Color32 PlayerColor;
        public Color32 FleetColor;
        public Color32 DormantColor;
        public Color32 BorderColor;
        public Color32 BackgroundColor;
        public Color32[] TypeColors;

        float _lastUpdateTime;
        int _lastTextureSize;

        NativeArray<Color32> _backgroundPixels;
        bool _backgroundDirty = true;

        NativeList<int> _dirtyPixels;
        NativeArray<byte> _pixelDirtyFlag;
        NativeHashSet<int> _occupiedPixels;

        EntityQuery _playerQuery;
        EntityQuery _shipQuery;
        EntityQuery _fleetQuery;
        EntityQuery _dormantQuery;

        protected override void OnCreate()
        {
            PlayerColor = new Color32(0, 255, 0, 255);
            FleetColor = new Color32(128, 179, 255, 255);
            DormantColor = new Color32(64, 64, 64, 153);
            BorderColor = new Color32(26, 102, 26, 255);
            BackgroundColor = new Color32(5, 5, 15, 230);
            TypeColors = new Color32[]
            {
                new Color32(255, 77, 77, 255),
                new Color32(0, 204, 255, 255),
                new Color32(179, 128, 51, 255),
                new Color32(102, 102, 102, 204),
                new Color32(255, 255, 77, 255)
            };

            AllocateBuffers();

            _playerQuery = GetEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<WorldPosition>());

            _shipQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ShipTag>(),
                    ComponentType.ReadOnly<WorldPosition>(),
                    ComponentType.ReadOnly<EntityIdentity>(),
                    ComponentType.ReadOnly<SimulationTierData>(),
                    ComponentType.ReadOnly<SensorContact>()
                },
                None = new[] { ComponentType.ReadOnly<PlayerTag>() }
            });

            _fleetQuery = GetEntityQuery(
                ComponentType.ReadOnly<FleetTag>(),
                ComponentType.ReadOnly<FleetData>());

            _dormantQuery = GetEntityQuery(
                ComponentType.ReadOnly<DormantTag>(),
                ComponentType.ReadOnly<DormantRecord>());

            RequireForUpdate<PlayerTag>();
        }

        void AllocateBuffers()
        {
            int pixelCount = TextureSize * TextureSize;
            Pixels = new NativeArray<Color32>(pixelCount, Allocator.Persistent);
            _backgroundPixels = new NativeArray<Color32>(pixelCount, Allocator.Persistent);
            Entries = new NativeList<MinimapEntry>(4096, Allocator.Persistent);
            CountByTier = new NativeArray<int>(5, Allocator.Persistent);
            _dirtyPixels = new NativeList<int>(8192, Allocator.Persistent);
            _pixelDirtyFlag = new NativeArray<byte>(pixelCount, Allocator.Persistent);
            _occupiedPixels = new NativeHashSet<int>(2048, Allocator.Persistent);
            _lastTextureSize = TextureSize;
            _backgroundDirty = true;
        }

        void ReallocateIfNeeded()
        {
            if (TextureSize == _lastTextureSize) return;

            Pixels.Dispose();
            _backgroundPixels.Dispose();
            _pixelDirtyFlag.Dispose();

            int pixelCount = TextureSize * TextureSize;
            Pixels = new NativeArray<Color32>(pixelCount, Allocator.Persistent);
            _backgroundPixels = new NativeArray<Color32>(pixelCount, Allocator.Persistent);
            _pixelDirtyFlag = new NativeArray<byte>(pixelCount, Allocator.Persistent);
            _lastTextureSize = TextureSize;
            _backgroundDirty = true;
        }

        public void MarkBackgroundDirty()
        {
            _backgroundDirty = true;
        }

        void RebuildBackground()
        {
            if (!_backgroundDirty) return;
            _backgroundDirty = false;

            int size = TextureSize;
            int center = size / 2;
            float outerRadius = size / 2f;
            float borderWidth = 2f;
            var clear = new Color32(0, 0, 0, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = math.sqrt(dx * dx + dy * dy);

                    Color32 c;
                    if (dist > outerRadius)
                        c = clear;
                    else if (dist > outerRadius - borderWidth)
                        c = BorderColor;
                    else
                        c = BackgroundColor;

                    int idx = y * size + x;
                    _backgroundPixels[idx] = c;
                    Pixels[idx] = c;
                }
            }

            _dirtyPixels.Clear();
        }

        void ClearDirtyPixels()
        {
            for (int i = 0; i < _dirtyPixels.Length; i++)
            {
                int idx = _dirtyPixels[i];
                Pixels[idx] = _backgroundPixels[idx];
                _pixelDirtyFlag[idx] = 0;
            }
            _dirtyPixels.Clear();
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();

            float elapsed = (float)SystemAPI.Time.ElapsedTime;
            if (elapsed - _lastUpdateTime < UpdateInterval)
            {
                DataReady = false;
                return;
            }
            _lastUpdateTime = elapsed;

            ReallocateIfNeeded();
            RebuildBackground();
            ClearDirtyPixels();

            Entries.Clear();
            ShipCount = 0;
            FleetCount = 0;
            DormantCount = 0;
            for (int i = 0; i < 5; i++) CountByTier[i] = 0;

            if (_playerQuery.IsEmpty)
            {
                DataReady = false;
                return;
            }

            var playerPositions = _playerQuery.ToComponentDataArray<WorldPosition>(Allocator.Temp);
            if (playerPositions.Length == 0)
            {
                playerPositions.Dispose();
                DataReady = false;
                return;
            }
            PlayerWorldPos = playerPositions[0].Value;
            playerPositions.Dispose();

            double viewRadiusSq = (double)ViewRadius * ViewRadius;
            double halfInvRadius = 0.5 / ViewRadius;
            int texSize = TextureSize;
            _occupiedPixels.Clear();

            bool highZoom = ViewRadius > 200000f;
            int adaptiveShipDot = highZoom ? 1 : ShipDotSize;
            int adaptiveFleetDot = highZoom ? math.max(FleetDotSize - 1, 1) : FleetDotSize;
            int adaptiveDormantDot = highZoom ? 1 : DormantDotSize;

            var posHandle = GetComponentTypeHandle<WorldPosition>(true);
            var idHandle = GetComponentTypeHandle<EntityIdentity>(true);
            var tierHandle = GetComponentTypeHandle<SimulationTierData>(true);
            var sensorHandle = GetComponentTypeHandle<SensorContact>(true);
            ProcessShipEntities(ref posHandle, ref idHandle, ref tierHandle, ref sensorHandle,
                viewRadiusSq, halfInvRadius, texSize, adaptiveShipDot);

            if (ViewRadius >= FleetMinDistance)
            {
                var fleetHandle = GetComponentTypeHandle<FleetData>(true);
                ProcessFleetEntities(ref fleetHandle, viewRadiusSq, halfInvRadius, texSize, adaptiveFleetDot);
            }

            if (ViewRadius >= DormantMinDistance)
            {
                var dormantHandle = GetComponentTypeHandle<DormantRecord>(true);
                ProcessDormantEntities(ref dormantHandle, viewRadiusSq, halfInvRadius, texSize, adaptiveDormantDot);
            }

            int center = texSize / 2;
            DrawDot(center, center, PlayerDotSize, PlayerColor);
            Entries.Add(new MinimapEntry
            {
                PixelX = center,
                PixelY = center,
                DotRadius = PlayerDotSize,
                Category = CategoryPlayer,
                WorldPos = PlayerWorldPos
            });

            DataReady = true;
        }

        void ProcessShipEntities(
            ref ComponentTypeHandle<WorldPosition> posHandle,
            ref ComponentTypeHandle<EntityIdentity> idHandle,
            ref ComponentTypeHandle<SimulationTierData> tierHandle,
            ref ComponentTypeHandle<SensorContact> sensorHandle,
            double viewRadiusSq, double halfInvRadius, int texSize, int dotSize)
        {
            var chunks = _shipQuery.ToArchetypeChunkArray(Allocator.Temp);

            for (int c = 0; c < chunks.Length; c++)
            {
                var chunk = chunks[c];
                var positions = chunk.GetNativeArray(ref posHandle);
                var identities = chunk.GetNativeArray(ref idHandle);
                var tiers = chunk.GetNativeArray(ref tierHandle);
                var sensors = chunk.GetNativeArray(ref sensorHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    double2 rel = positions[i].Value - PlayerWorldPos;
                    double distSq = rel.x * rel.x + rel.y * rel.y;
                    if (distSq > viewRadiusSq) continue;

                    if (!RelToPixel(rel, halfInvRadius, texSize, out int px, out int py)) continue;

                    int centerIdx = py * texSize + px;
                    if (!_occupiedPixels.Add(centerIdx)) continue;

                    byte entityType = identities[i].EntityType;
                    Color32 color = entityType < TypeColors.Length ? TypeColors[entityType] : new Color32(255, 255, 255, 255);
                    DrawDot(px, py, dotSize, color);

                    byte tier = (byte)tiers[i].Tier;
                    CountByTier[math.min(tier, 4)]++;
                    ShipCount++;

                    Entries.Add(new MinimapEntry
                    {
                        PixelX = px,
                        PixelY = py,
                        DotRadius = dotSize,
                        Category = CategoryShip,
                        EntityType = entityType,
                        Tier = tier,
                        HullPercent = sensors[i].HullPercent,
                        Speed = sensors[i].Speed,
                        SensorRange = sensors[i].SensorRange,
                        AIState = sensors[i].CurrentAIState,
                        WorldPos = positions[i].Value
                    });
                }
            }

            chunks.Dispose();
        }

        void ProcessFleetEntities(ref ComponentTypeHandle<FleetData> fleetHandle,
            double viewRadiusSq, double halfInvRadius, int texSize, int dotSize)
        {
            var chunks = _fleetQuery.ToArchetypeChunkArray(Allocator.Temp);

            for (int c = 0; c < chunks.Length; c++)
            {
                var chunk = chunks[c];
                var fleets = chunk.GetNativeArray(ref fleetHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    double2 rel = fleets[i].Position - PlayerWorldPos;
                    double distSq = rel.x * rel.x + rel.y * rel.y;
                    if (distSq > viewRadiusSq) continue;

                    if (!RelToPixel(rel, halfInvRadius, texSize, out int px, out int py)) continue;

                    int centerIdx = py * texSize + px;
                    if (!_occupiedPixels.Add(centerIdx)) continue;

                    DrawDot(px, py, dotSize, FleetColor);
                    CountByTier[3]++;
                    FleetCount++;

                    Entries.Add(new MinimapEntry
                    {
                        PixelX = px,
                        PixelY = py,
                        DotRadius = dotSize,
                        Category = CategoryFleet,
                        MemberCount = fleets[i].MemberCount,
                        TotalHP = fleets[i].TotalHP,
                        Behavior = fleets[i].CurrentBehavior,
                        WorldPos = fleets[i].Position
                    });
                }
            }

            chunks.Dispose();
        }

        void ProcessDormantEntities(ref ComponentTypeHandle<DormantRecord> dormantHandle,
            double viewRadiusSq, double halfInvRadius, int texSize, int dotSize)
        {
            var chunks = _dormantQuery.ToArchetypeChunkArray(Allocator.Temp);

            for (int c = 0; c < chunks.Length; c++)
            {
                var chunk = chunks[c];
                var dormants = chunk.GetNativeArray(ref dormantHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var chunkCenter = new double2(
                        dormants[i].ChunkX * 1000.0 + 500.0,
                        dormants[i].ChunkY * 1000.0 + 500.0);

                    double2 rel = chunkCenter - PlayerWorldPos;
                    double distSq = rel.x * rel.x + rel.y * rel.y;
                    if (distSq > viewRadiusSq) continue;

                    if (!RelToPixel(rel, halfInvRadius, texSize, out int px, out int py)) continue;

                    int centerIdx = py * texSize + px;
                    if (!_occupiedPixels.Add(centerIdx)) continue;

                    DrawDot(px, py, dotSize, DormantColor);
                    CountByTier[4]++;
                    DormantCount++;

                    Entries.Add(new MinimapEntry
                    {
                        PixelX = px,
                        PixelY = py,
                        DotRadius = dotSize,
                        Category = CategoryDormant,
                        EntityType = dormants[i].EntityType,
                        MemberCount = dormants[i].Count,
                        WorldPos = chunkCenter
                    });
                }
            }

            chunks.Dispose();
        }

        static bool RelToPixel(double2 rel, double halfInvRadius, int texSize, out int px, out int py)
        {
            double nx = rel.x * halfInvRadius + 0.5;
            double ny = rel.y * halfInvRadius + 0.5;

            px = (int)(nx * texSize);
            py = (int)(ny * texSize);

            return px >= 0 && px < texSize && py >= 0 && py < texSize;
        }

        void DrawPixel(int px, int py, int size, Color32 color)
        {
            if (px < 0 || px >= size || py < 0 || py >= size) return;
            int idx = py * size + px;
            Pixels[idx] = color;
            if (_pixelDirtyFlag[idx] == 0)
            {
                _pixelDirtyFlag[idx] = 1;
                _dirtyPixels.Add(idx);
            }
        }

        void DrawDot(int cx, int cy, int radius, Color32 color)
        {
            int size = TextureSize;

            if (radius <= 0)
            {
                DrawPixel(cx, cy, size, color);
                return;
            }

            if (radius == 1)
            {
                DrawPixel(cx, cy, size, color);
                DrawPixel(cx - 1, cy, size, color);
                DrawPixel(cx + 1, cy, size, color);
                DrawPixel(cx, cy - 1, size, color);
                DrawPixel(cx, cy + 1, size, color);
                return;
            }

            int radiusSq = radius * radius;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y > radiusSq) continue;
                    DrawPixel(cx + x, cy + y, size, color);
                }
            }
        }

        protected override void OnDestroy()
        {
            if (Pixels.IsCreated) Pixels.Dispose();
            if (_backgroundPixels.IsCreated) _backgroundPixels.Dispose();
            if (Entries.IsCreated) Entries.Dispose();
            if (CountByTier.IsCreated) CountByTier.Dispose();
            if (_dirtyPixels.IsCreated) _dirtyPixels.Dispose();
            if (_pixelDirtyFlag.IsCreated) _pixelDirtyFlag.Dispose();
            if (_occupiedPixels.IsCreated) _occupiedPixels.Dispose();
        }
    }
}
