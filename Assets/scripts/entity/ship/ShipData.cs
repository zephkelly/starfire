using Starfire.Core;
using Starfire.Faction;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;

namespace Starfire.Entity
{
    public struct ShipData : System.IDisposable
    {
        public int count;
        public int capacity;

        public NativeArray<EntityId> Ids;
        public NativeArray<int> ConfigIds;
        public NativeArray<FactionId> FactionIds;
        public NativeArray<EntityPersistence> Persistence;

        public NativeArray<EntityTransform> Transforms;
        public NativeArray<EntityPhysics> Physics;

        public NativeArray<ShipHullData> HullModules;
        public NativeArray<ShipRotationData> RotationModules;
        public NativeArray<ShipPropulsionData> PropulsionModules;

        public ShipData(int capacity, Allocator allocator)
        {
            this.capacity = capacity;
            count = 0;

            Ids = new NativeArray<EntityId>(capacity, allocator);
            ConfigIds = new NativeArray<int>(capacity, allocator);
            FactionIds = new NativeArray<FactionId>(capacity, allocator);
            Persistence = new NativeArray<EntityPersistence>(capacity, allocator);

            Transforms = new NativeArray<EntityTransform>(capacity, allocator);
            Physics = new NativeArray<EntityPhysics>(capacity, allocator);

            HullModules = new NativeArray<ShipHullData>(capacity, allocator);
            RotationModules = new NativeArray<ShipRotationData>(capacity, allocator);
            PropulsionModules = new NativeArray<ShipPropulsionData>(capacity, allocator);
        }

        public void Dispose()
        {
            if (Ids.IsCreated) Ids.Dispose();
            if (ConfigIds.IsCreated) ConfigIds.Dispose();
            if (FactionIds.IsCreated) FactionIds.Dispose();
            if (Persistence.IsCreated) Persistence.Dispose();

            if (Transforms.IsCreated) Transforms.Dispose();
            if (Physics.IsCreated) Physics.Dispose();

            if (HullModules.IsCreated) HullModules.Dispose();
            if (RotationModules.IsCreated) RotationModules.Dispose();
            if (PropulsionModules.IsCreated) PropulsionModules.Dispose();
        }
    }

    // public class Ship : IRichEntity
    // {
    //     public EntityId Id { get; set; }
    //     public int ConfigId { get; }
    //     public FactionId FactionId { get; set; }
    //     public EntityPersistence Persistence { get; set; }

    //     public AbsolutePosition Position { get; set; }
    //     public Velocity Velocity { get; set; }
    //     public float Rotation { get; set; }
    //     public float AngularVelocity { get; set; }

    //     public float Mass { get; set; }
    //     public float Radius { get; set; }
    //     public float Drag { get; set; }
    //     public float Restitution { get; set; }

    //     // Modules
    //     public ShipHullModule HullModule { get; set; }
    //     public ShipShieldModule ShieldModule { get; set; }
    //     public ShipPropulsionModule PropulsionModule { get; set; }
    //     public ShipRotationModule RotationModule { get; set; }

    //     public List<IShipAbility> Abilities { get; set; } = new List<IShipAbility>();

    //     public IBehaviourController BehaviourController { get; set; }

    //     public void UpdateModules(float deltaTime)
    //     {
    //         PropulsionModule?.Update(deltaTime);
    //         RotationModule?.Update(deltaTime);

    //         foreach (var ability in Abilities)
    //         {
    //             ability.Update(deltaTime);
    //         }
    //     }

    //     public void UpdatePhysics(float deltaTime)
    //     {
    //         // Simple physics integration
    //         Velocity.X += (Velocity.X * -Drag / Mass) * deltaTime;
    //         Velocity.Y += (Velocity.Y * -Drag / Mass) * deltaTime;

    //         Position.X += Velocity.X * deltaTime;
    //         Position.Y += Velocity.Y * deltaTime;

    //         AngularVelocity += (AngularVelocity * -Drag / Mass) * deltaTime;
    //         Rotation += AngularVelocity * deltaTime;
    //     }

    //     public void CreateStateSnapshot()
    //     {
    //     }

    //     public void RestoreStateSnapshot()
    //     {
    //     }
    // }
}