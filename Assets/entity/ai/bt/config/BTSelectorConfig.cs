using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    [CreateAssetMenu(fileName = "Selector", menuName = "Starfire/AI/BT/Selector")]
    public class BTSelectorConfig : BTNodeConfig
    {
        [SerializeField] private List<BTNodeConfig> children = new();

        public override IBTNode CreateNode()
        {
            var childNodes = children
                .Where(c => c != null)
                .Select(c => c.CreateNode())
                .ToList();
            return new BTSelector(childNodes);
        }
    }
}
