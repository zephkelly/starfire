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

            if (Input.GetKeyDown(KeyCode.L))
            {
                var mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
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
            Blackboard fleetBlackboard = fleet.GetBlackboard();
            fleetBlackboard.SetValue("Fleet", "Hello world!");
            fleets.Add(fleet);

            var randomFleetNumber = Random.Range(6, 8);
            
            for (int i = 0; i < randomFleetNumber; i++)
            {
                Vector2 position = new Vector2(i * 20, 0);
                var paladinPrefab = Resources.Load<GameObject>("Prefabs/Entities/Paladin");
                var newPaladin = Instantiate(paladinPrefab, position, Quaternion.identity);
                newPaladin.TryGetComponent<ShipController>(out var shipController);

                var newConfiguration = new ShipConfiguration(shipController, 360, 100, 100, 100, 160, 1500, 200);
                var newTransponder = new Transponder("Paladin", Faction.Friend, 90000);
                var newInventory = new Inventory();
                var aiCore = new StandardAICore();

                var newShip = new Ship(shipController, aiCore, newConfiguration, newTransponder, newInventory);
                newShip.InjectAIDependencies(fleet, fleetBlackboard);

                newShip.AICore.SetTarget(new Vector2(1000, 1000));

                if (i == 0)
                {
                    fleet.SetFlagship(newShip);
                }

                fleet.AddShip(newShip);
            }

        }
    }
}