using System.Collections.Generic;
using System.Linq;

namespace Starfire.Entity
{
    public class ControllerDriverStack
    {
        private readonly List<IControllerDriver> drivers = new();

        public void Push(IControllerDriver driver)
        {
            drivers.Add(driver);
            drivers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        public void Remove(IControllerDriver driver)
        {
            drivers.Remove(driver);
        }

        public IControllerDriver GetActiveDriver()
        {
            return drivers.FirstOrDefault(d => d.IsActive);
        }

        public void Clear()
        {
            drivers.Clear();
        }
    }
}