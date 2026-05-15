using System.Collections.Generic;
using Godot_V2.Scripts.Gameplay;

namespace Godot_V2.Scripts.World;

public sealed record RouteSelectionResult(
    bool Applied,
    string Message,
    RouteChoice Choice);

public static class RouteSelectionResolver
{
    public static IReadOnlyList<RouteChoice> GenerateChoices(TripState state, int seed) =>
        RouteGraph.GenerateChoices(state.CurrentLocation, seed);

    public static RouteSelectionResult ApplyChoice(TripState state, RouteChoice choice)
    {
        state.SetCurrentLocation(choice.Destination.Name);
        state.SetTargetDistance(choice.DistanceKm);
        state.SetWeather(choice.ExpectedWeather);

        return new RouteSelectionResult(
            true,
            $"已选择前往 {choice.Destination.Name}，下一段 {choice.DistanceKm:0} km。",
            choice);
    }
}
