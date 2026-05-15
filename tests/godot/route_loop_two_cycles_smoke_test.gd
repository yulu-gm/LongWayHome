extends SceneTree

var _frames := 0
var _cycle := 0
var _stage := 0
var _wait_frames := 0
var _expected_distance := ""
var _root: Node
var _service_panel: CanvasLayer
var _route_panel: CanvasLayer
var _hud: CanvasLayer

func _initialize() -> void:
	var scene := load("res://scenes/DriveSandbox.tscn")
	if scene == null:
		push_error("DriveSandbox scene failed to load.")
		quit(1)
		return

	_root = scene.instantiate()
	get_root().add_child(_root)
	_service_panel = _root.get_node_or_null("ServicePanel") as CanvasLayer
	_route_panel = _root.get_node_or_null("RouteSelectionPanel") as CanvasLayer
	_hud = _root.get_node_or_null("DrivingHud") as CanvasLayer

func _process(_delta: float) -> bool:
	_frames += 1
	if _frames < 8:
		return false

	if _service_panel == null or _route_panel == null or _hud == null:
		push_error("DriveSandbox should include service, route selection, and driving HUD panels.")
		quit(1)
		return true

	if _wait_frames > 0:
		_wait_frames -= 1
		return false

	match _stage:
		0:
			_service_panel.call("SetOpen", true)
			_wait_frames = 2
			_stage = 1
		1:
			var service_button := _find_first_enabled_service_button()
			if service_button == null:
				push_error("Expected at least one executable service before route selection.")
				quit(1)
				return true
			service_button.emit_signal("pressed")
			_wait_frames = 2
			_stage = 2
		2:
			var route_root := _route_panel.find_child("RouteSelectionRoot", true, false) as PanelContainer
			if route_root == null or not route_root.visible:
				push_error("Route selection should open after service completion.")
				quit(1)
				return true

			var first_route_button := _route_panel.find_child("RouteButton_0", true, false) as Button
			var first_route_preview := _route_panel.find_child("RoutePreview_0", true, false) as Label
			if first_route_button == null or first_route_preview == null:
				push_error("Route selection should expose the first route button and preview.")
				quit(1)
				return true

			_expected_distance = _extract_distance_text(first_route_preview.text)
			if _expected_distance.is_empty():
				push_error("Route preview should begin with a distance.")
				quit(1)
				return true

			first_route_button.emit_signal("pressed")
			_wait_frames = 2
			_stage = 3
		3:
			var route_root := _route_panel.find_child("RouteSelectionRoot", true, false) as PanelContainer
			if route_root != null and route_root.visible:
				push_error("Route selection should close after choosing a route.")
				quit(1)
				return true

			var hud_distance := _hud.find_child("DistanceValue", true, false) as Label
			if hud_distance == null or hud_distance.text != _expected_distance:
				push_error("Driving HUD should show selected route distance. Expected %s, got %s" % [_expected_distance, hud_distance.text if hud_distance != null else "<missing>"])
				quit(1)
				return true

			_cycle += 1
			if _cycle >= 2:
				quit(0)
				return true
			_stage = 0

	return false

func _find_first_enabled_service_button() -> Button:
	for service in ["Fuel", "Motel", "Repair", "Shop", "Rest"]:
		var button := _service_panel.find_child("ServiceButton_%s" % service, true, false) as Button
		if button != null and not button.disabled:
			return button
	return null

func _extract_distance_text(preview: String) -> String:
	var marker := " km"
	var marker_index := preview.find(marker)
	if marker_index < 0:
		return ""
	return preview.substr(0, marker_index) + marker
