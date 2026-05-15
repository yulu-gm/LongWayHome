using System.Collections.Generic;
using Godot;
using Godot_V2.Scripts.Events;
using Godot_V2.Scripts.Sandbox;

namespace Godot_V2.Scripts.Ui;

public partial class EventChoicePanel : CanvasLayer
{
    [Export] public NodePath? SandboxPath { get; set; }

    private readonly List<Button> _choiceButtons = [];
    private readonly List<Label> _choicePreviews = [];
    private DriveSandbox? _sandbox;
    private RoadEventDefinition? _event;
    private PanelContainer? _rootPanel;
    private VBoxContainer? _choiceList;
    private Label? _title;
    private Label? _body;
    private Label? _status;

    public override void _Ready()
    {
        Layer = 15;
        _sandbox = SandboxPath is not null && !SandboxPath.IsEmpty
            ? GetNodeOrNull<DriveSandbox>(SandboxPath)
            : GetParent() as DriveSandbox;
        BuildPanel();
        SetOpen(false);
    }

    public override void _Process(double delta)
    {
        _ = delta;
        _sandbox ??= GetParent() as DriveSandbox;
        UpdateLayout();
    }

    public void ShowSampleEvent()
    {
        ShowEvent(CreateSampleEvent());
    }

    public void ShowEvent(RoadEventDefinition roadEvent)
    {
        _event = roadEvent;
        Refresh();
        SetOpen(true);
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
            Name = "EventChoiceRoot",
            CustomMinimumSize = new Vector2(470f, 282f)
        };
        _rootPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.026f, 0.03f, 0.032f, 0.9f),
            BorderColor = new Color(1f, 0.72f, 0.28f, 0.46f),
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

        _title = new Label
        {
            Name = "EventTitle",
            Text = "旅途事件"
        };
        _title.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.62f, 1f));
        _title.AddThemeFontSizeOverride("font_size", 21);
        root.AddChild(_title);

        _body = new Label
        {
            Name = "EventBody",
            Text = "",
            CustomMinimumSize = new Vector2(420f, 54f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _body.AddThemeColorOverride("font_color", new Color(0.9f, 0.87f, 0.78f, 0.98f));
        _body.AddThemeFontSizeOverride("font_size", 14);
        root.AddChild(_body);

        _choiceList = new VBoxContainer();
        _choiceList.AddThemeConstantOverride("separation", 7);
        root.AddChild(_choiceList);

        _status = new Label
        {
            Name = "EventStatus",
            Text = "选择一个回应。",
            CustomMinimumSize = new Vector2(420f, 22f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _status.AddThemeColorOverride("font_color", new Color(1f, 0.75f, 0.3f, 1f));
        _status.AddThemeFontSizeOverride("font_size", 13);
        root.AddChild(_status);
    }

    private void Refresh()
    {
        if (_event is null || _choiceList is null)
        {
            return;
        }

        if (_title is not null)
        {
            _title.Text = _event.Title;
        }

        if (_body is not null)
        {
            _body.Text = _event.SceneText;
        }

        foreach (var child in _choiceList.GetChildren())
        {
            child.QueueFree();
        }

        _choiceButtons.Clear();
        _choicePreviews.Clear();
        for (var index = 0; index < _event.Choices.Count; index++)
        {
            AddChoiceRow(_choiceList, _event.Choices[index], index);
        }
    }

    private void AddChoiceRow(VBoxContainer parent, RoadEventChoice choice, int index)
    {
        var line = new HBoxContainer { Name = $"EventChoiceRow_{index}" };
        line.AddThemeConstantOverride("separation", 10);
        parent.AddChild(line);

        var preview = new Label
        {
            Name = $"EventChoicePreview_{index}",
            Text = choice.Text,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(320f, 34f),
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        preview.AddThemeColorOverride("font_color", new Color(0.9f, 0.86f, 0.76f, 1f));
        preview.AddThemeFontSizeOverride("font_size", 14);
        line.AddChild(preview);
        _choicePreviews.Add(preview);

        var button = new Button
        {
            Name = $"EventChoiceButton_{index}",
            Text = "选择",
            CustomMinimumSize = new Vector2(72f, 34f)
        };
        var capturedChoice = choice;
        button.Pressed += () => ApplyChoice(capturedChoice);
        line.AddChild(button);
        _choiceButtons.Add(button);
    }

    private void ApplyChoice(RoadEventChoice choice)
    {
        if (_sandbox is null)
        {
            return;
        }

        var result = RoadEventChoiceResolver.ApplyChoice(_sandbox.TripState, choice);
        if (_status is not null)
        {
            _status.Text = result.Message;
        }

        if (result.Applied)
        {
            SetOpen(false);
        }
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
            Mathf.Max(72f, viewportSize.Y - _rootPanel.CustomMinimumSize.Y - 42f));
    }

    private static RoadEventDefinition CreateSampleEvent() =>
        new(
            Id: "sample-broken-car",
            Title: "路边抛锚的旅行者",
            SceneText: "一辆旧车停在路肩，司机举着手电向你示意。雨后的路面还反着光，他看起来已经等了一阵。",
            Category: RoadEventCategory.Road,
            Weight: 1,
            CooldownLegs: 1,
            IsOneTime: false,
            Tags: ["road", "traveler"],
            Trigger: RoadEventTrigger.Any,
            Choices:
            [
                new RoadEventChoice(
                    "help",
                    "停下帮忙，得到 $30，但消耗一点精力。",
                    [
                        new RoadEventEffect(RoadEventEffectKind.Money, 30),
                        new RoadEventEffect(RoadEventEffectKind.Energy, -5),
                        new RoadEventEffect(RoadEventEffectKind.TimeMinutes, 20),
                        new RoadEventEffect(RoadEventEffectKind.Flag, 1, "helped-stranded-driver")
                    ]),
                new RoadEventChoice(
                    "leave",
                    "继续赶路，不冒额外风险。",
                    [new RoadEventEffect(RoadEventEffectKind.TimeMinutes, 5)])
            ]);
}
