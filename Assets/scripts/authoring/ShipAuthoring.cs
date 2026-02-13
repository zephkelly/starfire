using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Starfire.Core;
using Starfire.Entity;
using Starfire.Network;
using Starfire.Simulation;

namespace Starfire.Authoring
{
    public class ShipAuthoring : MonoBehaviour
    {
        [Header("Hull")]
        public float maxHealth = 100f;
        public byte hullType;

        [Header("Propulsion")]
        public float maxSpeed = 200f;
        public float acceleration = 60f;
        public float dragCoefficient = 0.4f;

        [Header("Rotation")]
        public float turnRate = 180f;

        [Header("Sensor")]
        public float sensorRange = 1000f;
        public float weaponRange = 500f;

        class ShipBaker : Baker<ShipAuthoring>
        {
            public override void Bake(ShipAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new WorldPosition());
                AddComponent(entity, new EntityIdentity
                {
                    EntityType = (byte)Starfire.Entity.EntityType.Ship
                });
                AddComponent(entity, new SimulationTierData
                {
                    Tier = SimulationTier.Loaded
                });
                AddComponent(entity, new TierTransition());
                SetComponentEnabled<TierTransition>(entity, false);

                AddComponent(entity, new ControlInput());
                AddComponent(entity, new PlayerInput());
                AddComponent<ShipTag>(entity);
                AddComponent(entity, new PlayerName());
                AddComponent(entity, new PlayerColor());

                AddComponent(entity, new ShipHull
                {
                    CurrentHealth = authoring.maxHealth,
                    MaxHealth = authoring.maxHealth,
                    MaxTemperature = 500f,
                    EfficiencyCoefficient = 1f,
                    HullType = authoring.hullType,
                    IsEnabled = 1
                });

                AddComponent(entity, new ShipPropulsion
                {
                    CurrentHealth = authoring.maxHealth,
                    MaxHealth = authoring.maxHealth,
                    MaxSpeed = authoring.maxSpeed,
                    Acceleration = authoring.acceleration,
                    DragCoefficient = authoring.dragCoefficient,
                    IsEnabled = 1
                });

                AddComponent(entity, new ShipRotation
                {
                    CurrentHealth = authoring.maxHealth,
                    MaxHealth = authoring.maxHealth,
                    TurnRate = authoring.turnRate,
                    IsEnabled = 1
                });

                AddComponent(entity, new ShipSnapshot());

                AddComponent(entity, new SensorContact
                {
                    HullPercent = 1f,
                    ShieldPercent = 1f,
                    SensorRange = authoring.sensorRange,
                    WeaponRange = authoring.weaponRange,
                    MaxSpeed = authoring.maxSpeed
                });

                AddComponent(entity, new RichTierTag());
                SetComponentEnabled<RichTierTag>(entity, true);
                AddComponent(entity, new VisualTierTag());
                SetComponentEnabled<VisualTierTag>(entity, true);
                AddComponent(entity, new SensorTierTag());
                SetComponentEnabled<SensorTierTag>(entity, false);
            }
        }
    }
}
