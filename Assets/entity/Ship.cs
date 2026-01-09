namespace Starfire.Entity
{
    public class Ship : Entity
    {
        public ShipClassDefinition ClassDefinition { get; }

        public Ship(ShipClassDefinition definition)
            : base(definition.Capabilities)
        {
            ClassDefinition = definition;
        }
    }
}
