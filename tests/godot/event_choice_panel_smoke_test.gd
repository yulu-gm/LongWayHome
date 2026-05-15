extends SceneTree

var _frames := 0
var _root: Node
var _panel: CanvasLayer
var _hud: CanvasLayer

func _initialize() -> void:
	var scene := load("res://scenes/DriveSandbox.tscn")
	if scene == null:
		push_error("DriveSandbox scene failed to load.")
		quit(1)
		return

	_root = scene.instantiate()
	get_root().add_child(_root)
	_panel = _root.get_node_or_null("EventChoicePanel") as CanvasLayer
	_hud = _root.get_node_or_null("DrivingHud") as CanvasLayer

func _process(_delta: float) -> bool:
	_frames += 1
	if _frames < 8:
		return false

	if _panel == null:
		push_error("EventChoicePanel was not found.")
		quit(1)
		return true
	if _hud == null:
		push_error("DrivingHud was not found.")
		quit(1)
		return true

	var panel_root := _panel.find_child("EventChoiceRoot", true, false) as PanelContainer
	if panel_root == null:
		push_error("EventChoicePanel is missing EventChoiceRoot.")
		quit(1)
		return true
	if panel_root.visible:
		push_error("EventChoicePanel should be hidden until an event is shown.")
		quit(1)
		return true

	_panel.call("ShowSampleEvent")
	await process_frame
	await process_frame

	if not panel_root.visible:
		push_error("EventChoicePanel should become visible after ShowSampleEvent.")
		quit(1)
		return true

	var title := _panel.find_child("EventTitle", true, false) as Label
	var body := _panel.find_child("EventBody", true, false) as Label
	var first_button := _panel.find_child("EventChoiceButton_0", true, false) as Button
	if title == null or title.text.strip_edges().is_empty():
		push_error("EventChoicePanel should render an event title.")
		quit(1)
		return true
	if body == null or body.text.strip_edges().is_empty():
		push_error("EventChoicePanel should render event body text.")
		quit(1)
		return true
	if first_button == null:
		push_error("EventChoicePanel should render event choice buttons.")
		quit(1)
		return true

	first_button.emit_signal("pressed")
	await process_frame
	await process_frame

	if panel_root.visible:
		push_error("EventChoicePanel should close after choosing an option.")
		quit(1)
		return true

	var money_label := _hud.find_child("MoneyValue", true, false) as Label
	if money_label == null or money_label.text != "$650":
		push_error("Choosing the sample event reward should update DrivingHud money to $650. Got: %s" % (money_label.text if money_label != null else "<missing>"))
		quit(1)
		return true

	quit(0)
	return true
