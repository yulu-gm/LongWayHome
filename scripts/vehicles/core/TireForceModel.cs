using System;

namespace Godot_V2.Scripts.Vehicles.Core;

public static class TireForceModel
{
    public static float CalculateLateralForce(
        VehiclePreset preset,
        float normalLoad,
        float lateralSlip,
        bool handbrake,
        bool isRearWheel)
    {
        if (normalLoad <= 0f || MathF.Abs(lateralSlip) <= 0.0001f)
        {
            return 0f;
        }

        var grip = preset.TireGrip;
        if (handbrake && isRearWheel)
        {
            grip *= preset.HandbrakeRearGripFactor;
        }

        var slipMagnitude = MathF.Abs(lateralSlip);
        var peakSlip = Math.Max(0.01f, preset.TirePeakSlip);
        var peakForce = normalLoad * grip;
        float forceMagnitude;

        if (slipMagnitude <= peakSlip)
        {
            forceMagnitude = peakForce * MathF.Sqrt(slipMagnitude / peakSlip);
        }
        else
        {
            var slideBlend = Math.Clamp((slipMagnitude - peakSlip) / Math.Max(0.01f, 1.1f - peakSlip), 0f, 1f);
            var slideGrip = Math.Clamp(preset.TireSlideGrip, 0.05f, 1f);
            forceMagnitude = peakForce * (1f - slideBlend * (1f - slideGrip));
        }

        return -MathF.Sign(lateralSlip) * forceMagnitude;
    }

    public static float CalculateLongitudinalForce(
        VehiclePreset preset,
        float normalLoad,
        float driveTorqueNm,
        float brakeTorqueNm,
        float wheelRadius,
        float localForwardVelocity)
    {
        if (normalLoad <= 0f || wheelRadius <= 0.01f)
        {
            return 0f;
        }

        var driveForce = driveTorqueNm / wheelRadius;
        var brakeDirection = MathF.Abs(localForwardVelocity) > 0.2f
            ? -MathF.Sign(localForwardVelocity)
            : -MathF.Sign(driveForce);
        var brakeForce = brakeTorqueNm / wheelRadius * brakeDirection;
        var requested = driveForce + brakeForce;
        var maxForce = normalLoad * preset.TireGrip * preset.TireLongitudinalGrip;

        return Math.Clamp(requested, -maxForce, maxForce);
    }

    public static float EstimateSlip(float lateralVelocity, float forwardSpeed)
    {
        var denominator = MathF.Max(MathF.Abs(forwardSpeed), 3f);
        return lateralVelocity / denominator;
    }
}
