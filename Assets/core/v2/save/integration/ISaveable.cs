namespace Starfire.Core.V2.Save.Integration
{
    public interface ISaveable
    {
        EntitySaveData ToSaveData();
        void FromSaveData(EntitySaveData data);
    }
}
