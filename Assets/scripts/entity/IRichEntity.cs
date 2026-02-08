using Starfire.Core;
using Starfire.Faction;

namespace Starfire.Entity
{
    public interface IRichEntity
    {
        EntityId Id { get; set; }
        int ConfigId { get; }
        FactionId FactionId { get; set; }
        EntityPersistence Persistence { get; set; }

        AbsolutePosition Position { get; set; }
        Velocity Velocity { get; set; }
        float Rotation { get; set; }
    }
}