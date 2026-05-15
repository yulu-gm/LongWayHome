using System;
using System.Collections.Generic;
using System.Linq;

namespace Godot_V2.Scripts.World;

public sealed record RoadSegmentPlacement(
    int SequenceIndex,
    string SegmentId,
    float StartDistanceMeters,
    float EndDistanceMeters,
    RoadSegmentSpec Spec);

public static class RoadSegmentSpawnPlanner
{
    public static IReadOnlyList<RoadSegmentPlacement> CreatePlan(
        IReadOnlyList<RoadSegmentSpec> catalog,
        IReadOnlyList<string> routeSegmentIds,
        float playerDistanceMeters,
        float preloadAheadMeters,
        float keepBehindMeters)
    {
        if (catalog.Count == 0)
        {
            throw new ArgumentException("Road segment catalog cannot be empty.", nameof(catalog));
        }

        if (routeSegmentIds.Count == 0)
        {
            throw new ArgumentException("Route must contain at least one road segment id.", nameof(routeSegmentIds));
        }

        var byId = catalog.ToDictionary(segment => segment.Id);
        foreach (var segmentId in routeSegmentIds)
        {
            if (!byId.ContainsKey(segmentId))
            {
                throw new ArgumentException($"Unknown road segment id: {segmentId}", nameof(routeSegmentIds));
            }
        }

        var safePlayerDistance = MathF.Max(0f, playerDistanceMeters);
        var windowStart = MathF.Max(0f, safePlayerDistance - MathF.Max(0f, keepBehindMeters));
        var windowEnd = safePlayerDistance + MathF.Max(0f, preloadAheadMeters);
        var placements = new List<RoadSegmentPlacement>();
        var distanceCursor = 0f;
        var sequenceIndex = 0;

        while (distanceCursor <= windowEnd)
        {
            var segmentId = routeSegmentIds[sequenceIndex % routeSegmentIds.Count];
            var spec = byId[segmentId];
            var segmentStart = distanceCursor;
            var segmentEnd = segmentStart + spec.LengthMeters;

            if (segmentEnd >= windowStart && segmentStart <= windowEnd)
            {
                placements.Add(new RoadSegmentPlacement(
                    sequenceIndex,
                    segmentId,
                    segmentStart,
                    segmentEnd,
                    spec));
            }

            distanceCursor = segmentEnd;
            sequenceIndex++;
        }

        return placements;
    }
}
