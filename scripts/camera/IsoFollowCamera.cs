using System;
using Godot;
using Godot_V2.Scripts.Vehicles.Runtime;

namespace Godot_V2.Scripts.Camera;

public partial class IsoFollowCamera : Camera3D
{
    [Export] public NodePath? TargetPath { get; set; }
    [Export] public Vector3 Offset { get; set; } = new(0f, 18f, 16f);
    [Export] public float FollowSharpness { get; set; } = 5.5f;
    [Export] public float LookAheadSeconds { get; set; } = 0.45f;
    [Export] public float LookHeight { get; set; } = 0.5f;

    private VehicleController? _target;

    public override void _Ready()
    {
        Current = true;
        if (TargetPath is not null && !TargetPath.IsEmpty)
        {
            _target = GetNodeOrNull<VehicleController>(TargetPath);
        }

        _target ??= GetTree().GetFirstNodeInGroup("player_vehicle") as VehicleController;
    }

    public override void _PhysicsProcess(double delta)
    {
        _target ??= GetTree().GetFirstNodeInGroup("player_vehicle") as VehicleController;
        if (_target is null)
        {
            return;
        }

        var targetPosition = _target.GlobalPosition;
        var speedLookAhead = _target.LinearVelocity * LookAheadSeconds;
        var desiredPosition = targetPosition + Offset;
        var blend = 1f - MathF.Exp(-FollowSharpness * (float)delta);
        GlobalPosition = GlobalPosition.Lerp(desiredPosition, blend);
        LookAt(targetPosition + speedLookAhead + Vector3.Up * LookHeight, Vector3.Up);
    }
}
