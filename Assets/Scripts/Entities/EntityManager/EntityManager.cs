using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    public class EntityManager : MonoBehaviour
    {
        [SerializeField] private List<Fleet> fleets = new List<Fleet>();

        private void Start()
        {
            CreateFleet();
        }

        private void Update()
        {
            UpdateFleets();
        }

        private void UpdateFleets()
        {
            foreach (var fleet in fleets)
            {
                fleet.Update();
            }
        }

        private void CreateFleet()
        {
            Fleet fleet = new Fleet(FleetType.Military, FleetAllegience.Friend);
            fleets.Add(fleet);

            var randomFleetNumber = Random.Range(1, 4);
            
            for (int i = 0; i < randomFleetNumber; i++)
            {
                Vector2 position = new Vector2(i * 20, 0);
                var paladinPrefab = Resources.Load<GameObject>("Prefabs/Entities/Paladin");
                var newPaladin = Instantiate(paladinPrefab, position, Quaternion.identity);
                newPaladin.TryGetComponent<PaladinShipController>(out var shipController);

                var newConfiguration = ScriptableObject.CreateInstance("ShipConfiguration") as ShipConfiguration;
                newConfiguration.SetConfiguration(shipController, 360, 100, 100, 100, 160, 1500, 200, 6);
                var newTransponder = new Transponder("Paladin", Faction.Friendly, 90000);
                var newInventory = new Inventory();

                var newShip = new Ship(shipController, newConfiguration, newTransponder, newInventory);
                shipController.SetShip(newShip, new StandardAICore());

                fleet.AddShip(newShip);
            }


            var newFleetCommand = new FleetCommand(FleetCommand.CommandType.Move, new Vector2(100, 2000));
            var newFleetCommand2 = new FleetCommand(FleetCommand.CommandType.Idle);
            fleet.AddNewCommand(newFleetCommand);
            fleet.AddNewCommand(newFleetCommand2);
        }
    }
}