using System;
using Starfire.Entity.Modules;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Parameters for HasModuleSubCategoryCondition.
    /// Checks if entity has any module in a specific subcategory.
    /// </summary>
    [Serializable]
    public class HasModuleSubCategoryParameters : IBTNodeParameters
    {
        /// <summary>
        /// The subcategory to check (e.g., FTL, Offensive).
        /// </summary>
        public ModuleSubCategory subCategory = ModuleSubCategory.FTL;

        /// <summary>
        /// Minimum number of equipped modules required for condition to pass.
        /// </summary>
        public int minimumCount = 1;
    }
}
