extends SceneTree

var _frames := 0
var _panel: CanvasLayer

func _initialize() -> void:
	if not InputMap.has_action("toggle_service_panel"):
		InputMap.add_action("toggle_service_panel")

	var scene := load("res://scenes/DriveSandbox.tscn")
	if scene == null:
		push_error("DriveSandbox scene failed to load.")
		quit(1)
		return

	var root: Node = scene.instantiate()
	get_root().add_child(root)
	_panel = root.get_node_or_null("ServicePanel") as CanvasLayer

func _process(_delta: float) -> bool:
	_frames += 1
	if _frames < 6:
		return false

	if _panel == null:
		push_error("ServicePanel was not found.")
		quit(1)
		return true

	var root_panel := _panel.find_child("ServicePanelRoot", true, false) as PanelContainer
	if root_panel == null:
		push_error("ServicePanel is missing ServicePanelRoot.")
		quit(1)
		return true
	if root_panel.visible:
		push_error("ServicePanel should be hidden until the player opens services.")
		quit(1)
		return true

	Input.action_press("toggle_service_panel")
	await process_frame
	Input.action_release("toggle_service_panel")
	await process_frame

	if not root_panel.visible:
		push_error("ServicePanel should open from the service toggle action.")
		quit(1)
		return true

	var title := _panel.find_child("PlaceTitle", true, false) as Label
	if title == null or title.text.strip_edges().is_empty():
		push_error("ServicePanel should show the current place title.")
		quit(1)
		return true

	var fuel_button := _panel.find_child("ServiceButton_Fuel", true, false) as Button
	var preview := _panel.find_child("ServicePreview_Fuel", true, false) as Label
	if fuel_button == null or preview == null:
		push_error("ServicePanel should render fuel service button and preview.")
		quit(1)
		return true
	if fuel_button.disabled:
		push_error("Fuel service should be executable for the default sandbox trip state.")
		quit(1)
		return true
	if not preview.text.contains("$") or not preview.text.contains("L"):
		push_error("Fuel preview should include price and fuel delta.")
		quit(1)
		return true

	_panel.call("SetPreviewTripMoney", 1)
	await process_frame
	if not fuel_button.disabled:
		push_error("Fuel service should be disabled when money is insufficient.")
		quit(1)
		return true
	if not preview.text.contains("金钱不足"):
		push_error("Disabled fuel preview should explain insufficient money.")
		quit(1)
		return true

	quit(0)
	return true
