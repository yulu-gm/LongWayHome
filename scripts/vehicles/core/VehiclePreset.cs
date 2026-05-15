using System.Collections.Generic;

namespace Godot_V2.Scripts.Vehicles.Core;

public sealed class VehiclePreset
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public DrivetrainType Drivetrain { get; init; }
    public float AwdFrontBias { get; init; } = 0.45f;

    public float MassKg { get; init; } = 1300f;
    public float CenterOfMassHeight { get; init; } = 0.42f;
    public float WheelBase { get; init; } = 2.65f;
    public float TrackWidth { get; init; } = 1.58f;
    public float WheelRadius { get; init; } = 0.34f;
    public float MaxSteerAngleDeg { get; init; } = 32f;

    public float SuspensionRestLength { get; init; } = 0.46f;
    public float SuspensionTravel { get; init; } = 0.34f;
    public float SpringStrength { get; init; } = 42000f;
    public float DamperStrength { get; init; } = 4800f;

    public float TireGrip { get; init; } = 1.1f;
    public float TirePeakSlip { get; init; } = 0.28f;
    public float TireSlideGrip { get; init; } = 0.62f;
    public float TireLongitudinalGrip { get; init; } = 1.05f;
    public float HandbrakeRearGripFactor { get; init; } = 0.42f;

    public float IdleRpm { get; init; } = 850f;
    public float RedlineRpm { get; init; } = 6500f;
    public float PeakTorqueNm { get; init; } = 280f;
    public float FinalDriveRatio { get; init; } = 3.42f;
    public float[] GearRatios { get; init; } = { 3.2f, 2.1f, 1.45f, 1.12f, 0.88f };
    public float UpshiftRpmFactor { get; init; } = 0.9f;
    public float DownshiftRpmFactor { get; init; } = 0.35f;
    public float MaxBrakeTorqueNm { get; init; } = 5600f;
    public float HandbrakeTorqueNm { get; init; } = 4200f;
}

public static class VehiclePresetFactory
{
    public static IReadOnlyList<VehiclePreset> CreateDefaults() =>
    [
        new VehiclePreset
        {
            Id = "fwd_family",
            DisplayName = "FWD 家用车",
            Drivetrain = DrivetrainType.Fwd,
            MassKg = 1360f,
            CenterOfMassHeight = 0.48f,
            PeakTorqueNm = 220f,
            TireGrip = 1.04f,
            TirePeakSlip = 0.24f,
            TireSlideGrip = 0.7f,
            HandbrakeRearGripFactor = 0.5f,
            MaxSteerAngleDeg = 34f,
            SpringStrength = 39000f,
            DamperStrength = 4600f
        },
        new VehiclePreset
        {
            Id = "rwd_standard",
            DisplayName = "RWD 标准车",
            Drivetrain = DrivetrainType.Rwd,
            MassKg = 1420f,
            CenterOfMassHeight = 0.44f,
            PeakTorqueNm = 305f,
            TireGrip = 1.08f,
            TirePeakSlip = 0.28f,
            TireSlideGrip = 0.62f,
            HandbrakeRearGripFactor = 0.36f,
            MaxSteerAngleDeg = 33f
        },
        new VehiclePreset
        {
            Id = "awd_road",
            DisplayName = "AWD 公路车",
            Drivetrain = DrivetrainType.Awd,
            AwdFrontBias = 0.42f,
            MassKg = 1540f,
            CenterOfMassHeight = 0.43f,
            PeakTorqueNm = 360f,
            TireGrip = 1.18f,
            TirePeakSlip = 0.25f,
            TireSlideGrip = 0.76f,
            HandbrakeRearGripFactor = 0.48f,
            SpringStrength = 46000f,
            DamperStrength = 5400f
        },
        new VehiclePreset
        {
            Id = "rwd_sports",
            DisplayName = "RWD 跑车",
            Drivetrain = DrivetrainType.Rwd,
            MassKg = 1280f,
            CenterOfMassHeight = 0.36f,
            PeakTorqueNm = 470f,
            RedlineRpm = 7200f,
            TireGrip = 1.2f,
            TirePeakSlip = 0.3f,
            TireSlideGrip = 0.58f,
            TireLongitudinalGrip = 1.15f,
            HandbrakeRearGripFactor = 0.32f,
            MaxSteerAngleDeg = 31f,
            SpringStrength = 52000f,
            DamperStrength = 6200f,
            GearRatios = [3.55f, 2.3f, 1.62f, 1.22f, 1.0f, 0.82f],
            FinalDriveRatio = 3.9f
        }
    ];
}
