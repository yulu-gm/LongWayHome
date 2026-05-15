extends SceneTree

var _frames := 0
var _car: RigidBody3D

func _initialize() -> void:
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

	for action in ["drive_throttle", "drive_brake", "drive_steer_left", "drive_steer_right", "drive_handbrake"]:
		if not InputMap.has_action(action):
			InputMap.add_action(action)

	Input.action_press("drive_throttle")

func _process(_delta: float) -> bool:
	_frames += 1
	if _frames == 120:
		var speed_kph := _car.linear_velocity.length() * 3.6
		if speed_kph < 8.0:
			push_error("Vehicle did not accelerate enough. Speed: %.2f km/h" % speed_kph)
			quit(1)
			return true
		Input.action_release("drive_throttle")
		Input.action_press("drive_steer_right")
		Input.action_press("drive_handbrake")

	if _frames == 210:
		Input.action_release("drive_steer_right")
		Input.action_release("drive_handbrake")
		quit(0)

	return false
