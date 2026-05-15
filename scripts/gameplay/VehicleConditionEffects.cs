using System;

namespace Godot_V2.Scripts.Gameplay;

public readonly record struct VehicleConditionEffectProfile(
    float EngineTorqueMultiplier,
    float TireGripMultiplier,
    float BrakeMultiplier,
    float HandbrakeRearGripMultiplier,
    float SuspensionMultiplier,
    float HeadlightVisibilityMultiplier,
    bool RequiresNightVisibilityWarning);

public static class VehicleConditionEffects
{
    public static VehicleConditionEffectProfile Calculate(TripState state)
    {
        var engine = MultiplierFromCondition(state.GetVehiclePart(VehiclePart.Engine), minMultiplier: 0.45f);
        var tires = MultiplierFromCondition(state.GetVehiclePart(VehiclePart.Tires), minMultiplier: 0.55f);
        var brakes = MultiplierFromCondition(state.GetVehiclePart(VehiclePart.Tires), minMultiplier: 0.65f);
        var suspension = MultiplierFromCondition(state.GetVehiclePart(VehiclePart.Suspension), minMultiplier: 0.55f);
        var lights = MultiplierFromCondition(state.GetVehiclePart(VehiclePart.Lights), minMultiplier: 0.25f);
        var weatherGrip = WeatherGripMultiplier(state.Weather);
        var weatherBrake = WeatherBrakeMultiplier(state.Weather);
        var visibilityWarning = state.TimeOfDay == TimeOfDayPhase.Night && lights < 0.75f;

        return new VehicleConditionEffectProfile(
            engine,
            tires * weatherGrip,
            brakes * weatherBrake,
            tires,
            suspension,
            lights,
            visibilityWarning);
    }

    private static float MultiplierFromCondition(float condition, float minMultiplier)
    {
        var normalized = Math.Clamp(condition, 0f, 100f) / 100f;
        return minMultiplier + normalized * (1f - minMultiplier);
    }

    private static float WeatherGripMultiplier(TripWeather weather) => weather switch
    {
        TripWeather.Rain => 0.9f,
        TripWeather.Fog => 0.97f,
        TripWeather.Storm => 0.82f,
        TripWeather.Snow => 0.75f,
        _ => 1f
    };

    private static float WeatherBrakeMultiplier(TripWeather weather) => weather switch
    {
        TripWeather.Rain => 0.95f,
        TripWeather.Fog => 0.98f,
        TripWeather.Storm => 0.9f,
        TripWeather.Snow => 0.85f,
        _ => 1f
    };
}
