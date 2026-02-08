using Unity.Mathematics;

namespace Starfire.Entity
{
    public struct EntityTransform
    {
        public double2 Position;
        public double2 Velocity;
        public float Rotation;
        public float AngularVelocity;
    }

    public struct EntityPhysics
    {
        public float Mass;
        public float Radius;
        public float Drag;
        public float Restitution;
    }

    
}