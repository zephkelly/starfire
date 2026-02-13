using Unity.NetCode;

namespace Starfire.Network
{
    public struct ZoneDamageEventRpc : IRpcCommand
    {
        public int SourceNetworkId;
        public int EntityId;
        public float NewHealth;
    }

    public struct ZoneDestroyEventRpc : IRpcCommand
    {
        public int SourceNetworkId;
        public int EntityId;
    }
}
