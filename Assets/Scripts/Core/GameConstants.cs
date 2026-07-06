/// <summary>Names of scenes in the project. Avoids using magic strings.</summary>
public static class SceneNames
{
    public const string MainMenu   = "MainMenu";
    public const string CommsScene = "CommsScene";
    public const string GameScene  = "Game";
}

/// <summary>Flight constants and aircraft physics on the radar.</summary>
public static class FlightConstants
{
    /// <summary>The speed of the aircraft when departing from the base.</summary>
    public const float DepartureSpeed = 68f;

    /// <summary>Distance from the take-off point, after which the “take-off mode” is removed.</summary>
    public const float TakeoffCompleteDistance = 150f;

    /// <summary>Default holding circle radius.</summary>
    public const float DefaultHoldingRadius = 80f;

    /// <summary>Fuel threshold (units) at which the warning starts flashing.</summary>
    public const float LowFuelThreshold = 30f;

    /// <summary>Time (seconds) after which a plane without fuel crashes.</summary>
    public const float FuelEmergencyDuration = 20f;

    /// <summary>Number of distance units per 1 unit of fuel (must match UIAirplane.distancePerFuelUnit).</summary>
    public const float FuelPerDistanceUnit = 6f;

    /// <summary>The distance (in radar units) below which aircraft receive a proximity warning.</summary>
    public const float ConflictWarningDistance = 125f;

    /// <summary>Distance with hysteresis - used to reset the warning (slightly larger than ConflictWarningDistance).</summary>
    public const float ConflictHysteresisDistance = 135f;

    /// <summary>Duration of unloading the aircraft (seconds).</summary>
    public const float UnloadTime  = 15f;

    /// <summary>Duration of refueling the aircraft (seconds).</summary>
    public const float RefuelTime  = 15f;

    /// <summary>Maintenance duration (seconds).</summary>
    public const float RepairTime  = 20f;
}

public static class SaveKeys
{
    public const string TriggerEngineer       = "Trigger_Engineer";
    public const string BaseEmergencyEconomy  = "BaseEmergencyEconomy";
    public const string ReputationXP          = "ReputationXP";
    public const string StartDayNumber        = "StartDayNumber";
    public const string Day3Slots             = "Day3Slots";
}

public static class Callsigns
{
    public const string TR_404 = "TR-404";
    public const string TR_11  = "TR-11";
    public const string TR_88  = "TR-88";
    public const string TR_99  = "TR-99";
    public const string TR_33  = "TR-33";

    public const string GE_102 = "GE-102";
    public const string GE_201 = "GE-201";
    public const string GE_305 = "GE-305";
    public const string GE_55  = "GE-55";
    public const string GE_42  = "GE-42";
    public const string GE_98  = "GE-98";
    public const string GE_99  = "GE-99";

    public const string QY_884 = "QY-884";
    public const string QY_01  = "QY-01";
}
