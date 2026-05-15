using Godot_V2.Scripts.Gameplay;
using Godot_V2.Scripts.World;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class RouteGraphTests
{
    [Test]
    public void SameSeedCreatesStableRouteChoices()
    {
        var first = RouteGraph.GenerateChoices("旧木镇外", seed: 42);
        var second = RouteGraph.GenerateChoices("旧木镇外", seed: 42);

        Assert.That(first, Has.Count.InRange(2, 3));
        Assert.That(second, Has.Count.EqualTo(first.Count));
        Assert.That(second.Select(route => route.Id), Is.EqualTo(first.Select(route => route.Id)));
        Assert.That(second.Select(route => route.DistanceKm), Is.EqualTo(first.Select(route => route.DistanceKm)));
    }

    [Test]
    public void RouteChoicesContainTripDecisionData()
    {
        var routes = RouteGraph.GenerateChoices("旧木镇外", seed: 7);

        foreach (var route in routes)
        {
            Assert.That(route.Destination.Name, Is.Not.Empty);
            Assert.That(route.DistanceKm, Is.GreaterThan(0f));
            Assert.That(route.Risk, Is.InRange(RouteRisk.Low, RouteRisk.High));
            Assert.That(Enum.IsDefined(route.ExpectedWeather), Is.True);
            Assert.That(route.Services, Is.Not.Empty);
            Assert.That(route.EventTags, Is.Not.Empty);
        }
    }

    [Test]
    public void DifferentRoadTypesExposeDifferentServices()
    {
        var routes = RouteGraph.GenerateChoices("旧木镇外", seed: 17);

        Assert.That(routes.Any(route => route.Services.Contains(PlaceService.Fuel)), Is.True);
        Assert.That(routes.Any(route => route.RoadType is RoadType.MountainRoad or RoadType.OldRoad), Is.True);
    }
}
