using UnityEngine;

namespace StarfireV2
{
    public abstract class BTNodeConfig : ScriptableObject
    {
        public abstract IBTNode CreateNode();
    }
}
