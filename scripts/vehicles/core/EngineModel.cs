using System;

namespace Godot_V2.Scripts.Vehicles.Core;

public static class EngineModel
{
    public static float EvaluateTorque(VehiclePreset preset, float engineRpm, float throttle)
    {
        throttle = Math.Clamp(throttle, 0f, 1f);
        if (throttle <= 0f)
        {
            return 0f;
        }

        var rpmRange = Math.Max(1f, preset.RedlineRpm - preset.IdleRpm);
        var normalized = Math.Clamp((engineRpm - preset.IdleRpm) / rpmRange, 0f, 1f);
        var lowEnd = 0.62f + normalized * 0.45f;
        var highEndFalloff = normalized > 0.72f ? 1f - (normalized - 0.72f) * 0.48f : 1f;
        var torqueFactor = Math.Clamp(lowEnd * highEndFalloff, 0.35f, 1.08f);

        return preset.PeakTorqueNm * torqueFactor * throttle;
    }
}
