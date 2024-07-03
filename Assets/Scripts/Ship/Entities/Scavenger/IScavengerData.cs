using UnityEngine;

namespace Starfire
{
    public interface IScavengerData
    {
        StateMachine ScavengerStateMachine { get; }
        Transform ScavengerTransform { get; }
        Rigidbody2D ScavengerRigidbody { get; }
        GameObject ScavengerObject { get; }
        Transform TargetTransform { get; }
        Rigidbody2D TargetRigidbody { get; }
    }
}