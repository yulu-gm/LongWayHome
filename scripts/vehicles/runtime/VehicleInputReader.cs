using Godot;
using Godot_V2.Scripts.Vehicles.Core;

namespace Godot_V2.Scripts.Vehicles.Runtime;

public static class VehicleInputReader
{
    public static VehicleInputSnapshot Read()
    {
        var throttle = IsPressed("drive_throttle", Key.W) ? 1f : 0f;
        var brake = IsPressed("drive_brake", Key.S) ? 1f : 0f;
        var steerLeft = IsPressed("drive_steer_left", Key.A) ? 1f : 0f;
        var steerRight = IsPressed("drive_steer_right", Key.D) ? 1f : 0f;
        var handbrake = IsPressed("drive_handbrake", Key.Shift);

        return new VehicleInputSnapshot(
            throttle,
            brake,
            steerRight - steerLeft,
            handbrake);
    }

    public static bool IsTogglePanelPressed() =>
        InputMap.HasAction("toggle_debug_panel") && Input.IsActionJustPressed("toggle_debug_panel");

    private static bool IsPressed(string action, Key key) =>
        InputMap.HasAction(action) && Input.IsActionPressed(action) || Input.IsKeyPressed(key);
}
