using UnityEngine;

namespace Starfire
{
    [CreateAssetMenu(fileName = "Create New ShipConfigAsset", menuName = "Starfire/ShipConfiguration")]
    public class ShipConfigurationAsset : ScriptableObject
    {
        public int Health;
        public int Fuel;
        public int WarpFuel;
        public int Cargo;
        public int ThrusterMaxSpeed;
        public int WarpMaxSpeed;
        public int TurnSpeed;
        public int ProjectileDamage;
    }
}