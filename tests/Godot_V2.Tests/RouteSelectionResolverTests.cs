using Godot_V2.Scripts.Gameplay;
using Godot_V2.Scripts.World;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class RouteSelectionResolverTests
{
    [Test]
    public void GeneratesRouteChoicesForCurrentTripLocation()
    {
        var state = TripState.CreateNew();

        var choices = RouteSelectionResolver.GenerateChoices(state, seed: 23);

        Assert.That(choices, Has.Count.InRange(2, 3));
        Assert.That(choices.All(choice => choice.DistanceKm > 0f), Is.True);
        Assert.That(choices.All(choice => choice.Destination.Name.Length > 0), Is.True);
        Assert.That(choices.SelectMany(choice => choice.Services), Does.Contain(PlaceService.Fuel));
    }

    [Test]
    public void ApplyingRouteChoicePreparesNextDrivingLeg()
    {
        var state = TripState.CreateNew();
        var choice = RouteSelectionResolver.GenerateChoices(state, seed: 23).First();

        var result = RouteSelectionResolver.ApplyChoice(state, choice);

        Assert.That(result.Applied, Is.True);
        Assert.That(result.Message, Does.Contain(choice.Destination.Name));
        Assert.That(state.CurrentLocation, Is.EqualTo(choice.Destination.Name));
        Assert.That(state.TargetDistanceKm, Is.EqualTo(choice.DistanceKm));
        Assert.That(state.Weather, Is.EqualTo(choice.ExpectedWeather));
    }
}
