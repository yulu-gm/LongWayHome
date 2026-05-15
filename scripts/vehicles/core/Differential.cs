using System;

namespace Godot_V2.Scripts.Vehicles.Core;

public static class Differential
{
    public static WheelTorqueSet SplitTorque(DrivetrainType drivetrain, float totalTorque, float awdFrontBias)
    {
        awdFrontBias = Math.Clamp(awdFrontBias, 0f, 1f);

        return drivetrain switch
        {
            DrivetrainType.Fwd => new WheelTorqueSet(totalTorque * 0.5f, totalTorque * 0.5f, 0f, 0f),
            DrivetrainType.Rwd => new WheelTorqueSet(0f, 0f, totalTorque * 0.5f, totalTorque * 0.5f),
            DrivetrainType.Awd => new WheelTorqueSet(
                totalTorque * awdFrontBias * 0.5f,
                totalTorque * awdFrontBias * 0.5f,
                totalTorque * (1f - awdFrontBias) * 0.5f,
                totalTorque * (1f - awdFrontBias) * 0.5f),
            _ => throw new ArgumentOutOfRangeException(nameof(drivetrain), drivetrain, null)
        };
    }
}
