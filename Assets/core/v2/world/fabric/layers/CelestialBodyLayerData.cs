using System.Collections.Generic;

namespace StarfireV2
{
    public class CelestialBodyLayerData : WorldLayerData
    {
        public override string LayerId => "CelestialBodies";

        /// <summary>
        /// All celestial bodies whose gravity/visual radius overlaps this chunk.
        /// </summary>
        public List<CelestialBodyInfo> Bodies = new List<CelestialBodyInfo>();
    }
}
