namespace Starfire.Core.V2.Save.Migration
{
    public interface ISaveMigrator
    {
        int FromVersion { get; }
        int ToVersion { get; }
        SaveData Migrate(SaveData data);
    }
}
