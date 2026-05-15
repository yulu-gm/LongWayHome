using System;
using System.Linq;
using Godot;
using Godot_V2.Scripts.Gameplay;
using Godot_V2.Scripts.Vehicles.Core;

namespace Godot_V2.Scripts.Vehicles.Runtime;

public partial class VehicleController : RigidBody3D
{
    private readonly VehicleTelemetry _telemetry = new();
    private VehiclePreset[] _presets = [];
    private RaycastWheel[] _wheels = [];
    private VehiclePreset? _basePreset;
    private VehiclePreset? _runtimePreset;
    private Powertrain? _powertrain;
    private float _engineTorqueMultiplier = 1f;
    private float _tireGripMultiplier = 1f;
    private float _brakeMultiplier = 1f;
    private float _handbrakeRearGripMultiplier = 1f;
    private VehicleConditionEffectProfile _conditionEffects = new(1f, 1f, 1f, 1f, 1f, 1f, false);

    [Export] public int InitialPresetIndex { get; set; } = 1;
    [Export] public float SteeringResponse { get; set; } = 9f;
    [Export] public float AirDrag { get; set; } = 0.035f;
    [Export] public float Downforce { get; set; } = 12f;

    private float _steering;

    public VehiclePreset Preset => _runtimePreset ?? VehiclePresetFactory.CreateDefaults()[0];
    public VehicleTelemetry Telemetry => _telemetry;
    public VehicleConditionEffectProfile CurrentConditionEffects => _conditionEffects;

    public override void _Ready()
    {
        AddToGroup("player_vehicle");
        _presets = VehiclePresetFactory.CreateDefaults().ToArray();
        _wheels =
        [
            GetNode<RaycastWheel>("Wheels/FrontLeftWheel"),
            GetNode<RaycastWheel>("Wheels/FrontRightWheel"),
            GetNode<RaycastWheel>("Wheels/RearLeftWheel"),
            GetNode<RaycastWheel>("Wheels/RearRightWheel")
        ];
        SetPresetIndex(Math.Clamp(InitialPresetIndex, 0, _presets.Length - 1));
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_runtimePreset is null || _powertrain is null)
        {
            return;
        }

        Sleeping = false;
        var input = VehicleInputReader.Read();
        var forward = (-GlobalTransform.Basis.Z).Normalized();
        var forwardSpeed = LinearVelocity.Dot(forward);
        var speedKph = LinearVelocity.Length() * 3.6f;
        var reverseThrottle = 0f;
        var brake = input.Brake;

        if (input.Brake > 0f && input.Throttle <= 0.01f && forwardSpeed < 1.5f)
        {
            reverseThrottle = input.Brake;
            brake = 0f;
        }

        _steering = Mathf.Lerp(_steering, input.Steering, 1f - MathF.Exp(-SteeringResponse * (float)delta));
        // Godot's positive Y rotation turns the vehicle's -Z forward vector left.
        var steeringRadians = -Mathf.DegToRad(_runtimePreset.MaxSteerAngleDeg) * _steering;
        var power = _powertrain.Update(
            (float)delta,
            input.Throttle,
            brake,
            input.Handbrake,
            forwardSpeed * 3.6f,
            reverseThrottle);

        foreach (var wheel in _wheels)
        {
            wheel.Simulate(this, _runtimePreset, power, steeringRadians, delta);
        }

        ApplyForce(-LinearVelocity * LinearVelocity.Length() * AirDrag);
        ApplyForce(Vector3.Down * LinearVelocity.LengthSquared() * Downforce);
        UpdateTelemetry(speedKph, power, input.Handbrake);
    }

    public void SetPresetIndex(int index)
    {
        if (_presets.Length == 0)
        {
            _presets = VehiclePresetFactory.CreateDefaults().ToArray();
        }

        index = Math.Clamp(index, 0, _presets.Length - 1);
        ApplyPreset(_presets[index]);
    }

    public void SetPresetById(string id)
    {
        var index = Array.FindIndex(_presets, preset => preset.Id == id);
        if (index >= 0)
        {
            SetPresetIndex(index);
        }
    }

    public string[] GetPresetNames() => _presets.Select(p => p.DisplayName).ToArray();
    public string[] GetPresetIds() => _presets.Select(p => p.Id).ToArray();

    public void SetRuntimeTuning(
        float engineTorqueMultiplier,
        float tireGripMultiplier,
        float brakeMultiplier,
        float handbrakeRearGripMultiplier)
    {
        _engineTorqueMultiplier = engineTorqueMultiplier;
        _tireGripMultiplier = tireGripMultiplier;
        _brakeMultiplier = brakeMultiplier;
        _handbrakeRearGripMultiplier = handbrakeRearGripMultiplier;
        RebuildRuntimePreset();
    }

    public void ApplyTripStateEffects(TripState tripState)
    {
        _conditionEffects = VehicleConditionEffects.Calculate(tripState);
        RebuildRuntimePreset();
    }

    private void ApplyPreset(VehiclePreset preset)
    {
        _basePreset = preset;
        RebuildRuntimePreset();
    }

    private void RebuildRuntimePreset()
    {
        if (_basePreset is null)
        {
            return;
        }

        _runtimePreset = VehiclePresetTuning.Apply(
            _basePreset,
            _engineTorqueMultiplier * _conditionEffects.EngineTorqueMultiplier,
            _tireGripMultiplier * _conditionEffects.TireGripMultiplier,
            _brakeMultiplier * _conditionEffects.BrakeMultiplier,
            _handbrakeRearGripMultiplier * _conditionEffects.HandbrakeRearGripMultiplier,
            _conditionEffects.SuspensionMultiplier);
        _powertrain = new Powertrain(_runtimePreset);
        Mass = _runtimePreset.MassKg;
        CenterOfMassMode = CenterOfMassModeEnum.Custom;
        CenterOfMass = new Vector3(0f, -_runtimePreset.CenterOfMassHeight, 0f);
        GravityScale = 1f;
        LinearDamp = 0.08f;
        AngularDamp = 1.8f;
        _telemetry.Drivetrain = _runtimePreset.Drivetrain;
    }

    private void UpdateTelemetry(float speedKph, PowertrainOutput power, bool handbrake)
    {
        _telemetry.SpeedKph = speedKph;
        _telemetry.EngineRpm = power.EngineRpm;
        _telemetry.Gear = power.Gear;
        _telemetry.Handbrake = handbrake;
        _telemetry.Drivetrain = Preset.Drivetrain;

        _telemetry.FrontSlip = AverageSlip(WheelPosition.FrontLeft, WheelPosition.FrontRight);
        _telemetry.RearSlip = AverageSlip(WheelPosition.RearLeft, WheelPosition.RearRight);

        var frontLeft = GetWheelTelemetry(WheelPosition.FrontLeft);
        var frontRight = GetWheelTelemetry(WheelPosition.FrontRight);
        var rearLeft = GetWheelTelemetry(WheelPosition.RearLeft);
        var rearRight = GetWheelTelemetry(WheelPosition.RearRight);

        _telemetry.FrontLeftLoad = frontLeft.Load;
        _telemetry.FrontLeftCompression = frontLeft.Compression;
        _telemetry.FrontRightLoad = frontRight.Load;
        _telemetry.FrontRightCompression = frontRight.Compression;
        _telemetry.RearLeftLoad = rearLeft.Load;
        _telemetry.RearLeftCompression = rearLeft.Compression;
        _telemetry.RearRightLoad = rearRight.Load;
        _telemetry.RearRightCompression = rearRight.Compression;
    }

    private float AverageSlip(WheelPosition a, WheelPosition b)
    {
        var first = _wheels.FirstOrDefault(wheel => wheel.WheelPosition == a);
        var second = _wheels.FirstOrDefault(wheel => wheel.WheelPosition == b);
        return (MathF.Abs(first?.LateralSlip ?? 0f) + MathF.Abs(second?.LateralSlip ?? 0f)) * 0.5f;
    }

    private (float Load, float Compression) GetWheelTelemetry(WheelPosition position)
    {
        var wheel = _wheels.FirstOrDefault(candidate => candidate.WheelPosition == position);
        return (wheel?.NormalLoad ?? 0f, wheel?.CompressionMeters ?? 0f);
    }
}
