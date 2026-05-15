using Godot_V2.Scripts.Places;
using Godot_V2.Scripts.World;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class PlaceDefinitionTests
{
    [Test]
    public void CatalogContainsMvpPlaceTypesWithServices()
    {
        var places = PlaceCatalog.CreateDefaults();

        Assert.That(places.Select(place => place.Type), Does.Contain(PlaceType.FuelStation));
        Assert.That(places.Select(place => place.Type), Does.Contain(PlaceType.Motel));
        Assert.That(places.Select(place => place.Type), Does.Contain(PlaceType.RepairShop));
        Assert.That(places.Select(place => place.Type), Does.Contain(PlaceType.RestArea));

        foreach (var place in places)
        {
            Assert.That(place.Services, Is.Not.Empty);
            Assert.That(place.EventTags, Is.Not.Empty);
            Assert.That(place.Services.All(service => service.BasePrice >= 0), Is.True);
            Assert.That(place.Services.All(service => service.PriceMultiplier > 0f), Is.True);
        }
    }

    [Test]
    public void PlaceDefinitionCanQueryServiceOffers()
    {
        var fuelStation = PlaceCatalog.CreateDefaults().Single(place => place.Type == PlaceType.FuelStation);

        Assert.That(fuelStation.HasService(PlaceService.Fuel), Is.True);
        Assert.That(fuelStation.HasService(PlaceService.Motel), Is.False);
        Assert.That(fuelStation.GetServiceOffer(PlaceService.Fuel).BasePrice, Is.EqualTo(2));
    }

    [Test]
    public void RouteDestinationCreatesMatchingPlaceDefinition()
    {
        var route = RouteGraph.GenerateChoices("旧木镇外", seed: 17)
            .First(choice => choice.Services.Contains(PlaceService.Fuel));

        var place = PlaceCatalog.CreateForRouteDestination(route);

        Assert.That(place.Id, Is.EqualTo(route.Destination.Id));
        Assert.That(place.Name, Is.EqualTo(route.Destination.Name));
        Assert.That(route.Services.All(place.HasService), Is.True);
        Assert.That(place.EventTags, Does.Contain("route-destination"));
        Assert.That(place.EventTags.Intersect(route.EventTags), Is.Not.Empty);
    }

    [Test]
    public void HigherRiskRoutesIncreaseServicePrices()
    {
        var lowRiskRoute = FindRouteWithRisk(RouteRisk.Low);
        var highRiskRoute = FindRouteWithRisk(RouteRisk.High);

        var lowRiskPlace = PlaceCatalog.CreateForRouteDestination(lowRiskRoute);
        var highRiskPlace = PlaceCatalog.CreateForRouteDestination(highRiskRoute);

        Assert.That(
            highRiskPlace.Services.Min(service => service.PriceMultiplier),
            Is.GreaterThan(lowRiskPlace.Services.Min(service => service.PriceMultiplier)));
    }

    private static RouteChoice FindRouteWithRisk(RouteRisk risk)
    {
        for (var seed = 0; seed < 64; seed++)
        {
            var route = RouteGraph.GenerateChoices("旧木镇外", seed)
                .FirstOrDefault(choice => choice.Risk == risk);
            if (route is not null)
            {
                return route;
            }
        }

        Assert.Fail($"Could not find a route with risk: {risk}");
        throw new InvalidOperationException($"Could not find a route with risk: {risk}");
    }
}
