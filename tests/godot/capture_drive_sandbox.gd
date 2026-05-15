extends SceneTree

const OUTPUT_PATH := "C:/Users/yulu/Documents/godot-v-2/tmp/drive_sandbox_smoke.png"

var _frames := 0

func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute("C:/Users/yulu/Documents/godot-v-2/tmp")
	var scene := load("res://scenes/DriveSandbox.tscn")
	if scene == null:
		push_error("DriveSandbox scene failed to load.")
		quit(1)
		return

	var root: Node = scene.instantiate()
	get_root().add_child(root)

func _process(_delta: float) -> bool:
	_frames += 1
	if _frames >= 30:
		var image := get_root().get_texture().get_image()
		var err := image.save_png(OUTPUT_PATH)
		if err != OK:
			push_error("Failed to save screenshot: %s" % err)
			quit(1)
			return true
		print("Saved screenshot: %s" % OUTPUT_PATH)
		quit(0)
	return false
