using Unity.Entities;
using Unity.NetCode;

namespace Starfire.Network
{
    [UnityEngine.Scripting.Preserve]
    public class StarfireBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            UnityEngine.Application.runInBackground = true;
            AutoConnectPort = 7979;
            CreateDefaultClientServerWorlds();

            foreach (var world in World.All)
            {
                if (world.Name == "ClientWorld")
                {
                    World.DefaultGameObjectInjectionWorld = world;
                    UnityEngine.Debug.Log($"[Bootstrap] Set DefaultGameObjectInjectionWorld = '{world.Name}'");
                    break;
                }
            }

            return true;
        }
    }
}
