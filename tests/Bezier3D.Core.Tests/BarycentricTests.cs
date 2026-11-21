using System.Numerics;
using Bezier3D.Core.Rendering;

namespace Bezier3D.Core.Tests;

public class BarycentricTests
{
    private const float Eps = 1e-4f;
    private static readonly Vector2 A = new(10, 10), B = new(50, 20), C = new(20, 60);

    private static void AssertClose(Vector3 expected, Vector3 actual) =>
        Assert.True(Vector3.Distance(expected, actual) < Eps, $"oczekiwano {expected}, jest {actual}");

    [Fact]
    public void CrossOfAxes()
    {
        Assert.Equal(1f, Barycentric.Cross(Vector2.UnitX, Vector2.UnitY));
        Assert.Equal(-1f, Barycentric.Cross(Vector2.UnitY, Vector2.UnitX));
        Assert.Equal(-2f, Barycentric.Cross(new(1, 2), new(3, 4))); // 1·4 − 2·3
    }

    [Fact]
    public void VerticesGiveUnitWeights()
    {
        AssertClose(new(1, 0, 0), Barycentric.Compute(A, B, C, A));
        AssertClose(new(0, 1, 0), Barycentric.Compute(A, B, C, B));
        AssertClose(new(0, 0, 1), Barycentric.Compute(A, B, C, C));
    }

    [Fact]
    public void CentroidGivesThirds()
    {
        AssertClose(new(1 / 3f, 1 / 3f, 1 / 3f), Barycentric.Compute(A, B, C, (A + B + C) / 3));
    }

    [Fact]
    public void MidpointOfEdgeAB()
    {
        AssertClose(new(0.5f, 0.5f, 0), Barycentric.Compute(A, B, C, (A + B) / 2));
    }

    [Fact]
    public void OrientationDoesNotMatter()
    {
        // ten sam trójkąt podany w drugą stronę — znaki pól się skracają
        var p = new Vector2(25, 30);
        var w = Barycentric.Compute(A, B, C, p);
        var w2 = Barycentric.Compute(A, C, B, p);
        AssertClose(new(w.X, w.Z, w.Y), w2);
    }

    [Fact]
    public void WeightsSumToOneAndRecoverPoint()
    {
        var p = new Vector2(27.3f, 31.9f);
        var w = Barycentric.Compute(A, B, C, p);

        Assert.Equal(1f, w.X + w.Y + w.Z, Eps);
        // α·A + β·B + γ·C = P
        var back = new Vector3(A, 0) * w.X + new Vector3(B, 0) * w.Y + new Vector3(C, 0) * w.Z;
        AssertClose(new Vector3(p, 0), back);
    }

    [Fact]
    public void InterpolateFloat()
    {
        Assert.Equal(20f, Barycentric.Interpolate(new Vector3(0.5f, 0.25f, 0.25f), 10, 20, 40), Eps); // 5 + 5 + 10
    }

    [Fact]
    public void InterpolateVector()
    {
        var r = Barycentric.Interpolate(new Vector3(0.5f, 0.5f, 0), Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);
        AssertClose(new(0.5f, 0.5f, 0), r);
    }

    [Fact]
    public void InterpolatedZOfPlaneIsExact()
    {
        // płaszczyzna z = 2x + 3y + 1: z interpolowane w pikselu = z płaszczyzny w tym punkcie
        static float Z(Vector2 q) => 2 * q.X + 3 * q.Y + 1;
        var p = new Vector2(30.5f, 25.5f);
        var w = Barycentric.Compute(A, B, C, p);
        Assert.Equal(Z(p), Barycentric.Interpolate(w, Z(A), Z(B), Z(C)), 1e-2f);
    }
}
