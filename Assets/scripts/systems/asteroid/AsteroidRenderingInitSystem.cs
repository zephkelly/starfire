using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Starfire.Entity;
using Starfire.Simulation;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Starfire.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class AsteroidRenderingInitSystem : SystemBase
    {
        const int MaxPerFrame = 16;

        AsteroidTypeDefinition[] _typeDefinitions;
        BlobAssetReference<Unity.Physics.Collider>[] _colliders;
        EntityQuery _uninitializedQuery;
        bool _initialized;

        protected override void OnCreate()
        {
            _uninitializedQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<AsteroidTag>(),
                    ComponentType.ReadOnly<AsteroidData>(),
                    ComponentType.ReadOnly<RichTierTag>(),
                    ComponentType.ReadOnly<LocalTransform>(),
                    ComponentType.ReadOnly<LocalToWorld>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<MaterialMeshInfo>()
                }
            });

            RequireForUpdate(_uninitializedQuery);
        }

        public void SetTypeDefinitions(AsteroidTypeDefinition[] definitions)
        {
            _typeDefinitions = definitions;
            CreateColliders();
            _initialized = true;
        }

        void CreateColliders()
        {
            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i].IsCreated)
                        _colliders[i].Dispose();
                }
            }

            _colliders = new BlobAssetReference<Unity.Physics.Collider>[_typeDefinitions.Length];
            for (int i = 0; i < _typeDefinitions.Length; i++)
            {
                float radius = _typeDefinitions[i].ColliderRadius;
                _colliders[i] = SphereCollider.Create(
                    new SphereGeometry { Center = Unity.Mathematics.float3.zero, Radius = radius },
                    CollisionFilter.Default);
            }
        }

        protected override void OnUpdate()
        {
            if (!_initialized || _typeDefinitions == null || _typeDefinitions.Length == 0)
                return;

            Dependency.Complete();

            var entities = _uninitializedQuery.ToEntityArray(Allocator.Temp);
            int count = Unity.Mathematics.math.min(entities.Length, MaxPerFrame);

            for (int i = 0; i < count; i++)
            {
                var entity = entities[i];

                if (!EntityManager.Exists(entity))
                    continue;

                var asteroidData = EntityManager.GetComponentData<AsteroidData>(entity);
                int typeIndex = asteroidData.TypeId;
                if (typeIndex < 0 || typeIndex >= _typeDefinitions.Length)
                    typeIndex = 0;

                var def = _typeDefinitions[typeIndex];
                if (def.Mesh == null || def.Material == null)
                    continue;

                var desc = new RenderMeshDescription(ShadowCastingMode.Off);
                var meshArray = new RenderMeshArray(
                    new UnityEngine.Material[] { def.Material },
                    new Mesh[] { def.Mesh });

                RenderMeshUtility.AddComponents(entity, EntityManager, desc, meshArray,
                    MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

                if (typeIndex < _colliders.Length && _colliders[typeIndex].IsCreated
                    && EntityManager.HasComponent<PhysicsCollider>(entity))
                    EntityManager.SetComponentData(entity, new PhysicsCollider { Value = _colliders[typeIndex] });

                bool isVisual = EntityManager.IsComponentEnabled<VisualTierTag>(entity);
                if (!isVisual && !EntityManager.HasComponent<DisableRendering>(entity))
                    EntityManager.AddComponent<DisableRendering>(entity);
            }

            entities.Dispose();
        }

        protected override void OnDestroy()
        {
            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i].IsCreated)
                        _colliders[i].Dispose();
                }
            }
        }
    }
}
