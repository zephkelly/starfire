using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Starfire.Entity;
using Starfire.Simulation;
using Unity.NetCode;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TierEvaluationSystem))]
    public partial class ShipTierTransitionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (transition, entity) in
                SystemAPI.Query<RefRO<TierTransition>>()
                    .WithAll<ShipHull>()
                    .WithEntityAccess())
            {
                var from = transition.ValueRO.PreviousTier;
                var to = transition.ValueRO.NewTier;

                if (from <= SimulationTier.Active && to == SimulationTier.Sensor)
                    SnapshotToSensor(entity);
                else if (from == SimulationTier.Sensor && to <= SimulationTier.Active)
                    RestoreFromSensor(entity);
            }
        }

        void SnapshotToSensor(Unity.Entities.Entity entity)
        {
            var hull = EntityManager.GetComponentData<ShipHull>(entity);
            var propulsion = EntityManager.GetComponentData<ShipPropulsion>(entity);
            var rotation = EntityManager.GetComponentData<ShipRotation>(entity);
            var worldPos = EntityManager.GetComponentData<WorldPosition>(entity);

            float speed = 0f;
            float2 velocity = float2.zero;
            if (EntityManager.HasComponent<PhysicsVelocity>(entity))
            {
                var vel = EntityManager.GetComponentData<PhysicsVelocity>(entity);
                speed = math.length(vel.Linear.xy);
                velocity = vel.Linear.xy;
            }

            var sensor = EntityManager.GetComponentData<SensorContact>(entity);
            sensor.HullPercent = hull.CurrentHealth / math.max(hull.MaxHealth, 0.001f);
            sensor.MaxSpeed = propulsion.MaxSpeed;
            sensor.Speed = speed;
            sensor.Heading = rotation.CurrentHeading;
            sensor.CurrentAIState = 0;
            sensor.StateTimer = 0f;
            EntityManager.SetComponentData(entity, sensor);

            var snapshot = EntityManager.GetComponentData<ShipSnapshot>(entity);
            snapshot.Position = worldPos.Value;
            snapshot.Velocity = new double2(velocity.x, velocity.y);
            snapshot.Heading = rotation.CurrentHeading;
            snapshot.HullPercent = sensor.HullPercent;
            snapshot.PropulsionEfficiency = propulsion.CurrentHealth / math.max(propulsion.MaxHealth, 0.001f);
            snapshot.RotationEfficiency = rotation.CurrentHealth / math.max(rotation.MaxHealth, 0.001f);
            EntityManager.SetComponentData(entity, snapshot);
        }

        void RestoreFromSensor(Unity.Entities.Entity entity)
        {
            var snapshot = EntityManager.GetComponentData<ShipSnapshot>(entity);

            if (EntityManager.HasComponent<PhysicsVelocity>(entity))
            {
                var vel = EntityManager.GetComponentData<PhysicsVelocity>(entity);
                vel.Linear = new float3((float)snapshot.Velocity.x, (float)snapshot.Velocity.y, 0f);
                vel.Angular = float3.zero;
                EntityManager.SetComponentData(entity, vel);
            }
        }
    }
}
