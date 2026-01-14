using System.Linq;
using UnityEngine;
using Starfire.Entity.Modules.Transponder;

namespace Starfire.Entity.Modules.Sensor
{
    public struct DetectedEntity
    {
        public EntityControllerBase Controller;
        public DetectionLevel Level;
        public float Distance;
        public float LastUpdateTime;

        public bool IsValid => Controller != null;
        public Vector2 Position => Controller != null ? (Vector2)Controller.transform.position : Vector2.zero;

        public FactionData Faction
        {
            get
            {
                if (Level < DetectionLevel.Silhouette) return null;
                return GetTransponder()?.Faction;
            }
        }

        public string ShipClassName
        {
            get
            {
                if (Level < DetectionLevel.Silhouette) return null;
                return GetTransponder()?.ShipClass?.ClassName;
            }
        }

        public ShipClassDefinition ShipClass
        {
            get
            {
                if (Level < DetectionLevel.Silhouette) return null;
                return GetTransponder()?.ShipClass;
            }
        }

        public TransponderData? FullData
        {
            get
            {
                if (Level < DetectionLevel.Full) return null;
                var transponder = GetTransponder();
                return transponder?.GetTransponderData();
            }
        }

        private ITransponderModule GetTransponder()
        {
            return Controller?.Systems?.GetAllModulesOfType<ITransponderModule>().FirstOrDefault();
        }
    }
}
