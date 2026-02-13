using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using Starfire.Core;
using Starfire.Entity;
using Starfire.Sim;
using Starfire.Simulation;
using Unity.NetCode;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TierTransitionCleanupSystem))]
    public partial struct ShipFleetDisbandSystem : ISystem
    {
        const int MaxFleetsPerTick = 3;

        EntityArchetype _shipArchetype;
        bool _archetypeCreated;
        float _lastUpdateTime;

        public void OnCreate(ref SystemState state)
        {
            _lastUpdateTime = -0.667f;
            state.RequireForUpdate<SimulationConfig>();
            state.RequireForUpdate<PlayerTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            if (elapsedTime - _lastUpdateTime < 1.0f)
                return;
            _lastUpdateTime = elapsedTime;

            if (!_archetypeCreated)
            {
                _shipArchetype = state.EntityManager.CreateArchetype(
                    typeof(LocalTransform),
                    typeof(LocalToWorld),
                    typeof(WorldPosition),
                    typeof(EntityIdentity),
                    typeof(SimulationTierData),
                    typeof(TierTransition),
                    typeof(ControlInput),
                    typeof(ShipTag),
                    typeof(ShipHull),
                    typeof(ShipRotation),
                    typeof(ShipPropulsion),
                    typeof(ShipSnapshot),
                    typeof(SensorContact),
                    typeof(RichTierTag),
                    typeof(VisualTierTag),
                    typeof(SensorTierTag),
                    typeof(PhysicsCollider),
                    typeof(PhysicsMass),
                    typeof(PhysicsVelocity),
                    typeof(PhysicsDamping),
                    typeof(PhysicsGravityFactor),
                    typeof(PhysicsWorldIndex),
                    typeof(DisableRendering));
                _archetypeCreated = true;
            }

            var config = SystemAPI.GetSingleton<SimulationConfig>();

            double2 playerWorldPos = double2.zero;
            foreach (var (worldPos, _) in SystemAPI.Query<RefRO<WorldPosition>, RefRO<PlayerTag>>())
            {
                playerWorldPos = worldPos.ValueRO.Value;
                break;
            }

            double threshold = config.Bounds.Tier2MaxDistance - config.Bounds.Tier2Hysteresis;
            double thresholdSq = threshold * threshold;
            int fleetsProcessed = 0;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (fleetData, entity) in
                SystemAPI.Query<RefRO<FleetData>>()
                    .WithAll<FleetTag>()
                    .WithEntityAccess())
            {
                if (fleetsProcessed >= MaxFleetsPerTick)
                    break;

                double2 delta = fleetData.ValueRO.Position - playerWorldPos;
                double distSq = delta.x * delta.x + delta.y * delta.y;
                if (distSq >= thresholdSq)
                    continue;

                var buffer = state.EntityManager.GetBuffer<FleetMember>(entity);

                for (int i = 0; i < buffer.Length; i++)
                {
                    var member = buffer[i];
                    var memberPos = fleetData.ValueRO.Position + (double2)member.FormationOffset;
                    var localPos = (float2)memberPos;

                    var shipEntity = ecb.CreateEntity(_shipArchetype);

                    ecb.SetComponent(shipEntity, LocalTransform.FromPositionRotation(
                        new float3(localPos.x, localPos.y, 0f), quaternion.identity));

                    ecb.SetComponent(shipEntity, new WorldPosition { Value = memberPos });

                    ecb.SetComponent(shipEntity, new EntityIdentity
                    {
                        Id = member.EntityId,
                        EntityType = (byte)Starfire.Entity.EntityType.Ship,
                        Persistence = member.Persistence,
                        FactionId = fleetData.ValueRO.FactionIndex,
                        ConfigId = member.Snapshot.ConfigId
                    });

                    ecb.SetComponent(shipEntity, new SimulationTierData
                    {
                        Tier = SimulationTier.Sensor,
                        LastUpdatedTime = elapsedTime
                    });

                    ecb.SetComponent(shipEntity, new ShipHull
                    {
                        ConfigId = member.Snapshot.ConfigId,
                        CurrentHealth = member.Snapshot.HullPercent * 100f,
                        MaxHealth = 100f,
                        EfficiencyCoefficient = 1f,
                        IsEnabled = 1
                    });

                    ecb.SetComponent(shipEntity, new ShipPropulsion
                    {
                        ConfigId = member.Snapshot.ConfigId,
                        CurrentHealth = member.Snapshot.PropulsionEfficiency * 100f,
                        MaxHealth = 100f,
                        MaxSpeed = 200f,
                        Acceleration = 60f,
                        DragCoefficient = 0.4f,
                        IsEnabled = 1
                    });

                    ecb.SetComponent(shipEntity, new ShipRotation
                    {
                        ConfigId = member.Snapshot.ConfigId,
                        CurrentHealth = member.Snapshot.RotationEfficiency * 100f,
                        MaxHealth = 100f,
                        TurnRate = 180f,
                        CurrentHeading = member.Snapshot.Heading,
                        IsEnabled = 1
                    });

                    ecb.SetComponent(shipEntity, member.Snapshot);

                    ecb.SetComponent(shipEntity, new SensorContact
                    {
                        HullPercent = member.Snapshot.HullPercent,
                        ShieldPercent = member.Snapshot.ShieldPercent,
                        Speed = (float)math.length(member.Snapshot.Velocity),
                        Heading = member.Snapshot.Heading,
                        MaxSpeed = 200f,
                        WeaponRange = 500f,
                        SensorRange = 2000f,
                        CombatStrength = member.Strength,
                        CurrentAIState = member.Snapshot.AIState,
                        StateTimer = 0f,
                        TargetEntityId = -1,
                        Waypoint = member.Snapshot.Waypoint,
                        ShieldsActive = 1,
                        WeaponsArmed = 1
                    });

                    ecb.SetComponent(shipEntity, new PhysicsDamping { Linear = 0.05f, Angular = 5f });
                    ecb.SetComponent(shipEntity, new PhysicsGravityFactor { Value = 0f });

                    ecb.SetComponentEnabled<RichTierTag>(shipEntity, false);
                    ecb.SetComponentEnabled<VisualTierTag>(shipEntity, false);
                    ecb.SetComponentEnabled<SensorTierTag>(shipEntity, true);
                    ecb.SetComponentEnabled<TierTransition>(shipEntity, false);
                    ecb.RemoveComponent<PhysicsWorldIndex>(shipEntity);
                }

                ecb.DestroyEntity(entity);
                fleetsProcessed++;
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
