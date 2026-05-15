using System.Collections.Generic;
using Godot_V2.Scripts.Gameplay;

namespace Godot_V2.Scripts.World;

public enum RoadSurface
{
    Asphalt,
    WetAsphalt,
    Gravel,
    Dirt
}

public enum RegionType
{
    Forest,
    Mountain,
    Town,
    Desert,
    Coast
}

public sealed record RoadSegmentSpec(
    string Id,
    RoadType RoadType,
    float LengthMeters,
    float CurveStrength,
    RoadSurface Surface,
    RegionType Region,
    IReadOnlyList<string> VisualTags,
    IReadOnlyList<string> EventTags);

public static class RoadSegmentCatalog
{
    public static IReadOnlyList<RoadSegmentSpec> CreateDefaults() =>
    [
        new RoadSegmentSpec(
            "rural_straight",
            RoadType.RuralRoad,
            LengthMeters: 140f,
            CurveStrength: 0.05f,
            RoadSurface.Asphalt,
            RegionType.Forest,
            VisualTags: ["trees", "rocks", "shoulder", "utility-poles"],
            EventTags: ["cruise", "traveler"]),
        new RoadSegmentSpec(
            "gentle_curve",
            RoadType.RuralRoad,
            LengthMeters: 110f,
            CurveStrength: 0.35f,
            RoadSurface.Asphalt,
            RegionType.Forest,
            VisualTags: ["trees", "guardrail", "river-edge"],
            EventTags: ["curve", "scenery"]),
        new RoadSegmentSpec(
            "s_curve",
            RoadType.MountainRoad,
            LengthMeters: 120f,
            CurveStrength: 0.72f,
            RoadSurface.Asphalt,
            RegionType.Forest,
            VisualTags: ["trees", "rocks", "guardrail", "slope"],
            EventTags: ["curve", "weather", "low-grip"]),
        new RoadSegmentSpec(
            "town_entry",
            RoadType.TownRoad,
            LengthMeters: 90f,
            CurveStrength: 0.2f,
            RoadSurface.Asphalt,
            RegionType.Town,
            VisualTags: ["crosswalk", "signs", "shopfronts", "streetlights"],
            EventTags: ["town", "service", "traffic"]),
        new RoadSegmentSpec(
            "old_road_detour",
            RoadType.OldRoad,
            LengthMeters: 130f,
            CurveStrength: 0.46f,
            RoadSurface.Gravel,
            RegionType.Forest,
            VisualTags: ["broken-asphalt", "abandoned-sign", "shrubs"],
            EventTags: ["old-road", "abandoned", "detour"])
    ];
}
