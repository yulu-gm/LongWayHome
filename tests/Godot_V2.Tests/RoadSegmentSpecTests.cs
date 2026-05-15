using Godot_V2.Scripts.Gameplay;
using Godot_V2.Scripts.World;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class RoadSegmentSpecTests
{
    [Test]
    public void CatalogContainsMvpRoadSegments()
    {
        var segments = RoadSegmentCatalog.CreateDefaults();

        Assert.That(segments.Select(segment => segment.Id), Does.Contain("rural_straight"));
        Assert.That(segments.Select(segment => segment.Id), Does.Contain("gentle_curve"));
        Assert.That(segments.Select(segment => segment.Id), Does.Contain("s_curve"));
        Assert.That(segments.Select(segment => segment.Id), Does.Contain("town_entry"));
    }

    [Test]
    public void SegmentSpecsExposeDrivingAndVisualTags()
    {
        var segment = RoadSegmentCatalog.CreateDefaults().Single(segment => segment.Id == "s_curve");

        Assert.That(segment.LengthMeters, Is.GreaterThan(40f));
        Assert.That(segment.CurveStrength, Is.GreaterThan(0.4f));
        Assert.That(segment.Surface, Is.EqualTo(RoadSurface.Asphalt));
        Assert.That(segment.Region, Is.EqualTo(RegionType.Forest));
        Assert.That(segment.VisualTags, Does.Contain("trees"));
        Assert.That(segment.EventTags, Does.Contain("curve"));
    }

    [Test]
    public void RouteChoicesReferenceKnownRoadSegments()
    {
        var catalogIds = RoadSegmentCatalog.CreateDefaults().Select(segment => segment.Id).ToHashSet();
        var routes = RouteGraph.GenerateChoices("旧木镇外", seed: 23);

        foreach (var route in routes)
        {
            Assert.That(route.RoadSegmentIds, Is.Not.Empty);
            Assert.That(route.RoadSegmentIds.All(catalogIds.Contains), Is.True);
        }
    }
}
