using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Starfire.Core.V2.Save.Config;
using Starfire.Core.V2.Save.IO;
using Starfire.Core.V2.Save.Migration;
using Starfire.Core.V2.Save.Serialization;
using Starfire.Core.V2.Save.Tracking;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Simulation;
using StarfireV2;
using UnityEngine;

namespace Starfire.Core.V2.Save
{
    public enum SaveResult
    {
        Success,
        Failed,
        Corrupted,
        VersionMismatch
    }

    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [SerializeField] private SaveSystemConfig config;

        private ISaveSerializer _serializer;
        private SaveFileManager _fileManager;
        private SaveMigrationSystem _migrationSystem;
        private ChunkModificationTracker _chunkTracker;
        private EntityModificationTracker _entityTracker;

        private float _autoSaveTimer;
        private float _playTimeAccumulator;
        private bool _isSaving;
        private bool _isLoading;

        public ChunkModificationTracker ChunkTracker => _chunkTracker;
        public EntityModificationTracker EntityTracker => _entityTracker;
        public bool IsBusy => _isSaving || _isLoading;

        public event Action OnSaveStarted;
        public event Action<SaveResult> OnSaveCompleted;
        public event Action OnLoadStarted;
        public event Action<SaveResult> OnLoadCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Initialize()
        {
            _serializer = CreateSerializer();
            _fileManager = new SaveFileManager(config);
            _migrationSystem = new SaveMigrationSystem();
            _chunkTracker = new ChunkModificationTracker();
            _entityTracker = new EntityModificationTracker();

            // Register migrators here as versions evolve
            // _migrationSystem.RegisterMigrator(new SaveMigrator_V1_to_V2());
        }

        private ISaveSerializer CreateSerializer()
        {
            return config.format switch
            {
                SaveFormat.Binary => new BinarySaveSerializer(),
                SaveFormat.Json => new JsonSaveSerializer(config.jsonPrettyPrint),
                _ => new BinarySaveSerializer()
            };
        }

        private void Update()
        {
            _playTimeAccumulator += Time.unscaledDeltaTime;

            if (config.autoSaveEnabled && !IsBusy)
            {
                _autoSaveTimer += Time.unscaledDeltaTime;
                if (_autoSaveTimer >= config.autoSaveIntervalSeconds)
                {
                    _autoSaveTimer = 0f;
                    _ = AutoSaveAsync();
                }
            }
        }

        // ── Public API ───────────────────────────────────────────────────

        public async Task<SaveResult> SaveToSlotAsync(string slotName, string saveName = null)
        {
            if (IsBusy) return SaveResult.Failed;
            _isSaving = true;
            OnSaveStarted?.Invoke();

            try
            {
                var data = CollectSaveData(saveName ?? slotName);
                var bytes = await Task.Run(() => _serializer.Serialize(data));
                await _fileManager.WriteSlotAsync(slotName, bytes);

                Debug.Log($"[SaveSystem] Saved to slot '{slotName}' ({bytes.Length} bytes, {data.Entities.Count} entities, {data.ModifiedChunks.Count} chunk mods)");
                OnSaveCompleted?.Invoke(SaveResult.Success);
                return SaveResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Save failed: {ex.Message}");
                OnSaveCompleted?.Invoke(SaveResult.Failed);
                return SaveResult.Failed;
            }
            finally
            {
                _isSaving = false;
            }
        }

        public async Task<SaveResult> QuickSaveAsync()
        {
            if (IsBusy) return SaveResult.Failed;
            _isSaving = true;
            OnSaveStarted?.Invoke();

            try
            {
                var data = CollectSaveData("Quick Save");
                var bytes = await Task.Run(() => _serializer.Serialize(data));
                await _fileManager.WriteQuickSaveAsync(bytes);

                Debug.Log($"[SaveSystem] Quick saved ({bytes.Length} bytes)");
                OnSaveCompleted?.Invoke(SaveResult.Success);
                return SaveResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Quick save failed: {ex.Message}");
                OnSaveCompleted?.Invoke(SaveResult.Failed);
                return SaveResult.Failed;
            }
            finally
            {
                _isSaving = false;
            }
        }

        public async Task<SaveResult> LoadFromSlotAsync(string slotName)
        {
            if (IsBusy) return SaveResult.Failed;
            _isLoading = true;
            OnLoadStarted?.Invoke();

            try
            {
                var bytes = await _fileManager.ReadSlotAsync(slotName);
                return await LoadFromBytesAsync(bytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Load failed: {ex.Message}");
                OnLoadCompleted?.Invoke(SaveResult.Failed);
                return SaveResult.Failed;
            }
            finally
            {
                _isLoading = false;
            }
        }

        public async Task<SaveResult> LoadQuickSaveAsync()
        {
            if (IsBusy) return SaveResult.Failed;
            _isLoading = true;
            OnLoadStarted?.Invoke();

            try
            {
                var bytes = await _fileManager.ReadQuickSaveAsync();
                return await LoadFromBytesAsync(bytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Quick load failed: {ex.Message}");
                OnLoadCompleted?.Invoke(SaveResult.Failed);
                return SaveResult.Failed;
            }
            finally
            {
                _isLoading = false;
            }
        }

        public List<SaveSlotInfo> GetSaveSlots()
        {
            return _fileManager.GetAllSlots(_serializer);
        }

        public void DeleteSlot(string slotName)
        {
            _fileManager.DeleteSlot(slotName);
        }

        /// <summary>
        /// Collects the current game state and returns it as pretty-printed JSON.
        /// Useful for editor debugging — does not write to disk.
        /// </summary>
        public string CollectSaveDataAsJson(string saveName = "Debug Snapshot")
        {
            var data = CollectSaveData(saveName);
            return JsonUtility.ToJson(data, true);
        }

        // ── Internal ─────────────────────────────────────────────────────

        private async Task AutoSaveAsync()
        {
            if (IsBusy) return;
            _isSaving = true;

            try
            {
                var data = CollectSaveData("Auto Save");
                var bytes = await Task.Run(() => _serializer.Serialize(data));
                await _fileManager.WriteAutoSaveAsync(bytes);
                Debug.Log($"[SaveSystem] Auto-saved ({bytes.Length} bytes)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Auto-save failed: {ex.Message}");
            }
            finally
            {
                _isSaving = false;
            }
        }

        private SaveData CollectSaveData(string saveName)
        {
            var worldGen = WorldGenerationService.Instance;
            var registry = EntityRegistry.Instance;

            var data = new SaveData();

            // Header
            data.Header.TimestampTicks = DateTime.UtcNow.Ticks;
            data.Header.Flags = config.format == SaveFormat.Binary ? SaveFormatFlags.Binary : SaveFormatFlags.None;

            // Metadata
            data.Metadata.SaveName = saveName;
            data.Metadata.PlayTimeSeconds = _playTimeAccumulator;
            data.Metadata.GameVersion = Application.version;

            if (worldGen != null)
            {
                data.Metadata.WorldSeed = worldGen.WorldSeed;
                data.Metadata.ChunkSize = worldGen.ChunkSize;
                data.Metadata.FloatingOriginLimit = worldGen.FloatingOriginLimit;

                // Player state
                data.PlayerState.AbsolutePosition = worldGen.AbsolutePosition;
                data.PlayerState.OriginOffset = worldGen.OriginOffset;
            }

            // Collect player entity state
            if (registry != null)
            {
                CollectPlayerEntity(data, registry);
                CollectModifiedEntities(data, registry);
            }

            // Collect chunk modifications (delta only)
            foreach (var mod in _chunkTracker.GetAllModifications())
            {
                data.ModifiedChunks.Add(mod);
            }

            // Collect background-simulated entities
            CollectSimulatedEntities(data);

            return data;
        }

        private void CollectPlayerEntity(SaveData data, EntityRegistry registry)
        {
            // Find player-controlled entity
            foreach (var entity in registry.AllEntities)
            {
                var driver = entity.DriverStack?.GetActiveDriver();
                if (driver is PlayerEntityControllerDriver)
                {
                    data.PlayerState.EntityId = entity.Entity.Id;

                    if (entity.Rigid2D != null)
                    {
                        data.PlayerState.VelocityX = entity.Rigid2D.linearVelocity.x;
                        data.PlayerState.VelocityY = entity.Rigid2D.linearVelocity.y;
                        data.PlayerState.Rotation = entity.Rigid2D.rotation;
                        data.PlayerState.AngularVelocity = entity.Rigid2D.angularVelocity;
                    }
                    break;
                }
            }
        }

        private void CollectModifiedEntities(SaveData data, EntityRegistry registry)
        {
            foreach (var entity in registry.AllEntities)
            {
                int id = entity.Entity.Id;
                if (!_entityTracker.ShouldSave(id)) continue;

                var entityData = new EntitySaveData
                {
                    EntityId = id,
                    EntityTypeId = (int)entity.Entity.EntityType,
                    Rotation = entity.Rigid2D != null ? entity.Rigid2D.rotation : 0f,
                    VelocityX = entity.Rigid2D != null ? entity.Rigid2D.linearVelocity.x : 0f,
                    VelocityY = entity.Rigid2D != null ? entity.Rigid2D.linearVelocity.y : 0f,
                    AngularVelocity = entity.Rigid2D != null ? entity.Rigid2D.angularVelocity : 0f,
                    ModificationFlags = _entityTracker.GetFlags(id),
                    Modules = new List<ModuleSaveData>()
                };

                // Compute absolute position
                if (WorldGenerationService.Instance != null)
                {
                    var worldPos = Vector2D.FromVector2(entity.Transform.position);
                    entityData.AbsolutePosition = worldPos + WorldGenerationService.Instance.OriginOffset;
                }

                // Serialize modules if entity is a ship
                if (entity is ShipController ship)
                {
                    CollectModules(entityData, ship);
                }

                data.Entities.Add(entityData);
            }
        }

        private void CollectModules(EntitySaveData entityData, ShipController ship)
        {
            foreach (var slot in ship.Ship.Modules.GetAllSlots())
            {
                if (!slot.HasModule) continue;

                var runtimeData = slot.GetRuntimeData();
                if (runtimeData == null) continue;

                entityData.Modules.Add(new ModuleSaveData
                {
                    SlotId = slot.SlotId,
                    TypeId = (int)runtimeData.TypeId,
                    ModuleId = runtimeData.ModuleId,
                    SerializedData = JsonUtility.ToJson(runtimeData)
                });
            }
        }

        private void CollectSimulatedEntities(SaveData data)
        {
            var simManager = BackgroundSimulationManager.Instance;
            if (simManager == null) return;

            foreach (var simEntity in simManager.GetAllSimulatedEntities())
            {
                if (!simEntity.HasBeenModified) continue;

                data.Entities.Add(new EntitySaveData
                {
                    EntityId = simEntity.EntityId,
                    EntityTypeId = (int)simEntity.EntityType,
                    AbsolutePosition = simEntity.AbsolutePosition,
                    Rotation = simEntity.Rotation,
                    VelocityX = (float)simEntity.Velocity.X,
                    VelocityY = (float)simEntity.Velocity.Y,
                    AngularVelocity = simEntity.AngularVelocity,
                    IsProcedural = true,
                    ModificationFlags = EntityModificationFlags.PositionChanged,
                    Modules = new List<ModuleSaveData>(),
                    IsSimulated = true,
                    LastSimulationTime = simEntity.LastSimulationTime,
                    Mass = simEntity.Mass,
                    Radius = simEntity.Radius,
                    Drag = simEntity.Drag,
                    Variant = simEntity.Variant,
                    Seed = simEntity.Seed,
                    SourceType = simEntity.SourceType
                });
            }
        }

        private async Task<SaveResult> LoadFromBytesAsync(byte[] bytes)
        {
            // Deserialize header first
            SaveHeader header;
            try
            {
                header = await Task.Run(() => _serializer.DeserializeHeader(bytes));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Corrupted save file: {ex.Message}");
                OnLoadCompleted?.Invoke(SaveResult.Corrupted);
                return SaveResult.Corrupted;
            }

            // Check version and migrate if needed
            if (header.Version > SaveHeader.CurrentVersion)
            {
                Debug.LogError($"[SaveSystem] Save version {header.Version} is newer than current {SaveHeader.CurrentVersion}");
                OnLoadCompleted?.Invoke(SaveResult.VersionMismatch);
                return SaveResult.VersionMismatch;
            }

            // Deserialize
            SaveData data;
            try
            {
                data = await Task.Run(() => _serializer.Deserialize(bytes));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to deserialize save: {ex.Message}");
                OnLoadCompleted?.Invoke(SaveResult.Corrupted);
                return SaveResult.Corrupted;
            }

            // Migrate if needed
            if (data.Header.Version < SaveHeader.CurrentVersion)
            {
                if (!_migrationSystem.CanMigrate(data.Header.Version))
                {
                    Debug.LogError($"[SaveSystem] Cannot migrate from version {data.Header.Version}");
                    OnLoadCompleted?.Invoke(SaveResult.VersionMismatch);
                    return SaveResult.VersionMismatch;
                }

                data = _migrationSystem.MigrateToLatest(data);
                if (data == null)
                {
                    OnLoadCompleted?.Invoke(SaveResult.Failed);
                    return SaveResult.Failed;
                }
            }

            // Restore world state
            await RestoreWorldStateAsync(data);

            _playTimeAccumulator = data.Metadata.PlayTimeSeconds;
            Debug.Log($"[SaveSystem] Loaded save '{data.Metadata.SaveName}' ({data.Entities.Count} entities, {data.ModifiedChunks.Count} chunk mods)");
            OnLoadCompleted?.Invoke(SaveResult.Success);
            return SaveResult.Success;
        }

        private async Task RestoreWorldStateAsync(SaveData data)
        {
            var worldGen = WorldGenerationService.Instance;
            if (worldGen == null)
            {
                Debug.LogError("[SaveSystem] WorldGenerationService not found, cannot restore world state");
                return;
            }

            // Clear existing trackers
            _chunkTracker.Clear();
            _entityTracker.Clear();

            // Clear background simulation
            var simManager = BackgroundSimulationManager.Instance;
            simManager?.Clear();

            // Restore chunk modifications into tracker
            foreach (var mod in data.ModifiedChunks)
            {
                _chunkTracker.RestoreModification(mod);
            }

            // Restore entities in batches over multiple frames
            int perFrame = config.entitiesPerFrame;
            for (int i = 0; i < data.Entities.Count; i += perFrame)
            {
                int count = Mathf.Min(perFrame, data.Entities.Count - i);
                for (int j = 0; j < count; j++)
                {
                    RestoreEntity(data.Entities[i + j]);
                }
                await Task.Yield();
            }
        }

        private void RestoreEntity(EntitySaveData entityData)
        {
            // Register in tracker
            bool isProcedural = entityData.IsProcedural;
            _entityTracker.Register(entityData.EntityId, isProcedural);

            if (entityData.ModificationFlags != EntityModificationFlags.None)
            {
                _entityTracker.MarkModified(entityData.EntityId, entityData.ModificationFlags);
            }

            // Restore simulated entities into the background simulation manager
            if (entityData.IsSimulated)
            {
                var simManager = BackgroundSimulationManager.Instance;
                var worldGen = WorldGenerationService.Instance;
                if (simManager != null && worldGen != null)
                {
                    var chunkSize = worldGen.ChunkSize;
                    var absPos = entityData.AbsolutePosition;
                    var currentChunk = ChunkCoord.FromAbsolutePosition(absPos, chunkSize);
                    var playerChunk = ChunkCoord.FromAbsolutePosition(worldGen.AbsolutePosition, chunkSize);

                    var simEntity = new SimulatedEntity
                    {
                        EntityId = entityData.EntityId,
                        EntityType = (EntityType)entityData.EntityTypeId,
                        CurrentChunk = currentChunk,
                        OriginChunk = currentChunk,
                        AbsolutePosition = absPos,
                        Velocity = new Vector2D(entityData.VelocityX, entityData.VelocityY),
                        Rotation = entityData.Rotation,
                        AngularVelocity = entityData.AngularVelocity,
                        Mass = entityData.Mass,
                        Radius = entityData.Radius,
                        Drag = entityData.Drag,
                        HasBeenModified = true,
                        LastSimulationTime = entityData.LastSimulationTime,
                        Variant = entityData.Variant,
                        Seed = entityData.Seed,
                        SourceType = entityData.SourceType
                    };

                    simManager.RestoreEntity(simEntity, playerChunk);
                }
                return;
            }

            // Actual entity spawning/restoration would be handled by the entity system.
            // This method provides the hook point — specific entity restoration logic
            // depends on how entities are spawned (prefab instantiation, etc.)
            //
            // For ships with ISaveable:
            //   var controller = FindOrSpawnEntity(entityData);
            //   if (controller is ISaveable saveable)
            //       saveable.FromSaveData(entityData);
        }
    }
}
