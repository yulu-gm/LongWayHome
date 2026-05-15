extends SceneTree

var _frames := 0
var _hud: CanvasLayer
var _debug_panel: CanvasLayer

func _initialize() -> void:
	var scene := load("res://scenes/DriveSandbox.tscn")
	if scene == null:
		push_error("DriveSandbox scene failed to load.")
		quit(1)
		return

	var root: Node = scene.instantiate()
	get_root().add_child(root)
	_hud = root.get_node_or_null("DrivingHud") as CanvasLayer
	_debug_panel = root.get_node_or_null("VehicleDebugPanel") as CanvasLayer

func _process(_delta: float) -> bool:
	_frames += 1
	if _frames < 4:
		return false

	if _hud == null:
		push_error("DrivingHud was not found.")
		quit(1)
		return true
	if _debug_panel == null:
		push_error("VehicleDebugPanel was not found.")
		quit(1)
		return true

	var required_labels := [
		"TimeValue",
		"WeatherValue",
		"FuelValue",
		"ConditionValue",
		"EnergyValue",
		"MoneyValue",
		"DistanceValue",
		"SpeedValue",
		"WarningValue",
	]
	for label_name in required_labels:
		var label := _hud.find_child(label_name, true, false) as Label
		if label == null:
			push_error("DrivingHud is missing label: %s" % label_name)
			quit(1)
			return true
		if label.text.strip_edges().is_empty():
			push_error("DrivingHud label is empty: %s" % label_name)
			quit(1)
			return true

	var root_panel := _hud.find_child("HudRoot", true, false) as PanelContainer
	if root_panel == null:
		push_error("DrivingHud is missing HudRoot panel.")
		quit(1)
		return true
	if root_panel.position.x > 40.0 or root_panel.position.y < 300.0:
		push_error("DrivingHud should stay compact near the lower-left driving HUD area.")
		quit(1)
		return true

	var debug_root := _debug_panel.get_child(0) as PanelContainer
	if debug_root == null:
		push_error("VehicleDebugPanel did not create a PanelContainer child.")
		quit(1)
		return true
	if debug_root.visible:
		push_error("VehicleDebugPanel should be hidden by default now that DrivingHud is the player HUD.")
		quit(1)
		return true

	quit(0)
	return true
