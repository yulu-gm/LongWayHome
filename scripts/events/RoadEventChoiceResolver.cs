using System;
using System.Collections.Generic;
using Godot_V2.Scripts.Gameplay;

namespace Godot_V2.Scripts.Events;

public sealed record RoadEventChoiceApplyResult(
    bool Applied,
    string Message,
    IReadOnlyList<string> Flags);

public static class RoadEventChoiceResolver
{
    public static RoadEventChoiceApplyResult ApplyChoice(TripState state, RoadEventChoice choice)
    {
        var validation = Validate(choice);
        if (validation.Length > 0)
        {
            return new RoadEventChoiceApplyResult(false, validation, Array.Empty<string>());
        }

        var flags = new List<string>();
        foreach (var effect in choice.Results)
        {
            switch (effect.Kind)
            {
                case RoadEventEffectKind.FuelLiters:
                    state.SetFuel(state.FuelLiters + effect.Amount);
                    break;
                case RoadEventEffectKind.Money:
                    state.SetMoney(state.Money + (int)MathF.Round(effect.Amount));
                    break;
                case RoadEventEffectKind.Energy:
                    state.SetEnergy(state.Energy + effect.Amount);
                    break;
                case RoadEventEffectKind.VehiclePartCondition:
                    var part = Enum.Parse<VehiclePart>(effect.Target, ignoreCase: true);
                    state.SetVehiclePart(part, state.GetVehiclePart(part) + effect.Amount);
                    break;
                case RoadEventEffectKind.TimeMinutes:
                    state.SetClockMinutes(state.ClockMinutes + (int)MathF.Round(effect.Amount));
                    break;
                case RoadEventEffectKind.Flag:
                    if (!string.IsNullOrWhiteSpace(effect.Target))
                    {
                        flags.Add(effect.Target);
                    }
                    break;
            }
        }

        return new RoadEventChoiceApplyResult(true, $"已选择：{choice.Text}", flags);
    }

    private static string Validate(RoadEventChoice choice)
    {
        foreach (var effect in choice.Results)
        {
            if (effect.Kind == RoadEventEffectKind.VehiclePartCondition
                && !Enum.TryParse<VehiclePart>(effect.Target, ignoreCase: true, out _))
            {
                return $"未知车辆部件：{effect.Target}";
            }
        }

        return string.Empty;
    }
}
