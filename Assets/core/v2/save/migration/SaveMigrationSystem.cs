using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starfire.Core.V2.Save.Migration
{
    public class SaveMigrationSystem
    {
        private readonly List<ISaveMigrator> _migrators = new();

        public void RegisterMigrator(ISaveMigrator migrator)
        {
            _migrators.Add(migrator);
            _migrators.Sort((a, b) => a.FromVersion.CompareTo(b.FromVersion));
        }

        public SaveData MigrateToLatest(SaveData data)
        {
            int target = SaveHeader.CurrentVersion;
            int current = data.Header.Version;

            if (current >= target)
                return data;

            var chain = BuildMigrationChain(current, target);
            if (chain == null)
            {
                Debug.LogError($"[SaveMigration] No migration path from v{current} to v{target}");
                return null;
            }

            foreach (var migrator in chain)
            {
                Debug.Log($"[SaveMigration] Migrating v{migrator.FromVersion} -> v{migrator.ToVersion}");
                data = migrator.Migrate(data);
                data.Header.Version = migrator.ToVersion;
            }

            return data;
        }

        public bool CanMigrate(int fromVersion)
        {
            return BuildMigrationChain(fromVersion, SaveHeader.CurrentVersion) != null;
        }

        private List<ISaveMigrator> BuildMigrationChain(int from, int to)
        {
            var chain = new List<ISaveMigrator>();
            int current = from;

            while (current < to)
            {
                var migrator = _migrators.FirstOrDefault(m => m.FromVersion == current);
                if (migrator == null)
                    return null;

                chain.Add(migrator);
                current = migrator.ToVersion;
            }

            return current == to ? chain : null;
        }
    }
}
