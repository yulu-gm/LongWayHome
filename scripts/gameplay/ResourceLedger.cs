using System;
using System.Collections.Generic;
using System.Linq;

namespace Godot_V2.Scripts.Gameplay;

public enum RoadType
{
    Highway,
    RuralRoad,
    MountainRoad,
    TownRoad,
    OldRoad
}

public enum ResourceWarning
{
    FuelInsufficient,
    EnergyDepleted,
    VehiclePartCritical
}

public readonly record struct TravelSegmentSpec(
    float DistanceKm,
    RoadType RoadType,
    TripWeather Weather,
    float VehicleWeightKg = 1400f);

public sealed record TravelSettlementResult(
    float FuelUsedLiters,
    float EnergyUsed,
    float DistanceCompletedKm,
    bool FuelShortage,
    bool RequiresStop,
    IReadOnlyList<ResourceWarning> Warnings);

public sealed record CollisionDamageResult(IReadOnlyList<ResourceWarning> Warnings);

public static class ResourceLedger
{
    private const float BaseFuelLitersPer100Km = 8f;
    private const float BaseVehicleWeightKg = 1400f;

    public static TravelSettlementResult CalculateTravelCost(TripState state, TravelSegmentSpec segment)
    {
        var distanceKm = Math.Max(0f, segment.DistanceKm);
        var fuelUsed = distanceKm / 100f
            * BaseFuelLitersPer100Km
            * GetRoadFuelMultiplier(segment.RoadType)
            * GetWeatherFuelMultiplier(segment.Weather)
            * GetWeightFuelMultiplier(segment.VehicleWeightKg);

        var energyUsed = distanceKm / 10f
            * GetEnergyPer10Km(state.TimeOfDay)
            + distanceKm / 10f * GetWeatherEnergyExtra(segment.Weather)
            + distanceKm / 10f * GetRoadEnergyExtra(segment.RoadType);

        return new TravelSettlementResult(
            fuelUsed,
            energyUsed,
            distanceKm,
            FuelShortage: false,
            RequiresStop: false,
            Warnings: Array.Empty<ResourceWarning>());
    }

    public static TravelSettlementResult ApplyTravel(TripState state, TravelSegmentSpec segment)
    {
        var cost = CalculateTravelCost(state, segment);
        var warnings = new List<ResourceWarning>();
        var fuelAvailable = state.FuelLiters;
        var travelRatio = cost.FuelUsedLiters <= 0f ? 1f : Math.Min(1f, fuelAvailable / cost.FuelUsedLiters);
        var fuelUsed = cost.FuelUsedLiters * travelRatio;
        var energyUsed = cost.EnergyUsed * travelRatio;
        var distanceCompleted = cost.DistanceCompletedKm * travelRatio;
        var fuelShortage = travelRatio < 0.999f;

        if (fuelShortage)
        {
            warnings.Add(ResourceWarning.FuelInsufficient);
        }

        state.SetFuel(state.FuelLiters - fuelUsed);
        state.SetEnergy(state.Energy - energyUsed);
        if (state.Energy <= 0f)
        {
            warnings.Add(ResourceWarning.EnergyDepleted);
        }

        var durationMinutes = distanceCompleted / Math.Max(1f, GetAverageSpeedKph(segment.RoadType)) * 60f;
        state.SetClockMinutes(state.ClockMinutes + (int)MathF.Ceiling(durationMinutes));

        return new TravelSettlementResult(
            fuelUsed,
            energyUsed,
            distanceCompleted,
            fuelShortage,
            RequiresStop: fuelShortage,
            Warnings: warnings);
    }

    public static CollisionDamageResult ApplyCollisionDamage(
        TripState state,
        IReadOnlyDictionary<VehiclePart, float> partDamage)
    {
        var warnings = new HashSet<ResourceWarning>();

        foreach (var (part, damage) in partDamage)
        {
            if (damage <= 0f)
            {
                continue;
            }

            state.SetVehiclePart(part, state.GetVehiclePart(part) - damage);
            if (state.GetVehiclePart(part) <= 20f)
            {
                warnings.Add(ResourceWarning.VehiclePartCritical);
            }
        }

        return new CollisionDamageResult(warnings.ToArray());
    }

    private static float GetRoadFuelMultiplier(RoadType roadType) => roadType switch
    {
        RoadType.Highway => 0.95f,
        RoadType.RuralRoad => 1f,
        RoadType.MountainRoad => 1.15f,
        RoadType.TownRoad => 1.1f,
        RoadType.OldRoad => 1.08f,
        _ => 1f
    };

    private static float GetWeatherFuelMultiplier(TripWeather weather) => weather switch
    {
        TripWeather.Clear => 1f,
        TripWeather.Rain => 1.08f,
        TripWeather.Fog => 1.03f,
        TripWeather.Storm => 1.15f,
        TripWeather.Snow => 1.2f,
        _ => 1f
    };

    private static float GetWeightFuelMultiplier(float vehicleWeightKg)
    {
        var extraWeight = Math.Max(0f, vehicleWeightKg - BaseVehicleWeightKg);
        return 1f + extraWeight / 1000f * 0.1f;
    }

    private static float GetEnergyPer10Km(TimeOfDayPhase phase) => phase switch
    {
        TimeOfDayPhase.Dawn => 4f,
        TimeOfDayPhase.Day => 3f,
        TimeOfDayPhase.Dusk => 4f,
        TimeOfDayPhase.Night => 5f,
        _ => 3f
    };

    private static float GetWeatherEnergyExtra(TripWeather weather) => weather switch
    {
        TripWeather.Rain => 1f,
        TripWeather.Fog => 1f,
        TripWeather.Storm => 2f,
        TripWeather.Snow => 2f,
        _ => 0f
    };

    private static float GetRoadEnergyExtra(RoadType roadType) => roadType switch
    {
        RoadType.MountainRoad => 1f,
        RoadType.TownRoad => 0.5f,
        RoadType.OldRoad => 0.5f,
        _ => 0f
    };

    private static float GetAverageSpeedKph(RoadType roadType) => roadType switch
    {
        RoadType.Highway => 90f,
        RoadType.RuralRoad => 70f,
        RoadType.MountainRoad => 45f,
        RoadType.TownRoad => 35f,
        RoadType.OldRoad => 50f,
        _ => 60f
    };
}
