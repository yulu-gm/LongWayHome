extends SceneTree

var _mode := "right"
var _frames := 0
var _car: RigidBody3D
var _initial_forward := Vector3.ZERO
var _initial_right := Vector3.ZERO
var _right_dot := 0.0

func _initialize() -> void:
	_mode = "left" if OS.get_cmdline_user_args().has("--left") else "right"
	for action in ["drive_throttle", "drive_steer_left", "drive_steer_right"]:
		if not InputMap.has_action(action):
			InputMap.add_action(action)

	var scene := load("res://scenes/DriveSandbox.tscn")
	if scene == null:
		push_error("DriveSandbox scene failed to load.")
		quit(1)
		return

	var root: Node = scene.instantiate()
	get_root().add_child(root)
	_car = root.get_node("PrototypeCar") as RigidBody3D
	if _car == null:
		push_error("PrototypeCar was not found or is not a RigidBody3D.")
		quit(1)
		return

	Input.action_press("drive_throttle")
	if _mode == "left":
		Input.action_press("drive_steer_left")
	else:
		Input.action_press("drive_steer_right")

func _process(_delta: float) -> bool:
	_frames += 1
	if _frames == 1:
		_initial_forward = -_car.global_transform.basis.z.normalized()
		_initial_right = _car.global_transform.basis.x.normalized()

	if _frames >= 180:
		var final_forward := -_car.global_transform.basis.z.normalized()
		_right_dot = final_forward.dot(_initial_right)
		if _mode == "right" and _right_dot <= 0.04:
			push_error("D should steer toward initial right. right_dot=%.4f" % _right_dot)
			quit(1)
			return true
		if _mode == "left" and _right_dot >= -0.04:
			push_error("A should steer toward initial left. right_dot=%.4f" % _right_dot)
			quit(1)
			return true
		quit(0)
	return false
