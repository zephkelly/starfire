namespace Starfire.Entity
{
    public struct EntityId
    {
        public int Value;
        public EntityType Type;

        public EntityId(int value, EntityType type)
        {
            Value = value;
            Type = type;
        }
    }
}