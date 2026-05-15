using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot_V2.Scripts.Places;
using Godot_V2.Scripts.Sandbox;
using Godot_V2.Scripts.World;

namespace Godot_V2.Scripts.Ui;

public partial class ServicePanel : CanvasLayer
{
    [Export] public NodePath? SandboxPath { get; set; }
    [Export] public string DefaultPlaceId { get; set; } = "old-mill-gas";

    private readonly Dictionary<PlaceService, Button> _buttons = new();
    private readonly Dictionary<PlaceService, Label> _previews = new();
    private readonly Dictionary<PlaceService, Label> _descriptions = new();
    private DriveSandbox? _sandbox;
    private PlaceDefinition? _place;
    private PanelContainer? _rootPanel;
    private Label? _placeTitle;
    private Label? _placeSubtitle;
    private Label? _statusLabel;

    public override void _Ready()
    {
        Layer = 13;
        _sandbox = SandboxPath is not null && !SandboxPath.IsEmpty
            ? GetNodeOrNull<DriveSandbox>(SandboxPath)
            : GetParent() as DriveSandbox;
        _place = _sandbox?.CurrentPlace
            ?? PlaceCatalog.CreateDefaults().First(place => place.Id == DefaultPlaceId);
        BuildPanel();
        Refresh();
        SetOpen(false);
    }

    public override void _Process(double delta)
    {
        _ = delta;
        _sandbox ??= GetParent() as DriveSandbox;
        if (Input.IsActionJustPressed("toggle_service_panel"))
        {
            SetOpen(_rootPanel is not null && !_rootPanel.Visible);
        }

        UpdateLayout();
        if (_rootPanel?.Visible == true)
        {
            Refresh();
        }
    }

    public void SetPreviewTripMoney(int money)
    {
        (_sandbox?.TripState ?? throw new System.InvalidOperationException("ServicePanel needs a DriveSandbox TripState."))
            .SetMoney(money);
        Refresh();
    }

    public void ShowForPlaceId(string placeId)
    {
        var nextPlace = PlaceCatalog.CreateDefaults()
            .FirstOrDefault(place => place.Id == placeId);
        if (nextPlace is not null)
        {
            _place = nextPlace;
        }

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
            Name = "ServicePanelRoot",
            CustomMinimumSize = new Vector2(430f, 328f)
        };
        _rootPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.025f, 0.03f, 0.032f, 0.86f),
            BorderColor = new Color(1f, 0.72f, 0.28f, 0.42f),
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

        _placeTitle = new Label
        {
            Name = "PlaceTitle",
            Text = "服务点",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _placeTitle.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.62f, 1f));
        _placeTitle.AddThemeFontSizeOverride("font_size", 21);
        root.AddChild(_placeTitle);

        _placeSubtitle = new Label
        {
            Name = "PlaceSubtitle",
            Text = "停车后可使用服务",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _placeSubtitle.AddThemeColorOverride("font_color", new Color(0.82f, 0.8f, 0.72f, 0.96f));
        _placeSubtitle.AddThemeFontSizeOverride("font_size", 13);
        root.AddChild(_placeSubtitle);

        foreach (var service in new[]
        {
            PlaceService.Fuel,
            PlaceService.Motel,
            PlaceService.Repair,
            PlaceService.Shop,
            PlaceService.Rest
        })
        {
            AddServiceRow(root, service);
        }

        _statusLabel = new Label
        {
            Name = "ServiceStatus",
            Text = "选择服务前先查看资源变化。",
            CustomMinimumSize = new Vector2(390f, 22f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _statusLabel.AddThemeColorOverride("font_color", new Color(1f, 0.75f, 0.3f, 1f));
        _statusLabel.AddThemeFontSizeOverride("font_size", 13);
        root.AddChild(_statusLabel);
    }

    private void AddServiceRow(VBoxContainer parent, PlaceService service)
    {
        var row = new PanelContainer { Name = $"ServiceRow_{service}" };
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

        var line = new HBoxContainer();
        line.AddThemeConstantOverride("separation", 10);
        row.AddChild(line);

        var copy = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        copy.AddThemeConstantOverride("separation", 3);
        line.AddChild(copy);

        var title = new Label { Name = $"ServiceTitle_{service}" };
        title.AddThemeColorOverride("font_color", new Color(0.97f, 0.9f, 0.76f, 1f));
        title.AddThemeFontSizeOverride("font_size", 15);
        copy.AddChild(title);
        _descriptions[service] = title;

        var preview = new Label
        {
            Name = $"ServicePreview_{service}",
            Text = "-",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        preview.AddThemeColorOverride("font_color", new Color(0.82f, 0.8f, 0.72f, 0.95f));
        preview.AddThemeFontSizeOverride("font_size", 12);
        copy.AddChild(preview);
        _previews[service] = preview;

        var button = new Button
        {
            Name = $"ServiceButton_{service}",
            Text = "执行",
            CustomMinimumSize = new Vector2(72f, 34f)
        };
        button.Pressed += () => ApplyService(service);
        line.AddChild(button);
        _buttons[service] = button;
    }

    private void Refresh()
    {
        if (_sandbox is null || _place is null)
        {
            return;
        }

        if (_placeTitle is not null)
        {
            _placeTitle.Text = _place.Name;
        }

        if (_placeSubtitle is not null)
        {
            _placeSubtitle.Text = $"{FormatPlaceType(_place.Type)} / ${_sandbox.TripState.Money} / {FormatServiceList(_place)}";
        }

        foreach (var service in _buttons.Keys)
        {
            var preview = PlaceServiceResolver.CreatePreview(_sandbox.TripState, _place, service);
            _descriptions[service].Text = preview.Title;
            _previews[service].Text = preview.ChangePreview;
            _buttons[service].Disabled = !preview.CanExecute;
            _buttons[service].Text = preview.CanExecute ? $"${preview.Price}" : "不可用";
        }
    }

    private void ApplyService(PlaceService service)
    {
        if (_sandbox is null || _place is null)
        {
            return;
        }

        var result = PlaceServiceResolver.Apply(_sandbox.TripState, _place, service);
        if (_statusLabel is not null)
        {
            _statusLabel.Text = result.Message;
        }

        if (result.Applied)
        {
            SetOpen(false);
            GetParent()?.GetNodeOrNull<RouteSelectionPanel>("RouteSelectionPanel")?.OpenForCurrentLocation();
        }

        Refresh();
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

    private static string FormatPlaceType(PlaceType type) => type switch
    {
        PlaceType.FuelStation => "加油站",
        PlaceType.Motel => "汽车旅馆",
        PlaceType.RepairShop => "修理铺",
        PlaceType.RestArea => "休息区",
        PlaceType.RoadsideShop => "路边商店",
        PlaceType.Town => "小镇服务点",
        _ => "服务点"
    };

    private static string FormatServiceList(PlaceDefinition place) =>
        string.Join(" · ", place.Services.Select(offer => offer.Service switch
        {
            PlaceService.Fuel => "加油",
            PlaceService.Motel => "住宿",
            PlaceService.Repair => "维修",
            PlaceService.Shop => "补给",
            PlaceService.Rest => "休息",
            _ => "服务"
        }));
}
