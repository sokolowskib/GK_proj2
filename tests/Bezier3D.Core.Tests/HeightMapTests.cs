using System.Numerics;
using Bezier3D.Core.Rendering;

namespace Bezier3D.Core.Tests;

public class HeightMapTests
{
    private const float Eps = 1e-4f;
    private static readonly float S = MathF.Sqrt(2) / 2; // ≈ 0.707

    private static void AssertClose(Vector3 expected, Vector3 actual) =>
        Assert.True(Vector3.Distance(expected, actual) < Eps, $"oczekiwano {expected}, jest {actual}");

    // obraz 4×1, wysokość w R
    private static Texture Row(params float[] h) => new(h.Length, 1, h.Select(x => new Vector3(x)).ToArray());

    [Fact]
    public void FlatMapKeepsNormal()
    {
        // stała wysokość → H_u = H_v = 0 → D = 0
        var r = HeightMap.Apply(Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, Row(0.3f, 0.3f, 0.3f, 0.3f), 0.4f, 0.5f, 50);
        AssertClose(Vector3.UnitZ, r);
    }

    [Fact]
    public void RampInUTiltsNormalAgainstPu()
    {
        // środek piksela 1: sąsiedzi 0 i 0.5 → H_u = 0.25; k = 4 → D = 4·0.25·(z × y) = (−1, 0, 0)
        var r = HeightMap.Apply(Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, Row(0, 0.25f, 0.5f, 0.75f), 1.5f / 4, 0.5f, 4);
        AssertClose(new(-S, 0, S), r);
    }
}
