using System;
using System.Collections.Generic;
using Godot;
using Godot_V2.Scripts.Vehicles.Runtime;

namespace Godot_V2.Scripts.Ui;

public partial class VehicleDebugPanel : CanvasLayer
{
    [Export] public NodePath? VehiclePath { get; set; }
    [Export] public bool StartVisible { get; set; }

    private readonly Dictionary<string, Label> _values = new();
    private VehicleController? _vehicle;
    private PanelContainer? _panel;
    private bool _visible;
    private bool _tabWasDown;
    private float _engineTorqueMultiplier = 1f;
    private float _tireGripMultiplier = 1f;
    private float _brakeMultiplier = 1f;
    private float _handbrakeGripMultiplier = 1f;

    public override void _Ready()
    {
        Layer = 20;
        _visible = StartVisible;
        _vehicle = VehiclePath is not null && !VehiclePath.IsEmpty
            ? GetNodeOrNull<VehicleController>(VehiclePath)
            : null;
        _vehicle ??= GetTree().GetFirstNodeInGroup("player_vehicle") as VehicleController;
        BuildPanel();
        RebuildPresetButtons();
    }

    public override void _Process(double delta)
    {
        _ = delta;
        _vehicle ??= GetTree().GetFirstNodeInGroup("player_vehicle") as VehicleController;
        var tabDown = Input.IsKeyPressed(Key.Tab);
        var tabPressed = VehicleInputReader.IsTogglePanelPressed() || tabDown && !_tabWasDown;
        _tabWasDown = tabDown;
        if (tabPressed)
        {
            _visible = !_visible;
            if (_panel is not null)
            {
                _panel.Visible = _visible;
            }
        }

        if (_vehicle is null)
        {
            return;
        }

        var telemetry = _vehicle.Telemetry;
        SetValue("speed", $"{telemetry.SpeedKph:0} km/h");
        SetValue("rpm", $"{telemetry.EngineRpm:0}");
        SetValue("gear", telemetry.Gear.ToString());
        SetValue("drive", telemetry.Drivetrain.ToString().ToUpperInvariant());
        SetValue("handbrake", telemetry.Handbrake ? "ON" : "OFF");
        SetValue("front_slip", $"{telemetry.FrontSlip:0.00}");
        SetValue("rear_slip", $"{telemetry.RearSlip:0.00}");
        SetValue("fl_load", $"{telemetry.FrontLeftLoad / 1000f:0.0} kN / {telemetry.FrontLeftCompression:0.00} m");
        SetValue("fr_load", $"{telemetry.FrontRightLoad / 1000f:0.0} kN / {telemetry.FrontRightCompression:0.00} m");
        SetValue("rl_load", $"{telemetry.RearLeftLoad / 1000f:0.0} kN / {telemetry.RearLeftCompression:0.00} m");
        SetValue("rr_load", $"{telemetry.RearRightLoad / 1000f:0.0} kN / {telemetry.RearRightCompression:0.00} m");
    }

    private void BuildPanel()
    {
        _panel = new PanelContainer
        {
            Position = new Vector2(16f, 16f),
            CustomMinimumSize = new Vector2(340f, 520f),
            Visible = _visible
        };
        AddChild(_panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        _panel.AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        var title = new Label { Text = "Vehicle Debug Panel" };
        title.AddThemeFontSizeOverride("font_size", 20);
        root.AddChild(title);
        root.AddChild(new Label { Text = "Tab: show/hide | W/S/A/D + Shift" });

        AddReadout(root, "speed", "Speed");
        AddReadout(root, "rpm", "RPM");
        AddReadout(root, "gear", "Gear");
        AddReadout(root, "drive", "Drive");
        AddReadout(root, "handbrake", "Handbrake");
        AddReadout(root, "front_slip", "Front slip");
        AddReadout(root, "rear_slip", "Rear slip");
        AddReadout(root, "fl_load", "FL load/compress");
        AddReadout(root, "fr_load", "FR load/compress");
        AddReadout(root, "rl_load", "RL load/compress");
        AddReadout(root, "rr_load", "RR load/compress");

        var presetLabel = new Label { Text = "Presets" };
        presetLabel.AddThemeFontSizeOverride("font_size", 16);
        root.AddChild(presetLabel);

        var buttonGrid = new GridContainer { Columns = 2, Name = "PresetButtons" };
        buttonGrid.AddThemeConstantOverride("h_separation", 6);
        buttonGrid.AddThemeConstantOverride("v_separation", 6);
        root.AddChild(buttonGrid);

        var tuningLabel = new Label { Text = "Runtime tuning" };
        tuningLabel.AddThemeFontSizeOverride("font_size", 16);
        root.AddChild(tuningLabel);
        AddSlider(root, "Engine torque", 0.5f, 1.8f, 1f, value => _engineTorqueMultiplier = value);
        AddSlider(root, "Tire grip", 0.55f, 1.45f, 1f, value => _tireGripMultiplier = value);
        AddSlider(root, "Brake force", 0.5f, 1.6f, 1f, value => _brakeMultiplier = value);
        AddSlider(root, "Rear grip on handbrake", 0.35f, 1.1f, 1f, value => _handbrakeGripMultiplier = value);
    }

    private void RebuildPresetButtons()
    {
        if (_panel is null || _vehicle is null)
        {
            return;
        }

        var grid = _panel.FindChild("PresetButtons", true, false) as GridContainer;
        if (grid is null)
        {
            return;
        }

        foreach (var child in grid.GetChildren())
        {
            child.QueueFree();
        }

        var ids = _vehicle.GetPresetIds();
        var names = _vehicle.GetPresetNames();
        for (var index = 0; index < ids.Length; index++)
        {
            var id = ids[index];
            var button = new Button { Text = names[index] };
            button.Pressed += () => _vehicle.SetPresetById(id);
            grid.AddChild(button);
        }
    }

    private void AddReadout(VBoxContainer root, string key, string label)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        var name = new Label { Text = label, CustomMinimumSize = new Vector2(150f, 0f) };
        var value = new Label { Text = "-", HorizontalAlignment = HorizontalAlignment.Right };
        row.AddChild(name);
        row.AddChild(value);
        root.AddChild(row);
        _values[key] = value;
    }

    private void AddSlider(VBoxContainer root, string label, float min, float max, float value, Action<float> setter)
    {
        var caption = new Label { Text = $"{label}: {value:0.00}" };
        root.AddChild(caption);

        var slider = new HSlider
        {
            MinValue = min,
            MaxValue = max,
            Step = 0.01,
            Value = value,
            CustomMinimumSize = new Vector2(260f, 0f)
        };
        slider.ValueChanged += newValue =>
        {
            var floatValue = (float)newValue;
            setter(floatValue);
            caption.Text = $"{label}: {floatValue:0.00}";
            ApplyRuntimeTuning();
        };
        root.AddChild(slider);
    }

    private void ApplyRuntimeTuning()
    {
        _vehicle?.SetRuntimeTuning(
            _engineTorqueMultiplier,
            _tireGripMultiplier,
            _brakeMultiplier,
            _handbrakeGripMultiplier);
    }

    private void SetValue(string key, string value)
    {
        if (_values.TryGetValue(key, out var label))
        {
            label.Text = value;
        }
    }
}
