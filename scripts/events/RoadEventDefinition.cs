using System;
using System.Collections.Generic;
using System.Linq;
using Godot_V2.Scripts.Gameplay;

namespace Godot_V2.Scripts.Events;

public enum RoadEventCategory
{
    Road,
    LongTerm,
    Cabin,
    Condition,
    Place
}

public enum RoadEventEffectKind
{
    FuelLiters,
    Money,
    Energy,
    VehiclePartCondition,
    TimeMinutes,
    Flag
}

public sealed record RoadEventEffect(
    RoadEventEffectKind Kind,
    float Amount,
    string Target = "");

public sealed record RoadEventChoice(
    string Id,
    string Text,
    IReadOnlyList<RoadEventEffect> Results);

public sealed record RoadEventTrigger(
    IReadOnlyList<string>? RequiredTags = null,
    TripWeather? RequiredWeather = null,
    TimeOfDayPhase? RequiredTimeOfDay = null,
    float? MaxFuelRatio = null,
    float? MaxEnergy = null,
    VehiclePart? MaxVehiclePart = null,
    float? MaxVehiclePartCondition = null)
{
    public static RoadEventTrigger Any { get; } = new([]);

    public bool Matches(TripState state, RoadEventContext context)
    {
        var requiredTags = RequiredTags ?? Array.Empty<string>();
        if (requiredTags.Any(tag => !context.RouteTags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (RequiredWeather is not null && state.Weather != RequiredWeather)
        {
            return false;
        }

        if (RequiredTimeOfDay is not null && state.TimeOfDay != RequiredTimeOfDay)
        {
            return false;
        }

        if (MaxFuelRatio is not null)
        {
            var fuelRatio = state.FuelCapacityLiters <= 0f ? 0f : state.FuelLiters / state.FuelCapacityLiters;
            if (fuelRatio > MaxFuelRatio.Value)
            {
                return false;
            }
        }

        if (MaxEnergy is not null && state.Energy > MaxEnergy.Value)
        {
            return false;
        }

        if (MaxVehiclePart is not null && MaxVehiclePartCondition is not null)
        {
            if (state.GetVehiclePart(MaxVehiclePart.Value) > MaxVehiclePartCondition.Value)
            {
                return false;
            }
        }

        return true;
    }
}

public sealed record RoadEventDefinition(
    string Id,
    string Title,
    string SceneText,
    RoadEventCategory Category,
    int Weight,
    int CooldownLegs,
    bool IsOneTime,
    IReadOnlyList<string> Tags,
    RoadEventTrigger Trigger,
    IReadOnlyList<RoadEventChoice> Choices);

public sealed record RoadEventContext(
    IReadOnlyList<string> RouteTags,
    int LegIndex,
    IReadOnlyDictionary<string, int> Cooldowns,
    IReadOnlySet<string> CompletedOneTimeEventIds);

public static class RoadEventSelector
{
    public static IReadOnlyList<RoadEventDefinition> GetEligibleEvents(
        IReadOnlyList<RoadEventDefinition> events,
        TripState state,
        RoadEventContext context) =>
        events
            .Where(roadEvent => roadEvent.Weight > 0)
            .Where(roadEvent => !roadEvent.IsOneTime || !context.CompletedOneTimeEventIds.Contains(roadEvent.Id))
            .Where(roadEvent => !context.Cooldowns.TryGetValue(roadEvent.Id, out var availableLeg)
                || context.LegIndex >= availableLeg)
            .Where(roadEvent => roadEvent.Trigger.Matches(state, context))
            .ToArray();

    public static RoadEventDefinition? SelectWeighted(
        IReadOnlyList<RoadEventDefinition> events,
        int seed)
    {
        var weightedEvents = events.Where(roadEvent => roadEvent.Weight > 0).ToArray();
        var totalWeight = weightedEvents.Sum(roadEvent => roadEvent.Weight);
        if (totalWeight <= 0)
        {
            return null;
        }

        var random = new Random(seed);
        var roll = random.Next(totalWeight);
        var cursor = 0;
        foreach (var roadEvent in weightedEvents)
        {
            cursor += roadEvent.Weight;
            if (roll < cursor)
            {
                return roadEvent;
            }
        }

        return weightedEvents[^1];
    }
}
