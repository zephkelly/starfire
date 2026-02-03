using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.Save.Tracking;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;

namespace Starfire.Core.V2.Save
{
    // ── File Header ──────────────────────────────────────────────────────

    [Serializable]
    public class SaveHeader
    {
        public const string MagicString = "STFR";
        public const int CurrentVersion = 1;

        public string Magic = MagicString;
        public int Version = CurrentVersion;
        public SaveFormatFlags Flags;
        public long TimestampTicks;
        public uint Checksum;
    }

    [Flags]
    public enum SaveFormatFlags : byte
    {
        None = 0,
        Compressed = 1 << 0,
        Binary = 1 << 1,
    }

    // ── Metadata ─────────────────────────────────────────────────────────

    [Serializable]
    public class SaveMetadata
    {
        public string SaveName;
        public float PlayTimeSeconds;
        public float WorldSeed;
        public float ChunkSize;
        public float FloatingOriginLimit;
        public string GameVersion;
    }

    // ── Player State ─────────────────────────────────────────────────────

    [Serializable]
    public class PlayerSaveData
    {
        public int EntityId;
        public double AbsolutePositionX;
        public double AbsolutePositionY;
        public double OriginOffsetX;
        public double OriginOffsetY;
        public float VelocityX;
        public float VelocityY;
        public float Rotation;
        public float AngularVelocity;

        public Vector2D AbsolutePosition
        {
            get => new Vector2D(AbsolutePositionX, AbsolutePositionY);
            set { AbsolutePositionX = value.X; AbsolutePositionY = value.Y; }
        }

        public Vector2D OriginOffset
        {
            get => new Vector2D(OriginOffsetX, OriginOffsetY);
            set { OriginOffsetX = value.X; OriginOffsetY = value.Y; }
        }
    }

    // ── Entity State ─────────────────────────────────────────────────────

    [Serializable]
    public class EntitySaveData
    {
        public int EntityId;
        public int EntityTypeId;
        public double AbsolutePositionX;
        public double AbsolutePositionY;
        public float Rotation;
        public float VelocityX;
        public float VelocityY;
        public float AngularVelocity;
        public bool IsProcedural;
        public EntityModificationFlags ModificationFlags;
        public List<ModuleSaveData> Modules;

        // Background simulation fields
        public double LastSimulationTime;
        public float Mass;
        public float Radius;
        public float Drag;
        public int Variant;
        public float Seed;
        public int SourceType;
        public bool IsSimulated;

        public Vector2D AbsolutePosition
        {
            get => new Vector2D(AbsolutePositionX, AbsolutePositionY);
            set { AbsolutePositionX = value.X; AbsolutePositionY = value.Y; }
        }
    }

    [Serializable]
    public class ModuleSaveData
    {
        public string SlotId;
        public int TypeId;
        public string ModuleId;
        public string SerializedData;
    }

    // ── Chunk Modifications ──────────────────────────────────────────────

    [Serializable]
    public class ChunkModificationData
    {
        public long ChunkX;
        public long ChunkY;
        public ChunkModificationFlags ModificationFlags;
        public List<AsteroidModification> AsteroidModifications;
        public List<EntityMigration> EntityMigrations;

        public ChunkCoord ChunkCoord
        {
            get => new ChunkCoord(ChunkX, ChunkY);
            set { ChunkX = value.X; ChunkY = value.Y; }
        }
    }

    [Serializable]
    public class AsteroidModification
    {
        public AsteroidModificationType Type;
        public float LocalPositionX;
        public float LocalPositionY;
        public float Size;
        public float Rotation;
        public int Variant;
        public float Seed;
        public int SourceType;
    }

    public enum AsteroidModificationType : byte
    {
        Removed = 0,
        Added = 1,
        Modified = 2
    }

    [Serializable]
    public class EntityMigration
    {
        public int EntityId;
        public long SourceChunkX;
        public long SourceChunkY;
        public long DestinationChunkX;
        public long DestinationChunkY;
    }

    // ── Root Save Container ──────────────────────────────────────────────

    [Serializable]
    public class SaveData
    {
        public SaveHeader Header = new();
        public SaveMetadata Metadata = new();
        public PlayerSaveData PlayerState = new();
        public List<ChunkModificationData> ModifiedChunks = new();
        public List<EntitySaveData> Entities = new();
    }
}
