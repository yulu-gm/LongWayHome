namespace Godot_V2.Scripts.Vehicles.Core;

public enum DrivetrainType
{
    Fwd,
    Rwd,
    Awd
}

public enum WheelPosition
{
    FrontLeft,
    FrontRight,
    RearLeft,
    RearRight
}

public readonly record struct VehicleInputSnapshot(
    float Throttle,
    float Brake,
    float Steering,
    bool Handbrake);

public readonly record struct WheelTorqueSet(
    float FrontLeft,
    float FrontRight,
    float RearLeft,
    float RearRight)
{
    public float Total => FrontLeft + FrontRight + RearLeft + RearRight;
}

public sealed class VehicleTelemetry
{
    public float SpeedKph { get; set; }
    public float EngineRpm { get; set; }
    public int Gear { get; set; }
    public DrivetrainType Drivetrain { get; set; }
    public bool Handbrake { get; set; }
    public float FrontSlip { get; set; }
    public float RearSlip { get; set; }
    public float FrontLeftLoad { get; set; }
    public float FrontRightLoad { get; set; }
    public float RearLeftLoad { get; set; }
    public float RearRightLoad { get; set; }
    public float FrontLeftCompression { get; set; }
    public float FrontRightCompression { get; set; }
    public float RearLeftCompression { get; set; }
    public float RearRightCompression { get; set; }
}
