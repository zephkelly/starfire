using Unity.Entities;
using UnityEngine;
using Starfire.Entity;
using Starfire.Systems;

namespace Starfire.Demo
{
    public class GameConfig : MonoBehaviour
    {
        [Header("Ship Rendering")]
        [SerializeField] Mesh shipMesh;
        [SerializeField] Material shipMaterial;

        [Header("Star Rendering")]
        [SerializeField] Mesh starMesh;
        [SerializeField] Material starMaterial;

        [Header("Asteroid Types")]
        [SerializeField] AsteroidTypeDefinition[] asteroidTypes;

        Unity.Entities.World _registeredClientWorld;

        void Start()
        {
            RegisterAll();
        }

        void Update()
        {
            var clientWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (clientWorld == null || !clientWorld.IsCreated || clientWorld == _registeredClientWorld)
                return;

            RegisterAll();
        }

        void RegisterAll()
        {
            RegisterAsteroidTypes();
            RegisterEntityRendering();

            var clientWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (clientWorld != null && clientWorld.IsCreated)
                _registeredClientWorld = clientWorld;
        }

        void RegisterAsteroidTypes()
        {
            if (asteroidTypes == null || asteroidTypes.Length == 0)
            {
                Debug.LogWarning("[GameConfig] No asteroid types defined. Asteroids will not render.");
                return;
            }

            foreach (var world in Unity.Entities.World.All)
            {
                var renderInitSystem = world.GetExistingSystemManaged<AsteroidRenderingInitSystem>();
                if (renderInitSystem != null)
                {
                    renderInitSystem.SetTypeDefinitions(asteroidTypes);
                    Debug.Log($"[GameConfig] Registered {asteroidTypes.Length} asteroid types with {world.Name}");
                }
            }
        }

        void RegisterEntityRendering()
        {
            foreach (var world in Unity.Entities.World.All)
            {
                var ghostRenderSystem = world.GetExistingSystemManaged<GhostRenderingInitSystem>();
                if (ghostRenderSystem == null)
                    continue;

                if (shipMesh != null && shipMaterial != null)
                    ghostRenderSystem.SetShipRendering(shipMesh, shipMaterial);

                if (starMesh != null && starMaterial != null)
                    ghostRenderSystem.SetStarRendering(starMesh, starMaterial);

                Debug.Log($"[GameConfig] Registered ship/star rendering with {world.Name}");
            }
        }

        public Mesh ShipMesh => shipMesh;
        public Material ShipMaterial => shipMaterial;
        public Mesh StarMesh => starMesh;
        public Material StarMaterial => starMaterial;
    }
}
