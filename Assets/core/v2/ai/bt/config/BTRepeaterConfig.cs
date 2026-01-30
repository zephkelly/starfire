using UnityEngine;

namespace StarfireV2
{
    [CreateAssetMenu(fileName = "Repeater", menuName = "Starfire/AI/BT/Repeater")]
    public class BTRepeaterConfig : BTNodeConfig
    {
        [SerializeField] private BTNodeConfig child;
        [SerializeField] private int repeatCount = -1;

        public override IBTNode CreateNode()
        {
            var childNode = child?.CreateNode();
            return new BTRepeater(childNode, repeatCount);
        }
    }
}
