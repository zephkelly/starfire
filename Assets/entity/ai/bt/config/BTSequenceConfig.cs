using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    [CreateAssetMenu(fileName = "Sequence", menuName = "Starfire/AI/BT/Sequence")]
    public class BTSequenceConfig : BTNodeConfig
    {
        [SerializeField] private List<BTNodeConfig> children = new();

        public override IBTNode CreateNode()
        {
            var childNodes = children
                .Where(c => c != null)
                .Select(c => c.CreateNode())
                .ToList();
            return new BTSequence(childNodes);
        }
    }
}
