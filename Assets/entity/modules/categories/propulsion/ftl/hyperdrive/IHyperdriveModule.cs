using UnityEngine;

namespace Starfire.Entity.Modules.Hyperdrive
{
    public interface IHyperdriveModule : IEntityModule
    {
        float HyperdriveRange { get; }
        float ChargeTime { get; }
        bool IsReady { get; }
        void Jump(Vector2 destination);
    }
}
