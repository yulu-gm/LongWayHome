extends SceneTree

var _frames := 0
var _root: Node

func _initialize() -> void:
	var scene := load("res://scenes/DriveSandbox.tscn")
	if scene == null:
		push_error("DriveSandbox scene failed to load.")
		quit(1)
		return

	_root = scene.instantiate()
	get_root().add_child(_root)

func _process(_delta: float) -> bool:
	_frames += 1
	if _frames == 4:
		var event_panel := _root.get_node_or_null("EventChoicePanel")
		if event_panel == null:
			push_error("EventChoicePanel was not found.")
			quit(1)
			return true
		event_panel.call("ShowSampleEvent")

	if _frames < 10:
		return false

	var image := get_root().get_texture().get_image()
	var path := "res://tmp/event_choice_panel_smoke.png"
	var error := image.save_png(path)
	if error != OK:
		push_error("Failed to save event choice screenshot: %s" % error)
		quit(1)
		return true

	print("Saved screenshot: %s" % ProjectSettings.globalize_path(path))
	quit(0)
	return true
