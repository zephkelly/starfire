namespace Starfire.Entity
{
    public enum EntityPersistence
    {
        Transient, // Groups in a fleet, despawns at sim tier level-4
        Persistent, // Always individual, never despawns
        Critical
    }
}