using Godot_V2.Scripts.Gameplay;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class TripStateTests
{
    [Test]
    public void NewGameCreatesRoadTripDefaults()
    {
        var state = TripState.CreateNew();

        Assert.That(state.FuelCapacityLiters, Is.EqualTo(60f));
        Assert.That(state.FuelLiters, Is.EqualTo(45f));
        Assert.That(state.Money, Is.EqualTo(620));
        Assert.That(state.Energy, Is.EqualTo(82f));
        Assert.That(state.ClockMinutes, Is.EqualTo(9 * 60 + 42));
        Assert.That(state.TimeOfDay, Is.EqualTo(TimeOfDayPhase.Day));
        Assert.That(state.Weather, Is.EqualTo(TripWeather.Clear));
        Assert.That(state.CurrentLocation, Is.EqualTo("旧木镇外"));
        Assert.That(state.TargetDistanceKm, Is.EqualTo(86f));

        foreach (var part in Enum.GetValues<VehiclePart>())
        {
            Assert.That(state.GetVehiclePart(part), Is.EqualTo(100f));
        }
    }

    [Test]
    public void ScalarResourcesClampToValidRanges()
    {
        var state = TripState.CreateNew();

        state.SetFuel(999f);
        state.SetEnergy(-10f);
        state.SetMoney(-40);
        state.SetTargetDistance(-5f);

        Assert.That(state.FuelLiters, Is.EqualTo(60f));
        Assert.That(state.Energy, Is.EqualTo(0f));
        Assert.That(state.Money, Is.EqualTo(0));
        Assert.That(state.TargetDistanceKm, Is.EqualTo(0f));

        state.SetFuelCapacity(35f);

        Assert.That(state.FuelCapacityLiters, Is.EqualTo(35f));
        Assert.That(state.FuelLiters, Is.EqualTo(35f));
    }

    [Test]
    public void VehiclePartConditionClampsAndAverages()
    {
        var state = TripState.CreateNew();

        state.SetVehiclePart(VehiclePart.Engine, 72f);
        state.SetVehiclePart(VehiclePart.Tires, -5f);
        state.SetVehiclePart(VehiclePart.Lights, 130f);

        Assert.That(state.GetVehiclePart(VehiclePart.Engine), Is.EqualTo(72f));
        Assert.That(state.GetVehiclePart(VehiclePart.Tires), Is.EqualTo(0f));
        Assert.That(state.GetVehiclePart(VehiclePart.Lights), Is.EqualTo(100f));
        Assert.That(state.AverageVehicleCondition, Is.LessThan(100f));
    }

    [Test]
    public void ClockWrapsAndDerivesTimeOfDay()
    {
        var state = TripState.CreateNew();

        state.SetClockMinutes(18 * 60);
        Assert.That(state.TimeOfDay, Is.EqualTo(TimeOfDayPhase.Dusk));

        state.SetClockMinutes(23 * 60);
        Assert.That(state.TimeOfDay, Is.EqualTo(TimeOfDayPhase.Night));

        state.SetClockMinutes(-30);
        Assert.That(state.ClockMinutes, Is.EqualTo(23 * 60 + 30));
        Assert.That(state.TimeOfDay, Is.EqualTo(TimeOfDayPhase.Night));
    }
}
