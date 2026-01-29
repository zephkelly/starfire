using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarfireV2
{
    public class EntityRegistry : MonoBehaviour
    {
        public static EntityRegistry Instance { get; private set; }

        public event Action<IEntityController> OnEntityRegistered;
        public event Action<IEntityController> OnEntityUnregistered;
        public event Action<IEntityController, V2TransponderData> OnTransponderDataChanged;

        private readonly HashSet<IEntityController> _allEntities = new();
        private readonly Dictionary<V2FactionData, HashSet<IEntityController>> _entitiesByFaction = new();
        private readonly Dictionary<string, IEntityController> _entitiesById = new();
        public IReadOnlyCollection<IEntityController> AllEntities => _allEntities;
        public int EntityCount => _allEntities.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Register(IEntityController entity)
        {
            if (entity == null || _allEntities.Contains(entity))
                return;

            _allEntities.Add(entity);

            // var transponder = GetTransponder(entity);
            // if (transponder != null)
            // {
            //     _entitiesById[transponder.ShipId] = entity;

            //     if (transponder.Faction != null)
            //     {
            //         if (!_entitiesByFaction.TryGetValue(transponder.Faction, out var factionSet))
            //         {
            //             factionSet = new HashSet<IEntityController>();
            //             _entitiesByFaction[transponder.Faction] = factionSet;
            //         }
            //         factionSet.Add(entity);
            //     }
            // }

            OnEntityRegistered?.Invoke(entity);
        }

        public void Unregister(IEntityController entity)
        {
            if (entity == null || !_allEntities.Contains(entity))
                return;

            _allEntities.Remove(entity);

            // var transponder = GetTransponder(entity);
            // if (transponder != null)
            // {
            //     _entitiesById.Remove(transponder.ShipId);

            //     if (transponder.Faction != null &&
            //         _entitiesByFaction.TryGetValue(transponder.Faction, out var factionSet))
            //     {
            //         factionSet.Remove(entity);
            //         if (factionSet.Count == 0)
            //         {
            //             _entitiesByFaction.Remove(transponder.Faction);
            //         }
            //     }
            // }

            OnEntityUnregistered?.Invoke(entity);
        }

        public IEntityController GetById(string shipId)
        {
            if (string.IsNullOrEmpty(shipId))
                return null;

            _entitiesById.TryGetValue(shipId, out var entity);
            return entity;
        }

        public IEnumerable<IEntityController> GetInRange(Vector2 position, float range)
        {
            float rangeSqr = range * range;

            foreach (var entity in _allEntities)
            {
                if (entity == null) continue;

                Vector2 entityPos = entity.Transform.position;
                float distSqr = (entityPos - position).sqrMagnitude;

                if (distSqr <= rangeSqr)
                {
                    yield return entity;
                }
            }
        }

        /// <summary>
        /// Gets all registered entities.
        /// </summary>
        public IEnumerable<IEntityController> GetAllEntities()
        {
            return _allEntities.Where(e => e != null);
        }

        /// <summary>
        /// Gets all entities in range that have an active transponder.
        /// </summary>
        public IEnumerable<IEntityController> GetInRangeWithTransponder(Vector2 position, float range)
        {
            float rangeSqr = range * range;

            foreach (var entity in _allEntities)
            {
                if (entity == null) continue;

                var transponder = GetTransponder(entity);
                if (transponder == null || !transponder.IsTransmitting) continue;

                Vector2 entityPos = entity.Transform.position;
                float distSqr = (entityPos - position).sqrMagnitude;

                if (distSqr <= rangeSqr)
                {
                    yield return entity;
                }
            }
        }

        /// <summary>
        /// Gets entities by v2 faction.
        /// </summary>
        public IEnumerable<IEntityController> GetByFaction(V2FactionData faction)
        {
            if (faction == null)
                return Enumerable.Empty<IEntityController>();

            if (_entitiesByFaction.TryGetValue(faction, out var factionSet))
                return factionSet;

            return Enumerable.Empty<IEntityController>();
        }

        /// <summary>
        /// Notifies the registry that an entity's transponder data has changed.
        /// </summary>
        public void NotifyTransponderDataChanged(IEntityController entity, V2TransponderData data)
        {
            OnTransponderDataChanged?.Invoke(entity, data);
        }

        private static ITransponderModule GetTransponder(IEntityController entity)
        {
            if (entity is ShipController shipController)
            {
                return shipController.Ship?.Modules
                    ?.GetAllModulesOfType<ITransponderModule>()
                    ?.FirstOrDefault();
            }
            return null;
        }
    }
}
