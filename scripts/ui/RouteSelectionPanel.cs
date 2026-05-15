using System.Collections.Generic;
using Godot;
using Godot_V2.Scripts.Gameplay;
using Godot_V2.Scripts.Sandbox;
using Godot_V2.Scripts.World;

namespace Godot_V2.Scripts.Ui;

public partial class RouteSelectionPanel : CanvasLayer
{
    [Export] public NodePath? SandboxPath { get; set; }

    private readonly List<Button> _buttons = [];
    private readonly List<Label> _titles = [];
    private readonly List<Label> _previews = [];
    private readonly List<PanelContainer> _rows = [];
    private DriveSandbox? _sandbox;
    private PanelContainer? _rootPanel;
    private VBoxContainer? _routeList;
    private Label? _header;
    private Label? _status;

    public override void _Ready()
    {
        Layer = 14;
        _sandbox = SandboxPath is not null && !SandboxPath.IsEmpty
            ? GetNodeOrNull<DriveSandbox>(SandboxPath)
            : GetParent() as DriveSandbox;
        BuildPanel();
        Refresh();
        SetOpen(false);
    }

    public override void _Process(double delta)
    {
        _ = delta;
        _sandbox ??= GetParent() as DriveSandbox;
        UpdateLayout();
    }

    public void OpenForCurrentLocation()
    {
        SetOpen(true);
        Refresh();
    }

    public void SetOpen(bool isOpen)
    {
        if (_rootPanel is not null)
        {
            _rootPanel.Visible = isOpen;
        }
    }

    private void BuildPanel()
    {
        _rootPanel = new PanelContainer
        {
            Name = "RouteSelectionRoot",
            CustomMinimumSize = new Vector2(470f, 312f)
        };
        _rootPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.026f, 0.032f, 0.035f, 0.88f),
            BorderColor = new Color(0.82f, 0.67f, 0.34f, 0.5f),
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            CornerRadiusBottomLeft = 7,
            CornerRadiusBottomRight = 7,
            CornerRadiusTopLeft = 7,
            CornerRadiusTopRight = 7,
            ContentMarginBottom = 14,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 14
        });
        AddChild(_rootPanel);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 9);
        _rootPanel.AddChild(root);

        _header = new Label
        {
            Name = "RouteSelectionTitle",
            Text = "下一段路线",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _header.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.62f, 1f));
        _header.AddThemeFontSizeOverride("font_size", 21);
        root.AddChild(_header);

        _routeList = new VBoxContainer();
        _routeList.AddThemeConstantOverride("separation", 9);
        root.AddChild(_routeList);

        _status = new Label
        {
            Name = "RouteSelectionStatus",
            Text = "选择路线后回到驾驶。",
            CustomMinimumSize = new Vector2(420f, 22f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _status.AddThemeColorOverride("font_color", new Color(1f, 0.75f, 0.3f, 1f));
        _status.AddThemeFontSizeOverride("font_size", 13);
        root.AddChild(_status);
    }

    private void AddRouteRow(VBoxContainer parent, RouteChoice choice, int index)
    {
        var row = new PanelContainer { Name = $"RouteRow_{index}" };
        row.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.075f, 0.08f, 0.078f, 0.78f),
            BorderColor = new Color(0.95f, 0.65f, 0.24f, 0.2f),
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            ContentMarginBottom = 8,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8
        });
        parent.AddChild(row);
        _rows.Add(row);

        var line = new HBoxContainer();
        line.AddThemeConstantOverride("separation", 10);
        row.AddChild(line);

        var copy = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        copy.AddThemeConstantOverride("separation", 3);
        line.AddChild(copy);

        var title = new Label
        {
            Name = $"RouteTitle_{index}",
            Text = choice.Destination.Name
        };
        title.AddThemeColorOverride("font_color", new Color(0.97f, 0.9f, 0.76f, 1f));
        title.AddThemeFontSizeOverride("font_size", 15);
        copy.AddChild(title);
        _titles.Add(title);

        var preview = new Label
        {
            Name = $"RoutePreview_{index}",
            Text = FormatRoutePreview(choice),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        preview.AddThemeColorOverride("font_color", new Color(0.82f, 0.8f, 0.72f, 0.95f));
        preview.AddThemeFontSizeOverride("font_size", 12);
        copy.AddChild(preview);
        _previews.Add(preview);

        var button = new Button
        {
            Name = $"RouteButton_{index}",
            Text = "出发",
            CustomMinimumSize = new Vector2(72f, 34f)
        };
        var capturedIndex = index;
        button.Pressed += () => SelectRoute(capturedIndex);
        line.AddChild(button);
        _buttons.Add(button);
    }

    private void Refresh()
    {
        if (_sandbox is null)
        {
            return;
        }

        var choices = _sandbox.GetRouteChoices();
        RebuildRouteRows(choices);
        if (_header is not null)
        {
            _header.Text = $"从 {_sandbox.TripState.CurrentLocation} 出发";
        }
    }

    private void RebuildRouteRows(IReadOnlyList<RouteChoice> choices)
    {
        if (_routeList is null)
        {
            return;
        }

        foreach (var child in _routeList.GetChildren())
        {
            child.QueueFree();
        }

        _rows.Clear();
        _buttons.Clear();
        _titles.Clear();
        _previews.Clear();

        for (var index = 0; index < choices.Count; index++)
        {
            AddRouteRow(_routeList, choices[index], index);
        }
    }

    private static string FormatRoutePreview(RouteChoice choice) =>
        $"{choice.DistanceKm:0} km / {FormatRoadType(choice.RoadType)} / {FormatRisk(choice.Risk)} / {FormatWeather(choice.ExpectedWeather)} / {FormatServices(choice.Services)}";

    private void SelectRoute(int index)
    {
        if (_sandbox is null)
        {
            return;
        }

        var result = _sandbox.SelectRoute(index);
        if (_status is not null)
        {
            _status.Text = result.Message;
        }

        SetOpen(false);
    }

    private void UpdateLayout()
    {
        if (_rootPanel is null)
        {
            return;
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        _rootPanel.Position = new Vector2(
            Mathf.Max(18f, viewportSize.X - _rootPanel.CustomMinimumSize.X - 18f),
            72f);
    }

    private static string FormatRisk(RouteRisk risk) => risk switch
    {
        RouteRisk.Low => "低风险",
        RouteRisk.Medium => "中风险",
        RouteRisk.High => "高风险",
        _ => "未知风险"
    };

    private static string FormatWeather(TripWeather weather) => weather switch
    {
        TripWeather.Clear => "晴天",
        TripWeather.Rain => "雨天",
        TripWeather.Fog => "雾天",
        TripWeather.Storm => "暴雨",
        TripWeather.Snow => "雪天",
        _ => "未知天气"
    };

    private static string FormatRoadType(RoadType roadType) => roadType switch
    {
        RoadType.Highway => "公路",
        RoadType.RuralRoad => "郊外路",
        RoadType.MountainRoad => "山路",
        RoadType.TownRoad => "城郊路",
        RoadType.OldRoad => "旧路",
        _ => "道路"
    };

    private static string FormatServices(IReadOnlyList<PlaceService> services) =>
        string.Join(" · ", services.Select(service => service switch
        {
            PlaceService.Fuel => "加油",
            PlaceService.Motel => "住宿",
            PlaceService.Repair => "维修",
            PlaceService.Shop => "补给",
            PlaceService.Rest => "休息",
            _ => "服务"
        }));
}
