using UnityEngine;

namespace Starfire.Entity.Modules.Sensor
{
    [CreateAssetMenu(fileName = "BasicSensor", menuName = "Starfire/Modules/Sensor/Basic")]
    public class BasicSensorConfig : SensorModuleConfig
    {
        public override ISensorShipModule CreateModule()
        {
            return new BasicSensorModule(this);
        }
    }
}
