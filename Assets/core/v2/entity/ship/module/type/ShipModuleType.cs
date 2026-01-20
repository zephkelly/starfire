namespace StarfireV2
{
    public enum ShipModuleType
    {
        Hull,
        Shield,
        Deflector,

        ManeuveringThrusters,
        RotationalThrusters,

        ImpluseEngine,
        WarpDrive,
        HyperDrive,

        Weapon,
        PointDefense,

        SensorArray,
        Transponder,
        CargoBay,
        AICore
    }

    public enum ShipModuleTypeId
    {
        Hull = 0,
        Shield = 10,
        Deflector = 11,

        ManeuveringThrusters = 100,
        RotationalThrusters = 101,
        ImpluseEngine = 110,
        WarpDrive = 120,
        HyperDrive = 121,

        Weapon = 200,
        PointDefense = 210,

        SensorArray = 300,
        Transponder = 310,
        CargoBay = 400,
        AICore = 320
    }
}