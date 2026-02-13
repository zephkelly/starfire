using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Starfire.Network;
using Starfire.Sim;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class ServerSpawnSystem : SystemBase
    {
        bool _hasSpawned;

        protected override void OnCreate()
        {
            RequireForUpdate<SpawnConfig>();
            RequireForUpdate<SimulationConfig>();
            RequireForUpdate<AsteroidConfig>();
        }

        protected override void OnUpdate()
        {
            if (_hasSpawned)
            {
                Enabled = false;
                return;
            }

            _hasSpawned = true;

            var spawnConfig = SystemAPI.GetSingleton<SpawnConfig>();
            var simConfig = SystemAPI.GetSingleton<SimulationConfig>();
            var asteroidConfig = SystemAPI.GetSingleton<AsteroidConfig>();

            var tracker = World.GetExistingSystemManaged<ServerEntityTracker>();
            tracker.Initialize(spawnConfig, simConfig, asteroidConfig);

            Debug.Log($"[ServerSpawnSystem] Initialized ServerEntityTracker for zone relay routing.");

            Enabled = false;
        }
    }
}
