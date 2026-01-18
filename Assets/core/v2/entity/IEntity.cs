namespace StarfireV2
{
    public interface IEntity
    {
        EntityType EntityType { get; }
        int Id { get; }
    }
}