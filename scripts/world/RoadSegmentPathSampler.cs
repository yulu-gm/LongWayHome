using System;
using Godot;

namespace Godot_V2.Scripts.World;

public sealed record RoadPathSample(
    Vector3 Center,
    Vector3 Tangent,
    Vector3 Right,
    float T);

public static class RoadSegmentPathSampler
{
    public const float CurveAmplitudeMeters = 8f;

    public static RoadPathSample Sample(RoadSegmentPlacement placement, float t)
    {
        var clampedT = Mathf.Clamp(t, 0f, 1f);
        var length = MathF.Max(0.001f, placement.EndDistanceMeters - placement.StartDistanceMeters);
        var curveSign = placement.SequenceIndex % 2 == 0 ? 1f : -1f;
        var amplitude = curveSign * placement.Spec.CurveStrength * CurveAmplitudeMeters;
        var x = amplitude * MathF.Sin(clampedT * MathF.PI);
        var z = length * 0.5f - clampedT * length;
        var tangent = new Vector3(
            amplitude * MathF.PI / length * MathF.Cos(clampedT * MathF.PI),
            0f,
            -1f).Normalized();
        var right = tangent.Cross(Vector3.Up).Normalized();

        return new RoadPathSample(
            new Vector3(x, 0f, z),
            tangent,
            right,
            clampedT);
    }

    public static Vector3 OffsetFromCenter(RoadPathSample sample, float lateralOffsetMeters) =>
        sample.Center + sample.Right * lateralOffsetMeters;

    public static Basis CreateRoadAlignedBasis(Vector3 tangent) =>
        Basis.LookingAt(tangent.Normalized(), Vector3.Up);
}
