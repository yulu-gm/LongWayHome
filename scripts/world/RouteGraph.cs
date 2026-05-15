using System;
using System.Collections.Generic;
using System.Linq;
using Godot_V2.Scripts.Gameplay;

namespace Godot_V2.Scripts.World;

public enum RouteRisk
{
    Low,
    Medium,
    High
}

public enum PlaceService
{
    Fuel,
    Motel,
    Repair,
    Shop,
    Rest
}

public sealed record RouteNode(
    string Id,
    string Name,
    IReadOnlyList<PlaceService> Services,
    IReadOnlyList<string> EventTags);

public sealed record RouteChoice(
    string Id,
    RouteNode Destination,
    float DistanceKm,
    RoadType RoadType,
    RouteRisk Risk,
    TripWeather ExpectedWeather,
    IReadOnlyList<PlaceService> Services,
    IReadOnlyList<string> EventTags,
    IReadOnlyList<string> RoadSegmentIds);

public static class RouteGraph
{
    private static readonly RouteTemplate[] Templates =
    [
        new(
            DestinationId: "pine-rest",
            DestinationName: "松林休息区",
            DistanceMinKm: 42,
            DistanceMaxKm: 68,
            RoadType: RoadType.RuralRoad,
            Risk: RouteRisk.Low,
            WeatherOptions: [TripWeather.Clear, TripWeather.Fog],
            Services: [PlaceService.Rest, PlaceService.Shop],
            EventTags: ["forest", "rest", "traveler"],
            RoadSegmentIds: ["rural_straight", "gentle_curve"]),
        new(
            DestinationId: "old-mill-gas",
            DestinationName: "旧磨坊加油站",
            DistanceMinKm: 58,
            DistanceMaxKm: 92,
            RoadType: RoadType.Highway,
            Risk: RouteRisk.Medium,
            WeatherOptions: [TripWeather.Clear, TripWeather.Rain],
            Services: [PlaceService.Fuel, PlaceService.Shop, PlaceService.Repair],
            EventTags: ["fuel", "highway", "rumor"],
            RoadSegmentIds: ["rural_straight", "gentle_curve", "town_entry"]),
        new(
            DestinationId: "ridge-motel",
            DestinationName: "山脊汽车旅馆",
            DistanceMinKm: 70,
            DistanceMaxKm: 118,
            RoadType: RoadType.MountainRoad,
            Risk: RouteRisk.High,
            WeatherOptions: [TripWeather.Fog, TripWeather.Rain],
            Services: [PlaceService.Motel, PlaceService.Rest, PlaceService.Repair],
            EventTags: ["mountain", "motel", "weather"],
            RoadSegmentIds: ["gentle_curve", "s_curve"]),
        new(
            DestinationId: "county-old-road",
            DestinationName: "县道旧路岔口",
            DistanceMinKm: 64,
            DistanceMaxKm: 105,
            RoadType: RoadType.OldRoad,
            Risk: RouteRisk.High,
            WeatherOptions: [TripWeather.Clear, TripWeather.Fog, TripWeather.Rain],
            Services: [PlaceService.Rest],
            EventTags: ["old-road", "abandoned", "detour"],
            RoadSegmentIds: ["old_road_detour", "s_curve"]),
        new(
            DestinationId: "harbor-town-edge",
            DestinationName: "港湾镇外",
            DistanceMinKm: 52,
            DistanceMaxKm: 88,
            RoadType: RoadType.TownRoad,
            Risk: RouteRisk.Medium,
            WeatherOptions: [TripWeather.Clear, TripWeather.Rain],
            Services: [PlaceService.Fuel, PlaceService.Motel, PlaceService.Shop],
            EventTags: ["town", "service", "traffic"],
            RoadSegmentIds: ["rural_straight", "town_entry"])
    ];

    public static IReadOnlyList<RouteChoice> GenerateChoices(string currentNodeId, int seed)
    {
        var normalizedNode = string.IsNullOrWhiteSpace(currentNodeId) ? "start" : currentNodeId.Trim();
        var random = new Random(HashCode.Combine(normalizedNode, seed));
        var count = 2 + random.Next(0, 2);
        var selectedTemplates = Templates
            .OrderBy(_ => random.Next())
            .Take(count)
            .ToList();

        EnsureTemplate(
            selectedTemplates,
            Templates.First(template => template.Services.Contains(PlaceService.Fuel)),
            template => template.Services.Contains(PlaceService.Fuel),
            template => template.RoadType is not RoadType.MountainRoad and not RoadType.OldRoad);
        EnsureTemplate(
            selectedTemplates,
            Templates.First(template => template.RoadType == RoadType.MountainRoad),
            template => template.RoadType is RoadType.MountainRoad or RoadType.OldRoad,
            template => !template.Services.Contains(PlaceService.Fuel));

        return selectedTemplates
            .Select((template, index) => CreateChoice(template, normalizedNode, seed, random, index))
            .ToArray();
    }

    private static RouteChoice CreateChoice(
        RouteTemplate template,
        string currentNodeId,
        int seed,
        Random random,
        int index)
    {
        var distance = template.DistanceMinKm + random.NextSingle() * (template.DistanceMaxKm - template.DistanceMinKm);
        var weather = template.WeatherOptions[random.Next(template.WeatherOptions.Length)];
        var destination = new RouteNode(
            template.DestinationId,
            template.DestinationName,
            template.Services,
            template.EventTags);

        return new RouteChoice(
            $"{currentNodeId}-{template.DestinationId}-{seed}-{index}",
            destination,
            MathF.Round(distance),
            template.RoadType,
            template.Risk,
            weather,
            template.Services,
            template.EventTags,
            template.RoadSegmentIds);
    }

    private static void EnsureTemplate(
        List<RouteTemplate> selectedTemplates,
        RouteTemplate requiredTemplate,
        Func<RouteTemplate, bool> requirement,
        Func<RouteTemplate, bool> replacementPreference)
    {
        if (selectedTemplates.Any(requirement))
        {
            return;
        }

        var replacementIndex = selectedTemplates.FindIndex(template =>
            replacementPreference(template) && !requirement(template));
        if (replacementIndex < 0)
        {
            replacementIndex = selectedTemplates.FindIndex(template => !requirement(template));
        }

        selectedTemplates[replacementIndex < 0 ? 0 : replacementIndex] = requiredTemplate;
    }

    private sealed record RouteTemplate(
        string DestinationId,
        string DestinationName,
        float DistanceMinKm,
        float DistanceMaxKm,
        RoadType RoadType,
        RouteRisk Risk,
        TripWeather[] WeatherOptions,
        PlaceService[] Services,
        string[] EventTags,
        string[] RoadSegmentIds);
}
