using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Starfire.Core;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;

namespace Starfire.Network
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct GoInGameSystem : ISystem
    {
        EntityQuery _shipPrefabQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnConfig>();

            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ShipTag, Prefab>()
                .WithOptions(EntityQueryOptions.IncludePrefab);
            _shipPrefabQuery = state.GetEntityQuery(builder);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_shipPrefabQuery.IsEmpty)
                return;

            var shipPrefab = _shipPrefabQuery.GetSingletonEntity();
            var spawnConfig = SystemAPI.GetSingleton<SpawnConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (reqSrc, reqEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>()
                .WithAll<GoInGameRequest>()
                .WithEntityAccess())
            {
                var networkId = SystemAPI.GetComponent<NetworkId>(reqSrc.ValueRO.SourceConnection);
                Debug.Log($"[GoInGameSystem] Client {networkId.Value} going in game");

                var playerShip = ecb.Instantiate(shipPrefab);

                ecb.SetComponent(playerShip, LocalTransform.FromPositionRotation(
                    new float3(0f, 0f, 0f), quaternion.identity));
                ecb.SetComponent(playerShip, new WorldPosition { Value = double2.zero });
                ecb.SetComponent(playerShip, new EntityIdentity
                {
                    Id = networkId.Value + 10000,
                    EntityType = (byte)Starfire.Entity.EntityType.Ship,
                    Persistence = 2,
                    FactionId = 0,
                    ConfigId = 0
                });
                ecb.SetComponent(playerShip, new SimulationTierData
                {
                    Tier = SimulationTier.Loaded,
                    LastUpdatedTime = 0f
                });
                ecb.SetComponent(playerShip, new ShipHull
                {
                    ConfigId = 0,
                    CurrentHealth = spawnConfig.DefaultMaxHealth,
                    MaxHealth = spawnConfig.DefaultMaxHealth,
                    MaxTemperature = 500f,
                    EfficiencyCoefficient = 1f,
                    HullType = 0,
                    IsEnabled = 1
                });
                ecb.SetComponent(playerShip, new ShipPropulsion
                {
                    ConfigId = 0,
                    CurrentHealth = spawnConfig.DefaultMaxHealth,
                    MaxHealth = spawnConfig.DefaultMaxHealth,
                    MaxSpeed = spawnConfig.DefaultMaxSpeed,
                    Acceleration = spawnConfig.DefaultAcceleration,
                    DragCoefficient = 0.4f,
                    IsEnabled = 1
                });
                ecb.SetComponent(playerShip, new ShipRotation
                {
                    ConfigId = 0,
                    CurrentHealth = spawnConfig.DefaultMaxHealth,
                    MaxHealth = spawnConfig.DefaultMaxHealth,
                    TurnRate = spawnConfig.DefaultTurnRate,
                    IsEnabled = 1
                });
                ecb.SetComponent(playerShip, new SensorContact
                {
                    HullPercent = 1f,
                    ShieldPercent = 1f,
                    SensorRange = spawnConfig.PlayerSensorRange,
                    WeaponRange = 500f,
                    MaxSpeed = spawnConfig.DefaultMaxSpeed
                });

                ecb.AddComponent<PlayerTag>(playerShip);
                ecb.AddComponent(playerShip, new GhostOwner { NetworkId = networkId.Value });

                ecb.SetComponentEnabled<RichTierTag>(playerShip, true);
                ecb.SetComponentEnabled<VisualTierTag>(playerShip, true);
                ecb.SetComponentEnabled<SensorTierTag>(playerShip, false);
                ecb.SetComponentEnabled<TierTransition>(playerShip, false);

                ecb.AddComponent(reqSrc.ValueRO.SourceConnection, new NetworkStreamInGame());
                ecb.AddComponent(reqSrc.ValueRO.SourceConnection, new CommandTarget { targetEntity = playerShip });

                ecb.DestroyEntity(reqEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    public struct GoInGameRequest : IRpcCommand { }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ClientPlayerTagSystem : ISystem
    {
        bool _done;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
            state.RequireForUpdate<NetworkStreamInGame>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_done)
            {
                state.Enabled = false;
                return;
            }

            int localNetId = SystemAPI.GetSingleton<NetworkId>().Value;
            if (localNetId == 0)
                return;

            Unity.Entities.Entity targetEntity = Unity.Entities.Entity.Null;

            foreach (var (ghostOwner, entity) in SystemAPI.Query<RefRO<GhostOwner>>()
                .WithAll<ShipTag>()
                .WithNone<PlayerTag>()
                .WithEntityAccess())
            {
                if (ghostOwner.ValueRO.NetworkId == localNetId)
                {
                    targetEntity = entity;
                    break;
                }
            }

            if (targetEntity != Unity.Entities.Entity.Null)
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                ecb.AddComponent<PlayerTag>(targetEntity);
                ecb.Playback(state.EntityManager);
                ecb.Dispose();

                if (SystemAPI.HasSingleton<GhostPredictionSwitchingQueues>())
                {
                    SystemAPI.GetSingletonRW<GhostPredictionSwitchingQueues>().ValueRW
                        .ConvertToPredictedQueue.Enqueue(new ConvertPredictionEntry
                        {
                            TargetEntity = targetEntity,
                            TransitionDurationSeconds = 0f
                        });
                    Debug.Log($"[ClientPlayerTagSystem] Tagged player entity={targetEntity} for NetworkId={localNetId}, switched to predicted");
                }
                else
                {
                    Debug.LogWarning($"[ClientPlayerTagSystem] Tagged player entity={targetEntity} but GhostPredictionSwitchingQueues not available — player ship will be interpolated");
                }

                _done = true;
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct GoInGameClientSystem : ISystem
    {
        bool _hasSentRequest;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_hasSentRequest)
            {
                state.Enabled = false;
                return;
            }

            var connectionQuery = SystemAPI.QueryBuilder()
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>()
                .Build();

            if (connectionQuery.IsEmpty)
                return;

            _hasSentRequest = true;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                .WithNone<NetworkStreamInGame>()
                .WithEntityAccess())
            {
                ecb.AddComponent<NetworkStreamInGame>(entity);
                ecb.AddComponent(entity, new AutoCommandTarget { Enabled = true });

                var reqEntity = ecb.CreateEntity();
                ecb.AddComponent<GoInGameRequest>(reqEntity);
                ecb.AddComponent(reqEntity, new SendRpcCommandRequest
                {
                    TargetConnection = entity
                });

                Debug.Log("[GoInGameClientSystem] Sending go-in-game request");
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
