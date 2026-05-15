using System.Collections.Generic;
using Godot;
using Godot_V2.Scripts.Gameplay;
using Godot_V2.Scripts.Sandbox;
using Godot_V2.Scripts.Vehicles.Runtime;

namespace Godot_V2.Scripts.Ui;

public partial class DrivingHud : CanvasLayer
{
    [Export] public NodePath? VehiclePath { get; set; }
    [Export] public NodePath? SandboxPath { get; set; }

    private readonly Dictionary<string, Label> _values = new();
    private readonly Dictionary<string, ProgressBar> _bars = new();
    private VehicleController? _vehicle;
    private DriveSandbox? _sandbox;
    private PanelContainer? _rootPanel;
    private TripWarningKind _lastWarningKind = TripWarningKind.None;

    public override void _Ready()
    {
        Layer = 12;
        _sandbox = SandboxPath is not null && !SandboxPath.IsEmpty
            ? GetNodeOrNull<DriveSandbox>(SandboxPath)
            : GetParent() as DriveSandbox;
        _vehicle = VehiclePath is not null && !VehiclePath.IsEmpty
            ? GetNodeOrNull<VehicleController>(VehiclePath)
            : null;
        _vehicle ??= GetTree().GetFirstNodeInGroup("player_vehicle") as VehicleController;
        BuildHud();
        UpdateLayout();
        UpdateValues();
    }

    public override void _Process(double delta)
    {
        _ = delta;
        _sandbox ??= GetParent() as DriveSandbox;
        _vehicle ??= GetTree().GetFirstNodeInGroup("player_vehicle") as VehicleController;
        UpdateLayout();
        UpdateValues();
    }

    private void BuildHud()
    {
        _rootPanel = new PanelContainer
        {
            Name = "HudRoot",
            CustomMinimumSize = new Vector2(430f, 136f)
        };
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.03f, 0.035f, 0.035f, 0.74f),
            BorderColor = new Color(0.92f, 0.68f, 0.28f, 0.38f),
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            CornerRadiusBottomLeft = 7,
            CornerRadiusBottomRight = 7,
            CornerRadiusTopLeft = 7,
            CornerRadiusTopRight = 7,
            ContentMarginBottom = 10,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10
        };
        _rootPanel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(_rootPanel);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 7);
        _rootPanel.AddChild(root);

        var topRow = new HBoxContainer();
        topRow.AddThemeConstantOverride("separation", 12);
        root.AddChild(topRow);
        AddValuePill(topRow, "TimeValue", "09:42", wide: false);
        AddValuePill(topRow, "WeatherValue", "晴天", wide: false);
        AddValuePill(topRow, "MoneyValue", "$620", wide: false);

        var bars = new GridContainer { Columns = 3 };
        bars.AddThemeConstantOverride("h_separation", 10);
        bars.AddThemeConstantOverride("v_separation", 4);
        root.AddChild(bars);
        AddMeter(bars, "FuelValue", "FuelBar", "油量");
        AddMeter(bars, "ConditionValue", "ConditionBar", "耐久");
        AddMeter(bars, "EnergyValue", "EnergyBar", "精力");

        var bottomRow = new HBoxContainer();
        bottomRow.AddThemeConstantOverride("separation", 10);
        root.AddChild(bottomRow);
        AddValuePill(bottomRow, "DistanceValue", "86 km", wide: true);
        AddValuePill(bottomRow, "SpeedValue", "0 km/h", wide: true);

        var warning = new Label
        {
            Name = "WarningValue",
            Text = "旅途状态稳定",
            CustomMinimumSize = new Vector2(394f, 22f),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        warning.AddThemeColorOverride("font_color", new Color(1f, 0.74f, 0.28f, 1f));
        warning.AddThemeFontSizeOverride("font_size", 13);
        root.AddChild(warning);
        _values["WarningValue"] = warning;
    }

    private void AddMeter(GridContainer parent, string valueName, string barName, string labelText)
    {
        var stack = new VBoxContainer { CustomMinimumSize = new Vector2(126f, 0f) };
        stack.AddThemeConstantOverride("separation", 2);
        parent.AddChild(stack);

        var label = new Label { Text = labelText };
        label.AddThemeColorOverride("font_color", new Color(0.84f, 0.82f, 0.74f, 0.95f));
        label.AddThemeFontSizeOverride("font_size", 12);
        stack.AddChild(label);

        var value = new Label
        {
            Name = valueName,
            Text = "-",
            HorizontalAlignment = HorizontalAlignment.Right
        };
        value.AddThemeColorOverride("font_color", new Color(1f, 0.78f, 0.3f, 1f));
        value.AddThemeFontSizeOverride("font_size", 13);
        stack.AddChild(value);
        _values[valueName] = value;

        var bar = new ProgressBar
        {
            Name = barName,
            MaxValue = 100,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(112f, 7f)
        };
        bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0.09f, 0.1f, 0.1f, 0.86f) });
        bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color(0.95f, 0.63f, 0.2f, 1f) });
        stack.AddChild(bar);
        _bars[barName] = bar;
    }

    private void AddValuePill(HBoxContainer parent, string name, string initialText, bool wide)
    {
        var label = new Label
        {
            Name = name,
            Text = initialText,
            CustomMinimumSize = new Vector2(wide ? 112f : 72f, 24f),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeColorOverride("font_color", new Color(0.96f, 0.92f, 0.82f, 1f));
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeStyleboxOverride("normal", new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.085f, 0.08f, 0.82f),
            BorderColor = new Color(1f, 0.72f, 0.3f, 0.28f),
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            ContentMarginLeft = 8,
            ContentMarginRight = 8
        });
        parent.AddChild(label);
        _values[name] = label;
    }

    private void UpdateLayout()
    {
        if (_rootPanel is null)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        _rootPanel.Position = new Vector2(16f, Mathf.Max(320f, viewportSize.Y - 156f));
    }

    private void UpdateValues()
    {
        var trip = _sandbox?.TripState ?? TripState.CreateNew();
        var speedKph = _vehicle?.Telemetry.SpeedKph ?? 0f;
        var condition = trip.AverageVehicleCondition;

        SetValue("TimeValue", FormatTime(trip.ClockMinutes));
        SetValue("WeatherValue", FormatWeather(trip.Weather));
        SetValue("FuelValue", $"{trip.FuelLiters:0}/{trip.FuelCapacityLiters:0} L");
        SetValue("ConditionValue", $"{condition:0}%");
        SetValue("EnergyValue", $"{trip.Energy:0}%");
        SetValue("MoneyValue", $"${trip.Money}");
        SetValue("DistanceValue", $"{trip.TargetDistanceKm:0} km");
        SetValue("SpeedValue", $"{speedKph:0} km/h");
        UpdateWarning(trip);

        SetBar("FuelBar", trip.FuelCapacityLiters <= 0f ? 0f : trip.FuelLiters / trip.FuelCapacityLiters * 100f);
        SetBar("ConditionBar", condition);
        SetBar("EnergyBar", trip.Energy);
    }

    private static string FormatTime(int minutes)
    {
        var hour = (minutes / 60) % 24;
        var minute = minutes % 60;
        return $"{hour:00}:{minute:00}";
    }

    private static string FormatWeather(TripWeather weather) => weather switch
    {
        TripWeather.Clear => "晴天",
        TripWeather.Rain => "雨天",
        TripWeather.Fog => "雾天",
        TripWeather.Storm => "暴雨",
        TripWeather.Snow => "雪天",
        _ => "未知"
    };

    private void SetValue(string key, string value)
    {
        if (_values.TryGetValue(key, out var label))
        {
            label.Text = value;
        }
    }

    private void SetBar(string key, float value)
    {
        if (_bars.TryGetValue(key, out var bar))
        {
            bar.Value = Mathf.Clamp(value, 0f, 100f);
        }
    }

    private void UpdateWarning(TripState trip)
    {
        var warning = TripWarningAdvisor.GetPrimaryWarning(trip);
        if (warning.Kind == _lastWarningKind && _values.TryGetValue("WarningValue", out var label) && label.Text == warning.Message)
        {
            return;
        }

        _lastWarningKind = warning.Kind;
        SetValue("WarningValue", warning.Message);
    }
}
