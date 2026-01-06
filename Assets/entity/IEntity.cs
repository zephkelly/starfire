#nullable enable

namespace Starfire.Entity
{
    public interface IEntity
    {
        public int Health { get; }
        public Shield? Shield { get; }
        public int Energy { get; }
        public int Fuel { get; }
    }
}