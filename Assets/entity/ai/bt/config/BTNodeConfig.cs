using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    public abstract class BTNodeConfig : ScriptableObject
    {
        public abstract IBTNode CreateNode();
    }
}
