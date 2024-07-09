namespace Starfire
{
    public class EmergencyResponse : SequenceNode
    {
        public EmergencyResponse(Ship ship)
        {
            AddNode(new CheckForImmediateThreats(ship));
            AddNode(new RespondToThreats(ship));
        }
    }
}