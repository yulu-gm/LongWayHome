extends SceneTree

var _frames := 0
var _panel_container: PanelContainer

func _initialize() -> void:
	if not InputMap.has_action("toggle_debug_panel"):
		InputMap.add_action("toggle_debug_panel")

	var scene := load("res://scenes/DriveSandbox.tscn")
	if scene == null:
		push_error("DriveSandbox scene failed to load.")
		quit(1)
		return

	var root: Node = scene.instantiate()
	get_root().add_child(root)
	var debug_panel := root.get_node("VehicleDebugPanel")
	if debug_panel == null:
		push_error("VehicleDebugPanel was not found.")
		quit(1)
		return

	await process_frame
	_panel_container = debug_panel.get_child(0) as PanelContainer
	if _panel_container == null:
		push_error("VehicleDebugPanel did not create a PanelContainer child.")
		quit(1)
		return
	if _panel_container.visible:
		push_error("VehicleDebugPanel should be hidden by default.")
		quit(1)
		return

	Input.action_press("toggle_debug_panel")

func _process(_delta: float) -> bool:
	_frames += 1
	if _frames == 3:
		Input.action_release("toggle_debug_panel")
		if not _panel_container.visible:
			push_error("VehicleDebugPanel should show after toggle.")
			quit(1)
			return true
		Input.action_press("toggle_debug_panel")

	if _frames == 6:
		Input.action_release("toggle_debug_panel")
		if _panel_container.visible:
			push_error("VehicleDebugPanel should hide after second toggle.")
			quit(1)
			return true
		quit(0)

	return false
