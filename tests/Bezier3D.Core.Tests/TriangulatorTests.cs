using System.Numerics;
using Bezier3D.Core.Surfaces;

namespace Bezier3D.Core.Tests;

public class TriangulatorTests
{
    private const float Eps = 1e-3f;

    // V[i, j] = (i, j, i·j) — wygięta, żeby rogi i środek się różniły
    private static BezierSurface Surface()
    {
        var v = new Vector3[4, 4];
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                v[i, j] = new Vector3(i, j, i * j);
        return new BezierSurface(v);
    }

    private static IEnumerable<MeshVertex> Vertices(List<Triangle> tris) =>
        tris.SelectMany(t => new[] { t.A, t.B, t.C });

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void TriangleCountIsTwoNSquared(int n)
    {
        Assert.Equal(2 * n * n, Triangulator.Triangulate(Surface(), n).Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void VerticesAreSharedNotCopied(int n)
    {
        // ReferenceEquality: liczymy różne obiekty, nie różne wartości
        var distinct = Vertices(Triangulator.Triangulate(Surface(), n)).Distinct(ReferenceEqualityComparer.Instance);
        Assert.Equal((n + 1) * (n + 1), distinct.Count());
    }

    [Fact]
    public void ParametersAreOnTheGrid()
    {
        var uvs = Vertices(Triangulator.Triangulate(Surface(), 2)).Select(p => (p.U, p.V)).Distinct().ToList();
        float[] steps = [0, 0.5f, 1];

        Assert.Equal(9, uvs.Count);
        foreach (var u in steps)
            foreach (var v in steps)
                Assert.Contains((u, v), uvs);
    }

    [Fact]
    public void VertexHoldsEvaluateResult()
    {
        var s = Surface();
        foreach (var p in Vertices(Triangulator.Triangulate(s, 3)))
        {
            var (P, Pu, Pv, N) = s.Evaluate(p.U, p.V);
            Assert.True(Vector3.Distance(P, p.P) < Eps);
            Assert.True(Vector3.Distance(Pu, p.Pu) < Eps);
            Assert.True(Vector3.Distance(Pv, p.Pv) < Eps);
            Assert.True(Vector3.Distance(N, p.N) < Eps);
        }
    }

    [Fact]
    public void CornersAreSurfaceCorners()
    {
        var ps = Vertices(Triangulator.Triangulate(Surface(), 4)).Select(p => p.P).ToList();
        Vector3[] corners = [new(0, 0, 0), new(3, 0, 0), new(0, 3, 0), new(3, 3, 9)];

        foreach (var c in corners)
            Assert.Contains(ps, p => Vector3.Distance(p, c) < Eps);
    }

    [Fact]
    public void TrianglesAreNotDegenerate()
    {
        // każdy trójkąt ma niezerowe pole w (u, v) — złe indeksy dają zwykle dwa takie same rogi
        foreach (var t in Triangulator.Triangulate(Surface(), 3))
        {
            float area = (t.B.U - t.A.U) * (t.C.V - t.A.V) - (t.C.U - t.A.U) * (t.B.V - t.A.V);
            Assert.True(MathF.Abs(area) > 1e-6f, $"zdegenerowany: ({t.A.U},{t.A.V}) ({t.B.U},{t.B.V}) ({t.C.U},{t.C.V})");
        }
    }

    [Fact]
    public void TrianglesCoverWholeSquare()
    {
        // suma pól w (u, v) = pole kwadratu [0,1]² — brak dziur i nakładek
        float sum = Triangulator.Triangulate(Surface(), 3).Sum(t =>
            MathF.Abs((t.B.U - t.A.U) * (t.C.V - t.A.V) - (t.C.U - t.A.U) * (t.B.V - t.A.V)) / 2);
        Assert.Equal(1f, sum, 1e-4f);
    }
}
