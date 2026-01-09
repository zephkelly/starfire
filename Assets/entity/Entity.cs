namespace Starfire.Entity
{
    public abstract class Entity : IEntity
    {
        public EntityCapabilities[] Capabilities { get; protected set; }

        protected Entity(EntityCapabilities[] capabilities)
        {
            Capabilities = capabilities ?? new EntityCapabilities[0];
        }
    }
}
