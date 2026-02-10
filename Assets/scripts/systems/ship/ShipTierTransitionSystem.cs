using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(TierEvaluationSystem))]
    public partial class ShipTierTransitionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (transition, entity) in
                SystemAPI.Query<RefRO<TierTransition>>()
                    .WithPresent<TierTransition>()
                    .WithAll<ShipHull>()
                    .WithEntityAccess())
            {
                if (!SystemAPI.IsComponentEnabled<TierTransition>(entity))
                    continue;

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
            var vel = EntityManager.GetComponentData<PhysicsVelocity>(entity);
            var worldPos = EntityManager.GetComponentData<WorldPosition>(entity);

            var sensor = EntityManager.GetComponentData<SensorContact>(entity);
            sensor.HullPercent = hull.CurrentHealth / math.max(hull.MaxHealth, 0.001f);
            sensor.MaxSpeed = propulsion.MaxSpeed;
            sensor.Speed = math.length(vel.Linear.xy);
            sensor.Heading = rotation.CurrentHeading;
            sensor.CurrentAIState = 0;
            sensor.StateTimer = 0f;
            EntityManager.SetComponentData(entity, sensor);

            var snapshot = EntityManager.GetComponentData<ShipSnapshot>(entity);
            snapshot.Position = worldPos.Value;
            snapshot.Velocity = new double2(vel.Linear.x, vel.Linear.y);
            snapshot.Heading = rotation.CurrentHeading;
            snapshot.HullPercent = sensor.HullPercent;
            snapshot.PropulsionEfficiency = propulsion.CurrentHealth / math.max(propulsion.MaxHealth, 0.001f);
            snapshot.RotationEfficiency = rotation.CurrentHealth / math.max(rotation.MaxHealth, 0.001f);
            EntityManager.SetComponentData(entity, snapshot);
        }

        void RestoreFromSensor(Unity.Entities.Entity entity)
        {
            var snapshot = EntityManager.GetComponentData<ShipSnapshot>(entity);
            var vel = EntityManager.GetComponentData<PhysicsVelocity>(entity);
            vel.Linear = new float3((float)snapshot.Velocity.x, (float)snapshot.Velocity.y, 0f);
            vel.Angular = float3.zero;
            EntityManager.SetComponentData(entity, vel);
        }
    }
}
