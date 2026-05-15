extends SceneTree

var _frames := 0
var _root: Node
var _service_panel: CanvasLayer
var _route_panel: CanvasLayer

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

func _process(_delta: float) -> bool:
	_frames += 1
	if _frames < 8:
		return false

	if _service_panel == null:
		push_error("ServicePanel was not found.")
		quit(1)
		return true
	if _route_panel == null:
		push_error("RouteSelectionPanel was not found.")
		quit(1)
		return true

	var route_root := _route_panel.find_child("RouteSelectionRoot", true, false) as PanelContainer
	if route_root == null:
		push_error("RouteSelectionPanel is missing RouteSelectionRoot.")
		quit(1)
		return true
	if route_root.visible:
		push_error("RouteSelectionPanel should be hidden before service completion.")
		quit(1)
		return true

	_service_panel.call("SetOpen", true)
	await process_frame
	var fuel_button := _service_panel.find_child("ServiceButton_Fuel", true, false) as Button
	if fuel_button == null or fuel_button.disabled:
		push_error("Fuel service should be available before opening route selection.")
		quit(1)
		return true

	fuel_button.emit_signal("pressed")
	await process_frame
	if not route_root.visible:
		push_error("RouteSelectionPanel should open after a service is applied.")
		quit(1)
		return true

	var route_buttons := _route_panel.find_children("RouteButton_*", "Button", true, false)
	if route_buttons.size() < 2 or route_buttons.size() > 3:
		push_error("RouteSelectionPanel should render 2 to 3 route choices. Found: %s" % route_buttons.size())
		quit(1)
		return true

	var visible_route_rows := 0
	for row_value in _route_panel.find_children("RouteRow_*", "PanelContainer", true, false):
		var row := row_value as PanelContainer
		if row != null and row.visible:
			var title := row.find_child("RouteTitle_*", true, false) as Label
			var row_preview := row.find_child("RoutePreview_*", true, false) as Label
			if title == null or title.text.strip_edges().is_empty():
				push_error("Visible route rows must not render as empty bordered placeholders.")
				quit(1)
				return true
			if row_preview == null or row_preview.text.strip_edges().is_empty():
				push_error("Visible route rows must include a route preview.")
				quit(1)
				return true
			visible_route_rows += 1
	if visible_route_rows < 2 or visible_route_rows > 3:
		push_error("RouteSelectionPanel should show 2 to 3 visible route rows. Found: %s" % visible_route_rows)
		quit(1)
		return true

	var route_preview := _route_panel.find_child("RoutePreview_0", true, false) as Label
	if route_preview == null or not route_preview.text.contains("km"):
		push_error("RouteSelectionPanel should show route distance preview.")
		quit(1)
		return true

	var first_button := route_buttons[0] as Button
	first_button.emit_signal("pressed")
	await process_frame
	if route_root.visible:
		push_error("RouteSelectionPanel should close after selecting a route.")
		quit(1)
		return true

	quit(0)
	return true
