using Godot_V2.Scripts.Gameplay;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class ResourceLedgerTests
{
    [Test]
    public void RainyMountainRouteConsumesMoreFuelThanClearRuralRoute()
    {
        var state = TripState.CreateNew();
        var clearRural = new TravelSegmentSpec(50f, RoadType.RuralRoad, TripWeather.Clear, VehicleWeightKg: 1400f);
        var rainyMountain = new TravelSegmentSpec(50f, RoadType.MountainRoad, TripWeather.Rain, VehicleWeightKg: 1600f);

        var clearCost = ResourceLedger.CalculateTravelCost(state, clearRural);
        var rainyCost = ResourceLedger.CalculateTravelCost(state, rainyMountain);

        Assert.That(rainyCost.FuelUsedLiters, Is.GreaterThan(clearCost.FuelUsedLiters));
    }

    [Test]
    public void NightRainConsumesMoreEnergyThanDayClear()
    {
        var state = TripState.CreateNew();
        var dayClear = ResourceLedger.CalculateTravelCost(
            state,
            new TravelSegmentSpec(30f, RoadType.RuralRoad, TripWeather.Clear, VehicleWeightKg: 1400f));

        state.SetClockMinutes(23 * 60);
        var nightRain = ResourceLedger.CalculateTravelCost(
            state,
            new TravelSegmentSpec(30f, RoadType.RuralRoad, TripWeather.Rain, VehicleWeightKg: 1400f));

        Assert.That(nightRain.EnergyUsed, Is.GreaterThan(dayClear.EnergyUsed));
    }

    [Test]
    public void ApplyTravelClampsFuelAndWarnsWhenInsufficient()
    {
        var state = TripState.CreateNew();
        state.SetFuel(1f);

        var result = ResourceLedger.ApplyTravel(
            state,
            new TravelSegmentSpec(80f, RoadType.Highway, TripWeather.Clear, VehicleWeightKg: 1400f));

        Assert.That(state.FuelLiters, Is.EqualTo(0f));
        Assert.That(result.FuelShortage, Is.True);
        Assert.That(result.RequiresStop, Is.True);
        Assert.That(result.DistanceCompletedKm, Is.LessThan(80f));
        Assert.That(result.Warnings, Does.Contain(ResourceWarning.FuelInsufficient));
    }

    [Test]
    public void CollisionDamageClampsVehicleParts()
    {
        var state = TripState.CreateNew();

        var result = ResourceLedger.ApplyCollisionDamage(
            state,
            new Dictionary<VehiclePart, float>
            {
                [VehiclePart.Engine] = 12.5f,
                [VehiclePart.Body] = 130f
            });

        Assert.That(state.GetVehiclePart(VehiclePart.Engine), Is.EqualTo(87.5f));
        Assert.That(state.GetVehiclePart(VehiclePart.Body), Is.EqualTo(0f));
        Assert.That(result.Warnings, Does.Contain(ResourceWarning.VehiclePartCritical));
    }
}
