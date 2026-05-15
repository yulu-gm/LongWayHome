using System;
using System.Collections.Generic;
using System.Linq;

namespace Godot_V2.Scripts.Gameplay;

public enum TripWeather
{
    Clear,
    Rain,
    Fog,
    Storm,
    Snow
}

public enum TimeOfDayPhase
{
    Dawn,
    Day,
    Dusk,
    Night
}

public enum VehiclePart
{
    Engine,
    Tires,
    Battery,
    Lights,
    Suspension,
    Body
}

public sealed class TripState
{
    private const int MinutesPerDay = 24 * 60;
    private readonly Dictionary<VehiclePart, float> _vehicleParts;

    private TripState()
    {
        _vehicleParts = Enum.GetValues<VehiclePart>().ToDictionary(part => part, _ => 100f);
    }

    public float FuelCapacityLiters { get; private set; }
    public float FuelLiters { get; private set; }
    public int Money { get; private set; }
    public float Energy { get; private set; }
    public int ClockMinutes { get; private set; }
    public TripWeather Weather { get; private set; }
    public string CurrentLocation { get; private set; } = string.Empty;
    public float TargetDistanceKm { get; private set; }
    public IReadOnlyDictionary<VehiclePart, float> VehicleParts => _vehicleParts;

    public TimeOfDayPhase TimeOfDay => ClockMinutes switch
    {
        >= 5 * 60 and < 8 * 60 => TimeOfDayPhase.Dawn,
        >= 8 * 60 and < 17 * 60 => TimeOfDayPhase.Day,
        >= 17 * 60 and < 20 * 60 => TimeOfDayPhase.Dusk,
        _ => TimeOfDayPhase.Night
    };

    public float AverageVehicleCondition => _vehicleParts.Count == 0 ? 0f : _vehicleParts.Values.Average();

    public static TripState CreateNew()
    {
        var state = new TripState
        {
            FuelCapacityLiters = 60f,
            Weather = TripWeather.Clear,
            CurrentLocation = "旧木镇外"
        };
        state.SetFuel(45f);
        state.SetMoney(620);
        state.SetEnergy(82f);
        state.SetClockMinutes(9 * 60 + 42);
        state.SetTargetDistance(86f);
        return state;
    }

    public void SetFuelCapacity(float liters)
    {
        FuelCapacityLiters = Math.Max(1f, liters);
        FuelLiters = Clamp(FuelLiters, 0f, FuelCapacityLiters);
    }

    public void SetFuel(float liters)
    {
        FuelLiters = Clamp(liters, 0f, FuelCapacityLiters);
    }

    public void SetMoney(int money)
    {
        Money = Math.Max(0, money);
    }

    public void SetEnergy(float energy)
    {
        Energy = Clamp(energy, 0f, 100f);
    }

    public void SetClockMinutes(int minutes)
    {
        ClockMinutes = ((minutes % MinutesPerDay) + MinutesPerDay) % MinutesPerDay;
    }

    public void SetWeather(TripWeather weather)
    {
        Weather = weather;
    }

    public void SetCurrentLocation(string location)
    {
        CurrentLocation = string.IsNullOrWhiteSpace(location) ? CurrentLocation : location.Trim();
    }

    public void SetTargetDistance(float kilometers)
    {
        TargetDistanceKm = Math.Max(0f, kilometers);
    }

    public float GetVehiclePart(VehiclePart part) => _vehicleParts[part];

    public void SetVehiclePart(VehiclePart part, float condition)
    {
        _vehicleParts[part] = Clamp(condition, 0f, 100f);
    }

    private static float Clamp(float value, float min, float max) => Math.Min(max, Math.Max(min, value));
}
