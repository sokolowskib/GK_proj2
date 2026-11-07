using System.Numerics;
using Bezier3D.Core.Surfaces;

namespace Bezier3D.Core.Tests;

public class BezierSurfaceTests
{
    private const float Eps = 1e-3f;

    // Punkty na równej kratce: V[i, j] = (x_i, y_j, z(x_i, y_j)), x_i, y_j ∈ {0, 1, 2, 3}.
    private static BezierSurface Grid(Func<float, float, float> z)
    {
        var v = new Vector3[4, 4];
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                v[i, j] = new Vector3(i, j, z(i, j));
        return new BezierSurface(v);
    }

    private static void AssertClose(Vector3 expected, Vector3 actual) =>
        Assert.True(Vector3.Distance(expected, actual) < Eps, $"oczekiwano {expected}, jest {actual}");

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 0, 3, 0)]
    [InlineData(0, 1, 0, 3)]
    [InlineData(1, 1, 3, 3)]
    public void CornersAreCornerControlPoints(float u, float v, int i, int j)
    {
        var s = Grid((x, y) => x * y);

        AssertClose(s.V[i, j], s.Evaluate(u, v).P);
    }

    [Fact]
    public void FlatGridIsBilinear()
    {
        // Równo rozłożone punkty kontrolne → P(u,v) = (3u, 3v, 0), Pu = (3,0,0), Pv = (0,3,0).
        var (p, pu, pv, n) = Grid((_, _) => 0).Evaluate(0.25f, 0.6f);

        AssertClose(new Vector3(0.75f, 1.8f, 0), p);
        AssertClose(new Vector3(3, 0, 0), pu);
        AssertClose(new Vector3(0, 3, 0), pv);
        AssertClose(new Vector3(0, 0, 1), n);
    }

    [Fact]
    public void TiltedPlaneHasConstantNormal()
    {
        // Płaszczyzna z = x: normalna ∝ (−1, 0, 1).
        var expected = Vector3.Normalize(new Vector3(-1, 0, 1));
        var s = Grid((x, _) => x);

        AssertClose(expected, s.Evaluate(0.1f, 0.9f).N);
        AssertClose(expected, s.Evaluate(0.7f, 0.3f).N);
    }

    [Fact]
    public void TangentsMatchFiniteDifferences()
    {
        var s = Grid((x, y) => (x - 1.5f) * (x - 1.5f) - y * x);
        const float u = 0.3f, v = 0.7f, h = 1e-3f;

        var (_, pu, pv, n) = s.Evaluate(u, v);

        AssertClose((s.Evaluate(u + h, v).P - s.Evaluate(u - h, v).P) / (2 * h), pu);
        AssertClose((s.Evaluate(u, v + h).P - s.Evaluate(u, v - h).P) / (2 * h), pv);
        Assert.Equal(1f, n.Length(), 3);
        Assert.True(n.Z > 0);
    }
}
