using Godot_V2.Scripts.Vehicles.Core;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class VehicleCoreTests
{
    [Test]
    public void PresetFactoryCreatesFourDistinctDrivetrains()
    {
        var presets = VehiclePresetFactory.CreateDefaults();

        Assert.That(presets, Has.Count.EqualTo(4));
        Assert.That(presets.Select(p => p.Drivetrain).Distinct(), Is.SupersetOf(new[]
        {
            DrivetrainType.Fwd,
            DrivetrainType.Rwd,
            DrivetrainType.Awd
        }));
    }

    [Test]
    public void RwdDifferentialSendsTorqueOnlyToRearWheels()
    {
        var torque = Differential.SplitTorque(DrivetrainType.Rwd, 400f, 0.5f);

        Assert.That(torque.FrontLeft, Is.EqualTo(0f));
        Assert.That(torque.FrontRight, Is.EqualTo(0f));
        Assert.That(torque.RearLeft, Is.EqualTo(200f).Within(0.001f));
        Assert.That(torque.RearRight, Is.EqualTo(200f).Within(0.001f));
    }

    [Test]
    public void AwdDifferentialHonorsFrontBias()
    {
        var torque = Differential.SplitTorque(DrivetrainType.Awd, 1000f, 0.4f);

        Assert.That(torque.FrontLeft, Is.EqualTo(200f).Within(0.001f));
        Assert.That(torque.FrontRight, Is.EqualTo(200f).Within(0.001f));
        Assert.That(torque.RearLeft, Is.EqualTo(300f).Within(0.001f));
        Assert.That(torque.RearRight, Is.EqualTo(300f).Within(0.001f));
    }

    [Test]
    public void AutomaticGearboxUpshiftsNearRedline()
    {
        var gearbox = new Gearbox(new[] { 3.2f, 2.1f, 1.4f }, 3.42f, 0.8f, 0.35f);

        gearbox.Update(0.1f, 0.9f, 6100f, 6500f);

        Assert.That(gearbox.CurrentGear, Is.EqualTo(2));
    }

    [Test]
    public void PowertrainProducesForwardTorqueUnderThrottle()
    {
        var preset = VehiclePresetFactory.CreateDefaults().Single(p => p.Id == "rwd_standard");
        var powertrain = new Powertrain(preset);

        var output = powertrain.Update(0.016f, throttle: 1f, brake: 0f, handbrake: false, wheelSpeedKph: 20f);

        Assert.That(output.EngineRpm, Is.GreaterThan(preset.IdleRpm));
        Assert.That(output.WheelTorque.Total, Is.GreaterThan(0f));
    }

    [Test]
    public void PowertrainProducesReverseTorqueFromReverseInputAtLowSpeed()
    {
        var preset = VehiclePresetFactory.CreateDefaults().Single(p => p.Id == "rwd_standard");
        var powertrain = new Powertrain(preset);

        var output = powertrain.Update(0.016f, throttle: 0f, brake: 0f, handbrake: false, wheelSpeedKph: 0.5f, reverseThrottle: 1f);

        Assert.That(output.WheelTorque.Total, Is.LessThan(0f));
    }

    [Test]
    public void TireLateralForceFallsAfterPeakSlip()
    {
        var preset = VehiclePresetFactory.CreateDefaults().Single(p => p.Id == "rwd_standard");

        var lowSlip = TireForceModel.CalculateLateralForce(
            preset,
            normalLoad: 3500f,
            lateralSlip: 0.15f,
            handbrake: false,
            isRearWheel: true);
        var highSlip = TireForceModel.CalculateLateralForce(
            preset,
            normalLoad: 3500f,
            lateralSlip: 1.2f,
            handbrake: false,
            isRearWheel: true);

        Assert.That(MathF.Abs(highSlip), Is.LessThan(MathF.Abs(lowSlip)));
    }

    [Test]
    public void HandbrakeReducesRearLateralGrip()
    {
        var preset = VehiclePresetFactory.CreateDefaults().Single(p => p.Id == "rwd_standard");

        var normal = TireForceModel.CalculateLateralForce(
            preset,
            normalLoad: 3500f,
            lateralSlip: 0.25f,
            handbrake: false,
            isRearWheel: true);
        var handbrake = TireForceModel.CalculateLateralForce(
            preset,
            normalLoad: 3500f,
            lateralSlip: 0.25f,
            handbrake: true,
            isRearWheel: true);

        Assert.That(MathF.Abs(handbrake), Is.LessThan(MathF.Abs(normal) * 0.7f));
    }

    [Test]
    public void HandbrakeDoesNotReduceFrontLateralGrip()
    {
        var preset = VehiclePresetFactory.CreateDefaults().Single(p => p.Id == "rwd_standard");

        var normal = TireForceModel.CalculateLateralForce(
            preset,
            normalLoad: 3500f,
            lateralSlip: 0.25f,
            handbrake: false,
            isRearWheel: false);
        var handbrake = TireForceModel.CalculateLateralForce(
            preset,
            normalLoad: 3500f,
            lateralSlip: 0.25f,
            handbrake: true,
            isRearWheel: false);

        Assert.That(MathF.Abs(handbrake), Is.EqualTo(MathF.Abs(normal)).Within(0.001f));
    }

    [Test]
    public void RuntimeTuningScalesCopyWithoutChangingBasePreset()
    {
        var preset = VehiclePresetFactory.CreateDefaults().Single(p => p.Id == "rwd_standard");

        var tuned = VehiclePresetTuning.Apply(
            preset,
            engineTorqueMultiplier: 1.5f,
            tireGripMultiplier: 0.8f,
            brakeMultiplier: 1.2f,
            handbrakeRearGripMultiplier: 0.5f);

        Assert.That(tuned.PeakTorqueNm, Is.EqualTo(preset.PeakTorqueNm * 1.5f).Within(0.001f));
        Assert.That(tuned.TireGrip, Is.EqualTo(preset.TireGrip * 0.8f).Within(0.001f));
        Assert.That(tuned.MaxBrakeTorqueNm, Is.EqualTo(preset.MaxBrakeTorqueNm * 1.2f).Within(0.001f));
        Assert.That(tuned.HandbrakeRearGripFactor, Is.EqualTo(preset.HandbrakeRearGripFactor * 0.5f).Within(0.001f));
        Assert.That(preset.PeakTorqueNm, Is.EqualTo(305f));
    }
}
