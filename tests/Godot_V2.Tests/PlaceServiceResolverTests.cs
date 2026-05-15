using Godot_V2.Scripts.Gameplay;
using Godot_V2.Scripts.Places;
using Godot_V2.Scripts.World;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class PlaceServiceResolverTests
{
    [Test]
    public void FuelServicePreviewShowsFullTankChangeAndCost()
    {
        var state = TripState.CreateNew();
        state.SetFuel(30f);
        state.SetMoney(100);
        var place = PlaceCatalog.CreateDefaults().Single(place => place.Type == PlaceType.FuelStation);

        var preview = PlaceServiceResolver.CreatePreview(state, place, PlaceService.Fuel);

        Assert.That(preview.CanExecute, Is.True);
        Assert.That(preview.Price, Is.EqualTo(60));
        Assert.That(preview.MoneyAfter, Is.EqualTo(40));
        Assert.That(preview.FuelAfterLiters, Is.EqualTo(60f));
        Assert.That(preview.Title, Does.Contain("加油"));
        Assert.That(preview.ChangePreview, Does.Contain("+30 L"));
    }

    [Test]
    public void InsufficientMoneyDisablesServiceAndDoesNotMutateTripState()
    {
        var state = TripState.CreateNew();
        state.SetFuel(10f);
        state.SetMoney(15);
        var place = PlaceCatalog.CreateDefaults().Single(place => place.Type == PlaceType.FuelStation);

        var preview = PlaceServiceResolver.CreatePreview(state, place, PlaceService.Fuel);
        var result = PlaceServiceResolver.Apply(state, place, PlaceService.Fuel);

        Assert.That(preview.CanExecute, Is.False);
        Assert.That(preview.BlockedReason, Does.Contain("金钱不足"));
        Assert.That(result.Applied, Is.False);
        Assert.That(result.Message, Does.Contain("金钱不足"));
        Assert.That(state.FuelLiters, Is.EqualTo(10f));
        Assert.That(state.Money, Is.EqualTo(15));
    }

    [Test]
    public void MotelServiceRestoresEnergyAndAdvancesClock()
    {
        var state = TripState.CreateNew();
        state.SetEnergy(24f);
        state.SetMoney(100);
        state.SetClockMinutes(21 * 60 + 30);
        var place = PlaceCatalog.CreateDefaults().Single(place => place.Type == PlaceType.Motel);

        var result = PlaceServiceResolver.Apply(state, place, PlaceService.Motel);

        Assert.That(result.Applied, Is.True);
        Assert.That(state.Energy, Is.EqualTo(100f));
        Assert.That(state.ClockMinutes, Is.EqualTo(5 * 60 + 30));
        Assert.That(state.Money, Is.LessThan(100));
        Assert.That(result.Message, Does.Contain("休息"));
    }

    [Test]
    public void RepairServiceImprovesWeakVehicleParts()
    {
        var state = TripState.CreateNew();
        state.SetMoney(300);
        state.SetVehiclePart(VehiclePart.Engine, 35f);
        state.SetVehiclePart(VehiclePart.Tires, 48f);
        state.SetVehiclePart(VehiclePart.Body, 90f);
        var place = PlaceCatalog.CreateDefaults().Single(place => place.Type == PlaceType.RepairShop);

        var result = PlaceServiceResolver.Apply(state, place, PlaceService.Repair);

        Assert.That(result.Applied, Is.True);
        Assert.That(state.GetVehiclePart(VehiclePart.Engine), Is.EqualTo(70f));
        Assert.That(state.GetVehiclePart(VehiclePart.Tires), Is.EqualTo(83f));
        Assert.That(state.GetVehiclePart(VehiclePart.Body), Is.EqualTo(100f));
        Assert.That(result.Message, Does.Contain("维修"));
    }

    [Test]
    public void ShopServiceBuysBasicSuppliesAsSmallEnergyRecovery()
    {
        var state = TripState.CreateNew();
        state.SetEnergy(55f);
        state.SetMoney(80);
        var place = PlaceCatalog.CreateDefaults().Single(place => place.Type == PlaceType.FuelStation);

        var result = PlaceServiceResolver.Apply(state, place, PlaceService.Shop);

        Assert.That(result.Applied, Is.True);
        Assert.That(state.Energy, Is.EqualTo(67f));
        Assert.That(state.Money, Is.EqualTo(68));
        Assert.That(result.Message, Does.Contain("补给"));
    }
}
