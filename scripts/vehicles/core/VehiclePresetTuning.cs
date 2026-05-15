using System;
using System.Linq;

namespace Godot_V2.Scripts.Vehicles.Core;

public static class VehiclePresetTuning
{
    public static VehiclePreset Apply(
        VehiclePreset preset,
        float engineTorqueMultiplier,
        float tireGripMultiplier,
        float brakeMultiplier,
        float handbrakeRearGripMultiplier,
        float suspensionMultiplier = 1f)
    {
        engineTorqueMultiplier = Math.Clamp(engineTorqueMultiplier, 0.25f, 2.5f);
        tireGripMultiplier = Math.Clamp(tireGripMultiplier, 0.35f, 1.8f);
        brakeMultiplier = Math.Clamp(brakeMultiplier, 0.25f, 2f);
        handbrakeRearGripMultiplier = Math.Clamp(handbrakeRearGripMultiplier, 0.15f, 1.2f);
        suspensionMultiplier = Math.Clamp(suspensionMultiplier, 0.35f, 1.5f);

        return new VehiclePreset
        {
            Id = preset.Id,
            DisplayName = preset.DisplayName,
            Drivetrain = preset.Drivetrain,
            AwdFrontBias = preset.AwdFrontBias,
            MassKg = preset.MassKg,
            CenterOfMassHeight = preset.CenterOfMassHeight,
            WheelBase = preset.WheelBase,
            TrackWidth = preset.TrackWidth,
            WheelRadius = preset.WheelRadius,
            MaxSteerAngleDeg = preset.MaxSteerAngleDeg,
            SuspensionRestLength = preset.SuspensionRestLength,
            SuspensionTravel = preset.SuspensionTravel,
            SpringStrength = preset.SpringStrength * suspensionMultiplier,
            DamperStrength = preset.DamperStrength * suspensionMultiplier,
            TireGrip = preset.TireGrip * tireGripMultiplier,
            TirePeakSlip = preset.TirePeakSlip,
            TireSlideGrip = preset.TireSlideGrip,
            TireLongitudinalGrip = preset.TireLongitudinalGrip,
            HandbrakeRearGripFactor = preset.HandbrakeRearGripFactor * handbrakeRearGripMultiplier,
            IdleRpm = preset.IdleRpm,
            RedlineRpm = preset.RedlineRpm,
            PeakTorqueNm = preset.PeakTorqueNm * engineTorqueMultiplier,
            FinalDriveRatio = preset.FinalDriveRatio,
            GearRatios = preset.GearRatios.ToArray(),
            UpshiftRpmFactor = preset.UpshiftRpmFactor,
            DownshiftRpmFactor = preset.DownshiftRpmFactor,
            MaxBrakeTorqueNm = preset.MaxBrakeTorqueNm * brakeMultiplier,
            HandbrakeTorqueNm = preset.HandbrakeTorqueNm
        };
    }
}
