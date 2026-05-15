using Godot_V2.Scripts.Gameplay;
using Godot_V2.Scripts.Vehicles.Core;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class VehicleConditionEffectsTests
{
    [Test]
    public void EngineConditionReducesTorqueMultiplier()
    {
        var state = TripState.CreateNew();
        state.SetVehiclePart(VehiclePart.Engine, 40f);

        var effects = VehicleConditionEffects.Calculate(state);

        Assert.That(effects.EngineTorqueMultiplier, Is.LessThan(0.8f));
        Assert.That(effects.EngineTorqueMultiplier, Is.GreaterThan(0.45f));
    }

    [Test]
    public void WornTiresInRainReduceGripAndBraking()
    {
        var state = TripState.CreateNew();
        state.SetVehiclePart(VehiclePart.Tires, 40f);
        state.SetWeather(TripWeather.Rain);

        var effects = VehicleConditionEffects.Calculate(state);

        Assert.That(effects.TireGripMultiplier, Is.LessThan(0.7f));
        Assert.That(effects.BrakeMultiplier, Is.LessThan(0.85f));
    }

    [Test]
    public void WeakLightsAtNightReduceVisibilityMultiplier()
    {
        var state = TripState.CreateNew();
        state.SetClockMinutes(22 * 60);
        state.SetVehiclePart(VehiclePart.Lights, 40f);

        var effects = VehicleConditionEffects.Calculate(state);

        Assert.That(effects.HeadlightVisibilityMultiplier, Is.LessThan(0.7f));
        Assert.That(effects.RequiresNightVisibilityWarning, Is.True);
    }

    [Test]
    public void SuspensionConditionFeedsPresetTuning()
    {
        var preset = VehiclePresetFactory.CreateDefaults().Single(preset => preset.Id == "rwd_standard");

        var tuned = VehiclePresetTuning.Apply(
            preset,
            engineTorqueMultiplier: 1f,
            tireGripMultiplier: 1f,
            brakeMultiplier: 1f,
            handbrakeRearGripMultiplier: 1f,
            suspensionMultiplier: 0.65f);

        Assert.That(tuned.SpringStrength, Is.EqualTo(preset.SpringStrength * 0.65f).Within(0.001f));
        Assert.That(tuned.DamperStrength, Is.EqualTo(preset.DamperStrength * 0.65f).Within(0.001f));
    }
}
