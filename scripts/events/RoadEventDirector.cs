using System;
using System.Collections.Generic;
using System.Linq;
using Godot_V2.Scripts.Gameplay;

namespace Godot_V2.Scripts.Events;

public sealed record RoadEventCandidate(
    RoadEventDefinition Event,
    int AdjustedWeight);

public sealed record RoadEventDirectorResult(
    bool Triggered,
    RoadEventDefinition? Event,
    IReadOnlyList<RoadEventCandidate> Candidates);

public static class RoadEventDirector
{
    public static IReadOnlyList<RoadEventCandidate> GetWeightedCandidates(
        IReadOnlyList<RoadEventDefinition> events,
        TripState state,
        RoadEventContext context) =>
        RoadEventSelector.GetEligibleEvents(events, state, context)
            .Select(roadEvent => new RoadEventCandidate(
                roadEvent,
                Math.Max(0, roadEvent.Weight + GetContextWeightBonus(roadEvent, state))))
            .Where(candidate => candidate.AdjustedWeight > 0)
            .ToArray();

    public static RoadEventDirectorResult SelectEvent(
        IReadOnlyList<RoadEventDefinition> events,
        TripState state,
        RoadEventContext context,
        int seed)
    {
        var candidates = GetWeightedCandidates(events, state, context);
        if (candidates.Count == 0)
        {
            return new RoadEventDirectorResult(false, null, candidates);
        }

        var selected = SelectWeightedCandidate(candidates, seed);
        return new RoadEventDirectorResult(true, selected.Event, candidates);
    }

    private static RoadEventCandidate SelectWeightedCandidate(
        IReadOnlyList<RoadEventCandidate> candidates,
        int seed)
    {
        var totalWeight = candidates.Sum(candidate => candidate.AdjustedWeight);
        var random = new Random(seed);
        var roll = random.Next(totalWeight);
        var cursor = 0;
        foreach (var candidate in candidates)
        {
            cursor += candidate.AdjustedWeight;
            if (roll < cursor)
            {
                return candidate;
            }
        }

        return candidates[^1];
    }

    private static int GetContextWeightBonus(RoadEventDefinition roadEvent, TripState state)
    {
        var bonus = 0;
        var fuelRatio = state.FuelCapacityLiters <= 0f ? 0f : state.FuelLiters / state.FuelCapacityLiters;
        if (fuelRatio <= 0.2f && HasAnyTag(roadEvent, "fuel", "low-fuel"))
        {
            bonus += Math.Max(2, roadEvent.Weight);
        }

        var isRainyNight = state.Weather is TripWeather.Rain or TripWeather.Storm
            && state.TimeOfDay == TimeOfDayPhase.Night;
        if (isRainyNight && HasAnyTag(roadEvent, "visibility", "vehicle", "lights", "tires", "rain"))
        {
            bonus += Math.Max(2, roadEvent.Weight);
        }

        if (state.Energy <= 20f && HasAnyTag(roadEvent, "fatigue", "rest", "cabin"))
        {
            bonus += Math.Max(1, roadEvent.Weight / 2);
        }

        return bonus;
    }

    private static bool HasAnyTag(RoadEventDefinition roadEvent, params string[] tags) =>
        roadEvent.Tags.Any(eventTag => tags.Any(tag => string.Equals(eventTag, tag, StringComparison.OrdinalIgnoreCase)));
}
