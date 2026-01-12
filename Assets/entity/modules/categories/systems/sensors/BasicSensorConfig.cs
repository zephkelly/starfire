using UnityEngine;

namespace Starfire.Entity.Modules.Sensor
{
    [CreateAssetMenu(fileName = "BasicSensor", menuName = "Starfire/Modules/Sensor/Basic")]
    public class BasicSensorConfig : SensorModuleConfig
    {
        public override ISensorModule CreateModule()
        {
            return new BasicSensorModule(this);
        }
    }
}
