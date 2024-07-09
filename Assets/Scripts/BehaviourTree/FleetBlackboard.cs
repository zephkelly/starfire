using System.Collections.Generic;

namespace Starfire
{
    public class FleetBlackboard
    {
        public List<Ship> shipsUnderAttack = new List<Ship>();
        public IReadOnlyList<Ship> ShipsUnderAttack => shipsUnderAttack.AsReadOnly();
        public bool IsFleetUnderAttack => shipsUnderAttack.Count > 0;
    }
}