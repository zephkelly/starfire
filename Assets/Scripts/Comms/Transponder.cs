namespace Starfire
{
    public enum Faction
    {
        Player,
        Enemy,
        Friend,
        Neutral
    }

    public enum TransponderStatus
    {
        Active,
        Inactive
    }

    public class Transponder
    {
        public string Name { get; private set; }
        public Faction Faction { get; private set; }

        public TransponderStatus Status { get; private set; } 
        public int Frequency { get; private set; }

        public Transponder(string name, Faction factionType, int frequency)
        {
            Name = name;
            Faction = factionType;
            Frequency = frequency;
        }

        public void SetStatus(TransponderStatus status)
        {
            Status = status;
        }
    }
}