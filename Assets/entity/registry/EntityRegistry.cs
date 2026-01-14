using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Starfire.Entity.Modules.Transponder;

namespace Starfire.Entity
{
    public class EntityRegistry : MonoBehaviour
    {
        public static EntityRegistry Instance { get; private set; }

        public event Action<EntityControllerBase> OnEntityRegistered;
        public event Action<EntityControllerBase> OnEntityUnregistered;
        public event Action<EntityControllerBase, TransponderData> OnTransponderDataChanged;

        private readonly HashSet<EntityControllerBase> _allEntities = new();
        private readonly Dictionary<FactionData, HashSet<EntityControllerBase>> _entitiesByFaction = new();
        private readonly Dictionary<string, EntityControllerBase> _entitiesById = new();

        public IReadOnlyCollection<EntityControllerBase> AllEntities => _allEntities;
        public int EntityCount => _allEntities.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Register(EntityControllerBase entity)
        {
            if (entity == null || _allEntities.Contains(entity))
                return;

            _allEntities.Add(entity);

            var transponder = GetTransponder(entity);
            if (transponder != null)
            {
                _entitiesById[transponder.ShipId] = entity;

                if (transponder.Faction != null)
                {
                    if (!_entitiesByFaction.TryGetValue(transponder.Faction, out var factionSet))
                    {
                        factionSet = new HashSet<EntityControllerBase>();
                        _entitiesByFaction[transponder.Faction] = factionSet;
                    }
                    factionSet.Add(entity);
                }
            }

            OnEntityRegistered?.Invoke(entity);
        }

        public void Unregister(EntityControllerBase entity)
        {
            if (entity == null || !_allEntities.Contains(entity))
                return;

            _allEntities.Remove(entity);

            var transponder = GetTransponder(entity);
            if (transponder != null)
            {
                _entitiesById.Remove(transponder.ShipId);

                if (transponder.Faction != null &&
                    _entitiesByFaction.TryGetValue(transponder.Faction, out var factionSet))
                {
                    factionSet.Remove(entity);
                    if (factionSet.Count == 0)
                    {
                        _entitiesByFaction.Remove(transponder.Faction);
                    }
                }
            }

            OnEntityUnregistered?.Invoke(entity);
        }

        public EntityControllerBase GetById(string shipId)
        {
            if (string.IsNullOrEmpty(shipId))
                return null;

            _entitiesById.TryGetValue(shipId, out var entity);
            return entity;
        }

        public IEnumerable<EntityControllerBase> GetByFaction(FactionData faction)
        {
            if (faction == null)
                return Enumerable.Empty<EntityControllerBase>();

            if (_entitiesByFaction.TryGetValue(faction, out var factionSet))
                return factionSet;

            return Enumerable.Empty<EntityControllerBase>();
        }

        public IEnumerable<EntityControllerBase> GetInRange(Vector2 position, float range)
        {
            float rangeSqr = range * range;

            foreach (var entity in _allEntities)
            {
                if (entity == null) continue;

                Vector2 entityPos = entity.transform.position;
                float distSqr = (entityPos - position).sqrMagnitude;

                if (distSqr <= rangeSqr)
                {
                    yield return entity;
                }
            }
        }

        public IEnumerable<EntityControllerBase> GetInRangeWithTransponder(Vector2 position, float range)
        {
            float rangeSqr = range * range;

            foreach (var entity in _allEntities)
            {
                if (entity == null) continue;

                var transponder = GetTransponder(entity);
                if (transponder == null || !transponder.IsTransmitting) continue;

                Vector2 entityPos = entity.transform.position;
                float distSqr = (entityPos - position).sqrMagnitude;

                if (distSqr <= rangeSqr)
                {
                    yield return entity;
                }
            }
        }

        public void NotifyTransponderDataChanged(EntityControllerBase entity, TransponderData data)
        {
            OnTransponderDataChanged?.Invoke(entity, data);
        }

        private static ITransponderModule GetTransponder(EntityControllerBase entity)
        {
            return entity.Systems?.GetAllModulesOfType<ITransponderModule>().FirstOrDefault();
        }
    }
}
