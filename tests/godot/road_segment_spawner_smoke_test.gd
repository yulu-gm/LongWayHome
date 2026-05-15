extends SceneTree

var _frames := 0
var _spawner: Node3D

func _initialize() -> void:
	var scene := load("res://scenes/DriveSandbox.tscn")
	if scene == null:
		push_error("DriveSandbox scene failed to load.")
		quit(1)
		return

	var root: Node = scene.instantiate()
	get_root().add_child(root)
	_spawner = root.get_node_or_null("RoadSegmentSpawner") as Node3D

func _process(_delta: float) -> bool:
	_frames += 1
	if _frames < 8:
		return false

	if _spawner == null:
		push_error("RoadSegmentSpawner was not found.")
		quit(1)
		return true

	var spawned_segments := _spawner.get_node_or_null("SpawnedSegments")
	if spawned_segments == null:
		push_error("RoadSegmentSpawner is missing SpawnedSegments container.")
		quit(1)
		return true
	if spawned_segments.get_child_count() < 3:
		push_error("RoadSegmentSpawner should preload at least three visible road segments.")
		quit(1)
		return true

	var first_segment := spawned_segments.get_child(0)
	if first_segment.find_child("RoadCollision", true, false) == null:
		push_error("Spawned road segment is missing RoadCollision.")
		quit(1)
		return true
	if first_segment.find_child("RoadMesh", true, false) == null:
		push_error("Spawned road segment is missing RoadMesh.")
		quit(1)
		return true

	var roadside_prop_count := _count_roadside_props(spawned_segments)
	if roadside_prop_count < 6:
		push_error("RoadSegmentSpawner should add roadside trees, rocks, signs, poles, or guardrails. Found: %s" % roadside_prop_count)
		quit(1)
		return true

	if not _curved_segment_has_rotated_center_stripes(spawned_segments):
		push_error("Curved road center stripes should align to sampled road tangents instead of keeping one fixed direction.")
		quit(1)
		return true

	quit(0)
	return true

func _count_roadside_props(root: Node) -> int:
	var count := 0
	var stack: Array[Node] = [root]
	while not stack.is_empty():
		var node: Node = stack.pop_back()
		var node_name := String(node.name)
		if (
			node_name.contains("Tree")
			or node_name.contains("Rock")
			or node_name.contains("Sign")
			or node_name.contains("Guardrail")
			or node_name.contains("UtilityPole")
		):
			count += 1
		for child_value in node.get_children():
			var child: Node = child_value as Node
			if child != null:
				stack.push_back(child)
	return count

func _curved_segment_has_rotated_center_stripes(root: Node) -> bool:
	var curved_segment: Node = null
	for segment_value in root.get_children():
		var segment: Node = segment_value as Node
		if segment != null and String(segment.name).contains("s_curve"):
			curved_segment = segment
			break
	if curved_segment == null:
		return false

	var min_basis_z_x := INF
	var max_basis_z_x := -INF
	var stripe_count := 0
	for child_value in curved_segment.get_children():
		var child := child_value as Node3D
		if child != null and String(child.name).begins_with("CenterStripe_"):
			min_basis_z_x = min(min_basis_z_x, child.transform.basis.z.x)
			max_basis_z_x = max(max_basis_z_x, child.transform.basis.z.x)
			stripe_count += 1

	return stripe_count >= 2 and max_basis_z_x - min_basis_z_x > 0.02
