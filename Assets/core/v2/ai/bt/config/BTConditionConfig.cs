using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Base config for condition decorators.
    /// Subclasses define specific conditions.
    /// </summary>
    public abstract class BTConditionConfig : BTNodeConfig
    {
        [SerializeField] protected BTNodeConfig child;
    }
}
