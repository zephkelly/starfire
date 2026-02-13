using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using Starfire.Entity;
using Starfire.Systems;

namespace Starfire.Network
{
    public struct ZoneCorrectionEntry
    {
        public double2 Position;
        public float2 Velocity;
        public float Timestamp;
        public byte EntityType;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClientZoneBroadcastSystem))]
    [UpdateAfter(typeof(AsteroidSensorOrbitSystem))]
    [UpdateAfter(typeof(ClientLocalPhysicsSystem))]
    public partial class ClientZoneCorrectionSystem : SystemBase
    {
        NativeHashMap<int, ZoneCorrectionEntry> _recentCorrections;

        public NativeHashMap<int, ZoneCorrectionEntry> RecentCorrections => _recentCorrections;

        const float CorrectionExpiry = 2f;

        protected override void OnCreate()
        {
            _recentCorrections = new NativeHashMap<int, ZoneCorrectionEntry>(512, Allocator.Persistent);
            RequireForUpdate<NetworkId>();
        }

        protected override void OnDestroy()
        {
            if (_recentCorrections.IsCreated) _recentCorrections.Dispose();
        }

        protected override void OnUpdate()
        {
            var clientSpawner = World.GetExistingSystemManaged<ClientWorldSpawnSystem>();
            if (clientSpawner == null || !clientSpawner.EntityIdToLocal.IsCreated) return;

            float time = (float)SystemAPI.Time.ElapsedTime;
            int localNetId = SystemAPI.GetSingleton<NetworkId>().Value;

            var entityIdToLocal = clientSpawner.EntityIdToLocal;

            var batches = new NativeList<BatchData>(16, Allocator.Temp);

            foreach (var (rpc, entity) in
                SystemAPI.Query<RefRO<ZoneEntityBatchRpc>>()
                    .WithAll<ReceiveRpcCommandRequest>()
                    .WithEntityAccess())
            {
                if (rpc.ValueRO.SourceNetworkId == localNetId)
                {
                    batches.Add(new BatchData { Rpc = rpc.ValueRO, RpcEntity = entity, Skip = true });
                    continue;
                }

                batches.Add(new BatchData { Rpc = rpc.ValueRO, RpcEntity = entity, Skip = false });
            }

            for (int b = 0; b < batches.Length; b++)
            {
                var batch = batches[b];

                if (!batch.Skip)
                {
                    for (int slot = 0; slot < batch.Rpc.Count; slot++)
                    {
                        batch.Rpc.Get(slot, out int entityId, out double2 pos, out float2 vel);

                        byte entityType = 0;
                        if (entityIdToLocal.TryGetValue(entityId, out var localEntity) && EntityManager.Exists(localEntity))
                        {
                            EntityManager.SetComponentData(localEntity, new WorldPosition { Value = pos });

                            var localPos = (float2)pos;
                            var lt = EntityManager.GetComponentData<LocalTransform>(localEntity);
                            EntityManager.SetComponentData(localEntity, lt.WithPosition(
                                new float3(localPos.x, localPos.y, lt.Position.z)));

                            if (EntityManager.HasComponent<PhysicsVelocity>(localEntity))
                            {
                                EntityManager.SetComponentData(localEntity, new PhysicsVelocity
                                {
                                    Linear = new float3(vel.x, vel.y, 0f)
                                });
                            }

                            if (EntityManager.HasComponent<EntityIdentity>(localEntity))
                                entityType = EntityManager.GetComponentData<EntityIdentity>(localEntity).EntityType;
                        }

                        var correction = new ZoneCorrectionEntry
                        {
                            Position = pos,
                            Velocity = vel,
                            Timestamp = time,
                            EntityType = entityType
                        };

                        if (_recentCorrections.ContainsKey(entityId))
                            _recentCorrections[entityId] = correction;
                        else
                            _recentCorrections.Add(entityId, correction);
                    }
                }

                EntityManager.DestroyEntity(batch.RpcEntity);
            }

            batches.Dispose();

            CleanExpiredCorrections(time);
        }

        struct BatchData
        {
            public ZoneEntityBatchRpc Rpc;
            public Unity.Entities.Entity RpcEntity;
            public bool Skip;
        }

        void CleanExpiredCorrections(float currentTime)
        {
            var expired = new NativeList<int>(32, Allocator.Temp);
            var keys = _recentCorrections.GetKeyArray(Allocator.Temp);

            for (int i = 0; i < keys.Length; i++)
            {
                if (currentTime - _recentCorrections[keys[i]].Timestamp > CorrectionExpiry)
                    expired.Add(keys[i]);
            }

            for (int i = 0; i < expired.Length; i++)
                _recentCorrections.Remove(expired[i]);

            keys.Dispose();
            expired.Dispose();
        }
    }
}
