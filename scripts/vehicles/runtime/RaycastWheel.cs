using System;
using Godot;
using Godot_V2.Scripts.Vehicles.Core;

namespace Godot_V2.Scripts.Vehicles.Runtime;

public partial class RaycastWheel : Node3D
{
    private float _previousCompressionMeters;

    [Export] public WheelPosition WheelPosition { get; set; }
    [Export] public bool IsSteeringWheel { get; set; }
    [Export] public bool IsRearWheel { get; set; }
    [Export] public NodePath? VisualPath { get; set; }

    public float CompressionMeters { get; private set; }
    public float NormalLoad { get; private set; }
    public float LateralSlip { get; private set; }
    public bool IsGrounded { get; private set; }

    private Node3D? _visual;

    public override void _Ready()
    {
        if (VisualPath is not null && !VisualPath.IsEmpty)
        {
            _visual = GetNodeOrNull<Node3D>(VisualPath);
        }
    }

    public void Simulate(
        VehicleController body,
        VehiclePreset preset,
        PowertrainOutput powertrain,
        float steeringRadians,
        double delta)
    {
        var spaceState = GetWorld3D().DirectSpaceState;
        var up = body.GlobalTransform.Basis.Y.Normalized();
        var down = -up;
        var rayStart = GlobalPosition;
        var rayLength = preset.SuspensionRestLength + preset.SuspensionTravel + preset.WheelRadius;
        var rayEnd = rayStart + down * rayLength;
        var exclude = new Godot.Collections.Array<Rid> { body.GetRid() };
        var query = PhysicsRayQueryParameters3D.Create(rayStart, rayEnd, 0xffffffff, exclude);
        var hit = spaceState.IntersectRay(query);

        if (hit.Count == 0)
        {
            IsGrounded = false;
            CompressionMeters = 0f;
            NormalLoad = 0f;
            LateralSlip = 0f;
            _previousCompressionMeters = 0f;
            UpdateVisual(steeringRadians, preset.WheelRadius, 0f);
            return;
        }

        IsGrounded = true;
        var contactPoint = (Vector3)hit["position"];
        var contactNormal = ((Vector3)hit["normal"]).Normalized();
        var springLength = Math.Max(0f, rayStart.DistanceTo(contactPoint) - preset.WheelRadius);
        CompressionMeters = Math.Clamp(preset.SuspensionRestLength - springLength, 0f, preset.SuspensionTravel);
        var compressionVelocity = (CompressionMeters - _previousCompressionMeters) / Math.Max(0.001f, (float)delta);
        _previousCompressionMeters = CompressionMeters;

        NormalLoad = Math.Max(0f, CompressionMeters * preset.SpringStrength + compressionVelocity * preset.DamperStrength);

        var forward = (-body.GlobalTransform.Basis.Z).Normalized();
        if (IsSteeringWheel)
        {
            forward = forward.Rotated(up, steeringRadians).Normalized();
        }

        var right = forward.Cross(up).Normalized();
        var localOffset = contactPoint - body.GlobalPosition;
        var pointVelocity = body.LinearVelocity + body.AngularVelocity.Cross(localOffset);
        var forwardVelocity = pointVelocity.Dot(forward);
        var lateralVelocity = pointVelocity.Dot(right);
        LateralSlip = TireForceModel.EstimateSlip(lateralVelocity, forwardVelocity);

        var driveTorque = GetWheelTorque(powertrain.WheelTorque);
        var brakeTorque = powertrain.BrakeTorque * 0.25f;
        if (IsRearWheel)
        {
            brakeTorque += powertrain.HandbrakeTorque * 0.5f;
        }

        var suspensionForce = contactNormal * NormalLoad;
        var longitudinalForce = forward * TireForceModel.CalculateLongitudinalForce(
            preset,
            NormalLoad,
            driveTorque,
            brakeTorque,
            preset.WheelRadius,
            forwardVelocity);
        var lateralForce = right * TireForceModel.CalculateLateralForce(
            preset,
            NormalLoad,
            LateralSlip,
            powertrain.HandbrakeTorque > 0f,
            IsRearWheel);

        body.ApplyForce(suspensionForce + longitudinalForce + lateralForce, localOffset);
        UpdateVisual(steeringRadians, preset.WheelRadius, forwardVelocity);
    }

    private float GetWheelTorque(WheelTorqueSet torque) =>
        WheelPosition switch
        {
            WheelPosition.FrontLeft => torque.FrontLeft,
            WheelPosition.FrontRight => torque.FrontRight,
            WheelPosition.RearLeft => torque.RearLeft,
            WheelPosition.RearRight => torque.RearRight,
            _ => 0f
        };

    private void UpdateVisual(float steeringRadians, float wheelRadius, float forwardVelocity)
    {
        if (_visual is null)
        {
            return;
        }

        var steer = IsSteeringWheel ? steeringRadians : 0f;
        var spin = forwardVelocity / Math.Max(0.01f, wheelRadius) * (float)GetPhysicsProcessDeltaTime();
        _visual.RotateObjectLocal(Vector3.Right, spin);
        _visual.Rotation = new Vector3(_visual.Rotation.X, steer, _visual.Rotation.Z);
    }
}
