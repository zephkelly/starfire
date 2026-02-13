using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using Unity.Scenes;

namespace Starfire.Network
{
    [UnityEngine.Scripting.Preserve]
    public class StarfireBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            UnityEngine.Application.runInBackground = true;
            AutoConnectPort = 0;
            World.DefaultGameObjectInjectionWorld = new World(defaultWorldName);
            return true;
        }

        public static (World server, World client) CreateHostWorlds()
        {
            AutoConnectPort = 0;
            var server = CreateServerWorld("ServerWorld");
            var client = CreateClientWorld("ClientWorld");
            World.DefaultGameObjectInjectionWorld = client;
            LoadSubScenes(server, client);
            UnityEngine.Debug.Log("[Bootstrap] Created server + client worlds");
            return (server, client);
        }

        public static World CreateDedicatedServer()
        {
            AutoConnectPort = 0;
            var server = CreateServerWorld("ServerWorld");
            World.DefaultGameObjectInjectionWorld = server;
            LoadSubScenes(server);
            UnityEngine.Debug.Log("[Bootstrap] Created server world");
            return server;
        }

        public static World CreateClientOnly()
        {
            AutoConnectPort = 0;
            var client = CreateClientWorld("ClientWorld");
            World.DefaultGameObjectInjectionWorld = client;
            LoadSubScenes(client);
            UnityEngine.Debug.Log("[Bootstrap] Created client world");
            return client;
        }

        public static void DestroyAllWorlds()
        {
            var worlds = new List<World>();
            foreach (var w in World.All)
                worlds.Add(w);
            foreach (var w in worlds)
                if (w.IsCreated) w.Dispose();
            World.DefaultGameObjectInjectionWorld = null;
            UnityEngine.Debug.Log("[Bootstrap] Destroyed all worlds");
        }

        static void LoadSubScenes(params World[] worlds)
        {
            var subScenes = UnityEngine.Object.FindObjectsByType<SubScene>(
                UnityEngine.FindObjectsSortMode.None);
            foreach (var subScene in subScenes)
            {
                if (!subScene.AutoLoadScene) continue;
                foreach (var world in worlds)
                    SceneSystem.LoadSceneAsync(world.Unmanaged, subScene.SceneGUID);
            }
        }
    }
}
