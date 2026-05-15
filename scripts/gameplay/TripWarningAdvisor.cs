namespace Godot_V2.Scripts.Gameplay;

public enum TripWarningKind
{
    None,
    LowFuel,
    LowEnergy,
    VehicleConditionLow,
    RainWornTires,
    NightWeakLights
}

public readonly record struct TripWarning(TripWarningKind Kind, string Message);

public static class TripWarningAdvisor
{
    public static TripWarning GetPrimaryWarning(TripState state)
    {
        var fuelRatio = state.FuelCapacityLiters <= 0f ? 0f : state.FuelLiters / state.FuelCapacityLiters;
        if (fuelRatio <= 0.15f)
        {
            return new TripWarning(TripWarningKind.LowFuel, "油量偏低，优先寻找加油点");
        }

        var tires = state.GetVehiclePart(VehiclePart.Tires);
        if (state.Weather is TripWeather.Rain or TripWeather.Storm && tires < 50f)
        {
            return new TripWarning(TripWarningKind.RainWornTires, "雨天轮胎磨损，减速巡航");
        }

        var lights = state.GetVehiclePart(VehiclePart.Lights);
        if (state.TimeOfDay == TimeOfDayPhase.Night && lights < 50f)
        {
            return new TripWarning(TripWarningKind.NightWeakLights, "夜晚车灯偏弱，视野受限");
        }

        if (state.Energy <= 25f)
        {
            return new TripWarning(TripWarningKind.LowEnergy, "精力不足，考虑休息");
        }

        if (state.AverageVehicleCondition <= 50f)
        {
            return new TripWarning(TripWarningKind.VehicleConditionLow, "车况偏低，尽快维修");
        }

        return new TripWarning(TripWarningKind.None, "旅途状态稳定");
    }
}
