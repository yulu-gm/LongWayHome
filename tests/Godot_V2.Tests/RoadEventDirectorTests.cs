using Godot_V2.Scripts.Events;
using Godot_V2.Scripts.Gameplay;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class RoadEventDirectorTests
{
    [Test]
    public void LowFuelIncreasesFuelEventWeight()
    {
        var state = TripState.CreateNew();
        state.SetFuel(5f);
        var context = CreateContext(["road", "fuel"]);
        var fuelEvent = CreateEvent("fuel-tip", ["road", "fuel"], weight: 4);
        var travelerEvent = CreateEvent("traveler", ["road", "traveler"], weight: 4);

        var candidates = RoadEventDirector.GetWeightedCandidates(
            [fuelEvent, travelerEvent],
            state,
            context);

        Assert.That(
            candidates.Single(candidate => candidate.Event.Id == fuelEvent.Id).AdjustedWeight,
            Is.GreaterThan(candidates.Single(candidate => candidate.Event.Id == travelerEvent.Id).AdjustedWeight));
    }

    [Test]
    public void RainyNightIncreasesVisibilityAndVehicleRiskWeights()
    {
        var state = TripState.CreateNew();
        state.SetWeather(TripWeather.Rain);
        state.SetClockMinutes(23 * 60);
        var context = CreateContext(["road", "rain", "night"]);
        var visibilityEvent = CreateEvent("headlights-flicker", ["road", "visibility"], weight: 5);
        var vehicleEvent = CreateEvent("tires-slip", ["road", "vehicle"], weight: 5);
        var scenicEvent = CreateEvent("scenic-overlook", ["road", "scenic"], weight: 5);

        var candidates = RoadEventDirector.GetWeightedCandidates(
            [visibilityEvent, vehicleEvent, scenicEvent],
            state,
            context);

        var scenicWeight = candidates.Single(candidate => candidate.Event.Id == scenicEvent.Id).AdjustedWeight;
        Assert.That(candidates.Single(candidate => candidate.Event.Id == visibilityEvent.Id).AdjustedWeight, Is.GreaterThan(scenicWeight));
        Assert.That(candidates.Single(candidate => candidate.Event.Id == vehicleEvent.Id).AdjustedWeight, Is.GreaterThan(scenicWeight));
    }

    [Test]
    public void SelectEventReturnsNoTriggerWhenNothingIsEligible()
    {
        var state = TripState.CreateNew();
        var context = CreateContext(["forest"]);
        var desertOnlyEvent = CreateEvent(
            "desert-stall",
            ["road", "desert"],
            trigger: new RoadEventTrigger(RequiredTags: ["desert"]));

        var result = RoadEventDirector.SelectEvent([desertOnlyEvent], state, context, seed: 7);

        Assert.That(result.Triggered, Is.False);
        Assert.That(result.Event, Is.Null);
        Assert.That(result.Candidates, Is.Empty);
    }

    [Test]
    public void SelectEventIsDeterministicForSameSeedAndContext()
    {
        var state = TripState.CreateNew();
        var context = CreateContext(["road", "fuel"]);
        var fuelEvent = CreateEvent("fuel-tip", ["road", "fuel"], weight: 5);
        var travelerEvent = CreateEvent("traveler", ["road", "traveler"], weight: 5);

        var first = RoadEventDirector.SelectEvent([fuelEvent, travelerEvent], state, context, seed: 13);
        var second = RoadEventDirector.SelectEvent([fuelEvent, travelerEvent], state, context, seed: 13);

        Assert.That(first.Triggered, Is.True);
        Assert.That(second.Triggered, Is.True);
        Assert.That(first.Event!.Id, Is.EqualTo(second.Event!.Id));
    }

    private static RoadEventContext CreateContext(IReadOnlyList<string> tags) =>
        new(
            RouteTags: tags,
            LegIndex: 2,
            Cooldowns: new Dictionary<string, int>(),
            CompletedOneTimeEventIds: new HashSet<string>());

    private static RoadEventDefinition CreateEvent(
        string id,
        IReadOnlyList<string> tags,
        int weight = 3,
        RoadEventTrigger? trigger = null) =>
        new(
            Id: id,
            Title: id,
            SceneText: "A road event.",
            Category: RoadEventCategory.Road,
            Weight: weight,
            CooldownLegs: 1,
            IsOneTime: false,
            Tags: tags,
            Trigger: trigger ?? RoadEventTrigger.Any,
            Choices:
            [
                new RoadEventChoice(
                    "accept",
                    "接受",
                    [new RoadEventEffect(RoadEventEffectKind.Flag, 1, id)])
            ]);
}
