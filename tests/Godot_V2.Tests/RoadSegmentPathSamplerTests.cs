using Godot;
using Godot_V2.Scripts.World;
using NUnit.Framework;

namespace Godot_V2.Tests;

public sealed class RoadSegmentPathSamplerTests
{
    [Test]
    public void CurvedSegmentSamplesExposeChangingTangents()
    {
        var placement = CreatePlacement("s_curve", sequenceIndex: 0);
        var start = RoadSegmentPathSampler.Sample(placement, t: 0.05f);
        var middle = RoadSegmentPathSampler.Sample(placement, t: 0.5f);
        var end = RoadSegmentPathSampler.Sample(placement, t: 0.95f);

        Assert.That(start.Tangent.Length(), Is.EqualTo(1f).Within(0.001f));
        Assert.That(start.Right.Length(), Is.EqualTo(1f).Within(0.001f));
        Assert.That(Mathf.Abs(start.Tangent.Dot(start.Right)), Is.LessThan(0.001f));
        Assert.That(start.Tangent.X, Is.GreaterThan(0.01f));
        Assert.That(Mathf.Abs(middle.Tangent.X), Is.LessThan(0.01f));
        Assert.That(end.Tangent.X, Is.LessThan(-0.01f));
    }

    [Test]
    public void LateralOffsetsMovePerpendicularToRoadTangent()
    {
        var placement = CreatePlacement("gentle_curve", sequenceIndex: 1);
        var sample = RoadSegmentPathSampler.Sample(placement, t: 0.32f);

        var left = RoadSegmentPathSampler.OffsetFromCenter(sample, lateralOffsetMeters: -4.5f);
        var right = RoadSegmentPathSampler.OffsetFromCenter(sample, lateralOffsetMeters: 4.5f);
        var lateralDelta = right - left;

        Assert.That(lateralDelta.Dot(sample.Tangent), Is.EqualTo(0f).Within(0.001f));
        Assert.That(lateralDelta.Dot(sample.Right), Is.EqualTo(9f).Within(0.001f));
    }

    [Test]
    public void RoadAlignedBasisPointsLocalForwardAlongTangent()
    {
        var placement = CreatePlacement("s_curve", sequenceIndex: 0);
        var sample = RoadSegmentPathSampler.Sample(placement, t: 0.2f);

        var basis = RoadSegmentPathSampler.CreateRoadAlignedBasis(sample.Tangent);

        Assert.That((-basis.Z).Dot(sample.Tangent), Is.EqualTo(1f).Within(0.001f));
        Assert.That(basis.X.Dot(sample.Right), Is.EqualTo(1f).Within(0.001f));
    }

    private static RoadSegmentPlacement CreatePlacement(string segmentId, int sequenceIndex)
    {
        var spec = RoadSegmentCatalog.CreateDefaults().Single(segment => segment.Id == segmentId);
        return new RoadSegmentPlacement(
            sequenceIndex,
            spec.Id,
            StartDistanceMeters: 0f,
            EndDistanceMeters: spec.LengthMeters,
            spec);
    }
}
