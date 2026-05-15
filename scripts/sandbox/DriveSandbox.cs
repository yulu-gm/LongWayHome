using Godot;
using Godot_V2.Scripts.Gameplay;
using Godot_V2.Scripts.Vehicles.Runtime;

namespace Godot_V2.Scripts.Sandbox;

public partial class DriveSandbox : Node3D
{
    public TripState TripState { get; private set; } = TripState.CreateNew();

    public override void _Ready()
    {
        EnsureInputAction("drive_throttle", Key.W);
        EnsureInputAction("drive_brake", Key.S);
        EnsureInputAction("drive_steer_left", Key.A);
        EnsureInputAction("drive_steer_right", Key.D);
        EnsureInputAction("drive_handbrake", Key.Shift);
        EnsureInputAction("toggle_debug_panel", Key.Tab);
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetNodeOrNull<VehicleController>("PrototypeCar")?.ApplyTripStateEffects(TripState);
    }

    private static void EnsureInputAction(string name, Key key)
    {
        var action = new StringName(name);
        if (!InputMap.HasAction(action))
        {
            InputMap.AddAction(action);
        }

        var inputEvent = new InputEventKey { PhysicalKeycode = key };
        InputMap.ActionAddEvent(action, inputEvent);
    }
}
