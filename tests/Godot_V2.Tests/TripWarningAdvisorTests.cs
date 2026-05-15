using Godot_V2.Scripts.Gameplay;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class TripWarningAdvisorTests
{
    [Test]
    public void LowFuelProducesFuelWarning()
    {
        var state = TripState.CreateNew();
        state.SetFuel(6f);

        var warning = TripWarningAdvisor.GetPrimaryWarning(state);

        Assert.That(warning.Kind, Is.EqualTo(TripWarningKind.LowFuel));
        Assert.That(warning.Message, Does.Contain("油量"));
    }

    [Test]
    public void RainWithWornTiresProducesRainTireWarning()
    {
        var state = TripState.CreateNew();
        state.SetWeather(TripWeather.Rain);
        state.SetVehiclePart(VehiclePart.Tires, 42f);

        var warning = TripWarningAdvisor.GetPrimaryWarning(state);

        Assert.That(warning.Kind, Is.EqualTo(TripWarningKind.RainWornTires));
        Assert.That(warning.Message, Does.Contain("雨天"));
        Assert.That(warning.Message, Does.Contain("轮胎"));
    }

    [Test]
    public void NightWithWeakLightsProducesNightLightWarning()
    {
        var state = TripState.CreateNew();
        state.SetClockMinutes(22 * 60);
        state.SetVehiclePart(VehiclePart.Lights, 42f);

        var warning = TripWarningAdvisor.GetPrimaryWarning(state);

        Assert.That(warning.Kind, Is.EqualTo(TripWarningKind.NightWeakLights));
        Assert.That(warning.Message, Does.Contain("夜晚"));
        Assert.That(warning.Message, Does.Contain("车灯"));
    }

    [Test]
    public void HealthyStateProducesStableMessage()
    {
        var warning = TripWarningAdvisor.GetPrimaryWarning(TripState.CreateNew());

        Assert.That(warning.Kind, Is.EqualTo(TripWarningKind.None));
        Assert.That(warning.Message, Does.Contain("稳定"));
    }
}
