using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Starfire.Core.V2.Save.Serialization
{
    public class BinarySaveSerializer : ISaveSerializer
    {
        public byte[] Serialize(SaveData data)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            // Header
            w.Write(Encoding.ASCII.GetBytes(SaveHeader.MagicString));
            w.Write(data.Header.Version);
            w.Write((byte)data.Header.Flags);
            w.Write(data.Header.TimestampTicks);
            w.Write((uint)0); // checksum placeholder

            // Metadata
            WriteString(w, data.Metadata.SaveName);
            w.Write(data.Metadata.PlayTimeSeconds);
            w.Write(data.Metadata.WorldSeed);
            w.Write(data.Metadata.ChunkSize);
            w.Write(data.Metadata.FloatingOriginLimit);
            WriteString(w, data.Metadata.GameVersion);

            // Player state
            w.Write(data.PlayerState.EntityId);
            w.Write(data.PlayerState.AbsolutePositionX);
            w.Write(data.PlayerState.AbsolutePositionY);
            w.Write(data.PlayerState.OriginOffsetX);
            w.Write(data.PlayerState.OriginOffsetY);
            w.Write(data.PlayerState.VelocityX);
            w.Write(data.PlayerState.VelocityY);
            w.Write(data.PlayerState.Rotation);
            w.Write(data.PlayerState.AngularVelocity);

            // Modified chunks
            w.Write(data.ModifiedChunks.Count);
            foreach (var chunk in data.ModifiedChunks)
            {
                WriteChunkModification(w, chunk);
            }

            // Entities
            w.Write(data.Entities.Count);
            foreach (var entity in data.Entities)
            {
                WriteEntity(w, entity);
            }

            w.Flush();

            // Write checksum
            var bytes = ms.ToArray();
            uint checksum = ComputeChecksum(bytes, 17); // skip header up to checksum field
            BitConverter.TryWriteBytes(new Span<byte>(bytes, 13, 4), checksum);

            return bytes;
        }

        public SaveData Deserialize(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var r = new BinaryReader(ms, Encoding.UTF8);

            var save = new SaveData();

            // Header
            var magic = Encoding.ASCII.GetString(r.ReadBytes(4));
            if (magic != SaveHeader.MagicString)
                throw new InvalidDataException($"Invalid save file magic: {magic}");

            save.Header.Version = r.ReadInt32();
            save.Header.Flags = (SaveFormatFlags)r.ReadByte();
            save.Header.TimestampTicks = r.ReadInt64();
            save.Header.Checksum = r.ReadUInt32();

            // Validate checksum
            uint expected = ComputeChecksum(data, 17);
            if (save.Header.Checksum != expected)
                throw new InvalidDataException("Save file checksum mismatch — file may be corrupted.");

            // Metadata
            save.Metadata.SaveName = ReadString(r);
            save.Metadata.PlayTimeSeconds = r.ReadSingle();
            save.Metadata.WorldSeed = r.ReadSingle();
            save.Metadata.ChunkSize = r.ReadSingle();
            save.Metadata.FloatingOriginLimit = r.ReadSingle();
            save.Metadata.GameVersion = ReadString(r);

            // Player state
            save.PlayerState.EntityId = r.ReadInt32();
            save.PlayerState.AbsolutePositionX = r.ReadDouble();
            save.PlayerState.AbsolutePositionY = r.ReadDouble();
            save.PlayerState.OriginOffsetX = r.ReadDouble();
            save.PlayerState.OriginOffsetY = r.ReadDouble();
            save.PlayerState.VelocityX = r.ReadSingle();
            save.PlayerState.VelocityY = r.ReadSingle();
            save.PlayerState.Rotation = r.ReadSingle();
            save.PlayerState.AngularVelocity = r.ReadSingle();

            // Modified chunks
            int chunkCount = r.ReadInt32();
            save.ModifiedChunks = new List<ChunkModificationData>(chunkCount);
            for (int i = 0; i < chunkCount; i++)
            {
                save.ModifiedChunks.Add(ReadChunkModification(r));
            }

            // Entities
            int entityCount = r.ReadInt32();
            save.Entities = new List<EntitySaveData>(entityCount);
            for (int i = 0; i < entityCount; i++)
            {
                save.Entities.Add(ReadEntity(r));
            }

            return save;
        }

        public SaveHeader DeserializeHeader(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var r = new BinaryReader(ms, Encoding.UTF8);

            var header = new SaveHeader();
            header.Magic = Encoding.ASCII.GetString(r.ReadBytes(4));
            header.Version = r.ReadInt32();
            header.Flags = (SaveFormatFlags)r.ReadByte();
            header.TimestampTicks = r.ReadInt64();
            header.Checksum = r.ReadUInt32();
            return header;
        }

        // ── Chunk modifications ──────────────────────────────────────────

        private void WriteChunkModification(BinaryWriter w, ChunkModificationData mod)
        {
            w.Write(mod.ChunkX);
            w.Write(mod.ChunkY);
            w.Write((byte)mod.ModificationFlags);

            int asteroidCount = mod.AsteroidModifications?.Count ?? 0;
            w.Write(asteroidCount);
            if (mod.AsteroidModifications != null)
            {
                foreach (var asteroid in mod.AsteroidModifications)
                {
                    w.Write((byte)asteroid.Type);
                    w.Write(asteroid.LocalPositionX);
                    w.Write(asteroid.LocalPositionY);
                    w.Write(asteroid.Size);
                    w.Write(asteroid.Rotation);
                    w.Write(asteroid.Variant);
                    w.Write(asteroid.Seed);
                    w.Write(asteroid.SourceType);
                }
            }
        }

        private ChunkModificationData ReadChunkModification(BinaryReader r)
        {
            var mod = new ChunkModificationData
            {
                ChunkX = r.ReadInt64(),
                ChunkY = r.ReadInt64(),
                ModificationFlags = (Tracking.ChunkModificationFlags)r.ReadByte()
            };

            int asteroidCount = r.ReadInt32();
            mod.AsteroidModifications = new List<AsteroidModification>(asteroidCount);
            for (int i = 0; i < asteroidCount; i++)
            {
                mod.AsteroidModifications.Add(new AsteroidModification
                {
                    Type = (AsteroidModificationType)r.ReadByte(),
                    LocalPositionX = r.ReadSingle(),
                    LocalPositionY = r.ReadSingle(),
                    Size = r.ReadSingle(),
                    Rotation = r.ReadSingle(),
                    Variant = r.ReadInt32(),
                    Seed = r.ReadSingle(),
                    SourceType = r.ReadInt32()
                });
            }

            return mod;
        }

        // ── Entities ─────────────────────────────────────────────────────

        private void WriteEntity(BinaryWriter w, EntitySaveData entity)
        {
            w.Write(entity.EntityId);
            w.Write(entity.EntityTypeId);
            w.Write(entity.AbsolutePositionX);
            w.Write(entity.AbsolutePositionY);
            w.Write(entity.Rotation);
            w.Write(entity.VelocityX);
            w.Write(entity.VelocityY);
            w.Write(entity.AngularVelocity);
            w.Write(entity.IsProcedural);
            w.Write((ushort)entity.ModificationFlags);

            int moduleCount = entity.Modules?.Count ?? 0;
            w.Write(moduleCount);
            if (entity.Modules != null)
            {
                foreach (var mod in entity.Modules)
                {
                    WriteString(w, mod.SlotId);
                    w.Write(mod.TypeId);
                    WriteString(w, mod.ModuleId);
                    WriteString(w, mod.SerializedData);
                }
            }
        }

        private EntitySaveData ReadEntity(BinaryReader r)
        {
            var entity = new EntitySaveData
            {
                EntityId = r.ReadInt32(),
                EntityTypeId = r.ReadInt32(),
                AbsolutePositionX = r.ReadDouble(),
                AbsolutePositionY = r.ReadDouble(),
                Rotation = r.ReadSingle(),
                VelocityX = r.ReadSingle(),
                VelocityY = r.ReadSingle(),
                AngularVelocity = r.ReadSingle(),
                IsProcedural = r.ReadBoolean(),
                ModificationFlags = (Tracking.EntityModificationFlags)r.ReadUInt16()
            };

            int moduleCount = r.ReadInt32();
            entity.Modules = new List<ModuleSaveData>(moduleCount);
            for (int i = 0; i < moduleCount; i++)
            {
                entity.Modules.Add(new ModuleSaveData
                {
                    SlotId = ReadString(r),
                    TypeId = r.ReadInt32(),
                    ModuleId = ReadString(r),
                    SerializedData = ReadString(r)
                });
            }

            return entity;
        }

        // ── Utilities ────────────────────────────────────────────────────

        private static void WriteString(BinaryWriter w, string value)
        {
            value ??= string.Empty;
            w.Write(value);
        }

        private static string ReadString(BinaryReader r)
        {
            return r.ReadString();
        }

        private static uint ComputeChecksum(byte[] data, int startOffset)
        {
            // Simple CRC32 over payload (everything after header checksum field)
            uint crc = 0xFFFFFFFF;
            for (int i = startOffset; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
                }
            }
            return crc ^ 0xFFFFFFFF;
        }
    }
}
