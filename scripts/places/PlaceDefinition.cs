using System;
using System.Collections.Generic;
using System.Linq;
using Godot_V2.Scripts.World;

namespace Godot_V2.Scripts.Places;

public enum PlaceType
{
    FuelStation,
    Motel,
    RepairShop,
    RestArea,
    RoadsideShop,
    Town
}

public sealed record PlaceServiceOffer(
    PlaceService Service,
    int BasePrice,
    float PriceMultiplier);

public sealed record PlaceDefinition(
    string Id,
    string Name,
    PlaceType Type,
    IReadOnlyList<PlaceServiceOffer> Services,
    IReadOnlyList<string> EventTags)
{
    public bool HasService(PlaceService service) =>
        Services.Any(offer => offer.Service == service);

    public PlaceServiceOffer GetServiceOffer(PlaceService service) =>
        Services.FirstOrDefault(offer => offer.Service == service)
        ?? throw new InvalidOperationException($"{Name} does not provide service: {service}");
}

public static class PlaceCatalog
{
    public static IReadOnlyList<PlaceDefinition> CreateDefaults() =>
    [
        new PlaceDefinition(
            "old-mill-gas",
            "旧磨坊加油站",
            PlaceType.FuelStation,
            CreateOffers([PlaceService.Fuel, PlaceService.Shop, PlaceService.Repair], 1.0f),
            ["fuel", "shop", "repair", "rumor"]),
        new PlaceDefinition(
            "ridge-motel",
            "山脊汽车旅馆",
            PlaceType.Motel,
            CreateOffers([PlaceService.Motel, PlaceService.Rest, PlaceService.Repair], 1.08f),
            ["motel", "rest", "weather", "traveler"]),
        new PlaceDefinition(
            "county-repair",
            "县道修理铺",
            PlaceType.RepairShop,
            CreateOffers([PlaceService.Repair, PlaceService.Shop, PlaceService.Rest], 1.04f),
            ["repair", "old-road", "tools"]),
        new PlaceDefinition(
            "pine-rest",
            "松林休息区",
            PlaceType.RestArea,
            CreateOffers([PlaceService.Rest, PlaceService.Shop], 0.92f),
            ["rest", "forest", "traveler"]),
        new PlaceDefinition(
            "harbor-town-edge",
            "港湾镇外",
            PlaceType.Town,
            CreateOffers([PlaceService.Fuel, PlaceService.Motel, PlaceService.Shop], 1.12f),
            ["town", "service", "traffic"])
    ];

    public static PlaceDefinition CreateForRouteDestination(RouteChoice route)
    {
        var type = InferType(route.Services);
        var priceMultiplier = route.Risk switch
        {
            RouteRisk.Low => 1.0f,
            RouteRisk.Medium => 1.1f,
            RouteRisk.High => 1.22f,
            _ => 1.0f
        };
        var eventTags = route.EventTags
            .Concat(route.Services.Select(service => service.ToString().ToLowerInvariant()))
            .Append("route-destination")
            .Append(type.ToString().ToLowerInvariant())
            .Distinct()
            .ToArray();

        return new PlaceDefinition(
            route.Destination.Id,
            route.Destination.Name,
            type,
            CreateOffers(route.Services, priceMultiplier),
            eventTags);
    }

    private static PlaceType InferType(IReadOnlyList<PlaceService> services)
    {
        if (services.Contains(PlaceService.Motel))
        {
            return PlaceType.Motel;
        }

        if (services.Contains(PlaceService.Fuel))
        {
            return PlaceType.FuelStation;
        }

        if (services.Contains(PlaceService.Repair))
        {
            return PlaceType.RepairShop;
        }

        if (services.Contains(PlaceService.Shop))
        {
            return PlaceType.RoadsideShop;
        }

        return PlaceType.RestArea;
    }

    private static IReadOnlyList<PlaceServiceOffer> CreateOffers(
        IReadOnlyList<PlaceService> services,
        float priceMultiplier) =>
        services
            .Distinct()
            .Select(service => new PlaceServiceOffer(
                service,
                GetBasePrice(service),
                priceMultiplier))
            .ToArray();

    private static int GetBasePrice(PlaceService service) =>
        service switch
        {
            PlaceService.Fuel => 2,
            PlaceService.Motel => 35,
            PlaceService.Repair => 80,
            PlaceService.Shop => 12,
            PlaceService.Rest => 0,
            _ => 0
        };
}
