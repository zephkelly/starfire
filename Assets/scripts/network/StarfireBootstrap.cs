using Unity.NetCode;

namespace Starfire.Network
{
    [UnityEngine.Scripting.Preserve]
    public class StarfireBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            CreateLocalWorld(defaultWorldName);
            return true;
        }
    }
}
