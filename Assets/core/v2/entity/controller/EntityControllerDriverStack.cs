using System.Collections.Generic;
using System.Linq;

namespace StarfireV2
{
    public class EntityControllerDriverStack
    {
        private readonly List<IEntityControllerDriver> drivers = new();

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

        public void Clear()
        {
            drivers.Clear();
        }
    }
}