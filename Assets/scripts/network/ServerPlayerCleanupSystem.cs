using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Starfire.Entity;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ServerPlayerCleanupSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var activeNetIds = new NativeHashSet<int>(16, Allocator.Temp);

            foreach (var netId in SystemAPI.Query<RefRO<NetworkId>>()
                .WithAll<NetworkStreamInGame>())
            {
                activeNetIds.Add(netId.ValueRO.Value);
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            bool destroyed = false;

            foreach (var (ghostOwner, playerName, entity) in
                SystemAPI.Query<RefRO<GhostOwner>, RefRO<PlayerName>>()
                    .WithAll<PlayerTag>()
                    .WithEntityAccess())
            {
                if (!activeNetIds.Contains(ghostOwner.ValueRO.NetworkId))
                {
                    UnityEngine.Debug.Log(
                        $"[ServerPlayerCleanup] Destroying player entity for disconnected NetworkId={ghostOwner.ValueRO.NetworkId} name='{playerName.ValueRO.Value}'");
                    ecb.DestroyEntity(entity);
                    destroyed = true;
                }
            }

            if (destroyed)
            {
                ecb.Playback(state.EntityManager);
            }

            ecb.Dispose();
            activeNetIds.Dispose();
        }
    }
}
