using Godot;
using Godot_V2.Scripts.World;
using Godot_V2.Scripts.Gameplay;
using Godot_V2.Scripts.Places;
using Godot_V2.Scripts.Vehicles.Runtime;

namespace Godot_V2.Scripts.Sandbox;

public partial class DriveSandbox : Node3D
{
    public TripState TripState { get; private set; } = TripState.CreateNew();
    public PlaceDefinition CurrentPlace { get; private set; } =
        PlaceCatalog.CreateDefaults().First(place => place.Type == PlaceType.FuelStation);
    public IReadOnlyList<RouteChoice> CurrentRouteChoices { get; private set; } = [];
    public int RouteSeed { get; private set; } = 23;

    public override void _Ready()
    {
        EnsureInputAction("drive_throttle", Key.W);
        EnsureInputAction("drive_brake", Key.S);
        EnsureInputAction("drive_steer_left", Key.A);
        EnsureInputAction("drive_steer_right", Key.D);
        EnsureInputAction("drive_handbrake", Key.Shift);
        EnsureInputAction("toggle_debug_panel", Key.Tab);
        EnsureInputAction("toggle_service_panel", Key.E);
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetNodeOrNull<VehicleController>("PrototypeCar")?.ApplyTripStateEffects(TripState);
        RefreshRouteChoices();
    }

    public IReadOnlyList<RouteChoice> GetRouteChoices()
    {
        if (CurrentRouteChoices.Count == 0)
        {
            RefreshRouteChoices();
        }

        return CurrentRouteChoices;
    }

    public RouteSelectionResult SelectRoute(int index)
    {
        var choices = GetRouteChoices();
        if (index < 0 || index >= choices.Count)
        {
            return new RouteSelectionResult(false, "路线不可用。", choices.Count > 0 ? choices[0] : CreateFallbackRoute());
        }

        var choice = choices[index];
        var result = RouteSelectionResolver.ApplyChoice(TripState, choice);
        if (result.Applied)
        {
            CurrentPlace = PlaceCatalog.CreateForRouteDestination(choice);
            RouteSeed++;
            RefreshRouteChoices();
        }

        return result;
    }

    private void RefreshRouteChoices()
    {
        CurrentRouteChoices = RouteSelectionResolver.GenerateChoices(TripState, RouteSeed);
    }

    private static RouteChoice CreateFallbackRoute()
    {
        var destination = new RouteNode("fallback", "未知道路", [PlaceService.Rest], ["fallback"]);
        return new RouteChoice(
            "fallback",
            destination,
            0f,
            RoadType.RuralRoad,
            RouteRisk.Low,
            TripWeather.Clear,
            [PlaceService.Rest],
            ["fallback"],
            []);
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
