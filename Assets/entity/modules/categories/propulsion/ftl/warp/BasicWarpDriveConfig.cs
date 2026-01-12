using UnityEngine;

namespace Starfire.Entity.Modules.WarpDrive
{
    [CreateAssetMenu(fileName = "BasicWarpDrive", menuName = "Starfire/Modules/WarpDrive/Basic")]
    public class BasicWarpDriveConfig : WarpDriveModuleConfig
    {
        public override IWarpDriveModule CreateModule()
        {
            return new BasicWarpDriveModule(this);
        }
    }
}
