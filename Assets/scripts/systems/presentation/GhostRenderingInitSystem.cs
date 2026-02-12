using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Starfire.Entity;
using Starfire.Simulation;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GhostRenderingInitSystem : SystemBase
    {
        const int MaxPerFrame = 16;

        Mesh _shipMesh;
        Material _shipMaterial;
        Mesh _starMesh;
        Material _starMaterial;
        bool _shipReady;
        bool _starReady;

        EntityQuery _uninitShipQuery;
        EntityQuery _uninitStarQuery;

        protected override void OnCreate()
        {
            _uninitShipQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<ShipTag>(),
                    ComponentType.ReadOnly<LocalTransform>(),
                    ComponentType.ReadOnly<LocalToWorld>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<MaterialMeshInfo>()
                }
            });

            _uninitStarQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<StarTag>(),
                    ComponentType.ReadOnly<LocalTransform>(),
                    ComponentType.ReadOnly<LocalToWorld>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<MaterialMeshInfo>()
                }
            });
        }

        public void SetShipRendering(Mesh mesh, Material material)
        {
            _shipMesh = mesh;
            _shipMaterial = material;
            _shipReady = mesh != null && material != null;
        }

        public void SetStarRendering(Mesh mesh, Material material)
        {
            _starMesh = mesh;
            _starMaterial = material;
            _starReady = mesh != null && material != null;
        }

        protected override void OnUpdate()
        {
            if (!_shipReady && !_starReady)
                return;

            Dependency.Complete();

            int budget = MaxPerFrame;

            if (_shipReady && !_uninitShipQuery.IsEmpty)
                budget -= InitializeEntities(_uninitShipQuery, _shipMesh, _shipMaterial, budget);

            if (_starReady && budget > 0 && !_uninitStarQuery.IsEmpty)
                InitializeEntities(_uninitStarQuery, _starMesh, _starMaterial, budget);
        }

        int InitializeEntities(EntityQuery query, Mesh mesh, Material material, int maxCount)
        {
            var entities = query.ToEntityArray(Allocator.Temp);
            int count = math.min(entities.Length, maxCount);

            for (int i = 0; i < count; i++)
            {
                var entity = entities[i];

                if (!EntityManager.Exists(entity))
                    continue;

                var desc = new RenderMeshDescription(ShadowCastingMode.Off);
                var meshArray = new RenderMeshArray(
                    new Material[] { material },
                    new Mesh[] { mesh });

                RenderMeshUtility.AddComponents(entity, EntityManager, desc, meshArray,
                    MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
            }

            entities.Dispose();
            return count;
        }
    }
}
