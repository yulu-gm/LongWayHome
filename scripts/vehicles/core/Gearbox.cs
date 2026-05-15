using System;

namespace Godot_V2.Scripts.Vehicles.Core;

public sealed class Gearbox
{
    private readonly float[] _gearRatios;
    private readonly float _upshiftFactor;
    private readonly float _downshiftFactor;

    public Gearbox(float[] gearRatios, float finalDriveRatio, float upshiftFactor, float downshiftFactor)
    {
        if (gearRatios.Length == 0)
        {
            throw new ArgumentException("Gearbox requires at least one gear.", nameof(gearRatios));
        }

        _gearRatios = gearRatios;
        FinalDriveRatio = finalDriveRatio;
        _upshiftFactor = upshiftFactor;
        _downshiftFactor = downshiftFactor;
    }

    public int CurrentGear { get; private set; } = 1;
    public float FinalDriveRatio { get; }
    public float CurrentRatio => _gearRatios[CurrentGear - 1];

    public void Reset()
    {
        CurrentGear = 1;
    }

    public void Update(float deltaSeconds, float throttle, float engineRpm, float redlineRpm)
    {
        _ = deltaSeconds;
        var upshiftRpm = redlineRpm * _upshiftFactor;
        var downshiftRpm = redlineRpm * _downshiftFactor;

        if (throttle > 0.15f && engineRpm >= upshiftRpm && CurrentGear < _gearRatios.Length)
        {
            CurrentGear++;
            return;
        }

        if (engineRpm <= downshiftRpm && CurrentGear > 1)
        {
            CurrentGear--;
        }
    }
}
