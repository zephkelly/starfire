using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    public class SnapshotTierHandler : ITierHandler
    {
        public void OnTierChanged(Unity.Entities.Entity entity, SimulationTier from, SimulationTier to,
            EntityManager em, EntityCommandBuffer ecb)
        {
            if (from <= SimulationTier.Active && to == SimulationTier.Sensor)
                SnapshotToSensor(entity, em);
            else if (from == SimulationTier.Sensor && to <= SimulationTier.Active)
                RestoreFromSensor(entity, em);
        }

        static void SnapshotToSensor(Unity.Entities.Entity entity, EntityManager em)
        {
            var hull = em.GetComponentData<ShipHull>(entity);
            var propulsion = em.GetComponentData<ShipPropulsion>(entity);
            var rotation = em.GetComponentData<ShipRotation>(entity);
            var vel = em.GetComponentData<PhysicsVelocity>(entity);
            var worldPos = em.GetComponentData<WorldPosition>(entity);

            var sensor = em.GetComponentData<SensorContact>(entity);
            sensor.HullPercent = hull.CurrentHealth / math.max(hull.MaxHealth, 0.001f);
            sensor.MaxSpeed = propulsion.MaxSpeed;
            sensor.Speed = math.length(vel.Linear.xy);
            sensor.Heading = rotation.CurrentHeading;
            sensor.CurrentAIState = 0;
            sensor.StateTimer = 0f;
            em.SetComponentData(entity, sensor);

            var snapshot = em.GetComponentData<ShipSnapshot>(entity);
            snapshot.Position = worldPos.Value;
            snapshot.Velocity = new double2(vel.Linear.x, vel.Linear.y);
            snapshot.Heading = rotation.CurrentHeading;
            snapshot.HullPercent = sensor.HullPercent;
            snapshot.PropulsionEfficiency = propulsion.CurrentHealth / math.max(propulsion.MaxHealth, 0.001f);
            snapshot.RotationEfficiency = rotation.CurrentHealth / math.max(rotation.MaxHealth, 0.001f);
            em.SetComponentData(entity, snapshot);
        }

        static void RestoreFromSensor(Unity.Entities.Entity entity, EntityManager em)
        {
            var snapshot = em.GetComponentData<ShipSnapshot>(entity);
            var vel = em.GetComponentData<PhysicsVelocity>(entity);
            vel.Linear = new float3((float)snapshot.Velocity.x, (float)snapshot.Velocity.y, 0f);
            vel.Angular = float3.zero;
            em.SetComponentData(entity, vel);
        }
    }
}
