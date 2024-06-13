namespace Starfire
{
    public enum FactionType
    {
        Player,
        Scavenger,
        Pirate,
        Neutral
    }

    public enum TransponderType
    {
        Civilian,
        Military,
        Pirate,
        Scavenger,
        Neutral
    }

    public enum TransponderStatus
    {
        Active,
        Dark,
        Inactive,
        Damaged
    }

    public class Transponder
    {
        public string Name { get; private set; }
        public FactionType Faction { get; private set; }

        public TransponderStatus Status { get; private set; } 
        public string Frequency { get; private set; }

        public Transponder(string name, FactionType factionType, string frequency)
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