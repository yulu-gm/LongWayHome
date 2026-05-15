using Godot_V2.Scripts.Events;
using Godot_V2.Scripts.Gameplay;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class RoadEventCatalogTests
{
    private static readonly string[] ExpectedEventIds =
    [
        "broken-car",
        "hitchhiker",
        "roadside-vendor",
        "temporary-construction",
        "rain-night-detour",
        "fuel-price-rumor",
        "abandoned-town-rumor",
        "tire-noise",
        "lights-flicker",
        "late-night-radio",
        "motel-stranger",
        "old-tape"
    ];

    [Test]
    public void MvpCatalogContainsAllRequiredEventsWithUniqueIds()
    {
        var events = RoadEventCatalog.CreateMvpEvents();
        var distinctIds = events.Select(roadEvent => roadEvent.Id).Distinct().ToArray();

        Assert.That(events.Select(roadEvent => roadEvent.Id), Is.EquivalentTo(ExpectedEventIds));
        Assert.That(distinctIds, Has.Length.EqualTo(ExpectedEventIds.Length));
    }

    [Test]
    public void EveryMvpEventHasAtLeastTwoResultChoices()
    {
        var events = RoadEventCatalog.CreateMvpEvents();

        foreach (var roadEvent in events)
        {
            Assert.That(roadEvent.Title, Is.Not.Empty, roadEvent.Id);
            Assert.That(roadEvent.SceneText, Is.Not.Empty, roadEvent.Id);
            Assert.That(roadEvent.Weight, Is.GreaterThan(0), roadEvent.Id);
            Assert.That(roadEvent.Choices, Has.Count.GreaterThanOrEqualTo(2), roadEvent.Id);
            Assert.That(roadEvent.Choices.All(choice => choice.Results.Count > 0), Is.True, roadEvent.Id);
        }
    }

    [Test]
    public void LowFuelAndRainNightEventsCanBecomeEligible()
    {
        var state = TripState.CreateNew();
        state.SetFuel(5f);
        state.SetWeather(TripWeather.Rain);
        state.SetClockMinutes(23 * 60);
        var context = new RoadEventContext(
            RouteTags: ["road", "fuel", "rain", "night"],
            LegIndex: 5,
            Cooldowns: new Dictionary<string, int>(),
            CompletedOneTimeEventIds: new HashSet<string>());

        var eligibleIds = RoadEventSelector
            .GetEligibleEvents(RoadEventCatalog.CreateMvpEvents(), state, context)
            .Select(roadEvent => roadEvent.Id)
            .ToArray();

        Assert.That(eligibleIds, Does.Contain("fuel-price-rumor"));
        Assert.That(eligibleIds, Does.Contain("rain-night-detour"));
        Assert.That(eligibleIds, Does.Contain("lights-flicker"));
    }
}
