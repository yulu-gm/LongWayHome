using Godot_V2.Scripts.World;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class RoadSegmentSpawnPlannerTests
{
    [Test]
    public void PlanPreloadsConnectedSegmentsAheadOfPlayer()
    {
        var catalog = RoadSegmentCatalog.CreateDefaults();
        var routeSegmentIds = new[] { "rural_straight", "gentle_curve", "s_curve" };

        var placements = RoadSegmentSpawnPlanner.CreatePlan(
            catalog,
            routeSegmentIds,
            playerDistanceMeters: 20f,
            preloadAheadMeters: 300f,
            keepBehindMeters: 40f);

        Assert.That(placements, Is.Not.Empty);
        Assert.That(placements[0].StartDistanceMeters, Is.EqualTo(0f));
        Assert.That(placements[^1].EndDistanceMeters, Is.GreaterThanOrEqualTo(320f));

        for (var index = 1; index < placements.Count; index++)
        {
            Assert.That(
                placements[index].StartDistanceMeters,
                Is.EqualTo(placements[index - 1].EndDistanceMeters).Within(0.001f));
        }
    }

    [Test]
    public void PlanDropsOldSegmentsButKeepsSegmentUnderPlayer()
    {
        var catalog = RoadSegmentCatalog.CreateDefaults();
        var firstLength = catalog.Single(segment => segment.Id == "rural_straight").LengthMeters;
        var playerDistance = firstLength + 20f;

        var placements = RoadSegmentSpawnPlanner.CreatePlan(
            catalog,
            ["rural_straight", "gentle_curve"],
            playerDistance,
            preloadAheadMeters: 160f,
            keepBehindMeters: 8f);

        Assert.That(placements.Any(segment => segment.EndDistanceMeters < playerDistance - 8f), Is.False);
        Assert.That(
            placements.Any(segment =>
                segment.StartDistanceMeters <= playerDistance &&
                segment.EndDistanceMeters >= playerDistance),
            Is.True);
        Assert.That(placements[0].SegmentId, Is.EqualTo("gentle_curve"));
    }

    [Test]
    public void PlanRepeatsRouteSegmentsSoLongDrivesDoNotRunOutOfRoad()
    {
        var catalog = RoadSegmentCatalog.CreateDefaults();

        var placements = RoadSegmentSpawnPlanner.CreatePlan(
            catalog,
            ["rural_straight"],
            playerDistanceMeters: 500f,
            preloadAheadMeters: 240f,
            keepBehindMeters: 40f);

        Assert.That(placements, Has.Count.GreaterThanOrEqualTo(3));
        Assert.That(placements.All(segment => segment.SegmentId == "rural_straight"), Is.True);
        Assert.That(placements[^1].EndDistanceMeters, Is.GreaterThanOrEqualTo(740f));
        Assert.That(placements.Min(segment => segment.SequenceIndex), Is.GreaterThan(0));
    }

    [Test]
    public void UnknownRouteSegmentIdThrowsUsefulError()
    {
        var catalog = RoadSegmentCatalog.CreateDefaults();

        var error = Assert.Throws<ArgumentException>(() =>
            RoadSegmentSpawnPlanner.CreatePlan(
                catalog,
                ["missing-road"],
                playerDistanceMeters: 0f,
                preloadAheadMeters: 100f,
                keepBehindMeters: 20f));

        Assert.That(error!.Message, Does.Contain("missing-road"));
    }
}
