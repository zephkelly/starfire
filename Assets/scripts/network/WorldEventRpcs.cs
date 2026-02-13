using Unity.NetCode;

namespace Starfire.Network
{
    public struct EntityDestroyedRpc : IRpcCommand
    {
        public int EntityId;
    }

    public struct EntityDamagedRpc : IRpcCommand
    {
        public int EntityId;
        public float NewHealth;
    }
}
