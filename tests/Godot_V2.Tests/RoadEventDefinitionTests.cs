using Godot_V2.Scripts.Events;
using Godot_V2.Scripts.Gameplay;
using Godot_V2.Scripts.World;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class RoadEventDefinitionTests
{
    [Test]
    public void FiltersEventsByTagsWeatherAndResourceConditions()
    {
        var state = TripState.CreateNew();
        state.SetFuel(7f);
        state.SetWeather(TripWeather.Rain);
        var context = new RoadEventContext(
            RouteTags: ["road", "fuel", "rain"],
            LegIndex: 4,
            Cooldowns: new Dictionary<string, int>(),
            CompletedOneTimeEventIds: new HashSet<string>());
        var lowFuelRainEvent = CreateEvent(
            "low-fuel-rain",
            weight: 5,
            trigger: new RoadEventTrigger(
                RequiredTags: ["fuel", "rain"],
                RequiredWeather: TripWeather.Rain,
                MaxFuelRatio: 0.2f));
        var clearWeatherEvent = CreateEvent(
            "clear-weather",
            weight: 5,
            trigger: new RoadEventTrigger(RequiredWeather: TripWeather.Clear));
        var missingTagEvent = CreateEvent(
            "missing-tag",
            weight: 5,
            trigger: new RoadEventTrigger(RequiredTags: ["mountain"]));

        var eligible = RoadEventSelector.GetEligibleEvents(
            [lowFuelRainEvent, clearWeatherEvent, missingTagEvent],
            state,
            context);

        Assert.That(eligible, Is.EqualTo(new[] { lowFuelRainEvent }));
    }

    [Test]
    public void CooldownAndCompletedOneTimeEventsAreExcluded()
    {
        var state = TripState.CreateNew();
        var cooldownEvent = CreateEvent("cooling-down", cooldownLegs: 2);
        var completedOneTime = CreateEvent("completed", isOneTime: true);
        var available = CreateEvent("available");
        var context = new RoadEventContext(
            RouteTags: ["road"],
            LegIndex: 3,
            Cooldowns: new Dictionary<string, int>
            {
                [cooldownEvent.Id] = 5
            },
            CompletedOneTimeEventIds: new HashSet<string> { completedOneTime.Id });

        var eligible = RoadEventSelector.GetEligibleEvents(
            [cooldownEvent, completedOneTime, available],
            state,
            context);

        Assert.That(eligible, Is.EqualTo(new[] { available }));
    }

    [Test]
    public void WeightedSelectionIsDeterministicAndIgnoresZeroWeightEvents()
    {
        var zeroWeight = CreateEvent("zero", weight: 0);
        var common = CreateEvent("common", weight: 10);
        var rare = CreateEvent("rare", weight: 1);

        var first = RoadEventSelector.SelectWeighted([zeroWeight, common, rare], seed: 42);
        var second = RoadEventSelector.SelectWeighted([zeroWeight, common, rare], seed: 42);

        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Not.Null);
        Assert.That(first!.Id, Is.EqualTo(second!.Id));
        Assert.That(first.Id, Is.Not.EqualTo(zeroWeight.Id));
    }

    [Test]
    public void EventDefinitionRequiresChoicesWithResults()
    {
        var roadEvent = CreateEvent("broken-car");

        Assert.That(roadEvent.Choices, Has.Count.EqualTo(2));
        Assert.That(roadEvent.Choices.All(choice => choice.Results.Count > 0), Is.True);
        Assert.That(roadEvent.Tags, Does.Contain("road"));
    }

    private static RoadEventDefinition CreateEvent(
        string id,
        int weight = 3,
        int cooldownLegs = 1,
        bool isOneTime = false,
        RoadEventTrigger? trigger = null) =>
        new(
            Id: id,
            Title: $"Event {id}",
            SceneText: "A small road-trip moment.",
            Category: RoadEventCategory.Road,
            Weight: weight,
            CooldownLegs: cooldownLegs,
            IsOneTime: isOneTime,
            Tags: ["road"],
            Trigger: trigger ?? RoadEventTrigger.Any,
            Choices:
            [
                new RoadEventChoice(
                    "help",
                    "帮一把",
                    [new RoadEventEffect(RoadEventEffectKind.Money, 20)]),
                new RoadEventChoice(
                    "leave",
                    "继续赶路",
                    [new RoadEventEffect(RoadEventEffectKind.TimeMinutes, -10)])
            ]);
}
