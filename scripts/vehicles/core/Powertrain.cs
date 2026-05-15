using System;

namespace Godot_V2.Scripts.Vehicles.Core;

public readonly record struct PowertrainOutput(
    float EngineRpm,
    int Gear,
    WheelTorqueSet WheelTorque,
    float BrakeTorque,
    float HandbrakeTorque);

public sealed class Powertrain
{
    private const float DrivetrainEfficiency = 0.86f;
    private readonly VehiclePreset _preset;
    private readonly Gearbox _gearbox;

    public Powertrain(VehiclePreset preset)
    {
        _preset = preset;
        _gearbox = new Gearbox(
            preset.GearRatios,
            preset.FinalDriveRatio,
            preset.UpshiftRpmFactor,
            preset.DownshiftRpmFactor);
    }

    public int CurrentGear => _gearbox.CurrentGear;

    public PowertrainOutput Update(float deltaSeconds, float throttle, float brake, bool handbrake, float wheelSpeedKph, float reverseThrottle = 0f)
    {
        throttle = Math.Clamp(throttle, 0f, 1f);
        reverseThrottle = Math.Clamp(reverseThrottle, 0f, 1f);
        brake = Math.Clamp(brake, 0f, 1f);
        var isReverse = reverseThrottle > throttle && MathF.Abs(wheelSpeedKph) < 12f;
        var effectiveThrottle = isReverse ? reverseThrottle * 0.65f : throttle;

        var engineRpm = EstimateEngineRpm(wheelSpeedKph, effectiveThrottle);
        _gearbox.Update(deltaSeconds, effectiveThrottle, engineRpm, _preset.RedlineRpm);
        engineRpm = EstimateEngineRpm(wheelSpeedKph, effectiveThrottle);

        var engineTorque = EngineModel.EvaluateTorque(_preset, engineRpm, effectiveThrottle);
        var direction = isReverse ? -1f : 1f;
        var wheelTorqueTotal = engineTorque * _gearbox.CurrentRatio * _gearbox.FinalDriveRatio * DrivetrainEfficiency * direction;
        var wheelTorque = Differential.SplitTorque(_preset.Drivetrain, wheelTorqueTotal, _preset.AwdFrontBias);
        var handbrakeTorque = handbrake ? _preset.HandbrakeTorqueNm : 0f;

        return new PowertrainOutput(
            engineRpm,
            _gearbox.CurrentGear,
            wheelTorque,
            brake * _preset.MaxBrakeTorqueNm,
            handbrakeTorque);
    }

    private float EstimateEngineRpm(float wheelSpeedKph, float throttle)
    {
        var wheelCircumference = 2f * MathF.PI * _preset.WheelRadius;
        var wheelRpm = MathF.Abs(wheelSpeedKph) / 3.6f / wheelCircumference * 60f;
        var coupledRpm = wheelRpm * _gearbox.CurrentRatio * _gearbox.FinalDriveRatio;
        var freeRev = throttle * 850f;

        return Math.Clamp(Math.Max(_preset.IdleRpm + freeRev, coupledRpm), _preset.IdleRpm, _preset.RedlineRpm);
    }
}
