using Godot_V2.Scripts.Events;
using Godot_V2.Scripts.Gameplay;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class RoadEventChoiceResolverTests
{
    [Test]
    public void AppliesChoiceEffectsToTripState()
    {
        var state = TripState.CreateNew();
        state.SetMoney(100);
        state.SetFuel(20f);
        state.SetEnergy(50f);
        state.SetClockMinutes(9 * 60);
        state.SetVehiclePart(VehiclePart.Tires, 80f);
        var choice = new RoadEventChoice(
            "help",
            "帮忙修车",
            [
                new RoadEventEffect(RoadEventEffectKind.Money, 25),
                new RoadEventEffect(RoadEventEffectKind.FuelLiters, -3),
                new RoadEventEffect(RoadEventEffectKind.Energy, -10),
                new RoadEventEffect(RoadEventEffectKind.TimeMinutes, 45),
                new RoadEventEffect(RoadEventEffectKind.VehiclePartCondition, -20, "Tires"),
                new RoadEventEffect(RoadEventEffectKind.Flag, 1, "helped-stranger")
            ]);

        var result = RoadEventChoiceResolver.ApplyChoice(state, choice);

        Assert.That(result.Applied, Is.True);
        Assert.That(result.Flags, Is.EqualTo(new[] { "helped-stranger" }));
        Assert.That(state.Money, Is.EqualTo(125));
        Assert.That(state.FuelLiters, Is.EqualTo(17f));
        Assert.That(state.Energy, Is.EqualTo(40f));
        Assert.That(state.ClockMinutes, Is.EqualTo(9 * 60 + 45));
        Assert.That(state.GetVehiclePart(VehiclePart.Tires), Is.EqualTo(60f));
    }

    [Test]
    public void InvalidVehiclePartEffectReturnsFailureWithoutChangingState()
    {
        var state = TripState.CreateNew();
        state.SetVehiclePart(VehiclePart.Engine, 80f);
        var choice = new RoadEventChoice(
            "bad-part",
            "坏目标",
            [new RoadEventEffect(RoadEventEffectKind.VehiclePartCondition, -10, "UnknownPart")]);

        var result = RoadEventChoiceResolver.ApplyChoice(state, choice);

        Assert.That(result.Applied, Is.False);
        Assert.That(result.Message, Does.Contain("UnknownPart"));
        Assert.That(state.GetVehiclePart(VehiclePart.Engine), Is.EqualTo(80f));
    }
}
