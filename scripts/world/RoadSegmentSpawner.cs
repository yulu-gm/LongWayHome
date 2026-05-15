using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Godot_V2.Scripts.World;

public partial class RoadSegmentSpawner : Node3D
{
    private const float SegmentSurfaceY = 0.025f;

    private readonly Dictionary<int, Node3D> _activeSegments = new();
    private readonly IReadOnlyList<RoadSegmentSpec> _catalog = RoadSegmentCatalog.CreateDefaults();
    private Node3D? _segmentsRoot;
    private Node3D? _target;
    private Vector3 _routeOrigin;
    private Vector3 _routeForward;
    private Vector3 _routeRight;
    private float _lastPlannedDistance = float.NegativeInfinity;

    [Export] public NodePath TargetPath { get; set; } = new("");
    [Export] public string RouteSegmentIdList { get; set; } = "rural_straight,gentle_curve,s_curve,old_road_detour,town_entry";
    [Export] public float RouteYawDegrees { get; set; } = 15f;
    [Export] public float PreloadAheadMeters { get; set; } = 360f;
    [Export] public float KeepBehindMeters { get; set; } = 80f;
    [Export] public float InitialBackfillMeters { get; set; } = 70f;
    [Export] public float ReplanDistanceStepMeters { get; set; } = 12f;
    [Export] public float RoadWidthMeters { get; set; } = 9.5f;
    [Export] public float ShoulderWidthMeters { get; set; } = 120f;

    public int ActiveSegmentCount => _activeSegments.Count;

    public override void _Ready()
    {
        AddToGroup("road_segment_spawner");
        SetRouteBasis();
        _routeOrigin = GlobalPosition - _routeForward * InitialBackfillMeters;
        _segmentsRoot = GetNodeOrNull<Node3D>("SpawnedSegments");
        if (_segmentsRoot is null)
        {
            _segmentsRoot = new Node3D { Name = "SpawnedSegments" };
            AddChild(_segmentsRoot);
        }

        _target = ResolveTarget();
        RefreshSegments(force: true);
    }

    public override void _Process(double delta)
    {
        RefreshSegments(force: false);
    }

    private Node3D? ResolveTarget()
    {
        if (!TargetPath.IsEmpty)
        {
            return GetNodeOrNull<Node3D>(TargetPath);
        }

        return GetTree().GetNodesInGroup("player_vehicle").OfType<Node3D>().FirstOrDefault()
            ?? GetParent()?.GetNodeOrNull<Node3D>("PrototypeCar");
    }

    private void SetRouteBasis()
    {
        var yawRadians = Mathf.DegToRad(RouteYawDegrees);
        _routeForward = new Vector3(-MathF.Sin(yawRadians), 0f, -MathF.Cos(yawRadians)).Normalized();
        _routeRight = _routeForward.Cross(Vector3.Up).Normalized();
    }

    private void RefreshSegments(bool force)
    {
        if (_segmentsRoot is null)
        {
            return;
        }

        var playerDistance = GetPlayerDistance();
        if (!force && MathF.Abs(playerDistance - _lastPlannedDistance) < ReplanDistanceStepMeters)
        {
            return;
        }

        _lastPlannedDistance = playerDistance;
        var placements = RoadSegmentSpawnPlanner.CreatePlan(
            _catalog,
            ParseRouteSegmentIds(),
            playerDistance,
            PreloadAheadMeters,
            KeepBehindMeters);
        var activeIndices = placements.Select(placement => placement.SequenceIndex).ToHashSet();

        foreach (var staleIndex in _activeSegments.Keys.Where(index => !activeIndices.Contains(index)).ToArray())
        {
            _activeSegments[staleIndex].QueueFree();
            _activeSegments.Remove(staleIndex);
        }

        foreach (var placement in placements)
        {
            if (_activeSegments.ContainsKey(placement.SequenceIndex))
            {
                continue;
            }

            var segment = CreateSegmentNode(placement);
            _segmentsRoot.AddChild(segment);
            _activeSegments.Add(placement.SequenceIndex, segment);
        }
    }

    private IReadOnlyList<string> ParseRouteSegmentIds()
    {
        var segmentIds = RouteSegmentIdList
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(segmentId => !string.IsNullOrWhiteSpace(segmentId))
            .ToArray();

        return segmentIds.Length > 0 ? segmentIds : ["rural_straight"];
    }

    private float GetPlayerDistance()
    {
        if (_target is null)
        {
            return 0f;
        }

        return MathF.Max(0f, (_target.GlobalPosition - _routeOrigin).Dot(_routeForward));
    }

    private Node3D CreateSegmentNode(RoadSegmentPlacement placement)
    {
        var length = placement.EndDistanceMeters - placement.StartDistanceMeters;
        var centerDistance = placement.StartDistanceMeters + length * 0.5f;
        var segment = new Node3D
        {
            Name = $"RoadSegment_{placement.SequenceIndex:000}_{placement.SegmentId}"
        };

        var segmentBasis = Basis.LookingAt(_routeForward, Vector3.Up);
        segment.GlobalTransform = new Transform3D(segmentBasis, _routeOrigin + _routeForward * centerDistance);
        segment.AddToGroup("spawned_road_segment");
        AddRoadSurface(segment, placement, length);
        AddLaneMarkings(segment, placement, length);
        AddRoadsideProps(segment, placement, length);
        return segment;
    }

    private void AddRoadSurface(Node3D segment, RoadSegmentPlacement placement, float length)
    {
        var roadBody = new StaticBody3D { Name = "RoadBody" };
        segment.AddChild(roadBody);

        var collision = new CollisionShape3D { Name = "RoadCollision" };
        var collisionShape = new BoxShape3D
        {
            Size = new Vector3(RoadWidthMeters + ShoulderWidthMeters * 2f, 0.08f, length)
        };
        collision.Shape = collisionShape;
        collision.Position = new Vector3(0f, SegmentSurfaceY - 0.035f, 0f);
        roadBody.AddChild(collision);

        var roadMesh = new MeshInstance3D
        {
            Name = "RoadMesh",
            Mesh = CreateCurvedStripMesh(placement, length, RoadWidthMeters, 0f),
            MaterialOverride = CreateRoadMaterial(placement.Spec)
        };
        roadBody.AddChild(roadMesh);

        var shoulderWidth = ShoulderWidthMeters;
        var shoulderOffset = RoadWidthMeters * 0.5f + shoulderWidth * 0.5f;
        roadBody.AddChild(CreateShoulderMesh("LeftShoulderMesh", placement, length, shoulderWidth, -shoulderOffset));
        roadBody.AddChild(CreateShoulderMesh("RightShoulderMesh", placement, length, shoulderWidth, shoulderOffset));
    }

    private MeshInstance3D CreateShoulderMesh(
        string name,
        RoadSegmentPlacement placement,
        float length,
        float width,
        float lateralOffset)
    {
        return new MeshInstance3D
        {
            Name = name,
            Mesh = CreateCurvedStripMesh(placement, length, width, lateralOffset),
            MaterialOverride = CreateShoulderMaterial(placement.Spec)
        };
    }

    private Mesh CreateCurvedStripMesh(
        RoadSegmentPlacement placement,
        float length,
        float width,
        float lateralOffset,
        float y = SegmentSurfaceY)
    {
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        var sections = 10;

        for (var index = 0; index < sections; index++)
        {
            var t0 = index / (float)sections;
            var t1 = (index + 1) / (float)sections;
            var left0 = GetStripPoint(placement, width, lateralOffset, t0, y, left: true);
            var right0 = GetStripPoint(placement, width, lateralOffset, t0, y, left: false);
            var left1 = GetStripPoint(placement, width, lateralOffset, t1, y, left: true);
            var right1 = GetStripPoint(placement, width, lateralOffset, t1, y, left: false);

            AddTriangle(surfaceTool, left0, right0, left1);
            AddTriangle(surfaceTool, right0, right1, left1);
        }

        surfaceTool.GenerateNormals();
        return surfaceTool.Commit();
    }

    private static Vector3 GetStripPoint(
        RoadSegmentPlacement placement,
        float width,
        float lateralOffset,
        float t,
        float y,
        bool left)
    {
        var sample = RoadSegmentPathSampler.Sample(placement, t);
        var halfWidth = width * 0.5f;
        var side = left ? -halfWidth : halfWidth;
        var point = RoadSegmentPathSampler.OffsetFromCenter(sample, lateralOffset + side);
        point.Y = y;
        return point;
    }

    private static void AddTriangle(SurfaceTool surfaceTool, Vector3 a, Vector3 b, Vector3 c)
    {
        surfaceTool.SetNormal(Vector3.Up);
        surfaceTool.AddVertex(a);
        surfaceTool.SetNormal(Vector3.Up);
        surfaceTool.AddVertex(b);
        surfaceTool.SetNormal(Vector3.Up);
        surfaceTool.AddVertex(c);
    }

    private void AddLaneMarkings(Node3D segment, RoadSegmentPlacement placement, float length)
    {
        var stripeMaterial = CreateMaterial(new Color(0.96f, 0.68f, 0.16f), roughness: 0.48f, emission: new Color(0.34f, 0.2f, 0.02f));
        var edgeMaterial = CreateMaterial(new Color(0.84f, 0.82f, 0.72f), roughness: 0.62f);
        var dashStep = 11f;
        var edgeOffset = RoadWidthMeters * 0.5f - 0.28f;

        segment.AddChild(new MeshInstance3D
        {
            Name = "LeftEdgeLine",
            Mesh = CreateCurvedStripMesh(placement, length, width: 0.14f, lateralOffset: -edgeOffset, y: SegmentSurfaceY + 0.021f),
            MaterialOverride = edgeMaterial
        });
        segment.AddChild(new MeshInstance3D
        {
            Name = "RightEdgeLine",
            Mesh = CreateCurvedStripMesh(placement, length, width: 0.14f, lateralOffset: edgeOffset, y: SegmentSurfaceY + 0.021f),
            MaterialOverride = edgeMaterial
        });

        for (var distance = 7f; distance < length - 4f; distance += dashStep)
        {
            var t = distance / length;
            var centerStripe = CreateRoadAlignedBoxMeshNode(
                $"CenterStripe_{Mathf.RoundToInt(distance)}",
                new Vector3(0.24f, 0.025f, 4.8f),
                stripeMaterial,
                placement,
                t,
                lateralOffset: 0f,
                y: SegmentSurfaceY + 0.024f);
            segment.AddChild(centerStripe);
        }
    }

    private void AddRoadsideProps(Node3D segment, RoadSegmentPlacement placement, float length)
    {
        var tags = placement.Spec.VisualTags;
        if (tags.Contains("trees"))
        {
            AddTreeRow(segment, placement, length, side: -1f, count: 5);
            AddTreeRow(segment, placement, length, side: 1f, count: 4);
        }

        if (tags.Contains("rocks") || tags.Contains("slope"))
        {
            AddRockCluster(segment, placement, length);
        }

        if (tags.Contains("guardrail"))
        {
            AddGuardrail(segment, placement, length, side: -1f);
        }

        if (tags.Contains("utility-poles"))
        {
            AddUtilityPoles(segment, placement, length, side: 1f);
        }

        if (tags.Contains("signs") || tags.Contains("abandoned-sign"))
        {
            AddRoadSign(segment, placement, placement.Spec.Region == RegionType.Town ? "MOTEL" : "OLD RD");
        }

        if (tags.Contains("river-edge"))
        {
            AddRiverEdge(segment, placement, length, side: -1f);
        }

        if (tags.Contains("crosswalk"))
        {
            AddCrosswalk(segment, placement, length);
        }

        if (tags.Contains("shopfronts"))
        {
            AddShopfronts(segment, placement, length);
        }

        if (tags.Contains("streetlights"))
        {
            AddStreetlights(segment, placement, length);
        }

        if (tags.Contains("shrubs"))
        {
            AddShrubs(segment, placement, length);
        }
    }

    private void AddTreeRow(Node3D segment, RoadSegmentPlacement placement, float length, float side, int count)
    {
        for (var index = 0; index < count; index++)
        {
            var t = (index + 0.5f) / count;
            var lateral = side * (RoadWidthMeters * 0.5f + 7.5f + index % 3 * 3.0f);
            var tree = CreateTree($"Tree_{(side < 0 ? "L" : "R")}_{index}");
            tree.Transform = CreateRoadAlignedTransform(
                placement,
                t,
                lateral,
                SegmentSurfaceY,
                yawOffsetRadians: (index * 0.47f) % MathF.Tau);
            segment.AddChild(tree);
        }
    }

    private Node3D CreateTree(string name)
    {
        var tree = new Node3D { Name = name };
        var trunk = CreateCylinderMeshNode(
            "TreeTrunk",
            radius: 0.18f,
            height: 1.05f,
            CreateMaterial(new Color(0.37f, 0.22f, 0.13f), roughness: 0.92f),
            new Vector3(0f, 0.54f, 0f),
            radialSegments: 6);
        var foliage = CreateConeMeshNode(
            "TreeFoliage",
            radius: 1.05f,
            height: 1.9f,
            CreateMaterial(new Color(0.12f, 0.31f, 0.2f), roughness: 0.88f),
            new Vector3(0f, 1.75f, 0f));
        tree.AddChild(trunk);
        tree.AddChild(foliage);
        return tree;
    }

    private void AddRockCluster(Node3D segment, RoadSegmentPlacement placement, float length)
    {
        var rockMaterial = CreateMaterial(new Color(0.42f, 0.39f, 0.33f), roughness: 0.96f);
        for (var index = 0; index < 8; index++)
        {
            var t = (index + 0.35f) / 8f;
            var side = index % 2 == 0 ? -1f : 1f;
            var lateral = side * (RoadWidthMeters * 0.5f + 6f + index % 3 * 2.4f);
            var rock = CreateRoadAlignedBoxMeshNode(
                $"Rock_{index}",
                new Vector3(0.9f + index % 3 * 0.32f, 0.45f + index % 2 * 0.18f, 0.85f + index % 4 * 0.2f),
                rockMaterial,
                placement,
                t,
                lateral,
                SegmentSurfaceY + 0.22f,
                yawOffsetRadians: index * 0.18f);
            segment.AddChild(rock);
        }
    }

    private void AddGuardrail(Node3D segment, RoadSegmentPlacement placement, float length, float side)
    {
        var railMaterial = CreateMaterial(new Color(0.63f, 0.61f, 0.54f), roughness: 0.74f);
        var lateral = side * (RoadWidthMeters * 0.5f + 0.65f);
        var railPieces = 9;
        for (var index = 0; index < railPieces; index++)
        {
            var t = 0.12f + index * 0.76f / (railPieces - 1);
            var rail = CreateRoadAlignedBoxMeshNode(
                $"Guardrail_Rail_{(side < 0 ? "L" : "R")}_{index}",
                new Vector3(0.18f, 0.16f, length * 0.065f),
                railMaterial,
                placement,
                t,
                lateral,
                SegmentSurfaceY + 0.58f);
            segment.AddChild(rail);
        }

        for (var index = 0; index < 7; index++)
        {
            var t = 0.12f + index * 0.76f / 6f;
            var post = CreateRoadAlignedBoxMeshNode(
                $"Guardrail_Post_{index}",
                new Vector3(0.22f, 0.9f, 0.22f),
                railMaterial,
                placement,
                t,
                lateral,
                SegmentSurfaceY + 0.35f);
            segment.AddChild(post);
        }
    }

    private void AddUtilityPoles(Node3D segment, RoadSegmentPlacement placement, float length, float side)
    {
        var material = CreateMaterial(new Color(0.31f, 0.2f, 0.12f), roughness: 0.88f);
        for (var index = 0; index < 3; index++)
        {
            var t = 0.2f + index * 0.3f;
            var lateral = side * (RoadWidthMeters * 0.5f + 10f);
            var pole = new Node3D { Name = $"UtilityPole_{index}" };
            pole.Transform = CreateRoadAlignedTransform(placement, t, lateral, SegmentSurfaceY);
            pole.AddChild(CreateCylinderMeshNode("Pole", 0.13f, 3.8f, material, new Vector3(0f, 1.9f, 0f), radialSegments: 6));
            pole.AddChild(CreateBoxMeshNode("Crossbeam", new Vector3(2.2f, 0.12f, 0.12f), material, new Vector3(0f, 3.15f, 0f)));
            segment.AddChild(pole);
        }
    }

    private void AddRoadSign(Node3D segment, RoadSegmentPlacement placement, string label)
    {
        var poleMaterial = CreateMaterial(new Color(0.28f, 0.28f, 0.24f), roughness: 0.82f);
        var signMaterial = CreateMaterial(new Color(0.17f, 0.28f, 0.22f), roughness: 0.7f, emission: new Color(0.02f, 0.05f, 0.02f));
        var sign = new Node3D { Name = $"Sign_{label}" };
        sign.Transform = CreateRoadAlignedTransform(
            placement,
            t: 0.22f,
            lateralOffset: RoadWidthMeters * 0.5f + 7.0f,
            y: SegmentSurfaceY);
        sign.AddChild(CreateCylinderMeshNode("SignPole", 0.08f, 1.5f, poleMaterial, new Vector3(0f, 0.75f, 0f), radialSegments: 6));
        sign.AddChild(CreateBoxMeshNode("SignBoard", new Vector3(1.45f, 0.75f, 0.08f), signMaterial, new Vector3(0f, 1.55f, 0f)));
        segment.AddChild(sign);
    }

    private void AddRiverEdge(Node3D segment, RoadSegmentPlacement placement, float length, float side)
    {
        var waterMaterial = CreateMaterial(new Color(0.12f, 0.32f, 0.42f), roughness: 0.38f, emission: new Color(0.01f, 0.05f, 0.08f));
        segment.AddChild(new MeshInstance3D
        {
            Name = "RiverEdge",
            Mesh = CreateCurvedStripMesh(
                placement,
                length,
                width: 7.5f,
                lateralOffset: side * (RoadWidthMeters * 0.5f + 22f),
                y: SegmentSurfaceY - 0.018f),
            MaterialOverride = waterMaterial
        });
    }

    private void AddCrosswalk(Node3D segment, RoadSegmentPlacement placement, float length)
    {
        var material = CreateMaterial(new Color(0.87f, 0.84f, 0.72f), roughness: 0.55f);
        for (var index = 0; index < 6; index++)
        {
            var t = 0.66f + (index - 2.5f) * 0.9f / length;
            var stripe = CreateRoadAlignedBoxMeshNode(
                $"Crosswalk_{index}",
                new Vector3(RoadWidthMeters - 1.0f, 0.025f, 0.42f),
                material,
                placement,
                t,
                lateralOffset: 0f,
                y: SegmentSurfaceY + 0.03f);
            segment.AddChild(stripe);
        }
    }

    private void AddShopfronts(Node3D segment, RoadSegmentPlacement placement, float length)
    {
        var wallMaterial = CreateMaterial(new Color(0.48f, 0.28f, 0.18f), roughness: 0.82f);
        var awningMaterial = CreateMaterial(new Color(0.68f, 0.12f, 0.08f), roughness: 0.72f, emission: new Color(0.08f, 0.01f, 0.0f));
        for (var index = 0; index < 3; index++)
        {
            var t = 0.34f + index * 7f / length;
            var shop = new Node3D { Name = $"Shopfront_{index}" };
            shop.Transform = CreateRoadAlignedTransform(
                placement,
                t,
                lateralOffset: -(RoadWidthMeters * 0.5f + 9.0f),
                y: SegmentSurfaceY);
            shop.AddChild(CreateBoxMeshNode("ShopBody", new Vector3(4.6f, 2.4f, 3.3f), wallMaterial, new Vector3(0f, 1.2f, 0f)));
            shop.AddChild(CreateBoxMeshNode("ShopAwning", new Vector3(5.0f, 0.25f, 1.2f), awningMaterial, new Vector3(0f, 2.25f, -1.4f)));
            segment.AddChild(shop);
        }
    }

    private void AddStreetlights(Node3D segment, RoadSegmentPlacement placement, float length)
    {
        var postMaterial = CreateMaterial(new Color(0.22f, 0.22f, 0.2f), roughness: 0.7f);
        var lightMaterial = CreateMaterial(new Color(1.0f, 0.72f, 0.34f), roughness: 0.25f, emission: new Color(0.75f, 0.42f, 0.12f));
        for (var index = 0; index < 3; index++)
        {
            var t = 0.25f + index * 0.22f;
            var lamp = new Node3D { Name = $"Streetlight_{index}" };
            lamp.Transform = CreateRoadAlignedTransform(
                placement,
                t,
                lateralOffset: RoadWidthMeters * 0.5f + 6.2f,
                y: SegmentSurfaceY);
            lamp.AddChild(CreateCylinderMeshNode("LampPost", 0.09f, 3.2f, postMaterial, new Vector3(0f, 1.6f, 0f), radialSegments: 6));
            lamp.AddChild(CreateBoxMeshNode("LampHead", new Vector3(0.8f, 0.2f, 0.35f), lightMaterial, new Vector3(-0.35f, 3.15f, 0f)));
            segment.AddChild(lamp);
        }
    }

    private void AddShrubs(Node3D segment, RoadSegmentPlacement placement, float length)
    {
        var material = CreateMaterial(new Color(0.23f, 0.38f, 0.19f), roughness: 0.91f);
        for (var index = 0; index < 8; index++)
        {
            var side = index % 2 == 0 ? -1f : 1f;
            var t = 0.08f + index * 0.1f;
            var shrub = CreateRoadAlignedBoxMeshNode(
                $"Shrub_{index}",
                new Vector3(1.1f, 0.45f, 0.9f),
                material,
                placement,
                t,
                side * (RoadWidthMeters * 0.5f + 5.8f),
                SegmentSurfaceY + 0.22f,
                yawOffsetRadians: index * 0.4f);
            segment.AddChild(shrub);
        }
    }

    private static Transform3D CreateRoadAlignedTransform(
        RoadSegmentPlacement placement,
        float t,
        float lateralOffset,
        float y,
        float yawOffsetRadians = 0f)
    {
        var sample = RoadSegmentPathSampler.Sample(placement, t);
        var position = RoadSegmentPathSampler.OffsetFromCenter(sample, lateralOffset);
        position.Y = y;
        var basis = RoadSegmentPathSampler.CreateRoadAlignedBasis(sample.Tangent);
        if (MathF.Abs(yawOffsetRadians) > 0.0001f)
        {
            basis = basis.Rotated(Vector3.Up, yawOffsetRadians);
        }

        return new Transform3D(basis, position);
    }

    private MeshInstance3D CreateRoadAlignedBoxMeshNode(
        string name,
        Vector3 size,
        Material material,
        RoadSegmentPlacement placement,
        float t,
        float lateralOffset,
        float y,
        float yawOffsetRadians = 0f)
    {
        return new MeshInstance3D
        {
            Name = name,
            Transform = CreateRoadAlignedTransform(placement, t, lateralOffset, y, yawOffsetRadians),
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = material
        };
    }

    private MeshInstance3D CreateBoxMeshNode(string name, Vector3 size, Material material, Vector3 position)
    {
        return new MeshInstance3D
        {
            Name = name,
            Position = position,
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = material
        };
    }

    private MeshInstance3D CreateCylinderMeshNode(
        string name,
        float radius,
        float height,
        Material material,
        Vector3 position,
        int radialSegments)
    {
        return new MeshInstance3D
        {
            Name = name,
            Position = position,
            Mesh = new CylinderMesh
            {
                TopRadius = radius,
                BottomRadius = radius,
                Height = height,
                RadialSegments = radialSegments
            },
            MaterialOverride = material
        };
    }

    private MeshInstance3D CreateConeMeshNode(string name, float radius, float height, Material material, Vector3 position)
    {
        return new MeshInstance3D
        {
            Name = name,
            Position = position,
            Mesh = new CylinderMesh
            {
                TopRadius = 0.12f,
                BottomRadius = radius,
                Height = height,
                RadialSegments = 7
            },
            MaterialOverride = material
        };
    }

    private Material CreateRoadMaterial(RoadSegmentSpec spec)
    {
        return spec.Surface switch
        {
            RoadSurface.Gravel => CreateMaterial(new Color(0.28f, 0.26f, 0.22f), roughness: 0.95f),
            RoadSurface.Dirt => CreateMaterial(new Color(0.34f, 0.23f, 0.14f), roughness: 0.96f),
            RoadSurface.WetAsphalt => CreateMaterial(new Color(0.07f, 0.08f, 0.09f), roughness: 0.38f, emission: new Color(0.0f, 0.01f, 0.018f)),
            _ => CreateMaterial(new Color(0.12f, 0.13f, 0.14f), roughness: 0.82f)
        };
    }

    private Material CreateShoulderMaterial(RoadSegmentSpec spec)
    {
        return spec.Region switch
        {
            RegionType.Town => CreateMaterial(new Color(0.24f, 0.22f, 0.19f), roughness: 0.86f),
            RegionType.Desert => CreateMaterial(new Color(0.64f, 0.46f, 0.25f), roughness: 0.92f),
            RegionType.Mountain => CreateMaterial(new Color(0.28f, 0.3f, 0.25f), roughness: 0.94f),
            _ => CreateMaterial(new Color(0.21f, 0.32f, 0.2f), roughness: 0.91f)
        };
    }

    private static StandardMaterial3D CreateMaterial(Color albedo, float roughness, Color? emission = null)
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = albedo,
            Roughness = roughness,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };

        if (emission.HasValue)
        {
            material.EmissionEnabled = true;
            material.Emission = emission.Value;
            material.EmissionEnergyMultiplier = 0.55f;
        }

        return material;
    }
}
