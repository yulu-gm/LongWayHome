using System;
using System.Linq;
using Godot_V2.Scripts.Gameplay;
using Godot_V2.Scripts.World;

namespace Godot_V2.Scripts.Places;

public sealed record PlaceServicePreview(
    PlaceService Service,
    string Title,
    string Description,
    int Price,
    int MoneyAfter,
    float FuelAfterLiters,
    float EnergyAfter,
    float ConditionAfter,
    bool CanExecute,
    string BlockedReason,
    string ChangePreview);

public sealed record PlaceServiceApplyResult(
    bool Applied,
    string Message,
    PlaceServicePreview Preview);

public static class PlaceServiceResolver
{
    private const float FuelEpsilon = 0.05f;
    private const float RepairAmount = 35f;
    private const float ShopEnergyRecovery = 12f;
    private const float FreeRestEnergyRecovery = 20f;

    public static PlaceServicePreview CreatePreview(
        TripState state,
        PlaceDefinition place,
        PlaceService service)
    {
        if (!place.HasService(service))
        {
            return CreateBlockedPreview(
                state,
                service,
                GetTitle(service),
                "此地点不提供该服务",
                "此地点不提供该服务");
        }

        var offer = place.GetServiceOffer(service);
        var price = GetPrice(state, offer);
        var fuelAfter = GetFuelAfter(state, service);
        var energyAfter = GetEnergyAfter(state, service);
        var conditionAfter = GetConditionAfter(state, service);
        var blockedReason = GetBlockedReason(state, service, price);
        var canExecute = blockedReason.Length == 0;

        return new PlaceServicePreview(
            service,
            GetTitle(service),
            GetDescription(service),
            price,
            Math.Max(0, state.Money - price),
            fuelAfter,
            energyAfter,
            conditionAfter,
            canExecute,
            blockedReason,
            canExecute ? GetChangePreview(state, service, price, fuelAfter, energyAfter, conditionAfter) : blockedReason);
    }

    public static PlaceServiceApplyResult Apply(
        TripState state,
        PlaceDefinition place,
        PlaceService service)
    {
        var preview = CreatePreview(state, place, service);
        if (!preview.CanExecute)
        {
            return new PlaceServiceApplyResult(false, preview.BlockedReason, preview);
        }

        state.SetMoney(preview.MoneyAfter);
        switch (service)
        {
            case PlaceService.Fuel:
                state.SetFuel(preview.FuelAfterLiters);
                break;
            case PlaceService.Motel:
                state.SetEnergy(preview.EnergyAfter);
                state.SetClockMinutes(state.ClockMinutes + 8 * 60);
                break;
            case PlaceService.Repair:
                foreach (var part in Enum.GetValues<VehiclePart>())
                {
                    state.SetVehiclePart(part, Math.Min(100f, state.GetVehiclePart(part) + RepairAmount));
                }
                break;
            case PlaceService.Shop:
                state.SetEnergy(preview.EnergyAfter);
                break;
            case PlaceService.Rest:
                state.SetEnergy(preview.EnergyAfter);
                state.SetClockMinutes(state.ClockMinutes + 45);
                break;
        }

        return new PlaceServiceApplyResult(true, GetAppliedMessage(service), preview);
    }

    private static PlaceServicePreview CreateBlockedPreview(
        TripState state,
        PlaceService service,
        string title,
        string description,
        string reason) =>
        new(
            service,
            title,
            description,
            0,
            state.Money,
            state.FuelLiters,
            state.Energy,
            state.AverageVehicleCondition,
            false,
            reason,
            reason);

    private static int GetPrice(TripState state, PlaceServiceOffer offer)
    {
        var basePrice = offer.Service == PlaceService.Fuel
            ? GetMissingFuelLiters(state) * offer.BasePrice
            : offer.BasePrice;
        return Math.Max(0, (int)MathF.Ceiling(basePrice * offer.PriceMultiplier));
    }

    private static string GetBlockedReason(TripState state, PlaceService service, int price)
    {
        if (price > state.Money)
        {
            return $"金钱不足：需要 ${price}";
        }

        return service switch
        {
            PlaceService.Fuel when GetMissingFuelLiters(state) <= FuelEpsilon => "油箱已满",
            PlaceService.Motel when state.Energy >= 99.5f => "精力已满",
            PlaceService.Repair when state.VehicleParts.Values.All(value => value >= 99.5f) => "车辆状态良好",
            PlaceService.Rest when state.Energy >= 99.5f => "精力已满",
            _ => string.Empty
        };
    }

    private static float GetFuelAfter(TripState state, PlaceService service) =>
        service == PlaceService.Fuel ? state.FuelCapacityLiters : state.FuelLiters;

    private static float GetEnergyAfter(TripState state, PlaceService service) =>
        service switch
        {
            PlaceService.Motel => 100f,
            PlaceService.Shop => Math.Min(100f, state.Energy + ShopEnergyRecovery),
            PlaceService.Rest => Math.Min(100f, state.Energy + FreeRestEnergyRecovery),
            _ => state.Energy
        };

    private static float GetConditionAfter(TripState state, PlaceService service)
    {
        if (service != PlaceService.Repair)
        {
            return state.AverageVehicleCondition;
        }

        return Enum.GetValues<VehiclePart>()
            .Select(part => Math.Min(100f, state.GetVehiclePart(part) + RepairAmount))
            .Average();
    }

    private static float GetMissingFuelLiters(TripState state) =>
        Math.Max(0f, state.FuelCapacityLiters - state.FuelLiters);

    private static string GetTitle(PlaceService service) => service switch
    {
        PlaceService.Fuel => "加油",
        PlaceService.Motel => "住宿",
        PlaceService.Repair => "维修车辆",
        PlaceService.Shop => "购买补给",
        PlaceService.Rest => "短暂休息",
        _ => "服务"
    };

    private static string GetDescription(PlaceService service) => service switch
    {
        PlaceService.Fuel => "补满油箱，继续下一段路。",
        PlaceService.Motel => "住一晚，恢复全部精力。",
        PlaceService.Repair => "修复主要部件，降低下一段风险。",
        PlaceService.Shop => "购买咖啡、水和零食等基础补给。",
        PlaceService.Rest => "停车伸展，免费恢复少量精力。",
        _ => "旅途服务"
    };

    private static string GetChangePreview(
        TripState state,
        PlaceService service,
        int price,
        float fuelAfter,
        float energyAfter,
        float conditionAfter) =>
        service switch
        {
            PlaceService.Fuel => $"油量 +{fuelAfter - state.FuelLiters:0} L / ${price}",
            PlaceService.Motel => $"精力 {state.Energy:0}% -> {energyAfter:0}% / ${price}",
            PlaceService.Repair => $"车况 {state.AverageVehicleCondition:0}% -> {conditionAfter:0}% / ${price}",
            PlaceService.Shop => $"补给 + 精力 +{energyAfter - state.Energy:0}% / ${price}",
            PlaceService.Rest => $"精力 +{energyAfter - state.Energy:0}% / 免费",
            _ => $"费用 ${price}"
        };

    private static string GetAppliedMessage(PlaceService service) => service switch
    {
        PlaceService.Fuel => "已加满油箱。",
        PlaceService.Motel => "已休息一晚，精力恢复。",
        PlaceService.Repair => "已完成车辆维修。",
        PlaceService.Shop => "已购买基础补给。",
        PlaceService.Rest => "已短暂休息。",
        _ => "服务已完成。"
    };
}
