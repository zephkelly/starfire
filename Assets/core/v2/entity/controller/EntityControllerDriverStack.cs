using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarfireV2
{
    [Serializable]
    public class EntityControllerDriverStack
    {
        [SerializeReference]
        private List<IEntityControllerDriver> drivers = new();

        public void Push(IEntityControllerDriver driver)
        {
            drivers.Add(driver);
            drivers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        public void Remove(IEntityControllerDriver driver)
        {
            drivers.Remove(driver);
        }

        public IEntityControllerDriver GetActiveDriver()
        {
            return drivers.FirstOrDefault(d => d.IsActive);
        }

        public void SetActiveDriver(IEntityControllerDriver driver)
        {
            if (drivers.Contains(driver))
            {
                drivers.Remove(driver);
            }
            drivers.Insert(0, driver);
        }

        public void Clear()
        {
            drivers.Clear();
        }

        /// <summary>
        /// Initializes all drivers that require runtime setup.
        /// Call this from your controller's Start() method.
        /// </summary>
        public void InitializeDrivers()
        {
            foreach (var driver in drivers)
            {
                if (driver is PlayerEntityControllerDriver playerDriver)
                {
                    playerDriver.EnsureInitialized();
                }
            }
        }
    }
}